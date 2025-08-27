using System;
using System.IO;

namespace UStealth.ModularPipelines.Services
{
    public class FileService
    {
        public string GetSolutionDirectory()
        {
            var currentDir = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDir != null && !currentDir.GetFiles("*.sln").Any())
            {
                currentDir = currentDir.Parent;
            }

            return currentDir == null ? throw new InvalidOperationException("Solution directory not found.") : currentDir.FullName;
        }
    }
}