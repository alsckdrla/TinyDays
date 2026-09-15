param([switch]$SkipArt, [switch]$SkipBlenderRenders)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path $PSScriptRoot -Parent
$taskUnity = 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$taskBlender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
$taskRunning = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('/', '\').IndexOf($taskProject, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($taskRunning) { throw 'Close this project in Unity before batch regeneration. No editor was closed automatically.' }
New-Item -ItemType Directory -Force -Path (Join-Path $taskProject 'Logs') | Out-Null
if (!$SkipArt) {
    $taskArtArgs = @('--background','--factory-startup','--python-exit-code','1','--python',(Join-Path $PSScriptRoot 'generate_rabbit.py'))
    if ($SkipBlenderRenders) { $taskArtArgs += @('--','--skip-renders') }
    & $taskBlender @taskArtArgs *> (Join-Path $taskProject 'Logs/rabbit-blender.log')
    if ($LASTEXITCODE -ne 0) { throw 'Blender failed. See Logs/rabbit-blender.log.' }
}
$taskArgs = @('-batchmode','-quit','-projectPath',('"'+$taskProject+'"'),'-executeMethod','RabbitStudyBuilder.Execute','-logFile',('"'+(Join-Path $taskProject 'Logs/rabbit-unity.log')+'"'))
$taskProcess = Start-Process -FilePath $taskUnity -ArgumentList $taskArgs -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw 'Unity failed. See Logs/rabbit-unity.log.' }
if (!(Select-String -LiteralPath (Join-Path $taskProject 'Logs/rabbit-unity.log') -SimpleMatch 'TINYDAYS_STAGE22_OK' -Quiet)) { throw 'Unity finished without the verification marker.' }
Write-Output 'Rabbit study rebuilt and automatically verified. User visual approval remains separate.'
