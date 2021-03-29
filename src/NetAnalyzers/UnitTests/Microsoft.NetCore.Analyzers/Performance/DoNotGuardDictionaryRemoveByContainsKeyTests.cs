// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryOperationsAnalyzer,
    Microsoft.NetCore.CSharp.Analyzers.Performance.CSharpDoNotGuardDictionaryOperationsFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryOperationsAnalyzer,
    Microsoft.NetCore.VisualBasic.Analyzers.Performance.BasicDoNotGuardDictionaryOperationsFixer>;

namespace Microsoft.NetCore.Analyzers.Performance.UnitTests
{
    public class DoNotGuardDictionaryRemoveByContainsKeyTests
    {
        #region Tests

        [Fact]
        public Task RemoveIsTheOnlyStatement_OffersFixer_CS()
        {
            const string source = @"
            if ({|#0:MyDictionary.ContainsKey(""Key"")|})
                {|#1:MyDictionary.Remove(""Key"")|};";

            const string fixedSource = @"
            MyDictionary.Remove(""Key"");";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                FixedCode = CreateCSharpCode(fixedSource),
                ExpectedDiagnostics = { StandardDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task RemoveWithOutValueIsTheOnlyStatement_OffersFixer_CS()
        {
            const string source = @"
            if ({|#0:MyDictionary.ContainsKey(""Key"")|})
                {|#1:MyDictionary.Remove(""Key"", out var value)|};";

            const string fixedSource = @"
            MyDictionary.Remove(""Key"", out var value);";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                FixedCode = CreateCSharpCode(fixedSource),
                ExpectedDiagnostics = { StandardDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task RemoveIsTheOnlyStatementInABlock_OffersFixer_CS()
        {
            const string source = @"
            if ({|#0:MyDictionary.ContainsKey(""Key"")|}) {
                {|#1:MyDictionary.Remove(""Key"")|};
            }";

            const string fixedSource = @"
            MyDictionary.Remove(""Key"");";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                FixedCode = CreateCSharpCode(fixedSource),
                ExpectedDiagnostics = { StandardDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task HasElseBlock_NoDiagnostic_CS()
        {
            const string source = @"
            if ({|#0:MyDictionary.ContainsKey(""Key"")|}) {
                {|#1:MyDictionary.Remove(""Key"")|};
            }
            else
            {
                throw new Exception(""Key doesn't exist"");
            }";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                FixedCode = CreateCSharpCode(source)
            }.RunAsync();
        }

        [Fact]
        public Task NegatedCondition_ReportsDiagnostic_CS()
        {
            const string source = @"
            if (!{|#0:MyDictionary.ContainsKey(""Key"")|})
                {|#1:MyDictionary.Remove(""Key"")|};";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                ExpectedDiagnostics = { StandardDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task AdditionalCondition_NoDiagnostic_CS()
        {
            const string source = @"
            if (MyDictionary.ContainsKey(""Key"") && MyDictionary.Count > 2)
                MyDictionary.Remove(""Key"");";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source)
            }.RunAsync();
        }

        [Fact]
        public Task ConditionInVariable_NoDiagnostic_CS()
        {
            const string source = @"
            var result = MyDictionary.ContainsKey(""Key"");
            if (result)
	            MyDictionary.Remove(""Key"");";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source)
            }.RunAsync();
        }

        [Fact]
        public Task RemoveInSeparateLine_NoDiagnostic_CS()
        {
            const string source = @"
            if (MyDictionary.ContainsKey(""Key""))
	            _ = MyDictionary.Count;
	        MyDictionary.Remove(""Key"");";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source)
            }.RunAsync();
        }

        [Fact]
        public Task AdditionalStatements_ReportsDiagnostic_CS()
        {
            const string source = @"
            if ({|#0:MyDictionary.ContainsKey(""Key"")|})
            {
                {|#1:MyDictionary.Remove(""Key"")|};
                Console.WriteLine();
            }";

            const string fixedSource = @"
            if (MyDictionary.Remove(""Key""))
            {
                Console.WriteLine();
            }";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source),
                FixedCode = CreateCSharpCode(fixedSource),
                ExpectedDiagnostics = { StandardDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task AdditionalStatementsMultipleConditions_NoDiagnostic_CS()
        {
            const string source = @"
            if (MyDictionary.ContainsKey(""Key"") && MyDictionary.Count > 2)
            {
                MyDictionary.Remove(""Key"");
                Console.WriteLine();
            }";

            return new VerifyCS.Test
            {
                TestCode = CreateCSharpCode(source)
            }.RunAsync();
        }

        [Fact]
        public Task RemoveIsTheOnlyStatement_OffersFixer_VB()
        {
            const string source = @"
            If {|#0:MyDictionary.ContainsKey(""Key"")|} Then
                {|#1:MyDictionary.Remove(""Key"")|}
            End If";

            const string fixedSource = @"
            MyDictionary.Remove(""Key"")";

            return new VerifyVB.Test
            {
                TestCode = CreateVbCode(source),
                FixedCode = CreateVbCode(fixedSource),
                ExpectedDiagnostics = { StandardVbDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task NegatedCondition_ReportsDiagnostic_VB()
        {
            const string source = @"
            If Not {|#0:MyDictionary.ContainsKey(""Key"")|} Then {|#1:MyDictionary.Remove(""Key"")|}";

            return new VerifyVB.Test
            {
                TestCode = CreateVbCode(source),
                ExpectedDiagnostics = { StandardVbDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task SingleLineIf_OffersFixer_VB()
        {
            const string source = @"
            If {|#0:MyDictionary.ContainsKey(""Key"")|} Then {|#1:MyDictionary.Remove(""Key"")|}";

            const string fixedSource = @"
            MyDictionary.Remove(""Key"")";

            return new VerifyVB.Test
            {
                TestCode = CreateVbCode(source),
                FixedCode = CreateVbCode(fixedSource),
                ExpectedDiagnostics = { StandardVbDiagnostic() }
            }.RunAsync();
        }

        [Fact]
        public Task AdditionalStatements_ReportsDiagnostic_VB()
        {
            const string source = @"
            If {|#0:MyDictionary.ContainsKey(""Key"")|} Then
                {|#1:MyDictionary.Remove(""Key"")|}
                Console.WriteLine()
            End If";

            const string fixedSource = @"
            If MyDictionary.Remove(""Key"") Then
                Console.WriteLine()
            End If";

            return new VerifyVB.Test
            {
                TestCode = CreateVbCode(source),
                FixedCode = CreateVbCode(fixedSource),
                ExpectedDiagnostics = { StandardVbDiagnostic() }
            }.RunAsync();
        }

        #endregion

        #region Helpers

        private const string CSharpTemplate = @"
using System;
using System.Collections.Generic;

namespace Test
{{
    public class MyClass
    {{
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass() {{
            {0}
        }}
    }}
}}";

        private const string VbTemplate = @"
Imports System
Imports System.Collections.Generic
Namespace Testopolis
    Public Class SomeClass
        Public MyDictionary As New Dictionary(Of String, String)()

        Public Sub New()
            {0}
        End Sub
    End Class
End Namespace";

        private static string CreateCSharpCode(string content)
        {
            return string.Format(CultureInfo.InvariantCulture, CSharpTemplate, content);
        }

        private static string CreateVbCode(string content)
        {
            return string.Format(CultureInfo.InvariantCulture, VbTemplate, content);
        }

        private static DiagnosticResult StandardDiagnostic()
        {
            return VerifyCS.Diagnostic(DoNotGuardDictionaryOperationsAnalyzer.DoNotGuardRemoveByContainsKeyRule).WithLocation(0).WithLocation(1);
        }

        private static DiagnosticResult StandardVbDiagnostic()
        {
            return VerifyVB.Diagnostic(DoNotGuardDictionaryOperationsAnalyzer.DoNotGuardRemoveByContainsKeyRule).WithLocation(0).WithLocation(1);
        }

        #endregion
    }
}