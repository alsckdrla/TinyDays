$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskRunning=Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/', '\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch generation.'}
$taskLog=Join-Path $taskProject 'Logs/house-village.log'
$taskArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','HouseVillageBuilder.Execute','-logFile',('"'+$taskLog+'"'))
$taskProcess=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $taskLog -SimpleMatch 'HOUSE_VILLAGE_OK' -Quiet)){throw 'House village verification failed; see Logs/house-village.log'}
Write-Output 'House comparison generated and automatically checked. Live Game input and user approval remain separate.'
