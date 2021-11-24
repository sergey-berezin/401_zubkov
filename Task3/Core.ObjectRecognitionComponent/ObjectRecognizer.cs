using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;
using Core.ObjectRecognitionComponent.DataStructures;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System;

namespace Core.ObjectRecognitionComponent {
    public class ObjectRecognizer {
        class IntermediateResult<T> {
            public string FilePeth { get; }
            public T Result { get; }

            public IntermediateResult(string filePeth, T result) {
                FilePeth = filePeth;
                Result = result;
            }
        }


        #region Attrs
        static public string ONNX_MODEL_PATH = "./Model/yolov4.onnx";

        private readonly ReadOnlyCollection<string> bitmapTypes = new ReadOnlyCollection<string>(new string[] { ".bmp", ".gif", ".jpg", ".png", ".tif" });
        private MLContext mlContext = null;
        private Microsoft.ML.Data.TransformerChain<OnnxTransformer> model = null;
        static readonly string[] classesNames = new string[] {
            "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
            "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
            "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
            "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse",
            "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
        };
        private IEnumerable<string> imagesPathList;
        private ConcurrentDictionary<string, ConcurrentBag<RecognitionResult>> categories;

        CancellationTokenSource cancellationSource;
        TransformBlock<string, IntermediateResult<Bitmap>> loadImage;
        TransformBlock<IntermediateResult<Bitmap>, IntermediateResult<YoloV4Prediction>> predictImage;
        BroadcastBlock<IntermediateResult<YoloV4Prediction>> broadcastPredict;
        BufferBlock<object> badImageBuffer;
        BufferBlock<object> processedImageBuffer;
        ActionBlock<IntermediateResult<YoloV4Prediction>> writeResult;

        #endregion

        #region Method
        public ObjectRecognizer(string modelPath) {
            categories = new ConcurrentDictionary<string, ConcurrentBag<RecognitionResult>> ();
            LoadMLModel(modelPath);
        }

        public int GetImagePath(string imageFolder) {
            CreatePipeline();
            var dir = new DirectoryInfo(imageFolder);
            imagesPathList = dir.GetFiles().Where(file => bitmapTypes.Contains(file.Extension)).Select(file => file.FullName);

            return imagesPathList.Count();
        }

        public async Task<Dictionary<string, List<RecognitionResult>>> RunObjectRecognizer(IProgress<int> updateProgress) {
            badImageBuffer.AsObservable().Subscribe(_ => updateProgress.Report(1));
            processedImageBuffer.AsObservable().Subscribe(_ => updateProgress.Report(1));

            foreach (var imageName in imagesPathList) {
                loadImage.Post(imageName);
            }

            loadImage.Complete();
            await writeResult.Completion;

            return categories.ToDictionary(
                dict => dict.Key,
                dict => dict.Value.ToList());
        }

        public void Cancel() => cancellationSource.Cancel();

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

            loadImage = new TransformBlock<string, IntermediateResult<Bitmap>>(imageName => {
                try {
                    return new IntermediateResult<Bitmap>(imageName, new Bitmap(Image.FromFile(imageName)));
                } catch {
                    return null;
                }
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 1
                }
            );

            badImageBuffer = new BufferBlock<object>(
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token
                }
            );

            predictImage = new TransformBlock<IntermediateResult<Bitmap>, IntermediateResult<YoloV4Prediction>> (image => {
                var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                return new IntermediateResult<YoloV4Prediction>(image.FilePeth, predictionEngine.Predict(new YoloV4BitmapData() { Image = image.Result }));
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 2
                }
            );

            broadcastPredict = new BroadcastBlock<IntermediateResult<YoloV4Prediction>>(data => data,
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 1
                }
            );

            processedImageBuffer = new BufferBlock<object>(
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token
                }
            );

            writeResult = new ActionBlock<IntermediateResult<YoloV4Prediction>>(predict => {
                var results = predict.Result.GetResults(classesNames, 0.3f, 0.7f);
                foreach (var result in results) {
                    categories.AddOrUpdate(result.Label, new ConcurrentBag<RecognitionResult>() { new RecognitionResult(predict.FilePeth, result) }, (key, val) => { 
                        val.Add(new RecognitionResult(predict.FilePeth, result)); 
                        return val; 
                    });
                }
            },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 1
                }
            );

            loadImage.LinkTo(predictImage, new DataflowLinkOptions { PropagateCompletion = true }, data => data != null);
            loadImage.LinkTo(badImageBuffer, new DataflowLinkOptions { PropagateCompletion = true }, data => data == null);
            predictImage.LinkTo(broadcastPredict, new DataflowLinkOptions { PropagateCompletion = true });
            broadcastPredict.LinkTo(writeResult, new DataflowLinkOptions { PropagateCompletion = true });
            broadcastPredict.LinkTo(processedImageBuffer, new DataflowLinkOptions { PropagateCompletion = true });
        }
        #endregion
    }
}
