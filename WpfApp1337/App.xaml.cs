using System.Windows;

namespace WpfApp1337
{
    public partial class App : Application
    {
        // EF6 не требует FontResolver — PdfSharp используется отдельно
        // Регистрация FontResolver для PDF если нужно:
        // protected override void OnStartup(StartupEventArgs e)
        // {
        //     base.OnStartup(e);
        //     PdfSharp.Fonts.GlobalFontSettings.FontResolver = new WpfApp1337.ApplicationData.CustomFontResolver();
        // }
    }
}
