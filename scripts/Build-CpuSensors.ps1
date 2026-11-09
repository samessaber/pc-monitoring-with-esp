param(
 [Parameter(Mandatory=$true)][string]$LibreHardwareMonitorDir,
 [string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$sourceDirectory=Join-Path $repoRoot 'computer\sensors'
if(-not $OutputDirectory){$OutputDirectory=$sourceDirectory}
$libraryDirectory=(Resolve-Path -LiteralPath $LibreHardwareMonitorDir).Path
$library=Join-Path $libraryDirectory 'LibreHardwareMonitorLib.dll'
if(-not (Test-Path -LiteralPath $library)){throw 'Extract the official LibreHardwareMonitor 0.9.6 Windows release first.'}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output=(Resolve-Path -LiteralPath $OutputDirectory).Path
foreach($dependency in Get-ChildItem -LiteralPath $libraryDirectory -Filter '*.dll'){
 $destination=Join-Path $output $dependency.Name
 if($dependency.FullName -ne $destination){Copy-Item -LiteralPath $dependency.FullName -Destination $destination -Force}
}
$compiler=Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not (Test-Path -LiteralPath $compiler)){throw '.NET Framework 4.x C# compiler not found.'}
# The scheduled reader must not own a console that can be closed accidentally.
& $compiler /nologo /target:winexe /platform:x64 ('/reference:'+ $library) /reference:System.Web.Extensions.dll ('/out:'+(Join-Path $output 'CpuSensors.exe')) (Join-Path $sourceDirectory 'CpuSensors.cs') (Join-Path $sourceDirectory 'PowerSelection.cs') (Join-Path $sourceDirectory 'SensorFileSecurity.cs') (Join-Path $sourceDirectory 'SnapshotPublisher.cs')
if($LASTEXITCODE -ne 0){throw 'CPU sensor build failed.'}
Write-Output ('CPU sensor built in '+$output)
