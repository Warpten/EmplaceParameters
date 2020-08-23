using ClangSharp;
using ClangSharp.Interop;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LinqExpr = System.Linq.Expressions.Expression;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Type = ClangSharp.Type;

namespace EmplaceParameters.Parsing
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    internal class StatementHandlerAttribute : Attribute
    {
        public CX_StmtClass Kind { get; }

        public StatementHandlerAttribute(CX_StmtClass statementClass)
        {
            Kind = statementClass;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    internal class DeclarationHandlerAttribute : Attribute
    {
        public CX_DeclKind Kind { get; }

        public DeclarationHandlerAttribute(CX_DeclKind declKind)
        {
            Kind = declKind;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    internal class AttributeHandlerAttribute : Attribute
    {
        public CX_AttrKind Kind { get; }

        public AttributeHandlerAttribute(CX_AttrKind attrKind)
        {
            Kind = attrKind;
        }
    }

    public abstract class Visitor
    {
        private static Dictionary<CX_StmtClass, Action<Visitor, Stmt>> _statementHandlers = new Dictionary<CX_StmtClass, Action<Visitor, Stmt>>();
        private static Dictionary<CX_DeclKind, Action<Visitor, Decl>> _declarationHandlers = new Dictionary<CX_DeclKind, Action<Visitor, Decl>>();
        private static Dictionary<CX_AttrKind, Action<Visitor, Attr>> _attributeHandlers = new Dictionary<CX_AttrKind, Action<Visitor, Attr>>();

        static Visitor()
        {
            Action<Visitor, TBase> createLambda<TBase>(System.Type parameterType, MethodInfo methodInfo)
            {
                var visitorParam = LinqExpr.Parameter(typeof(Visitor), "visitor");
                var objectParam = LinqExpr.Parameter(typeof(TBase), "obj");

                MethodCallExpression methodCall = null;

                if (parameterType == typeof(TBase))
                {
                    methodCall = LinqExpr.Call(visitorParam, methodInfo, objectParam);
                }
                else
                {
                    var castExpr = LinqExpr.Convert(objectParam, parameterType);
                    methodCall = LinqExpr.Call(visitorParam, methodInfo, castExpr);
                }

                return LinqExpr.Lambda<Action<Visitor, TBase>>(methodCall, visitorParam, objectParam)
                    .Compile();
            }

            var methods = typeof(Visitor).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var methodInfo in methods)
            {
                if (methodInfo.ReturnType != typeof(void))
                    continue;

                if (!methodInfo.IsFamily)
                    continue;

                var parameterType = methodInfo.GetParameters().FirstOrDefault()?.ParameterType;
                if (parameterType == null)
                    continue;

                var statementHandler = methodInfo.GetCustomAttribute<StatementHandlerAttribute>();
                if (statementHandler != null)
                    _statementHandlers.Add(statementHandler.Kind, createLambda<Stmt>(parameterType, methodInfo));

                var declarationHandler = methodInfo.GetCustomAttribute<DeclarationHandlerAttribute>();
                if (declarationHandler != null)
                    _declarationHandlers.Add(declarationHandler.Kind, createLambda<Decl>(parameterType, methodInfo));

                var attributeHandler = methodInfo.GetCustomAttribute<AttributeHandlerAttribute>();
                if (attributeHandler != null)
                    _attributeHandlers.Add(attributeHandler.Kind, createLambda<Attr>(parameterType, methodInfo));

            }
        }

        public void Visit(TranslationUnit translationUnit)
        {
            foreach (var cursor in translationUnit.TranslationUnitDecl.CursorChildren)
                VisitCursor(cursor);
        }

        private void VisitCursor(Cursor cursor)
        {
            if (!ShouldVisit(cursor))
                return;

            foreach (var child in cursor.CursorChildren)
                VisitCursor(child);

            switch (cursor)
            {
                case Attr attr:
                    VisitAttr(attr);
                    break;
                case Decl decl:
                    VisitDecl(decl);
                    break;
                case Ref @ref:
                    VisitRef(@ref);
                    break;
                case Stmt stmt:
                    VisitStmt(stmt);
                    break;
            }
        }

        protected virtual bool ShouldVisit(Cursor cursor)
        {
            return true;
        }

        private void VisitDecl(Decl decl)
        {
            if (_declarationHandlers.TryGetValue(decl.Kind, out var handler))
                handler(this, decl);
        }

        private void VisitStmt(Stmt stmt)
        {
            if (_statementHandlers.TryGetValue(stmt.StmtClass, out var handler))
                handler(this, stmt);
        }

        private void VisitAttr(Attr attr)
        {
            if (_attributeHandlers.TryGetValue(attr.Kind, out var handler))
                handler(this, attr);
        }

        private void VisitRef(Ref @ref)
        {
            VisitCursor(@ref.Referenced);
        }

        [StatementHandler(CX_StmtClass.CX_StmtClass_CXXMemberCallExpr)]
        protected virtual void VisitMemberCallExpr(CXXMemberCallExpr memberCallExpr) { }
    }
}
