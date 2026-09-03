# AGENTS.md — Standing rules for SCLogMate

This file is the standing agreement for anyone working here — human or AI.
Read it before writing code.

## Project identity

| Key | Value |
|---|---|
| **Name** | SCLogMate |
| **Repo** | `gOOvER/SCLogMate` |
| **Framework** | .NET 10 / Avalonia UI (WinExe) |
| **Workspace** | `x:\Github Workspace\SCLogReader` |
| **Version** | `<Version>` in `SCLogMate.csproj` |
| **Language** | German UI, German/English log parsing |

NexusApp (`x:\Github Workspace\NexusApp`) and QuantumWake
(`x:\Github Workspace\QuantumWake`) are **read-only reference** projects in
this workspace. Use them for patterns and comparisons — never write to them.

logbackups (`j:\StarCitizen\LIVE\logbackups`) contains **original Star Citizen
log files**. Use them to test the parser against real data — **absolutely
read-only, never modify or delete any file in this directory**.

---

## After every change

### 1. Update CHANGELOG.md

Every code change **must** update `CHANGELOG.md` under the `[Unreleased]`
section in the same commit. Use the Keep a Changelog categories:
`Added`, `Changed`, `Fixed`, `Removed`, `Security`.

### 2. Test-build

After making changes, always build to verify. **Before building**, check
whether SCLogMate is already running — the build will fail if the exe is
locked:

```powershell
# Kill running instance if needed (locked exe blocks the build)
Stop-Process -Name SCLogMate -Force -ErrorAction SilentlyContinue

# Debug build
dotnet build -v q

# Publish build (single-file exe)
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
  -o publish
```

Do **not** start the app after building — Torsten runs it himself.

### 3. Git Commits & Pushes nur auf Aufforderung

- **Weder Commits noch Pushes eigenständig ausführen**, es sei denn, Torsten fordert ausdrücklich dazu auf ("commit", "push", "pushe", "sichern", etc.).
- Wenn Torsten dazu auffordert, direkt und minimal ausführen (ohne Umwege, überflüssige Abfragen oder Token-Verschwendung).

---

## OCR subsystem

The `Core/Ocr/` directory contains five services. Key invariants:

- **WalletCapture** uses cross-grab confirmation (same value 2× in one burst)
  and dual-read misread protection in `BestRead` (diverging passes → reject).
  Do not remove either safety layer.
- **OcrEngineService** serializes all OCR calls through `_ocrLock`. Every
  method must acquire and release it, including `Dispose()`.
- **RsOcrScanner** and **ContractScanner** use `AutoReset=false` timers with
  `ObjectDisposedException` guards on re-arm. `_busy` fields use `Interlocked`.
- **ScreenCapture** uses Win32 GDI. The cleanup order is:
  `SelectObject(old) → DeleteObject → DeleteDC → ReleaseDC`.

NexusApp's `Services/OcrService.cs` and `Services/WalletOcrService.cs` are the
reference implementations for preprocessing (scale, invert, contrast, padding).

## Build

- **Debug**: `dotnet build` — fast iteration
- **Release/Publish**: `dotnet publish -c Release -r win-x64 --self-contained true -o publish`
- **Full release**: `.\release.ps1` (builds, signs, tags, creates GitHub release)

The publish output is `publish\SCLogMate.exe` (single-file, self-contained).

## Debug Logging & Diagnostics

When debugging startup errors, crashes, or unhandled exceptions:
- Check the debug log immediately at:
  `%APPDATA%\SCLogMate\SCLogMate.debug.log` (PowerShell: `Get-Content "$env:APPDATA\SCLogMate\SCLogMate.debug.log" -Tail 50`)
- Fatal exceptions and startup errors are captured there automatically via `Core.Logger`.
- Always inspect this log directly without asking for confirmation.

## Style & Workflow

- Comments in German or English, both are fine.
- UI strings are German.
- Log parser supports both English and German game client strings.
- Work autonomously and directly — avoid unnecessary confirmations or asking for redundant approval. Just execute the plan and report the result.

