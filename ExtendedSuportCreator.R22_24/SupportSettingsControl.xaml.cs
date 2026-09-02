using Autodesk.Revit.DB;
using System.Windows;
using System.Windows.Controls;

namespace ExtendedSuportCreator.R22_24
{
    /// <summary>
    /// Interaction logic for SupportSettingsControl.xaml
    /// </summary>
    public partial class SupportSettingsControl : UserControl
    {
        private readonly Document _doc;
        public double StepInFeet { get; private set; }
        public double MinOffset { get; private set; }
        public double MaxOffset { get; private set; }

        public SupportSettingsControl(Document doc)
        {
            InitializeComponent();
            _doc = doc;
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (!TryParseLength(StepTextBox.Text, out double step) || step <= 0)
            {
                ShowError("Step must be a positive length (e.g. 1' 0\").");
                return;
            }
            if (!TryParseLength(MinOffsetTextBox.Text, out double minOffset) ||
                !TryParseLength(MaxOffsetTextBox.Text, out double maxOffset))
            {
                ShowError("Offsets must be a valid length (e.g. 6' 2 1/2\").");
                return;
            }

            if (minOffset > maxOffset)
            {
                ShowError("Min offset cannot exceed max offset.");
                return;
            }

            StepInFeet = step;
            MinOffset = minOffset;
            MaxOffset = maxOffset;

            Window.GetWindow(this).DialogResult = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).DialogResult = false;
        }

        private bool TryParseLength(string text, out double valueInFeet)
        {
            return UnitFormatUtils.TryParse(_doc.GetUnits(), SpecTypeId.Length, text, out valueInFeet);
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = System.Windows.Visibility.Visible;
        }

    }
}
