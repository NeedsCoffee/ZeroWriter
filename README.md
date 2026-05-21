# Zerowriter

Zerowriter is a Windows console utility that fills the free space on a selected volume with zero bytes.

It is similar in intent to `cipher /W`, but intentionally simpler:

- it only writes zeros
- it works on a single target volume at a time
- it accepts a drive letter as input
- it is published as small standalone Windows executables using NativeAOT

## What It Does

Zerowriter creates temporary wipe files on the target volume and writes zero bytes into them until the volume runs out of free space. When the operation ends or is cancelled, it removes the temporary files and workspace.

The tool automatically adapts to `FAT16` and `FAT32` volumes by splitting the wipe data across multiple temporary files so it does not hit filesystem file-size ceilings.

## Features

- Zero-fills free space on a selected Windows volume
- Supports `FAT16`, `FAT32`, `NTFS`, and other standard Windows filesystems
- Automatically splits wipe files on filesystems with known per-file limits
- Optional runtime cap for temporary file size
- Live text progress bar
- Rolling speed and ETA display
- `Ctrl+C` cancellation with cleanup
- Small standalone NativeAOT builds for:
  - `win-x86`
  - `win-x64`
  - `win-arm64`

## Usage

Command syntax:

```powershell
zerowriter <drive-letter> [-m|--max-file-size <size>]
```

Examples:

```powershell
zerowriter E:
zerowriter F: --max-file-size 2g
zerowriter G -m 4095m
```

Accepted drive-letter forms:

- `E`
- `E:`

Accepted size suffixes for `--max-file-size`:

- `B`
- `K`, `KB`
- `KiB`
- `M`, `MB`
- `MiB`
- `G`, `GB`
- `GiB`
- `TiB`

Examples:

- `1048576`
- `512m`
- `4gb`

## Progress Display

During a run, Zerowriter shows a live in-place status line with:

- percentage complete
- bytes consumed versus initial free space
- free space remaining
- write speed
- ETA

Speed and ETA are intentionally held back until the tool has enough real data to avoid noisy first-pass numbers.

## Expected Completion Behavior

Zerowriter stops when the volume can no longer accept more zero-filled data.

That means an internal "disk full" condition is expected at the end of a successful run. The tool treats that as normal completion and should finish with the standard completion message rather than surfacing it as an error.

## FAT Filesystem Behavior

`FAT16` and `FAT32` volumes have per-file size limits. Zerowriter detects these automatically and applies a safe maximum temporary file size just below the relevant limit.

Current automatic caps:

- `FAT` / `FAT16`: just below 2 GiB
- `FAT32`: just below 4 GiB

If you provide `--max-file-size` on a capped filesystem:

- values below the safe limit are used
- values above the safe limit are clamped down automatically

On filesystems without a known cap, no temporary file-size cap is applied unless you explicitly set one.

## Cancellation And Cleanup

If you press `Ctrl+C`, Zerowriter cancels the wipe loop and performs cleanup before exiting.

The tool also makes a best-effort cleanup attempt on process exit and unhandled exceptions.

Cleanup behavior includes:

- deleting the temporary wipe files
- deleting the temporary workspace directory
- retry-safe cleanup logic if a file handle briefly blocks deletion

## Release Builds

NativeAOT release artifacts are written into clean architecture-specific folders:

- `artifacts/win-x86/zerowriter.exe`
- `artifacts/win-x64/zerowriter.exe`
- `artifacts/win-arm64/zerowriter.exe`

Build a specific release artifact with:

```powershell
dotnet publish src/zerowriter/zerowriter.csproj -c Release -r win-x64
dotnet publish src/zerowriter/zerowriter.csproj -c Release -r win-x86
dotnet publish src/zerowriter/zerowriter.csproj -c Release -r win-arm64
```

Development builds still work normally:

```powershell
dotnet build zerowriter.sln
```

## Requirements

To build from source, you need:

- a current `.NET 10` SDK
- Windows
- the Visual C++ / platform toolchain needed for NativeAOT publishes

End users running the published `.exe` files do not need the .NET runtime installed separately.

## Project Layout

- `zerowriter/`
- `src/zerowriter/`
  - application source
- `tests/zerowriter.tests/`
  - automated tests
- `artifacts/`
  - final published executables
- `docs/technical-notes.md`
  - implementation notes

## Limitations

- Windows only
- Operates on a whole volume's free space, not an individual file or directory
- Zero-fill only; no multi-pass overwrite modes
- Progress, speed, and ETA are based on live free-space observations and can shift if other activity changes the volume during the run

## Technical Notes

Short implementation notes are available in [docs/technical-notes.md](docs/technical-notes.md).

## License

MIT. See [LICENSE](LICENSE).
