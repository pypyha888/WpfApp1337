using System.Windows;
using PdfSharp.Fonts;
using WpfApp1337.ApplicationData;

namespace WpfApp1337
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Регистрируем FontResolver один раз при старте —
            // решает ошибку "No appropriate font found for family name"
            GlobalFontSettings.FontResolver = CustomFontResolver.Instance;
        }
    }
}
