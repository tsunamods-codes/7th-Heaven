$ErrorActionPreference = 'Stop'

if (!(Get-Command git -ErrorAction SilentlyContinue)) {
  $bundledGit = if ($env:IndentationHookDevEnvDir) {
    Join-Path $env:IndentationHookDevEnvDir 'CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd'
  }
  if (!$bundledGit -or !(Test-Path -LiteralPath (Join-Path $bundledGit 'git.exe'))) {
    Write-Warning 'Git is not on PATH. The indentation hook was not installed.'
    return
  }
  $env:PATH = "$bundledGit;$env:PATH"
}

$hook = @'
#!/bin/sh
if [ -f .scripts/Check-Indentation.ps1 ]; then
  exec powershell.exe -NoProfile -ExecutionPolicy Bypass -File .scripts/Check-Indentation.ps1 -FixStaged
fi
'@
$hook = ($hook -replace "`r`n", "`n") + "`n"

$hooksPath = git --no-pager config --get core.hooksPath
if ($LASTEXITCODE -eq 0) {
  Write-Warning "core.hooksPath is already set to '$hooksPath'. Add the indentation checker to your existing pre-commit hook to enable autofix."
  return
}
if ($LASTEXITCODE -ne 1) {
  throw 'Could not read Git hook configuration.'
}

$path = git --no-pager rev-parse --path-format=absolute --git-path hooks/pre-commit
if ($LASTEXITCODE -ne 0) {
  throw 'Could not locate Git hooks directory.'
}
if ([IO.File]::Exists($path)) {
  if ([IO.File]::ReadAllText($path) -cne $hook) {
    Write-Warning 'An existing pre-commit hook was left unchanged. Add the indentation checker to it to enable autofix.'
  }
  return
}

[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
$stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
try {
  $bytes = [Text.Encoding]::UTF8.GetBytes($hook)
  $stream.Write($bytes, 0, $bytes.Length)
} finally {
  $stream.Dispose()
}
