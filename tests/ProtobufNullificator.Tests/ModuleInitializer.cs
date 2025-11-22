using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace ProtobufNullificator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}