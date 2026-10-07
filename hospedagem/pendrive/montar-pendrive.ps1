<#
 Prepara um pendrive de instalacao do Windows 10 que instala SOZINHO (sem teclado e sem tela) e liga a hospedagem.

 1) Crie o pendrive com o Rufus a partir de uma ISO do Windows 10 (64 bits, em portugues). Rufus: esquema GPT se a
    placa for UEFI, MBR se for BIOS antiga (na duvida, GPT + "UEFI (nao CSM)" e, se nao iniciar, MBR).
 2) Rode este script (PowerShell) apontando para o pendrive:

    .\montar-pendrive.ps1 -Drive E: -Senha "UmaSenhaForte123" `
        -Arquivos C:\Users\voce\RobloxHost `
        [-TunnelToken TOKEN] [-WifiSsid MinhaRede -WifiSenha SenhaDoWifi] [-Usuario host] [-Computador ROBLOXHOST]

    -Arquivos e uma pasta com o site (RobloxServer.Web), o IniciarServidor.exe e o RobloxPlayerLauncher.exe.
 AVISO: a instalacao APAGA o disco 0 do computador de destino.
#>
param(
    [Parameter(Mandatory = $true)][string]$Drive,
    [Parameter(Mandatory = $true)][string]$Senha,
    [string]$Arquivos = "",
    [string]$Usuario = "host",
    [string]$Computador = "ROBLOXHOST",
    [string]$TunnelToken = "",
    [string]$WifiSsid = "",
    [string]$WifiSenha = ""
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = $Drive.TrimEnd('\') + '\'
if (-not (Test-Path (Join-Path $root 'sources'))) { throw "Nao achei a pasta sources em $root. Crie o pendrive com o Rufus primeiro." }
if ($Senha.Length -lt 8) { throw "Use uma senha de pelo menos 8 caracteres." }

function Esc([string]$s) { [Security.SecurityElement]::Escape($s) }
$xml = Get-Content (Join-Path $here 'autounattend.template.xml') -Raw
$xml = $xml.Replace('__USUARIO__', (Esc $Usuario)).Replace('__SENHA__', (Esc $Senha)).Replace('__COMPUTADOR__', (Esc $Computador))
[IO.File]::WriteAllText((Join-Path $root 'autounattend.xml'), $xml, (New-Object Text.UTF8Encoding($false)))
Copy-Item (Join-Path $here 'disco.cmd') $root -Force

$dest = Join-Path $root 'sources\$OEM$\$1\RobloxHost'
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item (Join-Path $here 'posinstall.ps1') $dest -Force
Copy-Item (Join-Path $here '..\instalar-hospedagem.ps1') $dest -Force
if ($Arquivos) { Copy-Item (Join-Path $Arquivos '*') $dest -Recurse -Force }
if ($TunnelToken) { Set-Content (Join-Path $dest 'tunnel.token') $TunnelToken -NoNewline }
if ($WifiSsid) {
    $ssid = Esc $WifiSsid; $pass = Esc $WifiSenha
    $hex = ([Text.Encoding]::UTF8.GetBytes($WifiSsid) | ForEach-Object { $_.ToString('X2') }) -join ''
    @"
<?xml version="1.0"?>
<WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
  <name>$ssid</name>
  <SSIDConfig><SSID><hex>$hex</hex><name>$ssid</name></SSID></SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM><security>
    <authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption>
    <sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>$pass</keyMaterial></sharedKey>
  </security></MSM>
</WLANProfile>
"@ | Set-Content (Join-Path $dest 'wifi.xml') -Encoding UTF8
}
Write-Host "Pendrive pronto em $root. Usuario: $Usuario  Computador: $Computador"
Write-Host "Ligue a placa com o pendrive, espere uns 30-40 min e conecte com a Area de Trabalho Remota (mstsc) em $Computador."
