using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class Autorization : Page
    {
        public event Action Logined;

        public Autorization()
        {
            InitializeComponent();
        }

        private void btnVhod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLogin.Text))
                {
                    MessageBox.Show("Введите ваш логин!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtLogin.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Введите ваш пароль!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Focus();
                    return;
                }

                var wizard = AppConnect.model01.Users
                    .FirstOrDefault(x =>
                        x.Login == txtLogin.Text &&
                        x.Password == txtPassword.Password);

                if (wizard == null)
                {
                    MessageBox.Show("Неверный логин или пароль!\nПопробуйте снова.", "Ошибка входа",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassword.Clear();
                    txtLogin.Focus();
                    return;
                }
                else
                {
                    string welcomeMessage = $"Добро пожаловать, {wizard.UserName}!";
                    MessageBox.Show(welcomeMessage, "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);
                    Logined?.Invoke();
                    ApplicationData.AppFrame.Navigate(new PageTask());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при входе:\n{ex.Message}",
                    "Сбой системы", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
              ApplicationData.AppFrame.Navigate(new RegistrationPage());
        }
    }
}
