using FlashLFQ;
using GUI.DataGridObjects;
using IO.ThermoRawFileReader;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Util;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<SpectraFileForDataGrid> spectraFiles;
        private ObservableCollection<IdentificationFileForDataGrid> idFiles;
        private BackgroundWorker worker;
        private FlashLfqEngine flashLfqEngine;
        private FlashLfqSettings settings;
        private FlashLfqResults results;
        private string outputFolderPath;
        public static readonly string DefaultCondition = "Default";
        public static readonly string ExperimentalDesignFilename = "ExperimentalDesign.tsv";
        public ObservableCollection<string> conditions;

        public MainWindow()
        {
            InitializeComponent();
            PopulateSettings();

            spectraFiles = new ObservableCollection<SpectraFileForDataGrid>();

            conditions = new ObservableCollection<string>();
            ControlConditionComboBox.ItemsSource = conditions;

            // sort the spectra files by condition, then sample, then fraction, then replicate
            var collectionView = CollectionViewSource.GetDefaultView(spectraFiles);
            collectionView.SortDescriptions.Add(new SortDescription(nameof(SpectraFileForDataGrid.Condition), ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription(nameof(SpectraFileForDataGrid.Sample), ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription(nameof(SpectraFileForDataGrid.Fraction), ListSortDirection.Ascending));
            collectionView.SortDescriptions.Add(new SortDescription(nameof(SpectraFileForDataGrid.Replicate), ListSortDirection.Ascending));

            // file names are readonly
            spectraFilesDataGrid.Columns[0].IsReadOnly = true;
            identFilesDataGrid.Columns[0].IsReadOnly = true;

            idFiles = new ObservableCollection<IdentificationFileForDataGrid>();
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(RunProgram);

            flashLfqEngine = new FlashLfqEngine(new List<Identification>());

            identFilesDataGrid.ItemsSource = idFiles;
            spectraFilesDataGrid.DataContext = spectraFiles;

            BayesianSettings1.Visibility = Visibility.Hidden;
            BayesianSettings2.Visibility = Visibility.Hidden;

            var _writer = new TextBoxWriter(notificationsTextBox);
            Console.SetOut(_writer);
        }

        private void PopulateSettings()
        {
            settings = new FlashLfqSettings();
            settings.Silent = false;

            // basic
            ppmToleranceTextBox.Text = settings.PpmTolerance.ToString("F1");
            normalizeCheckbox.IsChecked = settings.Normalize;
            mbrCheckbox.IsChecked = settings.MatchBetweenRuns;
            sharedPeptideCheckbox.IsChecked = settings.UseSharedPeptidesForProteinQuant;
            bayesianCheckbox.IsChecked = settings.BayesianProteinQuant;
            FoldChangeCutoffManualTextBox.Text = "0.5";

            // advanced
            integrateCheckBox.IsChecked = settings.Integrate;
            precursorIdOnlyCheckbox.IsChecked = settings.IdSpecificChargeState;
            isotopePpmToleranceTextBox.Text = settings.IsotopePpmTolerance.ToString("F1");
            numIsotopesRequiredTextBox.Text = settings.NumIsotopesRequired.ToString();
            mbrRtWindowTextBox.Text = settings.MbrRtWindow.ToString("F1");
            mcmcIterationsTextBox.Text = settings.McmcSteps.ToString();
            mcmcRandomSeedTextBox.Text = settings.RandomSeed.ToString();
            requireMsmsIdInConditionCheckbox.IsChecked = settings.RequireMsmsIdInCondition;
        }

        private void ParseSettings()
        {
            // check for ID/spectra files
            if (!spectraFiles.Any())
            {
                throw new Exception("You need to add at least one spectra file!");
            }
            if (!idFiles.Any())
            {
                throw new Exception("You need to add at least one identification file!");
            }

            // fold-change cutoff
            if (!double.TryParse(FoldChangeCutoffManualTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double foldChangeCutoff))
            {
                throw new Exception("The fold-change cutoff must be a decimal number");
            }

            settings.ProteinQuantFoldChangeCutoff = double.Parse(FoldChangeCutoffManualTextBox.Text, CultureInfo.InvariantCulture);

            // ppm tolerance
            if (double.TryParse(ppmToleranceTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double ppmTolerance))
            {
                settings.PpmTolerance = ppmTolerance;
            }
            else
            {
                throw new Exception("The PPM tolerance must be a decimal number");
            }

            settings.Normalize = normalizeCheckbox.IsChecked.Value;
            settings.MatchBetweenRuns = mbrCheckbox.IsChecked.Value;
            settings.UseSharedPeptidesForProteinQuant = sharedPeptideCheckbox.IsChecked.Value;
            settings.BayesianProteinQuant = bayesianCheckbox.IsChecked.Value;

            settings.Integrate = integrateCheckBox.IsChecked.Value;
            settings.IdSpecificChargeState = precursorIdOnlyCheckbox.IsChecked.Value;
            settings.ProteinQuantBaseCondition = (string)ControlConditionComboBox.SelectedItem;
            settings.RequireMsmsIdInCondition = requireMsmsIdInConditionCheckbox.IsChecked.Value;

            // isotope PPM tolerance
            if (double.TryParse(isotopePpmToleranceTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double isotopePpmTolerance))
            {
                settings.IsotopePpmTolerance = isotopePpmTolerance;
            }
            else
            {
                throw new Exception("The isotope PPM tolerance must be a decimal number");
            }

            // num isotopes required
            if (int.TryParse(numIsotopesRequiredTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numIsotopesRequired))
            {
                settings.NumIsotopesRequired = numIsotopesRequired;
            }
            else
            {
                throw new Exception("The number of isotopes required must be an integer");
            }

            // mcmc iterations
            if (int.TryParse(mcmcIterationsTextBox.Text, out int McmcSteps))
            {
                settings.McmcSteps = McmcSteps;
            }
            else
            {
                throw new Exception("The number of MCMC iterations must be an integer");
            }

            // random seed
            if (int.TryParse(mcmcRandomSeedTextBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int randomSeed))
            {
                settings.RandomSeed = randomSeed;
            }
            else
            {
                throw new Exception("The random seed must be an integer");
            }

            // MBR time tolerance
            if (double.TryParse(mbrRtWindowTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out double MbrRtWindow))
            {
                settings.MbrRtWindow = MbrRtWindow;
            }
            else
            {
                throw new Exception("The MBR time window must be a decimal number");
            }

            settings.ValidateSettings(spectraFiles.Select(p => p.SpectraFileInfo).ToList());
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void RunProgram(object sender, DoWorkEventArgs e)
        {
            RunFlashLfq();
        }

        /// <summary>
        /// This event fires when the "Add Spectra" button is clicked. It opens a Windows dialog 
        /// to select the desired spectra files.
        /// </summary>
        private void AddSpectra_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Spectra Files|*.raw;*.mzML",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                foreach (var rawDataFromSelected in openFileDialog1.FileNames.OrderBy(p => p))
                {
                    AddAFile(rawDataFromSelected);
                }
            }

            spectraFilesDataGrid.Items.Refresh();
        }

        /// <summary>
        /// This event fires when the "Add Identifications" button is clicked. It opens a Windows 
        /// dialog to select the desired identification files.
        /// </summary>
        private void AddIdentFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openPicker = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Identification Files|*.txt;*.tsv;*.psmtsv;*.tabular",
                FilterIndex = 1,
                RestoreDirectory = true,
                Multiselect = true
            };

            if (openPicker.ShowDialog() == true)
            {
                foreach (var filepath in openPicker.FileNames.OrderBy(p => p))
                {
                    AddAFile(filepath);
                }
            }

            identFilesDataGrid.Items.Refresh();
        }

        /// <summary>
        /// This event fires when the user has dragged+dropped files into the program.
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (Run.IsEnabled)
            {
                string[] files = ((string[])e.Data.GetData(DataFormats.FileDrop)).OrderBy(p => p).ToArray();

                if (files != null)
                {
                    foreach (var draggedFilePath in files)
                    {
                        // folder has been dragged+dropped
                        if (Directory.Exists(draggedFilePath))
                        {
                            foreach (string file in Directory.EnumerateFiles(draggedFilePath, "*.*", SearchOption.AllDirectories))
                            {
                                AddAFile(file);
                            }
                        }
                        // file has been dragged+dropped
                        else
                        {
                            AddAFile(draggedFilePath);
                        }
                        spectraFilesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        identFilesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        spectraFilesDataGrid.Items.Refresh();
                        identFilesDataGrid.Items.Refresh();
                    }
                }
            }
        }

        /// <summary>
        /// Handles adding a file (spectra or ID file). The source can be by drag+drop or through 
        /// clicking one of the "Add" buttons.
        /// </summary>
        private void AddAFile(string filePath)
        {
            string filename = Path.GetFileName(filePath);
            string theExtension = Path.GetExtension(filename).ToLowerInvariant();

            if (theExtension == ".raw")
            {
                var licenceAgreement = LicenceAgreementSettings.ReadLicenceSettings();

                if (!licenceAgreement.HasAcceptedThermoLicence)
                {
                    var thermoLicenceWindow = new ThermoLicenceAgreementWindow();
                    thermoLicenceWindow.LicenceText.AppendText(ThermoRawFileReaderLicence.ThermoLicenceText);
                    var dialogResult = thermoLicenceWindow.ShowDialog();

                    if (dialogResult.HasValue && dialogResult.Value == true)
                    {
                        try
                        {
                            licenceAgreement.AcceptLicenceAndWrite();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            switch (theExtension)
            {
                case ".raw":
                case ".mzml":
                    List<int> existingDefaultSampleNumbers = spectraFiles.Where(p => p.Condition == DefaultCondition)
                        .Select(p => p.Sample)
                        .Distinct()
                        .OrderBy(p => p).ToList();

                    int sampleNumber = 1;
                    for (int i = 1; i < int.MaxValue; i++)
                    {
                        if (!existingDefaultSampleNumbers.Contains(i))
                        {
                            sampleNumber = i;
                            break;
                        }
                    }

                    SpectraFileForDataGrid spectraFile = new SpectraFileForDataGrid(filePath, DefaultCondition, sampleNumber, 1, 1);
                    if (!spectraFiles.Select(f => f.FilePath).Contains(spectraFile.FilePath))
                    {
                        CheckForExistingExperimentalDesignFile(spectraFile);
                        spectraFiles.Add(spectraFile);
                        DragAndDropHelperLabelSpectraFiles.Visibility = Visibility.Hidden;
                    }
                    if (string.IsNullOrEmpty(OutputFolderTextBox.Text))
                    {
                        var pathOfFirstSpectraFile = Path.GetDirectoryName(spectraFiles.First().FilePath);
                        OutputFolderTextBox.Text = Path.Combine(pathOfFirstSpectraFile, @"FlashLFQ_$DATETIME");
                    }
                    break;

                case ".txt":
                case ".tsv":
                case ".psmtsv":
                case ".tabular":
                    IdentificationFileForDataGrid identFile = new IdentificationFileForDataGrid(filePath);
                    if (!idFiles.Select(f => f.FilePath).Contains(identFile.FilePath) && !identFile.FileName.Equals("ExperimentalDesign.tsv"))
                    {
                        bool valid = ValidateIdentificationFile(identFile);

                        if (valid)
                        {
                            idFiles.Add(identFile);
                            DragAndDropHelperLabelIdFiles.Visibility = Visibility.Hidden;
                        }
                    }
                    break;

                default:
                    //TODO: change this to popup
                    AddNotification("Unrecognized file type: " + theExtension);
                    break;
            }
        }

        /// <summary>
        /// This event fires when the user has clicked the "Run FlashLFQ" button. First, the program
        /// checks to see if the input is valid, and if it is, it runs the FlashLFQ engine. If the
        /// input is not valid, it tells the user why the input is not valid.
        /// </summary>
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParseSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => Run_Click(sender, e)));
            }
            else
            {
                // output folder
                if (string.IsNullOrEmpty(OutputFolderTextBox.Text))
                {
                    var pathOfFirstSpectraFile = Path.GetDirectoryName(spectraFiles.First().FilePath);
                    OutputFolderTextBox.Text = Path.Combine(pathOfFirstSpectraFile, @"FlashLFQ_@$DATETIME");
                }

                var startTimeForAllFilenames = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                string outputFolder = OutputFolderTextBox.Text.Replace("$DATETIME", startTimeForAllFilenames);
                OutputFolderTextBox.Text = outputFolder;
                outputFolderPath = outputFolder;
                settings.OutputPath = outputFolderPath;

                // write FlashLFQ settings to a file
                if (!Directory.Exists(settings.OutputPath))
                {
                    Directory.CreateDirectory(settings.OutputPath);
                }
                Nett.Toml.WriteFile(settings, Path.Combine(settings.OutputPath, "FlashLfqSettings.toml"));

                WriteExperimentalDesignToFile();

                // disable everything except opening output folder
                Run.IsEnabled = false;
                AddIdsButton.IsEnabled = false;
                AddSpectraButton.IsEnabled = false;
                spectraFilesDataGrid.IsReadOnly = true;
                identFilesDataGrid.IsReadOnly = true;
                ppmToleranceTextBox.IsEnabled = false;
                normalizeCheckbox.IsEnabled = false;
                mbrCheckbox.IsEnabled = false;
                sharedPeptideCheckbox.IsEnabled = false;
                bayesianCheckbox.IsEnabled = false;
                BayesianSettings1.IsEnabled = false;
                BayesianSettings2.IsEnabled = false;
                integrateCheckBox.IsEnabled = false;
                precursorIdOnlyCheckbox.IsEnabled = false;
                isotopePpmToleranceTextBox.IsEnabled = false;
                numIsotopesRequiredTextBox.IsEnabled = false;
                requireMsmsIdInConditionCheckbox.IsEnabled = false;
                mbrRtWindowTextBox.IsEnabled = false;
                mcmcIterationsTextBox.IsEnabled = false;
                mcmcRandomSeedTextBox.IsEnabled = false;

                OpenOutputFolderButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ecc71"));

                worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// This event fires when the user has clicked the "Open" button, to open the output folder.
        /// This opens the folder via the Windows file explorer.
        /// </summary>
        private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string outputFolder = OutputFolderTextBox.Text;
            if (outputFolder.Contains("$DATETIME"))
            {
                // the exact file path isn't known, so just open the parent directory
                outputFolder = Directory.GetParent(outputFolder).FullName;
            }

            if (!Directory.Exists(outputFolder) && !string.IsNullOrEmpty(outputFolder))
            {
                // create the directory if it doesn't exist yet
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception ex)
                {
                    AddNotification("Error opening directory: " + ex.Message);
                }
            }

            if (Directory.Exists(outputFolder))
            {
                // open the directory
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                {
                    FileName = outputFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                // this should only happen if the file path is empty or something unexpected happened
                AddNotification("Output folder does not exist");
            }
        }

        /// <summary>
        /// Runs the FlashLFQ engine with the user's defined spectra files, ID files, and FlashLFQ 
        /// settings.
        /// </summary>
        private void RunFlashLfq()
        {
            // read IDs
            var ids = new List<Identification>();
            List<string> peptidesForMbr = null;

            try
            {
                var allPepFile = idFiles.FirstOrDefault(idFile => idFile.FilePath.Contains("AllPeptides.psmtsv"));
                if(allPepFile!=null)
                {
                    List<Identification> idsForMbr = PsmReader.ReadPsms(allPepFile.FilePath, false, spectraFiles.Select(p => p.SpectraFileInfo).ToList(), settings.DonorQValueThreshold);
                    peptidesForMbr = idsForMbr.Select(id => id.ModifiedSequence).ToList();
                }
                foreach (var identFile in idFiles.Where(idFile => idFile != allPepFile))
                {
                    ids = ids.Concat(PsmReader.ReadPsms(identFile.FilePath, false, spectraFiles.Select(p => p.SpectraFileInfo).ToList(), settings.DonorQValueThreshold)).ToList();
                }
            }
            catch (Exception e)
            {
                string errorReportPath = Directory.GetParent(spectraFiles.First().FilePath).FullName;
                if (outputFolderPath != null)
                {
                    errorReportPath = outputFolderPath;
                }

                try
                {
                    OutputWriter.WriteErrorReport(e, Directory.GetParent(spectraFiles.First().FilePath).FullName,
                        outputFolderPath);
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("FlashLFQ has crashed with the following error: " + e.Message +
                    ".\nThe error report could not be written: " + ex2.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                MessageBox.Show("FlashLFQ could not read the PSM file: " + e.Message +
                    ".\nError report written to " + errorReportPath, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                return;
            }

            if (!ids.Any())
            {
                MessageBox.Show("No peptide IDs for the specified spectra files were found! " +
                    "Check to make sure the spectra file names match between the ID file and the spectra files",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                return;
            }

            if (ids.Any(p => p.Ms2RetentionTimeInMinutes > 500))
            {
                var res = MessageBox.Show("It seems that some of the retention times in the PSM file(s) are in seconds and not minutes; FlashLFQ requires the RT to be in minutes. " +
                    "Continue with the FlashLFQ run? (only click yes if the RTs are actually in minutes)",
                    "Error", MessageBoxButton.YesNo, MessageBoxImage.Hand);

                if (res == MessageBoxResult.No)
                {
                    return;
                }
            }

            // run FlashLFQ engine
            try
            {
                flashLfqEngine = FlashLfqSettings.CreateEngineWithSettings(settings, ids, peptidesForMbr);

                results = flashLfqEngine.Run();
            }
            catch (Exception ex)
            {
                string errorReportPath = Directory.GetParent(spectraFiles.First().FilePath).FullName;

                if (outputFolderPath != null)
                {
                    errorReportPath = outputFolderPath;
                }

                try
                {
                    OutputWriter.WriteErrorReport(ex, Directory.GetParent(spectraFiles.First().FilePath).FullName,
                        outputFolderPath);
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("FlashLFQ has crashed with the following error: " + ex.Message +
                    ".\nThe error report could not be written: " + ex2.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                MessageBox.Show("FlashLFQ has crashed with the following error: " + ex.Message +
                    ".\nError report written to " + errorReportPath, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                return;
            }

            // write output
            if (results != null)
            {
                try
                {
                    OutputWriter.WriteOutput(Directory.GetParent(spectraFiles.First().FilePath).FullName, results, flashLfqEngine.Silent,
                        outputFolderPath);

                    MessageBox.Show("Run complete");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not write FlashLFQ output: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }
            }
        }

        /// <summary>
        /// Writes a notification (text) to the notifications text box. The written text is usually 
        /// coming from FlashLFQ's console output.
        /// </summary>
        private void AddNotification(string text)
        {
            notificationsTextBox.AppendText(text + Environment.NewLine);
            notificationsTextBox.ScrollToEnd();
        }

        /// <summary>
        /// This event fires when text is added to the notifications text box. It scrolls to the bottom
        /// of the text area, displaying the most recently added notification.
        /// </summary>
        private void notificationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            notificationsTextBox.ScrollToEnd();
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowObj.Close();
        }

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowObj.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowObj.WindowState == WindowState.Normal)
            {
                MainWindowObj.WindowState = WindowState.Maximized;
            }
            else
            {
                MainWindowObj.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// Opens the requested URL with the user's web browser.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Misc.StartProcess(e.Uri.ToString());
        }

        private void MailTo(object sender, RequestNavigateEventArgs e)
        {
            string mailto = string.Format("mailto:{0}?Subject={1}&Body={2}", "mm_support@chem.wisc.edu", "[FlashLFQ Help]", "");
            Misc.StartProcess(mailto);
        }

        /// <summary>
        /// Deletes a file (ID or spectra file) from the data grid.
        /// </summary>
        private void DeleteFileFromGrid_Click(object sender, RoutedEventArgs e)
        {
            SpectraFileForDataGrid spectraFile = (sender as Button).DataContext as SpectraFileForDataGrid;
            if (spectraFile != null)
            {
                spectraFiles.Remove(spectraFile);

                if (!spectraFiles.Any())
                {
                    DragAndDropHelperLabelSpectraFiles.Visibility = Visibility.Visible;
                }

                return;
            }

            IdentificationFileForDataGrid idFile = (sender as Button).DataContext as IdentificationFileForDataGrid;
            if (idFile != null)
            {
                idFiles.Remove(idFile);

                if (!idFiles.Any())
                {
                    DragAndDropHelperLabelIdFiles.Visibility = Visibility.Visible;
                }

                return;
            }
        }

        /// <summary>
        /// Checks that the ID file that the user has added is valid input. This is called
        /// each time a new ID file is added. The method first checks that the ID header is
        /// interpretable and then attempts to read the first identification. A popup is displayed
        /// if there is an error.
        /// </summary>
        private bool ValidateIdentificationFile(IdentificationFileForDataGrid idFile)
        {
            //TODO
            bool valid = true;

            return valid;
        }

        private void WriteExperimentalDesignToFile()
        {
            string expDesignFilePath = Path.Combine(Path.GetDirectoryName(outputFolderPath), ExperimentalDesignFilename);

            using (StreamWriter output = new StreamWriter(expDesignFilePath))
            {
                output.WriteLine("FileName\tCondition\tBiorep\tFraction\tTechrep");

                foreach (var spectraFile in spectraFiles)
                {
                    output.WriteLine(
                        spectraFile.SpectraFileInfo.FilenameWithoutExtension +
                        "\t" + spectraFile.Condition +
                        "\t" + (spectraFile.Sample) +
                        "\t" + (spectraFile.Fraction) +
                        "\t" + (spectraFile.Replicate));
                }
            }
        }

        private void CheckForExistingExperimentalDesignFile(SpectraFileForDataGrid file)
        {
            try
            {
                string experimentalDesignFilePath = Path.Combine(Path.GetDirectoryName(file.FilePath), ExperimentalDesignFilename);

                if (!File.Exists(experimentalDesignFilePath))
                {
                    return;
                }

                var lines = File.ReadAllLines(experimentalDesignFilePath);
                Dictionary<string, int> typeToIndex = new Dictionary<string, int>();

                for (int l = 0; l < lines.Length; l++)
                {
                    var split = lines[l].Split('\t');
                    if (l == 0)
                    {
                        foreach (var type in split)
                        {
                            typeToIndex.Add(type, Array.IndexOf(split, type));
                        }
                    }
                    else
                    {
                        if (split[typeToIndex["FileName"]] == file.SpectraFileInfo.FilenameWithoutExtension)
                        {
                            file.Condition = split[typeToIndex["Condition"]];
                            file.Sample = int.Parse(split[typeToIndex["Biorep"]]);
                            file.Fraction = int.Parse(split[typeToIndex["Fraction"]]);
                            file.Replicate = int.Parse(split[typeToIndex["Techrep"]]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // something went wrong trying to read the existing experimental design. not a critical error. just ignore it
            }
        }

        private void UpdateKnownConditions()
        {
            string selectedControlCondition = (string)ControlConditionComboBox.SelectedItem;
            conditions.Clear();

            foreach (SpectraFileForDataGrid item in spectraFiles)
            {
                if (!conditions.Contains(item.Condition))
                {
                    conditions.Add(item.Condition);
                }
            }

            if (conditions.Contains(selectedControlCondition))
            {
                ControlConditionComboBox.SelectedItem = selectedControlCondition;
            }
            else
            {
                ControlConditionComboBox.SelectedIndex = -1;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // this "if" stuff is here to check if the sender is a tab item
            // for some weird reason this event also gets triggered by selecting a control condition in the combobox in the settings...
            // that's why this "if" is here
            var senderType = e.OriginalSource.GetType().Name;

            if (senderType == "TabControl")
            {
                UpdateKnownConditions();
            }
        }

        private void BaseConditionComboBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ControlConditionComboBox.IsEnabled)
            {
                ControlConditionComboBox.SelectedIndex = 0;
            }
            else
            {
                ControlConditionComboBox.SelectedIndex = -1;
            }
        }

        private void BayesianCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (bayesianCheckbox.IsChecked.Value)
            {
                BayesianSettings1.Visibility = Visibility.Visible;
                BayesianSettings2.Visibility = Visibility.Visible;
                BaseConditionComboBox_IsEnabledChanged(sender, new DependencyPropertyChangedEventArgs());
            }
            else
            {
                BayesianSettings1.Visibility = Visibility.Hidden;
                BayesianSettings2.Visibility = Visibility.Hidden;
            }
        }
    }
}
