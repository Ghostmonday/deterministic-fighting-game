# Cleanup Script for Deterministic Fighting Game Engine
# Removes build artifacts, temporary files, and resets the project to a clean state

param(
    [switch]$Help,
    [switch]$DryRun,
    [switch]$Force,
    [string[]]$Targets = @("all")
)

# Configuration
$ProjectRoot = "."
$ScriptsDir = "scripts"

# Colors for output
$SuccessColor = "Green"
$ErrorColor = "Red"
$InfoColor = "Yellow"
$WarningColor = "Magenta"
$HeaderColor = "Cyan"

function Write-Header {
    param([string]$Message)
    Write-Host "`n================================================" -ForegroundColor $HeaderColor
    Write-Host $Message -ForegroundColor $HeaderColor
    Write-Host "================================================" -ForegroundColor $HeaderColor
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $SuccessColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ErrorColor
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $InfoColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $WarningColor
}

function Show-Help {
    Write-Header "CLEANUP SCRIPT - Deterministic Fighting Game Engine"
    Write-Host "Usage: .\scripts\clean.ps1 [-Targets <targets>] [-DryRun] [-Force] [-Help]`n" -ForegroundColor $InfoColor

    Write-Host "Available Targets:" -ForegroundColor $HeaderColor
    Write-Host "  all              - Clean everything (default)" -ForegroundColor $InfoColor
    Write-Host "  build            - Clean build artifacts (bin/, obj/)" -ForegroundColor $InfoColor
    Write-Host "  dotnet           - Clean .NET runtime files (dotnet/)" -ForegroundColor $InfoColor
    Write-Host "  test             - Clean test outputs and logs" -ForegroundColor $InfoColor
    Write-Host "  unity            - Clean Unity-related files" -ForegroundColor $InfoColor
    Write-Host "  temp             - Clean temporary files" -ForegroundColor $InfoColor
    Write-Host "  nuget            - Clean NuGet packages and cache" -ForegroundColor $InfoColor

    Write-Host "`nOptions:" -ForegroundColor $HeaderColor
    Write-Host "  -Help            - Show this help message" -ForegroundColor $InfoColor
    Write-Host "  -DryRun          - Show what would be deleted without actually deleting" -ForegroundColor $InfoColor
    Write-Host "  -Force           - Skip confirmation prompts" -ForegroundColor $InfoColor
    Write-Host "  -Targets <list>  - Comma-separated list of targets to clean" -ForegroundColor $InfoColor

    Write-Host "`nExamples:" -ForegroundColor $HeaderColor
    Write-Host "  .\scripts\clean.ps1                     # Clean everything with confirmation" -ForegroundColor $InfoColor
    Write-Host "  .\scripts\clean.ps1 -Force             # Clean everything without confirmation" -ForegroundColor $InfoColor
    Write-Host "  .\scripts\clean.ps1 -DryRun            # Show what would be cleaned" -ForegroundColor $InfoColor
    Write-Host "  .\scripts\clean.ps1 -Targets build     # Clean build artifacts only" -ForegroundColor $InfoColor
    Write-Host "  .\scripts\clean.ps1 -Targets build,test # Clean build and test artifacts" -ForegroundColor $InfoColor

    Write-Host "`nTarget Details:" -ForegroundColor $HeaderColor
    Write-Host "  Build Artifacts:" -ForegroundColor $InfoColor
    Write-Host "    - bin/ directory" -ForegroundColor "DarkGray"
    Write-Host "    - obj/ directory" -ForegroundColor "DarkGray"
    Write-Host "    - *.dll, *.exe, *.pdb files" -ForegroundColor "DarkGray"

    Write-Host "  .NET Runtime:" -ForegroundColor $InfoColor
    Write-Host "    - dotnet/ directory" -ForegroundColor "DarkGray"

    Write-Host "  Test Outputs:" -ForegroundColor $InfoColor
    Write-Host "    - Test logs and reports" -ForegroundColor "DarkGray"
    Write-Host "    - Coverage reports" -ForegroundColor "DarkGray"

    Write-Host "  Unity Files:" -ForegroundColor $InfoColor
    Write-Host "    - Library/, Temp/, Build/ directories" -ForegroundColor "DarkGray"
    Write-Host "    - *.meta files" -ForegroundColor "DarkGray"

    Write-Host "  Temporary Files:" -ForegroundColor $InfoColor
    Write-Host "    - *.tmp, *.cache, *.log files" -ForegroundColor "DarkGray"

    Write-Host "  NuGet Packages:" -ForegroundColor $InfoColor
    Write-Host "    - packages/ directory" -ForegroundColor "DarkGray"
    Write-Host "    - NuGet cache files" -ForegroundColor "DarkGray"

    exit 0
}

function Get-UserConfirmation {
    param([string]$Message)

    if ($Force) {
        return $true
    }

    Write-Host "`n$Message" -ForegroundColor $WarningColor
    $response = Read-Host "Continue? (y/N)"

    return ($response -eq 'y' -or $response -eq 'Y')
}

