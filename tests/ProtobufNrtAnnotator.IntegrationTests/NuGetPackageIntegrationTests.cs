namespace ProtobufNrtAnnotator.IntegrationTests;

public class NuGetPackageIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task NuGetPackageIntegration_BuildsAndEmitsWarnings()
    {
        // Step 1: Build NuGet package
        var packageVersion = await BuildNuGetPackageAsync();
        
        // Verify package was created with correct version
        Assert.NotNull(packageVersion);
        Assert.Contains("0.0.0-test.", packageVersion);
        
        var packageFiles = Directory.GetFiles(PackageOutputDirectory, "*.nupkg");
        Assert.Single(packageFiles);
        
        var packageFileName = Path.GetFileName(packageFiles[0]);
        Assert.Contains(packageVersion, packageFileName);
        
        // Step 2: Build embedded test project with the generated package
        var buildResult = await BuildTestProjectAsync(packageVersion);
        
        // Verify build succeeded
        Assert.True(0 == buildResult.ExitCode, "build failed: " + buildResult.Output);
        Assert.Contains("Build succeeded", buildResult.Output);
        
        // Step 3: Verify expected nullability warnings were emitted
        var warnings = ParseWarnings(buildResult.Output);
        
        // CS8625: Cannot convert null literal to non-nullable reference type
        Assert.Contains("CS8625", warnings);
        
        // CS8602: Dereference of a possibly null reference
        Assert.Contains("CS8602", warnings);
        
        var warningsSummary = string.Join(", ", warnings.OrderBy(w => w));
        Assert.True(warnings.Count >= 2, 
            $"Expected at least 2 different warning types, but found {warnings.Count}: {warningsSummary}");
    }
}
