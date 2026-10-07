# Roda no primeiro logon (pelo autounattend.xml). Log em C:\RobloxHost\posinstall.log
$ErrorActionPreference = "Continue"
$root = "C:\RobloxHost"
Start-Transcript -Path "$root\posinstall.log" -Append | Out-Null

try { Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private } catch { Write-Host "perfil de rede: $_" }

if (Test-Path "$root\wifi.xml") {
    netsh wlan add profile filename="$root\wifi.xml" user=all | Out-Null
    Remove-Item "$root\wifi.xml" -Force
}

# Acesso remoto (Area de Trabalho Remota, so no Windows 10 Pro): mstsc -> nome do computador ou IP
Set-ItemProperty -Path 'HKLM:\System\CurrentControlSet\Control\Terminal Server' -Name fDenyTSConnections -Value 0
New-NetFirewallRule -DisplayName "RobloxHost RDP" -Direction Inbound -Protocol TCP -LocalPort 3389 -Action Allow -Profile Any -ErrorAction SilentlyContinue | Out-Null
Set-Service TermService -StartupType Automatic
Start-Service TermService -ErrorAction SilentlyContinue

# Hospedagem (procura os arquivos copiados para C:\RobloxHost)
$starter = Get-ChildItem $root -Recurse -Filter IniciarServidor.exe -ErrorAction SilentlyContinue | Select-Object -First 1
$launcher = Get-ChildItem $root -Recurse -Filter RobloxPlayerLauncher.exe -ErrorAction SilentlyContinue | Select-Object -First 1
$web = Get-ChildItem $root -Recurse -Filter Web.config -ErrorAction SilentlyContinue | Select-Object -First 1
$extra = @()
if (Test-Path "$root\tunnel.token") {
    $extra = @("-TunnelToken", (Get-Content "$root\tunnel.token" -Raw).Trim())
    Remove-Item "$root\tunnel.token" -Force
}
if ($starter -and $launcher -and $web) {
    & "$root\instalar-hospedagem.ps1" -SiteDir $web.DirectoryName -StarterExe $starter.FullName -LauncherExe $launcher.FullName @extra
    Start-ScheduledTask -TaskName "RobloxServer-Site" -ErrorAction SilentlyContinue
    Start-ScheduledTask -TaskName "RobloxServer-Bots" -ErrorAction SilentlyContinue
} else {
    Write-Host "Arquivos da hospedagem nao encontrados em $root (IniciarServidor.exe, RobloxPlayerLauncher.exe, Web.config). Rode instalar-hospedagem.ps1 depois."
}
Stop-Transcript | Out-Null
