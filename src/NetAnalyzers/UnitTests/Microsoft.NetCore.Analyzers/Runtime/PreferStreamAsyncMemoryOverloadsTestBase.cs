﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.PreferStreamAsyncMemoryOverloads,
    Microsoft.NetCore.CSharp.Analyzers.Runtime.PreferStreamAsyncMemoryOverloadsCSharpFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Runtime.PreferStreamAsyncMemoryOverloads,
    Microsoft.NetCore.VisualBasic.Analyzers.Runtime.PreferStreamAsyncMemoryOverloadsVisualBasicFixer>;

namespace Microsoft.NetCore.Analyzers.Runtime.UnitTests
{
    public class PreferStreamAsyncMemoryOverloadsTestBase
    {
        // Verifies that the analyzer generates the specified C# diagnostic results, if any.
        protected Task CSharpVerifyAnalyzerAsync(string source, params DiagnosticResult[] expected) =>
            CSharpVerifyForVersionAsync(source, null, ReferenceAssemblies.NetCore.NetCoreApp50, expected);

        // Verifies that the analyzer generates the specified VB diagnostic results, if any.
        protected Task VisualBasicVerifyAnalyzerAsync(string source, params DiagnosticResult[] expected) =>
            VisualBasicVerifyForVersionAsync(source, null, ReferenceAssemblies.NetCore.NetCoreApp50, expected);

        // Verifies that the analyzer generates the specified C# diagnostic results, if any, in an unsupported .NET version.
        protected Task CSharpVerifyAnalyzerForUnsupportedVersionAsync(string source, params DiagnosticResult[] expected) =>
            CSharpVerifyForVersionAsync(source, null, ReferenceAssemblies.NetCore.NetCoreApp20, expected);

        // Verifies that the analyzer generates the specified VB diagnostic results, if any, in an unsupported .NET version.
        protected Task VisualBasicVerifyAnalyzerForUnsupportedVersionAsync(string source, params DiagnosticResult[] expected) =>
            VisualBasicVerifyForVersionAsync(source, null, ReferenceAssemblies.NetCore.NetCoreApp20, expected);

        // Verifies that the fixer generates the fixes for the specified C# diagnostic results, if any.
        protected Task CSharpVerifyExpectedCodeFixDiagnosticsAsync(string originalSource, string fixedSource, params DiagnosticResult[] expected) =>
            CSharpVerifyForVersionAsync(originalSource, fixedSource, ReferenceAssemblies.NetCore.NetCoreApp50, expected);

        // Verifies that the fixer generates the fixes for the specified VB diagnostic results, if any.
        protected Task VisualBasicVerifyExpectedCodeFixDiagnosticsAsync(string originalSource, string fixedSource, params DiagnosticResult[] expected) =>
            VisualBasicVerifyForVersionAsync(originalSource, fixedSource, ReferenceAssemblies.NetCore.NetCoreApp50, expected);

        // Verifies that the analyzer generates the specified C# diagnostic results, if any, for the specified originalSource.
        // If fixedSource is provided, also verifies that the fixer generates the fixes for the verified diagnostic results, if any.
        private Task CSharpVerifyForVersionAsync(string originalSource, string fixedSource, ReferenceAssemblies version, params DiagnosticResult[] expected)
        {
            var test = new VerifyCS.Test
            {
                TestCode = originalSource,
                ReferenceAssemblies = version,
            };

            if (!string.IsNullOrEmpty(fixedSource))
            {
                test.FixedCode = fixedSource;
            }

            test.ExpectedDiagnostics.AddRange(expected);

            return test.RunAsync();
        }

        // Verifies that the analyzer generates the specified VB diagnostic results, if any, for the specified originalSource.
        // If fixedSource is provided, also verifies that the fixer generates the fixes for the verified diagnostic results, if any.
        private Task VisualBasicVerifyForVersionAsync(string originalSource, string fixedSource, ReferenceAssemblies version, params DiagnosticResult[] expected)
        {
            var test = new VerifyVB.Test
            {
                TestCode = originalSource,
                ReferenceAssemblies = version,
            };

            if (!string.IsNullOrEmpty(fixedSource))
            {
                test.FixedCode = fixedSource;
            }

            test.ExpectedDiagnostics.AddRange(expected);

            return test.RunAsync();
        }

        // Retrieves the C# diagnostic for the specified rule, lines, columns, method and preferred method.
        protected DiagnosticResult GetCSResultForRule(int startLine, int startColumn, int endLine, int endColumn, DiagnosticDescriptor rule, string methodName, string methodPreferredName)
            => VerifyCS.Diagnostic(rule)
                .WithSpan(startLine, startColumn, endLine, endColumn)
                .WithArguments(methodName, methodPreferredName);

