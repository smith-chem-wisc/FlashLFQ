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
using System.Windows.Shapes;
using FlashLFQ;

namespace GUI
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        internal FlashLFQEngine tempFlashLfqEngine { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
        }

        public void PopulateSettings(FlashLFQEngine engine)
        {
            normalize.IsChecked = engine.Normalize;
            ppmTolerance.Text = engine.PpmTolerance.ToString();
            mbr.IsChecked = engine.MatchBetweenRuns;
            advancedProteinQuant.IsChecked = engine.AdvancedProteinQuant;

            integrate.IsChecked = engine.Integrate;
            precursorChargeOnly.IsChecked = engine.IdSpecificChargeState;
            requireMonoisotopicPeak.IsChecked = engine.RequireMonoisotopicMass;
            isotopeTolerance.Text = engine.IsotopePpmTolerance.ToString();
            numIsotopePeak.Text = engine.NumIsotopesRequired.ToString();
            maxMbrWindow.Text = engine.MbrRtWindow.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if(!CheckForValidEntries())
            {
                return;
            }

            tempFlashLfqEngine = new FlashLFQEngine(new List<Identification>(), 
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
