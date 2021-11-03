using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;
using ObjectRecognitionComponent.DataStructures;
using ShellProgressBar;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using YOLOv4MLNet.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;


namespace ObjectRecognitionComponent {
    public class ObjectRecognizer {
        #region Attrs
        private readonly ReadOnlyCollection<string> bitmapTypes = new ReadOnlyCollection<string>(new string[] { ".bmp", ".gif", ".jpg", ".png", ".tif" });
        private MLContext mlContext = null;
        private Microsoft.ML.Data.TransformerChain<OnnxTransformer> model = null;
        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };
        private ConcurrentDictionary<string, int> categories;
        private ProgressBar progressBar;

        CancellationTokenSource cancellationSource;
        TransformBlock<string, Bitmap> loadImage;
        TransformBlock<Bitmap, YoloV4Prediction> predictImage;
        ActionBlock<YoloV4Prediction> writeResult;
        #endregion

        #region Method
        public ObjectRecognizer(string modelPath) {
            categories = new ConcurrentDictionary<string, int>();
            LoadMLModel(modelPath);
            CreatePipeline();
        }

        public async Task<Dictionary<string, int>> RunObjectRecognizer(string imageFolder) {
            var dir = new DirectoryInfo(imageFolder);
            var imagesList = dir.GetFiles().Where(file => bitmapTypes.Contains(file.Extension)).Select(file => file.FullName);

            using (progressBar = new ProgressBar(imagesList.Count(), "Image processing...")) {
                foreach (var imageName in imagesList) {
                    loadImage.Post(imageName);
                }

                loadImage.Complete();
                await writeResult.Completion;
            }
            return categories.ToDictionary(
                dict => dict.Key,
                dict => dict.Value);
        }

        public void Cancel() {
            cancellationSource.Cancel();
        }

        private void LoadMLModel(string modelPath) {
            mlContext = new MLContext();
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>() {
                        { "input_1:0", new[] { 1, 416, 416, 3 } },
                        { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[] {
                        "input_1:0"
                    },
                    outputColumnNames: new[] {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: modelPath,
                    recursionLimit: 100));
            model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));
        }

        private void CreatePipeline() {
            cancellationSource = new CancellationTokenSource();

            loadImage = new TransformBlock<string, Bitmap>(imageName => {
                try {
                    return new Bitmap(Image.FromFile(imageName));
                } catch {
                    return null;
                }
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 1
                }
            );

            predictImage = new TransformBlock<Bitmap, YoloV4Prediction>(image => {
                if (image != null) {
                    var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                    return predictionEngine.Predict(new YoloV4BitmapData() { Image = image });
                } else {
                    return null;
                }
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 2
                }
            );

            writeResult = new ActionBlock<YoloV4Prediction>(predict => {
                if (predict != null) {
                    var results = predict.GetResults(classesNames, 0.3f, 0.7f);
                    foreach (var result in results) {
                        categories.AddOrUpdate(result.Label, 1, (key, val) => val + 1);
                    }
                }
                progressBar.Tick();
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 1
                }
            );

            loadImage.LinkTo(predictImage, new DataflowLinkOptions { PropagateCompletion = true });
            predictImage.LinkTo(writeResult, new DataflowLinkOptions { PropagateCompletion = true });
        }
        #endregion
    }
}
