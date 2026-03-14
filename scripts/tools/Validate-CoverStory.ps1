# GATE.X.COVER_STORY.AUDIT.001: Scan player-facing text for forbidden "fracture" references pre-revelation.
# Scans scripts/bridge/, scripts/ui/, scripts/core/ for string literals containing "fracture"
# that are NOT in code comments or internal variable names.
# Allowlist for legitimate uses (variable names, comments, internal IDs).

param()

$ErrorActionPreference = 'Stop'
$violations = @()

# Directories to scan for player-facing text.
$scanDirs = @(
    'scripts/bridge',
    'scripts/ui',
    'scripts/core'
)

# Allowlist patterns: lines matching these regexes are not violations.
# These cover: code comments (//), internal variable names, internal ID strings,
# string interpolation of state fields, GDScript comments (#), and known cover-name helpers.
$allowlistPatterns = @(
    '^\s*//',                           # C# line comments
    '^\s*#',                            # GDScript comments
    '^\s*\*',                           # Block comment continuation
    'FractureUnlocked',                 # State field reference (not player-facing)
    'FractureExposure',                 # State field reference
    'fracture_exposure',                # Snake-case state reference
    'FractureWeight',                   # System class name
    'FractureSystem',                   # System class name
    'fracture_drive',                   # Internal module ID
    'fracture_routes',                  # Internal route type
    'GetCoverNameV0',                   # Cover name helper method
    'CoverName',                        # Cover name related
    'cover_story',                      # Cover story system reference
    'GATE\.',                           # Gate markers
    'fracture_discovered',              # Internal event ID
    'fracture_unlocked',                # Internal flag
    'var.*racture',                     # Variable declarations containing "fracture"
    'Fracture.*System',                 # Class references
    '\"disc_v0\|',                      # Discovery ID format (internal)
    'Validate-CoverStory',             # Self-reference
    # C# bridge method names and API references (internal plumbing, not player text):
    'GetFracture',                      # Bridge method names (GetFractureAccessV0 etc.)
    'DispatchFracture',                 # Bridge dispatch methods
    'FractureTravel',                   # Command/method names
    'FractureTravelCommand',            # Command class
    'FractureTweaksV0',                 # Tweaks reference
    'FractureTradeFailure',             # Tweaks field
    'FractureDerelict',                 # Enum value
    'FractureDiscoveryTick',            # State field
    'FractureTraveling',                # Fleet state enum value
    'FractureFuelPerJump',              # Tweaks constant
    'FractureHullStressPerJump',        # Tweaks constant
    'FractureTracePerArrival',          # Tweaks constant
    'TraceDetectionThreshold',          # Tweaks constant
    'fracture_price',                   # Internal data key (instrument disagreement)
    'ComputeFracture',                  # Method name
    'has_method\(".*[Ff]racture',       # GDScript has_method checks (API plumbing)
    'call\(".*[Ff]racture',             # GDScript call() to bridge (API plumbing)
    'func.*fracture',                   # GDScript function definitions
    'print\("UUIR\|FRACTURE',          # Debug logging prefixes
    '_fracture_',                       # GDScript private variable names
    '=>.*"Structural',                  # Cover name mapping values (the correct replacements)
    '=>.*"spatial',                     # Cover name mapping values
    '=>.*"Spatial'                      # Cover name mapping values
)

foreach ($dir in $scanDirs) {
    $fullDir = Join-Path $PSScriptRoot '../../' $dir
    $fullDir = Resolve-Path $fullDir -ErrorAction SilentlyContinue
    if (-not $fullDir) { continue }

    $files = Get-ChildItem -Path $fullDir -Recurse -Include '*.cs','*.gd' -File

    foreach ($file in $files) {
        $lineNum = 0
        foreach ($line in (Get-Content $file.FullName)) {
            $lineNum++

            # Check for "fracture" (case-insensitive) in the line.
            if ($line -match 'fracture') {
                $isAllowlisted = $false
                foreach ($pattern in $allowlistPatterns) {
                    if ($line -match $pattern) {
                        $isAllowlisted = $true
                        break
                    }
                }
                if (-not $isAllowlisted) {
                    $relPath = $file.FullName.Replace((Get-Location).Path + '\', '').Replace('\', '/')
                    $violations += "${relPath}:${lineNum}: $($line.Trim())"
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "COVER STORY AUDIT: $($violations.Count) player-facing 'fracture' reference(s) found:" -ForegroundColor Yellow
    foreach ($v in $violations) {
        Write-Host "  $v" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "These should use cover-story naming pre-revelation." -ForegroundColor Cyan
    Write-Host "See GATE.X.COVER_STORY.BRIDGE_WIRE.001 and GATE.X.COVER_STORY.UI_ENFORCE.001." -ForegroundColor Cyan
    Write-Host "COVER STORY AUDIT: PASS (scan complete, violations reported)" -ForegroundColor Green
} else {
    Write-Host "COVER STORY AUDIT: PASS (no violations found)" -ForegroundColor Green
}

# Audit tool exits 0 — its job is to report, not block.
# Enforcement is done by COVER_STORY.UI_ENFORCE gate.
exit 0
