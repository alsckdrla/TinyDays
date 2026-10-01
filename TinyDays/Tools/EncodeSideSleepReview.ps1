$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskClip in 29..31){foreach($taskView in @(0,45,90)){
  & ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject ('Logs/SideSleepSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)) -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject ('Docs/Captures/SideSleep/{0}_{1}.mp4' -f $taskClip,$taskView))
  if($LASTEXITCODE -ne 0){throw 'Side sleep encoding failed'}
}}
& ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject 'Logs/SideSleepSequence/Cycle_%03d.png') -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject 'Docs/Captures/SideSleep/SleepWakeWalk_90.mp4')
if($LASTEXITCODE -ne 0){throw 'Side sleep cycle encoding failed'}
foreach($taskClip in 26..28){
  if(!(Test-Path -LiteralPath (Join-Path $taskProject ('Docs/Captures/SideBeforeV0111/{0}_90.mp4' -f $taskClip)))){continue}
  & ffmpeg -y -loglevel error -i (Join-Path $taskProject ('Docs/Captures/SideBeforeV0111/{0}_90.mp4' -f $taskClip)) -i (Join-Path $taskProject ('Docs/Captures/SideRest/{0}_90.mp4' -f $taskClip)) -filter_complex hstack=inputs=2 -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject ('Docs/Captures/SideSleep/{0}_BeforeLeftAfterRightV0111.mp4' -f $taskClip))
  if($LASTEXITCODE -ne 0){throw 'Pillow comparison encoding failed'}
}
