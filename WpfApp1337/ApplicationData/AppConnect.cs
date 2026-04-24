using System.Windows;

namespace WpfApp1337.ApplicationData
{
    public static class AppConnect
    {
        // EF6 контекст — генерируется из EDMX автоматически
        public static TechnoShopEntities model01 = new TechnoShopEntities();

        // Текущий авторизованный пользователь
        public static Users CurrentUser { get; set; }
    }
}
