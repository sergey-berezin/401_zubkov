using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using UI.Infrastructure;
using UI.Models;


namespace UI.Services
{
    internal class ObjectRecognizerWebService : IAsyncDataService
    {
        private static HttpClient client = new();
        private CancellationTokenSource cancellationTokenSourse = new();

        private readonly SourceCache<RecognizedImage, int> recognizedObjectsCache = new(obj => obj.Id);
        public IObservable<IChangeSet<RecognizedImage, int>> Connect() => recognizedObjectsCache.Connect();


        public async Task GetAll()
        {
            var response = await client.GetFromJsonAsync<RecognizedImage[]>("http://localhost:37396/api/recognizedimage/");

            recognizedObjectsCache.Edit(innerCache =>
            {
                foreach (var image in response)
                {
                    innerCache.AddOrUpdate(image);
                }
            });
        }

        public void Clear()
        {
            recognizedObjectsCache.Clear();
        }

        public async Task StartAction(string imagePath, IProgress<int>? updateProgress = null)
        {
            cancellationTokenSourse = new CancellationTokenSource();

            var imageData = File.ReadAllBytes(imagePath);
            var jsonImageData = new StringContent(JsonSerializer.Serialize(imageData), Encoding.Default, "application/json");

            var response = await client.PostAsync("http://localhost:37396/api/recognizedimage/recognize", jsonImageData, cancellationTokenSourse.Token);

            if (response.IsSuccessStatusCode) {

                var resultsRecognition = await response.Content.ReadFromJsonAsync<RecognizedImage[]>();

                foreach (var resultRecognition in resultsRecognition)
                {
                    recognizedObjectsCache.AddOrUpdate(resultRecognition);
                }

                updateProgress?.Report(1);
            }
        }

        public void StopAction()
        {
            cancellationTokenSourse.Cancel();
        }

        public async Task RemoveActions(RecognizedImage? removeImage)
        {
            if (removeImage != null)
            {
                await client.DeleteAsync($"http://localhost:37396/api/recognizedimage/remove/{removeImage.Id}");
                recognizedObjectsCache.RemoveKey(removeImage.Id);
            }
        }
    }
}
