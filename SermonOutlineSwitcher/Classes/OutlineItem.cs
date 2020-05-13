using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Shapes;

namespace SermonOutlineSwitcher.Classes
{
    public class OutlineItem
    {
        public enum Level { MainPoint = 0, SubPoint = 1, Point = 2}
        public int Order { get; set; }

        public bool Active { get; set; }
        public string Text { get; set; }
        private Level OutlineLevel { get; set; }

        public OutlineItem()
        {
            Text = string.Empty;
            OutlineLevel = Level.MainPoint;
        }

        public OutlineItem(string lineText, int order, bool active = false)
        {
            Text = lineText;
            OutlineLevel = Level.MainPoint;
            Order = order;
            Active = active;
        }

        public OutlineItem(string lineText, Level outlineLevel)
        {
            Text = lineText;
            OutlineLevel = outlineLevel;
        }
    }
}
