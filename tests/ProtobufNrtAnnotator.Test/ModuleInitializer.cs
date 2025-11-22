using System.Runtime.CompilerServices;
using VerifyTests.DiffPlex;

namespace ProtobufNrtAnnotator.Test;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyDiffPlex.Initialize(OutputType.Compact);
    }
}