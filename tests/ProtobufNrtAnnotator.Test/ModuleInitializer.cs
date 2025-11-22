using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace ProtobufNrtAnnotator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}