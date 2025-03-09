using SourceGeneratorCommons.CSharpDeclarations;

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

        var csDeclarationProvider = new CsDeclarationProvider(CancellationToken.None);

        {
            var typeReference = csDeclarationProvider.GetTypeReference(openGenericEnumerableTypeSymbol);

            //Assert.Null(definitionWithReference.ReferenceInfo);

            Assert.Equal("IEnumerable", typeReference.TypeDefinition.Name);
            Assert.Equal("global::System.Collections.Generic.IEnumerable<T>", typeReference.TypeDefinition.FullName);
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

            Assert.Equal("IEnumerable", typeReference.TypeDefinition.Name);
            Assert.Equal("global::System.Collections.Generic.IEnumerable<T>", typeReference.TypeDefinition.FullName);

            Assert.Equal("global::System.Collections.Generic.IEnumerable<global::System.Lazy<int>?>?", typeReference.ToString());
            Assert.True(typeReference.IsNullableAnnotated);
            Assert.Single(typeReference.TypeArgs.Values);
            Assert.Single(typeReference.TypeArgs[0].Values);

            var lazyReference = typeReference.TypeArgs[0][0];

            Assert.Equal("Lazy", lazyReference.TypeDefinition.Name);
            Assert.True(lazyReference.IsNullableAnnotated);
            Assert.Single(lazyReference.TypeArgs.Values);
            Assert.Single(lazyReference.TypeArgs[0].Values);

            var intReference = lazyReference.TypeArgs[0][0];

            Assert.Equal("Int32", intReference.TypeDefinition.Name);
            Assert.False(intReference.IsNullableAnnotated);
            Assert.Empty(intReference.TypeArgs.Values);

        }
    }
}
