using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;

namespace GhostOverlay
{
    public class NumberFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var incomingNumber = (long)value;
            return $"{incomingNumber,0:n0}";
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DestinySymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var lang = AppState.Data.Language.Value ?? Definitions.FallbackLanguage;

            if (value is string text && Definitions.IconData.ContainsKey(lang))
            {
                var iconDataForLanguage = Definitions.IconData[lang];

                foreach (var icon in iconDataForLanguage)
                {
                    text = text.Replace(icon[0], icon[1]);
                }

                return text;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
