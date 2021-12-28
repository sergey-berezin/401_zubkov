using System;
using System.Threading.Tasks;
using DynamicData;
using UI.Models;


namespace UI.Services
{
    public interface IAsyncDataService
    {
        public IObservable<IChangeSet<RecognizedImage, int>> Connect();
        public Task GetAll();
        public void Clear();
        public Task StartAction(string imagePath, IProgress<int>? updateProgress = default);
        public void StopAction();
        public Task RemoveActions(RecognizedImage? removeImage);
    }
}
