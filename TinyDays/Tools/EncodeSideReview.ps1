param([switch]$HistoricalV0110)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskClip in 26..28){foreach($taskView in @(0,45,90)){
  & ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject ('Logs/SideSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)) -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject ('Docs/Captures/SideRest/{0}_{1}.mp4' -f $taskClip,$taskView))
  if($LASTEXITCODE -ne 0){throw 'Side encoding failed'}
}}
& ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject 'Logs/SideSequence/Cycle_%03d.png') -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject 'Docs/Captures/SideRest/SideReturnWalk_45.mp4')
if($LASTEXITCODE -ne 0){throw 'Side cycle encoding failed'}
$taskBefore=Join-Path $taskProject 'Docs/Captures/SideBeforeV0110'
if($HistoricalV0110 -and (Test-Path -LiteralPath (Join-Path $taskBefore '26_0.mp4'))){
  foreach($taskClip in 26..28){
    & ffmpeg -y -loglevel error -i (Join-Path $taskBefore ($taskClip.ToString()+'_0.mp4')) -i (Join-Path $taskProject ('Docs/Captures/SideRest/{0}_0.mp4' -f $taskClip)) -filter_complex hstack=inputs=2 -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject ('Docs/Captures/SideRest/{0}_BeforeLeftAfterRightV0110.mp4' -f $taskClip))
    if($LASTEXITCODE -ne 0){throw 'Side same-camera comparison encoding failed'}
  }
}
