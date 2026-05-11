# PatchOpenApi.ps1
param(
    [string]$FilePath
)

if (-not (Test-Path $FilePath)) {
    Write-Host "Error: File not found at $FilePath"
    exit 1
}

$content = Get-Content $FilePath -Raw
$replaceSource = 'openapi: 3.0.4'
$replaceTarget = 'openapi: 3.1.0'

if ($content.StartsWith($replaceSource)) {
    $newContent = $replaceTarget + $content.Substring($replaceSource.Length)
    Set-Content $FilePath $newContent
    Write-Host "Patched openapi.yaml: '$replaceSource' replaced with '$replaceTarget'."
} else {
    Write-Host "No patch applied to openapi.yaml. First line is not '$replaceSource'."
}
