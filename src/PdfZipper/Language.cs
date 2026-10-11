using System.Globalization;

namespace PdfZipper;

public static class Language
{
    public static bool IsGerman => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
    public static string Text(string german, string english) => IsGerman ? german : english;
}