function Remove-PathSafely {
    param(
        [string]$Path,
        [string]$Description,
        [switch]$DryRun
    )

    if (Test-Path $Path) {
        if ($DryRun) {
            Write-Info "Would remove: $Description ($Path)"
            return $true
        }

        try {
            Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
            Write-Success "Removed: $Description"
            return $true
        }
        catch {
            Write-Error "Failed to remove $Description: $_"
            return $false
        }
    }
    else {
        Write-Info "Not found: $Description ($Path)"
        return $true
    }
}

function Remove-FilesByPattern {
    param(
        [string]$Pattern,
        [string]$Description,
        [switch]$DryRun,
        [switch]$Recursive = $true
    )

    $files = Get-ChildItem -Path $ProjectRoot -Include $Pattern -Recurse:$Recursive -ErrorAction SilentlyContinue

    if ($files.Count -gt 0) {
        if ($DryRun) {
            Write-Info "Would remove $($files.Count) $Description files"
            foreach ($file in $files) {
                Write-Host "  $($file.FullName)" -ForegroundColor "DarkGray"
            }
            return $true
        }

        $successCount = 0
        foreach ($file in $files) {
            try {
                Remove-Item -Path $file.FullName -Force -ErrorAction Stop
                $successCount++
            }
            catch {
                Write-Error "Failed to remove $($file.FullName): $_"
            }
        }

        if ($successCount -eq $files.Count) {
            Write-Success "Removed $successCount $Description files"
            return $true
        }
        else {
            Write-Warning "Removed $successCount of $($files.Count) $Description files"
            return $false
        }
    }
    else {
        Write-Info "No $Description files found"
        return $true
    }
}

function Clean-BuildArtifacts {
    param([switch]$DryRun)

    Write-Header "CLEANING BUILD ARTIFACTS"

    $paths = @(
        @{Path = "bin"; Description = "Binaries directory"},
        @{Path = "obj"; Description = "Object files directory"}
    )

    $results = @()
    foreach ($item in $paths) {
        $results += Remove-PathSafely -Path $item.Path -Description $item.Description -DryRun:$DryRun
    }

    $patterns = @(
        @{Pattern = "*.dll"; Description = "DLL files"},
        @{Pattern = "*.exe"; Description = "Executable files"},
        @{Pattern = "*.pdb"; Description = "Debug symbol files"},
        @{Pattern = "*.cache"; Description = "Build cache files"}
    )

    foreach ($pattern in $patterns) {
        $results += Remove-FilesByPattern -Pattern $pattern.Pattern -Description $pattern.Description -DryRun:$DryRun
    }

    return ($results -notcontains $false)
}

function Clean-DotNetRuntime {
    param([switch]$DryRun)

    Write-Header "CLEANING .NET RUNTIME FILES"

    $result = Remove-PathSafely -Path "dotnet" -Description ".NET runtime directory" -DryRun:$DryRun

    return $result
}

function Clean-TestOutputs {
    param([switch]$DryRun)

    Write-Header "CLEANING TEST OUTPUTS"

    $patterns = @(
        @{Pattern = "*.log"; Description = "Log files"},
        @{Pattern = "*.trx"; Description = "Test result files"},
        @{Pattern = "*.coverage"; Description = "Code coverage files"},
        @{Pattern = "TestResults"; Description = "Test results directories"}
    )

    $results = @()
    foreach ($pattern in $patterns) {
        $results += Remove-FilesByPattern -Pattern $pattern.Pattern -Description $pattern.Description -DryRun:$DryRun
    }

    return ($results -notcontains $false)
}

function Clean-UnityFiles {
    param([switch]$DryRun)

    Write-Header "CLEANING UNITY FILES"

    $paths = @(
        @{Path = "Library"; Description = "Unity Library directory"},
        @{Path = "Temp"; Description = "Unity Temp directory"},
        @{Path = "Build"; Description = "Unity Build directory"},
        @{Path = "Builds"; Description = "Unity Builds directory"},
        @{Path = "Logs"; Description = "Unity Logs directory"},
        @{Path = "UserSettings"; Description = "Unity UserSettings directory"}
    )

    $results = @()
    foreach ($item in $paths) {
        $results += Remove-PathSafely -Path $item.Path -Description $item.Description -DryRun:$DryRun
    }

    $patterns = @(
        @{Pattern = "*.meta"; Description = "Unity meta files"},
        @{Pattern = "*.unitypackage"; Description = "Unity package files"}
    )

    foreach ($pattern in $patterns) {
        $results += Remove-FilesByPattern -Pattern $pattern.Pattern -Description $pattern.Description -DryRun:$DryRun
    }

    return ($results -notcontains $false)
}

function Clean-TempFiles {
    param([switch]$DryRun)

    Write-Header "CLEANING TEMPORARY FILES"

    $patterns = @(
        @{Pattern = "*.tmp"; Description = "Temporary files"},
        @{Pattern = "*.temp"; Description = "Temporary files"},
        @{Pattern = "*.cache"; Description = "Cache files"},
        @{Pattern = "Thumbs.db"; Description = "Thumbnail cache"},
        @{Pattern = ".DS_Store"; Description = "macOS metadata"}
    )

    $results = @()
    foreach ($pattern in $patterns) {
        $results += Remove-FilesByPattern -Pattern $pattern.Pattern -Description $pattern.Description -DryRun:$DryRun
    }

    return ($results -notcontains $false)
}

