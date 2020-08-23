using ClangSharp;
using ClangSharp.Interop;

using EnvDTE;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EmplaceParameters.Emplace.Signatures;
using Microsoft.VisualStudio.Language.Intellisense;

namespace EmplaceParameters.Parsing
{
    internal static class ParsingExtensions
    {
        public static bool TryParseSource(this string filePath, CXIndex index, out TranslationUnit unit, out List<CXDiagnostic> diagnostics)
        {
            diagnostics = new List<CXDiagnostic>();
            unit = default;

            var translationError = CXTranslationUnit.TryParse(index,
                filePath, null, null,
                CXTranslationUnit_Flags.CXTranslationUnit_KeepGoing,
                out var translationHandle);

            if (translationError != CXErrorCode.CXError_Success)
                return false;

            if (translationHandle.NumDiagnostics != 0)
            {
                for (var i = 0u; i < translationHandle.NumDiagnostics; ++i)
                {
                    var diagnostic = translationHandle.GetDiagnostic(i);
                    diagnostics.Add(diagnostic);
                }
            }

            unit = TranslationUnit.GetOrCreate(translationHandle);
            return unit != default;
        }

        public static bool TryParseDocument(this (Document document, ITextSnapshot textSnapshot) tuple, CXIndex index,
            out TranslationUnit unit, out List<CXDiagnostic> diagnostics)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectionSpan = Span.FromBounds(0, tuple.textSnapshot.Length);
            var pendingText = tuple.textSnapshot.GetText(selectionSpan);
            var pendingFileContents = CXUnsavedFile.Create(tuple.document.FullName, pendingText);

            diagnostics = new List<CXDiagnostic>();
            unit = default;

            var translationError = CXTranslationUnit.TryParseFullArgv(index,
                tuple.document.FullName, new[] {
                    "-std=c++17",
                    "-xc++",
                    "-Wno-pragma-once-outside-header"
                },
                new[] { pendingFileContents },
                CXTranslationUnit_Flags.CXTranslationUnit_KeepGoing,
                out var translationHandle);

            if (translationError != CXErrorCode.CXError_Success)
                return false;

            if (translationHandle.NumDiagnostics != 0)
            {
                for (var i = 0u; i < translationHandle.NumDiagnostics; ++i)
                {
                    var diagnostic = translationHandle.GetDiagnostic(i);
                    diagnostics.Add(diagnostic);
                }
            }

            unit = TranslationUnit.GetOrCreate(translationHandle);
            return unit != default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDerived As<TDerived>(this Cursor decl) where TDerived : Cursor => decl as TDerived;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDerived As<TDerived>(this ClangSharp.Type decl) where TDerived : ClangSharp.Type => decl as TDerived;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDerived Cast<TDerived>(this Cursor decl) where TDerived : Cursor => (TDerived)decl;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDerived Cast<TDerived>(this ClangSharp.Type decl) where TDerived : ClangSharp.Type =>
            (TDerived) decl;
    }
}
