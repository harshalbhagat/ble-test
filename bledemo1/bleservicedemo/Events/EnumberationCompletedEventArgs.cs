using Windows.UI.Xaml.Controls;

namespace bleservicedemo
{
    public class EnumberationCompletedEventArgs
    {
        public Button Button { set; get; }
        public ProgressRing ProgressRing { get; set; }
        public TextBlock TextBlock { get; set; }
    }
}