[CmdletBinding(DefaultParameterSetName = 'Check')]
param(
  [Parameter(ParameterSetName = 'Check')]
  [string]$Base = 'HEAD^1',
  [Parameter(Mandatory, ParameterSetName = 'Fix')]
  [switch]$FixStaged
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Invoke-Git {
  $output = & git --no-pager --literal-pathspecs -c core.safecrlf=false @args
  if ($LASTEXITCODE -ne 0) {
    throw "git $($args -join ' ') failed with exit code $LASTEXITCODE."
  }
  $output
}

# Support the root config's filename globs and brace lists, in section order.
$rules = @()
$config = if ($FixStaged) { Invoke-Git show ':.editorconfig' } else { Get-Content -LiteralPath .editorconfig }
foreach ($line in $config) {
  if ($line -match '^\s*\[([^\]]+)\]\s*$') {
    $glob = $Matches[1]
    if ($glob -eq '*') {
      $patterns = @('*')
    } elseif ($glob -match '^\*\.\{(\w+(?:,\w+)*)\}$') {
      $patterns = $Matches[1].Split(',') | ForEach-Object { "*.$_" }
    } elseif ($glob -match '^\*\.\w+$') {
      $patterns = @($glob)
    } else {
      throw "Unsupported indentation glob [$glob] in .editorconfig."
    }
    $rule = @{ Patterns = $patterns }
    $rules += $rule
  } elseif ($line -match '^\s*(indent_style|indent_size)\s*=\s*(\S+)\s*$') {
    $rule[$Matches[1]] = $Matches[2]
  }
}

function Get-IndentationRule([string]$Path) {
  $name = [IO.Path]::GetFileName($Path)
  $settings = @{}
  foreach ($rule in $rules) {
    foreach ($pattern in $rule.Patterns) {
      if ($name -clike $pattern) {
        foreach ($key in 'indent_style', 'indent_size') {
          if ($rule.ContainsKey($key)) {
            $settings[$key] = $rule[$key]
          }
        }
        break
      }
    }
  }
  if ($settings.Count -and ($settings.indent_style -ne 'space' -or $settings.indent_size -notmatch '^[1-9][0-9]*$')) {
    throw "Expected indent_style = space and a positive indent_size in .editorconfig for $Path."
  }
  $settings
}

function Get-FixedLine([string]$Line, [int]$Width) {
  if ($Line -cnotmatch '^([ \t]+)[^ \t]') {
    return $Line
  }
  $indent = $Matches[1]
  if (!$indent.Contains("`t") -and $indent.Length % $Width -eq 0) {
    return $Line
  }
  $column = 0
  foreach ($character in $indent.ToCharArray()) {
    if ($character -eq "`t") {
      $column += $Width - ($column % $Width)
    } else {
      $column++
    }
  }
  $levels = [Math]::Round($column / $Width, [MidpointRounding]::AwayFromZero)
  (' ' * [int]($levels * $Width)) + $Line.Substring($indent.Length)
}

$indexPatch = [Collections.Generic.List[string]]::new()
$worktreePatch = [Collections.Generic.List[string]]::new()
$ok = $true
$diff = @('diff', '--no-ext-diff', '--no-textconv', '--no-color', '--no-renames', '--diff-filter=AM')
$range = @('--cached')
if (!$FixStaged) {
  $range = @((Invoke-Git merge-base $Base HEAD), 'HEAD')
}

$paths = (Invoke-Git @diff --name-only -z @range --) -join "`n"
foreach ($path in $paths.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)) {
  $settings = Get-IndentationRule $path
  if (!$settings.Count) {
    continue
  }
  $number = 0
  $header = $null
  $fixes = [Collections.Generic.List[string]]::new()
  $lines = @(Invoke-Git @diff --src-prefix=a/ --dst-prefix=b/ --unified=0 --inter-hunk-context=0 @range -- $path)
  for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@') {
      $number = [int]$Matches[1]
    } elseif ($number -eq 0 -and $line.StartsWith('+++ ')) {
      $header = $line.Substring(4)
    } elseif ($number -gt 0 -and $line.StartsWith('+')) {
      $original = $line.Substring(1)
      $fixed = Get-FixedLine $original $settings.indent_size
      if ($fixed -cne $original) {
        if (!$FixStaged) {
          Write-Host "${path}(${number}): error INDENT001: Use multiples of $($settings.indent_size) spaces for indentation, not tabs."
          $ok = $false
        } else {
          $noNewline = ($i + 1 -lt $lines.Count) -and $lines[$i + 1].StartsWith('\ ')
          $fixes.Add("@@ -$number +$number @@")
          $fixes.Add("-$original")
          if ($noNewline) { $fixes.Add('\ No newline at end of file') }
          $fixes.Add("+$fixed")
          if ($noNewline) { $fixes.Add('\ No newline at end of file') }
        }
      }
      $number++
    }
  }
  if ($fixes.Count) {
    # Never update the working copy of a partially staged file.
    $patch = $worktreePatch
    if (Invoke-Git diff --name-only -- $path) {
      $patch = $indexPatch
      Write-Host "Fixing staged indentation in $path (unstaged edits left alone)."
    } else {
      Write-Host "Fixing indentation in $path."
    }
    $patch.Add("--- $header")
    $patch.Add("+++ $header")
    $patch.AddRange($fixes)
  }
}

function Apply-IndentationPatch($Patch, [string]$Mode) {
  if (!$Patch.Count) {
    return
  }
  $temp = [IO.Path]::GetTempFileName()
  try {
    [IO.File]::WriteAllText($temp, ($Patch -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    Invoke-Git apply $Mode --unidiff-zero --whitespace=nowarn -- $temp
  } finally {
    Remove-Item -LiteralPath $temp
  }
}

Apply-IndentationPatch $worktreePatch --index
Apply-IndentationPatch $indexPatch --cached

if (!$ok) {
  $guide = 'https://github.com/tsunamods-codes/7th-Heaven/blob/master/CONTRIBUTING.md#fixing-indentation'
  Write-Host "How to fix indentation: $guide"
  if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value "Indentation check failed. [How to fix indentation]($guide)."
  }
  exit 1
}
