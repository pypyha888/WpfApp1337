using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfApp1337.ApplicationData;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using QRCoder;

namespace WpfApp1337.Pages
{
    public partial class PageCart : Page
    {
        public PageCart()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadCart();
        }

        private void LoadCart()
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login).ToList();
            CartGrid.ItemsSource = cart;
            decimal total = cart.Sum(x => x.TotalPrice);
            TotalBlock.Text = $"Итого: {total:N0} ₽";
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (CartGrid.SelectedItem is not Cart item)
            { Info("Выберите позицию для удаления!"); return; }
            try
            {
                AppConnect.model01.Cart.Remove(item);
                AppConnect.model01.SaveChanges();
                LoadCart();
            }
            catch (Exception ex) { Err(ex.Message); }
        }

        private void OnCheckoutClick(object sender, RoutedEventArgs e)
        {
            var cart = AppConnect.model01.Cart
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login).ToList();
            if (!cart.Any()) { Info("Корзина пуста!"); return; }

            try
            {
                // Снимок корзины до удаления
                var snapshot = cart.Select(i => (
                    Name:      i.ProductName,
                    UnitPrice: i.UnitPrice,
                    Qty:       i.Quantity,
                    Total:     i.TotalPrice
                )).ToList();

                decimal grandTotal = snapshot.Sum(i => i.Total);

                // Сохраняем заказы, очищаем корзину
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

                // Генерация PDF
                string pdfPath = GenerateReceiptPdf(snapshot, grandTotal);

                LoadCart();

                var result = MessageBox.Show(
                    "Заказ успешно оформлен!\n\nЧек сохранён на рабочем столе.\nОткрыть PDF?",
                    "Заказ оформлен", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes && File.Exists(pdfPath))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = pdfPath, UseShellExecute = true });
            }
            catch (Exception ex) { Err($"Ошибка при оформлении заказа:\n{ex.Message}"); }
        }

        // ─────────────────────────────────────────────────────
        private string GenerateReceiptPdf(
            List<(string Name, decimal UnitPrice, int Qty, decimal Total)> items,
            decimal grandTotal)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Чек_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            // Сначала генерируем QR во временный PNG файл
            string tmpQr = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid():N}.png");
            string orderNum = $"{DateTime.Now:yyyyMMdd}-{AppConnect.CurrentUser.Id:000}";
            string qrText   = $"Чек №{orderNum} | {AppConnect.CurrentUser.UserName} | {grandTotal:N0} руб.";
            bool   qrOk     = false;

            try
            {
                // Используем System.Drawing.Bitmap — надёжно работает с PdfSharp
                using var gen  = new QRCodeGenerator();
                var qrData     = gen.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(qrData);
                using Bitmap bmp = qrCode.GetGraphic(8);         // 8px на модуль → ~200px
                bmp.Save(tmpQr, ImageFormat.Png);
                qrOk = true;
            }
            catch { /* QR не критичен */ }

            using var doc = new PdfDocument();
            doc.Info.Title  = "Чек заказа";
            doc.Info.Author = AppConnect.CurrentUser.UserName;

            var page = doc.AddPage();
            page.Width  = XUnit.FromMillimeter(210);
            page.Height = XUnit.FromMillimeter(297);

            using var gfx = XGraphics.FromPdfPage(page);

            var fTitle = new XFont("Arial", 20, XFontStyleEx.Bold);
            var fHead  = new XFont("Arial", 13, XFontStyleEx.Bold);
            var fNorm  = new XFont("Arial", 11, XFontStyleEx.Regular);
            var fSmall = new XFont("Arial",  9, XFontStyleEx.Regular);
            var fBig   = new XFont("Arial", 16, XFontStyleEx.Bold);

            double ml = 40, mr = 40;
            double pw = page.Width - ml - mr;
            double y  = 35;

            // ── Заголовок ─────────────────────────────────────
            gfx.DrawString("Магазин бытовой техники",
                fTitle, XBrushes.DarkBlue,
                new XRect(ml, y, pw, 28), XStringFormats.TopLeft);
            y += 32;
            gfx.DrawString("ООО «ТехноШоп»  |  technoshop.ru  |  +7 800 000-00-00",
                fSmall, XBrushes.Gray,
                new XRect(ml, y, pw, 16), XStringFormats.TopLeft);
            y += 22;
            gfx.DrawLine(new XPen(XColors.DarkBlue, 2), ml, y, page.Width - mr, y);
            y += 14;

            // ── Реквизиты чека ────────────────────────────────
            gfx.DrawString($"ЧЕК №{orderNum}", fHead, XBrushes.Black,
                new XRect(ml, y, pw, 20), XStringFormats.TopLeft);
            y += 24;
            gfx.DrawString($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}", fNorm, XBrushes.Black,
                new XRect(ml, y, pw / 2, 18), XStringFormats.TopLeft);
            gfx.DrawString($"Покупатель: {AppConnect.CurrentUser.UserName}", fNorm, XBrushes.Black,
                new XRect(ml + pw / 2, y, pw / 2, 18), XStringFormats.TopLeft);
            y += 28;

            // ── Шапка таблицы ─────────────────────────────────
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(220, 230, 245)),
                new XRect(ml, y, pw, 22));
            gfx.DrawString("Наименование", fHead, XBrushes.Black,
                new XRect(ml + 4, y + 3, pw * 0.52, 18), XStringFormats.TopLeft);
            gfx.DrawString("Кол-во", fHead, XBrushes.Black,
                new XRect(ml + pw * 0.54, y + 3, pw * 0.12, 18), XStringFormats.TopLeft);
            gfx.DrawString("Цена", fHead, XBrushes.Black,
                new XRect(ml + pw * 0.67, y + 3, pw * 0.15, 18), XStringFormats.TopLeft);
            gfx.DrawString("Сумма", fHead, XBrushes.Black,
                new XRect(ml + pw * 0.83, y + 3, pw * 0.17, 18), XStringFormats.TopLeft);
            y += 24;

            // ── Строки товаров ────────────────────────────────
            bool alt = false;
            foreach (var item in items)
            {
                if (alt)
                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(248, 248, 252)),
                        new XRect(ml, y, pw, 20));

                gfx.DrawString(item.Name, fNorm, XBrushes.Black,
                    new XRect(ml + 4, y + 2, pw * 0.52, 18), XStringFormats.TopLeft);
                gfx.DrawString(item.Qty.ToString(), fNorm, XBrushes.Black,
                    new XRect(ml + pw * 0.54, y + 2, pw * 0.12, 18), XStringFormats.TopLeft);
                gfx.DrawString($"{item.UnitPrice:N0} р.", fNorm, XBrushes.Black,
                    new XRect(ml + pw * 0.67, y + 2, pw * 0.15, 18), XStringFormats.TopLeft);
                gfx.DrawString($"{item.Total:N0} р.", fNorm, XBrushes.Black,
                    new XRect(ml + pw * 0.83, y + 2, pw * 0.17, 18), XStringFormats.TopLeft);

                y += 22;
                alt = !alt;
            }

            // ── Итого ─────────────────────────────────────────
            gfx.DrawLine(new XPen(XColors.DarkBlue, 1.5), ml, y, page.Width - mr, y);
            y += 10;
            gfx.DrawString($"ИТОГО: {grandTotal:N0} р.", fBig, XBrushes.DarkBlue,
                new XRect(ml, y, pw, 24), XStringFormats.TopRight);
            y += 34;

            // ── Статус ────────────────────────────────────────
            gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(232, 245, 233)),
                new XRect(ml, y, pw, 24));
            gfx.DrawString("Статус заказа: Оформлен  |  Ожидайте подтверждения",
                fNorm, XBrushes.DarkGreen,
                new XRect(ml + 6, y + 4, pw, 18), XStringFormats.TopLeft);
            y += 40;

            // ── QR-код ────────────────────────────────────────
            double qrSz = 110;
            double qrX  = page.Width - mr - qrSz;

            if (qrOk && File.Exists(tmpQr))
            {
                try
                {
                    var qrImg = XImage.FromFile(tmpQr);
                    gfx.DrawImage(qrImg, qrX, y, qrSz, qrSz);
                    qrImg.Dispose();
                    gfx.DrawString("QR-код заказа", fSmall, XBrushes.Gray,
                        new XRect(qrX, y + qrSz + 4, qrSz, 14), XStringFormats.TopCenter);
                }
                catch { /* игнорируем */ }
                finally
                {
                    try { File.Delete(tmpQr); } catch { }
                }
            }

            // ── Текст ─────────────────────────────────────────
            gfx.DrawString("Спасибо за покупку!", fHead, XBrushes.DarkBlue,
                new XRect(ml, y + 10, pw * 0.6, 24), XStringFormats.TopLeft);
            gfx.DrawString("По вопросам: support@technoshop.ru", fNorm, XBrushes.Gray,
                new XRect(ml, y + 36, pw * 0.6, 18), XStringFormats.TopLeft);

            // ── Колонтитул ────────────────────────────────────
            double yf = page.Height - 30;
            gfx.DrawLine(new XPen(XColors.LightGray, 1), ml, yf, page.Width - mr, yf);
            gfx.DrawString($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                fSmall, XBrushes.Gray,
                new XRect(ml, yf + 4, pw, 14), XStringFormats.TopLeft);

            doc.Save(path);
            return path;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        private void Info(string m) => MessageBox.Show(m, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void Err(string m)  => MessageBox.Show(m, "Ошибка",     MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
