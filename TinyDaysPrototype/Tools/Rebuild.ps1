param([switch]$Verify, [switch]$SkipArt)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Close the Unity Editor before running the rebuild script.' }
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
if (!$SkipArt) {
& $blender --background --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'generate_art.py') *> (Join-Path $project 'Logs/blender-art.log')
if ($LASTEXITCODE -ne 0) { throw 'Blender generation failed. See Logs/blender-art.log.' }
}
$method = if ($Verify) { 'VerifyDiorama.Execute' } else { 'BuildDiorama.Build' }
$arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-executeMethod', $method, '-logFile', ('"' + (Join-Path $project 'Logs/rebuild.log') + '"'))
if (!$Verify) { $arguments += '-quit' }
$process = Start-Process -FilePath $unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'Unity failed. See Logs/rebuild.log.' }
Write-Output 'Tiny Days rebuild complete.'
