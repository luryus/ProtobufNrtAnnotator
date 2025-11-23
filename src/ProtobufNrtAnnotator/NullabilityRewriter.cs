using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtobufNrtAnnotator;

/// <summary>
/// Rewrites protobuf-generated C# code to add nullability annotations.
/// </summary>
internal class NullabilityRewriter(Dictionary<int, bool> nullabilityDecisions) : CSharpSyntaxRewriter
{
    /// <summary>
    /// Analyzes a syntax tree and determines which properties, fields, and parameters should be nullable.
    /// Call this BEFORE creating the rewriter.
    /// </summary>
    public static Dictionary<int, bool> AnalyzeTree(SyntaxNode root, SemanticModel semanticModel)
    {
        var decisions = new Dictionary<int, bool>();
        
        // Process each class separately to handle field scoping correctly
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var classDecl in classes)
        {
            // Map field names to their declarations for quick lookup within this class
            var fieldMap = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables.Select(v => new { Name = v.Identifier.Text, Field = f }))
                .GroupBy(x => x.Name) // Handle potential duplicates (though invalid C#) gracefully
                .ToDictionary(g => g.Key, g => g.First().Field);

            // Analyze properties
            foreach (var property in classDecl.Members.OfType<PropertyDeclarationSyntax>())
            {
                bool isNullable = ShouldPropertyBeNullable(property, property.Type, semanticModel);
                decisions[property.SpanStart] = isNullable;
            }
            
