$bad = @()
$files = Get-ChildItem -Path . -Recurse -File -Filter *.gd | Where-Object {
$_.FullName -notmatch '\addons\' -and
$_.FullName -notmatch '\.godot\' -and
$_.FullName -notmatch '\.git\'
}

foreach ($f in $files) {
Select-String -Path $f.FullName -Pattern '^[\t ]* [\t ]*\S' | ForEach-Object {
$bad += "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line
}
}

if ($bad.Count -gt 0) {
"FAIL: Found spaces in leading indentation of .gd files:"
$bad
exit 1
}
"OK: Tabs-only indentation verified."
exit 0