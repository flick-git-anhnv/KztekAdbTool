<#
.SYNOPSIS
    Create a Junction Link for the .gemini configuration directory to another project.
.DESCRIPTION
    This script creates an NTFS Directory Junction Link pointing to the central .gemini folder
    and automatically adds .gemini to the target project's .gitignore file.
.PARAMETER ProjectDir
    Absolute path to the root directory of the target project.
.EXAMPLE
    .\setup-gemini-link.ps1 -ProjectDir "C:\Users\nguye\Desktop\MyOtherProject"
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true, HelpMessage = "Path to the target project directory")]
    [string]$ProjectDir
)

# 1. Determine central .gemini path (folder containing this script + .gemini)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if ([string]::IsNullOrEmpty($ScriptDir)) {
    $ScriptDir = $PWD.Path
}
$SourceGeminiPath = Join-Path $ScriptDir ".gemini"

Write-Host "=== GEMINI AGENT JUNCTION LINK SETUP ===" -ForegroundColor Cyan
Write-Host "Source config: $SourceGeminiPath" -ForegroundColor Gray

# Check if central folder exists
if (-not (Test-Path $SourceGeminiPath)) {
    Write-Error "Central .gemini directory not found at: $SourceGeminiPath"
    exit 1
}

# 2. Normalize and check target project directory
if (-not (Test-Path $ProjectDir)) {
    Write-Error "Target project does not exist: $ProjectDir"
    exit 1
}
$TargetProjectFullPath = (Get-Item $ProjectDir).FullName
$TargetGeminiLinkPath = Join-Path $TargetProjectFullPath ".gemini"

Write-Host "Target project: $TargetProjectFullPath" -ForegroundColor Gray
Write-Host "Link path: $TargetGeminiLinkPath" -ForegroundColor Gray

# 3. Handle existing .gemini path in target project
if (Test-Path $TargetGeminiLinkPath) {
    $item = Get-Item $TargetGeminiLinkPath
    if ($item.Attributes -match "ReparsePoint") {
        Write-Host "Junction link or Symlink '.gemini' already exists at target. Re-creating..." -ForegroundColor Yellow
        Remove-Item -Path $TargetGeminiLinkPath -Force
    } else {
        Write-Warning "Physical directory '.gemini' already exists at target."
        $Choice = Read-Host "Do you want to BACKUP (.gemini_backup) and REPLACE with Junction Link? (Y/N)"
        if ($Choice -eq 'Y' -or $Choice -eq 'y') {
            $BackupPath = Join-Path $TargetProjectFullPath (".gemini_backup_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
            Rename-Item -Path $TargetGeminiLinkPath -NewName $BackupPath -Force
            Write-Host "Backed up old directory to: $BackupPath" -ForegroundColor Yellow
        } else {
            Write-Host "Operation cancelled." -ForegroundColor Red
            exit 0
        }
    }
}

# 4. Create Directory Junction Link
try {
    New-Item -ItemType Junction -Path $TargetGeminiLinkPath -Value $SourceGeminiPath -ErrorAction Stop | Out-Null
    Write-Host "[OK] Junction Link created successfully!" -ForegroundColor Green
} catch {
    Write-Error "Error creating Junction Link: $_"
    exit 1
}

# 5. Update target project's .gitignore
$GitignorePath = Join-Path $TargetProjectFullPath ".gitignore"
try {
    if (Test-Path $GitignorePath) {
        $gitignoreContent = Get-Content $GitignorePath
        if ($gitignoreContent -notcontains ".gemini") {
            Add-Content $GitignorePath "`n# Gemini Agent Shared Configuration`n.gemini"
            Write-Host "[OK] Added '.gemini' to target project's .gitignore." -ForegroundColor Green
        } else {
            Write-Host "'.gemini' is already in target project's .gitignore." -ForegroundColor Gray
        }
    } else {
        New-Item -ItemType File -Path $GitignorePath -Value "# Gemini Agent Shared Configuration`n.gemini" | Out-Null
        Write-Host "[OK] Created .gitignore and added '.gemini' to target project." -ForegroundColor Green
    }
} catch {
    Write-Warning "Failed to update .gitignore: $_"
}

Write-Host "=== COMPLETED ===" -ForegroundColor Green

