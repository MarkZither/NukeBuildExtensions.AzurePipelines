# NukeBuildExtensions.AzurePipelines

## Adding the nugetauth step

``` csharp
[AzurePipelinesExtended("NugetAuth",
        AzurePipelinesImage.UbuntuLatest, 
        AzurePipelinesImage.WindowsLatest,
        NuGetAuthenticate = true,
        AutoGenerate = true,
        InvokedTargets = new[] { nameof(Compile) })]
```

## Runing pipelines on on-premises agent pools

``` csharp
[AzurePipelinesExtended("OnPremAgentPool",
        AzurePipelinesImage.WindowsLatest, // this is redundant
        UseOnPremAgentPool = true,
        OnPremAgentPool = "MyOnPremAgentPool",
        AutoGenerate = true,
        InvokedTargets = new[] { nameof(Compile) })]
```

