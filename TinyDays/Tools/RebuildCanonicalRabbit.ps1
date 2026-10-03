param([switch]$SkipArt)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskRunning=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch building; no editor was closed.'}
New-Item -ItemType Directory -Force -Path (Join-Path $taskProject 'Logs') | Out-Null
if(!$SkipArt){
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'sync_adult_rabbit_model.py')
    if($LASTEXITCODE -ne 0){throw 'Canonical model sync failed'}
}
$taskArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','CanonicalRabbitUpdate.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/canonical-rabbit-unity.log')+'"'))
$taskProcess=Start-Process 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -Wait -PassThru
if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject 'Logs/canonical-rabbit-unity.log') -SimpleMatch 'CANONICAL_RABBIT_UNITY_OK' -Quiet)){throw 'Canonical Unity migration failed; see Logs/canonical-rabbit-unity.log'}
