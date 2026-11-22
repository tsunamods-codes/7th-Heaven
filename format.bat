@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 (
    echo Could not open the project directory.
    pause
    exit /b 1
)

echo Restoring formatter...
dotnet tool restore
if errorlevel 1 goto failed

echo Formatting C# and XML/XAML...
dotnet csharpier format .
if errorlevel 1 goto failed

echo Formatting C++...
powershell.exe -NoProfile -Command "$ErrorActionPreference = 'Stop'; $vs = & \"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe\" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }; $clangFormat = \"$vs\VC\Tools\Llvm\x64\bin\clang-format.exe\"; if (-not (Test-Path -LiteralPath $clangFormat)) { throw 'Visual Studio bundled clang-format was not found.' }; & $clangFormat -i (Get-ChildItem AppLoader\*.cpp, AppLoader\*.h -File).FullName; exit $LASTEXITCODE"
if errorlevel 1 goto failed

echo Formatting completed successfully.
set "result=0"
goto done

:failed
set "result=%errorlevel%"
echo Formatting failed. See the error above.

:done
pause
popd
endlocal & exit /b %result%
