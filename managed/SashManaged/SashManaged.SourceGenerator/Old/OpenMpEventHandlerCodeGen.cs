﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SashManaged.SourceGenerator.Old
{
    [Generator]
    public class OpenMpEventHandlerCodeGen : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var structPovider = context.SyntaxProvider
                .ForAttributeWithMetadataName(Constants.EventHandlerAttributeFQN,
                    static (s, _) => s is InterfaceDeclarationSyntax,
                    GetEventHandlerDeclaration)
                .WithTrackingName("Syntax");

            context.RegisterSourceOutput(structPovider, (ctx, node) =>
            {
                if (node == null)
                {
                    return;
                }

                ctx.AddSource(node.Symbol.Name + ".g.cs", SourceText.From(Process(node), Encoding.UTF8));
            });
        }

        private static EventHandlerDeclaration? GetEventHandlerDeclaration(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var declaration = (InterfaceDeclarationSyntax)ctx.TargetNode;
            if (ctx.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol)
                return null;

            var attribute = ctx.Attributes.Single();

            var handlerName = attribute.NamedArguments.FirstOrDefault(x => x.Key == "HandlerName").Value.Value as string ?? symbol.Name.Substring(1);

            var members = symbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Concat(symbol.AllInterfaces
                    .SelectMany(x => x.GetMembers())
                    .OfType<IMethodSymbol>())
                .ToList();

            return new EventHandlerDeclaration(symbol, members, handlerName);
        }

        private static string Process(EventHandlerDeclaration node)
        {
            var sb = new StringBuilder();

            sb.AppendLine($$"""
                            /// <auto-generated />
                            
                            #nullable enable
                            
                            namespace {{node.Symbol.ContainingNamespace.ToDisplayString()}}
                            {
                            """);

            // Generate EventHandler
            EmitEventHandler(node, sb, node.HandlerName);

            // Generate Dispatcher
            EmitEventDispatcher(node, sb, node.HandlerName);


            return sb.ToString();
        }

        private static void EmitEventDispatcher(EventHandlerDeclaration node, StringBuilder sb, string handlerName)
        {
            var dispatcherName = $"IEventDispatcher_{node.Symbol.Name.Substring(1)}";
            var nativeDispatcherName = $"IEventDispatcher_{handlerName}";

            sb.AppendLine($$"""
                                {{Constants.SequentialStructLayoutAttribute}}
                                internal struct {{dispatcherName}} : SashManaged.OpenMp.IEventDispatcher<{{node.Symbol.Name}}>
                                {
                                    private readonly nint _data;
                                    
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern bool {{nativeDispatcherName}}_addEventHandler({{dispatcherName}} dispatcher, nint handler, EventPriority priority);
                                    
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern bool {{nativeDispatcherName}}_removeEventHandler({{dispatcherName}} dispatcher, nint handler);
                                        
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern bool {{nativeDispatcherName}}_hasEventHandler({{dispatcherName}} dispatcher, nint handler, out EventPriority priority);
                                    
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern {{Constants.SizeFQN}} {{nativeDispatcherName}}_count({{dispatcherName}} dispatcher);
                                   
                                    public bool AddEventHandler({{node.Symbol.Name}} handler, EventPriority priority = EventPriority.Default)
                                    {
                                        var active = {{node.Symbol.Name}}_Handler.Activate(handler);
                                    
                                        if (!{{nativeDispatcherName}}_addEventHandler(this, active.Handle, priority))
                                        {
                                            return false;
                                        }
                                    
                                        var dispatcher = this;
                                        active.Disposing += (sender, e) => dispatcher.RemoveEventHandler(handler);
                                    
                                        return true;
                                    }
                                    
                                    public bool RemoveEventHandler({{node.Symbol.Name}} handler)
                                    {
                                        if ({{node.Symbol.Name}}_Handler.Active != handler)
                                        {
                                            return false;
                                        }
                                    
                                        return {{nativeDispatcherName}}_removeEventHandler(this, {{node.Symbol.Name}}_Handler.ActiveHandle!.Value);
                                    }
                                    
                                    public bool HasEventHandler({{node.Symbol.Name}} handler, out EventPriority priority)
                                    {
                                        if ({{node.Symbol.Name}}_Handler.Active != handler)
                                        {
                                            priority = default;
                                            return false;
                                        }
                                    
                                        return {{nativeDispatcherName}}_hasEventHandler(this, {{node.Symbol.Name}}_Handler.ActiveHandle!.Value, out priority);
                                    }
                                    
                                    public {{Constants.SizeFQN}} Count()
                                    {
                                        return {{nativeDispatcherName}}_count(this);
                                    }
                                }
                            }
                            """);
        }

        private static void EmitEventHandler(EventHandlerDeclaration node, StringBuilder sb, string handlerName)
        {
            sb.AppendLine($$"""
                                internal class {{node.Symbol.Name}}_Handler : SashManaged.BaseEventHandler<{{node.Symbol.Name}}>
                                {
                                
                            """);

            // all members...
            foreach (var method in node.Members)
            {
                sb.AppendLine($$"""
                                        [System.Runtime.InteropServices.UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvStdcall) })]
                                        private static {{Common.ToBlittableTypeString(method.ReturnType)}} {{method.Name}}({{Common.ParameterAsString(method.Parameters, blittable: true)}})
                                        {
                                            {{(!method.ReturnsVoid ? "return " : "")}}Active?.{{method.Name}}({{Common.GetForwardArguments(method, blittable: true)}}){{(!method.ReturnsVoid ? " ?? default" : "")}};
                                        }
                                        
                                """);

            }

            sb.AppendLine($$"""
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern unsafe nint {{handlerName}}Impl_create(
                            """);

            var count = 0;
            foreach (var method in node.Members)
            {
                sb.Append("delegate* unmanaged[Stdcall]<");

                sb.Append(string.Join(", ", method.Parameters.Select(GetBlittableTypeStringForDelegate)));

                if (method.Parameters.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendLine($"{Common.ToBlittableTypeString(method.ReturnType)}> _{method.Name}{(++count != node.Members.Count ? ", " : "")}");
            }

            sb.AppendLine($$"""
                                    );
                                    
                                    private {{node.Symbol.Name}}_Handler({{node.Symbol.Name}} handler, nint handle) : base(handler, handle)
                                    {
                                    }
                            
                                    {{Common.DllImportAttribute("SampSharp")}}
                                    private static extern void {{handlerName}}Impl_delete(nint ptr);
                                    
                                    protected override void Delete()
                                    {
                                        {{handlerName}}Impl_delete(Handle);
                                    }
                                    
                                    public static unsafe {{node.Symbol.Name}}_Handler Activate({{node.Symbol.Name}} handler)
                                    {
                                        if (Active == handler)
                                        {
                                            return ({{node.Symbol.Name}}_Handler)ActiveHandler!;
                                        }
                                    
                                        ThrowIfActive();
                                    
                                        var handle = {{handlerName}}Impl_create({{string.Join(", ", node.Members.Select(x => $"&{x.Name}"))}});
                                        return new {{node.Symbol.Name}}_Handler(handler, handle);
                                    }
                                }
                            """);
        }

        private static string GetBlittableTypeStringForDelegate(IParameterSymbol x)
        {
            if (x.RefKind == RefKind.Ref)
            {
                return "nint";
            }
            return $"{Common.RefArgumentString(x.RefKind)}{Common.ToBlittableTypeString(x.Type)}";
        }

        private class EventHandlerDeclaration(INamedTypeSymbol symbol, List<IMethodSymbol> members, string handlerName)
        {
            public string HandlerName { get; } = handlerName;
            public INamedTypeSymbol Symbol { get; } = symbol;
            public List<IMethodSymbol> Members { get; } = members;
        }
    }
}
