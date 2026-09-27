# Automatic 30fps renders, not physical player-input recordings.
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
foreach($taskView in @(0,45,90)){
  foreach($taskPrefix in @('','Interrupt_')){
    $taskInput=Join-Path $taskProject ('Logs/SandplaySequence/{0}{1}_%03d.png' -f $taskPrefix,$taskView)
    $taskOutput=Join-Path $taskProject ('Docs/Captures/Sandplay/{0}{1}.mp4' -f $taskPrefix,$taskView)
    & ffmpeg -hide_banner -loglevel error -y -framerate 30 -i $taskInput -an -c:v libx264 -crf 20 -pix_fmt yuv420p $taskOutput
    if($LASTEXITCODE -ne 0){throw "Encoding failed: $taskOutput"}
  }
}