            // Analyze fields
            foreach (var field in classDecl.Members.OfType<FieldDeclarationSyntax>())
            {
                decisions[field.SpanStart] = ShouldFieldBeNullable(field, semanticModel);
            }
        }
        
        // Analyze parameters
        var parameters = root.DescendantNodes().OfType<ParameterSyntax>();
        foreach (var parameter in parameters)
        {
            decisions[parameter.SpanStart] = ShouldParameterBeNullable(parameter);
        }
        
        return decisions;
    }



    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // Skip if already nullable
        if (node.Type is NullableTypeSyntax)
        {
            return base.VisitPropertyDeclaration(node);
        }
        
        // Use the position to look up the pre-computed decision from analysis phase
        if (nullabilityDecisions.TryGetValue(node.SpanStart, out var shouldBeNullable) && shouldBeNullable)
        {

            // Save the trailing trivia (typically a space before the property name)
            var trailingTrivia = node.Type.GetTrailingTrivia();
            
            // Create nullable type without trailing trivia on the base type
            var typeWithoutTrailing = node.Type.WithoutTrailingTrivia();
            var nullableType = SyntaxFactory.NullableType(typeWithoutTrailing);
            
            // Put the trailing trivia AFTER the ? token, not before it
            // This gives us "string? Nickname" instead of "string ?Nickname"
            var questionToken = nullableType.QuestionToken.WithTrailingTrivia(trailingTrivia);
            nullableType = nullableType.WithQuestionToken(questionToken);
            
            node = node.WithType(nullableType);
        }

        return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        // Skip if already nullable
        if (node.Declaration.Type is NullableTypeSyntax)
        {
            return base.VisitFieldDeclaration(node);
        }
        
        // Use the position to look up the pre-computed decision from analysis phase
        if (nullabilityDecisions.TryGetValue(node.SpanStart, out var shouldBeNullable) && shouldBeNullable)
        {
            // Apply the same trivia handling as properties
            var originalType = node.Declaration.Type;
            var trailingTrivia = originalType.GetTrailingTrivia();
            
            var typeWithoutTrailing = originalType.WithoutTrailingTrivia();
            var nullableType = SyntaxFactory.NullableType(typeWithoutTrailing);
            
            var questionToken = nullableType.QuestionToken.WithTrailingTrivia(trailingTrivia);
            nullableType = nullableType.WithQuestionToken(questionToken);
            
            var newDeclaration = node.Declaration.WithType(nullableType);
            node = node.WithDeclaration(newDeclaration);
        }

        return base.VisitFieldDeclaration(node);
    }

    public override SyntaxNode? VisitParameter(ParameterSyntax node)
    {
        // Skip if already nullable
        if (node.Type is NullableTypeSyntax)
        {
            return base.VisitParameter(node);
        }
        
        // Use the position to look up the pre-computed decision from analysis phase
        if (nullabilityDecisions.TryGetValue(node.SpanStart, out var shouldBeNullable) && shouldBeNullable)
        {
            // Apply the same trivia handling
            var originalType = node.Type;
            if (originalType == null)
            {
                return base.VisitParameter(node);
            }
            
            var trailingTrivia = originalType.GetTrailingTrivia();
            
            var typeWithoutTrailing = originalType.WithoutTrailingTrivia();
            var nullableType = SyntaxFactory.NullableType(typeWithoutTrailing);
            
            var questionToken = nullableType.QuestionToken.WithTrailingTrivia(trailingTrivia);
            nullableType = nullableType.WithQuestionToken(questionToken);
            
            node = node.WithType(nullableType);
        }

        return base.VisitParameter(node);
    }

    private static bool ShouldPropertyBeNullable(PropertyDeclarationSyntax property, TypeSyntax propertyType, SemanticModel semanticModel)
    {
        // Exclude static properties
        if (property.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return false;
        }

        // Get type information from semantic model
        var typeInfo = semanticModel.GetTypeInfo(propertyType);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol == null)
        {
            // Unable to resolve type, don't modify
            return false;
        }

        // Handle strings - special case for protobuf optional strings
        if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            // Check setter for CheckNotNull - if present, string is required (non-nullable)
            var setter = property.AccessorList?.Accessors
                .FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

            if (setter?.Body != null || setter?.ExpressionBody != null)
            {
                var setterText = setter.ToString();
                if (setterText.Contains("CheckNotNull"))
                {
                    return false; // Required string, keep as non-nullable
                }
            }

            return true; // Optional string (from StringValue wrapper)
        }

        // Handle ByteString - similar to strings
        if (IsByteString(typeSymbol))
        {
            // Check setter for CheckNotNull - if present, ByteString is required (non-nullable)
            var setter = property.AccessorList?.Accessors
                .FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

            if (setter?.Body != null || setter?.ExpressionBody != null)
            {
                var setterText = setter.ToString();
                if (setterText.Contains("CheckNotNull"))
                {
                    return false; // Required ByteString, keep as non-nullable
                }
            }

            return true; // Optional ByteString (from BytesValue wrapper)
        }

        // Exclude RepeatedField and MapField
        if (typeSymbol.Name is "RepeatedField" or "MapField"
            && typeSymbol.ContainingNamespace?.ToDisplayString() == "Google.Protobuf")
        {
            return false;
        }

        // Check if type implements IMessage
        if (ImplementsIMessage(typeSymbol))
        {
            return true;
        }

        // Default to false for everything else (enums, primitive structs, etc.)
        return false;
    }

    private static bool ImplementsIMessage(ITypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(i => 
            i.Name == "IMessage" && 
            i.ContainingNamespace.ToDisplayString() == "Google.Protobuf");
    }

    private static bool IsByteString(ITypeSymbol typeSymbol)
    {
        return typeSymbol.Name == "ByteString"
            && typeSymbol.ContainingNamespace?.ToDisplayString() == "Google.Protobuf";
    }

    private static bool ShouldFieldBeNullable(FieldDeclarationSyntax field, SemanticModel semanticModel)
    {
        // Exclude static fields
        if (field.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return false;
        }

        // Check if this is the _unknownFields field (common in protobuf generated code)
        var variables = field.Declaration.Variables;
        if (variables.Any(variable => variable.Identifier.Text == "_unknownFields"))
        {
            return true;
        }

        // Check for IMessage
        var typeInfo = semanticModel.GetTypeInfo(field.Declaration.Type);
        if (typeInfo.Type != null)
        {
            if (ImplementsIMessage(typeInfo.Type))
            {
                return true;
            }

            // All string backing fields are nullable, even if the properties aren't
            if (typeInfo.Type.SpecialType == SpecialType.System_String)
            {
                return true;
            }

            // ByteStrings as well
            if (IsByteString(typeInfo.Type))
            {
                return true;
            }
    
        }
        
        return false;
    }

    private static bool ShouldParameterBeNullable(ParameterSyntax parameter)
    {
        // Get the containing method
        if (parameter.Parent?.Parent is not MethodDeclarationSyntax method)
        {
            return false;
        }

        var methodName = method.Identifier.Text;

        return methodName switch
        {
            // Equals methods: both object Equals(object other) and bool Equals(TypeName other)
            "Equals" => true,
            // MergeFrom method: void MergeFrom(TypeName other)
            // CodedInputStream should NOT be nullable
            "MergeFrom" when parameter.Type != null && parameter.Type.ToString().Contains("CodedInputStream") => false,
            "MergeFrom" => true,
            _ => false
        };
    }
}
