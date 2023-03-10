using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Serilog;
using System.IO;
using Nuke.Common.CI.AzurePipelines;
using NukeBuildExtensions.AzurePipelines;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0, // fetch depth = 0 for versioning https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/doc/cloudbuild.md#github-actions
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Pack) })]
[GitHubActions(
    "publish",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushTags = new[] { "main" },
    InvokedTargets = new[] { nameof(Publish) },
    ImportSecrets = new[] { nameof(NuGetApiKey) })]
[AzurePipelines("Standard",
        AzurePipelinesImage.UbuntuLatest,
        //AzurePipelinesImage.WindowsLatest,
        FetchDepth = 0, // fetch depth = 0 for versioning https://github.com/dotnet/Nerdbank.GitVersioning/blob/main/doc/cloudbuild.md#azure-pipelines
        AutoGenerate = true,
        //CachePaths = new[]
        //{
        //        AzurePipelinesCachePaths.Nuke,
				//AzurePipelinesCachePaths.NuGet
		//},
        InvokedTargets = new[] { nameof(Pack) },
        ImportSecrets = new[] { nameof(NuGetApiKey) })]
[AzurePipelinesExtended("NugetAuth",
        AzurePipelinesImage.UbuntuLatest, 
        AzurePipelinesImage.WindowsLatest,
        NuGetAuthenticate = true,
        //AzurePipelinesImage.WindowsLatest,
        UseOnPremAgentPool = false,
        FetchDepth = 0,
        AutoGenerate = true,
        //CachePaths = new[]
        //{
        //        AzurePipelinesCachePaths.Nuke,
				//AzurePipelinesCachePaths.NuGet
		//},
        InvokedTargets = new[] { nameof(Compile) })]
[AzurePipelinesExtended("OnPremAgentPool",
        AzurePipelinesImage.WindowsLatest,
        NuGetAuthenticate = true,
        UseOnPremAgentPool = true,
        OnPremAgentPool = "MyOnPremAgentPool",
        FetchDepth = 0,
        AutoGenerate = true,
        //CachePaths = new[]
        //{
        //        AzurePipelinesCachePaths.Nuke,
        //AzurePipelinesCachePaths.NuGet
        //},
        InvokedTargets = new[] { nameof(Compile) },
        ImportSecrets = new[] { nameof(NuGetApiKey) })]

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;
    [NerdbankGitVersioning][Required] NerdbankGitVersioning Versioning;
    [Parameter] [Secret] readonly string NuGetApiKey;
    [Parameter] readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => RootDirectory / "packages";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(OutputDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
            EnsureCleanDirectory(PackagesDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
    .Executes(() =>
    {
        Log.Information("Version is {Versioning} on commit {GitCommit}", Versioning.Version, Versioning.GitCommitId);
        DotNetBuild(s => s
            .SetProjectFile(Solution)
            .SetConfiguration(Configuration)
            .SetDeterministic(true)
            .SetAssemblyVersion(Versioning.AssemblyVersion)
            .SetFileVersion(Versioning.AssemblyFileVersion)
            .SetInformationalVersion(Versioning.AssemblyInformationalVersion)
            .EnableNoRestore());
     });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Consumes(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
    .Executes(() =>
    {
        string NuGetReleaseNotes = "First release";
        DotNetPack(s => s
            .SetProject(RootDirectory / "src" / "NukeBuildExtensions.AzurePipelines" / "NukeBuildExtensions.AzurePipelines.csproj")
            .SetConfiguration(Configuration)
            .SetNoBuild(SucceededTargets.Contains(Compile))
            .SetOutputDirectory(PackagesDirectory)
            .SetVersion(Versioning.NuGetPackageVersion)
            .SetDeterministicSourcePaths(true)
            .SetIncludeSymbols(true)
            .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg)
            .SetPackageReleaseNotes(NuGetReleaseNotes));
    });

    Target Publish => _ => _
        .After(Pack)
        .Consumes(Pack)
        .Requires(() => NuGetSource)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(s => s
                .SetTargetPath(PackagesDirectory / "*.nupkg")
                .SetSource(NuGetSource)
                .SetApiKey(NuGetApiKey));
        });
}
