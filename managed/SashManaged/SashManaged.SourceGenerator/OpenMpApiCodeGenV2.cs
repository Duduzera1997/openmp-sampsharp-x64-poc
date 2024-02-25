﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SashManaged.SourceGenerator.Marshalling;

namespace SashManaged.SourceGenerator;

[Generator]
public class OpenMpApiCodeGenV2 : IIncrementalGenerator
{
    private const string COMMENT_SETUP = "// Setup - Perform required setup.";
    private const string COMMENT_MARSHAL = "// Marshal - Convert managed data to native data.";
    private const string COMMENT_PINNED_MARSHAL = "// PinnedMarshal - Convert managed data to native data that requires the managed data to be pinned.";
    private const string COMMENT_NOTIFY = "// NotifyForSuccessfulInvoke - Keep alive any managed objects that need to stay alive across the call.";
    private const string COMMENT_UNMARSHAL_CAPTURE =
        "// UnmarshalCapture - Capture the native data into marshaller instances in case conversion to managed data throws an exception.";
    private const string COMMENT_UNMARSHAL = "// Unmarshal - Convert native data to managed data.";
    private const string COMMENT_CLEANUP_CALLEE = "// CleanupCalleeAllocated - Perform cleanup of callee allocated resources.";
    private const string COMMENT_CLEANUP_CALLER = "// CleanupCallerAllocated - Perform cleanup of caller allocated resources.";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributedStructs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Constants.ApiAttribute2FQN, 
                static (s, _) => s is StructDeclarationSyntax str && str.IsPartial(), 
                static(ctx, ct) => GetStructDeclaration(ctx, ct))
            .Where(x => x is not null);

        context.RegisterSourceOutput(attributedStructs, (ctx, info) =>
        {
            var node = info.Node;
            var symbol = info.Symbol;

            var structDeclaration = StructDeclaration(node.Identifier)
                .WithModifiers(node.Modifiers)
                .WithMembers(GenerateStructMembers(info));

            var unit = CompilationUnit()
                .AddMembers(NamespaceDeclaration(ParseName(symbol.ContainingNamespace.ToDisplayString()))
                    .AddMembers(structDeclaration))
                .WithLeadingTrivia(Comment("// <auto-generated />"),
                    Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), SingletonSeparatedList<ExpressionSyntax>(IdentifierName("CS8500")), true)));

            var sourceText = unit.NormalizeWhitespace(elasticTrivia: true)
                .GetText(Encoding.UTF8);

            ctx.AddSource($"{symbol.Name}.g.cs", sourceText);
        });
    }

    private static SyntaxList<MemberDeclarationSyntax> GenerateStructMembers(StructDeclaration info)
    {
        return List(
            GenerateCommonStructMembers(info)
                .Concat(
                    info.Methods.Select(x => GenerateMethod(x, info))
                        .Where(x => x != null)
                )
            );
    }

    private static IEnumerable<MemberDeclarationSyntax> GenerateCommonStructMembers(StructDeclaration info)
    {
        var nint = ParseTypeName("nint");
        yield return FieldDeclaration(VariableDeclaration(nint, SingletonSeparatedList(VariableDeclarator("_handle"))))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)));

        yield return ConstructorDeclaration(Identifier(info.Symbol.Name))
            .WithParameterList(ParameterList(
                SingletonSeparatedList(
                Parameter(Identifier("handle")).WithType(nint)
                )))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block(
                SingletonList(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression, 
                            IdentifierName("_handle"),
                            IdentifierName("handle")
                            )
                        )
                    )
                )
            );

        yield return PropertyDeclaration(nint, "Handle")
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithExpressionBody(ArrowExpressionClause(IdentifierName("_handle")))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private static MemberDeclarationSyntax GenerateMethod((MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol) info, StructDeclaration structDeclaration)
    {
        var (node, symbol) = info;

        // Guard: cannot ref return a value that requires marshalling
        var returnMarshaller = GetMarshaller(symbol.ReturnType);
        if (returnMarshaller != null && symbol.ReturnsByRef)
        {
            // Cannot ref return a type that needs marshalling
            // TODO: diagnostic
            return null;
        }

        // TODO: ref return not yet supported
        if (symbol.ReturnsByRef)
        {
            return null;
        }

        var parameters = symbol.Parameters
            .Select(x => (parameter: x, marshaller: MarshallerStrategyFactory.GetMarshaller(x, structDeclaration.WellKnownMarshallerTypes)))
            .ToList();

        var invocation = CreateInvocation(symbol, parameters);
        
        // Extern P/Invoke
        var externReturnType = returnMarshaller?.ToMarshalledType(symbol.ReturnType) ?? 
                               ToTypeSyntax(symbol.ReturnType, symbol.ReturnsByRef, symbol.ReturnsByRefReadonly);

        var externFunction = CreateExternFunction(symbol, externReturnType, parameters);

        invocation = invocation.WithStatements(invocation.Statements.Add(externFunction));
     
        return MethodDeclaration(ToReturnTypeSyntax(symbol), node.Identifier)
            .WithModifiers(node.Modifiers)
            .WithParameterList(ToParameterListSyntax(symbol.Parameters))
            .WithAttributeLists(
                List(
                    new []{
                        AttributeFactory.GeneratedCode(),
                        AttributeFactory.SkipLocalsInit()
                    }
                ))
            .WithBody(invocation);
    }

    private static BlockSyntax CreateInvocation(IMethodSymbol symbol,
        List<(IParameterSymbol parameter, IMarshaller marshaller)> parameters)
    {
        var returnMarshaller = GetMarshaller(symbol.ReturnType);

        var marshallingRequired = returnMarshaller != null || parameters.Any(x => x.marshaller != null);

        return marshallingRequired 
            ? CreateInvocationWithMarshalling(symbol, parameters)
            : CreateInvocationWithoutMarshalling(symbol, parameters);
    }

    private static ArgumentSyntax GetArgumentForParameter(IParameterSymbol parameter, IMarshaller marshaller = null)
    {
        var identifier = parameter.Name;
        if (marshaller != null)
        {
            identifier = $"__{identifier}_native";
        }

        return WithParameterRefKind(Argument(IdentifierName(identifier)), parameter);
    }

    private static ArgumentSyntax WithParameterRefKind(ArgumentSyntax argument, IParameterSymbol parameter)
    {
        switch (parameter.RefKind)
        {
            case RefKind.Ref:
                return argument.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
            case RefKind.Out:
                return argument.WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
            default:
                return argument;
        }
    }
    private static BlockSyntax CreateInvocationWithoutMarshalling(IMethodSymbol symbol, 
        List<(IParameterSymbol parameter, IMarshaller marshaller)> parameters)
    {
        ExpressionSyntax invoke = InvocationExpression(IdentifierName("__PInvoke"))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(Argument(IdentifierName("_handle")))
                        .AddRange(
                            parameters.Select(x => GetArgumentForParameter(x.parameter, x.marshaller)
                            )
                        )
                )
            );

        // No marshalling required, call __PInvoke and return
        if (symbol.ReturnsVoid)
        {
            return Block(ExpressionStatement(invoke));
        }
            
        if (symbol.ReturnsByRef || symbol.ReturnsByRefReadonly)
        {
            invoke = RefExpression(invoke);
        }

        return Block(ReturnStatement(invoke));
    }

    private static BlockSyntax CreateInvocationWithMarshalling(IMethodSymbol symbol,
        List<(IParameterSymbol parameter, IMarshaller marshaller)> parameters)
    {
        var returnMarshaller = GetMarshaller(symbol.ReturnType);

        ExpressionSyntax invoke = 
            InvocationExpression(IdentifierName("__PInvoke"))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                                Argument(IdentifierName("_handle")))
                            .AddRange(
                                parameters.Select(x => GetArgumentForParameter(x.parameter, x.marshaller)))));
        
        if (!symbol.ReturnsVoid)
        {
            // TODO: ref return
            invoke = 
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, 
                    IdentifierName("__retVal"), 
                    invoke);
        }

        // The generated method consists of the following content:
        //
        // LocalsInit - Generate locals for marshalled types and return value
        // Setup - Perform required setup.
        // try
        // {
        //   Marshal - Convert managed data to native data.
        //   {
        //     PinnedMarshal - Convert managed data to native data that requires the managed data to be pinned.
        //     p/invoke 
        //   }
        //   NotifyForSuccessfulInvoke - Keep alive any managed objects that need to stay alive across the call.
        //   UnmarshalCapture - Capture the native data into marshaller instances in case conversion to managed data throws an exception.
        //   Unmarshal - Convert native data to managed data.
        // }
        // finally
        // {
        //   if (invokeSuccess)
        //   {
        //      CleanupCalleeAllocated - Perform cleanup of callee allocated resources.
        //   }
        //   CleanupCallerAllocated - Perform cleanup of caller allocated resources.
        // }
        //
        // return: retval
        //
        // NOTES:
        // - design doc: https://github.com/dotnet/runtime/blob/main/docs/design/libraries/LibraryImportGenerator/UserTypeMarshallingV2.md
        // - we're supporting Default, ManagedToUnmanagedIn, ManagedToUnmanagedOut, ManagedToUnmanagedRef
        // - not implementing element marshalling (arrays) at the moment.
        // - TODO: GetPinnableReference
        // - TODO: guaranteed unmarshalling
        // - TODO: stateful bidirectional
        // - TODO: if(invokeSuccess) around callee cleanup

        // init locals
        var statements = Step(parameters, null, (p, m) => 
            SingletonList<StatementSyntax>(
                CreateLocalDeclarationWithDefaultValue(m.ToMarshalledType(p.Type), $"__{p.Name}_native")));

        if (!symbol.ReturnsVoid)
        {
            var returnType = ToTypeSyntax(symbol.ReturnType);
            statements = statements.Add(CreateLocalDeclarationWithDefaultValue(returnType, "__retVal"));
        }

        if (returnMarshaller != null)
        {
            var nativeType = returnMarshaller.ToMarshalledType(symbol.ReturnType);
            statements = statements.Add(CreateLocalDeclarationWithDefaultValue(nativeType, "__retVal_native"));
        }
        
        // collect all marshalling steps
        // TODO: better return marshalling
        var setup = Step(parameters, COMMENT_SETUP, (p, m) => m.Setup(p));
        var marshal = Step(parameters, COMMENT_MARSHAL, (p, m) => m.Marshal(p));
        var pinnedMarshal = Step(parameters, COMMENT_PINNED_MARSHAL, (p, m) => m.PinnedMarshal(p));
        var notify = Step(parameters, COMMENT_NOTIFY, (p, m) => m.NotifyForSuccessfulInvoke(p));
        var unmarshalCapture = Step(parameters, COMMENT_UNMARSHAL_CAPTURE, (p, m) => m.UnmarshalCapture(p));
        var unmarshal = Step(parameters, COMMENT_UNMARSHAL, (p, m) => m.Unmarshal(p), returnMarshaller?.Unmarshal(null) ?? default);
        var cleanupCallee = Step(parameters, COMMENT_CLEANUP_CALLEE, (p, m) => m.CleanupCalleeAllocated(p));
        var cleanupCaller = Step(parameters, COMMENT_CLEANUP_CALLER, (p, m) => m.CleanupCallerAllocated(p));


        // wire up steps
        var finallyStatements = cleanupCallee.AddRange(cleanupCaller);

        var guarded = marshal
            .Add(
                Block(
                    pinnedMarshal.Add(
                        ExpressionStatement(invoke))))
            .AddRange(notify)
            .AddRange(unmarshalCapture)
            .AddRange(unmarshal);
        
        statements = statements.AddRange(setup);

        if (finallyStatements.Any())
        {
            statements = statements.Add(TryStatement()
                .WithBlock(Block(guarded))
                .WithFinally(
                    FinallyClause(
                        Block(finallyStatements)))
            );
        }
        else
        {
            statements = statements.AddRange(guarded);
        }

        if (!symbol.ReturnsVoid)
        {
            statements = statements.Add(
                ReturnStatement(
                    IdentifierName("__retVal")));
        }

        return Block(statements);

    }

    private static SyntaxList<TNode> Step<TNode>(
        List<(IParameterSymbol parameter, IMarshaller marshaller)> parameters,
        string comment, 
        Func<IParameterSymbol, IMarshaller, SyntaxList<TNode>> marshaller,
        SyntaxList<TNode> additional = default) where TNode : SyntaxNode
    {
        var result = List(parameters.Where(x => x.marshaller != null)
            .SelectMany(x => marshaller(x.parameter, x.marshaller)));

        result = result.AddRange(additional);

        if (comment != null && result.Count > 0)
        {
            result = result.Replace(result[0],
                result[0]
                    .WithLeadingTrivia(Comment(comment)));
        }

        return result;
    }
    
    private static LocalDeclarationStatementSyntax CreateLocalDeclarationWithDefaultValue(TypeSyntax type, string identifier) =>
        LocalDeclarationStatement(
            VariableDeclaration(type,
                SingletonSeparatedList(
                    VariableDeclarator(Identifier(identifier))
                        .WithInitializer(
                            EqualsValueClause(
                                LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword)))))));

    private static LocalFunctionStatementSyntax CreateExternFunction(IMethodSymbol symbol, 
        TypeSyntax externReturnType, 
        IEnumerable<(IParameterSymbol parameter, IMarshaller marshaller)> parameterMarshallers)
    {
        var handleParam = Parameter(Identifier("handle_")).WithType(ParseTypeName("nint"));

        var externParameters = ToParameterListSyntax(handleParam, parameterMarshallers);

        return LocalFunctionStatement(externReturnType, "__PInvoke")
            .WithModifiers(TokenList(          
                Token(SyntaxKind.StaticKeyword),
                Token(SyntaxKind.UnsafeKeyword),
                Token(SyntaxKind.ExternKeyword)
                ))
            .WithParameterList(externParameters)
            .WithAttributeLists(
                SingletonList(
                    AttributeFactory.DllImport("SampSharp", ToExternName(symbol))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(Comment("// Local P/Invoke"));
    }

    private static string ToExternName(IMethodSymbol symbol)
    {
        var type = symbol.ContainingType;

        var overload = symbol.GetAttribute(Constants.OverloadAttributeFQN)?.ConstructorArguments[0].Value as string;
        var functionName = symbol.GetAttribute(Constants.FunctionAttributeFQN)?.ConstructorArguments[0].Value as string;

        if (functionName != null)
        {
            return $"{type.Name}_{functionName}";
        }

        return $"{type.Name}_{FirstLower(symbol.Name)}{overload}";
    }
    
    private static string FirstLower(string value)
    {
        return $"{char.ToLowerInvariant(value[0])}{value.Substring(1)}";
    }

    private static IMarshaller GetMarshaller(ITypeSymbol typeSyntax)
    {
        switch (typeSyntax.SpecialType)
        {
            case SpecialType.System_Boolean:
                return MarshallerStrategyFactory.Boolean;
            case SpecialType.System_String:
                return MarshallerStrategyFactory.String;
        }

        if (typeSyntax.SpecialType != SpecialType.None)
        {
            return null;
        }

        // TODO: check for type marshalling attributes
        return null;
    }

    private static ParameterListSyntax ToParameterListSyntax(ParameterSyntax first, IEnumerable<(IParameterSymbol symbol, IMarshaller marshaller)> parameters)
    {
        return ParameterList(
            SingletonSeparatedList(first)
                .AddRange(
                    parameters
                        .Select(parameter => Parameter(Identifier(parameter.symbol.Name))
                        .WithType(parameter.marshaller?.ToMarshalledType(parameter.symbol.Type) ?? ToTypeSyntax(parameter.symbol.Type))
                        .WithModifiers(GetRefTokens(parameter.symbol.RefKind)))));
    }

    private static SyntaxTokenList GetRefTokens(RefKind refKind)
    {
        return refKind switch
        {
            RefKind.Ref => TokenList(Token(SyntaxKind.RefKeyword)),
            RefKind.Out => TokenList(Token(SyntaxKind.OutKeyword)),
            _ => default
        };
    }
    
    private static TypeSyntax ToReturnTypeSyntax(IMethodSymbol symbol)
    {
        return ToTypeSyntax(symbol.ReturnType, symbol.ReturnsByRef || symbol.ReturnsByRefReadonly);
    }

    public static TypeSyntax ToTypeSyntax(ITypeSymbol symbol, bool isRef = false, bool isReadonly = false)
    {
        var result = ParseTypeName(symbol.SpecialType == SpecialType.None 
            ? $"global::{symbol.ToDisplayString()}" 
            : symbol.ToDisplayString());

        if (isRef)
        {
            result = isReadonly 
                ? RefType(result).WithReadOnlyKeyword(Token(SyntaxKind.ReadOnlyKeyword)) 
                : RefType(result);
        }
        return result;
    }

    private static ParameterListSyntax ToParameterListSyntax(ImmutableArray<IParameterSymbol> parameters)
    {
        return ParameterList(
            SeparatedList(
                parameters.Select(x => 
                    Parameter(Identifier(x.Name))
                        .WithType(ToTypeSyntax(x.Type))
                        .WithModifiers(GetRefTokens(x.RefKind)))
            )
        );
    }

    private static StructDeclaration GetStructDeclaration(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var targetNode = (StructDeclarationSyntax)ctx.TargetNode;
        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
            return null;
        
        var stringViewMarshaller = ctx.SemanticModel.Compilation.GetTypeByMetadataName("SashManaged.StringViewMarshaller");
        var booleanMarshaller = ctx.SemanticModel.Compilation.GetTypeByMetadataName("SashManaged.BooleanMarshaller");

        var wellKnownMarshallerTypes = new WellKnownMarshallerTypes([
            ((x => x.SpecialType == SpecialType.System_String), stringViewMarshaller),
            ((x => x.SpecialType == SpecialType.System_Boolean), booleanMarshaller),
        ]);

        // partial, non-static, non-generic
        List<(MethodDeclarationSyntax, IMethodSymbol)> methods = targetNode.Members.OfType<MethodDeclarationSyntax>()
            .Where(x => x.IsPartial() && !x.HasModifier(SyntaxKind.StaticKeyword) && x.TypeParameterList == null)
            .Select(methodDeclaration => ctx.SemanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is not { } methodSymbol 
                ? (null, null)
                : (methodDeclaration, methodSymbol))
            .Where(x => x.methodSymbol != null)
            .ToList();

        return new StructDeclaration(symbol, targetNode, methods, wellKnownMarshallerTypes);
    }

    private record StructDeclaration(
        ISymbol Symbol,
        StructDeclarationSyntax Node,
        List<(MethodDeclarationSyntax node, IMethodSymbol symbol)> Methods,
        WellKnownMarshallerTypes WellKnownMarshallerTypes);
}