        // Retrieves the VB diagnostic for the specified rule, lines, columns, method and preferred method.
        protected DiagnosticResult GetVBResultForRule(int startLine, int startColumn, int endLine, int endColumn, DiagnosticDescriptor rule, string methodName, string methodPreferredName)
            => VerifyVB.Diagnostic(rule)
                .WithSpan(startLine, startColumn, endLine, endColumn)
                .WithArguments(methodName, methodPreferredName);

        protected string GetByteArrayWithDataCSharp(bool isEmpty) => isEmpty ? "" : @"byte[] buffer = { 0xBA, 0x5E, 0xBA, 0x11, 0xF0, 0x07, 0xBA, 0x11 };";

        protected string GetByteArrayWithDataVisualBasic(bool isEmpty) => isEmpty ? "" : @"Dim buffer As Byte() = {&HBA, &H5E, &HBA, &H11, &HF0, &H07, &HBA, &H11}";

        protected string GetByteArrayWithoutDataCSharp(bool isEmpty) => isEmpty ? "" : @"byte[] buffer = new byte[s.Length];";

        protected string GetByteArrayWithoutDataVisualBasic(bool isEmpty) => isEmpty ? "" : @"Dim buffer As Byte() = New Byte(s.Length - 1) {}";

        protected string GetConfigureAwaitCSharp(bool isEmpty) => isEmpty ? "" : @".ConfigureAwait(false)";

        protected string GetConfigureAwaitVisualBasic(bool isEmpty) => isEmpty ? "" : @".ConfigureAwait(False)";

        public static IEnumerable<object[]> CSharpUnnamedArgumentsTestData()
        {
            yield return new object[] { "buffer, 0, buffer.Length",
                                        "buffer.AsMemory(0, buffer.Length)" };
            yield return new object[] { "buffer, 0, buffer.Length, new CancellationToken()",
                                        "buffer.AsMemory(0, buffer.Length), new CancellationToken()" };
        }

