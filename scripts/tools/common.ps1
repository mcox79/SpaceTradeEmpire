Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
$root = (& git rev-parse --show-toplevel 2>$null)
if (-not $root) { throw "Not in a git repository." }
return $root.Trim()
}

function Ensure-Scratch {
param(
[Parameter(Mandatory=$true)]
[string]$RepoRoot
)
$scratch = Join-Path $RepoRoot "_scratch"
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
return $scratch
}

function Get-GodotExe {
param(
[Parameter(Mandatory=$true)]
[string]$RepoRoot
)

$config = Join-Path $RepoRoot "godot_path.cfg"
$candidates = @()

# 1) Repo config (preferred)
if (Test-Path -LiteralPath $config) {
$raw = Get-Content -LiteralPath $config -Raw -Encoding UTF8
if ($raw) { $candidates += $raw.Trim() }
}

# 2) Environment override (optional)
if ($env:GODOT_EXE) { $candidates += $env:GODOT_EXE }

# 3) Common locations (best-effort)
if ($env:ProgramFiles) { $candidates += (Join-Path $env:ProgramFiles "Godot\Godot.exe") }

foreach ($c in $candidates) {
if ($c -and (Test-Path -LiteralPath $c)) { return $c }
}

$hint =
"Godot executable not found.`n" +
"Set it by writing the full exe path into:`n  $config`n" +
"Example:`n  Set-Content -LiteralPath `"$config`" -Value `"<FULL PATH TO GODOT EXE>`" -Encoding UTF8"
throw $hint
}

function Write-TextUtf8NoBom {
param(
[Parameter(Mandatory=$true)]
[string]$Path,
[Parameter(Mandatory=$true)]
[string]$Text
)
$enc = New-Object System.Text.UTF8Encoding($false)
$dir = Split-Path -Parent $Path
if ($dir -and (-not (Test-Path -LiteralPath $dir))) {
New-Item -ItemType Directory -Force -Path $dir | Out-Null
}
[System.IO.File]::WriteAllText($Path, $Text, $enc)
}
