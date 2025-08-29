using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Modules;
using UStealth.ModularPipelines.Services;

namespace UStealth.ModularPipelines.Modules
{
    public class PackAndPublishDriveHelperModule(FileService fileService) : Module<Command>
    {
        protected override async Task<Command?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
        {
            var driveHelperProject = $@"{fileService.GetSolutionDirectory()}\UStealth.DriveHelper";
            // Get all the publish profiles from the Properties folder
            var publishProfiles = context.FileSystem.GetFiles($@"{driveHelperProject}\Properties"!, file => file.Exists );
            var publishProfileNames = publishProfiles
                .Where(f => f.Path.EndsWith(".pubxml", StringComparison.OrdinalIgnoreCase))
                .Select(f => System.IO.Path.GetFileNameWithoutExtension(f.Path))
                .ToList();
            // Clean up the bin folder if it exists
            if (context.FileSystem.FolderExists($@"{driveHelperProject}\bin"!))
            {
                context.FileSystem.DeleteFolder($@"{driveHelperProject}\bin"!);
            }

            // Get TFM dynamically
            var msbuildOptions = new DotNetMsbuildOptions($@"{driveHelperProject}\UStealth.DriveHelper.csproj")
            {
                Arguments = ["-getProperty:TargetFramework"]
            };
            var tfmResult = await context.DotNet().Msbuild(msbuildOptions, cancellationToken);
            var tfmOutput = tfmResult?.StandardOutput?.Trim();
            var tfm = string.IsNullOrWhiteSpace(tfmOutput) ? "net9.0-windows" : tfmOutput;

            foreach (var publishProfileName in publishProfileNames.Where(p=>p.Contains("win")))
            {
                await context.DotNet().Publish(new DotNetPublishOptions()
                {
                    WorkingDirectory = driveHelperProject,
                    Arguments = [$"/p:PublishProfile=\\Properties\\{publishProfileName}"],
                    Framework = tfm
                }, cancellationToken);
                var publishedDir = $@"{driveHelperProject}\bin\release\{tfm}\{publishProfileName}\publish";
                var publishedExecutable = $@"{publishedDir}\UStealth.DriveHelper.exe";
                var distributionFolder = Path.Combine(publishedDir, $"ustealth-cli-{publishProfileName}");
                if (!Directory.Exists(distributionFolder))
                {
                    Directory.CreateDirectory(distributionFolder);
                }
                var renamedExecutable = context.FileSystem.CopyFile(publishedExecutable, $@"{distributionFolder}\ustealth.exe");
                var zippedFolder = context.Zip.ZipFolder(distributionFolder,publishedDir);
                context.FileSystem.MoveFile(zippedFolder, $@"{publishedDir}\ustealth-cli-{publishProfileName}.zip");
                context.Logger.LogToConsole($"Published and zipped {publishProfileName} to {zippedFolder.Path}");
            }
            return await Task.FromResult<Command?>(new Command(null));
        }
    }
}
