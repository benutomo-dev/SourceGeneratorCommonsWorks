using SourceGeneratorCommons.CSharp.Declarations;

namespace SourceGeneratorCommons.Tests;

public class BuildTypeDefinitionInfoTests
{
    [Fact]
    public void X()
    {
        var compilation = UnitTestCompiler.Default;

        var openGenericEnumerableTypeSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

        Assert.NotNull(openGenericEnumerableTypeSymbol);

        var classA = new CsClass(
            new CsNameSpace("My.Name"),
            "MyClass",
            accessibility: CsAccessibility.Public,
            classModifier: CsClassModifier.Default
            );

        var csDeclarationProvider = new CsDeclarationProvider(compilation, CancellationToken.None);

        {
            var typeReference = csDeclarationProvider.GetTypeReference(openGenericEnumerableTypeSymbol);

            //Assert.Null(definitionWithReference.ReferenceInfo);

            Assert.Equal("IEnumerable", typeReference.Type.TypeDefinition.Name);
            Assert.Equal("global::System.Collections.Generic.IEnumerable<T>", typeReference.Type.TypeDefinition.FullName);
        }

        var intTypeSymbol = compilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32);

        var openGenericLazyTypeSymbol = compilation.GetTypeByMetadataName("System.Lazy`1");

        Assert.NotNull(openGenericLazyTypeSymbol);

        var nullableLazyIntTypeSymbol = openGenericLazyTypeSymbol
            .Construct(intTypeSymbol)
            .WithNullableAnnotation(Microsoft.CodeAnalysis.NullableAnnotation.Annotated);

        var nullableLazyIntGenericEnumerableTypeSymbol = openGenericEnumerableTypeSymbol
            .Construct(nullableLazyIntTypeSymbol)
            .WithNullableAnnotation(Microsoft.CodeAnalysis.NullableAnnotation.Annotated);

        {
            var typeReference = csDeclarationProvider.GetTypeReference(nullableLazyIntGenericEnumerableTypeSymbol);

            Assert.Equal("IEnumerable", typeReference.Type.TypeDefinition.Name);
            Assert.Equal("global::System.Collections.Generic.IEnumerable<T>", typeReference.Type.TypeDefinition.FullName);

            Assert.Equal("global::System.Collections.Generic.IEnumerable<global::System.Lazy<int>?>?", typeReference.GlobalReference);
            Assert.Equal("IEnumerable<global::System.Lazy<int>?>?", typeReference.InternalReference);
            Assert.Equal("System.Collections.Generic.IEnumerable{System.Lazy{int}}", typeReference.Cref);
            Assert.True(typeReference.IsNullable);
            Assert.Single(typeReference.Type.TypeArgs.Values);
            Assert.Single(typeReference.Type.TypeArgs[0].Values);

            var lazyReference = typeReference.Type.TypeArgs[0][0];

            Assert.Equal("Lazy", lazyReference.Type.TypeDefinition.Name);
            Assert.True(lazyReference.IsNullable);
            Assert.Single(lazyReference.Type.TypeArgs.Values);
            Assert.Single(lazyReference.Type.TypeArgs[0].Values);

            var intReference = lazyReference.Type.TypeArgs[0][0];

            Assert.Equal("Int32", intReference.Type.TypeDefinition.Name);
            Assert.False(intReference.IsNullable);
            Assert.Empty(intReference.Type.TypeArgs.Values);

        }
    }
}
