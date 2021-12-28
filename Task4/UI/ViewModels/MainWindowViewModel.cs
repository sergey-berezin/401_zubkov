using System;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReactiveUI.Fody.Helpers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
        [Reactive] public RecognizedImage? SelectedImage { get; set; }

        private readonly ReadOnlyObservableCollection<string> nameCategories;
        public ReadOnlyObservableCollection<string> NameCategories => nameCategories;

        private readonly ReadOnlyObservableCollection<RecognizedImage> selectedCategory;
        public ReadOnlyObservableCollection<RecognizedImage> SelectedCategory => selectedCategory;
        #endregion


        #region PublicMethods
        public MainWindowViewModel(IAsyncDataService dataService) {
            this.dataService = dataService;

            dataService.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .DistinctValues(obj => obj.Category)
                .Bind(out nameCategories)
                .Subscribe();

            var filter = this.WhenValueChanged(vm => vm.SelectedNameСategory)
                .Select(BuildFilter);

            dataService.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Filter(filter)
                .Bind(out selectedCategory)
                .Subscribe();


            try
            {
                dataService.GetAll();
            }
            catch
            {
                MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    "Внимание",
                    "Сервис временно недоступен."
                ).Show();
            }


            OpenFolderDialogCommand = ReactiveCommand.CreateFromTask(SelectFolderEndRunRecognition);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelRecognition, startedRecognition);
            RemoveCommand = ReactiveCommand.CreateFromTask(Remove, OpenFolderDialogCommand.CanExecute);

            ImageCarouselPrevious = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Previous());
            ImageCarouselNext = ReactiveCommand.Create(() => MainWindow.Instance.ImageCarousel.Next());
        }
        #endregion


        #region PrivateMethod
        private static Func<RecognizedImage, bool> BuildFilter(string? searchText) => obj => obj.Category.Equals(searchText);

        private async Task SelectFolderEndRunRecognition() {
            var result = await new OpenFolderDialog() {
                Title = "Выберите директорию"
            }.ShowAsync(MainWindow.Instance);

            if (result != null)
            {
                var imagesPath = Directory.GetFiles(result).Select(path => Path.GetFullPath(path)).ToArray();
                MainWindow.Instance.RecognizeProgressBar.Maximum = imagesPath.Length;

                var progress = new Progress<int>(UpdateProgress);
                startedRecognition.OnNext(true);

                dataService.Clear();

                foreach (var imagePath in imagesPath)
                {
                    try
                    {
                        await dataService.StartAction(imagePath, progress);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                            "Внимание",
                            "Сервис временно недоступен."
                        ).Show();
                        break;
                    }
                }

                startedRecognition.OnNext(false);
                MainWindow.Instance.RecognizeProgressBar.Value = 0;
            }
        }

        private async Task CancelRecognition()
        {
            dataService.StopAction();

            await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                "Внимание",
                "Процесс распознования отменен пользователем."
            ).Show();
        }

        private async Task Remove()
        {
            try
            {
                await dataService.RemoveActions(SelectedImage);
            }
            catch
            {
                await MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    "Внимание",
                    "Сервис временно недоступен."
                ).Show();
            }
        }

        private static void UpdateProgress(int step) => MainWindow.Instance.RecognizeProgressBar.Value += step;
        #endregion


        #region Commands Method
        public ReactiveCommand<Unit, Unit> OpenFolderDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

        public ReactiveCommand<Unit, Unit> ImageCarouselPrevious { get; }
        public ReactiveCommand<Unit, Unit> ImageCarouselNext { get; }
        #endregion
    }
}
