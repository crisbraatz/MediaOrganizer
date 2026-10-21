# MediaOrganizer

MediaOrganizer is a cross-platform console app for organizing photos and videos into a clean chronological sequence.

It renames media files by capture time, replacing the mixed naming patterns created by smartphones, cameras, downloads, exports, and duplicated folders. A messy trip folder can become a simple sequence like `0001.jpeg`, `0002.mp4`, `0003.png`.

## Features

- Runs as a CLI command: `mediaorganizer`.
- Works on Linux, Windows, and macOS.
- Organizes photos and videos by capture time.
- Reads media metadata first, then falls back to the physical file creation time.
- Detects duplicate media by file content.
- Optionally removes duplicate media before renaming.
- Optionally moves media from subfolders into the selected root folder.
- Uses an interactive terminal UI with arrow-key choices.
- Supports path input with `Tab` folder completion.
- Shows an analysis report before making changes.
- Shows a progress bar while organizing.
- Supports `restart` and `exit` commands during the interactive flow.

## Supported Media

MediaOrganizer supports common photo and video extensions, including:

- Photos: `heic`, `heif`, `jpg`, `jpeg`, `png`, `webp`, `gif`, `tif`, `tiff`, `bmp`
- RAW photos: `dng`, `arw`, `cr2`, `cr3`, `nef`, `raf`, `orf`, `rw2`
- Videos: `mp4`, `mov`, `m4v`, `avi`, `mkv`, `webm`, `wmv`, `mpeg`, `mpg`, `mts`, `3gp`

## Install

Install MediaOrganizer directly from the latest GitHub Release.

### Linux and macOS

```bash
curl -fsSL https://raw.githubusercontent.com/crisbraatz/MediaOrganizer/main/install.sh | sh
```

Open a new terminal, then run:

```bash
mediaorganizer
```

### Windows

Run this in PowerShell:

```powershell
irm https://raw.githubusercontent.com/crisbraatz/MediaOrganizer/main/install.ps1 | iex
```

Then run:

```powershell
mediaorganizer
```

The installer detects your operating system and architecture, downloads the right self-contained executable, installs it in your user profile, and adds it to your user `PATH`.

To update MediaOrganizer, run the same install command again. The installer always downloads the latest GitHub Release and replaces the installed executable.

Published release assets:

- Linux x64: `mediaorganizer-linux-x64.tar.gz`
- Windows x64: `mediaorganizer-win-x64.zip`
- macOS Apple Silicon: `mediaorganizer-osx-arm64.tar.gz`
- macOS Intel: `mediaorganizer-osx-x64.tar.gz`

Each release asset is published as a self-contained single-file build, so users do not need to install the .NET runtime separately.

## Usage

Start the app:

```bash
mediaorganizer
```

Enter the absolute path to the folder you want to organize. The app shows an OS-specific path example and lets you press `Tab` to complete folders from the current terminal location.

MediaOrganizer analyzes the folder and reports:

- Total media
- Total photos
- Total videos
- Totals by extension
- Total duplicate media
- Total duplicate photos
- Total duplicate videos
- Duplicate media by extension
- Total subfolders

After the analysis, choose:

- Whether duplicate media should be removed
- Whether files from subfolders should be moved into the root folder
- Whether you want to proceed with the selected options

Choices are selected with the arrow keys. You can type `restart` to restart the flow or `exit` to close the app.

## Organization Rules

MediaOrganizer sorts media by capture time and renames files into a single increasing sequence.

Examples:

```text
0001.heic
0002.mov
0003.jpg
0004.mp4
```

The number width is based on the final media count in the folder being organized:

- 9 files: `1.jpg` through `9.jpg`
- 98 files: `01.jpg` through `98.jpg`
- 987 files: `001.jpg` through `987.jpg`
- 9,876 files: `0001.jpg` through `9876.jpg`

When subfolder flattening is enabled, MediaOrganizer first moves files from subfolders into the root folder, removes the subfolders, and only then renames the media. This avoids generating conflicting sequences inside nested folders.

When duplicate removal is enabled, duplicate media is removed before the final sequence is generated.

## Safety

MediaOrganizer renames files and can optionally delete duplicate media and remove subfolders. Always review the analysis report and keep a backup before organizing important folders.

The app asks for confirmation before applying irreversible changes.
