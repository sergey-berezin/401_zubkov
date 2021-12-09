using System;
using System.Collections.Generic;
using System.Text;
using Core.ObjectRecognitionComponent;
using Core.ObjectRecognitionComponent.DataStructures;

namespace UI.Services
{
    internal class ObjectRecognizerDataService : IAsyncDataService
    {
        private readonly ObjectRecognizer objectRecognizer = new(ObjectRecognizer.ONNX_MODEL_PATH);

        private bool initStatus;

        private int objectCount;
        public int ObjectCount => initStatus ? objectCount : throw new InvalidOperationException();


        public void InitDataService(string imageFolder)
        {
            objectRecognizer.SetRootImageFolder(imageFolder);
            objectCount = objectRecognizer.ImageCount;
            initStatus = true;
        }

        public async IAsyncEnumerable<ResultRecognition> GetResult(IProgress<int>? updateProgress = null)
        {
            await foreach (var obj in objectRecognizer.RunObjectRecognizer(updateProgress))
            {
                yield return obj;
            }
        }

        public void StopAction() => objectRecognizer.Cancel();
    }
}
