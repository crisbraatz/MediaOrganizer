$ErrorActionPreference = "Stop"

$Repository = "crisbraatz/MediaOrganizer"
$InstallDir = if ($env:MEDIAORGANIZER_INSTALL_DIR) {
    $env:MEDIAORGANIZER_INSTALL_DIR
}
elseif ($env:LOCALAPPDATA) {
    Join-Path $env:LOCALAPPDATA "MediaOrganizer\bin"
}
else {
    Join-Path $HOME ".mediaorganizer\bin"
}

function Fail($Message) {
    Write-Error "MediaOrganizer install failed: $Message"
    exit 1
}

function Get-AssetName {
    $architecture = $env:PROCESSOR_ARCHITECTURE

    if ($architecture -in @("AMD64", "x86_64")) {
        return "mediaorganizer-win-x64.zip"
    }

    if ($architecture -eq "ARM64") {
        Write-Warning "Windows ARM64 is not published yet. Installing the x64 build, which may run through emulation."
        return "mediaorganizer-win-x64.zip"
    }

    Fail "Windows architecture '$architecture' is not supported yet."
}

function Add-ToUserPath($Directory) {
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")

    if ([string]::IsNullOrWhiteSpace($userPath)) {
        $paths = @()
    }
    else {
        $paths = $userPath -split ";"
    }

    if ($paths -notcontains $Directory) {
        $newPath = (@($paths) + $Directory | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ";"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Write-Host "Added MediaOrganizer to your user PATH."
    }

    if (($env:Path -split ";") -notcontains $Directory) {
        $env:Path = "$Directory;$env:Path"
    }
}

$Asset = Get-AssetName
$DownloadUrl = "https://github.com/$Repository/releases/latest/download/$Asset"
$TempDir = Join-Path ([IO.Path]::GetTempPath()) "mediaorganizer-install-$([Guid]::NewGuid().ToString("N"))"
$ZipPath = Join-Path $TempDir $Asset

Write-Host "Installing MediaOrganizer..."
Write-Host "Downloading $Asset"

try {
    New-Item -ItemType Directory -Path $TempDir -Force | Out-Null
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

    Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath -UseBasicParsing

    Get-ChildItem -Path $InstallDir -Filter "mediaorganizer*" -Force | Remove-Item -Recurse -Force
    Expand-Archive -Path $ZipPath -DestinationPath $InstallDir -Force
    Add-ToUserPath $InstallDir

    $ExecutablePath = Join-Path $InstallDir "mediaorganizer.exe"

    if (-not (Test-Path $ExecutablePath)) {
        Fail "the downloaded release did not contain mediaorganizer.exe."
    }

    Write-Host "MediaOrganizer installed at $ExecutablePath."
    Write-Host "Run it with: mediaorganizer"
}
catch {
    Fail "$($_.Exception.Message). Make sure a GitHub Release exists."
}
finally {
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force
    }
}
