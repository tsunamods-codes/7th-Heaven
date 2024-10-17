# Prepare for distribution
mkdir ${env:buildPath}\.dist | Out-Null

# Build installer
iscc ${env:buildPath}\.iss\installer.iss "/dMyAppVersion=${env:_BUILD_VERSION}" "/dMyAppPath=${env:buildPath}\AppUI" "/dMyAppRelease=${env:_RELEASE_CONFIGURATION}" "/O${env:buildPath}\.dist" "/F${env:_RELEASE_NAME}-${env:_RELEASE_VERSION}_${env:_RELEASE_CONFIGURATION}"

# Generate checksums
7z h -scrc* ${env:buildPath}\.dist\${env:_RELEASE_NAME}-${env:_RELEASE_VERSION}_${env:_RELEASE_CONFIGURATION}.exe > ${env:buildPath}\.dist\${env:_RELEASE_NAME}-${env:_RELEASE_VERSION}_${env:_RELEASE_CONFIGURATION}_checksums.txt
