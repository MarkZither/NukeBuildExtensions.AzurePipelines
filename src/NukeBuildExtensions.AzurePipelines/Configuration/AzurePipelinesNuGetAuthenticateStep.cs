using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Utilities;

namespace NukeBuildExtensions.AzurePipelines.Configuration
{
    public class AzurePipelinesNuGetAuthenticateStep : AzurePipelinesStep
    {
        public override void Write(CustomFileWriter writer) => writer.WriteLine("- task: NuGetAuthenticate@1");
    }
}