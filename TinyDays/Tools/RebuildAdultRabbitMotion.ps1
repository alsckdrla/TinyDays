param([switch]$SkipArt)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskRunning=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch building; no editor was closed.'}
if(!$SkipArt){
  $args=@('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'animate_adult_rabbit.py')+'"'))
  $p=Start-Process 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -ArgumentList $args -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/adult-rabbit-motion-art.log') -RedirectStandardError (Join-Path $taskProject 'Logs/adult-rabbit-motion-art-errors.log') -Wait -PassThru
  if($p.ExitCode -ne 0){throw 'Blender failed; see adult-rabbit-motion-art-errors.log'}
}
$uargs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitMotionBuilder.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/adult-rabbit-motion-unity.log')+'"'))
$u=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $uargs -WindowStyle Hidden -Wait -PassThru
if($u.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/adult-rabbit-motion-unity.log') -SimpleMatch 'ADULT_RABBIT_MOTION_OK' -Quiet)){throw 'Unity failed; see adult-rabbit-motion-unity.log'}
$rargs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitRunChecks.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/adult-rabbit-run-checks.log')+'"'))
$r=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $rargs -WindowStyle Hidden -Wait -PassThru
if($r.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/adult-rabbit-run-checks.log') -SimpleMatch 'ADULT_RABBIT_RUN_OK' -Quiet)){throw 'Run validation failed; see adult-rabbit-run-checks.log'}
$rhythmArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitRunChecks.MeasureRhythm','-logFile',('"'+(Join-Path $taskProject 'Logs/run-MeasureRhythm.log')+'"'))
$rhythm=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $rhythmArgs -WindowStyle Hidden -Wait -PassThru
if($rhythm.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/run-MeasureRhythm.log') -SimpleMatch 'RUN_RHYTHM_OK' -Quiet)){throw 'Run rhythm measurement failed'}
$smoothArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitRunSmoothnessChecks.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/run-smoothness.log')+'"'))
$smooth=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $smoothArgs -WindowStyle Hidden -Wait -PassThru
if($smooth.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/run-smoothness.log') -SimpleMatch 'RUN_SMOOTHNESS_OK' -Quiet)){throw 'Run smoothness validation failed'}
foreach($taskSitMethod in @('Execute','Sequence')){
  $sitArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',('AdultRabbitSitChecks.'+$taskSitMethod),'-logFile',('"'+(Join-Path $taskProject ('Logs/sit-'+$taskSitMethod+'.log'))+'"'))
  $sit=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $sitArgs -WindowStyle Hidden -Wait -PassThru
  if($sit.ExitCode -ne 0){throw ('Sitting validation failed: '+$taskSitMethod)}
}
$taskMeasureArgs=@('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'measure_sit_smoothness.py')+'"'))
$taskMeasure=Start-Process 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -ArgumentList $taskMeasureArgs -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/sit-smoothness.log') -RedirectStandardError (Join-Path $taskProject 'Logs/sit-smoothness-errors.log') -Wait -PassThru
if($taskMeasure.ExitCode -ne 0){throw 'Sitting smoothness measurement failed'}
$taskIdleCurveArgs=@('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'measure_common_idle.py')+'"'))
$taskIdleCurves=Start-Process 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -ArgumentList $taskIdleCurveArgs -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/idle-curves.log') -RedirectStandardError (Join-Path $taskProject 'Logs/idle-curves-errors.log') -Wait -PassThru
if($taskIdleCurves.ExitCode -ne 0){throw 'Common idle source curves failed'}
foreach($taskIdleMethod in @('Execute','Sequence')){
  $taskIdleArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',('AdultCommonIdleChecks.'+$taskIdleMethod),'-logFile',('"'+(Join-Path $taskProject ('Logs/idle-'+$taskIdleMethod+'.log'))+'"'))
  $taskIdle=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $taskIdleArgs -WindowStyle Hidden -Wait -PassThru
  if($taskIdle.ExitCode -ne 0){throw ('Common idle validation failed: '+$taskIdleMethod)}
}
$bargs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitMotionBuilder.BuildPlayer','-logFile',('"'+(Join-Path $taskProject 'Logs/adult-rabbit-motion-player-build.log')+'"'))
$b=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $bargs -WindowStyle Hidden -Wait -PassThru
if($b.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/adult-rabbit-motion-player-build.log') -SimpleMatch 'ADULT_RABBIT_MOTION_PLAYER_OK' -Quiet)){throw 'Player build failed; see adult-rabbit-motion-player-build.log'}
Write-Output 'Adult rabbit motion created and checked; user motion approval remains separate.'
