using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LightCsv
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Type dynamicCsvType = null;
        List<object> itemsSource = new List<object>();
        private string _fileOpened;

        public string FileOpened
        {
            get { return _fileOpened; }
            set { _fileOpened = value; Title = "Light CSV - " + value.Substring(value.LastIndexOf("\\") + 1); }
        }
        
        char separator = ';';

        public MainWindow()
        {
            InitializeComponent();
            //separatorChoice.Text = separator.ToString();
        }

        public void LoadFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            dynamic d = new ExpandoObject();

            using (TextFieldParser parser = new TextFieldParser(fileName))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(separator.ToString());
                int rowCount = 0;
                List<string> dynamicTypeFields = new List<string>();
                while (!parser.EndOfData)
                {
                    string[] currentRowfields = parser.ReadFields();
                    if (rowCount == 0) // build csv type dynamically
                    {
                        dynamicTypeFields = currentRowfields.ToList();
                        dynamicCsvType = CsvTypeBuilder.CreateNewObject(dynamicTypeFields);
                    }
                    else
                    {
                        var myObject = Activator.CreateInstance(dynamicCsvType);
                        for (int i = 0; i < dynamicTypeFields.Count; i++)
                        {
                            var property = myObject.GetType().GetProperty(dynamicTypeFields[i]);
                            property.SetValue(myObject, currentRowfields[i]);
                        }
                        itemsSource.Add(myObject);
                    }
                    rowCount++;
                }
            }
            csvDataGrid.ItemsSource = itemsSource;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileOpened = openFileDialog.FileName;
                LoadFile(FileOpened);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            csvDataGrid.SelectAllCells();
            csvDataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, csvDataGrid);
            csvDataGrid.UnselectAllCells();
            string result2 = (string)Clipboard.GetData(DataFormats.Text);
            result2 = result2.Substring(0, result2.LastIndexOf('\r'));
            result2 = result2.Substring(0, result2.LastIndexOf('\r'));
            File.WriteAllText(FileOpened, result2.Replace('\t', separator));
        }

        private void separatorChoice_TextChanged(object sender, TextChangedEventArgs e)
        {
            separator = (sender as TextBox).Text.ToString().First();
            LoadFile(FileOpened);
        }
    }
}
