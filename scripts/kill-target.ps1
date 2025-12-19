param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$TargetPath,
    [string]$AssemblyName,
    [switch]$Force
)

# Stop any .NET process that locks the built assembly (e.g., after a debug run)
$resolvedPath = [System.IO.Path]::GetFullPath($TargetPath)

$processes = Get-Process -ErrorAction SilentlyContinue | ForEach-Object {
    $p = $_
    $mainMatches = $false
    try { if ($p.MainModule -and $p.MainModule.FileName -ieq $resolvedPath) { $mainMatches = $true } } catch { $mainMatches = $false }

    $modulesMatch = $false
    try {
        $mods = @()
        try { $mods = $p.Modules } catch { $mods = @() }
        $modulesMatch = ($mods | Where-Object { $_.FileName -ieq $resolvedPath }).Count -gt 0
    } catch { $modulesMatch = $false }

    $nameMatch = $false
    try { if ($AssemblyName -and $p.ProcessName -ieq $AssemblyName) { $nameMatch = $true } } catch { }

    if ($mainMatches -or $modulesMatch -or $nameMatch) { $p }
}

if (-not $processes -or $processes.Count -eq 0) {
    Write-Host "No processes are locking $resolvedPath"
    exit 0
}

if (-not $Force) {
    Write-Host "Processes locking $($resolvedPath):" -ForegroundColor Yellow
    foreach ($process in $processes) { Write-Host " - $($process.ProcessName) (Id=$($process.Id))" }
    $confirmation = Read-Host "Kill these processes? [y/N]"
    if ($confirmation -notmatch '^(?i:y(es)?)$') { Write-Host "Aborting without killing processes."; exit 0 }
}

foreach ($process in $processes) {
    try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch { }
}

exit 0