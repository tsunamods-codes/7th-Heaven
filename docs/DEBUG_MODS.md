# Debugging Mods with 7th Heaven

Use this guide to collect and read a detailed log when a mod causes problems while playing through 7th Heaven.

## Create a Debug Log

1. Start 7th Heaven and enable the mods needed to reproduce the problem.
2. Open the menu beside the main **Play** button and choose **Play With Debug Log**.
3. Accept the warning, launch the game, and reproduce the issue. Note the steps and approximate point where it occurs.
4. Close the game, then open `log.txt` in the folder containing the FF7 executable configured in 7th Heaven.

For example, if the game is installed in `C:\Games\FF7`, the log is `C:\Games\FF7\log.txt`. Normal **Play** does not request this detailed AppWrapper log. Each debug run starts a fresh log and removes the previous one, so copy the file before starting another debug run. AppWrapper flushes each log entry as it is written; after a crash, entries written before the crash should still be available.

## What the Log Contains

The log is written by AppWrapper, the in-process mod and file-override wrapper. At startup it records the process and game paths, monitor paths, active mod folders, conditional and extra folders, and the status of wrapper initialization. It also records HEXT patch application and errors.

With detailed logging enabled, file access and override lines can show which mod file supplied a requested game file. Search the log for the affected filename, the mod name or path, and terms such as `Error`, `Exception`, `Failed`, `remapping`, or `Wrapper startup complete`.

- `Starting wrapper now` and the process/path entries confirm that AppWrapper started and show which game installation it sees.
- Under `Loading mods`, check that the expected mods are present and inspect their conditional and extra folders. A missing mod or folder can point to activation, configuration, or package-layout issues.
- `Loading hext patches` shows patch files that AppWrapper found. `Error applying patch` reports a patch instruction it could not process. A bad memory patch can also crash or alter the game without producing a useful log error.
- `CreateFileW remapping ... to ...` means a requested game file was redirected to a mod file. If no remapping appears, the game may not have requested that file, or the package path/condition may not match the request.
- `Wrapper startup complete` means wrapper setup finished. If it is missing, inspect the final entries for an exception or failed startup step.

The log is not a complete trace of the game's internal state. A file not mentioned in a remapping line is not necessarily broken: the game may not have requested it during that run. The log also does not automatically contain every message or exception from a mod's own code, FFNx, or the game engine.

## Narrow Down a Problem

- Reproduce it with **Play With Debug Log** and record the exact steps, enabled mods, load order, and relevant mod settings.
- If appropriate, disable unrelated mods and run the same reproduction again with debug logging. If the issue stops, re-enable mods in groups to identify an interaction.
- Check whether the expected mod and its folders appear in the startup section, then whether the expected file is remapped when the game reaches the affected area.
- For a HEXT issue, check the `Loading hext patches` section for `Error applying patch`. If you suspect a mod, deactivate it and repeat the same steps to see whether the problem persists.

When asking for help, include the `log.txt`, a short reproduction, the game executable/version, and the enabled mod list and order. Include relevant configuration values. The log can contain local file paths and other environment details; review it before sharing publicly.
