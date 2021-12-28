using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Core.ObjectRecognitionComponent.DataStructures;
using Core.WebApi.Models.Entities;


namespace Core.WebApi.Models
{
    public class RecognizedImage
    {
        public int Id { get; }
        public byte[] ImageByteData { get; }
        public string BBox { get; }
        public string Label { get; }


        public RecognizedImage(byte[] rawImageData, YoloV4Result predict)
        {
            Id = -1;

            var x1 = Convert.ToInt32(predict.BBox[0]);
            var y1 = Convert.ToInt32(predict.BBox[1]);
            var x2 = Convert.ToInt32(predict.BBox[2]);
            var y2 = Convert.ToInt32(predict.BBox[3]);

            Label = predict.Label;
            BBox = string.Join(";", new[] { x1, y1, x2, y2 }.Select(bboxCoord => Convert.ToString(bboxCoord)));

            ImageByteData = ImageExtensions.ImageFromByteArray(rawImageData)
                .CropImage(new Rectangle(x1, y1, x2 - x1, y2 - y1))
                .ToByteArray(ImageFormat.Bmp);
        }

        public RecognizedImage(RecognizedImageEntity entity)
        {
            Id = entity.Id;
            Label = entity.CategoryEntity.CategoryName;
            BBox = entity.BBox;
            ImageByteData = entity.SerializedImage;
        }
    }
}
