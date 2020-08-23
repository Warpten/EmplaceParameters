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
    public sealed class EmplaceVisitor : Visitor
    {
        private string _filePath;
        private int _line;

        public event Action<string, List<Parameter>> CtorFound;

        public EmplaceVisitor(string filePath, int line) : base()
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
            var objectInstance = methodReference.CursorChildren.FirstOrDefault(child => child is DeclRefExpr).As<DeclRefExpr>();
            if (objectInstance == default)
                return;

            // Find the actual object type (not the instanciated specialization)
            var objectType = objectInstance.Decl.CursorChildren
                .FirstOrDefault(child => child.CursorKind == CXCursorKind.CXCursor_TemplateRef)
                .As<Ref>()?.Referenced as ClassTemplateDecl;
            if (objectType == default)
                return;

            // From the resolved type, find the 'value_type" typedef.
            var valueTypedef = objectType.CursorChildren
                .FirstOrDefault(child => child.CursorKind == CXCursorKind.CXCursor_TypeAliasDecl && child.Spelling == "value_type")
                .As<TypedefNameDecl>();
            if (valueTypedef == default)
                return;

            var valueType = valueTypedef.CursorChildren.Single().Cast<Ref>().Referenced;

            // Retrieve the index of the actual type in the pack declaration
            var elementTypeIndex = objectType.TemplateParameters.IndexOf(valueType);
            if (elementTypeIndex == -1)
                return;

            // Retrieve the instanciated specialization
            var instanceType = objectInstance.Type.As<ElaboratedType>()?
                .NamedType.As<TemplateSpecializationType>()?
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
                        NotifyConstructorFound(methodReference.Spelling, constructor);

                    // Also search for function template ctors
                    var functionTemplates = recordDecl.CursorChildren.Where(child => child.CursorKind == CXCursorKind.CXCursor_FunctionTemplate);
                    foreach (var functionTemplate in functionTemplates.Cast<FunctionTemplateDecl>())
                    {
                        // Only keep template ctors
                        if (functionTemplate.Spelling != resolvedElementType.Spelling)
                            continue;

                        NotifyConstructorFound(methodReference.Spelling, functionTemplate);
                    }
                    break;
                }
                default: // Could be a builtin
                    Debug.WriteLine($"> Found an emplace call to a container with an unhandled {resolvedElementType.CursorKindSpelling}.");
                    break;
            }
        }

        private void NotifyConstructorFound(string methodName, FunctionTemplateDecl functionTemplate)
        {
            var parameters = new List<Parameter>();

            var defaultValueConstructorShown = false;

            var methodNameBuilder = new StringBuilder(methodName);
            methodNameBuilder.Append('<');
            foreach (var parameterType in functionTemplate.TemplateParameters)
                methodNameBuilder.Append(parameterType.Spelling).Append(", ");
            methodNameBuilder.Length -= 2;
            methodNameBuilder.Append('>');

            methodName = methodNameBuilder.ToString();

            foreach (var child in functionTemplate.CursorChildren.Where(child => child.CursorKind == CXCursorKind.CXCursor_ParmDecl))
            {
                var parameterDeclaration = child.As<ParmVarDecl>();
                var parameter = new Parameter(parameterDeclaration);
                if (!defaultValueConstructorShown && parameterDeclaration.HasDefaultArg)
                {
                    // As soon as a default parameter is found all following parameters are defaulted
                    // Which means that there's an extra "pseudo" constructor without all defaulted parameters
                    CtorFound?.Invoke(methodName, parameters);
                    defaultValueConstructorShown = true;
                }

                parameters.Add(parameter);
            }

            CtorFound?.Invoke(methodName, parameters);

        }

        private void NotifyConstructorFound(string methodName, CXXConstructorDecl constructor)
        {
            if (constructor.IsInvalidDecl)
                return;

            var children = constructor.CursorChildren.Where(child => child.CursorKind == CXCursorKind.CXCursor_ParmDecl);

            var parameters = new List<Parameter>();

            var defaultValueConstructorShown = false;
            foreach (var child in children)
            {
                var parameterDeclaration = child.As<ParmVarDecl>();
                var parameter = new Parameter(parameterDeclaration);
                if (!defaultValueConstructorShown && parameterDeclaration.HasDefaultArg)
                {
                    // As soon as a default parameter is found all following parameters are defaulted
                    // Which means that there's an extra "pseudo" constructor without all defaulted parameters
                    CtorFound?.Invoke(methodName, parameters);
                    defaultValueConstructorShown = true;
                }

                parameters.Add(parameter);
            }

            CtorFound?.Invoke(methodName, parameters);
        }
    }
}
