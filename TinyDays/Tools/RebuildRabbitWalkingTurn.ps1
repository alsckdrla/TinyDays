$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskUnity='C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$taskBlender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close Unity before rebuilding.'}
$taskLog=Join-Path $taskProject 'Logs/walking-turn-rebuild.log'
$taskProcess=Start-Process $taskUnity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','RabbitWalkingTurnChecks.Execute','-logFile',('"'+$taskLog+'"')) -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $taskLog -Pattern 'MOVING_TURN_COMPLETE_OK' -Quiet)){throw "Moving turn verification/build failed: $taskLog"}
# Existing art is read-only; only already-rendered frames are encoded.
$taskVideo=Start-Process $taskBlender -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'EncodeWaterBenchLifeReview.py')+'"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/walking-turn-video.log') -RedirectStandardError (Join-Path $taskProject 'Logs/walking-turn-video-error.log') -PassThru
$taskVideo.WaitForExit()
if($taskVideo.ExitCode -ne 0){throw 'Moving turn video encoding failed.'}
