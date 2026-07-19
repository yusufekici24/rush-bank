$ErrorActionPreference = "Stop"

$projectPath = Resolve-Path "$PSScriptRoot\.."
$unityExe = "C:\Program Files\Unity 6000.5.4f1\Editor\Unity.exe"
$fallbackCmd = "C:\Windows\SysWOW64\cmd.exe"

if (-not (Test-Path -LiteralPath $unityExe)) {
    throw "Unity Editor not found at: $unityExe"
}

if (Test-Path -LiteralPath $fallbackCmd) {
    $env:ComSpec = $fallbackCmd
    if (($env:Path -split ";") -notcontains "C:\Windows\SysWOW64") {
        $env:Path = "C:\Windows\SysWOW64;$env:Path"
    }
}

& $unityExe -projectPath $projectPath
