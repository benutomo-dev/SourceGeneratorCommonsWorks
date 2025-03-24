using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ShouldMethodAssertion.ShouldExtensions;
using SourceGeneratorCommons.CSharp.Declarations;

namespace SourceGeneratorCommons.Tests;

public class GetTypeReferenceTests
{
    [Theory]
    [CombinatorialData]
    public void 組み込みの値型(bool nullable)
    {
        var metadataName = "Class";
        var methodName = "Method";
        var source = $$"""
            class Class
            {
                int{{(nullable ? "?" : "")}} Method() => default!;
            }
            """;

        var type = GetTypeReference(metadataName, methodName, source);

        if (nullable)
        {
            type.InternalReference.Should().Be("int?");
            type.GlobalReference.Should().Be("int?");
            type.Cref.Should().Be("System.Nullable{int}");

            type.IsNullable.Should().BeFalse();

            type.Type.TypeDefinition.Should().BeOfType<CsStruct>();
            type.Type.TypeDefinition.Is(CsSpecialType.NullableT).Should().BeTrue();


            type.Type.TypeArgs.Values.Length.Should().Be(1);
            type.Type.TypeArgs.Values[0].Length.Should().Be(1);

            var typeArg1 = type.Type.TypeArgs.Values[0][0];

            typeArg1.Type.TypeDefinition.Should().BeOfType<CsStruct>();
            typeArg1.Type.TypeDefinition.Is(CsSpecialType.Int).Should().BeTrue();
        }
        else
        {
            type.InternalReference.Should().Be("int");
            type.GlobalReference.Should().Be("int");
            type.Cref.Should().Be("int");

            type.IsNullable.Should().BeFalse();

            type.Type.TypeDefinition.Should().BeOfType<CsStruct>();
            type.Type.TypeDefinition.Is(CsSpecialType.Int).Should().BeTrue();

            type.Type.TypeArgs.Values.Length.Should().Be(1);
            type.Type.TypeArgs.Values[0].Length.Should().Be(0);
        }
    }

    [Theory]
    [CombinatorialData]
    public void 組み込みの参照型(bool nullable)
    {
        var metadataName = "Class";
        var methodName = "Method";
        var source = $$"""
            class Class
            {
                string{{(nullable ? "?": "")}} Method() => default!;
            }
            """;

        var type = GetTypeReference(metadataName, methodName, source);

        if (nullable)
        {
            type.InternalReference.Should().Be("string?");
            type.GlobalReference.Should().Be("string?");
            type.Cref.Should().Be("string");

            type.IsNullable.Should().BeTrue();
        }
        else
        {
            type.InternalReference.Should().Be("string");
            type.GlobalReference.Should().Be("string");
            type.Cref.Should().Be("string");

            type.IsNullable.Should().BeFalse();
        }

        type.Type.TypeDefinition.Should().BeOfType<CsClass>();
        type.Type.TypeDefinition.Is(CsSpecialType.String).Should().BeTrue();

        type.Type.TypeArgs.Values.Length.Should().Be(1);
        type.Type.TypeArgs.Values[0].Length.Should().Be(0);
    }

    [Fact]
    public void ジェネリック型()
    {
        var metadataName = "Class`2";
        var methodName = "Method";
        var source = $$"""
            class Class<TypeArg1, TypeArg2>
                where TypeArg1 : struct, global::System.IEquatable<TypeArg1>
            {
                global::System.Collections.Generic.IDictionary<TypeArg1, TypeArg2?> Method() => default!;
            }
            """;

        var type = GetTypeReference(metadataName, methodName, source);

        type.InternalReference.Should().Be("IDictionary<TypeArg1,TypeArg2?>");
        type.GlobalReference.Should().Be("global::System.Collections.Generic.IDictionary<TypeArg1,TypeArg2?>");
        type.Cref.Should().Be("System.Collections.Generic.IDictionary{TypeArg1,TypeArg2}");

        type.IsNullable.Should().BeFalse();

        type.Type.TypeDefinition.Should().BeOfType<CsInterface>();

        type.Type.TypeArgs.Values.Length.Should().Be(1);
        type.Type.TypeArgs.Values[0].Length.Should().Be(2);

        var typeArg1 = type.Type.TypeArgs.Values[0][0].Type.TypeDefinition.Should().BeOfType<CsTypeParameterDeclaration>();
        var typeArg2 = type.Type.TypeArgs.Values[0][1].Type.TypeDefinition.Should().BeOfType<CsTypeParameterDeclaration>();

        typeArg1.Name.Should().Be("TypeArg1");
        typeArg1.Where.IsAny.Should().BeFalse();
        typeArg1.Where.TypeCategory.Should().Be(CsGenericConstraintTypeCategory.Struct);
        typeArg1.Where.HaveDefaultConstructor.Should().BeFalse();
        typeArg1.Where.BaseType.Should().BeNull();
        typeArg1.Where.Interfaces.Values.Length.Should().Be(1);

        typeArg1.Where.Interfaces.Values[0].GlobalReference.Should().Be("global::System.IEquatable<TypeArg1>");

        var equatableInterfaceTypeArg1 = typeArg1.Where.Interfaces.Values[0].TypeArgs[0][0].Type.TypeDefinition.Should().BeOfType<CsTypeParameterDeclaration>();
        equatableInterfaceTypeArg1.Should().SameReferenceAs(typeArg1);

        typeArg2.Name.Should().Be("TypeArg2");
        typeArg2.Where.IsAny.Should().BeTrue();
    }

    private static CsTypeRefWithAnnotation GetTypeReference(string metadataName, string methodName, string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        var compilation = UnitTestCompiler.Default.AddSyntaxTrees(syntaxTree);

        var typeSymbol = compilation.GetTypeByMetadataName(metadataName)!
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(v => v.Name == methodName)
            .Select(v => v.ReturnType)
            .Single();

        var type = new CsDeclarationProvider(compilation, CancellationToken.None).GetTypeReference(typeSymbol);

        return type;
    }
}
