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
            normalize.IsChecked = engine.normalize;
            ppmTolerance.Text = engine.ppmTolerance.ToString();
            mbr.IsChecked = engine.mbr;
            integrate.IsChecked = engine.integrate;
            precursorChargeOnly.IsChecked = engine.idSpecificChargeState;
            requireMonoisotopicPeak.IsChecked = engine.requireMonoisotopicMass;
            isotopeTolerance.Text = engine.isotopePpmTolerance.ToString();
            numIsotopePeak.Text = engine.numIsotopesRequired.ToString();
            maxMbrWindow.Text = engine.mbrRtWindow.ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            CheckForValidEntries();

            tempFlashLfqEngine = new FlashLFQEngine(new List<Identification>(), 
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

        private void CheckForValidEntries()
        {
            if(!double.TryParse(ppmTolerance.Text, out double ppmTol) || ppmTol < 0)
            {
                MessageBox.Show("PPM tolerance must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (!double.TryParse(isotopeTolerance.Text, out double isotopePpmTol) || isotopePpmTol < 0)
            {
                MessageBox.Show("Isotope PPM tolerance must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (!double.TryParse(maxMbrWindow.Text, out double maxMbr) || maxMbr < 0)
            {
                MessageBox.Show("Max MBR window must be >= 0", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (!int.TryParse(numIsotopePeak.Text, out int numIsotopesRequired) || numIsotopesRequired < 1)
            {
                MessageBox.Show("Num isotope peaks must be >= 1", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
        }
    }
}
