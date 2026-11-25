param(
    [Parameter(Mandatory)][string]$PilotHost,
    [Parameter(Mandatory)][string]$CertificatePath,
    [string]$PackagePath = "release\pilot\server",
    [string]$DataProtectionKeyPath = "$env:ProgramData\WorkControl\keys",
    [int]$Port = 5443
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$repoRoot = [IO.Path]::GetFullPath($repo).TrimEnd([IO.Path]::DirectorySeparatorChar)
$repoPrefix = $repoRoot + [IO.Path]::DirectorySeparatorChar
$server = (Resolve-Path -LiteralPath (Join-Path $repo $PackagePath)).Path
$certificate = (Resolve-Path -LiteralPath $CertificatePath).Path
$parsedIp = $null
if ([IPAddress]::TryParse($PilotHost, [ref]$parsedIp) -or [Uri]::CheckHostName($PilotHost) -ne [UriHostNameType]::Dns) { throw "PilotHost must be a DNS name." }
if (-not $server.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "PackagePath must remain inside the repository." }
if ([IO.Path]::GetExtension($certificate) -notin '.pfx', '.p12') { throw "A PFX/P12 server certificate is required." }
if ($certificate.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "The certificate must remain outside the repository." }
if (-not [IO.Path]::IsPathRooted($DataProtectionKeyPath)) { throw "DataProtectionKeyPath must be an absolute persistent path outside the repository." }
$keyPath = [IO.Path]::GetFullPath($DataProtectionKeyPath)
if ($keyPath.TrimEnd([IO.Path]::DirectorySeparatorChar).Equals($repoRoot, [StringComparison]::OrdinalIgnoreCase) -or $keyPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "DataProtectionKeyPath must remain outside the repository." }
if (-not (Test-Path (Join-Path $server "wwwroot\index.html"))) { throw "The package does not contain the built web UI." }
$password = Read-Host "Enter the PFX password (it will not be saved)" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
try {
    $env:WorkControl__Mode = "Pilot"
    $env:Security__PilotHost = $PilotHost
    $env:Security__DataProtectionKeyPath = $keyPath
    $env:ASPNETCORE_URLS = "https://$PilotHost`:$Port"
    $env:ASPNETCORE_Kestrel__Certificates__Default__Path = $certificate
    $env:ASPNETCORE_Kestrel__Certificates__Default__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    & dotnet (Join-Path $server "WorkControl.Api.dll")
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    Remove-Item Env:ASPNETCORE_Kestrel__Certificates__Default__Password -ErrorAction SilentlyContinue
}
