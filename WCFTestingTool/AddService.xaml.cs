using System.Windows;

namespace WCFTestingTool
{
    /// <summary>
    /// Interaction logic for AddService.xaml
    /// </summary>
    public partial class AddService
    {
        public string ReturnUrl
        { get; set; }

        int _returnValue;
        public static int ISOK = 1;
        public static int ISCANCEL = -1;
        public AddService()
        {
            InitializeComponent();
            txtEndPoint.Text = "http://";
        }

        public int ShowAddServiceDialog()
        {
            _returnValue = ISCANCEL;
            ShowDialog();
            return _returnValue;
        }

        void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ReturnUrl = txtEndPoint.Text.Trim();
            _returnValue = ISOK;
            Close();
        }

        void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
