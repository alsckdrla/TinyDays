$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskOutput=Join-Path $taskProject 'Docs/Captures/Lying'
foreach($taskClip in 17..20){
  foreach($taskView in @(0,45,90)){
    $taskInput=Join-Path $taskProject ('Logs/LyingSequence/'+$taskClip+'_'+$taskView+'_%03d.png')
    $taskVideo=Join-Path $taskOutput ('{0}_{1}.mp4' -f $taskClip,$taskView)
    & ffmpeg -y -loglevel error -framerate 30 -i $taskInput -c:v libx264 -crf 20 -pix_fmt yuv420p $taskVideo
    if($LASTEXITCODE -ne 0){throw ('Encoding failed '+$taskVideo)}
  }
}
