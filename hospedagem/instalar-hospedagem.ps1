<#
 Instala a hospedagem 24/7 do RobloxServer neste Windows (rode como Administrador):

   .\instalar-hospedagem.ps1 -SiteDir C:\RobloxServer\RobloxServer.Web `
       -StarterExe C:\RobloxServer\IniciarServidor.exe `
       -LauncherExe C:\RobloxLauncher\RobloxPlayerLauncher.exe `
       [-TunnelToken TOKEN_DO_CLOUDFLARE] [-Port 8080]

 O que faz:
   - Tarefa "RobloxServer-Site": IniciarServidor.exe ao fazer logon (site na porta escolhida)
   - Tarefa "RobloxServer-Bots": RobloxPlayerLauncher.exe --bot-host 1 min depois do logon
   - Firewall: libera as portas UDP dos bots
   - Energia: sem suspensao/hibernacao e sem acao ao fechar a tampa (notebook/Chromebook)
   - Opcional: Cloudflare Tunnel como servico do Windows (site na Internet com HTTPS, sem abrir porta)

 Remover tudo:  .\instalar-hospedagem.ps1 -Remove
#>
param(
    [string]$SiteDir,
    [string]$StarterExe,
    [string]$LauncherExe,
    [int]$Port = 8080,
    [string]$TunnelToken = "",
    [int]$FirstBotPort = 53641,
    [int]$BotPorts = 60,
    [switch]$Remove
)

$ErrorActionPreference = "Stop"
$SiteTask = "RobloxServer-Site"
$BotTask = "RobloxServer-Bots"
$FirewallRule = "RobloxServer Bots UDP"
$CloudflaredDir = Join-Path $env:ProgramFiles "cloudflared"
$CloudflaredExe = Join-Path $CloudflaredDir "cloudflared.exe"

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw "Rode este script como Administrador." }

if ($Remove) {
    foreach ($name in @($SiteTask, $BotTask)) {
        if (Get-ScheduledTask -TaskName $name -ErrorAction SilentlyContinue) {
            Unregister-ScheduledTask -TaskName $name -Confirm:$false
            Write-Host "Tarefa removida: $name"
        }
    }
    Remove-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue
    if (Test-Path $CloudflaredExe) {
        & $CloudflaredExe service uninstall 2>$null
        Write-Host "Servico do Cloudflare Tunnel removido."
    }
    Write-Host "Pronto."
    return
}

foreach ($item in @(@("SiteDir", $SiteDir), @("StarterExe", $StarterExe), @("LauncherExe", $LauncherExe))) {
    if (-not $item[1]) { throw "Faltou o parametro -$($item[0])." }
}
if (-not (Test-Path (Join-Path $SiteDir "Web.config"))) { throw "SiteDir invalido (sem Web.config): $SiteDir" }
if (-not (Test-Path $StarterExe)) { throw "StarterExe nao encontrado: $StarterExe" }
if (-not (Test-Path $LauncherExe)) { throw "LauncherExe nao encontrado: $LauncherExe" }

$user = "$env:USERDOMAIN\$env:USERNAME"
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -StartWhenAvailable -MultipleInstances IgnoreNew -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 5 -RestartInterval (New-TimeSpan -Minutes 1)

# 1) Site
$siteAction = New-ScheduledTaskAction -Execute $StarterExe -Argument "--minimized --port $Port" -WorkingDirectory (Split-Path $StarterExe)
$siteTrigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$sitePrincipal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Highest
Register-ScheduledTask -TaskName $SiteTask -Action $siteAction -Trigger $siteTrigger -Principal $sitePrincipal -Settings $settings -Force | Out-Null
Write-Host "Tarefa criada: $SiteTask"

# 2) Bots (1 minuto depois, para o site ja estar de pe)
$botArgs = "--bot-host --site-dir `"$SiteDir`" --site http://localhost:$Port/"
$botAction = New-ScheduledTaskAction -Execute $LauncherExe -Argument $botArgs -WorkingDirectory (Split-Path $LauncherExe)
$botTrigger = New-ScheduledTaskTrigger -AtLogOn -User $user
$botTrigger.Delay = "PT1M"
$botPrincipal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $BotTask -Action $botAction -Trigger $botTrigger -Principal $botPrincipal -Settings $settings -Force | Out-Null
Write-Host "Tarefa criada: $BotTask"

# 3) Firewall (UDP dos bots)
Remove-NetFirewallRule -DisplayName $FirewallRule -ErrorAction SilentlyContinue
$last = $FirstBotPort + $BotPorts - 1
New-NetFirewallRule -DisplayName $FirewallRule -Direction Inbound -Protocol UDP -LocalPort "$FirstBotPort-$last" -Action Allow | Out-Null
Write-Host "Firewall: UDP $FirstBotPort-$last liberado."

# 4) Energia: nunca dormir, nem ao fechar a tampa
powercfg /change standby-timeout-ac 0 | Out-Null
powercfg /change standby-timeout-dc 0 | Out-Null
powercfg /change hibernate-timeout-ac 0 | Out-Null
powercfg /hibernate off | Out-Null
powercfg /setacvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0 | Out-Null
powercfg /setdcvalueindex SCHEME_CURRENT SUB_BUTTONS LIDACTION 0 | Out-Null
powercfg /setactive SCHEME_CURRENT | Out-Null
Write-Host "Energia: sem suspensao e sem acao ao fechar a tampa."

# 5) Cloudflare Tunnel (opcional)
if ($TunnelToken) {
    if (-not (Test-Path $CloudflaredExe)) {
        New-Item -ItemType Directory -Force -Path $CloudflaredDir | Out-Null
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -UseBasicParsing -Uri "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe" -OutFile $CloudflaredExe
    }
    & $CloudflaredExe service install $TunnelToken
    Write-Host "Cloudflare Tunnel instalado como servico. No painel do Cloudflare aponte o dominio para http://localhost:$Port"
}

Write-Host ""
Write-Host "Pronto. Para subir tudo sozinho depois de reiniciar o PC, configure o login automatico do Windows"
Write-Host "(Sysinternals Autologon ou 'netplwiz'). Para testar agora: Start-ScheduledTask $SiteTask; Start-ScheduledTask $BotTask"
Write-Host "Lembrete: o jogo usa UDP; o Cloudflare Tunnel leva so o site. Defina PublicGameAddress no Web.config e libere as portas UDP no roteador."
