using System.ComponentModel.Composition;
using System.Windows.Media;

using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace EmplaceParameters.Emplace.Classifier
{
    /// <summary>
    /// Defines an editor format for the EmplaceClassifier type that has a purple background
    /// and is underlined.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "EmplaceClassifier")]
    [Name("EmplaceClassifier")]
    [UserVisible(true)] // This should be visible to the end user
    [Order(Before = Priority.Default)] // Set the priority to be after the default classifiers
    internal sealed class EmplaceClassifierFormat : ClassificationFormatDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmplaceClassifierFormat"/> class.
        /// </summary>
        public EmplaceClassifierFormat()
        {
            DisplayName = "Emplace Classifier"; // Human readable version of the name
        }
    }
}
