using Windows.UI.Xaml.Controls;

namespace bledemo1
{
    public class EnumberationCompletedEventArgs
    {
        public Button Button { set; get; }
        public ProgressRing ProgressRing { get; set; }
        public TextBlock TextBlock { get; set; }
    }
}