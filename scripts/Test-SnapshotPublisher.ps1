$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$build=Join-Path $repoRoot 'build\tests'
New-Item -ItemType Directory -Force -Path $build | Out-Null
$compiler=Join-Path $env:SystemRoot 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$test=Join-Path $build 'TestSnapshotPublisher.exe'
& $compiler /nologo /target:exe ('/out:'+$test) (Join-Path $repoRoot 'computer\sensors\SnapshotPublisher.cs') (Join-Path $repoRoot 'tests\test_snapshot_publisher.cs')
if($LASTEXITCODE -ne 0){throw 'Snapshot publisher test build failed.'}
& $test $build
if($LASTEXITCODE -ne 0){throw 'Snapshot publisher regression tests failed.'}
