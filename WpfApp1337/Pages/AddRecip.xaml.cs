using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;

namespace WpfApp1337.Pages
{
    public partial class AddRecip : Page
    {
        private Products _editingProduct;
        private bool _isEditMode;

        public AddRecip(Products product = null)
        {
            InitializeComponent();

            if (product != null)
            {
                _isEditMode      = true;
                _editingProduct  = product;
                LoadCategories();
                LoadProductData();
            }
            else
            {
                _isEditMode     = false;
                _editingProduct = new Products();
                LoadCategories();
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = AppConnect.model01.Categories.ToList();
                CategoryComboBox.ItemsSource        = categories;
                CategoryComboBox.DisplayMemberPath  = "CategoryName";
                CategoryComboBox.SelectedValuePath  = "CategoryName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductData()
        {
            if (_editingProduct == null) return;

            NameTextBox.Text     = _editingProduct.Name;
            BrandTextBox.Text    = _editingProduct.Brand;
            PriceTextBox.Text    = _editingProduct.Price.ToString();
            QuantityTextBox.Text = _editingProduct.Quantity.ToString();

            CategoryComboBox.SelectedItem = CategoryComboBox.Items
                .Cast<Categories>()
                .FirstOrDefault(x => x.CategoryName == _editingProduct.Category);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;

                if (_isEditMode)
                    UpdateProduct();
                else
                    AddProduct();

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                return false;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int qty) || qty < 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                return false;
            }

            return true;
        }

        private void AddProduct()
        {
            var cat = CategoryComboBox.SelectedItem as Categories;
            AppConnect.model01.Products.Add(new Products
            {
                Name     = NameTextBox.Text.Trim(),
                Brand    = BrandTextBox.Text.Trim(),
                Category = cat.CategoryName,
                Price    = decimal.Parse(PriceTextBox.Text),
                Quantity = int.Parse(QuantityTextBox.Text)
            });
            AppConnect.model01.SaveChanges();
            MessageBox.Show("Товар добавлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateProduct()
        {
            var cat = CategoryComboBox.SelectedItem as Categories;
            _editingProduct.Name     = NameTextBox.Text.Trim();
            _editingProduct.Brand    = BrandTextBox.Text.Trim();
            _editingProduct.Category = cat.CategoryName;
            _editingProduct.Price    = decimal.Parse(PriceTextBox.Text);
            _editingProduct.Quantity = int.Parse(QuantityTextBox.Text);
            AppConnect.model01.SaveChanges();
            MessageBox.Show("Товар обновлён!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
