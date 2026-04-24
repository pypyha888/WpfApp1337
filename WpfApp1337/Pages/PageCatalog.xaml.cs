using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    // ── Конвертер пути к изображению (с заглушкой) ─────────────────────────
    public class ImagePathConverter : IValueConverter
    {
        private static readonly string[] Roots = new[]
        {
            AppDomain.CurrentDomain.BaseDirectory,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."),
        };

        // Кэшируем заглушку, чтобы не грузить каждый раз
        private static BitmapImage _placeholderImage;

        private static BitmapImage GetPlaceholderImage()
        {
            if (_placeholderImage != null)
                return _placeholderImage;

            try
            {
                // Ищем no_image.jpg в разных местах
                string[] possiblePaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "no_image.jpg"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "no_image.jpg"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "no_image.jpg"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Resources", "no_image.jpg"),
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        _placeholderImage = LoadImage(path);
                        if (_placeholderImage != null)
                            return _placeholderImage;
                    }
                }

                // Если файл не найден, пробуем найти любой jpg/png в папке Resources
                string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                if (Directory.Exists(resourcesPath))
                {
                    var anyImage = Directory.GetFiles(resourcesPath, "*.jpg").FirstOrDefault()
                                 ?? Directory.GetFiles(resourcesPath, "*.png").FirstOrDefault();
                    if (anyImage != null && File.Exists(anyImage))
                    {
                        _placeholderImage = LoadImage(anyImage);
                        if (_placeholderImage != null)
                            return _placeholderImage;
                    }
                }
            }
            catch { }

            return null;
        }

        private static BitmapImage LoadImage(string fullPath)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (!(value is string path) || string.IsNullOrWhiteSpace(path))
                return GetPlaceholderImage();

            try
            {
                if (File.Exists(path))
                    return LoadImage(path);

                foreach (var root in Roots)
                {
                    string full = Path.GetFullPath(Path.Combine(root, path));
                    if (File.Exists(full))
                        return LoadImage(full);
                }
                return GetPlaceholderImage(); // Возвращаем заглушку, если файл не найден
            }
            catch
            {
                return GetPlaceholderImage(); // Возвращаем заглушку при ошибке
            }
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // ── Страница каталога ─────────────────────────────────────
    public partial class PageCatalog : Page
    {
        private bool IsAdmin => AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 1;
        private bool IsManager => AppConnect.CurrentUser != null && AppConnect.CurrentUser.RoleId == 2;
        private bool CanManage => IsAdmin || IsManager;

        private Products _selectedProduct;

        public PageCatalog()
        {
            InitializeComponent();
            Loaded += (s, e) => Init();
        }

        private void Init()
        {
            BtnAdd.Visibility = CanManage ? Visibility.Visible : Visibility.Collapsed;
            BtnEdit.Visibility = CanManage ? Visibility.Visible : Visibility.Collapsed;
            BtnDelete.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            AdminButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            string role = IsAdmin ? "Администратор" : IsManager ? "Менеджер" : "Покупатель";
            HeaderTextBlock.Text =
                $"Магазин бытовой техники  |  {AppConnect.CurrentUser.UserName}  [{role}]";

            SortBox.Items.Clear();
            SortBox.Items.Add("Без сортировки");
            SortBox.Items.Add("По возрастанию цены");
            SortBox.Items.Add("По убыванию цены");
            SortBox.Items.Add("По названию А-Я");
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

            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var s = SearchBox.Text.ToLower();
                list = list.Where(x =>
                    (x.Name != null && x.Name.ToLower().Contains(s)) ||
                    (x.Brand != null && x.Brand.ToLower().Contains(s))).ToList();
            }

            if (CategoryBox.SelectedIndex > 0)
            {
                string sel = CategoryBox.SelectedItem.ToString();
                list = list.Where(x => x.Category == sel).ToList();
            }

            switch (SortBox.SelectedIndex)
            {
                case 1: list = list.OrderBy(x => x.Price).ToList(); break;
                case 2: list = list.OrderByDescending(x => x.Price).ToList(); break;
                case 3: list = list.OrderBy(x => x.Name).ToList(); break;
            }

            ProductsGrid.ItemsSource = null;
            ProductsGrid.ItemsSource = list;

            CounterBlock.Text = list.Count > 0
                ? $"Найдено товаров: {list.Count}"
                : "Товары не найдены";
        }

        private void OnProductDoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Products;
            if (CanManage && _selectedProduct != null)
                NavigationService.Navigate(new AddRecip(_selectedProduct));
        }

        private void OnFilterChanged(object sender, EventArgs e) => LoadProducts();
        private void OnSortChanged(object sender, SelectionChangedEventArgs e) => LoadProducts();

        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Products;
            if (_selectedProduct == null) { Info("Выберите товар!"); return; }
            AddToCart(_selectedProduct);
        }

        private void OnAddToFavClick(object sender, RoutedEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Products;
            if (_selectedProduct == null) { Info("Выберите товар!"); return; }
            AddToFav(_selectedProduct);
        }

        private void OnAddProductClick(object sender, RoutedEventArgs e)
            => NavigationService.Navigate(new AddRecip(null));

        private void OnEditProductClick(object sender, RoutedEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Products;
            if (_selectedProduct != null)
                NavigationService.Navigate(new AddRecip(_selectedProduct));
            else Info("Выберите товар!");
        }

        private void OnDeleteProductClick(object sender, RoutedEventArgs e)
        {
            _selectedProduct = ProductsGrid.SelectedItem as Products;
            if (_selectedProduct == null) { Info("Выберите товар!"); return; }

            if (MessageBox.Show($"Удалить «{_selectedProduct.Name}»?",
                "Подтверждение", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    AppConnect.model01.Products.Remove(_selectedProduct);
                    AppConnect.model01.SaveChanges();
                    _selectedProduct = null;
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
            AppConnect.CurrentUser = null;
            NavigationService.Navigate(new Autorization());
        }

        private void AddToCart(Products p)
        {
            try
            {
                var ex = AppConnect.model01.Cart.FirstOrDefault(c =>
                    c.ProductId == p.Id &&
                    c.UserLogin == AppConnect.CurrentUser.Login);

                if (ex == null)
                    AppConnect.model01.Cart.Add(new Cart
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        UnitPrice = p.Price,
                        Quantity = 1,
                        UserLogin = AppConnect.CurrentUser.Login
                    });
                else
                    ex.Quantity++;

                AppConnect.model01.SaveChanges();
                Info($"«{p.Name}» добавлен в корзину!");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void AddToFav(Products p)
        {
            try
            {
                if (AppConnect.model01.Favorites.Any(f =>
                    f.ProductId == p.Id &&
                    f.UserLogin == AppConnect.CurrentUser.Login))
                {
                    Info($"«{p.Name}» уже в избранном!");
                    return;
                }

                AppConnect.model01.Favorites.Add(new Favorites
                {
                    ProductId = p.Id,
                    UserLogin = AppConnect.CurrentUser.Login,
                    AddedAt = DateTime.Now
                });
                AppConnect.model01.SaveChanges();
                Info($"«{p.Name}» добавлен в избранное!");
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void Info(string m) =>
            MessageBox.Show(m, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void Err(string m) =>
            MessageBox.Show(m, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}