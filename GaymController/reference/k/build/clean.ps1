$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path "$RepoRoot\.."
Get-ChildItem $RepoRoot -Recurse -Include bin,obj -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Clean complete."
