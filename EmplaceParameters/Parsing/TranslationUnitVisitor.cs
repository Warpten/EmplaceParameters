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

namespace EmplaceParameters.Parsing
{
    public abstract class TranslationUnitVisitor
    {
        public void Visit(TranslationUnit translationUnit)
        {
            Visit(translationUnit.TranslationUnitDecl);
        }

        public void Visit(TranslationUnitDecl translationUnit)
        {
            foreach (var cursor in translationUnit.CursorChildren)
                VisitCursor(cursor);
        }

        private void VisitCursor(Cursor cursor)
        {
            if (!ShouldVisit(cursor))
                return;

            /*foreach (var enumValue in Enum.GetNames(typeof(CXCursorKind)))
                if (enumValue.EndsWith("Decl"))
                    Debug.WriteLine($"{enumValue}");*/

            foreach (var child in cursor.CursorChildren)
                VisitCursor(child);

          /*  switch (cursor.CursorKind)
            {
                // case CXCursorKind.CXCursor_FirstExpr:
                case CXCursorKind.CXCursor_UnexposedExpr:
                case CXCursorKind.CXCursor_DeclRefExpr:
                case CXCursorKind.CXCursor_MemberRefExpr:
                case CXCursorKind.CXCursor_CallExpr:
                case CXCursorKind.CXCursor_ObjCMessageExpr:
                case CXCursorKind.CXCursor_BlockExpr:
                case CXCursorKind.CXCursor_ParenExpr:
                case CXCursorKind.CXCursor_ArraySubscriptExpr:
                case CXCursorKind.CXCursor_CStyleCastExpr:
                case CXCursorKind.CXCursor_CompoundLiteralExpr:
                case CXCursorKind.CXCursor_InitListExpr:
                case CXCursorKind.CXCursor_AddrLabelExpr:
                case CXCursorKind.CXCursor_StmtExpr:
                case CXCursorKind.CXCursor_GenericSelectionExpr:
                case CXCursorKind.CXCursor_GNUNullExpr:
                case CXCursorKind.CXCursor_CXXStaticCastExpr:
                case CXCursorKind.CXCursor_CXXDynamicCastExpr:
                case CXCursorKind.CXCursor_CXXReinterpretCastExpr:
                case CXCursorKind.CXCursor_CXXConstCastExpr:
                case CXCursorKind.CXCursor_CXXFunctionalCastExpr:
                case CXCursorKind.CXCursor_CXXTypeidExpr:
                case CXCursorKind.CXCursor_CXXBoolLiteralExpr:
                case CXCursorKind.CXCursor_CXXNullPtrLiteralExpr:
                case CXCursorKind.CXCursor_CXXThisExpr:
                case CXCursorKind.CXCursor_CXXThrowExpr:
                case CXCursorKind.CXCursor_CXXNewExpr:
                case CXCursorKind.CXCursor_CXXDeleteExpr:
                case CXCursorKind.CXCursor_UnaryExpr:
                case CXCursorKind.CXCursor_ObjCEncodeExpr:
                case CXCursorKind.CXCursor_ObjCSelectorExpr:
                case CXCursorKind.CXCursor_ObjCProtocolExpr:
                case CXCursorKind.CXCursor_ObjCBridgedCastExpr:
                case CXCursorKind.CXCursor_PackExpansionExpr:
                case CXCursorKind.CXCursor_SizeOfPackExpr:
                case CXCursorKind.CXCursor_LambdaExpr:
                case CXCursorKind.CXCursor_ObjCBoolLiteralExpr:
                case CXCursorKind.CXCursor_ObjCSelfExpr:
                case CXCursorKind.CXCursor_OMPArraySectionExpr:
                case CXCursorKind.CXCursor_ObjCAvailabilityCheckExpr:
                case CXCursorKind.CXCursor_LastExpr:
                case CXCursorKind.CXCursor_BuiltinBitCastExpr:
                    VisitExpr((Expr) cursor);
                    break;
            }*/

            switch (cursor)
            {
                case Decl decl:
                    VisitDecl(decl);
                    break;
                case Expr expr:
                    VisitExpr(expr);
                    break;
                case Stmt stmt:
                    VisitStmt(stmt);
                    break;
                case Attr attr:
                    VisitAttr(attr);
                    break;
                case Ref @ref:
                    VisitRef(@ref);
                    break;
                default:
                    Debug.WriteLine("> Failed to match cursor.");
                    break;
            }
        }

        protected virtual bool ShouldVisit(Cursor cursor)
        {
            return true;
        }

        protected virtual void VisitDecl(Decl decl) { }
        protected virtual void VisitStmt(Stmt stmt) { }
        protected virtual void VisitAttr(Attr attr) { }

        private void VisitExpr(Expr expr)
        {
            switch (expr.StmtClass)
            {
                case CX_StmtClass.CX_StmtClass_CXXMemberCallExpr:
                    VisitMemberCallExpr((CXXMemberCallExpr) expr);
                    break;
            }
        }

        protected virtual void VisitRef(Ref @ref) { }

        protected virtual void VisitMemberCallExpr(CXXMemberCallExpr memberCallExpr) { }
    }
}
