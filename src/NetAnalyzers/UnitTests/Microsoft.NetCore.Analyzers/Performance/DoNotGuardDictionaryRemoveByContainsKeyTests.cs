// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryRemoveByContainsKey,
    Microsoft.NetCore.CSharp.Analyzers.Performance.CSharpDoNotGuardDictionaryRemoveByContainsKeyFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryRemoveByContainsKey,
    Microsoft.NetCore.VisualBasic.Analyzers.Performance.BasicDoNotGuardDictionaryRemoveByContainsKeyFixer>;

namespace Microsoft.NetCore.Analyzers.Performance.UnitTests
{
    public class DoNotGuardDictionaryRemoveByContainsKeyTests
    {
        #region Tests
        [Fact]
        public async Task RemoveIsTheOnlyStatement_OffersFixer_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if ([|MyDictionary.ContainsKey(""Key"")|])
                MyDictionary.Remove(""Key"");
        }
    }
}";

            string fixedSource = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            MyDictionary.Remove(""Key"");
        }
    }
}";
            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task RemoveIsTheOnlyStatementInABlock_OffersFixer_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if ([|MyDictionary.ContainsKey(""Key"")|])
            {
                MyDictionary.Remove(""Key"");
            }
        }
    }
}";

            string fixedSource = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            MyDictionary.Remove(""Key"");
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task NegatedCondition_NoDiagnostic_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if (!MyDictionary.ContainsKey(""Key""))
                MyDictionary.Remove(""Key"");
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task AdditionalCondition_NoDiagnostic_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if (MyDictionary.ContainsKey(""Key"") && MyDictionary.Count > 2)
                MyDictionary.Remove(""Key"");
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task ConditionInVariable_NoDiagnostic_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            var result = MyDictionary.ContainsKey(""Key"");
            if (result)
	            MyDictionary.Remove(""Key"");
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task RemoveInSeparateLine_NoDiagnostic_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if (MyDictionary.ContainsKey(""Key""))
	            _ = MyDictionary.Count;
	        MyDictionary.Remove(""Key"");
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task AdditionalStatements_OffersFixer_CS()
        {
            string source = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if ([|MyDictionary.ContainsKey(""Key"")|])
            {
                MyDictionary.Remove(""Key"");
                Console.WriteLine();
            }
        }
    }
}";

            string fixedSource = @"
" + CSUsings + @"
namespace Testopolis
{
    public class MyClass
    {
        private readonly Dictionary<string, string> MyDictionary = new Dictionary<string, string>();

        public MyClass()
        {
            if (MyDictionary.Remove(""Key""))
            {
                Console.WriteLine();
            }
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task RemoveIsTheOnlyStatement_OffersFixer_VB()
        {
            string source = @"
" + VBUsings + @"
Namespace Testopolis
    Public Class SomeClass
        Public MyDictionary As New Dictionary(Of String, String)()

        Public Sub New()
            If [|MyDictionary.ContainsKey(""Key"")|] Then
                MyDictionary.Remove(""Key"")
            End If
        End Sub
    End Class
End Namespace";

            string fixedSource = @"
" + VBUsings + @"
Namespace Testopolis
    Public Class SomeClass
        Public MyDictionary As New Dictionary(Of String, String)()

        Public Sub New()
            MyDictionary.Remove(""Key"")
        End Sub
    End Class
End Namespace";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }

        [Fact]
        public async Task AdditionalStatements_OffersFixer_VB()
        {
            string source = @"
" + VBUsings + @"
Namespace Testopolis
    Public Class SomeClass
        Public MyDictionary As New Dictionary(Of String, String)()

        Public Sub New()
            If MyDictionary.ContainsKey(""Key"") Then
                MyDictionary.Remove(""Key"")
                Console.WriteLine()
            End If
        End Sub
    End Class
End Namespace";

            string fixedSource = @"
" + VBUsings + @"
Namespace Testopolis
    Public Class SomeClass
        Public MyDictionary As New Dictionary(Of String, String)()

        Public Sub New()
            If MyDictionary.Remove(""Key"") Then
                Console.WriteLine()
            End If
        End Sub
    End Class
End Namespace";

            await VerifyVB.VerifyCodeFixAsync(source, fixedSource);
        }
        #endregion

        #region Helpers
        private const string CSUsings = @"using System;
using System.Collections.Generic;";

        private const string VBUsings = @"Imports System
Imports System.Collections.Generic";
        #endregion
    }
}
