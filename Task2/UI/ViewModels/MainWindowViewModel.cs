using System;
using System.Reactive;
using Avalonia.Controls;
using Core.ObjectRecognitionComponent;
using Core.ObjectRecognitionComponent.DataStructures;
using ReactiveUI;
using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using UI.Services;
using UI.Views;


namespace UI.ViewModels {
    public class MainWindowViewModel: ViewModelBase {
        #region DataAttrs

        private readonly IAsyncDataService dataService;

        #endregion


        #region ControlAttrs

        private readonly Subject<bool> startedRecognition = new();
        private readonly SourceCache<ResultRecognition, string> recognizedObjectsCache = new(obj => obj.ImagePath);

        #endregion


        #region ViewAttrs

        [Reactive] public string Title { get; set; } = "Recognizer";
        [Reactive] public string SelectedNameСategory { get; set; } = "";

        private readonly ReadOnlyObservableCollection<string> nameCategories;
        public ReadOnlyObservableCollection<string> NameCategories => nameCategories;

        private readonly ReadOnlyObservableCollection<ResultRecognition> selectedCategory;
        public ReadOnlyObservableCollection<ResultRecognition> SelectedCategory => selectedCategory;

        #endregion


        #region PublicMethods

        public MainWindowViewModel() {
            dataService = new ObjectRecognizerDataService();

            recognizedObjectsCache.Connect()
                .DistinctValues(obj => obj.Label)
                .Bind(out nameCategories)
                .Subscribe();

            var filter = this.WhenValueChanged(vm => vm.SelectedNameСategory)
                .Select(BuildFilter);

            recognizedObjectsCache.Connect()
                .Filter(filter)
                .Bind(out selectedCategory)
                .Subscribe();


            OpenFolderDialogCommand = ReactiveCommand.Create(SelectFolderEndRunRecognition);
            CancelCommand = ReactiveCommand.Create(CancelRecognition, startedRecognition);
            

            ImageCarouselPrevious = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Previous());
            ImageCarouselNext = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Next());
        }

        #endregion


        #region PrivateMethod

        private static Func<ResultRecognition, bool> BuildFilter(string? searchText) => obj => obj.Label.Equals(searchText);

        private async Task SelectFolderEndRunRecognition() {
            var result = await new OpenFolderDialog() {
                Title = "Выберите директорию"
            }.ShowAsync(MainWindow.Instance);

            if (result != null) {
                recognizedObjectsCache.Clear();

                dataService.InitDataService(result);
                MainWindow.Instance.RecognizeProgressBar.Maximum = dataService.ObjectCount;

                var progress = new Progress<int>(UpdateProgress);
                startedRecognition.OnNext(true);

                await foreach (var recognizedObject in dataService.GetResult(progress))
                {
                    recognizedObjectsCache.AddOrUpdate(recognizedObject);
                }

                startedRecognition.OnNext(false);
                MainWindow.Instance.RecognizeProgressBar.Value = 0;
            }
        }

        private static void UpdateProgress(int step) => MainWindow.Instance.RecognizeProgressBar.Value += step;

        #endregion


        #region Commands Method

        public ReactiveCommand<Unit, Task> OpenFolderDialogCommand { get; }
        public ReactiveCommand<Unit, Task> CancelCommand { get; }

        public ReactiveCommand<Unit, Unit> ImageCarouselPrevious { get; }
        public ReactiveCommand<Unit, Unit> ImageCarouselNext { get; }


        private async Task CancelRecognition()
        {
            dataService.StopAction();

            await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Внимание",
                "Процесс распознования отменен пользователем."
            ).Show(MainWindow.Instance);

        }

        #endregion
    }
}
