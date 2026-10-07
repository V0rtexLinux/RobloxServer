@echo off
rem Roda no WinPE (chamado pelo autounattend.xml). Particiona o disco 0 de um jeito que a particao 3 sempre e o Windows.
wpeutil UpdateBootInfo >nul 2>&1
set FW=
for /f "tokens=3" %%A in ('reg query HKLM\System\CurrentControlSet\Control /v PEFirmwareType 2^>nul') do set FW=%%A
set DP=X:\disco-dp.txt
echo select disk 0> %DP%
echo clean>> %DP%
if "%FW%"=="0x2" (
  echo convert gpt>> %DP%
  echo create partition efi size=260>> %DP%
  echo format quick fs=fat32 label="Sistema">> %DP%
  echo create partition msr size=16>> %DP%
) else (
  echo create partition primary size=500>> %DP%
  echo format quick fs=ntfs label="Sistema">> %DP%
  echo active>> %DP%
  echo create partition primary size=16>> %DP%
)
echo create partition primary>> %DP%
echo format quick fs=ntfs label="Windows">> %DP%
diskpart /s %DP%
