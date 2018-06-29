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
        private string _tweakString;


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
                        _xdoc = XDocument.Load(((OpenFileDialog)ofd).FileName, LoadOptions.None);
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
        }

        private void BtnDestinationBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (_saveFileDialogInstance == null)
                _saveFileDialogInstance = new SaveFileDialog();

            if (!_saveFileDialogInstance.ShowDialog().GetValueOrDefault()) return;

            tbDestinationPath.Text = _saveFileDialogInstance.FileName;
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(_tweakString);
        }

        private void ChkOverwrite_Click(object sender, RoutedEventArgs e)
        {
            if (chkOverwrite.IsChecked.GetValueOrDefault())
            {
                btnDestinationBrowse.IsEnabled = false;
                tbDestinationPath.Text = tbSourcePath.Text;
                btnSave.IsEnabled = !string.IsNullOrWhiteSpace(_tweakString);
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
            File.WriteAllText(tbDestinationPath.Text,_tweakString);
        }

        private void BtnTweak_Click(object sender, RoutedEventArgs e)
        {
            // Configure Sorting

            // Perform Work
            if(chkRemoveEmptyNode.IsChecked.GetValueOrDefault())
                RemoveEmptyElement(_xdoc.Root);

            if (chkSortElement.IsChecked.GetValueOrDefault() || chkSortValue.IsChecked.GetValueOrDefault())
                SortElement(_xdoc.Root);

            DisplayResults();
        }

        #endregion Events

        #region Helpers

        private static void RemoveEmptyElement(XElement xElement)
        {
            xElement.Elements().Where(xe => xe.IsEmpty && !xe.HasAttributes).Remove();
            foreach (var childElement in xElement.Elements().Where(xe => xe.HasElements))
            {
                RemoveEmptyElement(childElement);
            }
        }

        private static void SortElement(XElement xElement)
        {
            // Check that this element has child elements before trying to sort them
            if (xElement.HasElements)
            {
                xElement.ReplaceNodes(xElement.Elements().OrderBy(ob => ob.Name.LocalName));

                foreach (var childElement in xElement.Elements())
                {
                    SortElement(childElement);
                }
            }
        }

        private void DisplayResults()
        {
            var xWriterSettings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "   ", // 3 spaces instead of the default 2
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = chkAttibuteNewLine.IsChecked.GetValueOrDefault(),
                OmitXmlDeclaration = false,
                WriteEndDocumentOnClose = true
            };

            using (var memoryStream = new MemoryStream())
            {
                using (var xWriter = XmlWriter.Create(memoryStream, xWriterSettings))
                {
                    _xdoc.Save(xWriter);
                }

                _tweakString = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            tbDisplayHighlight.Text = _tweakString;
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(tbDestinationPath.Text);
        }

        #endregion Helpers
    }
}
