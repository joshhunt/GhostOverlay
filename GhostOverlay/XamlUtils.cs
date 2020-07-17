using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;

namespace GhostOverlay
{
    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var input = System.Convert.ToBoolean(value);
            var paramString = parameter as string;

            if (paramString?.Equals("CollapsedWhenTrue") ?? false)
            {
                return input ? Visibility.Collapsed : Visibility.Visible;
            }

            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var colors = new[] {"#FF00FF00", "#FFFF0000"};
            var paramString = parameter as string;
            var input = System.Convert.ToBoolean(value);

            if (!string.IsNullOrEmpty(paramString))
            {
                colors = paramString.Split(new char[] { '|' });
            }

            var resultColor = input ? colors[0] : colors[1];

            Color x = (Color)XamlBindingHelper.ConvertValue(typeof(Color), resultColor);
            return new SolidColorBrush(x);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

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
