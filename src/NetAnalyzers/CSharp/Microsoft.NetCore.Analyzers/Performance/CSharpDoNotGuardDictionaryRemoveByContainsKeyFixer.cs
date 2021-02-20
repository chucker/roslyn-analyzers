// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            // remove Remove() call inside the block
            var newRoot = root.RemoveNode(childOperationNode, SyntaxRemoveOptions.KeepNoTrivia);

            // replace ContainsKey() condition with Remove()
            var newNode = expressionStatement.Expression.WithoutTrivia();
            var oldConditionNode = newRoot.FindNode(ifStatement.Condition.Span);
            newRoot = newRoot.ReplaceNode(oldConditionNode, newNode);

            // TODO: re-apply numerous suggestions from other branch

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