        public static IEnumerable<object[]> CSharpNamedArgumentsTestData()
        {
            // Normal argument order is: (byte[] buffer, int offset, int count)
            yield return new object[] { "buffer, offset: 0, count: buffer.Length",
                                        "buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "buffer: buffer, offset: 0, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "buffer: buffer, count: buffer.Length, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "count: buffer.Length, offset: 0, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "count: buffer.Length, buffer: buffer, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "offset: 0, buffer: buffer, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            yield return new object[] { "offset: 0, count: buffer.Length, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length)" };
            // Skipping naming
            yield return new object[] { "buffer: buffer, 0, buffer.Length",
                                        "buffer: buffer.AsMemory(0, buffer.Length)" };
            yield return new object[] { "buffer, offset: 0, buffer.Length",
                                        "buffer.AsMemory(start: 0, buffer.Length)" };
            yield return new object[] { "buffer, 0, count: buffer.Length",
                                        "buffer.AsMemory(0, length: buffer.Length)" };
            yield return new object[] { "buffer: buffer, 0, count: buffer.Length",
                                        "buffer: buffer.AsMemory(0, length: buffer.Length)" };
        }

        public static IEnumerable<object[]> CSharpNamedArgumentsWithCancellationTokenTestData()
        {
            // Normal argument order is: (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            // Cancellation token as fourth argument
            yield return new object[] { "buffer, offset: 0, count: buffer.Length, cancellationToken: new CancellationToken()",
                                        "buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, offset: 0, count: buffer.Length, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, count: buffer.Length, offset: 0, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, offset: 0, buffer: buffer, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, buffer: buffer, offset: 0, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, buffer: buffer, count: buffer.Length, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, count: buffer.Length, buffer: buffer, cancellationToken: new CancellationToken()",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            // Cancellation token as third argument
            yield return new object[] { "buffer, offset: 0, cancellationToken: new CancellationToken(), count: buffer.Length",
                                        "buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, offset: 0, cancellationToken: new CancellationToken(), count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, count: buffer.Length, cancellationToken: new CancellationToken(), offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, offset: 0, cancellationToken: new CancellationToken(), buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, buffer: buffer, cancellationToken: new CancellationToken(), offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, buffer: buffer, cancellationToken: new CancellationToken(), count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, count: buffer.Length, cancellationToken: new CancellationToken(), buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            // Cancellation token as second argument
            yield return new object[] { "buffer, cancellationToken: new CancellationToken(), offset: 0, count: buffer.Length",
                                        "buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, cancellationToken: new CancellationToken(), offset: 0, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "buffer: buffer, cancellationToken: new CancellationToken(), count: buffer.Length, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, cancellationToken: new CancellationToken(), offset: 0, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "count: buffer.Length, cancellationToken: new CancellationToken(), buffer: buffer, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, cancellationToken: new CancellationToken(), buffer: buffer, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "offset: 0, cancellationToken: new CancellationToken(), count: buffer.Length, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            // Cancellation token as first argument
            yield return new object[] { "cancellationToken: new CancellationToken(), buffer: buffer, offset: 0, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "cancellationToken: new CancellationToken(), buffer: buffer, count: buffer.Length, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "cancellationToken: new CancellationToken(), count: buffer.Length, offset: 0, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "cancellationToken: new CancellationToken(), count: buffer.Length, buffer: buffer, offset: 0",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "cancellationToken: new CancellationToken(), offset: 0, buffer: buffer, count: buffer.Length",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
            yield return new object[] { "cancellationToken: new CancellationToken(), offset: 0, count: buffer.Length, buffer: buffer",
                                        "buffer: buffer.AsMemory(start: 0, length: buffer.Length), cancellationToken: new CancellationToken()" };
        }

        public static IEnumerable<object[]> VisualBasicUnnamedArgumentsTestData()
        {
            yield return new object[] { "buffer, 0, buffer.Length",
                                        "buffer.AsMemory(0, buffer.Length)" };
            yield return new object[] { "buffer, 0, buffer.Length, New CancellationToken()",
                                        "buffer.AsMemory(0, buffer.Length), New CancellationToken()" };
        }

        public static IEnumerable<object[]> VisualBasicNamedArgumentsTestData()
        {
            // Normal argument order is: (byte[] buffer, int offset, int count)
            yield return new object[] { "buffer, offset:=0, count:=buffer.Length",
                                        "buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "buffer:=buffer, offset:=0, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "buffer:=buffer, count:=buffer.Length, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "count:=buffer.Length, offset:=0, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "count:=buffer.Length, buffer:=buffer, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "offset:=0, buffer:=buffer, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
            yield return new object[] { "offset:=0, count:=buffer.Length, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length)" };
        }

        public static IEnumerable<object[]> VisualBasicNamedArgumentsWithCancellationTokenTestData()
        {
            // Normal argument order is: (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            // Cancellation token as fourth argument
            yield return new object[] { "buffer, offset:=0, count:=buffer.Length, cancellationToken:=New CancellationToken()",
                                        "buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, offset:=0, count:=buffer.Length, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, count:=buffer.Length, offset:=0, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, offset:=0, buffer:=buffer, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, buffer:=buffer, offset:=0, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, buffer:=buffer, count:=buffer.Length, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, count:=buffer.Length, buffer:=buffer, cancellationToken:=New CancellationToken()",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            // Cancellation token as third argument
            yield return new object[] { "buffer, offset:=0, cancellationToken:=New CancellationToken(), count:=buffer.Length",
                                        "buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, offset:=0, cancellationToken:=New CancellationToken(), count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, count:=buffer.Length, cancellationToken:=New CancellationToken(), offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, offset:=0, cancellationToken:=New CancellationToken(), buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, buffer:=buffer, cancellationToken:=New CancellationToken(), offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, buffer:=buffer, cancellationToken:=New CancellationToken(), count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, count:=buffer.Length, cancellationToken:=New CancellationToken(), buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            // Cancellation token as second argument
            yield return new object[] { "buffer, cancellationToken:=New CancellationToken(), offset:=0, count:=buffer.Length",
                                        "buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, cancellationToken:=New CancellationToken(), offset:=0, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "buffer:=buffer, cancellationToken:=New CancellationToken(), count:=buffer.Length, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, cancellationToken:=New CancellationToken(), offset:=0, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "count:=buffer.Length, cancellationToken:=New CancellationToken(), buffer:=buffer, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, cancellationToken:=New CancellationToken(), buffer:=buffer, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "offset:=0, cancellationToken:=New CancellationToken(), count:=buffer.Length, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            // Cancellation token as first argument
            yield return new object[] { "cancellationToken:=New CancellationToken(), buffer:=buffer, offset:=0, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "cancellationToken:=New CancellationToken(), buffer:=buffer, count:=buffer.Length, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "cancellationToken:=New CancellationToken(), count:=buffer.Length, offset:=0, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "cancellationToken:=New CancellationToken(), count:=buffer.Length, buffer:=buffer, offset:=0",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "cancellationToken:=New CancellationToken(), offset:=0, buffer:=buffer, count:=buffer.Length",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
            yield return new object[] { "cancellationToken:=New CancellationToken(), offset:=0, count:=buffer.Length, buffer:=buffer",
                                        "buffer:=buffer.AsMemory(start:=0, length:=buffer.Length), cancellationToken:=New CancellationToken()" };
        }
    }
}