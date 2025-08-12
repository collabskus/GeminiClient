# build-release.ps1 - PowerShell script for local builds
param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "", # Leave empty for auto-detection

    [Parameter(Mandatory=$false)]
    [string[]]$Runtimes = @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "linux-musl-x64", "osx-x64", "osx-arm64"),
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests
)

# Auto-detect version from git tags if not provided
if ([string]::IsNullOrEmpty($Version)) {
    try {
        $gitTag = git describe --tags --exact-match HEAD 2>$null
        if ($gitTag -match '^v?(.+)$') {
            $Version = $Matches[1]
            Write-Host "Auto-detected version from git tag: $Version" -ForegroundColor Green
        } else {
            throw "No exact tag match"
        }
    }
    catch {
        Write-Host "Could not auto-detect version. Please provide -Version parameter or create a git tag." -ForegroundColor Red
        exit 1
    }
}

$ErrorActionPreference = "Stop"

Write-Host "Gemini Client Build Script v$Version (with Streaming Support)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "./publish") {
    Remove-Item -Path "./publish" -Recurse -Force
}
if (Test-Path "./artifacts") {
    Remove-Item -Path "./artifacts" -Recurse -Force
}

# Restore dependencies
Write-Host "`nRestoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Run tests if not skipped
if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed! Aborting build." -ForegroundColor Red
        exit 1
    }
}

# Create artifacts directory
New-Item -ItemType Directory -Path "./artifacts" -Force | Out-Null

# Build for each runtime
foreach ($runtime in $Runtimes) {
    Write-Host "`nBuilding for $runtime..." -ForegroundColor Green
    
    $outputPath = "./publish/$runtime"
    
    # Publish the application with streaming optimizations
    dotnet publish ./GeminiClientConsole/GeminiClientConsole.csproj `
        --configuration $Configuration `
        --runtime $runtime `
        --self-contained true `
        --output $outputPath `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:Version=$Version `
        -p:FileVersion=$Version `
        -p:AssemblyVersion=$Version `
        -p:ServerGarbageCollection=true `
        -p:ConcurrentGarbageCollection=true `
        -p:RetainVMGarbageCollection=true
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $runtime!" -ForegroundColor Red
        exit 1
    }
    
    # Rename executable
    $extension = if ($runtime -like "win-*") { ".exe" } else { "" }
    $oldName = Join-Path $outputPath "GeminiClientConsole$extension"
    $newName = Join-Path $outputPath "gemini-client-$runtime$extension"
    
    if (Test-Path $oldName) {
        Move-Item -Path $oldName -Destination $newName -Force
        
        # Make executable on Unix systems
        if ($runtime -notlike "win-*") {
            if ($IsLinux -or $IsMacOS) {
                chmod +x $newName
            }
        }
    }
    
    # Create archive
    Write-Host "Creating archive for $runtime..." -ForegroundColor Yellow
    
    if ($runtime -like "win-*") {
        # Create ZIP for Windows
        $zipPath = "./artifacts/gemini-client-$runtime-v$Version.zip"
        Compress-Archive -Path "$outputPath/*" -DestinationPath $zipPath -Force
        Write-Host "Created: $zipPath" -ForegroundColor Green
    }
    else {
        # Create TAR.GZ for Unix systems
        $tarPath = "./artifacts/gemini-client-$runtime-v$Version.tar.gz"
        
        if ($IsLinux -or $IsMacOS) {
            # Use native tar on Unix
            Push-Location $outputPath
            tar -czf "../../artifacts/gemini-client-$runtime-v$Version.tar.gz" .
            Pop-Location
        }
        else {
            # On Windows, create a ZIP instead (or install tar)
            $zipPath = "./artifacts/gemini-client-$runtime-v$Version.zip"
            Compress-Archive -Path "$outputPath/*" -DestinationPath $zipPath -Force
            Write-Host "Created ZIP instead of TAR.GZ (Windows host): $zipPath" -ForegroundColor Yellow
        }
    }
}

# Generate checksums
Write-Host "`nGenerating checksums..." -ForegroundColor Yellow
$checksumFile = "./artifacts/checksums.txt"
"# SHA256 Checksums for Gemini Client v$Version (with Streaming Support)" | Out-File $checksumFile
"# Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" | Add-Content $checksumFile
"" | Add-Content $checksumFile

Get-ChildItem "./artifacts" -Filter "*.zip" | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash
    "$hash  $($_.Name)" | Add-Content $checksumFile
}

Get-ChildItem "./artifacts" -Filter "*.tar.gz" | ForEach-Object {
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash
    "$hash  $($_.Name)" | Add-Content $checksumFile
}

# Summary
Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host "Build Complete! 🌊 Streaming Support Included" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Runtimes: $($Runtimes.Count) platforms" -ForegroundColor White
Write-Host "Artifacts created in: ./artifacts" -ForegroundColor White
Write-Host "" -ForegroundColor White
Write-Host "Files created:" -ForegroundColor Yellow
Get-ChildItem "./artifacts" | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($sizeMB MB)" -ForegroundColor White
}
Write-Host "`nFeatures included in this build:" -ForegroundColor Yellow
Write-Host "  ✅ Real-time streaming with SSE support" -ForegroundColor Green
Write-Host "  ✅ Interactive model selection with animations" -ForegroundColor Green  
Write-Host "  ✅ Advanced performance metrics" -ForegroundColor Green
Write-Host "  ✅ Session statistics tracking" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan