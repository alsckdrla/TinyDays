# Encode the automatic 30fps review renders; these are not player input recordings.
param([string]$Version='0104',[string]$BeforeVersion='0104',[double]$BeforeHoldSeconds=0)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskFfmpeg=(Get-Command ffmpeg -ErrorAction Stop).Source
$taskOutput=Join-Path $taskProject 'Docs/Captures/Fidgets'
foreach($taskClip in @(14,15)){
  foreach($taskView in @(0,45,90)){
    $taskInput=Join-Path $taskProject ('Logs/FidgetSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)
    $taskMovie=Join-Path $taskOutput ('{0}_{1}.mp4' -f $taskClip,$taskView)
    & $taskFfmpeg -hide_banner -loglevel error -y -framerate 30 -i $taskInput -an -c:v libx264 -crf 20 -pix_fmt yuv420p $taskMovie
    if($LASTEXITCODE -ne 0){throw "Encoding failed: $taskMovie"}
    $taskInterrupt=Join-Path $taskProject ('Logs/FidgetInterruptSequence/{0}_{1}_%03d.png' -f $taskClip,$taskView)
    if(Test-Path -LiteralPath ($taskInterrupt.Replace('%03d','000'))){
      $taskInterruptMovie=Join-Path $taskOutput ('{0}_{1}_InterruptV{2}.mp4' -f $taskClip,$taskView,$Version)
      & $taskFfmpeg -hide_banner -loglevel error -y -framerate 30 -i $taskInterrupt -an -c:v libx264 -crf 20 -pix_fmt yuv420p $taskInterruptMovie
      if($LASTEXITCODE -ne 0){throw "Interruption encoding failed: $taskInterruptMovie"}
    }
  }
  $taskBefore=Join-Path $taskProject ('Docs/Captures/FidgetsBeforeV{0}/{1}_45.mp4' -f $BeforeVersion,$taskClip)
  if(Test-Path -LiteralPath $taskBefore){
    $taskAfter=Join-Path $taskOutput ('{0}_45.mp4' -f $taskClip)
    $taskComparison=Join-Path $taskOutput ('{0}_BeforeLeftAfterRightV{1}.mp4' -f $taskClip,$Version)
    # The old standing clip is shorter; hold its last frame without changing speed.
    $taskFilter='[0:v]scale=-2:360,tpad=stop_mode=clone:stop_duration='+$BeforeHoldSeconds.ToString([Globalization.CultureInfo]::InvariantCulture)+'[a];[1:v]scale=-2:360[b];[a][b]hstack=shortest=1[v]'
    & $taskFfmpeg -hide_banner -loglevel error -y -i $taskBefore -i $taskAfter -filter_complex $taskFilter -map '[v]' -an -c:v libx264 -crf 20 -pix_fmt yuv420p $taskComparison
    if($LASTEXITCODE -ne 0){throw "Comparison encoding failed: $taskComparison"}
  }
}
