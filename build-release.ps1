<#
.SYNOPSIS
    Builds, signs, packages, and prepares DigitalClock for release.

.DESCRIPTION
    This script publishes a framework-dependent, single-file Windows executable (win-x64),
    signs it with Authenticode (using a provided PFX or auto-generating a self-signed
    certificate CN=DigitalClock (Bimal)), packages it into a compact ZIP archive along with
    the public certificate (.cer), and computes SHA-256 checksums.
    Optionally, it can create and push a git release tag to trigger the GitHub Actions workflow.

.PARAMETER Version
    Optional version override. If omitted, reads the <Version> tag from DigitalClock.csproj.

.PARAMETER Configuration
    Build configuration (default: "Release").

.PARAMETER Runtime
    Target runtime identifier (default: "win-x64").

.PARAMETER OutputDir
    Directory where release artifacts will be placed (default: "artifacts").

.PARAMETER CertPath
    Path to an existing .pfx code-signing certificate file.

.PARAMETER CertPassword
    Password for the specified PFX certificate file.

.PARAMETER SkipSign
    Skip the code signing step.

.PARAMETER CreateTag
    Create a local git tag (e.g., v2.6.0).

.PARAMETER PushTag
    Push the git tag to origin remote (triggers GitHub Actions release workflow).

.EXAMPLE
    .\build-release.ps1
    Builds and packages DigitalClock using the version in DigitalClock.csproj.

.EXAMPLE
    .\build-release.ps1 -CreateTag -PushTag
    Builds, packages, and pushes a release tag to GitHub to trigger GitHub Actions release.
#>

