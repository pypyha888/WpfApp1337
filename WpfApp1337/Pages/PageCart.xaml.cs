using System;
using System.Collections.Generic;
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
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList();
            CartGrid.ItemsSource = cart;

            decimal total = cart.Sum(x => x.TotalPrice);
            TotalBlock.Text = $"Итого: {total:N0} руб.";
        }

        private void OnRemoveClick(object sender, RoutedEventArgs e)
        {
            if (!(CartGrid.SelectedItem is Cart item))
            { Info("Выберите позицию!"); return; }
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
                .Where(x => x.UserLogin == AppConnect.CurrentUser.Login)
                .ToList();

            if (!cart.Any()) { Info("Корзина пуста!"); return; }

            try
            {
                var snapshot = new List<(string Name, decimal UnitPrice, int Qty, decimal Total)>();
                foreach (var item in cart)
                {
                    snapshot.Add((
                        item.ProductName ?? "",
                        item.UnitPrice,
                        item.Quantity,
                        item.TotalPrice
                    ));
                }

                decimal grandTotal = snapshot.Sum(i => i.Total);

                foreach (var item in cart)
                {
                    AppConnect.model01.Orders.Add(new Orders
                    {
                        UserLogin = item.UserLogin,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        OrderDate = DateTime.Now,
                        Status = "Оформлен"
                    });
                    AppConnect.model01.Cart.Remove(item);
                }
                AppConnect.model01.SaveChanges();

                string pdfPath = GenerateReceipt(snapshot, grandTotal);
                LoadCart();

                var res = MessageBox.Show(
                    "Заказ оформлен!\n\nОткрыть PDF-чек?",
                    "Готово", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes && File.Exists(pdfPath))
                {
                    // ===============================================================
                    // ИСПРАВЛЕНО: Используем UseShellExecute = true для открытия файла
                    // ===============================================================
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pdfPath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(processStartInfo);
                }
            }
            catch (Exception ex) { Err($"Ошибка:\n{ex.Message}"); }
        }

        private string GenerateReceipt(
            List<(string Name, decimal UnitPrice, int Qty, decimal Total)> items,
            decimal grandTotal)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Чек_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            string orderNum = $"{DateTime.Now:yyyyMMdd}-{AppConnect.CurrentUser.Id:000}";

            string tmpQr = Path.Combine(Path.GetTempPath(), $"qr_{Guid.NewGuid():N}.png");
            bool qrDone = false;
            try
            {
                using (var gen = new QRCodeGenerator())
                {
                    var qrData = gen.CreateQrCode(
                        $"Чек №{orderNum} | {AppConnect.CurrentUser.UserName} | {grandTotal:N0} руб.",
                        QRCodeGenerator.ECCLevel.Q);
                    using (var pngQr = new PngByteQRCode(qrData))
                    {
                        File.WriteAllBytes(tmpQr, pngQr.GetGraphic(8));
                        qrDone = true;
                    }
                }
            }
            catch { }

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = "Чек заказа";
                doc.Info.Author = AppConnect.CurrentUser.UserName ?? "";

                var page = doc.AddPage();
                page.Width = XUnit.FromMillimeter(210);
                page.Height = XUnit.FromMillimeter(297);

                using (var gfx = XGraphics.FromPdfPage(page))
                {
                    // Используем шрифт, который гарантированно есть в системе
                    var fTitle = new XFont("Segoe UI", 20, XFontStyleEx.Bold);
                    var fHead = new XFont("Segoe UI", 13, XFontStyleEx.Bold);
                    var fNorm = new XFont("Segoe UI", 11, XFontStyleEx.Regular);
                    var fSmall = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
                    var fBig = new XFont("Segoe UI", 16, XFontStyleEx.Bold);

                    double ml = 40;
                    double pw = page.Width.Point - 80;
                    double y = 35;

                    gfx.DrawString("Магазин бытовой техники", fTitle, XBrushes.DarkBlue,
                        new XRect(ml, y, pw, 28), XStringFormats.TopLeft);
                    y += 30;
                    gfx.DrawString("ООО «ТехноШоп»  |  technoshop.ru", fSmall, XBrushes.Gray,
                        new XRect(ml, y, pw, 16), XStringFormats.TopLeft);
                    y += 22;
                    gfx.DrawLine(new XPen(XColors.DarkBlue, 2), ml, y, page.Width.Point - ml, y);
                    y += 14;

                    gfx.DrawString($"ЧЕК №{orderNum}", fHead, XBrushes.Black,
                        new XRect(ml, y, pw, 20), XStringFormats.TopLeft);
                    y += 24;
                    gfx.DrawString($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}", fNorm, XBrushes.Black,
                        new XRect(ml, y, pw / 2, 18), XStringFormats.TopLeft);
                    gfx.DrawString($"Покупатель: {AppConnect.CurrentUser.UserName}", fNorm, XBrushes.Black,
                        new XRect(ml + pw / 2, y, pw / 2, 18), XStringFormats.TopLeft);
                    y += 28;

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

                    gfx.DrawLine(new XPen(XColors.DarkBlue, 1.5), ml, y, page.Width.Point - ml, y);
                    y += 10;
                    gfx.DrawString($"ИТОГО: {grandTotal:N0} руб.", fBig, XBrushes.DarkBlue,
                        new XRect(ml, y, pw, 24), XStringFormats.TopRight);
                    y += 34;

                    gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(232, 245, 233)),
                        new XRect(ml, y, pw, 24));
                    gfx.DrawString("Статус: Оформлен  |  Ожидайте подтверждения",
                        fNorm, XBrushes.DarkGreen,
                        new XRect(ml + 6, y + 4, pw, 18), XStringFormats.TopLeft);
                    y += 40;

                    if (qrDone && File.Exists(tmpQr))
                    {
                        try
                        {
                            double qrSz = 110;
                            double qrX = page.Width.Point - ml - qrSz;
                            var qrImg = XImage.FromFile(tmpQr);
                            gfx.DrawImage(qrImg, qrX, y, qrSz, qrSz);
                            qrImg.Dispose();
                            gfx.DrawString("QR-код заказа", fSmall, XBrushes.Gray,
                                new XRect(qrX, y + qrSz + 4, qrSz, 14), XStringFormats.TopCenter);
                        }
                        catch { }
                        finally { try { File.Delete(tmpQr); } catch { } }
                    }

                    gfx.DrawString("Спасибо за покупку!", fHead, XBrushes.DarkBlue,
                        new XRect(ml, y + 10, pw * 0.6, 24), XStringFormats.TopLeft);
                    gfx.DrawString("support@technoshop.ru", fNorm, XBrushes.Gray,
                        new XRect(ml, y + 36, pw * 0.6, 18), XStringFormats.TopLeft);

                    double yf = page.Height.Point - 30;
                    gfx.DrawLine(new XPen(XColors.LightGray, 1), ml, yf, page.Width.Point - ml, yf);
                    gfx.DrawString($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                        fSmall, XBrushes.Gray,
                        new XRect(ml, yf + 4, pw, 14), XStringFormats.TopLeft);
                }

                doc.Save(path);
            }

            return path;
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        private void Info(string m) =>
            MessageBox.Show(m, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        private void Err(string m) =>
            MessageBox.Show(m, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}