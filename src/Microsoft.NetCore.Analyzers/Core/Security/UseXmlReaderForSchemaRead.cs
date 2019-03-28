﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Security
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class UseXmlReaderForSchemaRead : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "CA5366";
        private static readonly LocalizableString s_Title = new LocalizableResourceString(
            nameof(SystemSecurityCryptographyResources.UseXmlReaderForSchemaRead),
            SystemSecurityCryptographyResources.ResourceManager,
            typeof(SystemSecurityCryptographyResources));
        private static readonly LocalizableString s_Message = new LocalizableResourceString(
            nameof(SystemSecurityCryptographyResources.UseXmlReaderForSchemaReadMessage),
            SystemSecurityCryptographyResources.ResourceManager,
            typeof(SystemSecurityCryptographyResources));
        private static readonly LocalizableString s_Description = new LocalizableResourceString(
            nameof(SystemSecurityCryptographyResources.UseXmlReaderForSchemaReadDescription),
            SystemSecurityCryptographyResources.ResourceManager,
            typeof(SystemSecurityCryptographyResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                DiagnosticId,
                s_Title,
                s_Message,
                DiagnosticCategory.Security,
                DiagnosticHelpers.DefaultDiagnosticSeverity,
                isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
                description: s_Description,
                helpLinkUri: null,
                customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Security analyzer - analyze and report diagnostics on generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                var compilation = compilationStartAnalysisContext.Compilation;
                var dataSetTypeSymbol = compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemDataDataSet);

                if (dataSetTypeSymbol == null)
                {
                    return;
                }

                var xmlReaderTypeSymbol = compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemXmlXmlReader);

                compilationStartAnalysisContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var invocationOperation = (IInvocationOperation)operationAnalysisContext.Operation;
                    var methodSymbol = invocationOperation.TargetMethod;
                    var methodName = methodSymbol.Name;

                    if (methodName.StartsWith("ReadXml", StringComparison.Ordinal) &&
                        MethodOverridenFromDataSet(methodSymbol))
                    {
                        if (xmlReaderTypeSymbol != null &&
                            methodSymbol.Parameters.Length > 0 &&
                            methodSymbol.Parameters[0].Type.Equals(xmlReaderTypeSymbol))
                        {
                            return;
                        }

                        operationAnalysisContext.ReportDiagnostic(
                            invocationOperation.CreateDiagnostic(
                                Rule,
                                methodName));
                    }
                }, OperationKind.Invocation);

                bool MethodOverridenFromDataSet(IMethodSymbol methodSymbol)
                {
                    if (methodSymbol == null)
                    {
                        return false;
                    }
                    else
                    {
                        if (methodSymbol.ContainingType.Equals(dataSetTypeSymbol))
                        {
                            return true;
                        }
                        else
                        {
                            return MethodOverridenFromDataSet(methodSymbol.OverriddenMethod);
                        }
                    }
                }
            });
        }
    }
}
