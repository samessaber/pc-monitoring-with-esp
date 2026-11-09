# Windows agent and CPU reader

## Run from source

Install Python 3.12 x64, the CP210x USB driver for the ESP32 USB bridge, NVIDIA
drivers with `nvidia-smi`, and [PawnIO](https://pawnio.eu/) for CPU sensor access.
Install PawnIO using its normal signed installer. The CPU reader needs elevated
hardware access; the tray application runs as the normal interactive user.

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install --upgrade "pip>=26.2"
.\.venv\Scripts\python.exe -m pip install --upgrade --only-binary=:all: -r computer/requirements-build.txt
```

Notifications use pystray’s native Windows implementation. Neither win10toast nor
a legacy setuptools/pkg_resources runtime dependency is required. The corrected
pystray minimum is 0.19.5. `computer/requirements-windows.lock.txt` records the
versions used for the security-checked Windows build; update and audit that
snapshot when rebuilding in the future.

Download and extract the official [LibreHardwareMonitor 0.9.6 Windows release](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/releases/tag/v0.9.6).
Then build the CPU reader with the directory containing its DLLs:

```powershell
.\scripts\Build-CpuSensors.ps1 -LibreHardwareMonitorDir C:\Tools\LibreHardwareMonitor
.\computer\Installer_capteur_CPU.cmd
.\.venv\Scripts\python.exe .\computer\eeveemonitoring.py
```

The installer requests Windows elevation and creates the interactive logon task
`ESP32Monitor-CPU-Temperature`. The elevated reader and its DLLs are copied into
`Program Files/EeveeMonitor/CpuSensors`, owned by Administrators, with write access
only for Administrators and SYSTEM. Measurements are stored in its protected
`Data/cpu-sensors.json` file. The ordinary tray application has read access only.
The executable path and working directory of the task are absolute, protected
paths; moving the portable app does not move the privileged task.

The installer rejects redirected installation paths and unsafe ownership/write
permissions. It migrates a recognized older CPU task, stopping its previous reader
before registering the protected action. Do not manually point the task back to
a user-writable folder. Updating the CPU reader requires running the installer
again and accepting Windows elevation. The task keeps normal CPU priority (4),
samples about every two seconds, and restarts on failure. Snapshot ownership and
ACLs are applied at file creation before atomic publication.


Use the tray menu to enable startup at login. The shortcut targets the current
source interpreter or packaged executable. USB reconnection is automatic.

## Build the portable Windows application

After building the CPU reader:

```powershell
.\scripts\Build-Agent.ps1 -Python .\.venv\Scripts\python.exe
```

The complete application is in `dist/EeveeMonitor`. Keep `_internal` and `sensors`
with the executable. On another PC, extract the entire directory, install the
USB/PawnIO prerequisites, run `Installer_capteur_CPU.cmd`, and start `EeveeMonitor.exe`.
A packaged application does not require Python or Arduino on the target PC.

For an existing installation, quit the tray application and stop the scheduled
CPU task before replacing its files. Keep the same installation path, replace
all application files, run the CPU installer again, and restart the application.
Avoid leaving a second old agent running from another directory.

## Diagnostics

```powershell
.\.venv\Scripts\python.exe .\computer\eeveemonitoring.py --diagnose
.\dist\EeveeMonitor\EeveeMonitor.exe --diagnose-file .\diagnostic-agent.json
```

Diagnostics include the agent version, CPU temperature/package watts, NVIDIA
readings, combined watts, USB bridge ports, assets, and logon-startup state.
They do not open the serial port. The CPU snapshot also lists raw power sensors.
An Intel package uses the name `CPU Package`; a Ryzen package uses `Package`.
Only a single valid package sensor is accepted. Core readings are not added again.

`ESP32_MONITOR_PORT` or `--port` selects an explicit port when auto-detection is
unsuitable. `ESP32_SENSOR_FILE` selects a different CPU snapshot file.
The default snapshot path is obtained from the Windows Program Files API; the
old JSON next to the portable executable is no longer used automatically.
NVIDIA tools are resolved only from the Windows system directory or the standard
NVIDIA NVSMI directory under Program Files, never the working directory or PATH.

CPU snapshots older than 10 seconds or more than two seconds in the future are
rejected. Read locks receive at most three 50 ms delays before returning unavailable.
Do not use old sensor JSON files as a live source.

The CPU writer retries Windows sharing/lock violations during atomic snapshot
publication (up to ten attempts, 50 ms apart). If the file remains locked, it
skips that publication and samples again on the next cycle instead of exiting.
The last complete snapshot is preserved; the agent still rejects it once stale.
Permission errors and other failures remain errors. Install an updated reader
with `Installer_capteur_CPU.cmd`; replacing only the portable payload is not enough.

The scheduled CPU reader runs without a console window. If CPU power and
temperature both become unavailable, check that `ESP32Monitor-CPU-Temperature`
is running in Task Scheduler. After updating an older console-style reader,
run `Installer_capteur_CPU.cmd` again to install the background build in the
protected directory; changing only the portable files does not replace it.

The agent writes a rotating `logs/monitor.log` beside itself. These files and
sensor snapshots are local runtime state and are excluded from Git.
