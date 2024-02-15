﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SashManaged.SourceGenerator;

[Generator]
public class OpenMpApiCodeGen : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var structPovider = context.SyntaxProvider
            .CreateSyntaxProvider(predicate: IsPartialStruct, transform: GetStructDeclaration)
            .WithTrackingName("Syntax");

        context.RegisterSourceOutput(structPovider, (ctx, node) =>
        {
            if (node == null)
            {
                return;
            }

            ctx.AddSource(node.Symbol.Name + ".g.cs", SourceText.From(ProcessStruct(node), Encoding.UTF8));
        });
    }

    private static string ProcessStruct(StructDecl node)
    {
        var attribute = node.Symbol.GetAttributes(Constants.ApiAttributeFQN).Single();
        var implementingTypes = attribute.ConstructorArguments.FirstOrDefault().Values;
        var isComponent = implementingTypes.Any(x => ((ITypeSymbol)x.Value!).ToDisplayString() == Constants.ComponentFQN);
        var isExtension = implementingTypes.Any(x => ((ITypeSymbol)x.Value!).ToDisplayString() == Constants.ExtensionFQN);

        var sb = new StringBuilder();
        sb.AppendLine($$"""
                        /// <auto-generated />

                        namespace {{node.Symbol.ContainingNamespace.ToDisplayString()}}
                        {
                            {{Constants.SequentialStructLayoutAttribute}}
                            {{node.TypeDeclaration.Modifiers}} struct {{node.Symbol.Name}}{{(isComponent ? $" : {Constants.ComponentInterfaceFQN}<{node.Symbol.Name}>" : "")}}{{(isExtension ? $" : {Constants.ExtensionInterfaceFQN}<{node.Symbol.Name}>" : "")}}
                            {
                                private readonly nint _data;
                                
                                public {{node.Symbol.Name}}(nint data)
                                {
                                    _data = data;
                                }
                                
                                public nint Handle => _data;
                                
                                public static {{node.Symbol.Name}} FromHandle(nint handle)
                                {
                                    return new {{node.Symbol.Name}}(handle);
                                }
                        """);

        foreach (var (methodDeclaration, methodSymbol) in node.Methods)
        {
            ProcessMethod(node, methodDeclaration, methodSymbol, sb);
        }

        foreach (var implementingValue in implementingTypes)
        {
            var implementingType = (ITypeSymbol)implementingValue.Value!;
            EmitImplementingType(node, implementingType, sb);
        }

        sb.Append("""
                      }
                  }
                  """);

        return sb.ToString();
    }

    private static void EmitImplementingType(StructDecl node, ITypeSymbol implementingType, StringBuilder sb)
    {
        var methods = implementingType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == MethodKind.Ordinary && !x.IsStatic);

        foreach (var method in methods)
        {
            sb.AppendLine($$"""
                                    public {{(method.ReturnsByRef ? "ref " : "")}}{{method.ReturnType.ToDisplayString()}} {{method.Name}}({{Common.ParameterAsString(method.Parameters)}})
                                    {
                                        var _target = new {{implementingType.ToDisplayString()}}(_data);
                                        {{(method.ReturnsVoid ? "" : "return ")}}_target.{{method.Name}}({{Common.GetForwardArguments(method)}});
                                    }
                                    
                            """);
        }

        sb.AppendLine($$"""
                                public static explicit operator {{node.Symbol.ToDisplayString()}}({{implementingType.ToDisplayString()}} src)
                                {
                                    return new {{node.Symbol.ToDisplayString()}}(src.Handle);
                                }
                                
                                public static implicit operator {{implementingType.ToDisplayString()}}({{node.Symbol.ToDisplayString()}} src)
                                {
                                    return new {{implementingType.ToDisplayString()}}(src.Handle);
                                }

                        """);
    }

    private struct MethodContext
    {
        public StructDecl Node;
        public MethodDeclarationSyntax MethodDeclaration;
        public IMethodSymbol MethodSymbol;
        public string MethodName;
        public string ProxyMethodName;
        public string ReturnType;
        public bool IsVoidReturn;
        public bool IsUnsafe;
        public bool RequiresExternUnsafe;
        public bool RequiresReturnMarshalling;
        public bool RequiresMarshalling;
        public bool RequiresUnsafeWrapper;
        public string ParametersString;
        public string ReturnTypeExtern;
    }

    private static void ProcessMethod(StructDecl node, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, StringBuilder sb)
    {
        var ctx = CreateContext(node, methodDeclaration, methodSymbol);

        // extern
        EmitExternMethod(ctx, sb);

        if (ctx.RequiresUnsafeWrapper)
        {
            EmitMethodWithUnsafeWrapper(ctx, sb);
        }
        else
        {
            EmitDefaultMethod(ctx, sb);
        }
    }

    private static void EmitMethodWithUnsafeWrapper(MethodContext ctx, StringBuilder sb)
    {
        var refModifier = ctx.MethodSymbol.ReturnsByRef ? "ref " : "";

        // unsafe wrapper
        sb.AppendLine($$"""
                                private unsafe {{refModifier}}{{ctx.ReturnType}} {{ctx.MethodName}}_unsafe({{ctx.ParametersString}})
                                {
                                    
                        """);

        // marshalling
        foreach (var parameter in ctx.MethodSymbol.Parameters)
        {
            if (parameter.HasAttribute(Constants.MarshallAttributeFQN))
            {
                sb.AppendLine($$"""
                                            var {{parameter.Name}}_ = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof({{parameter.Type.ToDisplayString()}})));
                                            System.Runtime.InteropServices.Marshal.StructureToPtr({{parameter.Name}}, {{parameter.Name}}_, true);
                                """);
            }
        }
                
        if (ctx.RequiresMarshalling)
        {
            sb.AppendLine("""
                                      try
                                      {
                          """);
        }

        // call to extern
        sb.Append($"                {(ctx.MethodSymbol.ReturnsVoid ? "" : "var returnValue = ")}{ctx.ProxyMethodName}(this");

        if (ctx.MethodDeclaration.ParameterList.Parameters.Count > 0)
        {
            sb.Append(", ");

            sb.Append(Common.GetForwardArguments(ctx.MethodSymbol, ctx.RequiresMarshalling));
        }

        sb.AppendLine(");");

        if (ctx.MethodSymbol.ReturnsByRef)
        {
            sb.AppendLine("            if(returnValue == null) throw new System.NullReferenceException(\"Result is null.\");");
            sb.AppendLine("            return ref *returnValue;");
        }
        else if (ctx.RequiresReturnMarshalling)
        {
                    
            sb.AppendLine($"            return System.Runtime.InteropServices.Marshal.PtrToStructure<{ctx.MethodSymbol.ReturnType.ToDisplayString()}>(returnValue);");
        }
        else if (!ctx.MethodSymbol.ReturnsVoid)
        {
            sb.AppendLine("            return returnValue;");
        }


        if (ctx.RequiresMarshalling)
        {
            // free alloc by marshall
            sb.AppendLine("""
                                      }
                                      finally
                                      {
                          """);

            foreach (var parameter in ctx.MethodSymbol.Parameters)
            {
                if (parameter.HasAttribute(Constants.MarshallAttributeFQN))
                {
                    sb.AppendLine($$"""
                                                    System.Runtime.InteropServices.Marshal.FreeHGlobal({{parameter.Name}}_);
                                    """);
                }
            }

            sb.AppendLine("""
                                      }
                          """);
        }

        sb.AppendLine("        }");
        sb.AppendLine();
                
        sb.AppendLine($$"""
                                {{ctx.MethodDeclaration.Modifiers}} {{refModifier}}{{ctx.ReturnType}} {{ctx.MethodName}}({{ctx.ParametersString}})
                                {
                                    {{(ctx.MethodSymbol.ReturnsVoid ? "" : "return ")}}{{refModifier}}{{ctx.MethodName}}_unsafe({{Common.GetForwardArguments(ctx.MethodSymbol)}});
                                }
                                
                        """);
    }

    private static void EmitDefaultMethod(MethodContext ctx, StringBuilder sb)
    {
        // method declaration
        sb.Append($$"""
                            {{ctx.MethodDeclaration.Modifiers}} {{ctx.ReturnType}} {{ctx.MethodName}}({{ctx.ParametersString}})
                            {
                                
                    """);

        // call to extern
        if (!ctx.IsVoidReturn)
        {
            sb.Append($"return ");
        }

        sb.Append($"{ctx.ProxyMethodName}(this");

        if (ctx.MethodDeclaration.ParameterList.Parameters.Count > 0)
        {
            sb.Append(", ");

            sb.Append(Common.GetForwardArguments(ctx.MethodSymbol));
        }

        sb.AppendLine(");");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static MethodContext CreateContext(StructDecl node, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)
    {
        var ctx = new MethodContext
        {
            Node = node,
            MethodDeclaration = methodDeclaration,
            MethodSymbol = methodSymbol,
            MethodName = methodDeclaration.Identifier.ToString(),
            ReturnType = methodSymbol.ReturnType.ToDisplayString(),
            IsVoidReturn = methodSymbol.ReturnsVoid,
            IsUnsafe = methodDeclaration.Modifiers.Any(x => x.IsKind(SyntaxKind.UnsafeKeyword)),
            RequiresReturnMarshalling = !methodSymbol.ReturnsVoid && methodSymbol.GetReturnTypeAttributes().HasAttribute(Constants.MarshallAttributeFQN),
            RequiresMarshalling = methodSymbol.Parameters.Any(x => x.HasAttribute(Constants.MarshallAttributeFQN)),
            ParametersString = Common.ParameterAsString(methodSymbol.Parameters),
        };

        var overload = methodSymbol.GetAttributes(Constants.OverloadAttributeFQN).FirstOrDefault()?.ConstructorArguments[0].Value as string;
        ctx.ProxyMethodName = $"{node.Symbol.Name}_{FirstLower(ctx.MethodName)}{overload}";
        ctx.RequiresExternUnsafe = ctx.IsUnsafe || methodSymbol.ReturnsByRef;
        ctx.RequiresUnsafeWrapper =  methodSymbol.ReturnsByRef || ctx.RequiresMarshalling || ctx.RequiresReturnMarshalling;
        ctx.ReturnTypeExtern = DetermineExternReturnType(ctx, methodSymbol);
        return ctx;
    }

    private static void EmitExternMethod(MethodContext ctx, StringBuilder sb)
    {
        sb.Append($$"""
                            {{Common.DllImportAttribute("SampSharp")}}
                            private static {{(ctx.RequiresExternUnsafe ? "unsafe " : "")}}extern {{ctx.ReturnTypeExtern}}{{(ctx.MethodSymbol.ReturnsByRef ? "*" : "")}} {{ctx.ProxyMethodName}}({{ctx.Node.Symbol.Name}} ptr
                    """);

        if (ctx.MethodDeclaration.ParameterList.Parameters.Count > 0)
        {
            var parametersStringMarshalled = Common.ParameterAsString(ctx.MethodSymbol.Parameters, ctx.RequiresMarshalling);
            sb.Append($", {parametersStringMarshalled}");
        }

        sb.AppendLine(");");
        sb.AppendLine();
    }

    private static string DetermineExternReturnType(MethodContext ctx, IMethodSymbol methodSymbol)
    {
        if (ctx.RequiresReturnMarshalling)
        {
            return "nint";
        }

        if (methodSymbol.ReturnType is INamedTypeSymbol
            {
                IsGenericType: true
            } returnTypeSymbol)
        {
            var genericType = returnTypeSymbol.OriginalDefinition.ToDisplayString();
            if (genericType == "SashManaged.OpenMp.IEventDispatcher<T>")
            {
                var handlerType = returnTypeSymbol.TypeArguments[0];
                var handlerName = handlerType.Name.Substring(1);
                var handlerNamespace = handlerType.ContainingNamespace.ToDisplayString();
                return $"{handlerNamespace}.IEventDispatcher_{handlerName}";
            }
        }
        
        return methodSymbol.ReturnType.ToDisplayString();
    }


    private static string FirstLower(string value)
    {
        return $"{char.ToLowerInvariant(value[0])}{value.Substring(1)}";
    }
        
    private static bool IsPartialStruct(SyntaxNode syntax, CancellationToken _)
    {
        return syntax is StructDeclarationSyntax
        {
            AttributeLists.Count: > 0
        } structDecl && structDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static StructDecl GetStructDeclaration(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        var structDeclaration = (StructDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(structDeclaration, cancellationToken) is not { } structSymbol)
            return null;
            
        if (!structSymbol.HasAttribute(Constants.ApiAttributeFQN))
            return null;

        var methods = structDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .Where(x => x.Modifiers.Any(y => y.IsKind(SyntaxKind.PartialKeyword)))
            .Select(methodDeclaration =>
            {
                if (ctx.SemanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is not { } methodSymbol)
                    return (null, null);

                return (methodDeclaration, methodSymbol);
            })
            .ToList();

        return new StructDecl(structSymbol, structDeclaration, methods);
    }

    private class StructDecl(ISymbol symbol, StructDeclarationSyntax typeDeclaration, List<(MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)> methods)
    {
        public ISymbol Symbol { get; } = symbol;
        public StructDeclarationSyntax TypeDeclaration { get; } = typeDeclaration;
        public List<(MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)> Methods { get; } = methods;
    }
}