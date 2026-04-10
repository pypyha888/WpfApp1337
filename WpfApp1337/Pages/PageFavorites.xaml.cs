using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageFavorites : Page
    {
        private List<Products> _favProducts = new();

        public PageFavorites()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadFavorites();
        }

        private void LoadFavorites()
        {
            var ids = AppConnect.model01.Favorites
                .Where(f => f.UserLogin == AppConnect.CurrentUser.Login)
                .Select(f => f.ProductId)
                .ToList();

            _favProducts = AppConnect.model01.Products
                .Where(p => ids.Contains(p.Id))
                .ToList();

            FavList.ItemsSource = null;
            FavList.ItemsSource = _favProducts;
            CountBlock.Text = $"⭐ Избранных товаров: {_favProducts.Count}";
        }

        // Удалить из избранного
        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button)?.Tag as Products;
            if (product == null) return;

            if (MessageBox.Show($"Удалить «{product.Name}» из избранного?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            try
            {
                var item = AppConnect.model01.Favorites.FirstOrDefault(f =>
                    f.ProductId == product.Id &&
                    f.UserLogin  == AppConnect.CurrentUser.Login);

                if (item != null)
                {
                    AppConnect.model01.Favorites.Remove(item);
                    AppConnect.model01.SaveChanges();
                }

                LoadFavorites();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Добавить в корзину
        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button)?.Tag as Products;
            if (product == null) return;

            try
            {
                var existing = AppConnect.model01.Cart.FirstOrDefault(c =>
                    c.ProductId == product.Id &&
                    c.UserLogin  == AppConnect.CurrentUser.Login);

                if (existing == null)
                    AppConnect.model01.Cart.Add(new Cart
                    {
                        ProductId   = product.Id,
                        ProductName = product.Name,
                        UnitPrice   = product.Price,
                        Quantity    = 1,
                        UserLogin   = AppConnect.CurrentUser.Login
                    });
                else
                    existing.Quantity++;

                AppConnect.model01.SaveChanges();

                MessageBox.Show($"«{product.Name}» добавлен в корзину!",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}
