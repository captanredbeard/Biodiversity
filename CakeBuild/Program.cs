using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Solution;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Common.Tools.MSBuild;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }

    public class BuildContext : FrostingContext
    {
        public const string ProjectName = "biodiversity";

        public string BuildConfiguration { get; }
        public string Version { get; }
        public string Name { get; }
        public bool SkipJsonValidation { get; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);
            var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
            Version = modInfo.Version;
            Name = modInfo.ModID;
        }
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation)
            {
                return;
            }
            var jsonFiles = context.GetFiles($"../*/assets/**/*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file.FullPath);
                    JToken.Parse(json);
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
            }
        }
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var projects = new[] { "biodiversity", "bdflower", "bdshrub", "bdaquatic", "bdherb", "bdcrop", "bdorchard", "bdtree" };

            foreach (var project in projects)
            {
                var modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{project}/modinfo.json");
                var modType = modInfo.Type.ToString();

                context.DotNetClean($"../{project}/{project}.csproj",
                new DotNetCleanSettings
                {
                    Configuration = context.BuildConfiguration
                });

                if (modType != "Code")
                {
                    continue;
                }

           //     var msBuildSettings = new DotNetMSBuildSettings()
             //   .WithProperty("OutputPath", $"../Releases/{project}/");




                context.DotNetPublish($"../{project}/{project}.csproj",
                    new DotNetPublishSettings
                    {
                        Configuration = context.BuildConfiguration,
                        //  OutputDirectory = $"../Releases/{project}",
                        //       MSBuildSettings = msBuildSettings,
                        NoBuild = false
                    });
            }
        }
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        /*
        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists("../Releases");
            context.CleanDirectory("../Releases");
            context.EnsureDirectoryExists($"../Releases/{context.Name}");
            context.CopyFiles($"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*", $"../Releases/{context.Name}");
            if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets"))
            {
                context.CopyDirectory($"../{BuildContext.ProjectName}/assets", $"../Releases/{context.Name}/assets");
            }
            context.CopyFile($"../{BuildContext.ProjectName}/modinfo.json", $"../Releases/{context.Name}/modinfo.json");
            if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
            {
                context.CopyFile($"../{BuildContext.ProjectName}/modicon.png", $"../Releases/{context.Name}/modicon.png");
            }
            context.Zip($"../Releases/{context.Name}", $"../Releases/{context.Name}_{context.Version}.zip");
        }
        */
        public override void Run(BuildContext context)
        {
            var projects = new[] { "biodiversity", "bdflower", "bdshrub", "bdaquatic", "bdherb", "bdcrop", "bdorchard", "bdtree" };



            foreach (var project in projects)
            {
                var projectPath = $"../{project}";
                var releasePath = $"../Releases/{project}";


                var modInfo = context.DeserializeJsonFromFile<ModInfo>($"{projectPath}/modinfo.json");
                var projectVersion = modInfo.Version;
                var modID = modInfo.ModID;

                context.EnsureDirectoryExists(releasePath);
                context.CleanDirectory(releasePath);
                context.EnsureDirectoryExists(releasePath);

                context.CopyFiles($"../bin/{context.BuildConfiguration}/Mods/{modID}/*", $"../Releases/{modID}");

                // Copy assets
                if (context.DirectoryExists($"{projectPath}/assets"))
                {
                    context.CopyDirectory($"{projectPath}/assets", $"{releasePath}/assets");
                }

                // Copy modinfo.json
                context.CopyFile($"{projectPath}/modinfo.json", $"{releasePath}/modinfo.json");

                // Copy icon if it exists
                if (context.FileExists($"{projectPath}/modicon.png"))
                {
                    context.CopyFile($"{projectPath}/modicon.png", $"{releasePath}/modicon.png");
                }

                // Zip the release
                context.Zip(releasePath, $"../Releases/{modID}_{projectVersion}.zip");
            }
        }
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask
    {
    }
}