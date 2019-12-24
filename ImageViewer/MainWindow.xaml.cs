using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageViewer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CommonCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CanvasViewer.OpenExecuted();
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CanvasViewer.SaveExecuted();
        }

        private void ZoomFitClick(object sender, RoutedEventArgs e)
        {
            CanvasViewer.ZoomFit();
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {

        }

        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
