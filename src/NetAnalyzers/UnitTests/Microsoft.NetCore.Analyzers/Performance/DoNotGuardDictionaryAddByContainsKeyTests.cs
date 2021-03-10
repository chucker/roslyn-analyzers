// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryOperationsAnalyzer,
    Microsoft.NetCore.CSharp.Analyzers.Performance.CSharpDoNotGuardDictionaryOperationsFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.DoNotGuardDictionaryOperationsAnalyzer,
    Microsoft.NetCore.VisualBasic.Analyzers.Performance.BasicDoNotGuardDictionaryOperationsFixer>;

namespace Microsoft.NetCore.Analyzers.Performance.UnitTests
{
    public class DoNotGuardDictionaryAddByContainsKeyTests
    {

    }
}