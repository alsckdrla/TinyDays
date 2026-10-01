$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskClip in @(32,33)){
  foreach($taskView in @(0,45,90)){
    $taskInput=Join-Path $taskProject ('Logs/SideRiseSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)
    $taskOutput=Join-Path $taskProject ('Docs/Captures/SideRise/{0}_{1}.mp4' -f $taskClip,$taskView)
    & ffmpeg -y -loglevel error -framerate 30 -i $taskInput -c:v libx264 -crf 20 -pix_fmt yuv420p $taskOutput
    if($LASTEXITCODE -ne 0){throw ('Side rise encoding failed: '+$taskClip+'/'+$taskView)}
  }
}
& ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject 'Logs/SideRiseSequence/Cycle_%03d.png') -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject 'Docs/Captures/SideRise/SideSleepToWalk_45.mp4')
if($LASTEXITCODE -ne 0){throw 'Side sleep to walk encoding failed'}
