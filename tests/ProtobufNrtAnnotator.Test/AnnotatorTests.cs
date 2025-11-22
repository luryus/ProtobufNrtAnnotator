
namespace ProtobufNrtAnnotator.Test;

public class AnnotatorTests
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
        var outputCode = Runner.ProcessContent(inputCode);

        // Assert
        await Verify(outputCode, extension: "cs").UseFileName(messageName).UseDirectory("Expected");
    }
    
    [Theory]
    [InlineData("GrpcService")]
    [InlineData("GrpcServiceGrpc")]
    public async Task ProcessGrpc_Snapshot(string fileName)
    {
        // Arrange
        var inputPath = Path.Combine("Generated", "GrpcProtos", $"{fileName}.cs");
        Assert.True(File.Exists(inputPath), $"Generated file not found at {inputPath}");
        
        var inputCode = await File.ReadAllTextAsync(inputPath);

        // Act
        var outputCode = Runner.ProcessContent(inputCode);

        // Assert
        await Verify(outputCode, extension: "cs").UseFileName(fileName).UseDirectory("Expected");
    }
}
