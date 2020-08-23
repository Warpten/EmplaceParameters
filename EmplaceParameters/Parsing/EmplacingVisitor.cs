using ClangSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClangSharp.Interop;
using EmplaceParameters.Extensions;
using Type = System.Type;

namespace EmplaceParameters.Parsing
{
    public sealed class EmplacingVisitor : Visitor
    {
        private string _filePath;
        private int _line;

        public delegate void CtorFoundHandler(IReadOnlyList<NamedDecl> templateParameters, IReadOnlyList<ParmVarDecl> parameters);
        public event CtorFoundHandler CtorFound;

        public EmplacingVisitor(string filePath, int line) : base()
        {
            _line = line;
            _filePath = filePath;
        }

        protected override bool ShouldVisit(Cursor cursor)
        {
            // Only visit nodes of the AST contained in the main file.

            cursor.Location.GetFileLocation(out var file, out _, out _, out _);
            if (file.Name.CString != _filePath)
                return false;

            return true;
        }

        protected override void VisitMemberCallExpr(CXXMemberCallExpr memberCallExpression)
        {
            // TODO: This is frail and breaks if a line has nested emplace_back (we push out signatures for all types constructed by emplace_back)

            // Only process emplace(_back) calls on the requested line and within the requested span
            memberCallExpression.Location.GetFileLocation(out var file, out var line, out _, out _);
            if (file.Name.CString != _filePath || line - 1 != _line)
                return;

            // Check if this is a method call expression.
            var methodReference = memberCallExpression.CursorChildren.FirstOrDefault(child => child.CursorKind == CXCursorKind.CXCursor_MemberRefExpr);
            if (methodReference == default)
                return;

            // Needs to be emplace or emplace_back
            if (methodReference.Spelling != "emplace" && methodReference.Spelling != "emplace_back")
                return;

            // Find the object instance on which the function is called
            var objectInstance = methodReference.CursorChildren.OfType<DeclRefExpr>().FirstOrDefault();
            if (objectInstance == default)
                return;

            // Find the actual object type (not the instanciated specialization)
            var objectType = objectInstance.Decl.CursorChildren
                .FirstOrDefault(child => child.CursorKind == CXCursorKind.CXCursor_TemplateRef)
                .As<Ref>()?.Referenced as ClassTemplateDecl;
            if (objectType == default)
                return;

            // From the resolved type, find the 'value_type" typedef.
            var valueType = objectType.ResolveTypeAlias(typedef => typedef.Spelling == "value_type");
            if (valueType == default)
                return;

            // Retrieve the index of the actual type in the pack declaration
            //! Not a .OfType<TemplateTypeParmDecl>() because of non-type template parameters.
            var elementTypeIndex = objectType.TemplateParameters.IndexOf(param => param is TemplateTypeParmDecl typeParam && typeParam.TypeForDecl == valueType);
            if (elementTypeIndex == -1)
                return;

            // Retrieve the instanciated specialization
            var instanceType = objectInstance.Type.GetTemplateSpecializationType()?
                .CanonicalType.As<RecordType>()?
                .Decl.As<ClassTemplateSpecializationDecl>();
            if (instanceType == null)
                return;

            // Find the argument type. If not a record type, exit out.
            var resolvedElementType = instanceType.TemplateArgs[elementTypeIndex].CanonicalType.As<RecordType>()?.Decl;
            if (resolvedElementType == default)
                return;

            switch (resolvedElementType)
            {
                case CXXRecordDecl recordDecl:
                    {
                        foreach (var constructor in recordDecl.Ctors)
                            TryNotifyConstructorFound(instanceType, constructor);

                        // Also search for function template ctors
                        foreach (var functionTemplate in recordDecl.CursorChildren.OfType<FunctionTemplateDecl>())
                        {
                            // Only keep template ctors
                            if (functionTemplate.Spelling != resolvedElementType.Spelling)
                                continue;

                            TryNotifyConstructorFound(instanceType, functionTemplate);
                        }

                        break;
                    }
                default: // Could be a builtin
                    Debug.WriteLine($"> Found an emplace call to a container with an unhandled {resolvedElementType.CursorKindSpelling}.");
                    break;
            }
        }

        private void TryNotifyConstructorFound(CXXRecordDecl caller, FunctionTemplateDecl functionTemplate)
        {
            // TODO: Friend containers?
            if (functionTemplate.Access != CX_CXXAccessSpecifier.CX_CXXPublic)
                return;

            CtorFound?.Invoke(functionTemplate.TemplateParameters,
                functionTemplate.CursorChildren.Where(child => child.CursorKind == CXCursorKind.CXCursor_ParmDecl).Cast<ParmVarDecl>().ToList());
        }

        private void TryNotifyConstructorFound(CXXRecordDecl caller, FunctionDecl constructor)
        {
            // If no body, error out (= delete)
            if (constructor.IsInvalidDecl || constructor.Body == null)
                return;

            // TODO: Friend containers?
            if (constructor.Access != CX_CXXAccessSpecifier.CX_CXXPublic)
                return;

            CtorFound?.Invoke(null, constructor.Parameters.ToList());
        }
    }
}
