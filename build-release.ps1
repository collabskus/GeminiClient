# build-release.ps1 - PowerShell script for local builds
param(
    [Parameter(Mandatory=$false)]
    [string]$Version = "1.0.0",
    
    [Parameter(Mandatory=$false)]
    [string[]]$Runtimes = @("win-x64", "linux-x64", "osx-x64", "osx-arm64"),
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "Gemini Client Build Script v$Version" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

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
    
    # Publish the application
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
        -p:AssemblyVersion=$Version
    
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
"# SHA256 Checksums for Gemini Client v$Version" | Out-File $checksumFile
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
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "Version: $Version" -ForegroundColor White
Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Artifacts created in: ./artifacts" -ForegroundColor White
Write-Host "" -ForegroundColor White
Write-Host "Files created:" -ForegroundColor Yellow
Get-ChildItem "./artifacts" | ForEach-Object {
    $sizeMB = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($sizeMB MB)" -ForegroundColor White
}
Write-Host "========================================" -ForegroundColor Cyan

---

# build-release.sh - Bash script for Unix systems
#!/bin/bash

VERSION=${1:-"1.0.0"}
CONFIGURATION=${2:-"Release"}
SKIP_TESTS=${3:-false}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}Gemini Client Build Script v$VERSION${NC}"
echo -e "${CYAN}=====================================${NC}"

# Runtimes to build for
RUNTIMES=("linux-x64" "linux-arm64" "osx-x64" "osx-arm64" "win-x64")

# Clean previous builds
echo -e "\n${YELLOW}Cleaning previous builds...${NC}"
rm -rf ./publish ./artifacts

# Restore dependencies
echo -e "\n${YELLOW}Restoring dependencies...${NC}"
dotnet restore

# Run tests if not skipped
if [ "$SKIP_TESTS" != "true" ]; then
    echo -e "\n${YELLOW}Running tests...${NC}"
    if ! dotnet test --configuration $CONFIGURATION --verbosity minimal; then
        echo -e "${RED}Tests failed! Aborting build.${NC}"
        exit 1
    fi
fi

# Create artifacts directory
mkdir -p ./artifacts

# Build for each runtime
for runtime in "${RUNTIMES[@]}"; do
    echo -e "\n${GREEN}Building for $runtime...${NC}"
    
    output_path="./publish/$runtime"
    
    # Publish the application
    if ! dotnet publish ./GeminiClientConsole/GeminiClientConsole.csproj \
        --configuration $CONFIGURATION \
        --runtime $runtime \
        --self-contained true \
        --output $output_path \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -p:DebugType=None \
        -p:DebugSymbols=false \
        -p:Version=$VERSION \
        -p:FileVersion=$VERSION \
        -p:AssemblyVersion=$VERSION; then
        echo -e "${RED}Build failed for $runtime!${NC}"
        exit 1
    fi
    
    # Rename executable
    if [[ $runtime == win-* ]]; then
        extension=".exe"
    else
        extension=""
    fi
    
    old_name="$output_path/GeminiClientConsole$extension"
    new_name="$output_path/gemini-client-$runtime$extension"
    
    if [ -f "$old_name" ]; then
        mv "$old_name" "$new_name"
        
        # Make executable on Unix systems
        if [[ $runtime != win-* ]]; then
            chmod +x "$new_name"
        fi
    fi
    
    # Create archive
    echo -e "${YELLOW}Creating archive for $runtime...${NC}"
    
    if [[ $runtime == win-* ]]; then
        # Create ZIP for Windows
        cd "$output_path"
        zip -q -r "../../artifacts/gemini-client-$runtime-v$VERSION.zip" .
        cd ../..
        echo -e "${GREEN}Created: ./artifacts/gemini-client-$runtime-v$VERSION.zip${NC}"
    else
        # Create TAR.GZ for Unix systems
        cd "$output_path"
        tar -czf "../../artifacts/gemini-client-$runtime-v$VERSION.tar.gz" .
        cd ../..
        echo -e "${GREEN}Created: ./artifacts/gemini-client-$runtime-v$VERSION.tar.gz${NC}"
    fi
done

# Generate checksums
echo -e "\n${YELLOW}Generating checksums...${NC}"
cd ./artifacts
shasum -a 256 * > checksums.txt
cd ..

# Summary
echo -e "\n${CYAN}========================================${NC}"
echo -e "${GREEN}Build Complete!${NC}"
echo -e "Version: $VERSION"
echo -e "Configuration: $CONFIGURATION"
echo -e "Artifacts created in: ./artifacts"
echo -e "\nFiles created:"
for file in ./artifacts/*; do
    if [ -f "$file" ]; then
        size=$(du -h "$file" | cut -f1)
        filename=$(basename "$file")
        echo -e "  - $filename ($size)"
    fi
done
echo -e "${CYAN}========================================${NC}"