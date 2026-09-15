param([ValidateSet('Blender','Unity')][string]$Source = 'Unity')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDir = Join-Path $taskRoot "Docs/Captures/Stage22/$Source"
$taskBitmap = [System.Drawing.Bitmap]::new(1600,960)
$taskGraphics = [System.Drawing.Graphics]::FromImage($taskBitmap)
$taskGraphics.Clear([System.Drawing.Color]::FromArgb(244,242,235))
$taskGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskFont = [System.Drawing.Font]::new('Segoe UI',15)
$taskTitle = [System.Drawing.Font]::new('Segoe UI',23,[System.Drawing.FontStyle]::Bold)
$taskBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(65,72,65))
$taskGraphics.DrawString("TINY DAYS  /  RABBIT STUDY",$taskTitle,$taskBrush,20,12)
$taskGraphics.DrawString("$Source render | Stage 2-2 | Static reference poses | Draft for visual review",$taskFont,$taskBrush,20,54)
$taskViews = @('Front','Side','Back','ThreeQuarter')
$taskLabels = @('FRONT','SIDE','BACK','THREE-QUARTER')
foreach ($taskRow in 0..1) {
    $taskPose = if ($taskRow -eq 0) { 'Biped' } else { 'Quadruped' }
    foreach ($taskColumn in 0..3) {
        $taskPrefix = if ($Source -eq 'Unity') { "Rabbit_$taskPose" } else { "Pose_$taskPose" }
        $taskImage = [System.Drawing.Image]::FromFile((Join-Path $taskDir ($taskPrefix+'_'+$taskViews[$taskColumn]+'.png')))
        try { $taskGraphics.DrawImage($taskImage,[System.Drawing.Rectangle]::new($taskColumn*400,95+$taskRow*430,400,400)) } finally { $taskImage.Dispose() }
        $taskGraphics.DrawString(($taskPose.ToUpper()+' / '+$taskLabels[$taskColumn]),$taskFont,$taskBrush,$taskColumn*400+18,496+$taskRow*430)
    }
}
$taskOutput = Join-Path $taskRoot "Docs/Captures/Stage22/$Source-ReviewSheet.png"
$taskBitmap.Save($taskOutput,[System.Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose();$taskBitmap.Dispose();$taskFont.Dispose();$taskTitle.Dispose();$taskBrush.Dispose()
Write-Output $taskOutput
