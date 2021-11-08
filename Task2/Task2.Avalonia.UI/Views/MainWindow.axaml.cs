using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Task2.Avalonia.UI.Views {
    public partial class MainWindow: Window {
        public static MainWindow Instance { get; private set; }
        public ProgressBar recognizeProgressBar { get; set; }
        public Carousel imageCarousel { get; set; }

        public MainWindow() {
            Instance = this;
            InitializeComponent();
            recognizeProgressBar = Instance.FindControl<ProgressBar>("recognizeProgressBar");
            imageCarousel = Instance.FindControl<Carousel>("imageCarousel");
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
