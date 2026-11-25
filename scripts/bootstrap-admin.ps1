param([string]$Email = "admin@workcontrol.local")
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$secure = Read-Host "Enter a new administrator password" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $env:WORKCONTROL_BOOTSTRAP_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    $env:WORKCONTROL_BOOTSTRAP_EMAIL = $Email
    $project = Join-Path $repo "src\server\WorkControl.Api\WorkControl.Api.csproj"
    & $dotnet run --project $project -- --bootstrap-admin
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    Remove-Item Env:WORKCONTROL_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:WORKCONTROL_BOOTSTRAP_EMAIL -ErrorAction SilentlyContinue
}
