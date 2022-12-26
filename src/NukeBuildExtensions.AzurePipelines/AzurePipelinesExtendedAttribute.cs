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
using Serilog;
using System.Diagnostics;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using JetBrains.Annotations;

namespace NukeBuildExtensions.AzurePipelines
{
    public class AzurePipelinesExtendedAttribute : AzurePipelinesAttribute
    {
        public AzurePipelinesExtendedAttribute(AzurePipelinesImage image, params AzurePipelinesImage[] images) : base(image, images)
        {
        }

        public AzurePipelinesExtendedAttribute([CanBeNull] string suffix, AzurePipelinesImage image, params AzurePipelinesImage[] images) : base(suffix, image, images)
        {
        }

        public bool NuGetAuthenticate { get; set; }
        public bool UseOnPremAgentPool { get; set; }
        public string OnPremAgentPool { get; set; } = string.Empty;
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

        protected override AzurePipelinesStage GetStage(AzurePipelinesImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            //Debugger.Launch();
            Log.Information("The image is {image}", image.ToString());
            if (UseOnPremAgentPool)
            {
                var lookupTable = new LookupTable<ExecutableTarget, AzurePipelinesJob>();
                var jobs = relevantTargets
                    .Select(x => (ExecutableTarget: x, Job: GetJob(x, lookupTable, relevantTargets, image)))
                    .ForEachLazy(x => lookupTable.Add(x.ExecutableTarget, x.Job))
                    .Select(x => x.Job).ToArray();

                return new AzurePipelinesOnPremStage
                {
                    Name = image.GetValue().Replace("-", "_").Replace(".", "_"),
                    DisplayName = image.GetValue(),
                    Image = image,
                    Dependencies = new AzurePipelinesStage[0],
                    Jobs = jobs,
                    UseOnPremAgentPool = UseOnPremAgentPool,
                    PoolName = OnPremAgentPool
                };
            }

            return base.GetStage(image, relevantTargets);
        }
    }
}