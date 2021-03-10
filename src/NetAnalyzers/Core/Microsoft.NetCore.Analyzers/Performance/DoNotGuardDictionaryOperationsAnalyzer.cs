// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotGuardDictionaryOperationsAnalyzer : DiagnosticAnalyzer
    {
        public const string DoNotGuardRemoveByContainsKeyId = "CA1839";
        public const string DoNotGuardIndexerAccessByContainsKeyId = "CA1840";
        public const string DoNotGuardAddByContainsKeyId = "CA1841";

        private static readonly LocalizableString s_localizableTitle =
            CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyTitle));

        private static readonly LocalizableString
            s_localizableMessage = CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyMessage));

        private static readonly LocalizableString s_localizableDescription =
            CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyDescription));

        internal static readonly DiagnosticDescriptor DoNotGuardRemoveByContainsKeyRule = DiagnosticDescriptorHelper.Create(
            DoNotGuardRemoveByContainsKeyId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static readonly DiagnosticDescriptor DoNotGuardIndexerAccessByContainsKeyRule = DiagnosticDescriptorHelper.Create(
            DoNotGuardIndexerAccessByContainsKeyId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static readonly DiagnosticDescriptor DoNotGuardAddByContainsKeyRule = DiagnosticDescriptorHelper.Create(
            DoNotGuardAddByContainsKeyId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        private const string ContainsKeyMethodName = nameof(IDictionary<dynamic, dynamic>.ContainsKey);
        internal const string AddMethodName = nameof(IDictionary.Add);
        internal const string RemoveMethodName = nameof(IDictionary.Remove);
        private const string IndexerName = "this[]";
        private const string IndexerNameVb = "Item";

        private static readonly Dictionary<Func<IOperation, ISymbol, bool>, DiagnosticDescriptor> DiagnosticsByCondition = new(3)
        {
            [(operation, dictionaryType) => operation is IPropertyReferenceOperation propertyReference && IsDictionaryType(propertyReference.Property.ContainingType, dictionaryType) && propertyReference.Property.IsIndexer && (propertyReference.Property.OriginalDefinition.Name == IndexerName || propertyReference.Language == "Visual Basic" && propertyReference.Property.OriginalDefinition.Name == IndexerNameVb)] = DoNotGuardIndexerAccessByContainsKeyRule,
            [(operation, dictionaryType) => operation is IInvocationOperation invocation && IsDictionaryType(invocation.TargetMethod.ContainingType, dictionaryType) && invocation.TargetMethod.Name == AddMethodName] = DoNotGuardAddByContainsKeyRule,
            [(operation, dictionaryType) => operation is IInvocationOperation invocation && IsDictionaryType(invocation.TargetMethod.ContainingType, dictionaryType) && invocation.TargetMethod.Name == RemoveMethodName] = DoNotGuardRemoveByContainsKeyRule
        };

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            if (!compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIDictionary2, out var dictionaryType))
            {
                return;
            }

            compilationContext.RegisterOperationAction(context => OnOperationAction(context, dictionaryType), OperationKind.Invocation);
        }

        private static void OnOperationAction(OperationAnalysisContext context, INamedTypeSymbol dictionaryType)
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (!IsDictionaryContainsKeyInvocation(invocation, dictionaryType) || !TryGetParentConditionalOperation(invocation, out var conditionalOperation) || !TryGetDiagnostic(conditionalOperation.WhenTrue, dictionaryType, out var diagnostic, out var location))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, invocation.Syntax.GetLocation(), ImmutableArray.Create(location)));
        }

        private static bool TryGetDiagnostic(IOperation whenTrueOperation, ISymbol dictionaryType, [NotNullWhen(true)] out DiagnosticDescriptor? diagnostic, [NotNullWhen(true)] out Location? location)
        {
            location = default;
            diagnostic = default;

            ImmutableArray<IOperation> operations = whenTrueOperation is IBlockOperation blockOperation ? blockOperation.Operations : ImmutableArray.Create(whenTrueOperation);
            foreach (var operation in operations)
            {
                foreach (var keyValuePair in DiagnosticsByCondition)
                {
                    if (operation.HasAnyOperationDescendant(op => keyValuePair.Key(op, dictionaryType), out var diagnostableOperation))
                    {
                        diagnostic = keyValuePair.Value;
                        location = diagnostableOperation.Syntax.GetLocation();

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetParentConditionalOperation(IOperation derivedOperation, [NotNullWhen(true)] out IConditionalOperation? conditionalOperation)
        {
            conditionalOperation = null;
            do
            {
                if (derivedOperation.Parent is IConditionalOperation c)
                {
                    conditionalOperation = c;

                    return true;
                }

                derivedOperation = derivedOperation.Parent;
            } while (derivedOperation.Parent != null);

            return false;
        }

        private static bool IsDictionaryContainsKeyInvocation(IInvocationOperation invocationOperation, INamedTypeSymbol dictionaryType)
        {
            return IsDictionaryType(invocationOperation.TargetMethod.ContainingType, dictionaryType) && invocationOperation.TargetMethod.Name == ContainsKeyMethodName;
        }

        private static bool IsDictionaryType(INamedTypeSymbol derived, ISymbol dictionaryType)
        {
            var constructedDictionaryType = derived.GetBaseTypesAndThis()
                .WhereAsArray(x => x.OriginalDefinition.Equals(dictionaryType, SymbolEqualityComparer.Default))
                .SingleOrDefault() ?? derived.AllInterfaces
                .WhereAsArray(x => x.OriginalDefinition.Equals(dictionaryType, SymbolEqualityComparer.Default))
                .SingleOrDefault();

            return constructedDictionaryType is not null;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DoNotGuardRemoveByContainsKeyRule, DoNotGuardIndexerAccessByContainsKeyRule, DoNotGuardAddByContainsKeyRule);

        private static LocalizableString CreateResource(string resourceName)
        {
            return new LocalizableResourceString(resourceName, MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        }
    }
}