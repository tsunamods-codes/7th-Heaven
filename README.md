![License](https://img.shields.io/github/license/tsunamods-codes/7th-Heaven) ![Latest Stable Downloads](https://img.shields.io/github/downloads/tsunamods-codes/7th-Heaven/latest/total?label=Latest%20Stable%20Downloads&sort=semver) ![Latest Canary Downloads](https://img.shields.io/github/downloads/tsunamods-codes/7th-Heaven/canary/total?label=Latest%20Canary%20Downloads) [![Build Status](https://dev.azure.com/julianxhokaxhiu/Github/_apis/build/status/tsunamods-codes.7th-Heaven?branchName=master)](https://dev.azure.com/julianxhokaxhiu/Github/_build/latest?definitionId=26&branchName=master)

<div align="center">
  <img src="https://github.com/tsunamods-codes/7th-Heaven/blob/master/.logo/tsunamods.png" alt="">
  <br><small>7thHeaven is now officially part of the <strong>Tsunamods</strong> initiative!</small>
</div>

# 7th Heaven

Mod manager for Final Fantasy VII PC.

## Introduction

This is a fork of the original [7th Heaven 2.x](https://github.com/unab0mb/7h) release, maintained now by the Tsunamods team.

## Download

- [Latest stable release](https://github.com/tsunamods-codes/7th-Heaven/releases/latest)
- [Latest canary release](https://github.com/tsunamods-codes/7th-Heaven/releases/tag/canary)

## Install

1. Download the latest release using one of the links above
2. Extract the .zip file to your preferred location e.g. C:\7th Heaven.
3. Run `7th Heaven.exe`

## Build

0. Download the the latest [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/) installer
1. Run the installer and import this [.vsconfig](.vsconfig) file in the installer to pick the required components to build this project
2. Once installed, open the file [`7thHeaven.sln`](7thHeaven.sln) in Visual Studio and click the build button

## Special thanks

The .NET 6 migration would not have been possibile without the help of these people. The order is purely Alphabetical.

These people are:

- [Benjamin Moir](https://github.com/DaZombieKiller):
  - For figuring out various .NET internals
  - For the Detours examples and logics to be used in C#
  - For the patience to guide through nitty gritty low level details

## License

See [LICENSE.txt](LICENSE.txt)
