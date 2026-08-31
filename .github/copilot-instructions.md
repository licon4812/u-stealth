# Copilot Instructions

## Project Guidelines

- When addressing the DriveHelper deployment issue, the user specifically wants a ProjectReference-based solution and does not want a custom MSBuild helper publish target. Preserve the native executable deployment in the packaged WinUI app; do not allow the ProjectReference to package or launch UStealth.DriveHelper.dll instead of UStealth.DriveHelper.exe.