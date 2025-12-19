# Utility scripts

## kill-target.sh
Stops any process currently locking a given file on Unix-like systems. By default the script lists the matching processes and asks before terminating them; pass `--force` to skip the prompt.

```
./scripts/kill-target.sh --force /path/to/RestoBooking.dll```

## kill-target.ps1
PowerShell equivalent for Windows. Useful when a build fails with messages such as the output DLL being locked by ".NET Host". Like the shell script, it prompts before killing unless `--Force` is provided.
```
powershell -ExecutionPolicy Bypass -File ./scripts/kill-target.ps1 .\bin\Debug\net10.0\RestoBooking.dll
powershell -ExecutionPolicy Bypass -File ./scripts/kill-target.ps1 .\bin\Debug\net10.0\RestoBooking.dll --Force
```

Each script resolves the file path, enumerates running processes, identifies any that have loaded the target file, and stops them so a rebuild can proceed.