<#
  CapsuleOS - remove the Windows repair toolkit.
  The revert mirror of Install-CapsuleRepair.ps1: removes the
  copied scripts, the logs folder, and both shortcuts.
#>
$ErrorActionPreference = 'Continue'
$dst = Join-Path $env:ProgramData 'CapsuleOS\Repair'
if (Test-Path $dst) { Remove-Item -Path $dst -Recurse -Force }
$links = @(
    (Join-Path ([Environment]::GetFolderPath('CommonDesktopDirectory')) 'Repair Windows.lnk'),
    (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\CapsuleOS\Repair Windows.lnk')
)
foreach ($lnk in $links) { if (Test-Path $lnk) { Remove-Item -Path $lnk -Force } }
Write-Host 'Capsule repair toolkit removed.'
