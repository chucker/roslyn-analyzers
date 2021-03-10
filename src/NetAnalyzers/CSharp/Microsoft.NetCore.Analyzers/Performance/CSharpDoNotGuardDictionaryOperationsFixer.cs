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
        protected override bool TryChangeDocument(Document document, SyntaxNode containsKeyNode, SyntaxNode dictionaryAccessNode,
            [NotNullWhen(true)] out Func<CancellationToken, Task<Document>> changedDocument)
        {
            changedDocument = null!;

            if (containsKeyNode is not InvocationExpressionSyntax containsKeyInvocation || containsKeyInvocation.Expression is not MemberAccessExpressionSyntax containsKeyAccess)
            {
                return false;
            }

            if (dictionaryAccessNode is ElementAccessExpressionSyntax elementAccess)
            {

                changedDocument = ct => FixIndexerAccess(document, containsKeyInvocation, containsKeyAccess, elementAccess, ct);

                return true;
            }

            if (dictionaryAccessNode is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } dictionaryAccessInvocation)
            {
                if (memberAccess.Name.Identifier.ValueText == DoNotGuardDictionaryOperationsAnalyzer.AddMethodName)
                {
                    changedDocument = ct => FixAddAccess(document, containsKeyInvocation, containsKeyAccess, dictionaryAccessInvocation, memberAccess, ct);

                    return true;
                }

                if (memberAccess.Name.Identifier.ValueText == DoNotGuardDictionaryOperationsAnalyzer.RemoveMethodName)
                {
                    changedDocument = ct => FixRemoveAccess(document, containsKeyInvocation, containsKeyAccess, dictionaryAccessInvocation, memberAccess, ct);

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

        private static async Task<Document> FixRemoveAccess(Document document, InvocationExpressionSyntax containsKeyInvocation, MemberAccessExpressionSyntax containsKeyAccess, InvocationExpressionSyntax removeInvocation, MemberAccessExpressionSyntax removeAccess, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var ifStatement = containsKeyInvocation.FirstAncestorOrSelf<IfStatementSyntax>();

            // ContainsKey check is only condition in if statement.
            if (ifStatement.Condition is InvocationExpressionSyntax && ifStatement.Statement is ExpressionStatementSyntax)
            {
                //editor.InsertBefore(ifStatement, ifStatement.Statement.NormalizeWhitespace());
                editor.RemoveNode(ifStatement);

                return editor.GetChangedDocument();
            }

            editor.ReplaceNode(containsKeyInvocation, removeInvocation);
            editor.RemoveNode(removeInvocation);

            return editor.GetChangedDocument();
        }

        private static async Task<Document> FixAddAccess(Document document, InvocationExpressionSyntax containsKeyInvocation, MemberAccessExpressionSyntax containsKeyAccess, InvocationExpressionSyntax addInvocation, MemberAccessExpressionSyntax addAccess, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var generator = editor.Generator;

            return editor.GetChangedDocument();
        }
    }
}