using System;
using System.Globalization;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
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
                numbers = paramString.Split(new [] {'|'}).Select(System.Convert.ToDouble).ToArray();
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
            var incomingNumber = (int)value;
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
            var icons = new[]
            {
                ("[Grenade Launcher]", "", 5924951L),
                ("[Sniper Rifle]", "", 27869698L),
                ("[Grenade]", "", 45245118L),
                ("[Hand Cannon]", "", 53304862L),
                ("[Void]", "", 54906502L),
                ("[Auto Rifle]", "", 60057218L),
                ("[Fusion Rifle]", "", 120861495L),
                ("[Kill]", "", 130479533L),
                ("[SMG]", "", 218704521L),
                ("[Arc]", "", 232832120L),
                ("[Headshot]", "", 234754498L),
                ("[Scout Rifle]", "", 236969823L),
                ("[Rocket Launcher]", "", 238063032L),
                ("[Shotgun]", "", 258599004L),
                ("[Hunter: Arcstrider Super]", "", 269520342L),
                ("[Small Blocker]", "", 276438067L),
                ("[Sidearm]", "", 299893109L),
                ("[Solar]", "", 312507792L),
                ("[Melee]", "", 314405660L),
                ("[Pulse Rifle]", "", 420092262L),
                ("[Sword]", "", 989767424L),
                ("[Titan: Sentinel Super]", "", 1043633269L),
                ("[Bow]", "", 1093443739L),
                ("[Warlock: Dawnblade Super]", "", 1363382181L),
                ("[Trace Rifle]", "", 1375652735L),
                ("[Linear Fusion Rifle]", "", 1448686440L),
                ("[Machine Gun]", "", 1452824294L),
                ("[Hunter: Gunslinger Super]", "", 1633845729L),
                ("[Warlock: Voidwalker Super]", "", 1733112051L),
                ("[Large Blocker]", "", 2031240843L),
                ("", "", 2258101260L),
                ("[Hunter: Nightstalker Super]", "", 2904388000L),
                ("[Titan: Sunbreaker Super]", "", 2905697046L),
                ("[Titan: Striker Super]", "", 2975056954L),
                ("[Medium Blocker]", "", 3792840449L),
                ("[Quest]", "", 3915460773L),
                ("", "", 4231452845L)
            };

            var incomming = (string)value;
            foreach (var icon in icons)
            {
                incomming = incomming.Replace(icon.Item1, icon.Item2);
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
