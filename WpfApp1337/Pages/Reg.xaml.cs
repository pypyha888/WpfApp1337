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
            InitializePage();
        }

        private void InitializePage()
        {
            Loaded += (s, e) => FullNameTextBox.Focus();
            RegisterButton.IsEnabled = false;
        }

        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IsLoginAlreadyExists())
                {
                    ShowMessage("Этот логин уже занят!", true);
                    LoginTextBox.Focus();
                    LoginTextBox.SelectAll();
                    return;
                }

                if (!AreRequiredFieldsFilled()) return;

                if (!DoPasswordsMatch())
                {
                    ShowMessage("Пароли не совпадают!", true);
                    PasswordBox.Focus();
                    return;
                }

                if (!ValidateInputData()) return;

                CreateNewUser();

                ShowMessage($"Регистрация успешна! Добро пожаловать, {FullNameTextBox.Text.Trim()}!", false);

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                timer.Tick += (ts, te) =>
                {
                    timer.Stop();
                    AppFrame.frmMain.GoBack();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                ShowMessage($"Ошибка: {ex.Message}", true);
            }
        }

        private bool IsLoginAlreadyExists()
        {
            return !string.IsNullOrWhiteSpace(LoginTextBox.Text) &&
                   AppConnect.model01.Users.Any(x => x.Login == LoginTextBox.Text.Trim());
        }

        private bool AreRequiredFieldsFilled()
        {
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                ShowMessage("Введите имя!", true);
                FullNameTextBox.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
            {
                ShowMessage("Введите логин!", true);
                LoginTextBox.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowMessage("Введите пароль!", true);
                PasswordBox.Focus();
                return false;
            }
            return true;
        }

        private bool DoPasswordsMatch() =>
            PasswordBox.Password == ConfirmPasswordBox.Password;

        private bool ValidateInputData()
        {
            if (PasswordBox.Password.Length < 4)
            {
                ShowMessage("Пароль должен содержать минимум 4 символа!", true);
                PasswordBox.Focus();
                return false;
            }
            return true;
        }

        private void CreateNewUser()
        {
            var newUser = new Users()
            {
                UserName = FullNameTextBox.Text.Trim(),   // UserName, не Name!
                Login    = LoginTextBox.Text.Trim(),
                Password = PasswordBox.Password,
                RoleId   = 3   // роль User по умолчанию
            };

            AppConnect.model01.Users.Add(newUser);
            AppConnect.model01.SaveChanges();
        }

        private void PasswordBoxes_PasswordChanged(object sender, RoutedEventArgs e)
        {
            bool match = DoPasswordsMatch();

            if (!match && !string.IsNullOrEmpty(ConfirmPasswordBox.Password))
            {
                RegisterButton.IsEnabled = false;
                ConfirmPasswordBox.Background = System.Windows.Media.Brushes.LightCoral;
                ConfirmPasswordBox.BorderBrush = System.Windows.Media.Brushes.Red;
                ConfirmPasswordBox.ToolTip = "Пароли не совпадают!";
            }
            else if (match)
            {
                RegisterButton.IsEnabled = true;
                ConfirmPasswordBox.Background = System.Windows.Media.Brushes.LightGreen;
                ConfirmPasswordBox.BorderBrush = System.Windows.Media.Brushes.Green;
                ConfirmPasswordBox.ToolTip = "Пароли совпадают!";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppFrame.frmMain.CanGoBack)
                AppFrame.frmMain.GoBack();
        }

        private void ShowMessage(string message, bool isError) =>
            MessageBox.Show(message, isError ? "Ошибка" : "Успех",
                MessageBoxButton.OK, isError ? MessageBoxImage.Error : MessageBoxImage.Information);
    }
}
