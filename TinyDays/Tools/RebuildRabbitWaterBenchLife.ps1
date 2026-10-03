$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskUnity='C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$taskBlender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close the Unity project before rebuilding.'}
# Existing clips and canonical Blender are read-only in this connection task.
$taskLog=Join-Path $taskProject 'Logs/water-bench-life-rebuild.log'
$taskProcess=Start-Process $taskUnity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','RabbitWaterBenchLifeChecks.Execute','-logFile',('"'+$taskLog+'"')) -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $taskLog -Pattern 'WATER_BENCH_LIFE_COMPLETE_OK' -Quiet)){throw "Water/bench checks/build failed: $taskLog"}
$taskVideo=Start-Process $taskBlender -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'EncodeWaterBenchLifeReview.py')+'"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/water-bench-life-video.log') -RedirectStandardError (Join-Path $taskProject 'Logs/water-bench-life-video-error.log') -PassThru
$taskVideo.WaitForExit()
if($taskVideo.ExitCode -ne 0){throw 'Water/bench video encoding failed.'}
