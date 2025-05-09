﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerator;

CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

var syntaxTreeA = CSharpSyntaxTree.ParseText("""
    public partial class A : B {}
    """, parseOptions);

var syntaxTreeB = CSharpSyntaxTree.ParseText("""
    public partial class B : System.Collections.Generic.IEnumerable<System.Guid> {}
    """, parseOptions);

var syntaxTreeC = CSharpSyntaxTree.ParseText("""
    public partial class C {}
    """, parseOptions);

var syntaxTreeD = CSharpSyntaxTree.ParseText("""
    public partial class D {}
    """, parseOptions);

var refs = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(v => !v.IsDynamic)
    .Where(v => v.Location.Contains("Microsoft.NETCore.App"))
    .Where(v => File.Exists(v.Location))
    .Select(v => MetadataReference.CreateFromFile(v.Location));

var compilation1 = CSharpCompilation.Create("SGTests", [syntaxTreeA, syntaxTreeB, syntaxTreeC, syntaxTreeD], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

var incremental = new IncrementalGenerator();

var driver1 = CSharpGeneratorDriver.Create([incremental.AsSourceGenerator()], driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

CancellationTokenSource cts = new CancellationTokenSource();

var driver2 = driver1.RunGeneratorsAndUpdateCompilation(compilation1, out var outputCompilation1, out _, cts.Token);

var syntaxTree_newB = CSharpSyntaxTree.ParseText("""
    public partial class B : System.Collections.Generic.IEnumerable<int>{}
    """, parseOptions);

var syntaxTree_newC = CSharpSyntaxTree.ParseText("""
    public partial class C : System.Collections.Generic.IEnumerable<int>{}
    """, parseOptions);

var compilation2 = compilation1
    .ReplaceSyntaxTree(syntaxTreeB, syntaxTree_newB)
    .ReplaceSyntaxTree(syntaxTreeC, syntaxTree_newC);

var driver3 = driver2.RunGeneratorsAndUpdateCompilation(compilation2, out var outputCompilation2, out _);


var result = driver3.GetRunResult();

;