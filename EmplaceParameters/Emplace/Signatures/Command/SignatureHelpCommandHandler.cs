using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace EmplaceParameters.Emplace.Signatures.Command
{
    internal class SignatureHelpCommandHandler : IOleCommandTarget
    {
        private IOleCommandTarget _nextCommandHandler;
        private ITextView _textView;
        private ISignatureHelpBroker _broker;
        private ISignatureHelpSession _session;
        private ITextStructureNavigator _navigator;

        internal SignatureHelpCommandHandler(IVsTextView textViewAdapter, ITextView textView, ITextStructureNavigator nav, ISignatureHelpBroker broker)
        {
            _textView = textView;
            _broker = broker;
            _navigator = nav;

            // Add to the filter chain
            textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
                if (typedChar.Equals('('))
                {
                    // Move the point back so it's in the preceding word
                    var point = _textView.Caret.Position.BufferPosition - 1;
                    var extent = _navigator.GetExtentOfWord(point);
                    var word = extent.Span.GetText();

                    if (word.Equals("emplace") || word.Equals("emplace_back"))
                        _session = _broker.TriggerSignatureHelp(_textView);

                }
                else if (typedChar.Equals(')') && _session != null)
                {
                    _session.Dismiss();
                    _session = null;
                }
            }

            return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
