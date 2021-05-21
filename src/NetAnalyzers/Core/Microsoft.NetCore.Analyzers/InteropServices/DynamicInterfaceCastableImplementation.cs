﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.InteropServices
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class DynamicInterfaceCastableImplementationAnalyzer : DiagnosticAnalyzer
    {
        internal const string DynamicInterfaceCastableImplementationUnsupportedRuleId = "CA2250";

        private static readonly DiagnosticDescriptor DynamicInterfaceCastableImplementationUnsupported =
            DiagnosticDescriptorHelper.Create(
                DynamicInterfaceCastableImplementationUnsupportedRuleId,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DynamicInterfaceCastableImplementationUnsupportedTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DynamicInterfaceCastableImplementationUnsupportedMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                DiagnosticCategory.Usage,
                RuleLevel.BuildWarning,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DynamicInterfaceCastableImplementationUnsupportedDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                isPortedFxCopRule: false,
                isDataflowRule: false);

        internal const string InterfaceMethodsMissingImplementationRuleId = "CA2251";

        private static readonly DiagnosticDescriptor InterfaceMethodsMissingImplementation =
            DiagnosticDescriptorHelper.Create(
                InterfaceMethodsMissingImplementationRuleId,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.InterfaceMethodsMissingImplementationTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.InterfaceMethodsMissingImplementationMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                DiagnosticCategory.Usage,
                RuleLevel.BuildWarning,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.InterfaceMethodsMissingImplementationDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                isPortedFxCopRule: false,
                isDataflowRule: false);

        internal const string MethodsDeclaredOnImplementationTypeMustBeSealedRuleId = "CA2252";

        private static readonly DiagnosticDescriptor MethodsDeclaredOnImplementationTypeMustBeSealed =
            DiagnosticDescriptorHelper.Create(
                MethodsDeclaredOnImplementationTypeMustBeSealedRuleId,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.MethodsDeclaredOnImplementationTypeMustBeSealedTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.MethodsDeclaredOnImplementationTypeMustBeSealedMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                DiagnosticCategory.Usage,
                RuleLevel.BuildWarning,
                new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.MethodsDeclaredOnImplementationTypeMustBeSealedDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
                isPortedFxCopRule: false,
                isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DynamicInterfaceCastableImplementationUnsupported,
            InterfaceMethodsMissingImplementation,
            MethodsDeclaredOnImplementationTypeMustBeSealed);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSymbolAction(context => AnalyzeType(context), SymbolKind.NamedType);
        }

        private const string DynamicInterfaceCastableImplementationAttributeTypeName = "System.Runtime.InteropServices.DynamicInterfaceCastableImplementationAttribute";

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            INamedTypeSymbol targetType = (INamedTypeSymbol)context.Symbol;

            if (targetType.TypeKind != TypeKind.Interface)
            {
                return;
            }

            bool isDynamicInterfaceImplementation = false;
            foreach (var attribute in targetType.GetAttributes())
            {
                if (attribute.AttributeClass.ToDisplayString(SymbolDisplayFormats.QualifiedTypeAndNamespaceSymbolDisplayFormat) == DynamicInterfaceCastableImplementationAttributeTypeName)
                {
                    isDynamicInterfaceImplementation = true;
                    break;
                }
            }

            if (!isDynamicInterfaceImplementation)
            {
                return;
            }

            // Default Interface Methods are required to provide an IDynamicInterfaceCastable implementation type.
            // Since Visual Basic does not support DIMs, an implementation type cannot be correctly provided in VB.
            if (context.Compilation.Language == LanguageNames.VisualBasic)
            {
                context.ReportDiagnostic(targetType.CreateDiagnostic(DynamicInterfaceCastableImplementationUnsupported));
                return;
            }

            bool missingMethodImplementations = false;
            foreach (var iface in targetType.AllInterfaces)
            {
                foreach (var member in iface.GetMembers())
                {
                    if (!member.IsStatic && targetType.FindImplementationForInterfaceMember(member) is null)
                    {
                        missingMethodImplementations = true;
                        break;
                    }
                }
            }

            if (missingMethodImplementations)
            {
                context.ReportDiagnostic(targetType.CreateDiagnostic(InterfaceMethodsMissingImplementation, targetType.ToDisplayString()));
            }

            foreach (var member in targetType.GetMembers())
            {
                if (member.IsVirtual || member.IsAbstract)
                {
                    // Emit diagnostic for non-concrete method on implementation interface
                    context.ReportDiagnostic(member.CreateDiagnostic(MethodsDeclaredOnImplementationTypeMustBeSealed, member.ToDisplayString(), targetType.ToDisplayString()));
                }
            }
        }
    }
}
