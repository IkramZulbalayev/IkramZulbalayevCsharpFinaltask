# C# Project

## Overview

WordScanner is a multi-threaded C# application that scans `.txt` files in a specified directory, counts word occurrences, and aggregates the results using multiple processes communicating via named pipes.

The project includes:

- **Master**: Aggregates and displays word counts received from scanners.
- **ScannerA**: Processes files with even indices and sends word counts to Master.
- **ScannerB**: Processes files with odd indices and sends word counts to Master.

## How it works

- The **Master** listens on two named pipes (`pipe_scannerA` and `pipe_scannerB`) concurrently.
- **ScannerA** and **ScannerB** split the workload by processing alternate files.
- Both scanners count words in their assigned files and send counts to the Master process via named pipes.
- The Master aggregates all counts and displays the final word count per file.

## Usage

1. Clone or download this repository.
2. Build the projects in Visual Studio 2022 or using the .NET CLI.
3. Run the **Master** application first to start listening on named pipes.
4. Run **ScannerA** and **ScannerB** applications, and enter the directory path containing `.txt` files when prompted.
5. The Master will display aggregated word counts after processing completes.

## Notes

- CPU affinity is set to specific cores for improved performance.
- Text processing is case-insensitive.
- Named pipes are used for efficient inter-process communication.
