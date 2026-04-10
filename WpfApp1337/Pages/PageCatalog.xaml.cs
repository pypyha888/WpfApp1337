using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class PageCatalog : Page
    {
        private bool IsAdmin   => AppConnect.CurrentUser?.RoleId == 1;
        private bool IsManager => AppConnect.CurrentUser?.RoleId == 2;
        private bool CanManage => IsAdmin || IsManager;

        public PageCatalog()
        {
            InitializeComponent();
            Loaded += (s, e) => Init();
        }

        private void Init()
        {
            // Роли
            BtnAdd.Visibility    = CanManage ? Visibility.Visible : Visibility.Collapsed;
            BtnEdit.Visibility   = CanManage ? Visibility.Visible : Visibility.Collapsed;
            BtnDelete.Visibility = IsAdmin   ? Visibility.Visible : Visibility.Collapsed;
            AdminButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            string role = IsAdmin ? "Администратор" : IsManager ? "Менеджер" : "Покупатель";
            HeaderTextBlock.Text = $"Магазин бытовой техники  |  {AppConnect.CurrentUser?.UserName}  [{role}]";

            // Фильтры
            SortBox.Items.Clear();
            SortBox.Items.Add("Без сортировки");
            SortBox.Items.Add("По возрастанию цены");
            SortBox.Items.Add("По убыванию цены");
            SortBox.SelectedIndex = 0;

            CategoryBox.Items.Clear();
            CategoryBox.Items.Add("Все категории");
            foreach (var c in AppConnect.model01.Categories.ToList())
                CategoryBox.Items.Add(c.CategoryName);
            CategoryBox.SelectedIndex = 0;

            LoadProducts();
        }

        private void LoadProducts()
        {
            var list = AppConnect.model01.Products.ToList();

            if (!string.IsNullOrWhiteSpace(SearchBox?.Text))
            {
                var s = SearchBox.Text.ToLower();
                list = list.Where(x =>
                    (x.Name  != null && x.Name.ToLower().Contains(s)) ||
                    (x.Brand != null && x.Brand.ToLower().Contains(s))).ToList();
            }

            if (CategoryBox?.SelectedIndex > 0)
                list = list.Where(x => x.Category == CategoryBox.SelectedItem.ToString()).ToList();

            switch (SortBox?.SelectedIndex)
            {
                case 1: list = list.OrderBy(x => x.Price).ToList(); break;
                case 2: list = list.OrderByDescending(x => x.Price).ToList(); break;
            }

            ProductsGrid.ItemsSource = list;
            CounterBlock.Text = list.Count > 0
                ? $"Найдено товаров: {list.Count}"
                : "Товары не найдены";
        }

        private void OnFilterChanged(object sender, EventArgs e) => LoadProducts();
        private void OnSortChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();

        private void OnProductDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CanManage && ProductsGrid.SelectedItem is Products p)
                NavigationService.Navigate(new AddRecip(p));
        }

        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not Products p)
            { Info("Выберите товар!"); return; }
            try
            {
                var ex = AppConnect.model01.Cart.FirstOrDefault(c =>
                    c.ProductId == p.Id && c.UserLogin == AppConnect.CurrentUser.Login);
                if (ex == null)
                    AppConnect.model01.Cart.Add(new Cart
                    { ProductId=p.Id, ProductName=p.Name, UnitPrice=p.Price,
                      Quantity=1, UserLogin=AppConnect.CurrentUser.Login });
                else ex.Quantity++;
                AppConnect.model01.SaveChanges();
                Info($"«{p.Name}» добавлен в корзину 🛒");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void OnAddToFavClick(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not Products p)
            { Info("Выберите товар!"); return; }
            try
            {
                if (AppConnect.model01.Favorites.Any(f =>
                    f.ProductId == p.Id && f.UserLogin == AppConnect.CurrentUser.Login))
                { Info($"«{p.Name}» уже в избранном!"); return; }

                AppConnect.model01.Favorites.Add(new Favorites
                { ProductId=p.Id, UserLogin=AppConnect.CurrentUser.Login, AddedAt=DateTime.Now });
                AppConnect.model01.SaveChanges();
                Info($"«{p.Name}» добавлен в избранное ⭐");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void OnAddProductClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new AddRecip(null));

        private void OnEditProductClick(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is Products p)
                NavigationService.Navigate(new AddRecip(p));
            else Info("Выберите товар!");
        }

        private void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.SelectedItem is not Products p)
            { Info("Выберите товар!"); return; }
            if (MessageBox.Show($"Удалить «{p.Name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model01.Products.Remove(p);
                    AppConnect.model01.SaveChanges();
                    LoadProducts();
                }
                catch (Exception ex) { Err(ex.Message); }
            }
        }

        private void OnFavoritesClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new PageFavorites());
        private void OnCartClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new PageCart());
        private void OnProfileClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new PageProfile());
        private void OnAdminClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new PageAdmin());
        private void OnLogoutClick(object sender, RoutedEventArgs e)
        {
            AppConnect.CurrentUser = null!;
            NavigationService.Navigate(new Autorization());
        }

        private void Info(string m) => MessageBox.Show(m, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void Err(string m)  => MessageBox.Show(m, "Ошибка",     MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
