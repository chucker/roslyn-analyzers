// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.NetCore.Analyzers.Performance;

namespace Microsoft.NetCore.CSharp.Analyzers.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class CSharpDoNotGuardDictionaryRemoveByContainsKeyFixer : DoNotGuardDictionaryRemoveByContainsKeyFixer
    {
        protected override Document ReplaceConditionWithChildRetainingStatements(Document document, SyntaxNode root,
                                                                                 SyntaxNode conditionalOperationNode,
                                                                                 SyntaxNode childOperationNode)
        {
            if (conditionalOperationNode is not IfStatementSyntax ifStatement ||
                childOperationNode is not ExpressionStatementSyntax expressionStatement)
            {
                return document;
            }

            // FIXME: doesn't clean up whitespace
            var newNode = expressionStatement.Expression.WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(ifStatement.Condition, newNode);

            // FIXME: doesn't work
            newRoot = newRoot.RemoveNode(childOperationNode, SyntaxRemoveOptions.KeepNoTrivia);

            // TODO: re-apply numerous suggestions from other branch

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
