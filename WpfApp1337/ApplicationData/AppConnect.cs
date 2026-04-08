// ============================================================
//  AppConnect.cs  — заменить файл WpfApp1337/ApplicationData/AppConnect.cs
// ============================================================

using System;

namespace WpfApp1337.ApplicationData
{
    public static class AppConnect
    {
        // DbContext — единственный экземпляр на всё приложение
        public static TechnoShopEntities model01 = new TechnoShopEntities();

        // Текущий авторизованный пользователь
        // Используется везде: AppConnect.CurrentUser.Login
        public static Users CurrentUser { get; set; } = null!;
    }
}
