// ============================================================
//  CustomFontResolver.cs
//  WpfApp1337/ApplicationData/CustomFontResolver.cs
//
//  Решает ошибку:
//  "No appropriate font found for family name 'Times New Roman'.
//   Implement IFontResolver and assign to GlobalFontSettings.FontResolver"
// ============================================================

using PdfSharp.Fonts;
using System;
using System.IO;

namespace WpfApp1337.ApplicationData
{
    public class CustomFontResolver : IFontResolver
    {
        // Единственный экземпляр — вызывать один раз при старте приложения
        public static readonly CustomFontResolver Instance = new();

        // Папка со шрифтами Windows
        private static readonly string FontsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        // ── Возвращает имя файла шрифта по имени гарнитуры ──
        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string lower = familyName.ToLowerInvariant();

            // Arial
            if (lower == "arial")
            {
                if (isBold && isItalic) return new FontResolverInfo("Arial#BI");
                if (isBold)            return new FontResolverInfo("Arial#B");
                if (isItalic)          return new FontResolverInfo("Arial#I");
                return new FontResolverInfo("Arial");
            }

            // Times New Roman
            if (lower is "times new roman" or "times")
            {
                if (isBold && isItalic) return new FontResolverInfo("Times#BI");
                if (isBold)            return new FontResolverInfo("Times#B");
                if (isItalic)          return new FontResolverInfo("Times#I");
                return new FontResolverInfo("Times");
            }

            // Courier New
            if (lower is "courier new" or "courier")
                return new FontResolverInfo("Courier");

            // Для всего остального — Arial как запасной
            return new FontResolverInfo("Arial");
        }

        // ── Возвращает байты файла шрифта ────────────────────
        public byte[]? GetFont(string faceName)
        {
            string? file = faceName switch
            {
                "Arial"    => "arial.ttf",
                "Arial#B"  => "arialbd.ttf",
                "Arial#I"  => "ariali.ttf",
                "Arial#BI" => "arialbi.ttf",
                "Times"    => "times.ttf",
                "Times#B"  => "timesbd.ttf",
                "Times#I"  => "timesi.ttf",
                "Times#BI" => "timesbi.ttf",
                "Courier"  => "cour.ttf",
                _          => "arial.ttf"   // запасной
            };

            string fullPath = Path.Combine(FontsFolder, file);

            // Если файл не найден — пробуем arial как запасной
            if (!File.Exists(fullPath))
                fullPath = Path.Combine(FontsFolder, "arial.ttf");

            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
        }
    }
}
