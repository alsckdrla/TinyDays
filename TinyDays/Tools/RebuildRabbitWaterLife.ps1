param([switch]$SkipArt)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskUnity='C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$taskBlender='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Close the Unity project before rebuilding.'}
if(!$SkipArt){
  & $taskBlender --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'append_water_pickup.py')
  if($LASTEXITCODE -ne 0){throw 'Water pickup art failed'}
}
$taskLog=Join-Path $taskProject 'Logs/water-life-rebuild.log'
$taskProcess=Start-Process $taskUnity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','RabbitWaterLifeChecks.Execute','-logFile',('"'+$taskLog+'"')) -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $taskLog -Pattern 'WATER_LIFE_COMPLETE_OK' -Quiet)){throw "Water life checks/build failed: $taskLog"}
$taskVideoLog=Join-Path $taskProject 'Logs/water-life-video.log'
$taskVideoError=Join-Path $taskProject 'Logs/water-life-video-error.log'
$taskVideo=Start-Process $taskBlender -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'EncodeWaterLifeReview.py')+'"')) -WindowStyle Hidden -RedirectStandardOutput $taskVideoLog -RedirectStandardError $taskVideoError -PassThru
$taskVideo.WaitForExit()
if($taskVideo.ExitCode -ne 0){throw "Video encoding failed: $taskVideoLog"}
