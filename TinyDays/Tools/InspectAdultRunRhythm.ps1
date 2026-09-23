$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
if(Get-Process Unity -ErrorAction SilentlyContinue){throw 'Unity is running; close it before inspection.'}
$taskBlender=Start-Process 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -ArgumentList @('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'animate_adult_rabbit.py')+'"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/run-rhythm-art.log') -RedirectStandardError (Join-Path $taskProject 'Logs/run-rhythm-art-errors.log') -Wait -PassThru
if($taskBlender.ExitCode -ne 0){throw 'Blender generation failed'}
foreach($taskMethod in @('MeasureRhythm','InspectRunCoat')){
  $taskUnity=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',('AdultRabbitRunChecks.'+$taskMethod),'-logFile',('"'+(Join-Path $taskProject ('Logs/run-'+$taskMethod+'.log'))+'"')) -WindowStyle Hidden -Wait -PassThru
  if($taskUnity.ExitCode -ne 0){throw ('Unity inspection failed: '+$taskMethod)}
}
