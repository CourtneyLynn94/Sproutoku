# Builds the signed release APK.
# The keystore and its password never go in the repo: SPROUTOKU_KEYSTORE says where the keystore is,
# and the password is typed in here each time and forgotten when the build ends.
$ErrorActionPreference = 'Stop'

if (-not $env:SPROUTOKU_KEYSTORE -or -not (Test-Path $env:SPROUTOKU_KEYSTORE)) {
    throw "SPROUTOKU_KEYSTORE isn't set to your keystore's full path. See 'Signing releases' in the README."
}

$secure = Read-Host "Keystore password" -AsSecureString
$env:SPROUTOKU_KEY_PASSWORD = [System.Net.NetworkCredential]::new('', $secure).Password

try {
    dotnet publish src/Sproutoku.App -f net10.0-android -c Release -p:AndroidPackageFormat=apk
    if ($LASTEXITCODE -ne 0) {
        throw "The build failed (exit code $LASTEXITCODE)."
    }

    Write-Host "Signed APK: src/Sproutoku.App/bin/Release/net10.0-android/publish/com.courtneylynn94.sproutoku-Signed.apk"
}
finally {
    # Forget the password even if the build failed.
    Remove-Item Env:SPROUTOKU_KEY_PASSWORD -ErrorAction SilentlyContinue
}