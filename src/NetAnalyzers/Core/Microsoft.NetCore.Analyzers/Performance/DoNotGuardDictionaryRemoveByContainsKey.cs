// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using Resx = Microsoft.NetCore.Analyzers.MicrosoftNetCoreAnalyzersResources;

namespace Microsoft.NetCore.Analyzers.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotGuardDictionaryRemoveByContainsKey : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1839";

        private static readonly LocalizableString s_localizableTitle =
            new LocalizableResourceString(nameof(Resx.DoNotGuardDictionaryRemoveByContainsKeyTitle), Resx.ResourceManager, typeof(Resx));
        private static readonly LocalizableString s_localizableMessage =
            new LocalizableResourceString(nameof(Resx.DoNotGuardDictionaryRemoveByContainsKeyMessage), Resx.ResourceManager, typeof(Resx));
        private static readonly LocalizableString s_localizableDescription =
            new LocalizableResourceString(nameof(Resx.DoNotGuardDictionaryRemoveByContainsKeyDescription), Resx.ResourceManager, typeof(Resx));

        public const string AdditionalDocumentLocationInfoSeparator = ";;";

        public struct PropertyKeys
        {
            public const string ConditionalOperation = nameof(ConditionalOperation);
            public const string ChildStatementOperation = nameof(ChildStatementOperation);
            public const string HasMultipleStatements = nameof(HasMultipleStatements);
        }

        internal static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorHelper.Create(
            RuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false,
            additionalCustomTags: WellKnownDiagnosticTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var compilation = context.Compilation;

            if (!compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericDictionary2, out var dictionaryType))
                return;

            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);

            static void AnalyzeOperation(OperationAnalysisContext context)
            {
                var invocationOperation = (IInvocationOperation)context.Operation;

                if (invocationOperation.TargetMethod.Name != "ContainsKey")
                    return;

                if (invocationOperation.Parent is not IConditionalOperation parentConditionalOperation)
                    return;

                if (parentConditionalOperation.WhenFalse == null &&
                    parentConditionalOperation.WhenTrue.Children?.Any() == true)
                {
                    var properties = ImmutableDictionary.CreateBuilder<string, string>();
                    properties[PropertyKeys.ConditionalOperation] = CreateLocationInfo(parentConditionalOperation.Syntax);

                    var nestedInvocationOperation = parentConditionalOperation.WhenTrue.Children.OfType<IExpressionStatementOperation>()
                            .FirstOrDefault(o => ((IInvocationOperation)o.Operation).TargetMethod.Name == "Remove");

                    if (nestedInvocationOperation != null)
                    {
                        properties[PropertyKeys.ChildStatementOperation] = CreateLocationInfo(nestedInvocationOperation.Syntax);
                        properties[PropertyKeys.HasMultipleStatements] = parentConditionalOperation.WhenTrue.Children.HasMoreThan(1).ToString();

                        context.ReportDiagnostic(Diagnostic.Create(Rule, invocationOperation.Syntax.GetLocation(), properties.ToImmutable()));
                    }
                }
            }
        }

        private static string CreateLocationInfo(SyntaxNode syntax)
        {
            // see DiagnosticDescriptorCreationAnalyzer

            var location = syntax.GetLocation();
            var span = location.SourceSpan;

            return $"{span.Start}{AdditionalDocumentLocationInfoSeparator}{span.Length}";
        }
    }
}
