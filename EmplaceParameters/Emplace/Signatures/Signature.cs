using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmplaceParameters.Emplace.Signatures
{
    internal class Signature : ISignature
    {
        private ITextBuffer _subjectBuffer;
        private IParameter _currentParameter;

        public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

        public IParameter CurrentParameter
        {
            get => _currentParameter;
            private set
            {
                if (_currentParameter == value)
                    return;

                var prevCurrentParameter = _currentParameter;
                _currentParameter = value;
                CurrentParameterChanged?.Invoke(this, new CurrentParameterChangedEventArgs(prevCurrentParameter, _currentParameter));
            }
        }

        public ITrackingSpan ApplicableToSpan { get; set; }
        public string Content { get; set; }
        public string PrettyPrintedContent { get; set; }
        public string Documentation { get; set; }
        public ReadOnlyCollection<IParameter> Parameters { get; set; }

        internal Signature(ITextBuffer subjectBuffer, string content, ReadOnlyCollection<IParameter> parameters)
        {
            _subjectBuffer = subjectBuffer;
            _subjectBuffer.Changed += (sender, e) => {
                ComputeCurrentParameter();
            };

            Content = content;
            Documentation = null;
            Parameters = parameters;

        }

        internal void ComputeCurrentParameter()
        {
            if (Parameters.Count == 0)
            {
                CurrentParameter = null;
                return;
            }

            //the number of commas in the string is the index of the current parameter
            var sigText = ApplicableToSpan.GetText(_subjectBuffer.CurrentSnapshot);

            var currentIndex = 0;
            var commaCount = 0;
            while (currentIndex < sigText.Length)
            {
                var commaIndex = sigText.IndexOf(',', currentIndex);
                if (commaIndex == -1)
                    break;

                commaCount++;
                currentIndex = commaIndex + 1;
            }

            if (commaCount < Parameters.Count)
                CurrentParameter = Parameters[commaCount];
            else
            {
                // Too many commas, so use the last parameter as the current one.
                CurrentParameter = Parameters.Last();
            }
        }
    }
}
