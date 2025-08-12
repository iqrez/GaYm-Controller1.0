param(
    [switch]$Publish,
    [switch]$Clean,
    [switch]$Test,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $false,
    [switch]$SingleFile = $true,
    [switch]$Verbose
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"
$Cyan = "Cyan"

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Status $Message $Green
}

function Write-Warning {
    param([string]$Message)
    Write-Status $Message $Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Status $Message $Red
}

function Write-Info {
    param([string]$Message)
    Write-Status $Message $Cyan
}

try {
    Write-Info "=== WootMouseRemap Build Script ==="
    Write-Info "Configuration: $Configuration"
    Write-Info "Runtime: $Runtime"
    Write-Info "Self-Contained: $SelfContained"
    Write-Info "Single File: $SingleFile"
    
    # Clean if requested
    if ($Clean) {
        Write-Info "Cleaning previous builds..."
        dotnet clean WootMouseRemap.csproj -c $Configuration
        if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
        if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }
        Write-Success "Clean completed"
    }

    # Restore packages
    Write-Info "Restoring NuGet packages..."
    $restoreArgs = @("restore", "WootMouseRemap.csproj")
    if ($Verbose) { $restoreArgs += "--verbosity", "detailed" }
    
    & dotnet @restoreArgs
    if ($LASTEXITCODE -ne 0) { throw "Package restore failed" }
    Write-Success "Package restore completed"

    # Build
    Write-Info "Building solution..."
    $buildArgs = @("build", "WootMouseRemap.csproj", "-c", $Configuration, "--no-restore")
    if ($Verbose) { $buildArgs += "--verbosity", "detailed" }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Success "Build completed successfully"

    # Run tests if requested
    if ($Test) {
        Write-Info "Running tests..."
        $testArgs = @("test", "-c", $Configuration, "--no-build", "--logger", "console;verbosity=normal")
        
        & dotnet @testArgs
        if ($LASTEXITCODE -ne 0) { 
            Write-Warning "Some tests failed, but continuing with build"
        } else {
            Write-Success "All tests passed"
        }
    }

    # Publish if requested
    if ($Publish) {
        Write-Info "Publishing application..."
        
        $publishArgs = @(
            "publish", 
            "WootMouseRemap.csproj",
            "-c", $Configuration,
            "-r", $Runtime,
            "--no-build"
        )
        
        if ($SelfContained) {
            $publishArgs += "-p:SelfContained=true"
        } else {
            $publishArgs += "-p:SelfContained=false"
        }
        
        if ($SingleFile) {
            $publishArgs += "-p:PublishSingleFile=true"
        }
        
        # Additional publish properties for optimization
        $publishArgs += "-p:PublishTrimmed=false"  # Disable trimming for compatibility
        $publishArgs += "-p:PublishReadyToRun=true"  # Enable R2R for faster startup
        $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"  # Include native libs
        
        if ($Verbose) { $publishArgs += "--verbosity", "detailed" }
        
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
        
        # Get publish output path
        $publishPath = "bin\$Configuration\net8.0-windows\$Runtime\publish"
        
        Write-Success "Publish completed successfully"
        Write-Info "Published to: $publishPath"
        
        # Show file information
        if (Test-Path $publishPath) {
            $files = Get-ChildItem $publishPath -File
            Write-Info "Published files:"
            foreach ($file in $files) {
                $sizeKB = [math]::Round($file.Length / 1KB, 2)
                Write-Host "  $($file.Name) ($sizeKB KB)" -ForegroundColor Gray
            }
        }
        
        # Create version info file
        $gitCommit = try { git rev-parse HEAD 2>$null } catch { "Unknown" }
        $gitBranch = try { git branch --show-current 2>$null } catch { "Unknown" }
        
        $versionInfo = @{
            BuildDate = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
            Configuration = $Configuration
            Runtime = $Runtime
            SelfContained = $SelfContained
            SingleFile = $SingleFile
            GitCommit = $gitCommit
            GitBranch = $gitBranch
        }
        
        $versionInfoPath = Join-Path $publishPath "build-info.json"
        $versionInfo | ConvertTo-Json -Depth 2 | Out-File $versionInfoPath -Encoding UTF8
        Write-Info "Build info saved to: build-info.json"
    }

    Write-Success "=== Build completed successfully ==="
    
} catch {
    Write-Error "Build failed: $($_.Exception.Message)"
    exit 1
}
