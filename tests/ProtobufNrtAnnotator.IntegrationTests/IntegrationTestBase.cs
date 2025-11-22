using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtobufNrtAnnotator.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
    protected string TestOutputDirectory { get; }
    protected string PackageOutputDirectory { get; }
    protected string SourceProjectPath { get; }
    protected string EmbeddedTestProjectPath { get; }
    
    protected IntegrationTestBase()
    {
        // Create a unique directory for this test run
        TestOutputDirectory = Path.Combine(Path.GetTempPath(), $"ProtobufNrtAnnotator.IntegrationTests.{Guid.NewGuid()}");
        Directory.CreateDirectory(TestOutputDirectory);
        
        PackageOutputDirectory = Path.Combine(TestOutputDirectory, "nupkg");
        Directory.CreateDirectory(PackageOutputDirectory);
        
        // Get paths to source project
        var currentDir = Directory.GetCurrentDirectory();
        SourceProjectPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", "..", "src", "ProtobufNrtAnnotator", "ProtobufNrtAnnotator.csproj"));
        
        // Get path to embedded test project (relative to this assembly)
        EmbeddedTestProjectPath = Path.Combine(currentDir, "TestProject");
    }
    
    /// <summary>
    /// Builds the NuGet package with a timestamp-based version suffix
    /// </summary>
    /// <returns>The full version string of the built package</returns>
    protected async Task<string> BuildNuGetPackageAsync()
    {
        // Parse version from project file
        var csprojContent = await File.ReadAllTextAsync(SourceProjectPath);
        var versionMatch = Regex.Match(csprojContent, "<Version>(.*?)</Version>");
        if (!versionMatch.Success)
        {
            throw new InvalidOperationException("Could not find version in csproj file");
        }
        
        var baseVersion = versionMatch.Groups[1].Value;
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var fullVersion = $"{baseVersion}-test-{timestamp}";
        
        var result = await RunDotNetCommandAsync(
            "pack",
            Path.GetDirectoryName(SourceProjectPath)!,
            $"-p:Version={fullVersion}",
            "-o", PackageOutputDirectory,
            "-c", "Release"
        );
        
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to build NuGet package. Exit code: {result.ExitCode}\nOutput: {result.Output}\nError: {result.Error}");
        }
        
        return fullVersion;
    }
    
    /// <summary>
    /// Builds an embedded test project with the specified package version
    /// </summary>
    /// <param name="packageVersion">The version of ProtobufNrtAnnotator to use</param>
    /// <returns>Build result with exit code, output, and error</returns>
    protected async Task<BuildResult> BuildTestProjectAsync(string packageVersion)
    {
        // Copy embedded test project to temp directory
        var testProjectDir = Path.Combine(TestOutputDirectory, "TestProject");
        CopyDirectory(EmbeddedTestProjectPath, testProjectDir);
        
        var result = await RunDotNetCommandAsync(
            "build",
            testProjectDir,
            $"/p:ProtobufNrtAnnotatorVersion={packageVersion}",
            $"/p:ProtobufNrtAnnotatorPackageSource={PackageOutputDirectory}",
            "-c Debug",
            "/v:n"
        );
        
        return result;
    }
    
    /// <summary>
    /// Parses MSBuild output for warning codes
    /// </summary>
    protected static List<string> ParseWarnings(string buildOutput)
    {
        var warnings = new List<string>();
        
        // Match patterns like "warning CS8625:" or "WarningTests.cs(12,24): warning CS8625:"
        var warningPattern = new Regex(@"warning (CS\d+):", RegexOptions.Multiline);
        var matches = warningPattern.Matches(buildOutput);
        
        foreach (Match match in matches)
        {
            warnings.Add(match.Groups[1].Value);
        }
        
        return warnings.Distinct().ToList();
    }
    
    /// <summary>
    /// Runs a dotnet command and returns the result
    /// </summary>
    private async Task<BuildResult> RunDotNetCommandAsync(string command, string workingDirectory, params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} {string.Join(" ", args)}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        await process.WaitForExitAsync();
        
        return new BuildResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString()
        };
    }
    
    /// <summary>
    /// Recursively copies a directory
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
    
    public void Dispose()
    {
        // Clean up test output directory
        if (Directory.Exists(TestOutputDirectory))
        {
            try
            {
                Directory.Delete(TestOutputDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

public record BuildResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
}
