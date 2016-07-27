using System.Windows;
using System.Windows.Controls;

namespace Tanji.Controls
{
    public class LabelBox : TextBox
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(LabelBox), new UIPropertyMetadata("Title"));

        public static readonly DependencyProperty RelativeWidthProperty = DependencyProperty.Register(nameof(RelativeWidth),
            typeof(double), typeof(LabelBox), new UIPropertyMetadata(100.0));

        public string Title
        {
            get { return GetValue(TitleProperty) as string; }
            set { SetValue(TitleProperty, value); }
        }
        public double RelativeWidth
        {
            get { return (double)GetValue(RelativeWidthProperty); }
            set { SetValue(RelativeWidthProperty, value); }
        }
    }
}