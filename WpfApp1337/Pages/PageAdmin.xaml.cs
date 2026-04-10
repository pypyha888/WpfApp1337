using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageAdmin : Page
    {
        private Users? _selectedUser;

        public PageAdmin()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadAll();
        }

        private void LoadAll()
        {
            // Только для Администратора
            if (AppConnect.CurrentUser?.RoleId != 1)
            {
                MessageBox.Show("Доступ запрещён!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                if (NavigationService.CanGoBack) NavigationService.GoBack();
                return;
            }

            UsersGrid.ItemsSource     = AppConnect.model01.Users.ToList();
            AllOrdersGrid.ItemsSource = AppConnect.model01.Orders
                .OrderByDescending(o => o.OrderDate).ToList();
            SuppliersGrid.ItemsSource = AppConnect.model01.Suppliers.ToList();

            SelectedUserBlock.Text = "не выбран";
        }

        // Выбор пользователя в таблице → показываем его в панели
        private void OnUserSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = UsersGrid.SelectedItem as Users;
            if (_selectedUser == null) return;

            SelectedUserBlock.Text = $"{_selectedUser.UserName}  (логин: {_selectedUser.Login})";

            // Устанавливаем ComboBox на текущую роль
            foreach (ComboBoxItem item in RoleComboBox.Items)
            {
                if (item.Tag?.ToString() == _selectedUser.RoleId.ToString())
                { RoleComboBox.SelectedItem = item; break; }
            }
        }

        // Сохранить новую роль
        private void OnSaveRoleClick(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            { MessageBox.Show("Выберите пользователя!", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (RoleComboBox.SelectedItem is not ComboBoxItem selected)
            { MessageBox.Show("Выберите роль!", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            // Нельзя изменить роль самому себе
            if (_selectedUser.Login == AppConnect.CurrentUser?.Login)
            { MessageBox.Show("Нельзя изменить роль самому себе!", "Внимание",
                MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            try
            {
                int newRole = int.Parse(selected.Tag!.ToString()!);

                // Находим пользователя в БД и меняем роль
                var dbUser = AppConnect.model01.Users
                    .FirstOrDefault(u => u.Id == _selectedUser.Id);
                if (dbUser == null) return;

                dbUser.RoleId = newRole;
                AppConnect.model01.SaveChanges();

                string roleName = newRole switch { 1 => "Администратор", 2 => "Менеджер", _ => "Покупатель" };
                MessageBox.Show(
                    $"Роль пользователя «{dbUser.UserName}» изменена на «{roleName}»",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем список
                UsersGrid.ItemsSource = AppConnect.model01.Users.ToList();
                SelectedUserBlock.Text = "не выбран";
                _selectedUser = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        { if (NavigationService.CanGoBack) NavigationService.GoBack(); }
    }
}
