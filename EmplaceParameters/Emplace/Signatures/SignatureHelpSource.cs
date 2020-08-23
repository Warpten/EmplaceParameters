using ClangSharp;
using ClangSharp.Interop;

using EmplaceParameters.Parsing;

using EnvDTE;

using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.VCCodeModel;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using EmplaceParameters.Extensions;

namespace EmplaceParameters.Emplace.Signatures
{
    internal class SignatureHelpSource : ISignatureHelpSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly SVsServiceProvider _serviceProvider;
        private readonly ISignatureHelpBroker _broker;
        private readonly DTE _dte;
        private bool _disposed;

        public SignatureHelpSource(ITextBuffer textBuffer, SVsServiceProvider serviceProvider)
        {
            _textBuffer = textBuffer;
            _serviceProvider = serviceProvider; 

            _broker = _serviceProvider.GetService<ISignatureHelpBroker>();
            _dte = _serviceProvider.GetService<DTE>();
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var snapshot = _textBuffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(_textBuffer);
            var snapshotPoint = triggerPoint.GetPoint(snapshot);
            var position = triggerPoint.GetPosition(snapshot);

            var applicableToSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(new Span(position, 0), SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward);

            // Parse the entire document into an AST
            var index = CXIndex.Create(false, false);
            if ((_dte.ActiveDocument, snapshot).TryParseDocument(index, out var translationUnit, out _)) 
            {
                var visitor = new EmplaceVisitor(_dte.ActiveDocument.FullName, snapshotPoint.GetContainingLine().LineNumber);
                visitor.CtorFound += (methodName, parameters) =>
                {
                    // TODO: eliminate this stupid back-and-forth string handling

                    var signatureBuilder = new StringBuilder(methodName);
                    signatureBuilder.Append('(');
                    foreach (var parameter in parameters)
                        signatureBuilder.Append(parameter.Type).Append(' ').Append(parameter.Name).Append(", ");

                    if (parameters.Count > 0)
                        signatureBuilder.Length -= 2;
                    signatureBuilder.Append(')');

                    signatures.Add(CreateSignature(_textBuffer, signatureBuilder.ToString(), applicableToSpan));
                };
                
                visitor.Visit(translationUnit);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
                GC.SuppressFinalize(this);

            _disposed = true;
        }

        public ISignature GetBestMatch(ISignatureHelpSession session)
        {
            if (session.Signatures.Count > 0)
            {
                var applicableToSpan = session.Signatures[0].ApplicableToSpan;
                var text = applicableToSpan.GetText(applicableToSpan.TextBuffer.CurrentSnapshot);

                // TODO: Parse again but this time constraint the ctors retrieved to those that match current parameter types
            }
            return null;
        }

        private static Signature CreateSignature(ITextBuffer textBuffer, string methodSig, ITrackingSpan span)
        {
            var sig = new Signature(textBuffer, methodSig, null);

            // Find the parameters in the method signature
            var pars = methodSig.Split('(', ',', ')');
            var paramList = new List<IParameter>();

            var locusSearchStart = 0;
            for (var i = 1; i < pars.Length; i++)
            {
                var param = pars[i].Trim();

                if (string.IsNullOrEmpty(param))
                    continue;

                // Find where this parameter is located in the method signature
                var locusStart = methodSig.IndexOf(param, locusSearchStart, StringComparison.InvariantCulture);
                if (locusStart >= 0)
                {
                    var locus = new Span(locusStart, param.Length);
                    locusSearchStart = locusStart + param.Length;
                    paramList.Add(new Parameter(null, locus, param, sig));
                }
            }

            sig.Parameters = new ReadOnlyCollection<IParameter>(paramList);
            sig.ApplicableToSpan = span;
            sig.ComputeCurrentParameter();
            return sig;
        }
    }

    [Export(typeof(ISignatureHelpSourceProvider))]
    [Name("Signature Help source")]
    [Order(Before = "default")]
    [ContentType("text")]
    [FileExtension(".cpp")]
    internal class SignatureHelpSourceProvider : ISignatureHelpSourceProvider
    {
        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            return new SignatureHelpSource(textBuffer, ServiceProvider);
        }
    }
}
