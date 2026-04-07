using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageTask : Page
    {
        public PageTask()
        {
            InitializeComponent();
            InitializePage();
        }

        private void InitializePage()
        {
            try
            {
                LoadProducts();
                InitializeFilters();
                SetInitialValues();
                UpdateProductCounter();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при загрузке страницы:\n{ex.Message}");
            }
        }

        private void LoadProducts()
        {
            var products = AppConnect.model01.Products.ToList();
            ProductsDataGrid.ItemsSource = products;
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

            var categories = AppConnect.model01.Categories.ToList();
            foreach (var category in categories)
            {
                CategoryFilterComboBox.Items.Add(category.CategoryName);
            }

            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void SetInitialValues()
        {
            SearchTextBox.Focus();
            SearchTextBox.Text = "";
        }

        private void UpdateProductCounter()
        {
            var products = GetFilteredProducts();

            if (products != null && products.Length > 0)
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

                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    products = products.Where(x =>
                        x.Name.ToLower().Contains(SearchTextBox.Text.ToLower()) ||
                        x.Brand.ToLower().Contains(SearchTextBox.Text.ToLower())
                    ).ToList();
                }

                if (CategoryFilterComboBox.SelectedIndex > 0)
                {
                    string selectedCategory = CategoryFilterComboBox.SelectedItem.ToString();
                    products = products.Where(x => x.Category == selectedCategory).ToList();
                }

                if (SortComboBox.SelectedIndex > 0)
                {
                    switch (SortComboBox.SelectedIndex)
                    {
                        case 1:
                            products = products.OrderBy(x => x.Price).ToList();
                            break;
                        case 2:
                            products = products.OrderByDescending(x => x.Price).ToList();
                            break;
                    }
                }

                return products.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            var filteredProducts = GetFilteredProducts();
            ProductsDataGrid.ItemsSource = filteredProducts;
            UpdateProductCounter();
        }

        private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox.SelectedIndex >= 0)
            {
                var filteredProducts = GetFilteredProducts();
                ProductsDataGrid.ItemsSource = filteredProducts;
                UpdateProductCounter();
            }
        }

        private void OnSortChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedIndex >= 0)
            {
                var filteredProducts = GetFilteredProducts();
                ProductsDataGrid.ItemsSource = filteredProducts;
                UpdateProductCounter();
            }
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products selectedProduct)
            {
                EditProduct(selectedProduct);
            }
        }

        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products selectedProduct)
            {
                AddToCart(selectedProduct);
            }
            else
            {
                ShowInfoMessage("Выберите товар для добавления в корзину!");
            }
        }

        private void OnAddProductClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddEditProductPage(null));
        }

        private void OnEditProductClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products selectedProduct)
            {
                EditProduct(selectedProduct);
            }
            else
            {
                ShowInfoMessage("Выберите товар для редактирования!");
            }
        }

        private void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is Products selectedProduct)
            {
                DeleteProduct(selectedProduct);
            }
            else
            {
                ShowInfoMessage("Выберите товар для удаления!");
            }
        }

        private void OnRemoveFromCartClick(object sender, RoutedEventArgs e)
        {
            if (CartDataGrid.SelectedItem is Cart selectedCartItem)
            {
                RemoveFromCart(selectedCartItem);
            }
            else
            {
                ShowInfoMessage("Выберите товар для удаления из корзины!");
            }
        }

        private void OnCheckoutClick(object sender, RoutedEventArgs e)
        {
            Checkout();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            UpdateProductCounter();
            ShowInfoMessage("Данные обновлены!");
        }

        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }

        private void EditProduct(Products product)
        {
            NavigationService.Navigate(new AddEditProductPage(product));
        }

        private void AddToCart(Products product)
        {
            try
            {
                var existingCartItem = AppConnect.model01.Cart
                    .FirstOrDefault(x => x.ProductId == product.Id && x.UserLogin == AppConnect.CurrentUser.Login);

                if (existingCartItem == null)
                {
                    Cart newCartItem = new Cart()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = 1,
                        UserLogin = AppConnect.CurrentUser.Login
                    };
                    AppConnect.model01.Cart.Add(newCartItem);
                }
                else
                {
                    existingCartItem.Quantity++;
                }

                AppConnect.model01.SaveChanges();
                ShowSuccessMessage($"Товар \"{product.Name}\" добавлен в корзину!");
                LoadCart();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при добавлении в корзину:\n{ex.Message}");
            }
        }

        private void DeleteProduct(Products product)
        {
            var result = MessageBox.Show($"Удалить товар \"{product.Name}\"?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model01.Products.Remove(product);
                    AppConnect.model01.SaveChanges();
                    LoadProducts();
                    UpdateProductCounter();
                    ShowSuccessMessage("Товар удален!");
                }
                catch (Exception ex)
                {
                    ShowErrorMessage($"Ошибка при удалении:\n{ex.Message}");
                }
            }
        }

        private void RemoveFromCart(Cart cartItem)
        {
            try
            {
                AppConnect.model01.Cart.Remove(cartItem);
                AppConnect.model01.SaveChanges();
                LoadCart();
                ShowInfoMessage("Товар удален из корзины!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при удалении из корзины:\n{ex.Message}");
            }
        }

        private void LoadCart()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList();
            CartDataGrid.ItemsSource = cart;
            UpdateCartTotal();
        }

        private void UpdateCartTotal()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList();
            decimal total = cart.Sum(x => x.TotalPrice);
            CartTotalTextBlock.Text = $"Итого: {total:C}";
        }

        private void Checkout()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList();

            if (!cart.Any())
            {
                ShowInfoMessage("Корзина пуста!");
                return;
            }

            try
            {
                foreach (var item in cart)
                {
                    Order newOrder = new Order()
                    {
                        UserLogin = item.UserLogin,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        OrderDate = DateTime.Now,
                        Status = "Оформлен"
                    };
                    AppConnect.model01.Orders.Add(newOrder);
                    AppConnect.model01.Cart.Remove(item);
                }

                AppConnect.model01.SaveChanges();
                LoadCart();
                ShowSuccessMessage("Заказ оформлен!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка при оформлении заказа:\n{ex.Message}");
            }
        }

        private void ShowInfoMessage(string message)
        {
            MessageBox.Show(message, "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}