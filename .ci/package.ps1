mkdir ${env:buildPath}\.dist | Out-Null

Set-Location "${env:buildPath}\SeventhHeavenUI\bin\Release\net8.0-windows7.0"
7z a "${env:buildPath}\.dist\${env:_RELEASE_NAME}-${env:_RELEASE_VERSION}_${env:_RELEASE_CONFIGURATION}.zip" ".\*"
