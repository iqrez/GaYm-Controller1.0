Param(
  [switch]$UseDevcon
)

$busInf  = Resolve-Path "..\src\drivers\bus\VPadBus.inf"
$funcInf = Resolve-Path "..\src\drivers\func\VPadFunc.inf"

if ($UseDevcon) {
  if (-not (Get-Command devcon.exe -ErrorAction SilentlyContinue)) {
    Write-Error "devcon.exe not found. Ensure WDK tools are in PATH or omit -UseDevcon."; exit 1
  }
  # Install via devcon (class install from INF). Adjust hardware IDs if needed.
  devcon.exe install $busInf *VPadBus || exit $LASTEXITCODE
  devcon.exe install $funcInf *VPadFunc || exit $LASTEXITCODE
}
else {
  pnputil /add-driver $busInf /install || exit $LASTEXITCODE
  pnputil /add-driver $funcInf /install || exit $LASTEXITCODE
}

Write-Host "Drivers installed."
