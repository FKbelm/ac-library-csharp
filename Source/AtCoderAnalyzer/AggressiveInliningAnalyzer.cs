﻿using System;
using System.Collections.Immutable;
using System.Linq;
using AtCoderAnalyzer.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using static AtCoderAnalyzer.Helpers.Constants;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;

namespace AtCoderAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AggressiveInliningAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DiagnosticDescriptors.AC0007_AgressiveInlining_Descriptor);

        private class ContainingOperatorTypes
        {
            public INamedTypeSymbol MethodImplAttribute { get; }
            public INamedTypeSymbol IsOperatorAttribute { get; }

            public ContainingOperatorTypes(INamedTypeSymbol methodImpl, INamedTypeSymbol isOperator)
            {
                MethodImplAttribute = methodImpl;
                IsOperatorAttribute = isOperator;
            }
            public static bool TryParseTypes(Compilation compilation, out ContainingOperatorTypes types)
            {
                types = null;
                var methodImpl = compilation.GetTypeByMetadataName(System_Runtime_CompilerServices_MethodImplAttribute);
                if (methodImpl is null)
                    return false;
                var isOperator = compilation.GetTypeByMetadataName(AtCoder_IsOperatorAttribute);
                if (isOperator is null)
                    return false;
                types = new ContainingOperatorTypes(methodImpl, isOperator);
                return true;
            }
        }
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                if (ContainingOperatorTypes.TryParseTypes(compilationStartContext.Compilation, out var types))
                {
                    compilationStartContext.RegisterSyntaxNodeAction(
                        c => AnalyzeTypeDecra(c, types),
                        SyntaxKind.StructDeclaration, SyntaxKind.ClassConstraint);
                }
            });
        }

        private void AnalyzeTypeDecra(SyntaxNodeAnalysisContext context, ContainingOperatorTypes types)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node, context.CancellationToken)
                is not INamedTypeSymbol symbol)
                return;
            var concurrentBuild = context.Compilation.Options.ConcurrentBuild;

            bool HasIsOperatorAttribute(INamedTypeSymbol symbol)
            {
                foreach (var at in symbol.ConstructedFrom.GetAttributes())
                    if (SymbolEqualityComparer.Default.Equals(at.AttributeClass, types.IsOperatorAttribute))
                        return true;
                return false;
            }

            if (concurrentBuild)
            {
                if (symbol.AllInterfaces
                    .AsParallel(context.CancellationToken)
                    .Any(HasIsOperatorAttribute))
                    goto HasIsOperator;
            }
            else
            {
                if (symbol.AllInterfaces
                    .Do(_ => context.CancellationToken.ThrowIfCancellationRequested())
                    .Any(HasIsOperatorAttribute))
                    goto HasIsOperator;
            }
            return;
        HasIsOperator:

            bool DoesNotHaveMethodImplInlining(IMethodSymbol m)
            {
                if (m.MethodKind is
                     not (MethodKind.ExplicitInterfaceImplementation or MethodKind.Ordinary))
                    return false;

                if (m.GetAttributes()
                       .FirstOrDefault(at => SymbolEqualityComparer.Default.Equals(at.AttributeClass, types.MethodImplAttribute)) is not { } attr
                   || attr.ConstructorArguments.Length == 0)
                    return true;

                var arg = attr.ConstructorArguments[0];
                if (arg.Kind is TypedConstantKind.Primitive or TypedConstantKind.Enum)
                    try
                    {
                        return !((MethodImplOptions)Convert.ToInt32(arg.Value)).HasFlag(MethodImplOptions.AggressiveInlining);
                    }
                    catch
                    {
                        return true;
                    }
                return true;
            }

            string[] notMethodImplInliningMethods;
            if (concurrentBuild)
                notMethodImplInliningMethods = symbol.GetMembers()
                    .AsParallel(context.CancellationToken)
                    .OfType<IMethodSymbol>()
                    .Where(DoesNotHaveMethodImplInlining)
                    .Select(m => m.Name)
                    .ToArray();
            else
                notMethodImplInliningMethods = symbol.GetMembers()
                    .Do(_ => context.CancellationToken.ThrowIfCancellationRequested())
                    .OfType<IMethodSymbol>()
                    .Where(DoesNotHaveMethodImplInlining)
                    .Select(m => m.Name)
                    .ToArray();
            if (notMethodImplInliningMethods.Length == 0)
                return;

            var diagnostic = DiagnosticDescriptors.AC0007_AgressiveInlining(
                context.Node.GetLocation(), notMethodImplInliningMethods);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
