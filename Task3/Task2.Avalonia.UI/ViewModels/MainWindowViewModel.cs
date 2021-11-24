using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core.ObjectRecognitionComponent;
using Core.ObjectRecognitionComponent.DataStructures;
using ReactiveUI;
using Task2.Avalonia.UI.Views;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using ReactiveUI.Fody.Helpers;
using System.Drawing;
using System.Linq;

namespace Task2.Avalonia.UI.ViewModels {
    public class MainWindowViewModel: ViewModelBase {
        #region Attrs
        public ObjectRecognizer objectRecognizer { get; private set; }
        [Reactive] public List<KeyValuePair<string, List<Tuple<string, YoloV4Result>>>>? categories_list { get; private set; }
        [Reactive] public KeyValuePair<string, List<Tuple<string, YoloV4Result>>>? selectedCategoriesItem { get; set; }

        #endregion

        #region Events
        #endregion

        #region PublicMethods
        public MainWindowViewModel() {
            objectRecognizer = new ObjectRecognizer(ObjectRecognizer.ONNX_MODEL_PATH);
            categories_list = new List<KeyValuePair<string, List<Tuple<string, YoloV4Result>>>>();

            OpenFolderDialogCommand = ReactiveCommand.Create(SelectFolderEndRunRecognition);
            CancelCommand = ReactiveCommand.Create(() => objectRecognizer.Cancel());

            ImageCarouselPrevious = ReactiveCommand.Create(() => MainWindow.Instance.imageCarousel.Previous());
            ImageCarouselNext = ReactiveCommand.Create(() => MainWindow.Instance.imageCarousel.Next());
        }

        #endregion

        #region PrivateMethod
        async void SelectFolderEndRunRecognition() {
            var result = await new OpenFolderDialog() {
                Title = "Выберите директорию"
            }.ShowAsync(MainWindow.Instance);

            if (result != null) {
                categories_list = null;

                var cntImages = objectRecognizer.GetImagePath(result);

                MainWindow.Instance.recognizeProgressBar.Value = 0;
                MainWindow.Instance.recognizeProgressBar.Maximum = cntImages;

                var progress = new Progress<int>(UpdateProgress);
                try {
                    var categories_dict = await objectRecognizer.RunObjectRecognizer(progress);
                    categories_list = categories_dict.ToList();
                } catch (TaskCanceledException) {
                    var msgbox = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                        "Внимание",
                        "Процесс распознования отменен пользователем.");
                    await msgbox.Show();
                    MainWindow.Instance.recognizeProgressBar.Value = 0;
                }
            }
        }

        private void UpdateProgress(int step) => MainWindow.Instance.recognizeProgressBar.Value += step;
        #endregion

        #region Commands Method
        public ReactiveCommand<Unit, Unit> OpenFolderDialogCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public ReactiveCommand<Unit, Unit> ImageCarouselPrevious { get; }
        public ReactiveCommand<Unit, Unit> ImageCarouselNext { get; }

        #endregion

    }
}
