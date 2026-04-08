using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageTask : Page
    {
        // Удобные свойства для проверки роли
        private bool IsAdmin   => AppConnect.CurrentUser?.RoleId == 1;
        private bool IsManager => AppConnect.CurrentUser?.RoleId == 2;
        private bool CanManage => IsAdmin || IsManager; // может управлять товарами

        public PageTask()
        {
            InitializeComponent();
            InitializePage();
        }

        private void InitializePage()
        {
            try
            {
                ApplyRolePermissions();
                LoadProducts();
                LoadCart();
                LoadSuppliers();
                LoadOrders();
                InitializeFilters();
                SetInitialValues();
                UpdateProductCounter();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при загрузке страницы:\n{ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────
        //  РАСПРЕДЕЛЕНИЕ РОЛЕЙ
        //  Admin   (RoleId=1) — видит всё, может всё включая удаление
        //  Manager (RoleId=2) — видит товары/поставщиков, добавляет/редактирует товары
        //  User    (RoleId=3) — только просмотр каталога и корзина
        // ──────────────────────────────────────────────────────────
        private void ApplyRolePermissions()
        {
            if (AppConnect.CurrentUser == null) return;

            // Кнопки управления товарами
            AddProductButton.Visibility    = CanManage ? Visibility.Visible : Visibility.Collapsed;
            EditProductButton.Visibility   = CanManage ? Visibility.Visible : Visibility.Collapsed;
            DeleteProductButton.Visibility = IsAdmin   ? Visibility.Visible : Visibility.Collapsed;

            // Вкладка Поставщики — только Admin и Manager
            SuppliersTab.Visibility = CanManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadProducts()
        {
            ProductsDataGrid.ItemsSource = AppConnect.model01.Products.ToList();
        }

        private void LoadSuppliers()
        {
            SuppliersDataGrid.ItemsSource = AppConnect.model01.Suppliers.ToList();
        }

        private void LoadOrders()
        {
            // User видит только свои заказы, Admin/Manager — все
            var orders = CanManage
                ? AppConnect.model01.Orders.ToList()
                : AppConnect.model01.Orders
                    .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                    .ToList();
            OrdersDataGrid.ItemsSource = orders;
        }

        private void InitializeFilters()
        {
            SortComboBox.Items.Clear();
            CategoryFilterComboBox.Items.Clear();

            SortComboBox.Items.Add("Без сортировки");
            SortComboBox.Items.Add("По возрастанию цены");
            SortComboBox.Items.Add("По убыванию цены");
            SortComboBox.SelectedIndex = 0;

            CategoryFilterComboBox.Items.Add("Все категории");
            foreach (var cat in AppConnect.model01.Categories.ToList())
                CategoryFilterComboBox.Items.Add(cat.CategoryName);
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void SetInitialValues()
        {
            SearchTextBox.Text = "";
            if (AppConnect.CurrentUser == null) return;

            string roleName = IsAdmin ? "Администратор" : IsManager ? "Менеджер" : "Покупатель";
            UserHeaderTextBlock.Text =
                $"Магазин бытовой техники  |  {AppConnect.CurrentUser.UserName}  [{roleName}]";
            ProfileTextBlock.Text =
                $"Пользователь: {AppConnect.CurrentUser.UserName}  |  " +
                $"Логин: {AppConnect.CurrentUser.Login}  |  Роль: {roleName}";
        }

        private void UpdateProductCounter()
        {
            var products = GetFilteredProducts();
            if (products.Length > 0)
            {
                tbCounter.Text = $"Найдено товаров: {products.Length}";
                tbCounter.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                tbCounter.Text = "Товары не найдены";
                tbCounter.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private Products[] GetFilteredProducts()
        {
            try
            {
                var products = AppConnect.model01.Products.ToList();

                if (!string.IsNullOrWhiteSpace(SearchTextBox?.Text))
                {
                    var s = SearchTextBox.Text.ToLower();
                    products = products.Where(x =>
                        (x.Name  != null && x.Name.ToLower().Contains(s)) ||
                        (x.Brand != null && x.Brand.ToLower().Contains(s))
                    ).ToList();
                }

                if (CategoryFilterComboBox?.SelectedIndex > 0)
                {
                    string sel = CategoryFilterComboBox.SelectedItem.ToString()!;
                    products = products.Where(x => x.Category == sel).ToList();
                }

                switch (SortComboBox?.SelectedIndex)
                {
                    case 1: products = products.OrderBy(x => x.Price).ToList(); break;
                    case 2: products = products.OrderByDescending(x => x.Price).ToList(); break;
                }

                return products.ToArray();
            }
            catch { return Array.Empty<Products>(); }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ProductsDataGrid.ItemsSource = GetFilteredProducts();
            UpdateProductCounter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox?.SelectedIndex >= 0)
            {
                ProductsDataGrid.ItemsSource = GetFilteredProducts();
                UpdateProductCounter();
            }
        }

        private void OnSortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox?.SelectedIndex >= 0)
            {
                ProductsDataGrid.ItemsSource = GetFilteredProducts();
                UpdateProductCounter();
            }
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CanManage && ProductsDataGrid.SelectedItem is Products p)
                EditProduct(p);
        }

        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products p) AddToCart(p);
            else ShowInfoMessage("Выберите товар для добавления в корзину!");
        }

        private void OnAddProductClick(object sender, RoutedEventArgs e)
        {
            if (!CanManage) { ShowInfoMessage("Недостаточно прав!"); return; }
            NavigationService.Navigate(new AddRecip(null));
        }

        private void OnEditProductClick(object sender, RoutedEventArgs e)
        {
            if (!CanManage) { ShowInfoMessage("Недостаточно прав!"); return; }
            if (ProductsDataGrid.SelectedItem is Products p) EditProduct(p);
            else ShowInfoMessage("Выберите товар для редактирования!");
        }

        private void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowInfoMessage("Удаление доступно только администратору!"); return; }
            if (ProductsDataGrid.SelectedItem is Products p) DeleteProduct(p);
            else ShowInfoMessage("Выберите товар для удаления!");
        }

        private void OnRemoveFromCartClick(object sender, RoutedEventArgs e)
        {
            if (CartDataGrid.SelectedItem is Cart item) RemoveFromCart(item);
            else ShowInfoMessage("Выберите товар для удаления из корзины!");
        }

        private void OnCheckoutClick(object sender, RoutedEventArgs e) => Checkout();

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            LoadSuppliers();
            LoadOrders();
            UpdateProductCounter();
            ShowInfoMessage("Данные обновлены!");
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null!;
            NavigationService.Navigate(new Autorization());
        }

        private void EditProduct(Products product) =>
            NavigationService.Navigate(new AddRecip(product));

        private void AddToCart(Products product)
        {
            try
            {
                var existing = AppConnect.model01.Cart.FirstOrDefault(x =>
                    x.ProductId == product.Id && x.UserLogin == AppConnect.CurrentUser.Login);

                if (existing == null)
                    AppConnect.model01.Cart.Add(new Cart
                    {
                        ProductId = product.Id, ProductName = product.Name,
                        UnitPrice = product.Price, Quantity = 1,
                        UserLogin = AppConnect.CurrentUser.Login
                    });
                else
                    existing.Quantity++;

                AppConnect.model01.SaveChanges();
                ShowSuccessMessage($"Товар \"{product.Name}\" добавлен в корзину!");
                LoadCart();
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        private void DeleteProduct(Products product)
        {
            if (MessageBox.Show($"Удалить \"{product.Name}\"?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model01.Products.Remove(product);
                    AppConnect.model01.SaveChanges();
                    LoadProducts();
                    UpdateProductCounter();
                    ShowSuccessMessage("Товар удалён!");
                }
                catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
            }
        }

        private void RemoveFromCart(Cart cartItem)
        {
            try
            {
                AppConnect.model01.Cart.Remove(cartItem);
                AppConnect.model01.SaveChanges();
                LoadCart();
                ShowInfoMessage("Товар удалён из корзины!");
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        private void LoadCart()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login).ToList();
            CartDataGrid.ItemsSource = cart;
            UpdateCartTotal();
        }

        private void UpdateCartTotal()
        {
            decimal total = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList().Sum(x => x.TotalPrice);
            CartTotalTextBlock.Text = $"Итого: {total:N0} ₽";
        }

        private void Checkout()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login).ToList();
            if (!cart.Any()) { ShowInfoMessage("Корзина пуста!"); return; }

            try
            {
                foreach (var item in cart)
                {
                    AppConnect.model01.Orders.Add(new Orders
                    {
                        UserLogin   = item.UserLogin,
                        ProductName = item.ProductName,
                        Quantity    = item.Quantity,
                        UnitPrice   = item.UnitPrice,
                        OrderDate   = DateTime.Now,
                        Status      = "Оформлен"
                    });
                    AppConnect.model01.Cart.Remove(item);
                }
                AppConnect.model01.SaveChanges();
                LoadCart();
                LoadOrders();
                ShowSuccessMessage("Заказ успешно оформлен!");
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        private void ShowInfoMessage(string msg)    => MessageBox.Show(msg, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void ShowSuccessMessage(string msg) => MessageBox.Show(msg, "Успех",      MessageBoxButton.OK, MessageBoxImage.Information);
        private void ShowErrorMessage(string msg)   => MessageBox.Show(msg, "Ошибка",     MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
