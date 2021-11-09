using ObjectRecognitionComponent;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Task1 {
    class Program {
        static string ONNX_MODEL_PATH = "./Model/yolov4.onnx";

        static async Task Main(string[] args) {
            var test = new ObjectRecognizer(ONNX_MODEL_PATH);

            Console.WriteLine("Enter the path to the directory with images.");

            var cts = new CancellationTokenSource();
            var cancelTask = Task.Factory.StartNew(() => {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
                test.Cancel();
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            string directoryPath = Console.ReadLine();

            Console.WriteLine("Press ESC to stop process");

            int cntImage = test.GetImagePath(directoryPath);

            try {
                var categories = new Dictionary<string, List<Tuple<string, YOLOv4MLNet.DataStructures.YoloV4Result>>>();
                using (var progressBar = new ProgressBar(cntImage, "Image processing...")) {
                    categories = await test.RunObjectRecognizer(progressBar.AsProgress<int>());
                }
                foreach (var (key, val) in categories) {
                    Console.WriteLine($"{key}: {val.Count};");
                }

                cts.Cancel();
            } catch (TaskCanceledException) {
                Console.WriteLine("The process was stopped by the user.");
            }
        }
    }
}
