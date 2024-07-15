using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WorldCompanyDataViewer.Utils
{
    public static class ColorGenerator
    {
        private static readonly List<Color> predefinedColors = new List<Color>
        {
            Colors.DarkBlue,
            Colors.DarkRed,
            Colors.DarkGreen,
            Colors.DarkOrange,
            Colors.DarkViolet,
            Colors.DarkCyan,
            Colors.DarkGoldenrod,
            Colors.DarkMagenta,
            Colors.DarkSeaGreen,
            Colors.DarkTurquoise,
            Colors.DarkSalmon,
            Colors.Blue,
            Colors.Red,
            Colors.Green,
            Colors.Orange,
            Colors.Purple,
            Colors.Teal,
            Colors.Brown,
            Colors.Magenta,
            Colors.LimeGreen,
            Colors.Cyan,
            Colors.Yellow,
        };

        private static int currentIndex = 0;

        public static SolidColorBrush GetNextBrush()
        {
            Color color = predefinedColors[currentIndex];
            currentIndex = (currentIndex + 1) % predefinedColors.Count;
            return new SolidColorBrush(color);
        }

        internal static void ResetBrush()
        {
            currentIndex = 0;
        }
    }
}
