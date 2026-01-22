$bad = @()
Get-ChildItem .\scripts -Recurse -File -Filter *.gd | ForEach-Object {
Select-String -Path $_.FullName -Pattern '^( +)\S' | ForEach-Object {
$bad += "{0}:{1}: {2}" -f $_.Path, $_.LineNumber, $_.Line
}
}
if ($bad.Count -gt 0) {
"FAIL: Found leading-space indentation in .gd files:"
$bad
exit 1
}
"OK: No leading-space indentation found."
exit 0
