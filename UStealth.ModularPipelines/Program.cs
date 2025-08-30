using Microsoft.Extensions.DependencyInjection;
using ModularPipelines.Host;
using UStealth.ModularPipelines.Modules;
using UStealth.ModularPipelines.Services;

await PipelineHostBuilder.Create()
    .ConfigureServices((context, collection) =>
    {
        collection.AddSingleton<FileService>();
    })
    .AddModule<PackAndPublishDriveHelperModuleWindows>()
    .ExecutePipelineAsync();
