$ErrorActionPreference = 'SilentlyContinue'

# Kill apphost instances
Get-Process -Name CustomerService | Stop-Process -Force

# Kill dotnet hosts running this specific service
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
  Where-Object { $_.CommandLine -like '*src\CustomerService\CustomerService.csproj*' -or $_.CommandLine -like '*CustomerService.dll*' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

exit 0
