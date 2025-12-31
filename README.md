# u-stealth

u-stealth is a legacy tool for allowing for drives to be hidden so that the wii-u doesn't hound the user for formatting.

It has been migrated from Windows Forms targeting .NET Framework 3.5 to Windows Forms for .NET 9.0.

I have also migrated it to the windows app sdk as a UI application as well as a CLI application for both windows and linux. For more modern UI and for Native AOT and trimming.

## U-Stealth

U-Stealth has been rebuilt in the windows app sdk and is available here:

### Microsoft Store

<a href="https://apps.microsoft.com/detail/9PP5QM8RK8XQ?mode=direct">
 <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

### WinGet

`winget install 9PP5QM8RK8XQ`

## U-Stealth CLI

The U-Stealth CLI is a command-line interface for managing U-Stealth settings and configurations. It allows users to perform actions without the need for a graphical interface.

### Windows 

#### WinGet

`winget install 9P1TXW2R0R4T`

#### Microsoft Store

<a href="https://apps.microsoft.com/detail/9P1TXW2R0R4T?mode=direct">
 <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
</a>

### Linux
I have not published this package to any package manager as of yet. I will look into publishing it to snap or apt in the future.
For now you will have to download the executable from the releases page on this repo.


### GitHub Releases

| Release Name                                             | Version | Download Link                                                              |
|----------------------------------------------------------|---------|----------------------------------------------------------------------------|
| CLI 1.0.4.0 .NET 10.0 Upgrade                            | 1.0.4.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/WinUI-1.0.7.0_CLI-1.0.4.0)|
| CLI 1.0.3.0 Linux Release                                | 1.0.3.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/CLI-1.0.3.0)|
| CLI 1.0.2.0 Native AOT                                   | 1.0.2.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/CLI-1.0.2.0)|				
| Migrated to the Windows App SDK and Released CLI version | 1.0.1.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/1.0.1.0)    |

## Legacy Version

The legacy version of U-Stealth is still available for download. I have updated it to dotnet 9.0 from .NET Framework 3.5

### GitHub Releases

| Release Name         | Version | Download Link                                                           |
|----------------------|---------|-------------------------------------------------------------------------|
| Upgraded to .NET 10.0 & Theme Support | 1.0.1.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/Legacy-1.0.1) |
| Upgraded to .NET 9.0 | 1.0.0.0 | [Download](https://github.com/licon4812/u-stealth/releases/tag/1.0.0.0) |


## WinUI.Table View

A modern table view control for WinUI applications. I have cloned this repo and added it as a project reference for UStealth.WinUI/
This was due to the table not supporting WinRT interop bindings. I have made a PR to the original repo to add this functionality.

- [GitHub Repository](https://github.com/w-ahmad/WinUI.TableView)
- [GitHub Issue](https://github.com/w-ahmad/WinUI.TableView/issues/207)
- [Pull Request](https://github.com/w-ahmad/WinUI.TableView/pull/209)

One the PR is merged I will remove the project reference and add it as a nuget package.
