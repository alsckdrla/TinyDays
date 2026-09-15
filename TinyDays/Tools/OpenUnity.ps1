$taskProject = Split-Path $PSScriptRoot -Parent
# This launcher opens the interactive editor for the user's visual review.
Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\2022.3.20f1\Editor\Unity.exe' -ArgumentList @('-projectPath',('"'+$taskProject+'"')) -WindowStyle Normal
