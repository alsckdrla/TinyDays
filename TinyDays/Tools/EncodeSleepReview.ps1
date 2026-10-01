$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskClip in 22..25){
  foreach($taskView in @(0,45,90)){
    $taskInput=Join-Path $taskProject ('Logs/SleepSequence/'+$taskClip+'_'+$taskView+'_%03d.png')
    $taskVideo=Join-Path $taskProject ('Docs/Captures/Sleep/{0}_{1}.mp4' -f $taskClip,$taskView)
    & ffmpeg -y -loglevel error -framerate 30 -i $taskInput -c:v libx264 -crf 20 -pix_fmt yuv420p $taskVideo
    if($LASTEXITCODE -ne 0){throw ('Encoding failed '+$taskVideo)}
  }
}
if(Test-Path -LiteralPath (Join-Path $taskProject 'Logs/SleepCycle/000.png')){
  & ffmpeg -y -loglevel error -framerate 30 -i (Join-Path $taskProject 'Logs/SleepCycle/%03d.png') -c:v libx264 -crf 20 -pix_fmt yuv420p (Join-Path $taskProject 'Docs/Captures/Sleep/SleepWakeStand_45.mp4')
  if($LASTEXITCODE -ne 0){throw 'Sleep cycle encoding failed'}
}
