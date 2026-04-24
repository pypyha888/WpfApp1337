using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WpfApp1337.Pages;

namespace WpfApp1337
{
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
            FrmMain.Navigated += FrmMain_Navigated;
        }

        private void FrmMain_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is Autorization)
            {
                WindowState = WindowState.Normal;
                Width = 650;
                Height = 550;
                MinWidth = 480;
                MinHeight = 380;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else if (e.Content is PageCatalog || e.Content is PageFavorites || e.Content is PageCart || e.Content is PageProfile || e.Content is PageAdmin)
            {
                WindowState = WindowState.Normal;
                Width = 1300;
                Height = 750;
                MinWidth = 900;
                MinHeight = 550;
            }
        }

        private void LoadAuthorizationPage()
        {
            FrmMain.Navigate(new Autorization());
        }
    }
}