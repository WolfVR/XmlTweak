using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.Win32;

namespace XmlTweak
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly XmlFoldingStrategy _foldingStrategy = new XmlFoldingStrategy {ShowAttributesWhenFolded = true};
        private FoldingManager _foldingManager;
        private OpenFileDialog _openFileDialogInstance;
        private SaveFileDialog _saveFileDialogInstance;
        private string _tweakString;
        private XDocument _xdoc;


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
                        _xdoc = XDocument.Load(((OpenFileDialog) ofd).FileName, LoadOptions.None);
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
            cbAttributes.ItemsSource = new HashSet<XName>(
                _xdoc.Descendants().Attributes()
                    .Where(xa => !xa.IsNamespaceDeclaration)
                    .OrderBy(ob => ob.Name.LocalName)
                    .Select(xa => xa.Name)
            ).ToList();

            // Load the raw file into the syntax highlighter for display
            tbDisplayHighlight.Text = File.ReadAllText(tbSourcePath.Text);
            lblStatus.Content = "XML Loaded";
            btnTweak.IsEnabled = true;
            gbSort.IsEnabled = true;
            gbFormat.IsEnabled = true;
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

        private void ChkSortValue_Click(object sender, RoutedEventArgs e)
        {
            //if (chkSortValue.IsChecked.GetValueOrDefault())
            //{
            //    chkSortSubElement.IsChecked = true;
            //    chkSortSubElement.IsEnabled = false;
            //}
            //else
            //{
            //    chkSortSubElement.IsEnabled = true;
            //}
        }

        private void cbAttributes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            chkSortValue.IsChecked = true;
            ChkSortValue_Click(null, null);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(tbDestinationPath.Text, _tweakString);
            lblStatus.Content = "Save Complete";
            btnSave.IsEnabled = false;
        }

        private void BtnTweak_Click(object sender, RoutedEventArgs e)
        {
            if (chkSortValue.IsChecked.GetValueOrDefault() && cbAttributes.SelectedIndex == -1)
            {
                MessageBox.Show("Please select an attribute to use for sorting.");
                return;
            }

            if (chkRemoveEmptyElement.IsChecked.GetValueOrDefault())
                RemoveEmptyElement(_xdoc.Root);

            if (chkSortAttribute.IsChecked.GetValueOrDefault()
                || chkSortValue.IsChecked.GetValueOrDefault())
                SortElement(_xdoc.Root);

            DisplayResults();
            lblStatus.Content = "XML has been tweaked!";
        }

        #endregion Events

        #region Helpers

        private static void RemoveEmptyElement(XContainer xElement)
        {
            xElement.Elements().Where(xe => xe.IsEmpty && !xe.HasAttributes).Remove();
            foreach (var childElement in xElement.Elements().Where(xe => xe.HasElements))
                RemoveEmptyElement(childElement);
        }

        private void SortElement(XElement xElement)
        {
            // Sort Attributes if desired
            if (chkSortAttribute.IsChecked.GetValueOrDefault() && xElement.HasAttributes)
                xElement.ReplaceAttributes(xElement.Attributes().Where(x => x.IsNamespaceDeclaration),
                    xElement.Attributes().Where(x => !x.IsNamespaceDeclaration).OrderBy(ob => ob.Name.LocalName));

            // If no child elements, or no additional sorting requested, then return
            if (!xElement.HasElements || (!chkSortSubElement.IsChecked.GetValueOrDefault() &&
                !chkSortValue.IsChecked.GetValueOrDefault())) return;


            xElement.ReplaceNodes(xElement.Elements().OrderBy(ob => ob.Name.LocalName)
                .ThenBy(ob => ob.Attribute(cbAttributes.SelectedItem as XName)?.Value));

            if (!chkSortSubElement.IsChecked.GetValueOrDefault()) return;
            foreach (var childElement in xElement.Elements()) SortElement(childElement);
        }

        private void DisplayResults()
        {
            var xWriterSettings = new XmlWriterSettings
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(false),
                Indent = true,
                //IndentChars = "   ", // 3 spaces instead of the default 2
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

            // Remove extra space before closing shortcut introduced by the xWriter " />" => "/>"
            _tweakString = _tweakString.Replace(" />", "/>");
            tbDisplayHighlight.Text = _tweakString;
            btnSave.IsEnabled = !string.IsNullOrWhiteSpace(tbDestinationPath.Text);
        }

        #endregion Helpers
    }
}