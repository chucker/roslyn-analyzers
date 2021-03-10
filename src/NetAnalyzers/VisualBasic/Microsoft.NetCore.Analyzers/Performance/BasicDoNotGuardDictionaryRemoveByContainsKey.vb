﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If OLDDONOTGUARDANALYZER Then
Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.NetCore.Analyzers.Performance

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Performance
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Public NotInheritable Class BasicDoNotGuardDictionaryRemoveByContainsKeyFixer
        Inherits DoNotGuardDictionaryRemoveByContainsKeyFixer

        Protected Overrides Function OperationSupportedByFixer(conditionalOperation As SyntaxNode) As Boolean
            If TypeOf conditionalOperation Is IfStatementSyntax Then
                Return True
            End If

            If TypeOf conditionalOperation Is MultiLineIfBlockSyntax Then
                Return CType(conditionalOperation, MultiLineIfBlockSyntax).IfStatement.ChildNodes().Count() = 1
            End If

            Return False
        End Function

        Protected Overrides Function ReplaceConditionWithChild(document As Document, root As SyntaxNode, conditionalOperationNode As SyntaxNode, childOperationNode As SyntaxNode) As Document
            Dim newConditionNode As SyntaxNode = childOperationNode

            ' if there's an else block, negate the condition and replace the single true statement with it
            Dim multiLineIfBlockSyntax = TryCast(conditionalOperationNode, MultiLineIfBlockSyntax)
            If multiLineIfBlockSyntax?.ElseBlock?.ChildNodes().Any() Then
                Dim generator = SyntaxGenerator.GetGenerator(document)
                Dim negatedExpression = generator.LogicalNotExpression(CType(childOperationNode, ExpressionStatementSyntax).Expression.WithoutTrivia())

                Dim oldElseBlock = multiLineIfBlockSyntax.ElseBlock.Statements

                newConditionNode = multiLineIfBlockSyntax.WithIfStatement(multiLineIfBlockSyntax.IfStatement.WithCondition(CType(negatedExpression, ExpressionSyntax))) _
                    .WithStatements(oldElseBlock) _
                    .WithElseBlock(Nothing) _
                    .WithAdditionalAnnotations(Formatter.Annotation).WithTriviaFrom(conditionalOperationNode)
            Else
                newConditionNode = newConditionNode.WithAdditionalAnnotations(Formatter.Annotation).WithTriviaFrom(conditionalOperationNode)
            End If

            Dim newRoot = root.ReplaceNode(conditionalOperationNode, newConditionNode)

            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace
#End If