function Clean-NuGetPackages {
    param([switch]$DryRun)

    Write-Header "CLEANING NUGET PACKAGES"

    $paths = @(
        @{Path = "packages"; Description = "NuGet packages directory"}
    )

    $results = @()
    foreach ($item in $paths) {
        $results += Remove-PathSafely -Path $item.Path -Description $item.Description -DryRun:$DryRun
    }

    # Clean NuGet cache files
    $cacheFiles = @(
        "project.assets.json",
        "project.nuget.cache",
        "*.nuget.dgspec.json",
        "*.nuget.g.props",
        "*.nuget.g.targets"
    )

    foreach ($pattern in $cacheFiles) {
        $results += Remove-FilesByPattern -Pattern $pattern -Description "NuGet cache files" -DryRun:$DryRun -Recursive $false
    }

    return ($results -notcontains $false)
}

function Clean-All {
    param([switch]$DryRun)

    Write-Header "CLEANING EVERYTHING"

    $results = @()
    $results += Clean-BuildArtifacts -DryRun:$DryRun
    $results += Clean-DotNetRuntime -DryRun:$DryRun
    $results += Clean-TestOutputs -DryRun:$DryRun
    $results += Clean-UnityFiles -DryRun:$DryRun
    $results += Clean-TempFiles -DryRun:$DryRun
    $results += Clean-NuGetPackages -DryRun:$DryRun

    return ($results -notcontains $false)
}

# Main execution
try {
    # Handle help
    if ($Help) {
        Show-Help
    }

    # Show banner
    Write-Header "DETERMINISTIC FIGHTING GAME ENGINE - CLEANUP UTILITY"

    # Check if we're in dry run mode
    if ($DryRun) {
        Write-Warning "DRY RUN MODE - No files will be deleted"
        Write-Host ""
    }

    # Get confirmation for non-dry runs
    if (-not $DryRun -and -not $Force) {
        if ($Targets -contains "all") {
            $confirmed = Get-UserConfirmation -Message "This will delete ALL build artifacts, temporary files, and reset the project. This action cannot be undone."
        }
        else {
            $targetList = $Targets -join ", "
            $confirmed = Get-UserConfirmation -Message "This will clean: $targetList. This action cannot be undone."
        }

        if (-not $confirmed) {
            Write-Info "Cleanup cancelled by user"
            exit 0
        }
    }

    # Process targets
    $results = @{}

    foreach ($target in $Targets) {
        switch ($target.ToLower()) {
            "all" {
                $results["all"] = Clean-All -DryRun:$DryRun
                break
            }
            "build" {
                $results["build"] = Clean-BuildArtifacts -DryRun:$DryRun
                break
            }
            "dotnet" {
                $results["dotnet"] = Clean-DotNetRuntime -DryRun:$DryRun
                break
            }
            "test" {
                $results["test"] = Clean-TestOutputs -DryRun:$DryRun
                break
            }
            "unity" {
                $results["unity"] = Clean-UnityFiles -DryRun:$DryRun
                break
            }
            "temp" {
                $results["temp"] = Clean-TempFiles -DryRun:$DryRun
                break
            }
            "nuget" {
                $results["nuget"] = Clean-NuGetPackages -DryRun:$DryRun
                break
            }
            default {
                Write-Error "Unknown target: $target"
                Write-Host "Use -Help to see available targets" -ForegroundColor $InfoColor
                exit 1
            }
        }
    }

    # Summary
    Write-Header "CLEANUP SUMMARY"

    $allSuccessful = $true
    foreach ($key in $results.Keys) {
        if ($results[$key]) {
            Write-Success "$key: Cleaned successfully"
        }
        else {
            Write-Error "$key: Cleanup failed or partially failed"
            $allSuccessful = $false
        }
    }

    if ($DryRun) {
        Write-Warning "DRY RUN COMPLETE - No files were actually deleted"
        Write-Host "`nTo perform actual cleanup, run without -DryRun flag" -ForegroundColor $InfoColor
    }
    elseif ($allSuccessful) {
        Write-Success "Cleanup completed successfully!"
        Write-Host "`nThe project is now in a clean state." -ForegroundColor $SuccessColor
        Write-Host "You can rebuild with: .\scripts\build-test.ps1" -ForegroundColor $InfoColor
    }
    else {
        Write-Warning "Cleanup completed with some issues"
        Write-Host "`nSome files may not have been cleaned properly." -ForegroundColor $WarningColor
        Write-Host "Check permissions or try running with administrator privileges." -ForegroundColor $InfoColor
    }

    exit $(if ($allSuccessful) { 0 } else { 1 })
}
catch {
    Write-Error "Unexpected error during cleanup: $_"
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor $ErrorColor
    exit 1
}
