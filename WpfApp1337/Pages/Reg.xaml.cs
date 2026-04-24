using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class Reg : Page
    {
        public Reg()
        {
            InitializeComponent();
            RegisterButton.IsEnabled = false;
        }

        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
                { MessageBox.Show("Введите имя!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
                { MessageBox.Show("Введите логин!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (AppConnect.model01.Users.Any(x => x.Login == LoginTextBox.Text.Trim()))
                { MessageBox.Show("Этот логин уже занят!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                { MessageBox.Show("Введите пароль!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                { MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                if (PasswordBox.Password.Length < 4)
                { MessageBox.Show("Пароль минимум 4 символа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                var newUser = new Users
                {
                    UserName  = FullNameTextBox.Text.Trim(),
                    Login     = LoginTextBox.Text.Trim(),
                    Password  = PasswordBox.Password,
                    RoleId    = 3  // Покупатель
                };

                AppConnect.model01.Users.Add(newUser);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация успешна!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigateBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            if (AppFrame.frmMain.CanGoBack)
                AppFrame.frmMain.GoBack();
            else
                AppFrame.frmMain.Navigate(new Autorization());
        }

        private void PasswordBoxes_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordBox == null || ConfirmPasswordBox == null) return;

            bool match = PasswordBox.Password == ConfirmPasswordBox.Password
                         && !string.IsNullOrEmpty(ConfirmPasswordBox.Password);

            RegisterButton.IsEnabled = match;

            if (!string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                ConfirmPasswordBox.Background = match
                    ? System.Windows.Media.Brushes.LightGreen
                    : System.Windows.Media.Brushes.LightCoral;
            }
            else
            {
                ConfirmPasswordBox.Background = System.Windows.Media.Brushes.White;
            }
        }
    }
}
