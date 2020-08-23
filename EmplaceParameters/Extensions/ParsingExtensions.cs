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
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Language.Intellisense;
using Type = System.Type;

namespace EmplaceParameters.Extensions
{
    internal static class ParsingExtensions
    {
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

        /// <summary>
        /// In the given node, return the first typedef statement that matches the provided predicate.
        /// </summary>
        /// <param name="declaration"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static ClangSharp.Type ResolveTypeAlias(this Cursor declaration, Func<NamedDecl, bool> filter)
        {
            var typedefType = declaration.CursorChildren.OfType<TypedefNameDecl>().FirstOrDefault<TypedefNameDecl>(filter);
            if (typedefType != null)
                return typedefType.UnderlyingType;

            var usingType = declaration.CursorChildren.OfType<UsingDecl>().FirstOrDefault<UsingDecl>(filter);
            return null;
        }

        public static TemplateSpecializationType GetTemplateSpecializationType(this ClangSharp.Type type)
        {
            if (type is ElaboratedType elaboratedType)
                return GetTemplateSpecializationType(elaboratedType.NamedType);

            if (type is TemplateSpecializationType templateSpecializationType)
                return templateSpecializationType;

            return null;
        }
    }
}
