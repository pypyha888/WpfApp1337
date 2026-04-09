using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageTask : Page
    {
        private bool IsAdmin   => AppConnect.CurrentUser?.RoleId == 1;
        private bool IsManager => AppConnect.CurrentUser?.RoleId == 2;
        private bool CanManage => IsAdmin || IsManager;

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
            catch (Exception ex) { ShowErrorMessage($"Ошибка загрузки:\n{ex.Message}"); }
        }

        // ──────────────────────────────────────────────────────
        //  РОЛИ
        // ──────────────────────────────────────────────────────
        private void ApplyRolePermissions()
        {
            if (AppConnect.CurrentUser == null) return;
            AddProductButton.Visibility    = CanManage ? Visibility.Visible : Visibility.Collapsed;
            EditProductButton.Visibility   = CanManage ? Visibility.Visible : Visibility.Collapsed;
            DeleteProductButton.Visibility = IsAdmin   ? Visibility.Visible : Visibility.Collapsed;
            SuppliersTab.Visibility        = CanManage ? Visibility.Visible : Visibility.Collapsed;
        }
        

        // ──────────────────────────────────────────────────────
        //  ЗАГРУЗКА
        // ──────────────────────────────────────────────────────
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
            var orders = CanManage
                ? AppConnect.model01.Orders.ToList()
                : AppConnect.model01.Orders
                    .Where(x => x.UserLogin == AppConnect.CurrentUser.Login).ToList();
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
            string role = IsAdmin ? "Администратор" : IsManager ? "Менеджер" : "Покупатель";
            UserHeaderTextBlock.Text =
                $"Магазин бытовой техники  |  {AppConnect.CurrentUser.UserName}  [{role}]";
            ProfileTextBlock.Text =
                $"Пользователь: {AppConnect.CurrentUser.UserName}  |  Логин: {AppConnect.CurrentUser.Login}  |  Роль: {role}";
        }

        private void UpdateProductCounter()
        {
            var products = GetFilteredProducts();
            tbCounter.Text = products.Length > 0
                ? $"Найдено товаров: {products.Length}"
                : "Товары не найдены";
            tbCounter.Foreground = products.Length > 0
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
        }

        private Products[] GetFilteredProducts()
        {
            try
            {
                var list = AppConnect.model01.Products.ToList();
                if (!string.IsNullOrWhiteSpace(SearchTextBox?.Text))
                {
                    var s = SearchTextBox.Text.ToLower();
                    list = list.Where(x =>
                        (x.Name  != null && x.Name.ToLower().Contains(s)) ||
                        (x.Brand != null && x.Brand.ToLower().Contains(s))).ToList();
                }
                if (CategoryFilterComboBox?.SelectedIndex > 0)
                {
                    string sel = CategoryFilterComboBox.SelectedItem.ToString()!;
                    list = list.Where(x => x.Category == sel).ToList();
                }
                switch (SortComboBox?.SelectedIndex)
                {
                    case 1: list = list.OrderBy(x => x.Price).ToList(); break;
                    case 2: list = list.OrderByDescending(x => x.Price).ToList(); break;
                }
                return list.ToArray();
            }
            catch { return Array.Empty<Products>(); }
        }

        // ──────────────────────────────────────────────────────
        //  ФИЛЬТРЫ
        // ──────────────────────────────────────────────────────
        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            ProductsDataGrid.ItemsSource = GetFilteredProducts();
            UpdateProductCounter();
        }
        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox?.SelectedIndex >= 0)
            { ProductsDataGrid.ItemsSource = GetFilteredProducts(); UpdateProductCounter(); }
        }
        private void OnSortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox?.SelectedIndex >= 0)
            { ProductsDataGrid.ItemsSource = GetFilteredProducts(); UpdateProductCounter(); }
        }

        // ──────────────────────────────────────────────────────
        //  КНОПКИ ТОВАРОВ
        // ──────────────────────────────────────────────────────
        private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CanManage && ProductsDataGrid.SelectedItem is Products p) EditProduct(p);
        }

        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products p) AddToCart(p);
            else ShowInfoMessage("Выберите товар для добавления в корзину!");
        }

        // ── ДОБАВИТЬ В ИЗБРАННОЕ ──────────────────────────────
        private void OnAddToFavoritesClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is not Products product)
            { ShowInfoMessage("Выберите товар для добавления в избранное!"); return; }

            try
            {
                bool already = AppConnect.model01.Favorites.Any(f =>
                    f.ProductId == product.Id &&
                    f.UserLogin == AppConnect.CurrentUser.Login);

                if (already)
                { ShowInfoMessage($"«{product.Name}» уже в избранном!"); return; }

                AppConnect.model01.Favorites.Add(new Favorites
                {
                    ProductId = product.Id,
                    UserLogin = AppConnect.CurrentUser.Login,
                    AddedAt   = DateTime.Now
                });
                AppConnect.model01.SaveChanges();
                ShowSuccessMessage($"«{product.Name}» добавлен в избранное ⭐");
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        // ── ПЕРЕЙТИ К ИЗБРАННОМУ ─────────────────────────────
        private void OnOpenFavoritesClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageFavorites());
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
            else ShowInfoMessage("Выберите товар!");
        }

        private void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin) { ShowInfoMessage("Удаление — только для администратора!"); return; }
            if (ProductsDataGrid.SelectedItem is Products p) DeleteProduct(p);
            else ShowInfoMessage("Выберите товар!");
        }

        private void OnRemoveFromCartClick(object sender, RoutedEventArgs e)
        {
            if (CartDataGrid.SelectedItem is Cart item) RemoveFromCart(item);
            else ShowInfoMessage("Выберите товар из корзины!");
        }

        private void OnCheckoutClick(object sender, RoutedEventArgs e) => Checkout();

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadProducts(); LoadSuppliers(); LoadOrders(); UpdateProductCounter();
            ShowInfoMessage("Данные обновлены!");
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null!;
            NavigationService.Navigate(new Autorization());
        }

        // ──────────────────────────────────────────────────────
        //  БИЗНЕС-ЛОГИКА
        // ──────────────────────────────────────────────────────
        private void EditProduct(Products p) => NavigationService.Navigate(new AddRecip(p));

        private void AddToCart(Products product)
        {
            try
            {
                var ex = AppConnect.model01.Cart.FirstOrDefault(c =>
                    c.ProductId == product.Id && c.UserLogin == AppConnect.CurrentUser.Login);
                if (ex == null)
                    AppConnect.model01.Cart.Add(new Cart
                    { ProductId=product.Id, ProductName=product.Name,
                      UnitPrice=product.Price, Quantity=1, UserLogin=AppConnect.CurrentUser.Login });
                else ex.Quantity++;
                AppConnect.model01.SaveChanges();
                ShowSuccessMessage($"«{product.Name}» добавлен в корзину!");
                LoadCart();
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        private void DeleteProduct(Products product)
        {
            if (MessageBox.Show($"Удалить «{product.Name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model01.Products.Remove(product);
                    AppConnect.model01.SaveChanges();
                    LoadProducts(); UpdateProductCounter();
                    ShowSuccessMessage("Товар удалён!");
                }
                catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
            }
        }

        private void RemoveFromCart(Cart item)
        {
            try
            {
                AppConnect.model01.Cart.Remove(item);
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
                    { UserLogin=item.UserLogin, ProductName=item.ProductName,
                      Quantity=item.Quantity, UnitPrice=item.UnitPrice,
                      OrderDate=DateTime.Now, Status="Оформлен" });
                    AppConnect.model01.Cart.Remove(item);
                }
                AppConnect.model01.SaveChanges();
                LoadCart(); LoadOrders();
                ShowSuccessMessage("Заказ оформлен!");
            }
            catch (Exception ex) { ShowErrorMessage($"Ошибка:\n{ex.Message}"); }
        }

        private void ShowInfoMessage(string m)    => MessageBox.Show(m, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void ShowSuccessMessage(string m) => MessageBox.Show(m, "Успех",      MessageBoxButton.OK, MessageBoxImage.Information);
        private void ShowErrorMessage(string m)   => MessageBox.Show(m, "Ошибка",     MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
