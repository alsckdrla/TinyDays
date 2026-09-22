param([switch]$SkipArt,[switch]$BuildPlayer)
$ErrorActionPreference='Stop'
$taskProject=Split-Path $PSScriptRoot -Parent
$taskRunning=Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/','\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase) -ge 0 }
if($taskRunning){throw 'Close this project in Unity before batch building; no editor was closed.'}
New-Item -ItemType Directory -Force -Path (Join-Path $taskProject 'Logs') | Out-Null
if(!$SkipArt){
    $taskArtArgs=@('--background','--factory-startup','--python-exit-code','1','--python',('"'+(Join-Path $PSScriptRoot 'generate_adult_rabbit.py')+'"'))
    $taskArtProcess=Start-Process -FilePath 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' -ArgumentList $taskArtArgs -WindowStyle Hidden -RedirectStandardOutput (Join-Path $taskProject 'Logs/adult-rabbit-art.log') -RedirectStandardError (Join-Path $taskProject 'Logs/adult-rabbit-art-errors.log') -Wait -PassThru
    if($taskArtProcess.ExitCode -ne 0){throw 'Blender failed; see Logs/adult-rabbit-art-errors.log'}
}
function Run-AdultUnity($method,$log,$marker){
    $taskArgs=@('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod',$method,'-logFile',('"'+(Join-Path $taskProject $log)+'"'))
    $taskProcess=Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
    $taskProcess.WaitForExit()
    if($taskProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath (Join-Path $taskProject $log) -SimpleMatch $marker -Quiet)){throw "Unity failed; see $log"}
}
Run-AdultUnity 'AdultRabbitBuilder.Execute' 'Logs/adult-rabbit-unity.log' 'ADULT_RABBIT_UNITY_OK'
if($BuildPlayer){Run-AdultUnity 'AdultRabbitBuilder.BuildPlayer' 'Logs/adult-rabbit-player-build.log' 'ADULT_RABBIT_PLAYER_OK'}
Write-Output 'Adult rabbit created and automatically checked. User visual approval and live input remain separate.'