[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "artifacts",
    [string]$CertPath,
    [string]$CertPassword,
    [switch]$SkipSign,
    [switch]$CreateTag,
    [switch]$PushTag
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $ScriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       DigitalClock Release Build & Package Script        " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Determine Version
$csprojPath = Join-Path $ScriptDir "DigitalClock.csproj"
if (-not (Test-Path $csprojPath)) {
    throw "DigitalClock.csproj not found at $csprojPath"
}

if (-not $Version) {
    [xml]$csprojXml = Get-Content $csprojPath
    $Version = ($csprojXml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)
    if (-not $Version) {
        $Version = "2.6.0"
    }
}

$tagName = "v$Version"
Write-Host "[1/5] Target Version: $Version (Tag: $tagName)" -ForegroundColor Green

# 2. Setup Directories
$publishDir = Join-Path $ScriptDir "bin\$Configuration\publish"
$artifactsPath = Join-Path $ScriptDir $OutputDir
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (-not (Test-Path $artifactsPath)) { New-Item -ItemType Directory -Force -Path $artifactsPath | Out-Null }

# 3. Publish Framework-Dependent Single-File Executable
Write-Host "[2/5] Publishing framework-dependent single-file executable ($Runtime)..." -ForegroundColor Green
dotnet publish "$csprojPath" `
    -c "$Configuration" `
    -r "$Runtime" `
    --no-self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o "$publishDir"

$exePath = Join-Path $publishDir "DigitalClock.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish failed: DigitalClock.exe was not created in $publishDir."
}
$exeSizeKb = [math]::Round(((Get-Item $exePath).Length / 1KB), 1)
Write-Host "Published: $exePath ($exeSizeKb KB)" -ForegroundColor Gray

# 4. Sign Binary & Export Public Certificate
$publicCertPath = Join-Path $artifactsPath "DigitalClock-PublicCert.cer"

if ($SkipSign) {
    Write-Host "[3/5] Code signing skipped by user flag." -ForegroundColor Yellow
} else {
    Write-Host "[3/5] Code signing DigitalClock.exe..." -ForegroundColor Green
    $cert = $null
    $isTempCert = $false

    if ($CertPath -and (Test-Path $CertPath)) {
        Write-Host "Using provided certificate: $CertPath" -ForegroundColor Gray
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($CertPath, $CertPassword)
    } elseif ($env:WINDOWS_CERT_BASE64) {
        Write-Host "Using certificate from WINDOWS_CERT_BASE64 environment variable..." -ForegroundColor Gray
        $certBytes = [System.Convert]::FromBase64String($env:WINDOWS_CERT_BASE64)
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(
            $certBytes,
            $env:WINDOWS_CERT_PASSWORD,
            [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
        )
    } else {
        Write-Host "Generating self-signed certificate: 'CN=Digital Clock (Bimal)'..." -ForegroundColor Gray
        $cert = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject "CN=Digital Clock (Bimal)" `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -NotAfter (Get-Date).AddYears(10) `
            -KeyExportPolicy Exportable
        $isTempCert = $true
    }

    # Export Public Certificate (.cer)
    Write-Host "Exporting public certificate to $publicCertPath..." -ForegroundColor Gray
    Export-Certificate -Cert $cert -FilePath $publicCertPath | Out-Null

    # Sign using Set-AuthenticodeSignature with timestamp server
    $timestampUrls = @(
        "http://timestamp.digicert.com",
        "http://timestamp.sectigo.com",
        "http://tsa.starfieldtech.com"
    )

    $signed = $false
    foreach ($tsUrl in $timestampUrls) {
        try {
            $sig = Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert -TimestampServer $tsUrl -HashAlgorithm SHA256
            if ($sig.SignerCertificate -ne $null -and $sig.TimeStamperCertificate -ne $null) {
                Write-Host "Signed successfully with timestamp server: $tsUrl" -ForegroundColor Gray
                $signed = $true
                break
            }
        } catch {
            Write-Verbose "Timestamp failed for $tsUrl : $_"
        }
    }

    if (-not $signed) {
        Write-Warning "Remote timestamp server unavailable or timed out; signing without external timestamp..."
        $sig = Set-AuthenticodeSignature -FilePath $exePath -Certificate $cert -HashAlgorithm SHA256
        if ($sig.SignerCertificate -ne $null) {
            Write-Host "Signed successfully without timestamp." -ForegroundColor Gray
        }
    }

    # Verify signature
    $sigCheck = Get-AuthenticodeSignature -FilePath $exePath
    Write-Host "Signature Status: $($sigCheck.Status) ($($sigCheck.SignerCertificate.Subject))" -ForegroundColor Gray

    # Clean up private key from local store if self-generated
    if ($isTempCert -and $cert.Thumbprint) {
        Get-ChildItem "Cert:\CurrentUser\My\$($cert.Thumbprint)" -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    }
}

# 5. Packaging into ZIP
Write-Host "[4/5] Packaging release ZIP archive..." -ForegroundColor Green
$zipName = "DigitalClock-v$Version-$Runtime.zip"
$zipPath = Join-Path $artifactsPath $zipName
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

# Stage only DigitalClock.exe and public certificate (per user request)
$stagingDir = Join-Path $ScriptDir "bin\release_staging"
if (Test-Path $stagingDir) { Remove-Item -Recurse -Force $stagingDir }
New-Item -ItemType Directory -Force -Path $stagingDir | Out-Null

Copy-Item $exePath -Destination $stagingDir\
if (Test-Path $publicCertPath) {
    Copy-Item $publicCertPath -Destination $stagingDir\
}

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -CompressionLevel Optimal -Force
Remove-Item -Recurse -Force $stagingDir

$zipSizeKb = [math]::Round(((Get-Item $zipPath).Length / 1KB), 1)
Write-Host "Created Archive: $zipPath ($zipSizeKb KB)" -ForegroundColor Gray

# 6. Checksum Calculation
Write-Host "[5/5] Generating SHA-256 checksums..." -ForegroundColor Green
$checksumFile = Join-Path $artifactsPath "SHA256SUMS.txt"
if (Test-Path $checksumFile) { Remove-Item -Force $checksumFile }

$items = Get-ChildItem -Path $artifactsPath -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" }
foreach ($item in $items) {
    $hash = (Get-FileHash -Path $item.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($item.Name)" | Out-File -FilePath $checksumFile -Append -Encoding ascii
}

# Clean temporary publish folder
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }

# Summary Report
Write-Host ""
Write-Host "=================== RELEASE ARTIFACTS ===================" -ForegroundColor Cyan
Get-ChildItem -Path $artifactsPath -File | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 1)
    $sha = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.Substring(0, 16) + "..."
    Write-Host ("{0,-35} | {1,8} KB | SHA256: {2}" -f $_.Name, $size, $sha) -ForegroundColor White
}
Write-Host "==========================================================" -ForegroundColor Cyan

# 7. Git Tag Handling
if ($CreateTag -or $PushTag) {
    Write-Host ""
    Write-Host "Checking git tag $tagName..." -ForegroundColor Yellow
    $existingTag = git tag -l $tagName
    if (-not $existingTag) {
        Write-Host "Creating git tag $tagName..." -ForegroundColor Green
        git tag -a $tagName -m "DigitalClock release $tagName"
    } else {
        Write-Host "Tag $tagName already exists." -ForegroundColor Gray
    }

    if ($PushTag) {
        Write-Host "Pushing git tag $tagName to origin..." -ForegroundColor Green
        git push origin $tagName
        Write-Host "Tag pushed successfully! GitHub Actions release workflow has been triggered." -ForegroundColor Cyan
    }
} else {
    Write-Host ""
    Write-Host "Next Steps to Publish to GitHub:" -ForegroundColor Yellow
    Write-Host "  1. Verify the artifacts in '$OutputDir\'."
    Write-Host "  2. Create and push tag to trigger GitHub Actions release:"
    Write-Host "       git tag $tagName" -ForegroundColor White
    Write-Host "       git push origin $tagName" -ForegroundColor White
    Write-Host "  Or re-run this script with: .\build-release.ps1 -CreateTag -PushTag" -ForegroundColor White
}
