using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace EmplaceParameters.Emplace.Signatures.Command
{
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("Signature Help controller")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [ContentType("text")]
    [FileExtension(".cpp")]
    internal class SignatureHelpCommandProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ISignatureHelpBroker SignatureHelpBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = AdapterService.GetWpfTextView(textViewAdapter);

            textView?.Properties.GetOrCreateSingletonProperty(() => new SignatureHelpCommandHandler(textViewAdapter,
                textView,
                NavigatorService.GetTextStructureNavigator(textView.TextBuffer),
                SignatureHelpBroker));
        }
    }
}
