using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageProfile : Page
    {
        public PageProfile()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadProfile();
        }

        private void LoadProfile()
        {
            var u = AppConnect.CurrentUser;
            if (u == null) return;

            AvatarBlock.Text = u.UserName.Length > 0
                ? u.UserName[0].ToString().ToUpper() : "?";

            NameBlock.Text  = u.UserName;
            LoginBlock.Text = $"Логин: {u.Login}";

            string roleName = u.RoleId == 1 ? "Администратор"
                            : u.RoleId == 2 ? "Менеджер"
                            : "Покупатель";
            RoleBlock.Text  = $"Роль: {roleName}";

            PhoneBlock.Text = string.IsNullOrEmpty(u.Phone)
                ? "Телефон: не указан" : $"Тел: {u.Phone}";
            EmailBlock.Text = string.IsNullOrEmpty(u.Email)
                ? "Email: не указан" : u.Email;
            SinceBlock.Text = $"Регистрация: {u.CreatedAt:dd.MM.yyyy}";

            var orders = AppConnect.model01.Orders
                .Where(o => o.UserLogin == u.Login)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            OrdersGrid.ItemsSource = orders;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}
