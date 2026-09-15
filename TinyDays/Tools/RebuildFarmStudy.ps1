$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskRunning=Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/', '\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch regeneration. Existing editor is not closed automatically.'}
New-Item -ItemType Directory -Force -Path (Join-Path $taskProject 'Logs') | Out-Null
$taskArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','FarmStudyBuilder.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/farm-study-unity.log')+'"'))
$taskProcess=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw 'Farm generation/check failed; see Logs/farm-study-unity.log'}
if(!(Select-String -LiteralPath (Join-Path $taskProject 'Logs/farm-study-unity.log') -SimpleMatch 'TINYDAYS_STAGE26_OK' -Quiet)){throw 'Verification marker missing'}
Write-Output 'Farm scene generated and automatically checked. Actual Game-window observation and user approval are separate.'
