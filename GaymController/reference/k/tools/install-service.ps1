Param([string]$Name = "VPadBroker")

$exe = Resolve-Path "..\src\service\VPadBroker\bin\Release\net8.0-windows\VPadBroker.exe" -ErrorAction SilentlyContinue
if (-not $exe) { Write-Error "Build VPadBroker Release first."; exit 1 }

# If service exists, stop and delete to allow upgrade
$svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
if ($svc) {
  if ($svc.Status -ne 'Stopped') { sc.exe stop $Name | Out-Null; Start-Sleep -Seconds 2 }
  sc.exe delete $Name | Out-Null
  Start-Sleep -Seconds 1
}

sc.exe create $Name binPath= "`"$exe`"" start= auto DisplayName= "InHouse VPad Broker"
sc.exe start $Name
