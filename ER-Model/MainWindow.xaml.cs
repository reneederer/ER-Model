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

namespace ER_Model
{

    [Serializable]
    public class ERModel
    {
        public Dictionary<Table, TableColumns> Tables { get; set; } = new Dictionary<Table, TableColumns>();
    }

    [Serializable]
    public struct Table
    {
        public string Owner { get; set; }
        public string Name { get; set; }
    }

    [Serializable]
    public class Column
    {
        public string ColumnName { get; set; } = "";
        public bool ColumnNullable { get; set; } = false;
        public string ColumnType { get; set; } = "";
    }

    [Serializable]
    public class ReferenceColumn : Column
    {
        public string ReferenceOwnerName { get; set; }
        public string ReferenceTableName { get; set; }
        public string ReferenceColumnName { get; set; }
    }

    [Serializable]
    public class TableColumns
    {

        public IEnumerable<ReferenceColumn> From { get; set; } = new List<ReferenceColumn>();
        public IEnumerable<ReferenceColumn> To { get; set; } = new List<ReferenceColumn>();
        public IEnumerable<Column> NormalColumns { get; set; } = new List<Column>();
    }
    public partial class MainWindow : Window
    {
        ERModel erModel = new Database().GetERModel("entwickler", "bewmail", new ERModel(), 0);
        public MainWindow()
        {
            InitializeComponent();

            addERModel(erModel);
            WriteToBinaryFile("c:/users/rene/myFile.dat", erModel);
            var x = ReadFromBinaryFile<ERModel>("c:/users/rene/myFile.dat");
   //         addERModel(x);
        }
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite)
        {
            using (Stream stream = File.Open(filePath, FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        public void addERModel(ERModel erModel)
        {
            TreeView t = new TreeView();
            var item = getCanvasERModel(erModel, "ENTWICKLER", "BEWMAIL", null);
            t.Items.Add(item);
            Canvas.SetLeft(t, 10);
            Canvas.SetTop(t, 20);
            grid.Children.Add(t);
            item.Selected += Item_Selected;
        }

        private void Item_Selected(object sender, RoutedEventArgs e)
        {
            var t = sender as TreeViewItem;
            MessageBox.Show("Clicked: " + t.Header);
        }



        public TreeViewItem getCanvasERModel(ERModel erModel, string owner, string tableName, string header, bool isRecursive = false)
        {
            var t = new TreeViewItem();
            t.Header = header ?? (owner + "." + tableName);
            if(!erModel.Tables.ContainsKey(new Table { Owner = owner, Name = tableName }) || isRecursive)
            {
                t.Expanded += T_Expanded;
                t.Items.Add(owner);
                t.Items.Add(tableName);
            }
            else
            {
                foreach(var column in erModel.Tables[new Table { Owner = owner, Name = tableName }].NormalColumns)
                {
                    t.Items.Add(column.ColumnName + " (" + (column.ColumnNullable ? "null" : "not null") + ")");
                }
                foreach (var r in erModel.Tables[new Table { Owner = owner, Name = tableName }].To)
                {
                    t.Items.Add(
                        getCanvasERModel(
                            erModel,
                            r.ReferenceOwnerName,
                            r.ReferenceTableName,
                            r.ColumnName + "(" +
                              (r.ColumnNullable ? "null" : "not null") +
                              ") => " + r.ReferenceTableName,
                            (owner == r.ReferenceOwnerName && tableName == r.ReferenceTableName)
                        )
                    );
                }
            }
            return t;
        }

        private void T_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = sender as TreeViewItem;
            treeViewItem.Expanded -= T_Expanded;
            var owner = treeViewItem.Items[0].ToString();
            var tableName = treeViewItem.Items[1].ToString();
            treeViewItem.Items.Clear();
            erModel = new Database().GetERModel(owner, tableName, erModel, 1);
            foreach(var column in erModel.Tables[new Table { Owner = owner, Name = tableName }].NormalColumns)
            {
                treeViewItem.Items.Add(column.ColumnName + " (" + (column.ColumnNullable ? "null" : "not null") + ")");
            }
            foreach (var r in erModel.Tables[new Table { Owner = owner, Name = tableName }].To)
            {
                treeViewItem.Items.Add(
                    getCanvasERModel(
                        erModel,
                        r.ReferenceOwnerName,
                        r.ReferenceTableName,
                        r.ColumnName + "(" +
                          (r.ColumnNullable ? "null" : "not null") +
                          ") => " + r.ReferenceTableName));
            }
        }
    }
}
