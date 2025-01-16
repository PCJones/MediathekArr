using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediathekArr.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Remove all accents from a string but keep German Umlauts
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string RemoveAccentButKeepGermanUmlauts(this string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            if (unicodeCategory != UnicodeCategory.NonSpacingMark || c == '\u0308')
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
