param (
    [string]$src,
    [string]$dst
)

Add-Type -AssemblyName System.Drawing

Write-Host "Opening: $src"
if (-not (Test-Path $src)) {
    Write-Error "Source file not found: $src"
    exit 1
}

$img = [System.Drawing.Image]::FromFile($src)
Write-Host "Flipping..."
$img.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)

Write-Host "Saving to: $dst"
$img.Save($dst)
$img.Dispose()
Write-Host "Done."
