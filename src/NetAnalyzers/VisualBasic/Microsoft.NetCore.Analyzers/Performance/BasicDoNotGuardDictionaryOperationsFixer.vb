'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.NetCore.Analyzers.Performance

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Performance

    <ExportCodeFixProvider(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicDoNotGuardDictionaryOperationsFixer
        Inherits DoNotGuardDictionaryOperationsFixer

        Protected Overrides Function TryChangeDocument(document As Document, containsKeyNode As SyntaxNode,
                                                       dictionaryAccessNode As SyntaxNode,
                                                       <Out> ByRef codeActionMethod As _
                                                          Func(Of CancellationToken,Task(Of Document))) As Boolean
            codeActionMethod = Nothing

            Dim containsKeyInvocation = TryCast(containsKeyNode, InvocationExpressionSyntax)
            Dim containsKeyAccess = TryCast(containsKeyInvocation?.Expression, MemberAccessExpressionSyntax)
            If containsKeyInvocation Is Nothing Or containsKeyAccess Is Nothing
                Return False
            End If

            Dim condition = TryCast(If(containsKeyInvocation.FirstAncestorOrSelf(Of IfStatementSyntax)?.Condition, containsKeyInvocation.FirstAncestorOrSelf(Of SingleLineIfStatementSyntax)?.Condition), InvocationExpressionSyntax)
            If condition Is Nothing
                Return False
            End If

            Dim dictionaryAccessInvocation = TryCast(dictionaryAccessNode, InvocationExpressionSyntax)
            Dim memberAccess = TryCast(dictionaryAccessInvocation?.Expression, MemberAccessExpressionSyntax)
            If dictionaryAccessInvocation IsNot Nothing And memberAccess IsNot Nothing
                If memberAccess.Name.Identifier.ValueText = DoNotGuardDictionaryOperationsAnalyzer.AddMethodName Or memberAccess.Name.Identifier.ValueText = DoNotGuardDictionaryOperationsAnalyzer.RemoveMethodName
                    codeActionMethod = Function(token) 
                        Return FixDictionaryAccess(document, containsKeyInvocation, dictionaryAccessInvocation, token)
                    End function

                    Return True
                End If
            End If

            If dictionaryAccessInvocation IsNot Nothing
                codeActionMethod = Function(token) 
                    Return FixIndexerAccess(document, containsKeyInvocation, containsKeyAccess, dictionaryAccessInvocation, token)
                End function

                Return True
            End If
            
            Return False
        End Function

        Private Shared Async Function FixIndexerAccess(document As Document,
                                                       containsKeyInvocation As InvocationExpressionSyntax,
                                                       containsKeyAccess As MemberAccessExpressionSyntax,
                                                       dictionaryAccessNode As InvocationExpressionSyntax,
                                                       ct As CancellationToken) As Task(Of Document)
            Dim editor = Await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(False)
            Dim generator = editor.Generator

            Dim tryGetValueAccess = generator.MemberAccessExpression(containsKeyAccess.Expression, "TryGetValue")
            Dim keyArgument = containsKeyInvocation.ArgumentList.Arguments.FirstOrDefault()

            Dim valueAssignment = generator.LocalDeclarationStatement(generator.TypeExpression(SpecialType.None),"value")
            Dim tryGetValueInvocation = generator.InvocationExpression(tryGetValueAccess, keyArgument,
                                                                       generator.Argument(
                                                                           generator.IdentifierName("value")))

            Dim ifStatement = containsKeyInvocation.AncestorsAndSelf().OfType (Of IfStatementSyntax).FirstOrDefault()
            editor.InsertBefore(ifStatement, valueAssignment)
            editor.ReplaceNode(containsKeyInvocation, tryGetValueInvocation)
            editor.ReplaceNode(dictionaryAccessNode, generator.IdentifierName("value"))

            Return editor.GetChangedDocument()
        End Function

        Private Shared Async Function FixDictionaryAccess(document As Document,
                                                          containsKeyInvocation As InvocationExpressionSyntax,
                                                          operationInvocation As InvocationExpressionSyntax,
                                                          ct As CancellationToken) As Task(Of Document)
            Dim editor = Await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(False)

            Dim multiLineIfStatement = containsKeyInvocation.FirstAncestorOrSelf (Of MultiLineIfBlockSyntax)
            If multiLineIfStatement IsNot Nothing
                If multiLineIfStatement.Statements.Count = 1
                    editor.ReplaceNode(multiLineIfStatement, multiLineIfStatement.Statements(0).WithTriviaFrom(multiLineIfStatement))
                Else
                    editor.RemoveNode(operationInvocation)
                    editor.ReplaceNode(containsKeyInvocation, Function(token, generator)
                        Return generator.InvocationExpression(operationInvocation.Expression, operationInvocation.ArgumentList.Arguments).WithTriviaFrom(operationInvocation)
                    End Function)
                End If
                
                Return editor.GetChangedDocument()
            End If
            
            Dim singleLineIfStatement = containsKeyInvocation.FirstAncestorOrSelf(Of SingleLineIfStatementSyntax)
            If singleLineIfStatement IsNot Nothing
                If singleLineIfStatement.Statements.Count = 1
                    editor.ReplaceNode(singleLineIfStatement, singleLineIfStatement.Statements(0).WithTriviaFrom(singleLineIfStatement))
                Else
                    editor.RemoveNode(operationInvocation)
                    editor.ReplaceNode(containsKeyInvocation, Function(token, generator)
                        Return generator.InvocationExpression(operationInvocation.Expression, operationInvocation.ArgumentList.Arguments)
                    End Function)
                End If
                
                Return editor.GetChangedDocument()
            End If
            
            Return editor.GetChangedDocument()
        End Function
    End Class
End Namespace