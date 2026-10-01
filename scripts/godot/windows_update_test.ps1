# On a Windows runner: installs the older test version with its Setup.exe (as a person would),
# lets it update itself from a local feed holding this version, and checks the update arrived and
# saved data survived. Both versions are packed on Linux by the export job.
param(
    [Parameter(Mandatory)] [string] $OldDir,   # older version: <PackId>-win-Setup.exe
    [Parameter(Mandatory)] [string] $NewDir,   # this version: the win/ feed folder
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $PackId
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path "$PSScriptRoot/../.."
$work = Join-Path $env:RUNNER_TEMP 'godot-win'
New-Item -ItemType Directory -Force $work | Out-Null

$setup = Join-Path $OldDir "$PackId-win-Setup.exe"
Write-Host "Installing $setup"
$install = Start-Process -FilePath $setup -ArgumentList '--silent' -PassThru -Wait
Write-Host "Setup exited with $($install.ExitCode)"
Start-Sleep -Seconds 5
# Setup may open the game after installing; close it so the test starts cleanly.
Get-Process -Name 'Bip Island' -ErrorAction SilentlyContinue | Stop-Process -Force
$exe = Join-Path $env:LOCALAPPDATA "$PackId\current\Bip Island.exe"
if (-not (Test-Path $exe)) {
    Get-ChildItem -Recurse (Join-Path $env:LOCALAPPDATA $PackId) -ErrorAction SilentlyContinue | Select-Object -First 40 | ForEach-Object { $_.FullName }
    throw "The game isn't installed at $exe"
}

$server = Start-Process -FilePath python -ArgumentList '-m', 'http.server', '8765', '--directory', $NewDir -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 3
$report = Join-Path $work 'report.json'
Remove-Item $report -ErrorAction SilentlyContinue
try {
    Start-Process -FilePath $exe -ArgumentList '--headless', '--', '--bip-update-test', 'http://127.0.0.1:8765/', "`"$report`""
    for ($i = 0; $i -lt 90 -and -not ((Test-Path $report) -and (Get-Item $report).Length -gt 0)); $i++) { Start-Sleep -Seconds 2 }
    if (-not (Test-Path $report)) {
        Write-Host "::error::The installed game didn't write its report after updating."
        Get-ChildItem -Recurse (Join-Path $env:LOCALAPPDATA $PackId) -Filter '*.log' -ErrorAction SilentlyContinue |
            ForEach-Object { Write-Host "== $($_.FullName)"; Get-Content $_.FullName -Tail 60 }
        exit 1
    }
    Start-Sleep -Seconds 2
} finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}
python "$root/scripts/godot/check_report.py" $report --version $Version --expect-save
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
