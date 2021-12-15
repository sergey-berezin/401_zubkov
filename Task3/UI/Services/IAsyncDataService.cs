using System;
using System.Threading.Tasks;
using DynamicData;
using UI.Models;


namespace UI.Services
{
    public interface IAsyncDataService
    {
        public int ObjectCount { get; }

        public IObservable<IChangeSet<RecognizedCroppedImage, int>> Connect();
        public void InitDataService(string imageFolder);
        public Task StartAction(IProgress<int>? updateProgress = default);
        public void StopAction();
        public Task RemoveActions(RecognizedCroppedImage? removeImage);
    }
}
