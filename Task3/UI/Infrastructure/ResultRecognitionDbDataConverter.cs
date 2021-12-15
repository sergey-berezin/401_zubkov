using Core.DataAccessLayer.Entities;
using UI.Models;


namespace UI.Infrastructure
{
    internal class ResultRecognitionDbDataConverter
    {
        public RecognizedImage Convert(RecognizedCroppedImage recObj, Category category) => new ()
            {
                BBox = recObj.BBox,
                Category = category,
                SerializedImage = recObj.ImageByteData
            };

        public RecognizedCroppedImage ConvertBack(RecognizedImage dbObj) => new (dbObj);
    }
}
