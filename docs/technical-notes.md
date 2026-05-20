# Technical Notes

This document explains how Zerowriter works internally at a high level.

## Overview

Zerowriter is a small Windows console app that zero-fills free space on a selected volume. It accepts a drive letter, creates temporary wipe files on that volume, writes zero bytes until no more free space remains, then cleans up.

The app is designed around a few focused units:

- `AppOptions`
  - parses CLI arguments
- `VolumePathParser`
  - validates and normalizes drive-letter input
- `VolumeWritePolicy`
  - applies filesystem-specific file-size rules
- `VolumeWipeOperation`
  - owns the temporary workspace and wipe file naming
- `ZeroFillEngine`
  - performs chunked zero writes and handles expected disk-full behavior
- `VolumeZeroWriter`
  - coordinates the end-to-end wipe process and progress reporting
- `ProgressReporter`
  - computes displayed progress, speed, and ETA
- `ShutdownCoordinator`
  - handles cancellation and shutdown cleanup

## Free-Space Wipe Strategy

The core wipe strategy is file-based rather than raw-disk-based.

Zerowriter:

1. normalizes the target drive letter
2. inspects the volume format
3. creates a temporary workspace on the target volume
4. creates one or more wipe files in that workspace
5. fills them with zero bytes until the volume is full
6. deletes the workspace on completion or cancellation

This keeps the implementation simpler and avoids raw volume APIs.

## FAT Filesystem File Splitting

`FAT16` and `FAT32` have maximum single-file sizes. To avoid failing at those boundaries, Zerowriter automatically enables a safe per-file cap just below each filesystem's limit.

Implementation notes:

- caps are represented by `VolumeWritePolicy.Fat16SafeMaxFileSizeBytes` and `VolumeWritePolicy.Fat32SafeMaxFileSizeBytes`
- wipe files are named sequentially:
  - `wipe-0001.bin`
  - `wipe-0002.bin`
  - and so on
- if a per-file cap is reached, the writer rotates to a new file
- if opening the next file fails with `ERROR_DISK_FULL`, that is treated as a normal successful stop condition

## Cleanup Model

Cleanup is workspace-based rather than file-by-file.

Important behavior:

- completed split files are intentionally left in place during the run
- they are not deleted on close
- the workspace is deleted once the operation ends

This is important for capped filesystems: deleting earlier completed wipe files during the run would free space again and prevent the wipe from ever finishing.

`VolumeWipeOperation.Cleanup()` is retry-safe:

- if a first deletion attempt fails because a file is still open
- a later call can still remove the workspace successfully

## Progress, Speed, And ETA

Progress is based on live free-space measurements rather than the number of bytes the app believes it wrote.

That means Zerowriter computes:

- initial free space at the start of the run
- current free space during the run
- consumed space as `initial - current`

This gives more truthful progress when allocation overhead or other volume activity affects free space.

Speed and ETA behavior:

- the first measured interval is treated as warm-up
- speed and ETA are first shown on the second measured update
- once valid values are available, the last known values are retained through short stalls
- updates are intentionally more frequent than the original implementation, but still smoothed enough to avoid excessive noise

## Cancellation And Console Behavior

`ShutdownCoordinator` listens for:

- `Console.CancelKeyPress`
- `ProcessExit`
- `UnhandledException`

On `Ctrl+C`, the app cancels the wipe loop and lets the normal unwind path clean up.

Console-specific behavior:

- the interactive cursor is hidden while the app runs
- the progress line is redrawn in place
- redirected output is treated differently so logs remain readable

## NativeAOT Release Layout

Release publishes use NativeAOT and write the final executable directly into:

- `artifacts/win-x86/`
- `artifacts/win-x64/`
- `artifacts/win-arm64/`

The `bin` and `obj` folders still contain normal SDK and AOT intermediate data. Those are build internals, not the intended release artifacts.

The clean user-facing release outputs are the `.exe` files under `artifacts/`.
