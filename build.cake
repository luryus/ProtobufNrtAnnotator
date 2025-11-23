var target = Argument("target", "Build");
var targetVersion = Argument("target-version", "");
var configuration = Argument("configuration", "Release");

Task("Clean")
    .Does(() => 
{
    CleanDirectories("*/**/bin");
    CleanDirectories("*/**/obj");
});

Task("Build")
    .Does(() =>
{
    DotNetBuild("./ProtobufNrtAnnotator.sln", new DotNetBuildSettings
    {
        Configuration = configuration,
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetTest("./ProtobufNrtAnnotator.sln", new DotNetTestSettings
    {
        Configuration = configuration,
        NoBuild = true,
    });
});

Task("PackageNuget")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetPack("./src/ProtobufNrtAnnotator/ProtobufNrtAnnotator.csproj",
        new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = "./nupkg"
        });

    Information("Nuget package saved to ./nupkg");
});

Task("BumpVersion")
    .Does(() =>
{
    if (string.IsNullOrEmpty(targetVersion))
    {
        throw new Exception("Please provide a target version using --target-version=X.Y.Z");
    }

    Information($"Bumping version to {targetVersion}");

    // Update Directory.Build.props
    var buildProps = "./Directory.Build.props";
    XmlPoke(buildProps, "/Project/PropertyGroup/VersionPrefix", targetVersion);
    Information($"Updated {buildProps}");

    // Update Directory.Packages.props
    var packagesProps = "./Directory.Packages.props";
    XmlPoke(packagesProps, "/Project/ItemGroup/PackageVersion[@Include='ProtobufNrtAnnotator']/@Version", targetVersion);
    Information($"Updated {packagesProps}");

    // Update README.md
    var readme = "./README.md";
    var content = System.IO.File.ReadAllText(readme);
    
    // Update PackageReference
    // <PackageReference Include="ProtobufNrtAnnotator" Version="0.2.0">
    var packageRefRegex = new System.Text.RegularExpressions.Regex(@"<PackageReference Include=""ProtobufNrtAnnotator"" Version="".*?""");
    content = packageRefRegex.Replace(content, $"<PackageReference Include=\"ProtobufNrtAnnotator\" Version=\"{targetVersion}\"");

    System.IO.File.WriteAllText(readme, content);
    Information($"Updated {readme}");
});

RunTarget(target);
