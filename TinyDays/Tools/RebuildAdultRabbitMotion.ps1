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
$bargs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AdultRabbitMotionBuilder.BuildPlayer','-logFile',('"'+(Join-Path $taskProject 'Logs/adult-rabbit-motion-player-build.log')+'"'))
$b=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $bargs -WindowStyle Hidden -Wait -PassThru
if($b.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/adult-rabbit-motion-player-build.log') -SimpleMatch 'ADULT_RABBIT_MOTION_PLAYER_OK' -Quiet)){throw 'Player build failed; see adult-rabbit-motion-player-build.log'}
Write-Output 'Adult rabbit motion created and checked; user motion approval remains separate.'
