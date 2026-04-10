using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class Autorization : Page
    {
        public Autorization()
        {
            InitializeComponent();
        }

        private void btnVhod_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtLogin.Text))
                { MessageBox.Show("Введите логин!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); txtLogin.Focus(); return; }

                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                { MessageBox.Show("Введите пароль!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); txtPassword.Focus(); return; }

                var user = AppConnect.model01.Users.FirstOrDefault(x =>
                    x.Login == txtLogin.Text && x.Password == txtPassword.Password);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    txtPassword.Clear(); txtLogin.Focus(); return;
                }

                AppConnect.CurrentUser = user;
                AppFrame.frmMain.Navigate(new PageCatalog());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReg_Click(object sender, RoutedEventArgs e)
            => AppFrame.frmMain.Navigate(new Reg());
    }
}
