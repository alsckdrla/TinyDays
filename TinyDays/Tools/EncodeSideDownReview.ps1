$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskClip in @(34,35)){
  foreach($taskView in @(0,45,90)){
    $taskInput=Join-Path $taskProject ('Logs/SideDownSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)
    $taskOutput=Join-Path $taskProject ('Docs/Captures/SideDown/{0}_{1}.mp4' -f $taskClip,$taskView)
    & ffmpeg -y -loglevel error -framerate 30 -i $taskInput -c:v libx264 -crf 20 -pix_fmt yuv420p $taskOutput
    if($LASTEXITCODE -ne 0){throw ('Side entry encoding failed: '+$taskClip+'/'+$taskView)}
  }
}
