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


        public MainWindow()
        {
            InitializeComponent();
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
                        _xdoc = XDocument.Load(((OpenFileDialog) ofd).FileName, LoadOptions.PreserveWhitespace);
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
            cbAttributes.ItemsSource =
                new HashSet<string>(_xdoc.Descendants().Attributes().Where(xa => !xa.IsNamespaceDeclaration).Select(xa => xa.Name.LocalName));

            // Load the raw file into the syntax highlighter for display
            tbDisplayHighlight.Text = File.ReadAllText(tbSourcePath.Text);
        }

        private void BtnDestinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            if(_saveFileDialogInstance == null)
                _saveFileDialogInstance = new SaveFileDialog();

            if (_saveFileDialogInstance.ShowDialog().GetValueOrDefault())
                tbDestinationPath.Text = _saveFileDialogInstance.FileName;
        }

        private void ChkOverwrite_Click(object sender, RoutedEventArgs e)
        {
            if (chkOverwrite.IsChecked.GetValueOrDefault())
            {
                lblDestinationPath.IsEnabled = false;
                tbDestinationPath.IsEnabled = false;
                btnDestinationBrowse.IsEnabled = false;
                tbDestinationPath.Text = tbSourcePath.Text;
            }
            else
            {
                lblDestinationPath.IsEnabled = true;
                tbDestinationPath.IsEnabled = true;
                btnDestinationBrowse.IsEnabled = true;
                tbDestinationPath.Text = string.Empty;
            }
        }
    }
}
