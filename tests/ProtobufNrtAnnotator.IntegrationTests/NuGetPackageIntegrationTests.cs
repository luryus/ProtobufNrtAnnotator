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
        
        // CS8625: Cannot convert null literal to non-nullable reference type
        Assert.Contains("WarningTests.cs(12,20): warning CS8625", buildResult.Output);
        Assert.Contains("WarningTests.cs(13,21): warning CS8625", buildResult.Output);
        Assert.Contains("WarningTests.cs(13,21): warning CS8625", buildResult.Output);
        Assert.Contains("WarningTests.cs(19,22): warning CS8625", buildResult.Output);
        Assert.Contains("WarningTests.cs(19,30): warning CS8625", buildResult.Output);
        
        // CS8602: Dereference of a possibly null reference
        Assert.Contains("WarningTests.cs(16,9): warning CS8602", buildResult.Output);
        Assert.Contains("WarningTests.cs(22,9): warning CS8602", buildResult.Output);
        
        // The other file should have no warnings
        Assert.DoesNotMatch(@"PartialClassConstructorTest.cs\(.*\): warning", buildResult.Output);
    }
}
