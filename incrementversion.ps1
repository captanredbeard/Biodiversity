param (
    [string]$projectDir = "."
)

$resolvedProjDir = (Resolve-Path $projectDir).ProviderPath

# Find all modinfo.json files recursively
$modInfoFiles = @(Get-ChildItem -Path $resolvedProjDir -Recurse -Filter "modinfo.json" |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.git|\.vs|\.vscode)[\\/]' })

if ($modInfoFiles.Count -eq 0) {
    Write-Host "No modinfo.json found in $projectDir or its subdirectories, skipping version increment." -ForegroundColor Yellow
    Read-Host "Press Enter to continue"
    return
}

foreach ($modInfoFile in $modInfoFiles) {
    $modDir = $modInfoFile.DirectoryName
    $modFolderName = Split-Path $modDir -Leaf
    $modInfoPath = $modInfoFile.FullName
    $modIconPath = Join-Path $modDir "modicon.png"
    $hashPath = Join-Path $modDir ".versionhash"

    # Gather all files for this specific mod folder
    $files = Get-ChildItem -Path $modDir -Recurse -File |
        Where-Object {
            $_.Name -ne 'modinfo.json' -and 
            $_.Name -ne '.versionhash' -and 
            $_.Name -notmatch '\.user$' -and
            $_.FullName -notmatch '[\\/](bin|obj|\.git|\.vs|\.vscode)[\\/]'
        } | Sort-Object FullName
    
    # Compute a state hash on those files for this mod
    $currentState = $(foreach ($file in $files) {
        $relPath = $file.FullName.Substring($modDir.Length).TrimStart('\','/')
        $hash = (Get-FileHash -Path $file.FullName -Algorithm MD5).Hash
        "${relPath}:$hash"
    }) -join "`n"

    # Check against previous state for this mod
    $previousState = ""
    if (Test-Path $hashPath) {
        $previousState = (Get-Content -Raw -Path $hashPath).Trim().Replace("`r`n", "`n")
    }
    if ($currentState -eq $previousState) {
        Write-Host "No files changed in $modFolderName, skipping." -ForegroundColor Gray
        continue
    }

    $content = Get-Content -Raw -Path $modInfoPath
    
    if ($content -match '"version"\s*:\s*"(\d+)\.(\d+)\.(\d+)"') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $patch = [int]$matches[3]
        
        $newPatch = $patch + 1
        $newVersion = "$major.$minor.$newPatch"
        
        $content = $content -replace '"version"\s*:\s*"\d+\.\d+\.\d+"', "`"version`": `"$newVersion`""
        
        Set-Content -Path $modInfoPath -Value $content -NoNewline
        Set-Content -Path $hashPath -Value $currentState -NoNewline
        
        Write-Host "Updated $($modInfoPath) to version $newVersion" -ForegroundColor Cyan
        
        $debugModPath = Join-Path $resolvedProjDir "bin/Debug/Mods/$modFolderName/"
        if (Test-Path $debugModPath) {
            # Sync modinfo.json
            Copy-Item -Path $modInfoPath -Destination (Join-Path $debugModPath "modinfo.json") -Force
            Write-Host "  -> Synced modinfo.json to debug folder" -ForegroundColor Cyan

                # Sync modicon.png (if it exists)
                if (Test-Path $modIconPath) {
                    Copy-Item -Path $modIconPath -Destination (Join-Path $debugModPath "modicon.png") -Force
                    Write-Host "  -> Synced modicon.png to debug folder" -ForegroundColor Cyan
                }
        } else {
            Write-Host "  (Debug folder $debugModPath for $modFolderName not found, skipping sync)" -ForegroundColor Gray
        }
    } else {
        Write-Host "Warning: Could not find version string in $modFolderName/modinfo.json" -ForegroundColor Yellow
    }        

}

# Read-Host "Press Enter to continue"
