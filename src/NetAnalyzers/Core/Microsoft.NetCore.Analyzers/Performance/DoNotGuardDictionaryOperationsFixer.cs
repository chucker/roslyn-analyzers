// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.NetCore.Analyzers.Performance
{
    public abstract class DoNotGuardDictionaryOperationsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DoNotGuardDictionaryOperationsAnalyzer.DoNotGuardRemoveByContainsKeyId, DoNotGuardDictionaryOperationsAnalyzer.DoNotGuardIndexerAccessByContainsKeyId, DoNotGuardDictionaryOperationsAnalyzer.DoNotGuardAddByContainsKeyId);
        
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            var dictionaryAccessLocation = diagnostic?.AdditionalLocations.FirstOrDefault();
            if (dictionaryAccessLocation is null)
            {
                return;
            }

            Document document = context.Document;
            SyntaxNode root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var containsKeyNode = root.FindNode(context.Span);
            var dictionaryAccessNode = root.FindNode(dictionaryAccessLocation.SourceSpan, getInnermostNodeForTie: true);
            if (containsKeyNode is null || dictionaryAccessNode is null || !TryChangeDocument(document, containsKeyNode, dictionaryAccessNode, out var codeActionMethod))
            {
                return;
            }

            var codeAction = CodeAction.Create("", codeActionMethod, "");
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        protected abstract bool TryChangeDocument(Document document, SyntaxNode containsKeyNode, SyntaxNode dictionaryAccessNode, [NotNullWhen(true)] out Func<CancellationToken, Task<Document>> changedDocument);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}