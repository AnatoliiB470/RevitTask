using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExtendedSuportCreator.R22_24
{
    /// <summary>
    /// Interaction logic for SupportSettingsControl.xaml
    /// </summary>
    public partial class SupportSettingsControl : UserControl
    {
        public double StepInFeet { get; private set; }
        public double MinOffset { get; private set; }
        public double MaxOffset { get; private set; }

        public SupportSettingsControl()
        {
            InitializeComponent();
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(StepTextBox.Text, out double step) || step <= 0)
            {
                ShowError("Step must be a positive number.");
                return;
            }

            if (!double.TryParse(MinOffsetTextBox.Text, out double minOffset) ||
                !double.TryParse(MaxOffsetTextBox.Text, out double maxOffset))
            {
                ShowError("Offsets must be numbers.");
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

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

    }
}
