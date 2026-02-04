param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath
)

Write-Host "Updating version in: $ProjectPath"

if (-not (Test-Path -Path $ProjectPath)) {
    Write-Host "ProjectPath does not exist, skipping."
    exit 0
}

$currentDate = Get-Date -Format 'yyyy.MM.dd.HHmm'

try {
    # Read the whole file as one string
    $original = Get-Content -Path $ProjectPath -Raw -ErrorAction Stop

    if ([string]::IsNullOrWhiteSpace($original)) {
        Write-Host "Project file is empty or whitespace, skipping."
        exit 0
    }

    # Make sure there is a <Version> element at all
    if ($original -notmatch '<Version>') {
        Write-Host "No <Version> element found, skipping."
        exit 0
    }

    # Regex: single-line mode (?s) so . matches newlines
    $pattern     = '(?s)<Version>.*?</Version>'
    $replacement = "<Version>$currentDate</Version>"

    # Replace only the first occurrence
    $updated = [regex]::Replace($original, $pattern, $replacement, 1)

    if ([string]::IsNullOrWhiteSpace($updated)) {
        Write-Host "Updated content is empty, aborting without writing."
        exit 0
    }

    # Simple sanity check: file should still look like a .csproj
    if ($updated.TrimStart() -notmatch '^<Project\b') {
        Write-Host "Updated content does not look like a .csproj, aborting."
        exit 0
    }

    # Backup before overwrite
    $backupPath = "$ProjectPath.bak"
    Copy-Item -Path $ProjectPath -Destination $backupPath -Force
    Write-Host "Backup created at: $backupPath"

    # Overwrite file
    Set-Content -Path $ProjectPath -Value $updated -Encoding UTF8

    Write-Host "Version updated to $currentDate"
    exit 0
}
catch {
    Write-Host "Error updating version: $($_.Exception.Message)"
    # Don't break the build because of versioning
    exit 0
}
