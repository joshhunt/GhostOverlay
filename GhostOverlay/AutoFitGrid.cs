using System;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GhostOverlay
{
    internal class AutoFitGrid : Panel
    {
        private double cellWidth, cellHeight, maxcellheight;

        private double rowCount, colCount;

        private Size LimitUnboundedSize(Size input)
        {
            if (double.IsInfinity(input.Height))
            {
                input.Height = maxcellheight * rowCount;
                cellHeight = maxcellheight;
            }

            return input;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double minimumWidth = 250; // does not take into account margins!

            colCount = Math.Floor(availableSize.Width / minimumWidth);
            rowCount = Math.Ceiling(Children.Count / colCount);

            cellWidth = availableSize.Width / colCount;
            cellHeight = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : availableSize.Height;

            foreach (var child in Children)
            {
                child.Measure(new Size(cellWidth, cellHeight));
                maxcellheight = child.DesiredSize.Height > maxcellheight ? child.DesiredSize.Height : maxcellheight;
            }

            return LimitUnboundedSize(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var count = 0;

            double x, y;
            double col, row;

            Debug.WriteLine("ArrangeOverride");

            foreach (var child in Children)
            {
                row = Math.Floor(count / colCount);
                col = count % colCount;

                x = col * cellWidth;
                y = row * cellHeight;

                Debug.WriteLine($"child {count} row: {row}, col: {col}, x: {x}, y: {y}");

                var anchorPoint = new Point(x, y);
                child.Arrange(new Rect(anchorPoint, child.DesiredSize));
                count++;
            }

            return finalSize;
        }
    }


    internal class SmartPanel : Panel
    {
        public static readonly DependencyProperty ItemMinWidthProperty =
            DependencyProperty.Register("ItemMinWidth", typeof(double), typeof(SmartPanel), new PropertyMetadata(300D));


        public static readonly DependencyProperty MaxColumnCountProperty =
            DependencyProperty.Register("MaxColumnCount", typeof(int), typeof(SmartPanel), new PropertyMetadata(10));

        private double[] _cellHeights;
        private double _cellWidth;
        private int _colCount;

        public double ItemMinWidth => (double) GetValue(ItemMinWidthProperty);

        public int MaxColumnCount
        {
            get => (int) GetValue(MaxColumnCountProperty);
            set => SetValue(MaxColumnCountProperty, value);
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            _colCount = (int) (availableSize.Width / ItemMinWidth);
            if (_colCount > MaxColumnCount) _colCount = MaxColumnCount;

            _cellWidth = (int) (availableSize.Width / _colCount);

            var rowCount = (int) Math.Ceiling((float) Children.Count / _colCount);

            _cellHeights = new double[rowCount];

            var y = 0;
            var x = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(_cellWidth, double.PositiveInfinity));
                _cellHeights[y] = Math.Max(_cellHeights[y], child.DesiredSize.Height);

                x++;
                if (x >= _colCount)
                {
                    x = 0;
                    y++;
                }
            }

            y = 0;
            x = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(_cellWidth, _cellHeights[y]));

                x++;
                if (x >= _colCount)
                {
                    x = 0;
                    y++;
                }
            }

            if (double.IsInfinity(availableSize.Height)) availableSize.Height = _cellHeights.Sum();

            return availableSize;
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            double x = 0;
            double y = 0;
            var colNum = 0;
            var rowNum = 0;
            foreach (var child in Children)
            {
                child.Arrange(new Rect(x, y, _cellWidth, _cellHeights[rowNum]));
                //child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
                x += _cellWidth;
                colNum++;

                if (colNum >= _colCount)
                {
                    x = 0;
                    y += _cellHeights[rowNum];
                    colNum = 0;
                    rowNum++;
                }
            }

            return finalSize;
        }
    }
}