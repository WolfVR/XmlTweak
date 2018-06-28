using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.Win32;

namespace XmlTweak
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenFileDialog _openFileDialogInstance;
        private SaveFileDialog _saveFileDialogInstance;
        private XDocument _xdoc;
        private FoldingManager _foldingManager;
        private readonly XmlFoldingStrategy _foldingStrategy = new XmlFoldingStrategy();


        public MainWindow()
        {
            InitializeComponent();
        }

        #region Events
        private void TbDisplayHighlight_Loaded(object sender, RoutedEventArgs e)
        {
            _foldingManager = FoldingManager.Install(tbDisplayHighlight.TextArea);
        }

        private void TbDisplayHighlight_TextChanged(object sender, EventArgs e)
        {
            _foldingStrategy.UpdateFoldings(_foldingManager, tbDisplayHighlight.Document);
        }

        private void BtnSourceBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (_openFileDialogInstance == null)
            {
                _openFileDialogInstance = new OpenFileDialog();
                _openFileDialogInstance.FileOk += (ofd, args) =>
                {
                    try
                    {
                        _xdoc = XDocument.Load(((OpenFileDialog)ofd).FileName, LoadOptions.PreserveWhitespace);
                        args.Cancel = false;
                    }
                    catch
                    {
                        MessageBox.Show("File is not a valid XML document.");
                        args.Cancel = true;
                    }
                };
            }

            if (!_openFileDialogInstance.ShowDialog().GetValueOrDefault())
                return;

            tbSourcePath.Text = _openFileDialogInstance.FileName;
            if (chkOverwrite.IsChecked.GetValueOrDefault())
                tbDestinationPath.Text = tbSourcePath.Text;


            // Get the attributes in the document and populate the combo box to use for sorting.
            var attributes = new HashSet<string>(_xdoc.Descendants().Attributes()
                .Where(xa => !xa.IsNamespaceDeclaration).Select(xa => xa.Name.LocalName)).ToList();
            attributes.Sort();
            cbAttributes.ItemsSource = attributes;

            // Load the raw file into the syntax highlighter for display
            tbDisplayHighlight.Text = File.ReadAllText(tbSourcePath.Text);

            btnTweak.IsEnabled = true;
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(tbDestinationPath.Text);
        }

        private void BtnDestinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (_saveFileDialogInstance == null)
                _saveFileDialogInstance = new SaveFileDialog();

            if (_saveFileDialogInstance.ShowDialog().GetValueOrDefault())
            {
                tbDestinationPath.Text = _saveFileDialogInstance.FileName;
                btnSave.IsEnabled = btnTweak.IsEnabled;
            }
        }

        private void ChkOverwrite_Click(object sender, RoutedEventArgs e)
        {
            if (chkOverwrite.IsChecked.GetValueOrDefault())
            {
                btnDestinationBrowse.IsEnabled = false;
                tbDestinationPath.Text = tbSourcePath.Text;
                btnSave.IsEnabled = btnTweak.IsEnabled;
            }
            else
            {
                btnDestinationBrowse.IsEnabled = true;
                tbDestinationPath.Text = string.Empty;
                btnSave.IsEnabled = false;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnTweak_Click(object sender, RoutedEventArgs e)
        {
            // Configure Formatting

            // Configure Sorting

            // Perform Work

            // Display Results
        }

        #endregion Events


    }
}
