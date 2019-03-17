using System;
using System.Windows;
using System.Windows.Controls;

namespace WCFTestingTool
{
    /// <summary>
    /// Interaction logic for ParamaterDialog.xaml
    /// </summary>
    public partial class ParamaterDialog
    {
        public static Type ParamType
        { get; set; }

        public string ParamValue
        { get; set; }

        int _result;
        public static int IsCancel = -1;
        public static int IsOk = 1;

        public ParamaterDialog()
        {
            InitializeComponent();
            if (MainWindow.IsEnum)
            {
                var comboBoxParamValue = new ComboBox();
                gridParamValue.Children.Add(comboBoxParamValue);
                Grid.SetColumn(comboBoxParamValue, 1);
                Grid.SetRow(comboBoxParamValue, 0);

                var i = 0;
                foreach (var field in ParamType.GetFields())
                {
                    if (i == 0)
                    {
                        i++;
                        continue;
                    }
                    var cbItem = new ComboBoxItem {Content = field.Name};
                    comboBoxParamValue.Items.Add(cbItem);
                }
                comboBoxParamValue.SelectedIndex = 0;
            }
            else
            {
                var txtBoxParamValue = new TextBox();
                gridParamValue.Children.Add(txtBoxParamValue);
                Grid.SetColumn(txtBoxParamValue, 1);
                Grid.SetRow(txtBoxParamValue, 0);
            }
            
        }

        public int ShowParameterDialog()
        {
            txtType.Text = ParamType.FullName;
            _result = IsCancel;
            ShowDialog();
            return _result;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var paramValue = Settings.Default.EmptyString;
            object objType;
            if (MainWindow.IsEnum)
            {
                var cbParamValue = gridParamValue.Children[1] as ComboBox;
                if (cbParamValue != null) paramValue = ((ComboBoxItem) cbParamValue.SelectedItem).Content.ToString();
                try
                {
                    objType = Enum.Parse(ParamType, paramValue);
                    if (objType.GetType() == ParamType)
                    {
                        ParamValue = paramValue;
                        _result = IsOk;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Not able to parse, value to " + txtType.Text);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                var txtParamValue = gridParamValue.Children[1] as TextBox;
                if (txtParamValue != null) paramValue = txtParamValue.Text;
                try
                {
                    object objValue = paramValue;
                    objType = Convert.ChangeType(objValue, ParamType);
                    if (objType.GetType() == ParamType)
                    {
                        ParamValue = paramValue;
                        _result = IsOk;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Not able to parse, value to " + txtType.Text, "Error");
                    }
                }
                catch (Exception ex)
                {   
                     MessageBox.Show(ex.Message);
                }
            }

           
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

