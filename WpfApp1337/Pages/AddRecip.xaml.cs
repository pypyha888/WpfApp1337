using WpfApp1337.ApplicationData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1337.Pages.AddRecip
{
    public partial class ProductEditorPage : Page
    {
        private Products _editingProduct;
        private bool _isEditMode;

        public ProductEditorPage(Products product = null)
        {
            InitializeComponent();

            if (product != null)
            {
                _isEditMode = true;
                _editingProduct = product;
                Title = "Редактирование товара";
                LoadCategories();
                LoadProductData();
            }
            else
            {
                _isEditMode = false;
                _editingProduct = new Products();
                Title = "Добавление товара";
                LoadCategories();
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = AppConnect.model01.Categories.ToList();
                CategoryComboBox.ItemsSource = categories;
                CategoryComboBox.DisplayMemberPath = "CategoryName";
                CategoryComboBox.SelectedValuePath = "CategoryName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProductData()
        {
            if (_editingProduct != null)
            {
                NameTextBox.Text = _editingProduct.Name;
                CategoryComboBox.SelectedItem = CategoryComboBox.Items
                    .Cast<Categories>()
                    .FirstOrDefault(x => x.CategoryName == _editingProduct.Category);
                BrandTextBox.Text = _editingProduct.Brand;
                PriceTextBox.Text = _editingProduct.Price.ToString();
                QuantityTextBox.Text = _editingProduct.Quantity.ToString();
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                if (_isEditMode)
                {
                    UpdateProduct();
                }
                else
                {
                    AddProduct();
                }

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
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
                MessageBox.Show("Введите название товара!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите категорию!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(BrandTextBox.Text))
            {
                MessageBox.Show("Введите бренд!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BrandTextBox.Focus();
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PriceTextBox.Focus();
                PriceTextBox.SelectAll();
                return false;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                QuantityTextBox.Focus();
                QuantityTextBox.SelectAll();
                return false;
            }

            return true;
        }

        private void AddProduct()
        {
            var newProduct = new Products()
            {
                Name = NameTextBox.Text.Trim(),
                Category = (CategoryComboBox.SelectedItem as Categories).CategoryName,
                Brand = BrandTextBox.Text.Trim(),
                Price = decimal.Parse(PriceTextBox.Text),
                Quantity = int.Parse(QuantityTextBox.Text)
            };

            AppConnect.model01.Products.Add(newProduct);
            AppConnect.model01.SaveChanges();

            MessageBox.Show("Товар успешно добавлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UpdateProduct()
        {
            _editingProduct.Name = NameTextBox.Text.Trim();
            _editingProduct.Category = (CategoryComboBox.SelectedItem as Categories).CategoryName;
            _editingProduct.Brand = BrandTextBox.Text.Trim();
            _editingProduct.Price = decimal.Parse(PriceTextBox.Text);
            _editingProduct.Quantity = int.Parse(QuantityTextBox.Text);

            AppConnect.model01.SaveChanges();

            MessageBox.Show("Товар успешно обновлен!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}