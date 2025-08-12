# Template for recording golden mouse traces (legacy app or new app):
# Writes CSV with timestamp(ms), dx, dy, dt_ms, rx, ry (if available)
param([string]$OutPath = "reference/traces/capture.csv")
"timestamp_ms,dx,dy,dt_ms,rx,ry" | Out-File -FilePath $OutPath -Encoding ascii
Write-Host "Capture template created at $OutPath"
