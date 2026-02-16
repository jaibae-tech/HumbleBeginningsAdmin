# Git / Patch Workflow

This project uses Git as the shared baseline. Changes are exchanged as unified-diff patches and applied locally in Unity.

---

## Start of Day (Always First)

Open Git Bash in project root:

```bash
cd /h/UnityProjects/HumbleBeginningsAdmin
git status -sb
git fetch origin --prune
git pull
```

Verify:
- Working tree is clean (no modified/untracked files you did not intend)
- You are on the correct branch

If not clean:

```bash
git status
git diff
```

Resolve unexpected changes before continuing.

---

## Create a Work Branch

Never work directly on `main`.

```bash
git checkout -b codex/<topic>
git push -u origin HEAD
```

---

## Normal Development Cycle

After edits inside Unity or IDE:

```bash
git status -sb
git diff
```

Commit early:

```bash
git add -A
git commit -m "<module>: <summary>"
git push
```

---

## When Unity Shows Compile Errors

Send to ChatGPT:
1) Full Unity Console error output (include stack traces)
2) Output of:

```bash
git status -sb
git diff
```

Do not upload files unless specifically needed.

---

## Applying ChatGPT Patches (Unity Tool)

In Unity:
**Tools → Patch Applier (Unified Diff)**

Steps:
1) Paste patch text
2) Validate
3) Apply Patch
4) Allow Unity to refresh and recompile

---

## After Patch Applies Cleanly

```bash
git add -A
git commit -m "Apply patch: <summary>"
git push
```

---

## Merge Stable Work to Main

```bash
git checkout main
git pull
git merge --no-ff codex/<topic>
git push
```

Optional cleanup:

```bash
git branch -d codex/<topic>
```

---

## Handling Unintended Changes

Revert a single file:

```bash
git restore <path>
```

Discard all local changes (use with care):

```bash
git reset --hard
```