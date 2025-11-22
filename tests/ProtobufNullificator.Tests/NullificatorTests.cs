using Xunit;
using ProtobufNullificator.PostProcessor;
using System.IO;
using System.Threading.Tasks;

namespace ProtobufNullificator.Tests;

public class NullificatorTests
{
    [Theory]
    [InlineData("SimpleMessage")]
    [InlineData("ComplexMessage")]
    [InlineData("Proto2Message")]
    [InlineData("Edition2023Message")]
    [InlineData("ExtensionsMessage")]
    [InlineData("NestedMessage")]
    [InlineData("BytesMessage")]
    [InlineData("WrappersMessage")]
    [InlineData("CollectionsMessage")]
    [InlineData("EnumsMessage")]
    [InlineData("AnyMessage")]
    public async Task ProcessMessage_Snapshot(string messageName)
    {
        // Arrange
        var inputPath = Path.Combine("Generated", "Protos", $"{messageName}.cs");
        Assert.True(File.Exists(inputPath), $"Generated file not found at {inputPath}");
        
        var inputCode = await File.ReadAllTextAsync(inputPath);

        // Act
        var outputCode = await NullificatorRunner.ProcessContentAsync(inputCode);

        // Assert
        await Verify(outputCode).UseFileName(messageName).UseDirectory("Expected");
    }
}
