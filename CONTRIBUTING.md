# Contributing to 7th Heaven

## Before opening a pull request

1. Fork [7th Heaven](https://github.com/tsunamods-codes/7th-Heaven) on GitHub. Clone your fork and create a branch from the upstream `master` branch. Replace `YOUR-USERNAME` with your GitHub username and `my-change` with a short branch name:

   ```powershell
   git clone https://github.com/YOUR-USERNAME/7th-Heaven.git
   Set-Location 7th-Heaven
   git remote add upstream https://github.com/tsunamods-codes/7th-Heaven.git
   git fetch upstream
   git switch -c my-change upstream/master
   ```

2. Follow the [build setup](README.md#build) and build **AppUI**. If you use VS Code, install the recommended **EditorConfig for VS Code** extension.
3. The first local **AppUI** build installs the pre-commit hook. If you need to commit before building, run this once from the repository root:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\.scripts\Install-IndentationHook.ps1
   ```

   If the installer warns about an existing hook or `core.hooksPath`, it leaves your setup unchanged. Add the checker to your active pre-commit hook instead: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .scripts/Check-Indentation.ps1 -FixStaged`.
4. Make your changes, rebuild if you changed code, and test the affected behavior. Stage only the files you intend to submit and commit normally, without `--no-verify`. The hook fixes indentation on added or edited staged lines and leaves unstaged edits alone.
5. Check the committed changes locally before pushing. The checker compares your branch to its merge base with `upstream/master`; it does not check uncommitted edits.

   ```powershell
   git fetch upstream
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\.scripts\Check-Indentation.ps1 -Base upstream/master
   ```

6. Once the check passes, run `git push -u origin my-change`. On GitHub, open a pull request from your fork's `my-change` branch into `tsunamods-codes/7th-Heaven:master`. Describe what changed, why, and how you tested it.

## Fixing indentation

The check reports the file and line number for each violation. Use spaces, not tabs: **four spaces per level** for C#, C++, and XAML; **two spaces per level** for project files, PowerShell scripts, and YAML. See [.editorconfig](.editorconfig) for the full list of file types. Fix the reported lines without reformatting unrelated code.

Stage and commit the fixes. Run the local check from step 5 again, then push. If a pull request is already open, pushing to the same branch updates it and reruns CI.
