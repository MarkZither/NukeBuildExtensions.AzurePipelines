using System.Linq;
using System.Linq.Expressions;
using Nuke.Common.Execution;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities;

using NukeBuildExtensions.AzurePipelines.Configuration;
using Nuke.Common.CI.AzurePipelines.Configuration;

namespace NukeBuildExtensions.AzurePipelines
{
    public class AzurePipelinesExtendedAttribute : AzurePipelinesAttribute
    {
        public AzurePipelinesExtendedAttribute(string suffix, AzurePipelinesImage image, params AzurePipelinesImage[] images) : base(suffix, image, images)
        {
        }

        public bool NuGetAuthenticate { get; set; }
        protected override IEnumerable<AzurePipelinesStep> GetSteps(ExecutableTarget executableTarget, IReadOnlyCollection<ExecutableTarget> relevantTargets, AzurePipelinesImage image)
        {
            if (NuGetAuthenticate)
            {
                yield return new AzurePipelinesNuGetAuthenticateStep();
            }

            foreach (var step in base.GetSteps(executableTarget, relevantTargets, image))
            {
                yield return step;
            }
        }
        //protected override IEnumerable<AzurePipelinesStep> GetSteps(ExecutableTarget executableTarget, IReadOnlyCollection<ExecutableTarget> relevantTargets)
          //  => base.GetSteps(executableTarget, relevantTargets).Prepend(new AzurePipelinesNuGetAuthenticateStep());
    }
}