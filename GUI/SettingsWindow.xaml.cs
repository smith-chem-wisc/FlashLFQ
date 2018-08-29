using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using FlashLFQ;

namespace GUI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        internal FlashLFQEngine TempFlashLfqEngine { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
        }

        public void PopulateSettings(FlashLFQEngine engine)
        {
            normalize.IsChecked = engine.Normalize;
            ppmTolerance.Text = engine.PpmTolerance.ToString(CultureInfo.InvariantCulture);
            mbr.IsChecked = engine.MatchBetweenRuns;
            advancedProteinQuant.IsChecked = engine.AdvancedProteinQuant;

            integrate.IsChecked = engine.Integrate;
            precursorChargeOnly.IsChecked = engine.IdSpecificChargeState;
            requireMonoisotopicPeak.IsChecked = engine.RequireMonoisotopicMass;
            isotopeTolerance.Text = engine.IsotopePpmTolerance.ToString(CultureInfo.InvariantCulture);
            numIsotopePeak.Text = engine.NumIsotopesRequired.ToString();
            maxMbrWindow.Text = engine.MbrRtWindow.ToString(CultureInfo.InvariantCulture);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if(!CheckForValidEntries())
            {
                return;
            }

            TempFlashLfqEngine = new FlashLFQEngine(new List<Identification>(), 
                advancedProteinQuant: advancedProteinQuant.IsChecked.Value,
                normalize: normalize.IsChecked.Value, 
                ppmTolerance: double.Parse(ppmTolerance.Text),
                matchBetweenRuns: mbr.IsChecked.Value, 
                integrate: integrate.IsChecked.Value,
                idSpecificChargeState: precursorChargeOnly.IsChecked.Value,
                requireMonoisotopicMass: requireMonoisotopicPeak.IsChecked.Value,
                isotopeTolerancePpm: double.Parse(isotopeTolerance.Text),
                numIsotopesRequired: int.Parse(numIsotopePeak.Text),
                maxMbrWindow: double.Parse(maxMbrWindow.Text));

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private bool CheckForValidEntries()
        {
            if(!double.TryParse(ppmTolerance.Text, out double ppmTol) || ppmTol < 0)
            {
                MessageBox.Show("PPM tolerance must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }

            if (!double.TryParse(isotopeTolerance.Text, out double isotopePpmTol) || isotopePpmTol < 0)
            {
                MessageBox.Show("Isotope PPM tolerance must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }

            if (!double.TryParse(maxMbrWindow.Text, out double maxMbr) || maxMbr < 0)
            {
                MessageBox.Show("Max MBR window must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }

            if (!int.TryParse(numIsotopePeak.Text, out int numIsotopesRequired) || numIsotopesRequired < 1)
            {
                MessageBox.Show("Num isotope peaks must be >= 1", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return false;
            }

            return true;
        }
    }
}
