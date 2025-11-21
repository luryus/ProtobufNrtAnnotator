using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtobufNullificator.PostProcessor;

public static class NullificatorRunner
{
    public static async Task<string?> ProcessContentAsync(string code, IEnumerable<string>? referencePaths = null)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Create compilation
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Google.Protobuf.MessageParser).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"))
        };

        if (referencePaths != null)
        {
            foreach (var path in referencePaths)
            {
                if (File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        var compilation = CSharpCompilation.Create("ProtobufNullificatorAnalysis")
            .AddReferences(references)
            .AddSyntaxTrees(tree);

        var semanticModel = compilation.GetSemanticModel(tree);

        // Step 1: Analyze the original tree to determine which properties should be nullable
        var nullabilityDecisions = NullabilityRewriter.AnalyzeTree(root, semanticModel);

        // Step 2: Rewrite the tree using the pre-computed decisions
        var rewriter = new NullabilityRewriter(nullabilityDecisions);
        var newRoot = rewriter.Visit(root);

        // Step 3: Add #nullable enable directive at the top
        if (newRoot is CompilationUnitSyntax compilationUnit)
        {
            var nullableText = "#nullable enable annotations" + Environment.NewLine;
            var nullableTree = CSharpSyntaxTree.ParseText(nullableText + "class Dummy {}");
            var nullableDirective = nullableTree.GetRoot()
                .DescendantTrivia()
                .First(t => t.IsKind(SyntaxKind.NullableDirectiveTrivia));

            var newLine = SyntaxFactory.EndOfLine(Environment.NewLine);

            var leadingTrivia = compilationUnit.GetLeadingTrivia()
                .Insert(0, newLine)
                .Insert(0, nullableDirective);
            newRoot = compilationUnit.WithLeadingTrivia(leadingTrivia);
        }

        return newRoot?.ToFullString();
    }
}
