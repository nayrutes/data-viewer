using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WorldCompanyDataViewer.Utils;

namespace WorldCompanyDataViewer.CustomControls
{
    public class PieChart : ItemsControl
    {
        /// <summary>
        /// Conveniance method to replace all PieChartEntry items in an ObservableCollection
        /// </summary>
        /// <typeparam name="T">type of collecion to display</typeparam>
        /// <param name="cutoff">count from when an entry is part of 'others'</param>
        /// <param name="pieChartEntries">collection of PieChartEntries to update</param>
        /// <param name="collection">Collection to display</param>
        /// <param name="propertyAccessorCount">count porperty</param>
        /// <param name="propertyAccessorLabel">label property</param>
        /// <param name="otherLabel">label for the cutoff part</param>
        /// <returns></returns>
        public static async Task SetPieChartAsync<T>(int cutoff, ObservableCollection<PieChart.PieChartEntry> pieChartEntries, IEnumerable<T> collection, Func<T, int> propertyAccessorCount, Func<T, string> propertyAccessorLabel, string otherLabel = "other")
        {
            ColorGenerator.ResetBrush();
            pieChartEntries.Clear();
            List<PieChart.PieChartEntry> toAdd = await Task.Run(() =>
            {
                List<PieChart.PieChartEntry> entries = new List<PieChart.PieChartEntry>();
                var mainEntries = collection.Where(x => propertyAccessorCount(x) > cutoff)
                    .Select(x => new PieChart.PieChartEntry
                    {
                        Value = propertyAccessorCount(x),
                        Label = propertyAccessorLabel(x),
                        Color = ColorGenerator.GetNextColor()
                    });
                entries.AddRange(mainEntries);

                int elseCount = collection.Where(x => propertyAccessorCount(x) <= cutoff).Sum(x => propertyAccessorCount(x));
                PieChart.PieChartEntry elseEntry = new PieChart.PieChartEntry()
                {
                    Value = elseCount,
                    Label = otherLabel,
                    Color = Colors.Gray,
                };
                entries.Add(elseEntry);
                return entries;
            });
            foreach (var item in toAdd)
            {
                pieChartEntries.Add(item);
            }

        }




        static PieChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PieChart), new FrameworkPropertyMetadata(typeof(PieChart)));
        }

        private static List<PieChartEntry> DefaultList = new List<PieChartEntry>() {
            new PieChartEntry
            {
                Value = 1,
                Color = Colors.DarkBlue,
                Label = "No yet set"
            }
        };

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (ItemsSource == null)
                return;
            List<PieChartEntry> entries = new();
            if(entries.Count == 0)
            {
                entries = DefaultList;
            }
            foreach (var item in ItemsSource)
            {
                if (item is not PieChartEntry)
                    continue;
                entries.Add((PieChartEntry)item);
            }
            int entriesCount = entries.Count;
            if (entries == null || entriesCount == 0)
                return;

            double total = 0;
            foreach (var entry in entries)
            {
                total += entry.Value;
            }

            double angle = 0;
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;
            double radius = Math.Min(centerX, centerY);


            for (int i = 0; i < entries.Count; i++)
            {
                PieChartEntry entry = entries[i];
                double sliceValue = entry.Value;
                double sliceAngle = (sliceValue / total) * 360;
                Brush sliceBrush = new SolidColorBrush(entry.Color);
                double borderSize = 2;
                if (entriesCount > 10)
                {
                    borderSize = (1 - (i / (double)entriesCount));
                }

                var borderpen = new Pen(new SolidColorBrush(Colors.Black), borderSize);
                drawingContext.DrawGeometry(sliceBrush, borderpen, CreatePieSliceGeometry(centerX, centerY, radius, angle, angle + sliceAngle));

                angle += sliceAngle;
            }
            angle = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PieChartEntry entry = entries[i];
                double sliceValue = entry.Value;
                double sliceAngle = (sliceValue / total) * 360;

                if (!String.IsNullOrWhiteSpace(entries[i].Label))
                {
                    FormattedText formattedText = new FormattedText($"({sliceValue}) {entries[i].Label}", CultureInfo.GetCultureInfo("en-us"),
                                  FlowDirection.LeftToRight,
                                  new Typeface(new FontFamily("Arial").ToString()),
                                  12, Brushes.Black, 1.25);

                    //double interpolatedValue = (Math.Sin(((angle + 270)%360) * (Math.PI / 180.0)) + 1);
                    //double textRadius = double.Lerp(0.5, 0.9, interpolatedValue);
                    //textRadius = double.Clamp(textRadius, 0.5, 0.9);
                    drawingContext.DrawText(formattedText, CalculateTextPos(centerX, centerY, radius, angle, sliceAngle, 0.8));
                }
                angle += sliceAngle;
            }
        }

        private StreamGeometry CreatePieSliceGeometry(double cx, double cy, double r, double startAngle, double endAngle)
        {
            if (endAngle == 360)
            {
                endAngle = 360 - 0.001;
            }
            Point startPoint = new Point(
                cx + r * Math.Cos(startAngle * Math.PI / 180),
                cy - r * Math.Sin(startAngle * Math.PI / 180));
            Point endPoint = new Point(
                cx + r * Math.Cos(endAngle * Math.PI / 180),
                cy - r * Math.Sin(endAngle * Math.PI / 180));

            bool isLargeArc = endAngle - startAngle > 180;

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new Point(cx, cy), true, true);
                context.LineTo(startPoint, true, true);
                context.ArcTo(endPoint, new Size(r, r), 0, isLargeArc, SweepDirection.Counterclockwise, true, true);
                context.LineTo(new Point(cx, cy), true, true);
            }

            return geometry;
        }

        private Point CalculateTextPos(double cx, double cy, double r, double startAngle, double sliceAngle, double distanceFromMiddleFactor)
        {
            double angleMiddle = (startAngle + (sliceAngle / 2));
            double distanceFromMiddle = r * Math.Clamp(distanceFromMiddleFactor, 0, 1);

            Point textPoint = new Point(
                cx + distanceFromMiddle * Math.Cos(angleMiddle * Math.PI / 180),
                cy - distanceFromMiddle * Math.Sin(angleMiddle * Math.PI / 180));
            return textPoint;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            InvalidateVisual();
        }


        public struct PieChartEntry
        {
            public double Value { get; set; }
            //public Brush Brush { get; set; }
            public Color Color { get; set; }
            public string Label { get; set; }
        }
    }

}