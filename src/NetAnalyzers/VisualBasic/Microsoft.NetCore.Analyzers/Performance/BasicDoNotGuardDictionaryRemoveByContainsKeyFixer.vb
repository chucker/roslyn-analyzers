' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.NetCore.Analyzers.Performance

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Performance
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicDoNotGuardDictionaryRemoveByContainsKeyFixer
        Inherits DoNotGuardDictionaryRemoveByContainsKeyFixer

        Protected Overrides Function ReplaceConditionWithChildRetainingStatements(document As Document, root As SyntaxNode, conditionalOperationNode As SyntaxNode, childOperationNode As SyntaxNode) As Document
            Dim ifStatement = TryCast(conditionalOperationNode, IfStatementSyntax)
            Dim expressionStatement = TryCast(childOperationNode, ExpressionStatementSyntax)

            If ifStatement Is Nothing OrElse expressionStatement Is Nothing Then
                Return document
            End If

            Dim newNode = expressionStatement.Expression.WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(ifStatement.Condition, newNode)

            newRoot = newRoot.RemoveNode(childOperationNode, SyntaxRemoveOptions.KeepNoTrivia)

            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace
