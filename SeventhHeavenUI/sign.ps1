# Files to be signed
$files = '7th Heaven.exe',
	 'TurBoLog.exe',
	 '7thHeaven.Code.dll',
	 '7thWrapperLib.dll';
	 
#certificate Common Name
$cert = "Open Source Developer, MARTIN BARKER";

# get Operating system drive letter
$drive = (Get-WmiObject Win32_OperatingSystem).SystemDrive;

# setup defaults
$progFiles = $drive+"\Program Files";
$sdkArch = "x86";

# Check if this is a 64bit windows or 32 (only 64 bit will have (x86) varient of program files
$checkPath = $drive+"\Program Files (x86)\";
if(Test-Path -Path $checkPath){
	"OS Architecture: Detected 64bit Windows"
	$progFiles = $progFiles + " (x86)\";
	$sdkArch = "x64";
} else {
	"OS Architecture: Detected 32bit Windows"
	$progFiles = $progFiles + "\";
}

#Check for the Windows SDK intall
$checkPath = $progFiles+"Windows Kits\10\";
"Checking for windows 10 SDK";
if(Test-Path -Path $checkPath){
	$win10sdkPath = $progFiles+"Windows Kits\10\bin\";
	"SDK Path: "+$win10sdkPath;
} else {
	# not found end the script
	"[Error] Unable to find Windows 10 SDK, is Visual Studios 2019 or newer installed?";
	Break Script;
}

# get the latest SDK Version Path (ordered by name so latest will be last)
Get-ChildItem $win10sdkPath -Filter 10.* |
Foreach-Object{
	# check that this SDK version has signtool
	$checkPath = $_.FullName+"\"+$sdkArch+"\signtool.exe"
	if(Test-Path -Path $checkPath){
		#sign tool found add set this path as comptable
		$SDKVersionPath = $_.FullName;
	}
}

$SDKVersionPath = $SDKVersionPath+"\"+$sdkArch;
# Windows 10 signing system (signtool.exe from Windows 10 SDK)
$signPath = $SDKVersionPath+"\signtool.exe";
"Signtool Path: "+$SDKVersionPath+"\signtool.exe";

# create alias to the discovered path so we can invoke with arguments
new-alias signtool $signPath;

# basic aguments for sign tool
$appArgs = 'sign',
	'/fd',	
	'sha256',
	'/n';

# support adding a password for the certificate if it's required supplied from the args to this script
if($args[0] -ne $null){
	"Certificate Password detected"
	$passSupp = '/p', 
				$args[1];
	$appArgs = $appArgs + $passSupp;
}

$appArgs = $appArgs + $cert;

# add the files to be signed to the end of the args array
$appArgs = $appArgs + $files;

#run the signtool
signtool $appArgs;