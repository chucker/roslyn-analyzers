'Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.NetCore.Analyzers.Performance

Namespace Microsoft.NetCore.VisualBasic.Analyzers.Performance
    
    <ExportCodeFixProvider(LanguageNames.VisualBasic)>
    Public NotInheritable Class BasicDoNotGuardDictionaryOperationsFixer
        Inherits DoNotGuardDictionaryOperationsFixer

        Protected Overrides Function TryChangeDocument(document As Document, containsKeyNode As SyntaxNode, dictionaryAccessNode As SyntaxNode, <Out> ByRef changedDocument As Func(Of CancellationToken,Task(Of Document))) As Boolean
            Throw New NotImplementedException
        End Function
    End Class
End Namespace