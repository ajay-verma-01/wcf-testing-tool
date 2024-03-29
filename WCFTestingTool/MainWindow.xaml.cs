﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Reflection;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.ComponentModel;

namespace WCFTestingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Fields

        //Field to hold SVC Background Work Process.
        readonly BackgroundWorker _svcBackGroundWorker;

        //Field to hold CSC Background Work Process.
        readonly BackgroundWorker _cscBackGroundWorker;

        //Field to hold proxy assembly created by svcutil and csc process. 
        Assembly _proxyAssembly;

        //Field to hold seleted method's information. 
        MethodInfo[] _methodsInfo;

        //Filed to hold Client Type.
        Type _clientType;

        //Field to hold the path of configuration file.
        string _configFilePath;

        #endregion

        #region Properties

        //Base end point address of the service.
        private string BaseAddress
        {
            get;
            set;
        }

        //Is method parameter is a enum type.
        public static bool IsEnum
        {
            get;
            set;
        }

        //Type of the method parameter.
        public static Type ParamType
        {
            get;
            set;
        }

        public static string SvcUtilFilePath
        { get; set; }

        public static string CscFilePath
        { get; set; }

        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            SvcUtilFilePath = Settings.Default.SvcUtilFilePath;
            _configFilePath = Settings.Default.ConfigFilePath;
            CscFilePath = Settings.Default.CscFilePath;

            //Initialize background worker process.
            _svcBackGroundWorker = new BackgroundWorker();
            _cscBackGroundWorker = new BackgroundWorker();

            //Set the event end property of the background worker process.
            _svcBackGroundWorker.DoWork += BackGroundWorkerDoWork;
            _svcBackGroundWorker.ProgressChanged += BackGroundWorkerProgressChanged;
            _svcBackGroundWorker.RunWorkerCompleted += BackGroundWorkerRunWorkerCompleted;
            _svcBackGroundWorker.WorkerReportsProgress = true;
            _svcBackGroundWorker.WorkerSupportsCancellation = true;

            _cscBackGroundWorker.DoWork += CscBackGroundWorkerDoWork;
            _cscBackGroundWorker.ProgressChanged += CscBackGroundWorkerProgressChanged;
            _cscBackGroundWorker.RunWorkerCompleted += CscBackGroundWorkerRunWorkerCompleted;
            _cscBackGroundWorker.WorkerReportsProgress = true;
            _cscBackGroundWorker.WorkerSupportsCancellation = true;

        }

        #endregion

        #region Event Handler

        private void CscBackGroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtStatus.Text += "\n" + Settings.Default.MethodLoadedSuccess;
            //Load the proxy assembly.
            try
            {
                var assemblyBytes = File.ReadAllBytes(Settings.Default.ProxyAssemblyPath);
                _proxyAssembly = Assembly.Load(assemblyBytes);
                var types = _proxyAssembly.GetTypes();

                foreach (var type in types)
                {
                    foreach (var objAt in type.GetCustomAttributes(false))
                    {
                        if (objAt.GetType() != typeof(ServiceContractAttribute)) continue;
                        var itemInterface = new TreeViewItem { Header = type.FullName, IsExpanded = true };
                        tvMethods.Items.Add(itemInterface);
                        _clientType = type;
                        _methodsInfo = _clientType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                        foreach (var method in _methodsInfo)
                        {
                            Type genericType;
                            if (method.DeclaringType.IsGenericType)
                            {
                                //Get the base generic type i.e. ClientBase.
                                genericType = method.DeclaringType.GetGenericTypeDefinition();
                                //Filter non ClientBase class's methods.
                                if (!genericType.FullName.Equals(Settings.Default.BaseServiceClassName))
                                {
                                    var itemMethod = new TreeViewItem { Header = method.Name };
                                    itemInterface.Items.Add(itemMethod);
                                }
                            }
                            //Filter non Object class's methods.
                            else if (!method.DeclaringType.Equals(typeof(Object)))
                            {
                                var itemMethod = new TreeViewItem { Header = method.Name };
                                itemInterface.Items.Add(itemMethod);
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                txtStatus.Text = Settings.Default.GeneratingProxyFail;
                return;
            }
        }

        private void CscBackGroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            txtStatus.Text += "\n" + Settings.Default.LoadingMethodWait;
        }

        private void CscBackGroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            //Run the Csc process to create the proxy dll from proxy class
            var cscProcess = new Process { StartInfo = GetCscStartProcessInfo() };
            cscProcess.Start();
            _cscBackGroundWorker.ReportProgress(10, cscProcess.StandardOutput.ReadToEnd());
            e.Result = cscProcess.StandardOutput.ReadToEnd();
            cscProcess.WaitForExit();
        }

        private void BackGroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtStatus.Text += "\n" + Settings.Default.ServiceFound;
            _cscBackGroundWorker.RunWorkerAsync();
        }

        private void BackGroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            txtStatus.Text = Settings.Default.EmptyString;
            txtStatus.Text = Settings.Default.FindingService;
        }

        private void BackGroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            //Run the Svcutil process to create the proxy class.
            var svcUtilProcess = new Process
                                     {
                                         StartInfo = GetSvcUtilStartProcessInfo(BaseAddress)
                                     };
            svcUtilProcess.Start();

            _svcBackGroundWorker.ReportProgress(10, svcUtilProcess.StandardOutput.ReadToEnd());
            e.Result = svcUtilProcess.StandardOutput.ReadToEnd();
            svcUtilProcess.WaitForExit();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            //Initialize the Modal popup window to receive endpoint
            var addService = new AddService();

            if (addService.ShowAddServiceDialog() == AddService.ISOK)
            {
                try
                {
                    BaseAddress = addService.ReturnUrl;
                    _svcBackGroundWorker.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddSvcUtil_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ".exe",
                Filter = "Data Sources (*.exe)|*.exe*|All Files|*.*"
            };
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SvcUtilFilePath = openDialog.FileName;
            }
        }

        private void AddCscFile_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ".exe",
                Filter = "Data Sources (*.exe)|*.exe*|All Files|*.*"
            };
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CscFilePath = openDialog.FileName;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new About { Owner = this };
            dlg.ShowDialog();
        }
        
        private void Configuration_Click(object sender, RoutedEventArgs e)
        {

        }
        private void tvMethods_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = tvMethods.SelectedItem as TreeViewItem;
            if (item == null)
                return;
            if (item.HasItems)
                return;
            _clientType = _proxyAssembly.GetType(((TreeViewItem)item.Parent).Header.ToString());
            var mInfo = _clientType.GetMethod(((TreeViewItem)tvMethods.SelectedItem).Header.ToString());
            tvParameters.Items.Clear();
            var mItem = new TreeViewItem { Header = mInfo.Name + "'s Parameters", IsExpanded = true };
            tvParameters.Items.Add(mItem);
            foreach (var pInfo in mInfo.GetParameters())
            {
                if (IsUserDefinedType(pInfo.ParameterType))
                {
                    if (pInfo.ParameterType.IsEnum)
                    {
                        AddParameterEnumToTreeView(pInfo, mItem);
                    }
                    else
                    {
                        AddPrametersToTreeView(pInfo.ParameterType, mItem);
                    }
                }
                else
                {
                    var itemParam = new TreeViewItem
                                        {
                                            Header =
                                               Settings.Default.Type + " = " + pInfo.ParameterType.FullName + " : " + Settings.Default.Name + " = " + pInfo.Name +
                                                " : " + Settings.Default.Value + " = "
                                        };
                    mItem.Items.Add(itemParam);
                }
            }
        }

        private void tvParameters_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = tvParameters.SelectedItem as TreeViewItem;
            if (item == null)
                return;
            if (item.HasItems)
                return;
            var fields = item.Header.ToString().Split(':');
            var pType = _proxyAssembly.GetType(fields[0].Substring(6).Trim());
            if (pType != null)
            {
                IsEnum = true;
            }
            else
            {
                pType = Type.GetType(fields[0].Substring(6).Trim());
                IsEnum = false;
            }
            ParamaterDialog.ParamType = pType;
            var paramDialog = new ParamaterDialog();
            //paramDialog.ParamType = pType;

            if (paramDialog.ShowParameterDialog() == AddService.ISOK)
            {
                try
                {
                    item.Header = item.Header.ToString().Remove(item.Header.ToString().LastIndexOf('=') + 1);
                    item.Header += " " + paramDialog.ParamValue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        /// <summary>
        /// Run the method on click of run button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            if (tvMethods.SelectedItem == null)
            {
                MessageBox.Show("Please select method to test.", "Message");
                return;
            }
            var parameterList = new List<object>();

            foreach (TreeViewItem item in tvParameters.Items)
            {
                if (item.Header.ToString().Substring(item.Header.ToString().IndexOf(Convert.ToChar("'"))) !=
                    "'s Parameters") continue;
                if (item.HasItems)
                {
                    ConvertItemToType(item, parameterList);
                }
            }
            var objParameters = parameterList.ToArray();
            var mInfo = _clientType.GetMethod(((TreeViewItem)tvMethods.SelectedItem).Header.ToString());
            var channelType = typeof(CustomClientChannel<>);
            var clientChannel = channelType.MakeGenericType(_clientType);

            MethodInfo channelMethod = null;
            foreach (var cMethod in clientChannel.GetMethods())
            {
                if (cMethod.Name != Settings.Default.CreateChannel) continue;
                if (cMethod.GetParameters().Length != 0) continue;
                channelMethod = cMethod;
                break;
            }
            if (channelMethod != null)
            {
                try
                {
                    //Invoke the CreateChannel method of ChannelFactory base class to create the communication channel.
                    var channelResult =
                        channelMethod.Invoke(Activator.CreateInstance(clientChannel, new object[] { _configFilePath }),
                                             new object[] { });

                    //Invoke the selected method
                    var result = channelResult.GetType().InvokeMember(mInfo.Name, BindingFlags.InvokeMethod, null,
                                                                         channelResult, objParameters);

                    if (result != null)
                    {
                        //Show the result of the method.
                        txtResult.Text = SerializeObject(result, mInfo.ReturnType);
                    }
                }
                catch (Exception ex)
                {
                    txtResult.Text = "Error while executing the method. Please contact vendor." + "\n" + ex.Message;
                }
            }
        }

        private void insertEndPoint_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog
                                 {
                                     DefaultExt = Settings.Default.ConfigFileExt,
                                     Filter = Settings.Default.ConfigFileFilter
                                 };
            if (openDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _configFilePath = openDialog.FileName;
            }
        }

        private void HowToUse_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// Return the information to start the CSC process to create proxy dll.
        /// </summary>
        /// <returns></returns>
        private static ProcessStartInfo GetCscStartProcessInfo()
        {
            return new ProcessStartInfo
            {
                FileName = CscFilePath,
                Arguments = Settings.Default.CscCmdParameter,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
        }

        /// <summary>
        /// Return the information to start the SVCUTIL process to create proxy class.
        /// </summary>
        /// <param name="serviceUrl"></param>
        /// <returns></returns>
        private static ProcessStartInfo GetSvcUtilStartProcessInfo(string serviceUrl)
        {
            return new ProcessStartInfo
                       {
                           FileName = SvcUtilFilePath,
                           Arguments = Settings.Default.SvcCmdParameter + " " + serviceUrl,
                           CreateNoWindow = true,
                           UseShellExecute = false,
                           RedirectStandardOutput = true
                       };
        }

        private static void AddParameterEnumToTreeView(ParameterInfo proInfo, ItemsControl mItem)
        {
            var tvNewItem = new TreeViewItem
                                {
                                    Header =
                                       Settings.Default.Type + " = " + proInfo.ParameterType.FullName + " : " + Settings.Default.Name + " = " + proInfo.Name +
                                        " : " + Settings.Default.Value + " = ",
                                    IsExpanded = true
                                };
            mItem.Items.Add(tvNewItem);
        }

        private static void AddPrametersToTreeView(Type pType, ItemsControl mItem)
        {
            var tItem = new TreeViewItem { Header = pType.FullName };
            foreach (var proInfo in pType.GetProperties())
            {
                if (proInfo.PropertyType.FullName == Settings.Default.ExtensionDataObject ||
                    proInfo.Name == Settings.Default.ExtensionDataField) continue;
                var pItem = new TreeViewItem();
                if (IsUserDefinedType(proInfo.PropertyType))
                {
                    if (proInfo.PropertyType.IsEnum)
                    {
                        AddParameterEnumToTreeView(proInfo, pItem);
                    }
                    else
                    {
                        AddPrametersToTreeView(proInfo.PropertyType, pItem);
                    }
                }
                pItem.Header = Settings.Default.Type + " = " + proInfo.PropertyType.FullName + " : " + Settings.Default.Name + " = " + proInfo.Name + " : " + Settings.Default.Value + " = ";
                tItem.Items.Add(pItem);
            }

            mItem.Items.Add(tItem);
        }

        private static void AddParameterEnumToTreeView(PropertyInfo proInfo, HeaderedItemsControl pItem)
        {
            pItem.Header = Settings.Default.Type + " = " + proInfo.PropertyType.Name + " : " + Settings.Default.Name + " = " + proInfo.Name + " : " + Settings.Default.Value + " = ";

        }

        private static bool IsUserDefinedType(ICustomAttributeProvider type)
        {
            var isTrue = false;
            foreach (var objAt in type.GetCustomAttributes(false))
            {
                if (objAt.GetType() != typeof(DataContractAttribute)) continue;
                isTrue = true;
                break;
            }
            return isTrue;
        }

        private void ConvertItemToType(ItemsControl item, ICollection<object> paramList)
        {
            foreach (TreeViewItem innerItem in item.Items)
            {
                if (innerItem.HasItems)
                {
                    paramList.Add(ItemHasItems(innerItem));
                }
                else
                {
                    paramList.Add(ItemNotHasItems(innerItem));
                }
            }
        }

        private object ItemHasItems(HeaderedItemsControl item)
        {
            var type = _proxyAssembly.GetType(item.Header.ToString().Trim());
            var result = Activator.CreateInstance(type);
            foreach (TreeViewItem innerItem in item.Items)
            {

                if (innerItem.HasItems)
                {
                    return ItemHasItems(innerItem);
                }
                var objNot = ItemNotHasItems(innerItem);
                var fields = innerItem.Header.ToString().Split(':');
                var pInfo = type.GetProperty(fields[1].Substring(7).Trim());
                pInfo.SetValue(result, objNot, null);
            }
            return result;
        }

        private object ItemNotHasItems(HeaderedItemsControl item)
        {
            object result;
            var fields = item.Header.ToString().Split(':');
            var type = _proxyAssembly.GetType(fields[0].Substring(6).Trim());
            if (type == null)
            {
                if (fields[2].Trim() == "Value =")
                    return null;

                object objValue = fields[2].Substring(8).Trim();
                result = Convert.ChangeType(objValue, Type.GetType(fields[0].Substring(6).Trim()));
            }
            else
            {
                if (fields[2].Trim() == "Value =")
                    return null;

                result = Enum.Parse(type, fields[2].Substring(8).Trim());
            }
            return result;
        }

        /// <summary>
        /// Serialize the object to a string.
        /// </summary>
        /// <param name="result">Object to serialize</param>
        /// <param name="type">Type in which the object serialize</param>
        /// <returns></returns>
        private static string SerializeObject(object result, Type type)
        {
            try
            {
                var memoryStream = new MemoryStream();
                var xs = new XmlSerializer(type);
                var xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8)
                                        {
                                            Formatting = Formatting.Indented,
                                            Indentation = 4
                                        };
                xs.Serialize(xmlTextWriter, result);

                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
                var xmlizedString = UTF8ByteArrayToString(memoryStream.ToArray());
                return xmlizedString;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
                return null;
            }
        }

        /// <summary>
        /// Return string from UTF8 Byte Array.
        /// </summary>
        /// <param name="characters">Byte array</param>
        /// <returns></returns>
        private static string UTF8ByteArrayToString(Byte[] characters)
        {
            var encoding = new UTF8Encoding();
            var constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            File.Delete(Settings.Default.ConfigFilePath);
            File.Delete(Settings.Default.ProxyAssemblyPath);
            File.Delete(Settings.Default.ProxyClassPath);
        }


        #endregion

        #region Future Use

        //public Object DeserializeObject(String pXmlizedString)
        //{
        //    XmlSerializer xs = new XmlSerializer(typeof(Automobile));
        //    MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(pXmlizedString));
        //    XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

        //    return xs.Deserialize(memoryStream);
        //}

        //private Byte[] StringToUTF8ByteArray(String pXmlString)
        //{
        //    UTF8Encoding encoding = new UTF8Encoding();
        //    Byte[] byteArray = encoding.GetBytes(pXmlString);
        //    return byteArray;
        //}

        #endregion
    }
}
