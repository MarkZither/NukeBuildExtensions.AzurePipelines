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

