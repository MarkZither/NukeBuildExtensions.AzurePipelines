using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NukeBuildExtensions.AzurePipelines.Configuration
{
    public class AzurePipelinesOnPremStage : AzurePipelinesStage
    { 
        public bool UseOnPremAgentPool { get; set; }
        public string PoolName { get; set; } = string.Empty;
        public override void Write(CustomFileWriter writer)
        {
            using (writer.WriteBlock($"- stage: {Name}"))
            {
                writer.WriteLine($"displayName: {DisplayName.SingleQuote()}");
                writer.WriteLine($"dependsOn: [ {Dependencies.Select(x => x.Name).JoinCommaSpace()} ]");

                writer.WriteLine($"pool: {PoolName.SingleQuote()}");

                using (writer.WriteBlock("jobs:"))
                {
                    Jobs.ForEach(x => x.Write(writer));
                }
            }
        }
    }
}
