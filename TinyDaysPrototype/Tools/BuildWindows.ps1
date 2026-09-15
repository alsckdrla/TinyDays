$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Close the Unity Editor before building.' }
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
$process = Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','BuildWindows.Execute','-logFile',('"'+(Join-Path $project 'Logs/windows-build.log')+'"')) -PassThru -WindowStyle Hidden
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'Windows build failed. See Logs/windows-build.log.' }
Write-Output 'Tiny Days Windows build complete.'
