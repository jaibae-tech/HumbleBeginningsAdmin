// Assets/Editor/PatchApplier/UnifiedDiffPatchApplierWindow.cs
// Minimal unified-diff patch applier with backups.
// Supports: modify existing text files (UTF-8), create new files, delete files.
// Limitations: does not apply binary patches; expects standard "diff --git" blocks.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class UnifiedDiffPatchApplierWindow : EditorWindow
{
    private string _patchText = "";
    private Vector2 _scroll;
    private string _status = "Paste a unified diff patch, then Validate, then Apply.";

    private const string BackupRoot = "Assets/Editor/PatchApplier/_PatchBackups";

    [MenuItem("Tools/Patch Applier (Unified Diff)")]
    public static void ShowWindow()
    {
        var w = GetWindow<UnifiedDiffPatchApplierWindow>("Patch Applier");
        w.minSize = new Vector2(720, 520);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unified Diff Patch", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1) Paste patch text (diff --git ...)\n" +
            "2) Validate\n" +
            "3) Apply Patch (backs up files first)\n\n" +
            "Notes:\n" +
            "- Applies text patches only.\n" +
            "- Uses Unity project-relative paths like Assets/... or Packages/...\n" +
            "- Backups stored under: " + BackupRoot,
            MessageType.Info);

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;
            _patchText = EditorGUILayout.TextArea(_patchText, GUILayout.ExpandHeight(true));
        }

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate", GUILayout.Height(32)))
            {
                _status = ValidatePatch(_patchText);
            }

            GUI.enabled = !string.IsNullOrWhiteSpace(_patchText);
            if (GUILayout.Button("Apply Patch", GUILayout.Height(32)))
            {
                try
                {
                    var result = ApplyPatch(_patchText);
                    _status = result;
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    _status = "ERROR: " + ex.Message;
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear", GUILayout.Height(32)))
            {
                _patchText = "";
                _status = "Cleared.";
            }
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(_status, GUILayout.MinHeight(80));
    }

    private static string ValidatePatch(string patch)
    {
        if (string.IsNullOrWhiteSpace(patch))
            return "No patch text provided.";

        var parsed = ParseDiffBlocks(patch, out var errors);
        if (errors.Count > 0)
            return "Validation failed:\n- " + string.Join("\n- ", errors);

        if (parsed.Count == 0)
            return "No diff blocks found. Expected lines starting with 'diff --git'.";

        // Basic path validation
        var bad = parsed
            .SelectMany(b => new[] { b.OldPath, b.NewPath })
            .Where(p => !string.IsNullOrEmpty(p))
            .Where(p => !(p.StartsWith("Assets/") || p.StartsWith("Packages/") || p.StartsWith("ProjectSettings/")))
            .Distinct()
            .ToList();

        if (bad.Count > 0)
            return "Validation failed: patch references non-project paths:\n- " + string.Join("\n- ", bad);

        return $"Validation OK. Diff blocks: {parsed.Count}.";
    }

    private static string ApplyPatch(string patch)
    {
        var blocks = ParseDiffBlocks(patch, out var errors);
        if (errors.Count > 0)
            throw new InvalidOperationException("Patch parse errors:\n- " + string.Join("\n- ", errors));
        if (blocks.Count == 0)
            throw new InvalidOperationException("No diff blocks found.");

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupDir = Path.Combine(BackupRoot, timestamp).Replace("\\", "/");
        Directory.CreateDirectory(backupDir);

        int filesChanged = 0;
        int hunksApplied = 0;
        var summary = new StringBuilder();
        summary.AppendLine("Backup: " + backupDir);

        foreach (var block in blocks)
        {
            // Determine operation type
            bool isDelete = block.NewPath == "/dev/null";
            bool isCreate = block.OldPath == "/dev/null";
            string targetPath = isDelete ? block.OldPath : block.NewPath;

            if (string.IsNullOrEmpty(targetPath) || targetPath == "/dev/null")
                throw new InvalidOperationException("Invalid target path in diff block.");

            if (!IsProjectPath(targetPath))
                throw new InvalidOperationException("Refusing to patch non-project path: " + targetPath);

            if (isDelete)
            {
                if (File.Exists(targetPath))
                {
                    BackupFile(targetPath, backupDir);
                    File.Delete(targetPath);
                    filesChanged++;
                    summary.AppendLine($"DELETE {targetPath}");
                }
                else
                {
                    summary.AppendLine($"SKIP delete (missing) {targetPath}");
                }
                continue;
            }

            if (isCreate)
            {
                // Start from empty content
                var content = new List<string>();
                var (newLines, applied) = ApplyHunks(content, block.Hunks, targetPath);
                hunksApplied += applied;
                EnsureParentDir(targetPath);
                BackupIfExists(targetPath, backupDir);
                WriteAllLinesUtf8(targetPath, newLines);
                filesChanged++;
                summary.AppendLine($"CREATE {targetPath} (hunks {applied})");
                continue;
            }

            // Modify existing
            if (!File.Exists(targetPath))
                throw new FileNotFoundException("Target file not found: " + targetPath);

            BackupFile(targetPath, backupDir);

            var original = ReadAllLinesUtf8(targetPath);
            var (patched, appliedCount) = ApplyHunks(original, block.Hunks, targetPath);

            hunksApplied += appliedCount;

            WriteAllLinesUtf8(targetPath, patched);
            filesChanged++;
            summary.AppendLine($"PATCH {targetPath} (hunks {appliedCount})");
        }

        summary.AppendLine();
        summary.AppendLine($"Done. Files changed: {filesChanged}, Hunks applied: {hunksApplied}.");
        return summary.ToString();
    }

    private static bool IsProjectPath(string p)
        => p.StartsWith("Assets/") || p.StartsWith("Packages/") || p.StartsWith("ProjectSettings/");

    private static void EnsureParentDir(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static void BackupIfExists(string filePath, string backupRoot)
    {
        if (File.Exists(filePath))
            BackupFile(filePath, backupRoot);
    }

    private static void BackupFile(string filePath, string backupRoot)
    {
        var safeRel = filePath.Replace(":", "").Replace("\\", "/");
        var dest = Path.Combine(backupRoot, safeRel).Replace("\\", "/");
        EnsureParentDir(dest);
        File.Copy(filePath, dest, true);
    }

    private static List<string> ReadAllLinesUtf8(string path)
    {
        // Preserve \n semantics in patching by splitting on \n.
        var text = File.ReadAllText(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        // If file ends with newline, Split gives trailing empty item; keep it (patches depend on it).
        return lines;
    }

    private static void WriteAllLinesUtf8(string path, List<string> lines)
    {
        var text = string.Join("\n", lines);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    // ---------- Patch parsing ----------

    private sealed class DiffBlock
    {
        public string OldPath; // "Assets/..." or "/dev/null"
        public string NewPath; // "Assets/..." or "/dev/null"
        public readonly List<Hunk> Hunks = new List<Hunk>();
    }

    private sealed class Hunk
    {
        public int OldStart, OldCount;
        public int NewStart, NewCount;
        public readonly List<string> Lines = new List<string>(); // includes leading ' ', '+', '-'
    }

    private static List<DiffBlock> ParseDiffBlocks(string patch, out List<string> errors)
    {
        errors = new List<string>();
        var blocks = new List<DiffBlock>();

        var lines = patch.Replace("\r\n", "\n").Split('\n');
        int i = 0;

        DiffBlock current = null;

        while (i < lines.Length)
        {
            var line = lines[i];

            if (line.StartsWith("diff --git "))
            {
                current = new DiffBlock();
                blocks.Add(current);
                i++;
                continue;
            }

            if (current == null)
            {
                i++;
                continue;
            }

            if (line.StartsWith("--- "))
            {
                current.OldPath = NormalizePath(line.Substring(4).Trim());
                i++;
                continue;
            }

            if (line.StartsWith("+++ "))
            {
                current.NewPath = NormalizePath(line.Substring(4).Trim());
                i++;
                continue;
            }

            if (line.StartsWith("@@ "))
            {
                var h = ParseHunkHeader(line, errors);
                if (h == null)
                {
                    i++;
                    continue;
                }

                i++;
                while (i < lines.Length)
                {
                    var l2 = lines[i];
                    if (l2.StartsWith("diff --git ") || l2.StartsWith("@@ "))
                        break;

                    // Hunk body lines begin with ' ', '+', '-', or '\'
                    if (l2.Length == 0 || l2[0] == ' ' || l2[0] == '+' || l2[0] == '-' || l2[0] == '\\')
                        h.Lines.Add(l2);

                    i++;
                }

                current.Hunks.Add(h);
                continue;
            }

            i++;
        }

        // Validate each block has paths
        foreach (var b in blocks)
        {
            if (string.IsNullOrEmpty(b.OldPath) || string.IsNullOrEmpty(b.NewPath))
                errors.Add("Diff block missing ---/+++ paths. Ensure patch includes '--- a/...' and '+++ b/...'.");
        }

        return blocks;
    }

    private static string NormalizePath(string raw)
    {
        // raw is like "a/Assets/Foo.cs" or "b/Assets/Foo.cs" or "/dev/null"
        if (raw == "/dev/null") return "/dev/null";
        if (raw.StartsWith("a/") || raw.StartsWith("b/"))
            return raw.Substring(2);
        return raw;
    }

    private static Hunk ParseHunkHeader(string header, List<string> errors)
    {
        // @@ -oldStart,oldCount +newStart,newCount @@
        try
        {
            int first = header.IndexOf(" -", StringComparison.Ordinal);
            int plus = header.IndexOf(" +", StringComparison.Ordinal);
            int end = header.IndexOf(" @@", StringComparison.Ordinal);
            if (first < 0 || plus < 0 || end < 0) throw new FormatException();

            var oldPart = header.Substring(first + 2, plus - (first + 2)).Trim(); // "12,3"
            var newPart = header.Substring(plus + 2, end - (plus + 2)).Trim();   // "12,4"

            ParseRange(oldPart, out int os, out int oc);
            ParseRange(newPart, out int ns, out int nc);

            return new Hunk { OldStart = os, OldCount = oc, NewStart = ns, NewCount = nc };
        }
        catch
        {
            errors.Add("Failed to parse hunk header: " + header);
            return null;
        }
    }

    private static void ParseRange(string part, out int start, out int count)
    {
        // "12,3" or "12"
        var pieces = part.Split(',');
        start = int.Parse(pieces[0]);
        count = (pieces.Length > 1) ? int.Parse(pieces[1]) : 1;
    }

    // ---------- Hunk application ----------

    private static (List<string> patched, int hunksApplied) ApplyHunks(List<string> original, List<Hunk> hunks, string pathForErrors)
    {
        // Work on a copy
        var cur = new List<string>(original);
        int applied = 0;

        // Apply hunks in order; adjust offsets as we go.
        int lineOffset = 0;

        foreach (var h in hunks)
        {
            // Convert 1-based start to 0-based index
            int expectedIndex = Math.Max(0, (h.OldStart - 1) + lineOffset);

            // Build the "old" lines from hunk (lines starting with ' ' or '-')
            var oldSeq = new List<string>();
            foreach (var l in h.Lines)
            {
                if (l.StartsWith("\\"))
                    continue; // "\ No newline at end of file" - ignore
                if (l.Length == 0)
                {
                    // empty line in hunk body is treated as context with empty string (rare)
                    oldSeq.Add("");
                    continue;
                }

                char c = l[0];
                string content = l.Substring(1);
                if (c == ' ' || c == '-')
                    oldSeq.Add(content);
            }

            // Build the "new" lines from hunk (lines starting with ' ' or '+')
            var newSeq = new List<string>();
            foreach (var l in h.Lines)
            {
                if (l.StartsWith("\\"))
                    continue;
                if (l.Length == 0)
                {
                    newSeq.Add("");
                    continue;
                }

                char c = l[0];
                string content = l.Substring(1);
                if (c == ' ' || c == '+')
                    newSeq.Add(content);
            }

            // Try apply at expectedIndex; if mismatch, search a small window.
            int foundIndex = FindSequence(cur, oldSeq, expectedIndex, window: 50);
            if (foundIndex < 0)
                throw new InvalidOperationException($"Hunk failed for {pathForErrors} near original line {h.OldStart}. Could not locate context.");

            // Replace oldSeq with newSeq
            cur.RemoveRange(foundIndex, oldSeq.Count);
            cur.InsertRange(foundIndex, newSeq);

            // Update offset for subsequent hunks
            lineOffset += (newSeq.Count - oldSeq.Count);
            applied++;
        }

        return (cur, applied);
    }

    private static int FindSequence(List<string> haystack, List<string> needle, int expectedIndex, int window)
    {
        if (needle.Count == 0)
            return expectedIndex; // insertion

        int start = Math.Max(0, expectedIndex - window);
        int end = Math.Min(haystack.Count - needle.Count, expectedIndex + window);

        // First try exact expectedIndex
        if (expectedIndex >= 0 && expectedIndex <= haystack.Count - needle.Count && MatchesAt(haystack, needle, expectedIndex))
            return expectedIndex;

        for (int i = start; i <= end; i++)
        {
            if (MatchesAt(haystack, needle, i))
                return i;
        }
        return -1;
    }

    private static bool MatchesAt(List<string> haystack, List<string> needle, int index)
    {
        for (int j = 0; j < needle.Count; j++)
        {
            if (index + j >= haystack.Count) return false;
            if (!string.Equals(haystack[index + j], needle[j], StringComparison.Ordinal))
                return false;
        }
        return true;
    }
}

