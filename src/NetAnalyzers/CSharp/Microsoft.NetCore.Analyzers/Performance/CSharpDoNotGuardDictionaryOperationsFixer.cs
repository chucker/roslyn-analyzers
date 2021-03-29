// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.NetCore.Analyzers.Performance;

namespace Microsoft.NetCore.CSharp.Analyzers.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CSharpDoNotGuardDictionaryOperationsFixer : DoNotGuardDictionaryOperationsFixer
    {
        protected override bool TryChangeDocument(Document document, SyntaxNode containsKeyNode, SyntaxNode dictionaryAccessNode, [NotNullWhen(true)] out Func<CancellationToken, Task<Document>> codeActionMethod)
        {
            codeActionMethod = null!;

            if (containsKeyNode is not InvocationExpressionSyntax containsKeyInvocation ||
                containsKeyInvocation.Expression is not MemberAccessExpressionSyntax containsKeyAccess)
            {
                return false;
            }

            var ifStatement = containsKeyInvocation.FirstAncestorOrSelf<IfStatementSyntax>();
            if (ifStatement.Condition is not InvocationExpressionSyntax)
            {
                // Do not offer fixer when the ContainsKey check is not the only condition.
                return false;
            }

            if (dictionaryAccessNode is ElementAccessExpressionSyntax elementAccess)
            {
                codeActionMethod = ct => FixIndexerAccess(document, containsKeyInvocation, containsKeyAccess, elementAccess, ct);

                return true;
            }

            if (dictionaryAccessNode is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } dictionaryAccessInvocation)
            {
                if (memberAccess.Name.Identifier.ValueText == DoNotGuardDictionaryOperationsAnalyzer.AddMethodName || memberAccess.Name.Identifier.ValueText == DoNotGuardDictionaryOperationsAnalyzer.RemoveMethodName)
                {
                    codeActionMethod = ct => FixDictionaryAccess(document, containsKeyInvocation, dictionaryAccessInvocation, ct);

                    return true;
                }
            }

            return false;
        }

        private static async Task<Document> FixIndexerAccess(Document document, InvocationExpressionSyntax containsKeyInvocation, MemberAccessExpressionSyntax containsKeyAccess, ElementAccessExpressionSyntax dictionaryAccessNode, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var generator = editor.Generator;

            var tryGetValueAccess = generator.MemberAccessExpression(containsKeyAccess.Expression, "TryGetValue");
            var keyArgument = containsKeyInvocation.ArgumentList.Arguments.FirstOrDefault();

            var outArgument = generator.Argument(RefKind.Out,
                SyntaxFactory.DeclarationExpression(SyntaxFactory.IdentifierName("var"), SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier("value"))));
            var tryGetValueInvocation = generator.InvocationExpression(tryGetValueAccess, keyArgument, outArgument);
            editor.ReplaceNode(containsKeyInvocation, tryGetValueInvocation);

            editor.ReplaceNode(dictionaryAccessNode, generator.IdentifierName("value"));

            return editor.GetChangedDocument();
        }

        private static async Task<Document> FixDictionaryAccess(Document document, InvocationExpressionSyntax containsKeyInvocation, InvocationExpressionSyntax operationInvocation, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var ifStatement = containsKeyInvocation.FirstAncestorOrSelf<IfStatementSyntax>();

            switch (ifStatement.Statement)
            {
                // If statement is a block and has a single statement (the operationInvocation)
                case BlockSyntax block when block.Statements.Count == 1:
                {
                    editor.ReplaceNode(ifStatement, block.Statements[0]);

                    return editor.GetChangedDocument();
                }
                // If statement has a single statement (the operationInvocation)
                case ExpressionStatementSyntax expressionStatementSyntax:
                {
                    editor.ReplaceNode(ifStatement, expressionStatementSyntax.WithTriviaFrom(ifStatement));

                    return editor.GetChangedDocument();
                }
                // If statement has multiple statements, in which case, the ContainsKey check should be replaced by the operationInvocation.
                default:
                {
                    var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
                    editor.ReplaceNode(containsKeyInvocation, (_, generator) => generator.InvocationExpression(operationInvocation.Expression, operationInvocation.ArgumentList.Arguments).WithTriviaFrom(operationInvocation));
                    editor.RemoveNode(root.FindNode(operationInvocation.Span));

                    return editor.GetChangedDocument();
                }
            }
        }
    }
}