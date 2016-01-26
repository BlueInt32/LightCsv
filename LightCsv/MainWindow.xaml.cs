﻿using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

        public static RoutedCommand SaveByKeyboardCommand = new RoutedCommand();
        public static RoutedCommand OpenByKeyboardCommand = new RoutedCommand();
        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            Save_File();
        }
        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            Open_File();
        }
        public MainWindow()
        {
            InitializeComponent();
            SaveByKeyboardCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            OpenByKeyboardCommand.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
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
                var expectedColumnsCount = dynamicTypeFields.Count;
                while (!parser.EndOfData)
                {
                    string[] currentRowfields = parser.ReadFields();
                    
                    if (rowCount == 0) // build csv type dynamically
                    {
                        dynamicTypeFields = currentRowfields.Select(fieldName => fieldName.Replace("_", "__")).ToList(); // datagrid needs underscores to be escaped (unless removed)
                        dynamicCsvType = CsvTypeBuilder.CreateNewObject(dynamicTypeFields);
                    }
                    else
                    {
                        var currentLineColumnsCount = currentRowfields.Count();
                        var myObject = Activator.CreateInstance(dynamicCsvType);
                        for (int i = 0; i < currentLineColumnsCount; i++)
                        {
                            if (i >= dynamicTypeFields.Count)
                            {
                                continue;
                            }
                            var property = myObject.GetType().GetProperty(dynamicTypeFields[i]);
                            property.SetValue(myObject, currentRowfields[i]);
                        }
                        itemsSource.Add(myObject);
                    }
                    rowCount++;
                }
            }
            //csvDataGrid.
            csvDataGrid.ItemsSource = itemsSource;
        }

        private void MenuItem_Click_Open(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            Open_File();
        }

        private void MenuItem_Click_Save(object sender, RoutedEventArgs e)
        {
            Keyboard.ClearFocus();
            Save_File();
        }

        private void MenuItem_Click_SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                FileOpened = saveFileDialog.FileName;
                Save_File();
            }
            //Save_File();
        }
        private void Open_File()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FileOpened = openFileDialog.FileName;
                LoadFile(FileOpened);
            }
        }
        private void Save_File()
        {
            csvDataGrid.SelectAllCells();
            csvDataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, csvDataGrid);
            csvDataGrid.UnselectAllCells();
            string resultCsvContent = (string)Clipboard.GetData(DataFormats.Text);
            int lastNewLine = resultCsvContent.Trim('\n').LastIndexOf('\n');
            resultCsvContent = resultCsvContent.Substring(0, lastNewLine);
            resultCsvContent = Regex.Replace(resultCsvContent, @"^\s+$", "", RegexOptions.Multiline);
            resultCsvContent = resultCsvContent
                .Replace('\t', separator)
                .Trim()
                .Replace("__", "_");
            try
            {
                File.WriteAllText(FileOpened, resultCsvContent);
            }
            catch
            {
                MessageBox.Show("Cannot write the file. Maybe check if it is being used by another program.");
            }
        }

        private void separatorChoice_TextChanged(object sender, TextChangedEventArgs e)
        {
            separator = (sender as TextBox).Text.ToString().First();
            LoadFile(FileOpened);
        }

        private void DataGrid_CellGotFocus(object sender, RoutedEventArgs e)
        {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);
            }
        }

        private T GetFirstChildByType<T>(DependencyObject prop) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild((prop), i) as DependencyObject;
                if (child == null)
                    continue;

                T castedProp = child as T;
                if (castedProp != null)
                    return castedProp;

                castedProp = GetFirstChildByType<T>(child);

                if (castedProp != null)
                    return castedProp;
            }
            return null;
        }

        private void MenuItem_Click_AddColumn(object sender, RoutedEventArgs e)
        {

            DataGridTextColumn textColumn = new DataGridTextColumn();
            textColumn.Header = "Test";
            textColumn.Binding = new Binding("FirstName");
            csvDataGrid.Columns.Add(textColumn);
        }
    }
}
