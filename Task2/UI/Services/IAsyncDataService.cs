using System;
using System.Collections.Generic;
using Core.ObjectRecognitionComponent.DataStructures;

namespace UI.Services
{
    internal interface IAsyncDataService
    {
        public int ObjectCount { get; }

        public void InitDataService(string imageFolder);
        public IAsyncEnumerable<ResultRecognition> GetResult(IProgress<int>? updateProgress);
        public void StopAction();
    }
}