if ($env:_BUILD_BRANCH -eq "refs/heads/master" -Or $env:_BUILD_BRANCH -eq "refs/tags/canary") {
  $env:_IS_BUILD_CANARY = "true"
  $env:_IS_GITHUB_RELEASE = "true"
}
elseif ($env:_BUILD_BRANCH -like "refs/tags/*") {
  $env:_BUILD_VERSION = $env:_BUILD_VERSION.Substring(0, $env:_BUILD_VERSION.LastIndexOf('.')) + ".0"
  $env:_IS_GITHUB_RELEASE = "true"
}
$env:_RELEASE_VERSION = "v${env:_BUILD_VERSION}"

$vcpkgRoot = "C:\vcpkg"
$vcpkgBaseline = [string](jq --arg baseline "builtin-baseline" -r '.[$baseline]' 7thWrapperLoader/vcpkg.json)
$vcpkgOriginUrl = &"git" -C $vcpkgRoot remote get-url origin
$vcpkgBranchName = &"git" -C $vcpkgRoot branch --show-current

Write-Output "--------------------------------------------------"
Write-Output "BUILD CONFIGURATION: $env:_RELEASE_CONFIGURATION"
Write-Output "RELEASE VERSION: $env:_RELEASE_VERSION"
Write-Output "VCPKG ORIGIN: $vcpkgOriginUrl"
Write-Output "VCPKG BRANCH: $vcpkgBranchName"
Write-Output "VCPKG BASELINE: $vcpkgBaseline"
Write-Output "--------------------------------------------------"

Write-Host "##vso[task.setvariable variable=_BUILD_VERSION;]${env:_BUILD_VERSION}"
Write-Host "##vso[task.setvariable variable=_RELEASE_VERSION;]${env:_RELEASE_VERSION}"
Write-Host "##vso[task.setvariable variable=_IS_BUILD_CANARY;]${env:_IS_BUILD_CANARY}"
Write-Host "##vso[task.setvariable variable=_IS_GITHUB_RELEASE;]${env:_IS_GITHUB_RELEASE}"

git -C $vcpkgRoot pull --all
git -C $vcpkgRoot checkout $vcpkgBaseline
git -C $vcpkgRoot clean -fxd

cmd.exe /c "call $vcpkgRoot\bootstrap-vcpkg.bat"

vcpkg integrate install
