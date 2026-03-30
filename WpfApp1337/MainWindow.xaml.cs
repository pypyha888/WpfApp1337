using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1337
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            SetupNavigationSystem();
            LoadAuthorizationPage();
        }
        private void InitializeDatabaseConnection()
        {
            try
            {
                ApplicationData.AppConnect.model01 = new ApplicationData.TechnoShopEntities();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться к базе данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SetupNavigationSystem()
        {
            ApplicationData.AppFrame.frmMain = FrmMain;
        }
        private void LoadAuthorizationPage()
        {
            FrmMain.Navigate(new Autorization()); 
        }

    }
}