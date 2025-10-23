# Local build script for GeminiClient
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "./publish"
)

Write-Host "Building GeminiClientConsole..." -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Runtime: $Runtime" -ForegroundColor Gray
Write-Host "Output: $OutputPath" -ForegroundColor Gray
Write-Host ""

# Clean previous build
if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item -Path $OutputPath -Recurse -Force
}

# Build and publish
dotnet publish GeminiClientConsole/GeminiClientConsole.csproj `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $OutputPath `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output location: $OutputPath" -ForegroundColor Cyan
    
    # List generated files
    Write-Host ""
    Write-Host "Generated files:" -ForegroundColor Cyan
    Get-ChildItem -Path $OutputPath | ForEach-Object {
        $sizeInMB = [math]::Round($_.Length / 1MB, 2)
        $fileName = $_.Name
        Write-Host "  - ${fileName} (${sizeInMB} MB)" -ForegroundColor Gray
    }
    
    Write-Host ""
    $exePath = Join-Path $OutputPath "GeminiClientConsole.exe"
    Write-Host "Run with: .$exePath" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
