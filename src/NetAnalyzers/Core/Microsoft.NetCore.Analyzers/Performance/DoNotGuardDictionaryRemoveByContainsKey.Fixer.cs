﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.NetCore.Analyzers.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    public abstract class DoNotGuardDictionaryRemoveByContainsKeyFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(DoNotGuardDictionaryRemoveByContainsKey.RuleId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            if (node is null)
            {
                return;
            }

            var diagnostic = context.Diagnostics.FirstOrDefault();

            if (TryParseLocationInfo(diagnostic, DoNotGuardDictionaryRemoveByContainsKey.PropertyKeys.ConditionalOperation, out var conditionalOperationSpan) &&
                TryParseLocationInfo(diagnostic, DoNotGuardDictionaryRemoveByContainsKey.PropertyKeys.ChildStatementOperation, out var childStatementOperationSpan) &&
                diagnostic.Properties.TryGetValue(DoNotGuardDictionaryRemoveByContainsKey.PropertyKeys.HasMultipleStatements, out var _hasMultipleStatements) &&
                bool.TryParse(_hasMultipleStatements, out var hasMultipleStatements) &&
                root.FindNode(conditionalOperationSpan) is SyntaxNode conditionalOperationNode &&
                root.FindNode(childStatementOperationSpan) is SyntaxNode childStatementOperationNode)
            {
                context.RegisterCodeFix(new DoNotGuardDictionaryRemoveByContainsKeyCodeAction(_ =>
                    Task.FromResult(ReplaceConditionWithChild(context.Document, root, conditionalOperationNode, childStatementOperationNode, hasMultipleStatements))),
                    diagnostic);
            }
        }

        private Document ReplaceConditionWithChild(Document document, SyntaxNode root,
                                                   SyntaxNode conditionalOperationNode, SyntaxNode childOperationNode,
                                                   bool hasMultipleStatements)
        {
            if (!hasMultipleStatements)
            {
                var newNode = childOperationNode.WithAdditionalAnnotations(Formatter.Annotation);
                var newRoot = root.ReplaceNode(conditionalOperationNode, newNode);

                return document.WithSyntaxRoot(newRoot);
            }
            else
            {
                return ReplaceConditionWithChildRetainingStatements(document, root, conditionalOperationNode,
                                                                    childOperationNode);
            }
        }

        protected abstract Document ReplaceConditionWithChildRetainingStatements(Document document, SyntaxNode root, SyntaxNode conditionalOperationNode, SyntaxNode childOperationNode);

        private static bool TryParseLocationInfo(Diagnostic diagnostic, string propertyKey, out TextSpan span)
        {
            span = default;

            if (!diagnostic.Properties.TryGetValue(propertyKey, out var locationInfo))
                return false;

            var parts = locationInfo.Split(new[] { DoNotGuardDictionaryRemoveByContainsKey.AdditionalDocumentLocationInfoSeparator }, StringSplitOptions.None);
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out var spanStart) ||
                !int.TryParse(parts[1], out var spanLength))
            {
                return false;
            }

            span = new TextSpan(spanStart, spanLength);
            return true;
        }

        private class DoNotGuardDictionaryRemoveByContainsKeyCodeAction : DocumentChangeAction
        {
            public DoNotGuardDictionaryRemoveByContainsKeyCodeAction(Func<CancellationToken, Task<Document>> action)
            : base(MicrosoftNetCoreAnalyzersResources.RemoveRedundantGuardCall, action,
                   DoNotGuardDictionaryRemoveByContainsKey.RuleId)
            { }
        }
    }
}
