using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldCompanyDataViewer.CustomControls
{
    public class PieChart : ItemsControl
    {
        static PieChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PieChart), new FrameworkPropertyMetadata(typeof(PieChart)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (ItemsSource == null)
                return;
            List<PieChartEntry> entries = new();
            foreach (var item in ItemsSource)
            {
                if (item is not PieChartEntry)
                    continue;
                entries.Add((PieChartEntry)item);
            }
            if (entries == null || entries.Count == 0)
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
                Brush sliceBrush = entry.Brush;

                drawingContext.DrawGeometry(sliceBrush, null, CreatePieSliceGeometry(centerX, centerY, radius, angle, angle + sliceAngle));

                if (!String.IsNullOrWhiteSpace(entries[i].Label))
                {
                    FormattedText formattedText = new FormattedText($"{entries[i].Label}", CultureInfo.GetCultureInfo("en-us"),
                                  FlowDirection.LeftToRight,
                                  new Typeface(new FontFamily("Arial").ToString()),
                                  12, Brushes.Black, 1.25);
                    drawingContext.DrawText(formattedText, CalculateTextPos(centerX, centerY, radius, angle, sliceAngle));
                }
                angle += sliceAngle;
            }

            angle = 0;
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

        private Point CalculateTextPos(double cx, double cy, double r, double startAngle, double sliceAngle, double distanceFromMiddleFactor = 0.7)
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
            public Brush Brush { get; set; }
            public string Label { get; set; }
        }
    }

}