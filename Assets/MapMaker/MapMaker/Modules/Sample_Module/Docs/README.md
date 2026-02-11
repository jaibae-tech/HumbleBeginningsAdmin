# Sample Module Template

Copy this entire folder to create a new module:
`Assets/MapMaker/MapMaker/Modules/<NN_ModuleName>/`

Then:
- Rename namespaces from `Modules.Sample_Module` to your module name.
- Rename `HB_SampleModuleConfig` to `HB_<ModuleName>Config`.
- Update LogContext/LogPhase selections for your module.
- Add your module's inputs/outputs to `ModuleNotes.md` and record changes in `PatchLog.md`.

This template intentionally does **no** world generation work.
