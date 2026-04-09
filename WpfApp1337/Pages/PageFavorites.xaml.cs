// ============================================================
//  PageFavorites.xaml.cs
//  WpfApp1337/Pages/PageFavorites.xaml.cs
//
//  NuGet пакеты (добавить в .csproj):
//  <PackageReference Include="QRCoder" Version="1.4.3" />
//  <PackageReference Include="PdfSharp" Version="6.0.0" />
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfApp1337.ApplicationData;

// PDF
using PdfSharp.Drawing;
using PdfSharp.Pdf;

// QR
using QRCoder;

namespace WpfApp1337.Pages
{
    public partial class PageFavorites : Page
    {
        private List<Products> _favoriteProducts = new();

        public PageFavorites()
        {
            InitializeComponent();
            LoadFavorites();
        }

        // ──────────────────────────────────────────────────────
        //  ЗАГРУЗКА ИЗБРАННОГО
        // ──────────────────────────────────────────────────────
        private void LoadFavorites()
        {
            try
            {
                // Получаем ID избранных товаров текущего пользователя
                var favoriteIds = AppConnect.model01.Favorites
                    .Where(f => f.UserLogin == AppConnect.CurrentUser.Login)
                    .Select(f => f.ProductId)
                    .ToList();

                // Загружаем полные данные товаров
                _favoriteProducts = AppConnect.model01.Products
                    .Where(p => favoriteIds.Contains(p.Id))
                    .ToList();

                FavoritesListView.ItemsSource = _favoriteProducts;
                CountTextBlock.Text = $"Избранных товаров: {_favoriteProducts.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки избранного:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────────────────────────────────────────────────
        //  УДАЛЕНИЕ ИЗ ИЗБРАННОГО
        // ──────────────────────────────────────────────────────
        private void OnDeleteFavoriteClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var product = btn?.DataContext as Products;
            if (product == null) return;

            var result = MessageBox.Show(
                $"Удалить \"{product.Name}\" из избранного?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var item = AppConnect.model01.Favorites
                    .FirstOrDefault(f =>
                        f.ProductId == product.Id &&
                        f.UserLogin == AppConnect.CurrentUser.Login);

                if (item != null)
                {
                    AppConnect.model01.Favorites.Remove(item);
                    AppConnect.model01.SaveChanges();
                }

                LoadFavorites(); // Обновляем список
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────────────────────────────────────────────────
        //  ДОБАВИТЬ В КОРЗИНУ ИЗ ИЗБРАННОГО
        // ──────────────────────────────────────────────────────
        private void OnAddToCartClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var product = btn?.DataContext as Products;
            if (product == null) return;

            try
            {
                var existing = AppConnect.model01.Cart
                    .FirstOrDefault(c =>
                        c.ProductId == product.Id &&
                        c.UserLogin == AppConnect.CurrentUser.Login);

                if (existing == null)
                    AppConnect.model01.Cart.Add(new Cart
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = 1,
                        UserLogin = AppConnect.CurrentUser.Login
                    });
                else
                    existing.Quantity++;

                AppConnect.model01.SaveChanges();
                MessageBox.Show($"«{product.Name}» добавлен в корзину!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────────────────────────────────────────────────
        //  ГЕНЕРАЦИЯ QR-КОДА ДЛЯ PDF (работает через BMP)
        // ──────────────────────────────────────────────────────
        /// <summary>Создаёт XImage с QR-кодом для PDFsharp через временный BMP-файл</summary>
        private static XImage GenerateQrXImage(string content, int size = 200)
        {
            using var qrGenerator = new QRCodeGenerator();
            QRCodeData qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            // Используем BitmapByteQRCode (создаёт BMP, который лучше работает с PDFsharp Core)
            using var qrCode = new BitmapByteQRCode(qrData);
            int pixelsPerModule = size / 25;
            if (pixelsPerModule < 1) pixelsPerModule = 1;
            byte[] qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);

            // Сохраняем во временный BMP-файл
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".bmp");
            File.WriteAllBytes(tempFile, qrCodeBytes);

            // Загружаем через FromFile (обходит проблему с MemoryStream в PDFsharp Core)
            XImage image = XImage.FromFile(tempFile);

            // Удаляем временный файл
            try { File.Delete(tempFile); } catch { }

            return image;
        }

        // ──────────────────────────────────────────────────────
        //  ГЕНЕРАЦИЯ PDF-КАТАЛОГА ИЗБРАННОГО
        // ──────────────────────────────────────────────────────
        private void OnGeneratePdfClick(object sender, RoutedEventArgs e)
        {
            if (!_favoriteProducts.Any())
            {
                MessageBox.Show("Список избранного пуст!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Путь для сохранения
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string pdfPath = Path.Combine(desktopPath, $"Избранное_{DateTime.Now:yyyyMMdd_HHmm}.pdf");

                using var doc = new PdfDocument();
                doc.Info.Title = "Избранные товары";
                doc.Info.Author = AppConnect.CurrentUser.UserName;

                foreach (var product in _favoriteProducts)
                {
                    PdfPage page = doc.AddPage();
                    page.Width = XUnit.FromMillimeter(210);  // A4
                    page.Height = XUnit.FromMillimeter(297);

                    using XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Шрифты (Times New Roman)
                    var fontTitle = new XFont("Times New Roman", 18, XFontStyleEx.Bold);
                    var fontNormal = new XFont("Times New Roman", 12, XFontStyleEx.Regular);
                    var fontSmall = new XFont("Times New Roman", 10, XFontStyleEx.Regular);
                    var fontBold = new XFont("Times New Roman", 14, XFontStyleEx.Bold);

                    double marginLeft = 40;
                    double y = 40;

                    // ── Заголовок страницы ────────────────────
                    gfx.DrawString("Магазин бытовой техники — Избранное",
                        fontTitle, XBrushes.DarkBlue,
                        new XRect(marginLeft, y, page.Width - 80, 30),
                        XStringFormats.TopLeft);
                    y += 35;

                    // Линия
                    gfx.DrawLine(new XPen(XColors.Gold, 2), marginLeft, y, page.Width - marginLeft, y);
                    y += 15;

                    // ── Фото товара ───────────────────────────
                    double imgX = marginLeft;
                    double imgY = y;
                    double imgW = 160;
                    double imgH = 120;

                    bool hasImage = false;
                    if (!string.IsNullOrEmpty(product.ImagePath) && File.Exists(product.ImagePath))
                    {
                        try
                        {
                            using var img = XImage.FromFile(product.ImagePath);
                            gfx.DrawImage(img, imgX, imgY, imgW, imgH);
                            hasImage = true;
                        }
                        catch { /* картинка не загрузилась — пропускаем */ }
                    }

                    if (!hasImage)
                    {
                        // Заглушка для отсутствующего фото
                        gfx.DrawRectangle(XBrushes.LightGray,
                            new XRect(imgX, imgY, imgW, imgH));
                        gfx.DrawString("Нет фото", fontSmall, XBrushes.Gray,
                            new XRect(imgX, imgY + imgH / 2 - 8, imgW, 20),
                            XStringFormats.Center);
                    }

                    // ── Данные товара справа от фото ──────────
                    double textX = marginLeft + imgW + 20;
                    double textW = page.Width - textX - marginLeft;
                    double ty = imgY;

                    gfx.DrawString(product.Name ?? "",
                        fontBold, XBrushes.Black,
                        new XRect(textX, ty, textW, 25), XStringFormats.TopLeft);
                    ty += 28;

                    gfx.DrawString($"Бренд: {product.Brand ?? "—"}",
                        fontNormal, XBrushes.DarkGray,
                        new XRect(textX, ty, textW, 20), XStringFormats.TopLeft);
                    ty += 22;

                    gfx.DrawString($"Категория: {product.Category ?? "—"}",
                        fontNormal, XBrushes.DarkGray,
                        new XRect(textX, ty, textW, 20), XStringFormats.TopLeft);
                    ty += 22;

                    gfx.DrawString($"В наличии: {product.Quantity} шт.",
                        fontNormal, XBrushes.DarkGray,
                        new XRect(textX, ty, textW, 20), XStringFormats.TopLeft);
                    ty += 30;

                    // Цена крупно
                    gfx.DrawString($"{product.Price:N0} ₽",
                        new XFont("Times New Roman", 22, XFontStyleEx.Bold),
                        XBrushes.DodgerBlue,
                        new XRect(textX, ty, textW, 30), XStringFormats.TopLeft);

                    y = imgY + imgH + 20;

                    // ── Описание ─────────────────────────────
                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        gfx.DrawString("Описание:",
                            fontBold, XBrushes.Black,
                            new XRect(marginLeft, y, page.Width - 80, 20),
                            XStringFormats.TopLeft);
                        y += 22;

                        gfx.DrawString(product.Description,
                            fontNormal, XBrushes.Black,
                            new XRect(marginLeft, y, page.Width - 80, 60),
                            XStringFormats.TopLeft);
                        y += 65;
                    }

                    // ── QR-код для данного товара (РАБОТАЕТ) ──
                    try
                    {
                        string qrUrl = $"https://technoshop.ru/product/{product.Id}";

                        using (XImage qrImage = GenerateQrXImage(qrUrl, 200))
                        {
                            double qrSize = 100;
                            double qrX = page.Width - marginLeft - qrSize;
                            double qrY = y;

                            gfx.DrawImage(qrImage, qrX, qrY, qrSize, qrSize);
                        }

                        gfx.DrawString("Скан для деталей:",
                            fontSmall, XBrushes.Gray,
                            new XRect(page.Width - marginLeft - 100, y + 103, 100, 15),
                            XStringFormats.TopLeft);
                    }
                    catch (Exception qrEx)
                    {
                        // Если QR-код не создался, показываем текст вместо него
                        gfx.DrawString("QR-код недоступен",
                            fontSmall, XBrushes.Red,
                            new XRect(page.Width - marginLeft - 100, y, 100, 50),
                            XStringFormats.TopLeft);
                    }

                    // ── Нижний колонтитул ─────────────────────
                    gfx.DrawLine(new XPen(XColors.LightGray, 1),
                        marginLeft, page.Height - 35,
                        page.Width - marginLeft, page.Height - 35);

                    gfx.DrawString(
                        $"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}  |  Пользователь: {AppConnect.CurrentUser.UserName}",
                        fontSmall, XBrushes.Gray,
                        new XRect(marginLeft, page.Height - 28, page.Width - 80, 20),
                        XStringFormats.TopLeft);
                }

                doc.Save(pdfPath);
                MessageBox.Show($"PDF сохранён:\n{pdfPath}", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Открываем файл
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания PDF:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────────────────────────────────────────────────
        //  QR-КОД ОБЩЕГО КАТАЛОГА
        // ──────────────────────────────────────────────────────
        private void OnShowQRClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "https://technoshop.ru/catalog";
                byte[] qrBytes = GenerateQrBytes(url, 300);

                // Показываем QR в новом окне
                var win = new Window
                {
                    Title = "QR-код каталога",
                    Width = 380,
                    Height = 420,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White,
                    ResizeMode = ResizeMode.NoResize
                };

                var panel = new StackPanel { Margin = new Thickness(20) };

                var bitmap = BytesToBitmapImage(qrBytes);
                panel.Children.Add(new Image
                {
                    Source = bitmap,
                    Width = 300,
                    Height = 300
                });
                panel.Children.Add(new TextBlock
                {
                    Text = url,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = System.Windows.Media.Brushes.Gray
                });

                win.Content = panel;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации QR:\n{ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ──────────────────────────────────────────────────────
        //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ──────────────────────────────────────────────────────

        /// <summary>Генерирует QR-код и возвращает PNG-байты</summary>
        private static byte[] GenerateQrBytes(string content, int pixelSize = 300)
        {
            using var qrGenerator = new QRCodeGenerator();
            QRCodeData qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            int pixelsPerModule = pixelSize / 25;
            if (pixelsPerModule < 1) pixelsPerModule = 1;
            return qrCode.GetGraphic(pixelsPerModule);
        }

        /// <summary>Конвертирует PNG-байты в WPF BitmapImage</summary>
        private static BitmapImage BytesToBitmapImage(byte[] bytes)
        {
            var bitmap = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}