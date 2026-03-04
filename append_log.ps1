$logFile = "C:\Users\marsh\Documents\Space Trade Empire\docs\56_SESSION_LOG.md"
$newEntry = "- 2026-03-04, main, GATE.X.HYGIENE.GEN_REPORT_EXTRACT.001 PASS (Extracted BuildTopologyDump, BuildEconLoopsReport, BuildInvariantsReport, BuildWorldClassReport + helpers to ReportBuilder.cs; 258/258 tests pass). Evidence: SimCore/Gen/ReportBuilder.cs"
Add-Content -Path $logFile -Value $newEntry
