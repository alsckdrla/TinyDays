param([switch]$SkipArt)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskUnity='C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$taskRunning=Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/', '\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch regeneration. No editor was closed automatically.'}
New-Item -ItemType Directory -Force -Path (Join-Path $taskProject 'Logs') | Out-Null
if(!$SkipArt){
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'animate_rabbit.py') *> (Join-Path $taskProject 'Logs/rabbit-motion-blender.log')
    if($LASTEXITCODE -ne 0){throw 'Blender motion generation failed; see Logs/rabbit-motion-blender.log'}
}
$taskArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','RabbitMotionBuilder.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/rabbit-motion-unity.log')+'"'))
$taskProcess=Start-Process -FilePath $taskUnity -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0){throw 'Unity motion verification failed; see Logs/rabbit-motion-unity.log'}
if(!(Select-String -LiteralPath (Join-Path $taskProject 'Logs/rabbit-motion-unity.log') -SimpleMatch 'TINYDAYS_STAGE23_OK' -Quiet)){throw 'Verification marker missing'}
& python (Join-Path $PSScriptRoot 'verify_rabbit_motion.py')
if($LASTEXITCODE -ne 0){throw 'Source contact verification failed'}
Write-Output 'Elastic motion generated and automatically verified. Actual Game-window checks and user approval are separate.'
