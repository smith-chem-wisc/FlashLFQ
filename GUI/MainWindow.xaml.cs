using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FlashLFQ;
using CMD;
using GUI.DataGridObjects;
using IO.Thermo;

namespace GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<SpectraFileForDataGrid> spectraFilesForDataGrid;
        private ObservableCollection<IdentificationFileForDataGrid> identFilesForDataGrid;
        private BackgroundWorker worker;
        private FlashLFQEngine flashLfqEngine;
        private FlashLFQResults results;
        private List<SpectraFileInfo> spectraFileInfo;
        private string outputFolderPath;

        public MainWindow()
        {
            InitializeComponent();

            spectraFilesForDataGrid = new ObservableCollection<SpectraFileForDataGrid>();
            identFilesForDataGrid = new ObservableCollection<IdentificationFileForDataGrid>();
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(RunProgram);

            flashLfqEngine = new FlashLFQEngine(new List<Identification>(), normalize: false);
            spectraFileInfo = new List<SpectraFileInfo>();

            identFilesDataGrid.DataContext = identFilesForDataGrid;
            dataGridSpectraFiles.DataContext = spectraFilesForDataGrid;

            var _writer = new TextBoxWriter(notificationsTextBox);
            Console.SetOut(_writer);
        }

        private void RunProgram(object sender, DoWorkEventArgs e)
        {
            RunFlashLfq();
        }

        private void changeSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsWindow();
            dialog.PopulateSettings(flashLfqEngine);

            if (dialog.ShowDialog() == true)
            {
                flashLfqEngine = dialog.tempFlashLfqEngine;
            }
        }

        private void SetExperDesign_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExperimentalDesignWindow(spectraFilesForDataGrid);
            dialog.ShowDialog();
        }

        private void ClearSpectra_Click(object sender, RoutedEventArgs e)
        {
            spectraFilesForDataGrid.Clear();
            OutputFolderTextBox.Clear();
        }

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

            dataGridSpectraFiles.Items.Refresh();
        }

        private void ClearIdentFiles_Click(object sender, RoutedEventArgs e)
        {
            identFilesForDataGrid.Clear();
        }

        private void AddIdentFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openPicker = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "Identification Files|*.txt;*.tsv;*.psmtsv",
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (Run.IsEnabled)
            {
                string[] files = ((string[])e.Data.GetData(DataFormats.FileDrop)).OrderBy(p => p).ToArray();

                if (files != null)
                {
                    foreach (var draggedFilePath in files)
                    {
                        if (Directory.Exists(draggedFilePath))
                        {
                            foreach (string file in Directory.EnumerateFiles(draggedFilePath, "*.*", SearchOption.AllDirectories))
                            {
                                AddAFile(file);
                            }
                        }
                        else
                        {
                            AddAFile(draggedFilePath);
                        }
                        dataGridSpectraFiles.CommitEdit(DataGridEditingUnit.Row, true);
                        identFilesDataGrid.CommitEdit(DataGridEditingUnit.Row, true);
                        dataGridSpectraFiles.Items.Refresh();
                        identFilesDataGrid.Items.Refresh();
                    }
                }
            }
        }

        private void AddAFile(string filePath)
        {
            string filename = Path.GetFileName(filePath);
            string theExtension = Path.GetExtension(filename).ToLowerInvariant();

            switch (theExtension)
            {
                case ".raw":
                    if (!ThermoStaticData.CheckForMsFileReader())
                    {
                        AddNotification("Warning! Cannot find Thermo MSFileReader (v3.0 SP2 is preferred); a crash may result from searching this .raw file");
                    }

                    goto case ".mzml";

                case ".mzml":
                    SpectraFileForDataGrid spectraFile = new SpectraFileForDataGrid(filePath);
                    if (!spectraFilesForDataGrid.Contains(spectraFile))
                    {
                        spectraFilesForDataGrid.Add(spectraFile);
                    }
                    if (string.IsNullOrEmpty(OutputFolderTextBox.Text))
                    {
                        var pathOfFirstSpectraFile = Path.GetDirectoryName(spectraFilesForDataGrid.First().FilePath);
                        OutputFolderTextBox.Text = Path.Combine(pathOfFirstSpectraFile, @"$DATETIME");
                    }
                    break;

                case ".txt":
                case ".tsv":
                case ".psmtsv":
                    IdentificationFileForDataGrid identFile = new IdentificationFileForDataGrid(filePath);
                    if (!identFilesForDataGrid.Contains(identFile) && !identFile.FileName.Equals("ExperimentalDesign.tsv"))
                    {
                        identFilesForDataGrid.Add(identFile);
                    }
                    break;

                default:
                    AddNotification("Unrecognized file type: " + theExtension);
                    break;
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            // check for valid tasks/spectra files/protein databases
            if (!spectraFilesForDataGrid.Any())
            {
                MessageBox.Show("You need to add at least one spectra file!", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            if (!identFilesForDataGrid.Any())
            {
                MessageBox.Show("You need to add at least one identification file!", "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            // get experimental design
            try
            {
                SetupSpectraFileInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem setting up run:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
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
                    var pathOfFirstSpectraFile = Path.GetDirectoryName(spectraFilesForDataGrid.First().FilePath);
                    OutputFolderTextBox.Text = Path.Combine(pathOfFirstSpectraFile, @"$DATETIME");
                }

                var startTimeForAllFilenames = "FlashLFQ_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                string outputFolder = OutputFolderTextBox.Text.Replace("$DATETIME", startTimeForAllFilenames);
                OutputFolderTextBox.Text = outputFolder;

                Run.IsEnabled = false;
                changeSettingsMenuItem.IsEnabled = false;
                AddIdentFile.IsEnabled = false;
                AddSpectra.IsEnabled = false;
                ClearIdentFiles.IsEnabled = false;
                ClearSpectra.IsEnabled = false;
                SetExperDesign.IsEnabled = false;

                dataGridSpectraFiles.IsReadOnly = true;
                identFilesDataGrid.IsReadOnly = true;

                outputFolderPath = outputFolder;

                worker.RunWorkerAsync();

                AddNotification("All done");
            }
        }

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

        private void RunFlashLfq()
        {
            // read IDs
            var ids = new List<Identification>();
            foreach (var identFile in identFilesForDataGrid)
            {
                ids = ids.Concat(PsmReader.ReadPsms(identFile.FilePath, false, spectraFileInfo)).ToList();
            }

            // run FlashLFQ engine
            try
            {
                flashLfqEngine = new FlashLFQEngine(ids, flashLfqEngine.normalize, flashLfqEngine.ppmTolerance, flashLfqEngine.isotopePpmTolerance, flashLfqEngine.mbr,
                    flashLfqEngine.mbrppmTolerance, flashLfqEngine.integrate, flashLfqEngine.numIsotopesRequired, flashLfqEngine.idSpecificChargeState,
                    flashLfqEngine.requireMonoisotopicMass, false, null, flashLfqEngine.mbrRtWindow);

                results = flashLfqEngine.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("FlashLFQ has crashed with the following error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }

            // write output
            try
            {
                OutputWriter.WriteOutput(Directory.GetParent(spectraFileInfo.First().fullFilePathWithExtension).FullName, results, outputFolderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not write FlashLFQ output: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        private void AddNotification(string text)
        {
            notificationsTextBox.AppendText(text + Environment.NewLine);
            notificationsTextBox.ScrollToEnd();
        }

        private void notificationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            notificationsTextBox.ScrollToEnd();
        }

        private void SetupSpectraFileInfo()
        {
            if (flashLfqEngine.normalize)
            {
                string assumedExperimentalDesignPath = Directory.GetParent(spectraFilesForDataGrid.First().FilePath).FullName;
                assumedExperimentalDesignPath = Path.Combine(assumedExperimentalDesignPath, "ExperimentalDesign.tsv");

                if (File.Exists(assumedExperimentalDesignPath))
                {
                    var experimentalDesign = File.ReadAllLines(assumedExperimentalDesignPath)
                        .ToDictionary(p => p.Split('\t')[0], p => p);

                    foreach (var file in spectraFilesForDataGrid)
                    {
                        string filename = Path.GetFileNameWithoutExtension(file.FileName);

                        var expDesignForThisFile = experimentalDesign[filename];
                        var split = expDesignForThisFile.Split('\t');

                        string condition = split[1];
                        int biorep = int.Parse(split[2]);
                        int fraction = int.Parse(split[3]);
                        int techrep = int.Parse(split[4]);

                        // experimental design info passed in here for each spectra file
                        spectraFileInfo.Add(new SpectraFileInfo(fullFilePathWithExtension: file.FilePath,
                            condition: condition,
                            biorep: biorep - 1,
                            fraction: fraction - 1,
                            techrep: techrep - 1));
                    }
                }
                else
                {
                    throw new Exception("Could not find experimental design file!\nYou need to define this if you want to normalize");
                }
            }
            else
            {
                foreach (var file in spectraFilesForDataGrid)
                {
                    // experimental design info passed in here for each spectra file
                    spectraFileInfo.Add(new SpectraFileInfo(fullFilePathWithExtension: file.FilePath, condition: "", biorep: 0, fraction: 0, techrep: 0));
                }
            }
        }
    }
}
