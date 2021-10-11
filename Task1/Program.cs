using ObjectRecognitionComponent;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Task1 {
    class Program {
        static string ONNX_MODEL_PATH = "./Model/yolov4.onnx";

        static async Task Main(string[] args) {
            var test = new ObjectRecognizer(ONNX_MODEL_PATH);

            Console.WriteLine("Enter the path to the directory with images.");

            var cts = new CancellationTokenSource();
            var cancelTask = Task.Run(() => {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
                test.Cancel();
            }, cts.Token);

            string directoryPath = Console.ReadLine();

            Console.WriteLine("Press ESC to stop process");

            try {
                var categories = await test.RunObjectRecognizer(directoryPath);

                foreach (var (key, val) in categories) {
                    Console.WriteLine($"{key}: {val};");
                }

                cts.Cancel();
            } catch (TaskCanceledException) {
                Console.WriteLine("The process was stopped by the user.");
            }
        }
    }
}
