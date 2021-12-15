using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;
using Core.ObjectRecognitionComponent.DataStructures;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System;


namespace Core.ObjectRecognitionComponent {
    public class ObjectRecognizer {
        #region ConfigurationAttrs

        public static string ONNX_MODEL_PATH = @"Model\yolov4.onnx";

        private static readonly string[] bitmapTypes = {".bmp", ".gif", ".jpg", ".png", ".tif" };
        private static readonly string[] ClassesNames = {
            "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", 
            "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", 
            "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite",
            "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", 
            "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", 
            "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", 
            "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", 
            "teddy bear", "hair drier", "toothbrush"
        };

        #endregion

        #region MLAttrs

        private MLContext mlContext;
        private Microsoft.ML.Data.TransformerChain<OnnxTransformer> model;

        #endregion

        #region DataAttrs

        private IEnumerable<string> imagesPathList;

        public int ImageCount => imagesPathList.Count();

        #endregion

        #region PipelineAttrs

        private CancellationTokenSource cancellationSource;
        private TransformBlock<string, Tuple<string, Bitmap>> loadImage;
        private TransformBlock<Tuple<string, Bitmap>, Tuple<string, YoloV4Prediction>> predictImage;
        private BroadcastBlock<Tuple<string, YoloV4Prediction>> broadcastPredict;
        private BufferBlock<object> badImageBuffer;
        private BufferBlock<object> processedImageBuffer;
        private TransformManyBlock<Tuple<string, YoloV4Prediction>, ResultRecognition> transformPredict;
        private BufferBlock<ResultRecognition> resultBuffer;

        #endregion

        #region PublicMethod

        public ObjectRecognizer(string modelPath) {
            LoadMLModel(modelPath);
        }

        public void SetRootImageFolder(string imageFolder) {
            CreatePipeline();

            var dir = new DirectoryInfo(imageFolder);
            imagesPathList = dir.GetFiles().Where(file => bitmapTypes.Contains(file.Extension)).Select(file => file.FullName);
        }

        public async IAsyncEnumerable<ResultRecognition> RunObjectRecognizer(IProgress<int> updateProgress = null) {
            badImageBuffer.AsObservable().Subscribe(_ => updateProgress?.Report(1));
            processedImageBuffer.AsObservable().Subscribe(_ => updateProgress?.Report(1));

            foreach (var imageName in imagesPathList) {
                loadImage.Post(imageName);
            }

            loadImage.Complete();

            await foreach (var result in resultBuffer.ReceiveAllAsync()) {
                yield return result;
            }
        }

        public void Cancel() => cancellationSource?.Cancel();

        #endregion

        #region PrivateMethod

        private void LoadMLModel(string modelPath) {
            mlContext = new MLContext();
            var pipeline = mlContext.Transforms.ResizeImages(
                    inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
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

            loadImage = new TransformBlock<string, Tuple<string, Bitmap>>(imageName => {
                    try {
                        return new Tuple<string, Bitmap>(imageName, new Bitmap(Image.FromFile(imageName)));
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

            predictImage = new TransformBlock<Tuple<string, Bitmap>, Tuple<string, YoloV4Prediction>>(image => {
                    var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                    return new Tuple<string, YoloV4Prediction>(image.Item1, predictionEngine.Predict(new YoloV4BitmapData() { Image = image.Item2 }));
                },
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 3
                }
            );

            broadcastPredict = new BroadcastBlock<Tuple<string, YoloV4Prediction>>(data => data,
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

            transformPredict = new TransformManyBlock<Tuple<string, YoloV4Prediction>, ResultRecognition>(predict => GetResultPredict(predict),
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = 2
                }
            );

            resultBuffer = new BufferBlock<ResultRecognition>(
                new ExecutionDataflowBlockOptions {
                    CancellationToken = cancellationSource.Token
                }
            );

            loadImage.LinkTo(predictImage, new DataflowLinkOptions { PropagateCompletion = true }, data => data != null);
            loadImage.LinkTo(badImageBuffer, new DataflowLinkOptions { PropagateCompletion = true }, data => data == null);
            predictImage.LinkTo(broadcastPredict, new DataflowLinkOptions { PropagateCompletion = true });
            broadcastPredict.LinkTo(processedImageBuffer, new DataflowLinkOptions { PropagateCompletion = true });
            broadcastPredict.LinkTo(transformPredict, new DataflowLinkOptions { PropagateCompletion = true });
            transformPredict.LinkTo(resultBuffer, new DataflowLinkOptions { PropagateCompletion = true });
        }

        private IEnumerable<ResultRecognition> GetResultPredict(Tuple<string, YoloV4Prediction> predict) {
            var results = predict.Item2.GetResults(ClassesNames, 0.3f, 0.7f);
            foreach (var result in results) {
                yield return new ResultRecognition(predict.Item1, result);
            }
        }

        #endregion
    }
}
