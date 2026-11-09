# Validation scope

Source-branch checks completed on Windows:

- 26 Python unit tests and the C# package-power selection tests passed.
- Sprite header and GIF previews regenerated from the committed source sheets;
  all source hashes in the asset manifest matched.
- `Build-CpuSensors.ps1` and `Build-Agent.ps1` rebuilt the CPU reader and agent
  using Python 3.12.14 and PyInstaller 6.22.3. The packaged agent reported
  version 5, valid assets, and real Intel CPU + NVIDIA GPU power.
- `Build-Firmware.ps1` compiled using the repository's explicit TFT setup:
  349764 bytes of program storage and 23908 bytes of global RAM.

The firmware carried into this branch is version 6. It was built with ESP32
core 3.3.12 and TFT_eSPI 2.5.43 and uploaded to a NodeMCU-32S/ILI9341 unit.
On-device `@SELFTEST` passed, rotation 3 was reported, real CPU + GPU readings
were checked, and VRAM formats for 8, 24, and 48 GiB capacities were verified.
The user confirmed the enlarged VRAM text and the complete ESP temperature
line, then requested Go as the permanent unit. The firmware now uses that unit.
Eevee sleep/awake/run modes were checked on the same hardware in firmware 5;
the animation code and frame data are unchanged in firmware 6.

The Windows full-load correction was validated on an Intel Core i7-8700K with
an NVIDIA RTX 3070. With normal-priority CPU sampling and file-read retries,
30 samples (29 at >=99% CPU load) remained valid with a maximum observed age
below three seconds. This is a measured test result, not a timing guarantee.

Agent version 4 adds both AMD `Package` and Intel `CPU Package` names. Python
and C# selection tests cover both names and invalid/missing readings. The
compiled agent was checked against the Intel sensor bridge. The local agent
on the Ryzen test PC was separately corrected and confirmed working by the
user; its exact later files have not been imported into this branch. This
branch therefore includes the independently tested portable AMD-name fix.

No machine-specific logs, full chat transcripts, live sensor JSON, serial
port assignments, Windows account paths, credentials, or executable bundles
are needed to reproduce the source build and none are included here.


The version 5 protected installation was applied to the Intel server and checked
from a non-elevated process: reader-directory creation and snapshot write access
were denied, while six consecutive fresh CPU samples remained readable. The
compiled ordinary agent resumed USB monitoring. A fresh build environment with
pip 26.2.2 and setuptools 84.2.1 reported no known dependency vulnerabilities.
See SECURITY_CHANGES.md for migration and scope.

The CPU reader is built as a Windows application (`/target:winexe`) so the
scheduled background process has no console window to close accidentally.
A console-style build was observed stopped with status 0xC000013A; its stale
CPU snapshot correctly caused the agent to report combined power as N/A.
Restarting the protected task restored fresh CPU readings and combined power.

Snapshot-publication regression tests (`scripts/Test-SnapshotPublisher.ps1`)
hold a real Windows file handle that denies delete sharing. They cover recovery
within the retry window, a longer lock that preserves the previous complete
snapshot, and successful publication after releasing that lock. Temporary-file
cleanup and propagation of permission/non-sharing errors are also checked.
The publisher always uses the protected snapshot creation routine and atomic
replacement; it never truncates an existing snapshot to work around a lock.
