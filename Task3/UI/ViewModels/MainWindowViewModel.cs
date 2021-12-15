using System;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Collections.ObjectModel;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using UI.Models;
using UI.Services;
using UI.Views;


namespace UI.ViewModels {
    public class MainWindowViewModel: ViewModelBase {
        #region DataAttrs

        private readonly IAsyncDataService dataService;

        #endregion


        #region ControlAttrs

        private readonly Subject<bool> startedRecognition = new();

        #endregion


        #region ViewAttrs

        [Reactive] public string Title { get; set; } = "Recognizer";
        [Reactive] public string SelectedNameСategory { get; set; } = "";
        [Reactive] public RecognizedCroppedImage? SelectedImage { get; set; }

        private readonly ReadOnlyObservableCollection<string> nameCategories;
        public ReadOnlyObservableCollection<string> NameCategories => nameCategories;

        private readonly ReadOnlyObservableCollection<RecognizedCroppedImage> selectedCategory;
        public ReadOnlyObservableCollection<RecognizedCroppedImage> SelectedCategory => selectedCategory;

        #endregion


        #region PublicMethods

        public MainWindowViewModel(IAsyncDataService dataService) {
            this.dataService = dataService;

            dataService.Connect()
                .DistinctValues(obj => obj.Label)
                .Bind(out nameCategories)
                .Subscribe();

            var filter = this.WhenValueChanged(vm => vm.SelectedNameСategory)
                .Select(BuildFilter);

            dataService.Connect()
                .Filter(filter)
                .Bind(out selectedCategory)
                .Subscribe();


            OpenFolderDialogCommand = ReactiveCommand.CreateFromTask(SelectFolderEndRunRecognition);
            CancelCommand = ReactiveCommand.Create(CancelRecognition, startedRecognition);
            RemoveCommand = ReactiveCommand.CreateFromTask(Remove, OpenFolderDialogCommand.CanExecute);

            ImageCarouselPrevious = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Previous());
            ImageCarouselNext = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Next());
        }

        #endregion


        #region PrivateMethod

        private static Func<RecognizedCroppedImage, bool> BuildFilter(string? searchText) => obj => obj.Label.Equals(searchText);

        private async Task SelectFolderEndRunRecognition() {
            var result = await new OpenFolderDialog() {
                Title = "Выберите директорию"
            }.ShowAsync(MainWindow.Instance);

            if (result != null) {
                dataService.InitDataService(result);
                MainWindow.Instance.RecognizeProgressBar.Maximum = dataService.ObjectCount;

                var progress = new Progress<int>(UpdateProgress);
                startedRecognition.OnNext(true);
                await dataService.StartAction(progress);
                startedRecognition.OnNext(false);
                MainWindow.Instance.RecognizeProgressBar.Value = 0;
            }
        }

        private static void UpdateProgress(int step) => MainWindow.Instance.RecognizeProgressBar.Value += step;

        private async Task Remove()
        {
            await dataService.RemoveActions(SelectedImage);
        }

        #endregion


        #region Commands Method

        public ReactiveCommand<Unit, Unit> OpenFolderDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

        public ReactiveCommand<Unit, Unit> ImageCarouselPrevious { get; }
        public ReactiveCommand<Unit, Unit> ImageCarouselNext { get; }


        private void CancelRecognition()
        {
            dataService.StopAction();

            MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Внимание",
                "Процесс распознования отменен пользователем."
            ).Show();
        }

        #endregion
    }
}
