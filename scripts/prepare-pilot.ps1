param(
    [Parameter(Mandatory)][string]$PilotHost,
    [Parameter(Mandatory)][string]$CertificatePath,
    [string]$OutputPath = "release\pilot",
    [string]$DataProtectionKeyPath = "$env:ProgramData\WorkControl\keys"
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$certificate = (Resolve-Path -LiteralPath $CertificatePath).Path
$parsedIp = $null
if ([IPAddress]::TryParse($PilotHost, [ref]$parsedIp) -or [Uri]::CheckHostName($PilotHost) -ne [UriHostNameType]::Dns) { throw "PilotHost must be an approved DNS name, not an IP address." }
if ([IO.Path]::GetExtension($certificate) -notin '.pfx', '.p12') { throw "A PFX/P12 server certificate is required." }
if (-not [IO.Path]::IsPathRooted($DataProtectionKeyPath)) { throw "DataProtectionKeyPath must be an absolute persistent path outside the repository." }
$repoRoot = [IO.Path]::GetFullPath($repo).TrimEnd([IO.Path]::DirectorySeparatorChar)
$repoPrefix = $repoRoot + [IO.Path]::DirectorySeparatorChar
$keyPath = [IO.Path]::GetFullPath($DataProtectionKeyPath)
if ($keyPath.TrimEnd([IO.Path]::DirectorySeparatorChar).Equals($repoRoot, [StringComparison]::OrdinalIgnoreCase) -or $keyPath.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "DataProtectionKeyPath must remain outside the repository." }
if ($certificate.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "The certificate must remain outside the repository." }
$target = [IO.Path]::GetFullPath((Join-Path $repo $OutputPath))
if (-not $target.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "OutputPath must remain inside the repository." }

& npm --prefix (Join-Path $repo "src\web") ci --no-audit --no-fund
if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }
& npm --prefix (Join-Path $repo "src\web") run build
if ($LASTEXITCODE -ne 0) { throw "Web build failed." }
& dotnet publish (Join-Path $repo "src\server\WorkControl.Api\WorkControl.Api.csproj") -c Release -o (Join-Path $target "server")
if ($LASTEXITCODE -ne 0) { throw "API publish failed." }
New-Item -ItemType Directory -Force (Join-Path $target "server\wwwroot") | Out-Null
Copy-Item -Path (Join-Path $repo "src\web\dist\*") -Destination (Join-Path $target "server\wwwroot") -Recurse -Force
& dotnet publish (Join-Path $repo "src\agent\WorkControl.Agent\WorkControl.Agent.csproj") -c Release --self-contained false -p:UseAppHost=false -o (Join-Path $target "agent")
if ($LASTEXITCODE -ne 0) { throw "Agent publish failed." }
Copy-Item -LiteralPath (Join-Path $repo "src\agent\WorkControl.Agent\employee.bat") -Destination (Join-Path $target "agent\employee.bat") -Force

Write-Host "Pilot package prepared: $target"
Write-Host "The certificate and password were not copied. No firewall rule was changed."
Write-Host "Start only after approval documented in docs\NETWORK_PILOT.md."
