$project = Split-Path $PSScriptRoot -Parent
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList @('-projectPath', ('"'+$project+'"')) -WindowStyle Hidden
