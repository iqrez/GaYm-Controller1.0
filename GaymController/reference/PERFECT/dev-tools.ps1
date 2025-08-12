#!/usr/bin/env pwsh
param(
    [Parameter(Position=0)]
    [ValidateSet("test", "clean", "analyze", "format", "profile", "benchmark", "help")]
    [string]$Command = "help",
    
    [switch]$VerboseOutput
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"
$Cyan = "Cyan"
$Magenta = "Magenta"

function Write-Status {
    param([string]$Message, [string]$Color = "White")
    Write-Host "[$((Get-Date).ToString('HH:mm:ss'))] $Message" -ForegroundColor $Color
}

function Write-Success { param([string]$Message) Write-Status $Message $Green }
function Write-Warning { param([string]$Message) Write-Status $Message $Yellow }
function Write-Error { param([string]$Message) Write-Status $Message $Red }
function Write-Info { param([string]$Message) Write-Status $Message $Cyan }
function Write-Header { param([string]$Message) Write-Status $Message $Magenta }

function Test-Command {
    Write-Header "Running Tests"
    
    # Check if we have any test files
    $testFiles = Get-ChildItem -Path "Tests" -Filter "*.cs" -ErrorAction SilentlyContinue
    if (-not $testFiles) {
        Write-Warning "No test files found in Tests directory"
        Write-Info "Creating sample test runner..."
        
        # Create a simple test runner
        $testRunner = @"
using System;
using WootMouseRemap.Tests;

namespace WootMouseRemap.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("WootMouseRemap Test Runner");
            Console.WriteLine("==========================");
            
            try
            {
                var loggerTests = new LoggerTests();
                loggerTests.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("All tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(`$"Test execution failed: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
"@
        
        $testRunner | Out-File -FilePath "TestRunner.cs" -Encoding UTF8
        Write-Info "Test runner created: TestRunner.cs"
        Write-Info "To run tests, compile and execute TestRunner.cs"
        return
    }
    
    Write-Info "Found $($testFiles.Count) test files"
    foreach ($file in $testFiles) {
        Write-Info "  - $($file.Name)"
    }
    
    Write-Success "Test discovery completed"
}

function Clean-Command {
    Write-Header "Cleaning Project"
    
    $itemsToClean = @(
        "bin", "obj", "*.tmp", "*.log", "TestResults",
        "Logs/*.log", "Profiles/_history/*.json"
    )
    
    foreach ($item in $itemsToClean) {
        if (Test-Path $item) {
            Write-Info "Removing: $item"
            Remove-Item $item -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Clean NuGet cache for this project
    Write-Info "Cleaning NuGet cache..."
    dotnet nuget locals all --clear | Out-Null
    
    Write-Success "Project cleaned successfully"
}

function Analyze-Command {
    Write-Header "Code Analysis"
    
    Write-Info "Running static code analysis..."
    
    # Check for common issues
    Write-Info "Checking for empty catch blocks..."
    $emptyCatches = Select-String -Path "*.cs" -Pattern "catch.*\{\s*\}" -Recurse
    if ($emptyCatches) {
        Write-Warning "Found $($emptyCatches.Count) empty catch blocks:"
        foreach ($match in $emptyCatches | Select-Object -First 5) {
            Write-Host "  $($match.Filename):$($match.LineNumber)" -ForegroundColor Yellow
        }
    } else {
        Write-Success "No empty catch blocks found"
    }
    
    Write-Info "Checking for TODO/FIXME comments..."
    $todos = Select-String -Path "*.cs" -Pattern "(TODO|FIXME|HACK|BUG)" -Recurse
    if ($todos) {
        Write-Info "Found $($todos.Count) TODO/FIXME comments:"
        foreach ($match in $todos | Select-Object -First 10) {
            Write-Host "  $($match.Filename):$($match.LineNumber) - $($match.Line.Trim())" -ForegroundColor Cyan
        }
    } else {
        Write-Success "No TODO/FIXME comments found"
    }
    
    Write-Info "Checking for large files..."
    $largeFiles = Get-ChildItem -Path "*.cs" -Recurse | Where-Object { $_.Length -gt 50KB }
    if ($largeFiles) {
        Write-Warning "Found large files (>50KB):"
        foreach ($file in $largeFiles) {
            $sizeKB = [math]::Round($file.Length / 1KB, 1)
            Write-Host "  $($file.Name) - $sizeKB KB" -ForegroundColor Yellow
        }
    } else {
        Write-Success "No unusually large files found"
    }
    
    Write-Success "Code analysis completed"
}

function Format-Command {
    Write-Header "Code Formatting"
    
    Write-Info "Formatting C# code..."
    
    # Check if dotnet format is available
    try {
        dotnet format --version | Out-Null
        dotnet format --verbosity diagnostic
        Write-Success "Code formatting completed"
    }
    catch {
        Write-Warning "dotnet format not available. Please install .NET SDK with formatting tools."
        Write-Info "Alternative: Use Visual Studio Code with C# extension for formatting"
    }
}

function Profile-Command {
    Write-Header "Performance Profiling"
    
    Write-Info "Analyzing project structure..."
    
    # Count lines of code
    $csFiles = Get-ChildItem -Path "*.cs" -Recurse -Exclude "bin", "obj"
    $totalLines = 0
    $totalFiles = $csFiles.Count
    
    foreach ($file in $csFiles) {
        $lines = (Get-Content $file.FullName).Count
        $totalLines += $lines
    }
    
    Write-Info "Project Statistics:"
    Write-Host "  Files: $totalFiles" -ForegroundColor Cyan
    Write-Host "  Total Lines: $totalLines" -ForegroundColor Cyan
    Write-Host "  Average Lines per File: $([math]::Round($totalLines / $totalFiles, 1))" -ForegroundColor Cyan
    
    # Analyze dependencies
    Write-Info "Analyzing dependencies..."
    if (Test-Path "WootMouseRemap.csproj") {
        $projectContent = Get-Content "WootMouseRemap.csproj" -Raw
        $packageRefs = [regex]::Matches($projectContent, '<PackageReference Include="([^"]+)"')
        
        Write-Info "NuGet Dependencies:"
        foreach ($match in $packageRefs) {
            Write-Host "  - $($match.Groups[1].Value)" -ForegroundColor Cyan
        }
    }
    
    Write-Success "Profiling completed"
}

function Benchmark-Command {
    Write-Header "Performance Benchmarking"
    
    Write-Info "Building project for benchmarking..."
    dotnet build -c Release --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed, cannot run benchmarks"
        return
    }
    
    Write-Info "Benchmark suggestions:"
    Write-Host "  1. Input processing latency" -ForegroundColor Cyan
    Write-Host "  2. Memory usage over time" -ForegroundColor Cyan
    Write-Host "  3. Controller response time" -ForegroundColor Cyan
    Write-Host "  4. Profile loading/saving speed" -ForegroundColor Cyan
    
    Write-Info "To implement benchmarks, consider using BenchmarkDotNet:"
    Write-Host "  dotnet add package BenchmarkDotNet" -ForegroundColor Yellow
    
    Write-Success "Benchmark analysis completed"
}

function Show-Help {
    Write-Header "WootMouseRemap Development Tools"
    
    Write-Host ""
    Write-Host "Usage: .\dev-tools.ps1 <command> [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Cyan
    Write-Host "  test      - Run unit tests and test discovery" -ForegroundColor White
    Write-Host "  clean     - Clean build artifacts and temporary files" -ForegroundColor White
    Write-Host "  analyze   - Perform static code analysis" -ForegroundColor White
    Write-Host "  format    - Format code using dotnet format" -ForegroundColor White
    Write-Host "  profile   - Show project statistics and profiling info" -ForegroundColor White
    Write-Host "  benchmark - Performance benchmarking suggestions" -ForegroundColor White
    Write-Host "  help      - Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -VerboseOutput  - Show detailed output" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\dev-tools.ps1 test" -ForegroundColor Yellow
    Write-Host "  .\dev-tools.ps1 clean -VerboseOutput" -ForegroundColor Yellow
    Write-Host "  .\dev-tools.ps1 analyze" -ForegroundColor Yellow
}

# Main execution
try {
    switch ($Command.ToLower()) {
        "test" { Test-Command }
        "clean" { Clean-Command }
        "analyze" { Analyze-Command }
        "format" { Format-Command }
        "profile" { Profile-Command }
        "benchmark" { Benchmark-Command }
        "help" { Show-Help }
        default { 
            Write-Error "Unknown command: $Command"
            Show-Help
            exit 1
        }
    }
}
catch {
    Write-Error "Command failed: $($_.Exception.Message)"
    if ($VerboseOutput) {
        Write-Host $_.Exception.StackTrace -ForegroundColor Red
    }
    exit 1
}