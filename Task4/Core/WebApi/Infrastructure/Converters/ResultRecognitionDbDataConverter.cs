using Core.WebApi.Models;
using Core.WebApi.Models.Entities;

namespace Core.WebApi.Infrastructure.Converters
{
    internal static class ResultRecognitionDbDataConverter
    {
        public static RecognizedImageEntity Convert(RecognizedImage recObj, CategoryEntity categoryEntity) => new () {
            BBox = recObj.BBox,
            CategoryEntity = categoryEntity,
            SerializedImage = recObj.ImageByteData
        };

        //public static RecognizedImage ConvertBack(RecognizedImageEntity dbObj) => new (dbObj);
    }
}
