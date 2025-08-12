param(
  [string]$File,
  [string]$CertPfx = "$env:USERPROFILE\certs\testdriver.pfx",
  [Security.SecureString]$CertPassword,
  [string]$TimestampUrl = "http://timestamp.digicert.com"
)
if (-not (Test-Path $File)) { Write-Error "File not found: $File"; exit 1 }
if (-not (Get-Command signtool.exe -ErrorAction SilentlyContinue)) { Write-Error "signtool.exe not found. Install Windows SDK."; exit 1 }

$pwArg = @()
if ($CertPassword) {
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($CertPassword)
  try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $pwArg = @('/p', $plain)
  }
  finally {
    if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
  }
}

& signtool.exe sign /fd SHA256 /tr $TimestampUrl /td SHA256 /f $CertPfx @pwArg $File
if ($LASTEXITCODE -ne 0) { Write-Error "Signing failed"; exit 1 }
Write-Host "Signed: $File"
