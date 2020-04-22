using System;
using System.Globalization;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;

namespace GhostOverlay
{
    public class BooleanNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var numbers = new [] { 1, 0.5 };
            var paramString = parameter as string;
            var input = System.Convert.ToBoolean(value);

            if (!string.IsNullOrEmpty(paramString))
            {
                numbers = paramString.Split(new char[] {'|'}).Select(System.Convert.ToDouble).ToArray();
            }

            var result = input ? numbers[0] : numbers[1];

            return result;
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
            var incomingNumber = (int)value;
            return $"{incomingNumber,0:n0}";
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class UpperCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var incomming = (string)value;
            return incomming.ToUpper();
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
            var icons = new[]
            {
                new [] {"[Arc]", ""},
                new [] {"[Void]", ""},
                new [] {"[Solar]", ""},
                new [] {"[Kill]", ""},
                new [] {"[Headshot]", ""},
                new [] {"[Melee]", ""},
                new [] {"[Grenade]", ""},
                new [] {"[Auto Rifle]", ""},
                new [] {"[Pulse Rifle]", ""},
                new [] {"[Scout Rifle]", ""},
                new [] {"[Sniper Rifle]", ""},
                new [] {"[Fusion Rifle]", ""},
                new [] {"[Trace Rifle]", ""},
                new [] {"[Linear Fusion Rifle]", ""},
                new [] {"[Hand Cannon]", ""},
                new [] {"[Shotgun]", ""},
                new [] {"[SMG]", ""},
                new [] {"[Bow]", ""},
                new [] {"[Sidearm]", ""},
                new [] {"[Linear Fusion Rifle]", ""},
                new [] {"[Grenade Launcher]", ""},
                new [] {"[Rocket Launcher]", ""},
                new [] {"[Machine Gun]", ""},
                new [] {"[Sword]", ""},
                new [] {"", ""},
                new [] {"[Small Blocker]", ""},
                new [] {"[Medium Blocker]", ""},
                new [] {"[Large Blocker]", ""},
                new [] {"[Quest]", ""}
            };

            var incomming = (string)value;
            foreach (var icon in icons)
            {
                incomming = incomming.Replace(icon[0], icon[1]);
            }


            return incomming;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
