using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Modules;
using UStealth.ModularPipelines.Services;

namespace UStealth.ModularPipelines.Modules
{
    [RunOnWindowsOnly]
    public class PackAndPublishDriveHelperModule(FileService fileService) : Module<Command>
    {
        protected override async Task<Command?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
        {
            var driveHelperProject = $@"{fileService.GetSolutionDirectory()}\UStealth.DriveHelper";
            // Get all the publish profiles from the Properties folder
            var publishProfiles = context.FileSystem.GetFiles($@"{driveHelperProject}\Properties"!, file => file.Exists );
            var publishProfileNames = publishProfiles
                .Where(f => f.Path.EndsWith(".pubxml", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetFileNameWithoutExtension(f.Path))
                .ToList();
            // Clean up the bin folder if it exists
            if (context.FileSystem.FolderExists($@"{driveHelperProject}\bin"!))
            {
                context.FileSystem.DeleteFolder($@"{driveHelperProject}\bin"!);
            }

            // Get TFM dynamically
            var msbuildOptions = new DotNetMsbuildOptions($@"{driveHelperProject}\UStealth.DriveHelper.csproj")
            {
                Arguments = ["-getProperty:TargetFrameworks"]
            };
            var tfmResult = await context.DotNet().Msbuild(msbuildOptions, cancellationToken);
            var tfmOutput = tfmResult?.StandardOutput?.Trim();
            var targetFrameworks = tfmOutput.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var tfm in targetFrameworks)
            {
                var filteredProfiles = tfm switch
                {
                    _ when tfm.Contains("windows", StringComparison.OrdinalIgnoreCase) =>
                        publishProfileNames.Where(p => p.Contains("win", StringComparison.OrdinalIgnoreCase)),
                    _ when tfm.Contains("osx",StringComparison.OrdinalIgnoreCase) =>
                        publishProfileNames.Where(p => p.Contains("osx", StringComparison.OrdinalIgnoreCase)),
                    _ => publishProfileNames.Where(p=>p.Contains("linux",StringComparison.OrdinalIgnoreCase))
                };

                foreach (var publishProfileName in filteredProfiles)
                {
                    await context.DotNet().Publish(new DotNetPublishOptions()
                    {
                        WorkingDirectory = driveHelperProject,
                        Arguments = [$"/p:PublishProfile=\\Properties\\{publishProfileName}"],
                        Framework = tfm
                    }, cancellationToken);
                    var publishedDir = $@"{driveHelperProject}\bin\release\{tfm}\{publishProfileName}\publish";
                    var distributionFolder = Path.Combine(publishedDir, $"ustealth-cli-{publishProfileName}");
                    var renamedFileName = string.Empty;
                    if (!Directory.Exists(distributionFolder))
                    {
                        Directory.CreateDirectory(distributionFolder);
                    }
                    var publishedExecutable = string.Empty;
                    if (publishProfileName.Contains("win"))
                    {
                        publishedExecutable = $@"{publishedDir}\UStealth.DriveHelper.exe";
                        renamedFileName = "ustealth.exe";
                    }
                    else if (publishProfileName.Contains("osx"))
                    {
                        //todo: add mac support
                    }
                    else
                    {
                        publishedExecutable = $@"{publishedDir}\UStealth.DriveHelper";
                        renamedFileName = "ustealth";
                    }
                    context.FileSystem.CopyFile(publishedExecutable, $@"{distributionFolder}\{renamedFileName}");
                    var zippedFolder = context.Zip.ZipFolder(distributionFolder, publishedDir);
                    context.FileSystem.MoveFile(zippedFolder, $@"{publishedDir}\ustealth-cli-{publishProfileName}.zip");
                    context.Logger.LogToConsole($"Published and zipped {publishProfileName} to {zippedFolder.Path}");
                }
            }
            return await Task.FromResult<Command?>(new Command(null));
        }
    }
}
