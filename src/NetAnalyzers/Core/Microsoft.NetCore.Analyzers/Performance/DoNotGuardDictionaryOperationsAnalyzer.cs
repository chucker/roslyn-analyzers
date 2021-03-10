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

        private static readonly LocalizableString s_localizableTitle = CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyTitle));

        private static readonly LocalizableString s_localizableMessage = CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyMessage));

        private static readonly LocalizableString s_localizableDescription = CreateResource(nameof(MicrosoftNetCoreAnalyzersResources.DoNotGuardDictionaryRemoveByContainsKeyDescription));

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

        private static readonly Dictionary<Func<IOperation, IOperation, ISymbol, bool>, DiagnosticDescriptor> s_diagnosticsByCondition = new(3)
        {
            [IsDictionaryIndexerAccess] = DoNotGuardIndexerAccessByContainsKeyRule,
            [(operation, containsKeyArgument, dictionaryType) => IsDictionaryMethodAccess(operation, containsKeyArgument, dictionaryType, AddMethodName)] = DoNotGuardAddByContainsKeyRule,
            [(operation, containsKeyArgument, dictionaryType) => IsDictionaryMethodAccess(operation, containsKeyArgument, dictionaryType, RemoveMethodName)] = DoNotGuardRemoveByContainsKeyRule
        };

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
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
            if (!IsDictionaryContainsKeyInvocation(invocation, dictionaryType)
                || !TryGetParentConditionalOperation(invocation, out var conditionalOperation)
                // There are multiple conditions and it would be difficult to evaluate possible side-effects.
                || (conditionalOperation.Condition is not IUnaryOperation && conditionalOperation.Condition is not IInvocationOperation)
                || conditionalOperation.WhenFalse is not null
                || !TryGetDiagnostic(conditionalOperation.WhenTrue, invocation, dictionaryType, out var diagnostic, out var location))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, invocation.Syntax.GetLocation(), ImmutableArray.Create(location)));
        }

        private static bool TryGetDiagnostic(IOperation whenTrueOperation, IInvocationOperation containsKeyInvocation, ISymbol dictionaryType, [NotNullWhen(true)] out DiagnosticDescriptor? diagnostic, [NotNullWhen(true)] out Location? location)
        {
            location = default;
            diagnostic = default;

            ImmutableArray<IOperation> operations = whenTrueOperation is IBlockOperation blockOperation ? blockOperation.Operations : ImmutableArray.Create(whenTrueOperation);
            foreach (var operation in operations)
            {
                if (IsDictionaryModificationOperation(operation, containsKeyInvocation))
                {
                    return false;
                }
                
                foreach (var keyValuePair in s_diagnosticsByCondition)
                {
                    if (operation.HasAnyOperationDescendant(op => keyValuePair.Key(op, containsKeyInvocation.Arguments[0].Value, dictionaryType), out var diagnostableOperation))
                    {
                        diagnostic = keyValuePair.Value;
                        location = diagnostableOperation.Syntax.GetLocation();

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsDictionaryModificationOperation(IOperation operation, IInvocationOperation containsKeyInvocation)
        {
            return operation.HasAnyOperationDescendant(op => op is IAssignmentOperation { Target: IPropertyReferenceOperation propertyReference } 
                                                               && IsDataReferenceEqual(propertyReference.Instance, containsKeyInvocation.Instance) 
                                                               && IsDataReferenceEqual(propertyReference.Arguments[0].Value, containsKeyInvocation.Arguments[0].Value));
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

        private static bool IsDictionaryIndexerAccess(IOperation operation, IOperation containsKeyArgument, ISymbol dictionaryType)
        {
            return operation is IPropertyReferenceOperation propertyReference 
                   && IsDictionaryType(propertyReference.Property.ContainingType, dictionaryType)
                   && operation.Parent is not IAssignmentOperation
                   && propertyReference.Arguments.Length == 1
                   && IsDataReferenceEqual(propertyReference.Arguments[0].Value, containsKeyArgument)
                   && propertyReference.Property.IsIndexer
                   && (propertyReference.Property.OriginalDefinition.Name == IndexerName ||
                       propertyReference.Language == "Visual Basic" && propertyReference.Property.OriginalDefinition.Name == IndexerNameVb);
        }

        private static bool IsDictionaryMethodAccess(IOperation operation, IOperation containsKeyArgument, ISymbol dictionaryType, string methodName)
        {
            return operation is IInvocationOperation invocation 
                   && IsDictionaryType(invocation.TargetMethod.ContainingType, dictionaryType)
                   && IsDataReferenceEqual(invocation.Arguments[0].Value, containsKeyArgument)
                   && invocation.TargetMethod.Name == methodName;
        }
        
        private static bool IsDataReferenceEqual(IOperation a, IOperation b)
        {
            if (a.Kind != b.Kind)
            {
                return false;
            }

            return a switch
            {
                ILocalReferenceOperation aLocalOperation when b is ILocalReferenceOperation bLocalOperation => aLocalOperation.Local.Equals(bLocalOperation.Local, SymbolEqualityComparer.Default),
                IFieldReferenceOperation aFieldOperation when b is IFieldReferenceOperation bFieldOperation => aFieldOperation.Field.Equals(bFieldOperation.Field, SymbolEqualityComparer.Default), 
                ILiteralOperation aLiteralOperation when b is ILiteralOperation bLiteralOperation => aLiteralOperation.ConstantValue.HasValue && bLiteralOperation.ConstantValue.HasValue && aLiteralOperation.ConstantValue.Value.Equals(bLiteralOperation.ConstantValue.Value),
                IPropertyReferenceOperation aPropertyOperation when b is IPropertyReferenceOperation bPropertyOperation => aPropertyOperation.Property.Equals(bPropertyOperation.Property, SymbolEqualityComparer.Default),
                IMethodReferenceOperation aMethodOperation when b is IMethodReferenceOperation bMethodOperation => aMethodOperation.Method.Equals(bMethodOperation.Method, SymbolEqualityComparer.Default),
                _ => false
            };
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DoNotGuardRemoveByContainsKeyRule, DoNotGuardIndexerAccessByContainsKeyRule, DoNotGuardAddByContainsKeyRule);

        private static LocalizableString CreateResource(string resourceName)
        {
            return new LocalizableResourceString(resourceName, MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        }
    }
}