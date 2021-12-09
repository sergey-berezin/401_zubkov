using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UI.Views {
    public partial class MainWindow: Window
    {
        public static MainWindow Instance { get; private set; }

        public ProgressBar RecognizeProgressBar { get; set; }
        public Carousel ImageCarousel { get; set; }

        public MainWindow() {
            Instance = this;
            InitializeComponent();
            RecognizeProgressBar = Instance.FindControl<ProgressBar>("RecognizeProgressBar");
            ImageCarousel = Instance.FindControl<Carousel>("ImageCarousel");
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
