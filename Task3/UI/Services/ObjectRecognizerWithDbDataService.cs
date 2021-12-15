using System;
using System.Linq;
using System.Threading.Tasks;
using Core.DataAccessLayer.Context;
using Core.DataAccessLayer.Entities;
using Core.ObjectRecognitionComponent;
using DynamicData;
using Microsoft.EntityFrameworkCore;
using UI.Infrastructure;
using UI.Models;


namespace UI.Services
{
    internal class ObjectRecognizerWithDbDataService : IAsyncDataService
    {
        #region Utils

        private readonly string errorInitMessage =
            "Data Service has not been initialized. Before use, you need to call the InitDataService method";
        private readonly ResultRecognitionDbDataConverter converter = new();
        private bool initStatus;

        #endregion

        #region Database

        private readonly RecognizedImagesDb dbContext;
        private readonly DbSet<Category> categorySet;
        private readonly DbSet<RecognizedImage> recognizedImageSet;

        #endregion


        private readonly ObjectRecognizer objectRecognizer = new(ObjectRecognizer.ONNX_MODEL_PATH);
        private readonly SourceCache<RecognizedCroppedImage, int> recognizedObjectsCache = new(obj => obj.Id);

        private int objectCount;
        public int ObjectCount => initStatus ? objectCount : throw new InvalidOperationException(errorInitMessage);


        public ObjectRecognizerWithDbDataService(RecognizedImagesDb dbContext)
        {
            this.dbContext = dbContext;
            categorySet = dbContext.Set<Category>();
            recognizedImageSet = dbContext.Set<RecognizedImage>();
            GetAllData();
        }

        private void GetAllData()
        {
            recognizedObjectsCache.Edit(innerCache =>
            {
                foreach (var image in recognizedImageSet.Include(item => item.Category).ToArray())
                {
                    innerCache.AddOrUpdate(converter.ConvertBack(image));
                }
            });
        }

        public IObservable<IChangeSet<RecognizedCroppedImage, int>> Connect() => recognizedObjectsCache.Connect();

        public void InitDataService(string imageFolder)
        {
            objectRecognizer.SetRootImageFolder(imageFolder);
            objectCount = objectRecognizer.ImageCount;
            initStatus = true;
        }

        public async Task StartAction(IProgress<int>? updateProgress = null)
        {
            if (!initStatus)
            {
                throw new InvalidOperationException(errorInitMessage);
            }

            recognizedObjectsCache.Clear();

            await foreach (var rawRecognizedImage in objectRecognizer.RunObjectRecognizer(updateProgress))
            {
                var recognizedImage = new RecognizedCroppedImage(rawRecognizedImage);
                recognizedObjectsCache.AddOrUpdate(recognizedImage);

                var categoryEntity = await categorySet
                    .SingleOrDefaultAsync(item => item.CategoryName == recognizedImage.Label)
                    .ConfigureAwait(false);

                if (categoryEntity is null)
                {
                    categoryEntity = new Category() { CategoryName = recognizedImage.Label };
                    dbContext.Entry(categoryEntity).State = EntityState.Added;
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }

                var candidatesRecognizedImageEntity = await recognizedImageSet
                    .Include(item => item.Category)
                    .Where(item => item.Category.CategoryName == categoryEntity.CategoryName && item.BBox == recognizedImage.BBox)
                    .ToArrayAsync().ConfigureAwait(false);

                if (candidatesRecognizedImageEntity.Length == 0)
                {
                    var recognizedImageEntity = converter.Convert(recognizedImage, categoryEntity);
                    dbContext.Entry(recognizedImageEntity).State = EntityState.Added;
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                {
                    bool inDb = false;

                    foreach (var candidateRecognizedImageEntity in candidatesRecognizedImageEntity)
                    {
                        if (candidateRecognizedImageEntity.SerializedImage.SequenceEqual(recognizedImage.ImageByteData))
                        {
                            inDb = true;
                            break;
                        }
                    }

                    if (!inDb)
                    {
                        var recognizedImageEntity = converter.Convert(recognizedImage, categoryEntity);
                        dbContext.Entry(recognizedImageEntity).State = EntityState.Added;
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        public void StopAction()
        {
            if (initStatus)
            {
                objectRecognizer.Cancel();
            }
            else
            {
                throw new InvalidOperationException(errorInitMessage);
            }
        }

        public async Task RemoveActions(RecognizedCroppedImage? removeImage)
        {
            if (removeImage is null)
            {
                return;
            }

            var candidatesRecognizedImageEntity = await recognizedImageSet
                .Include(item => item.Category)
                .Where(item => item.Category.CategoryName == removeImage.Label && item.BBox == removeImage.BBox)
                .ToArrayAsync().ConfigureAwait(false);

            foreach (var candidateRecognizedImageEntity in candidatesRecognizedImageEntity)
            {
                if (candidateRecognizedImageEntity.SerializedImage.SequenceEqual(removeImage.ImageByteData))
                {
                    dbContext.Remove(candidateRecognizedImageEntity);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            var categoryEntity = await categorySet
                .SingleOrDefaultAsync(item => item.CategoryName == removeImage.Label)
                .ConfigureAwait(false);

            if (categoryEntity.Images.Count == 0)
            {
                dbContext.Remove(categoryEntity);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            recognizedObjectsCache.Remove(removeImage);
        }
    }
}
