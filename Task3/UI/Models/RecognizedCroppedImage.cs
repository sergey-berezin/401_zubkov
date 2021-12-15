using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Core.DataAccessLayer.Entities;
using Core.ObjectRecognitionComponent.DataStructures;


namespace UI.Models
{
    public class RecognizedCroppedImage
    {
        public int Id { get; }
        public byte[] ImageByteData { get; }
        public string BBox { get; }
        public string Label { get; }


        public RecognizedCroppedImage(ResultRecognition rawObj)
        {
            var x1 = Convert.ToInt32(rawObj.BBox[0]);
            var y1 = Convert.ToInt32(rawObj.BBox[1]);
            var x2 = Convert.ToInt32(rawObj.BBox[2]);
            var y2 = Convert.ToInt32(rawObj.BBox[3]);

            Label = rawObj.Label;
            BBox = string.Join(";", new[] { x1, y1, x2, y2 }.Select(bboxCoord => System.Convert.ToString(bboxCoord)));
            ImageByteData = Image.FromFile(rawObj.ImagePath)
                .CropImage(new Rectangle(x1, y1, x2 - x1, y2 - y1))
                .ToByteArray(ImageFormat.Bmp);
            Id = ImageByteData.GetHashCode();
        }

        public RecognizedCroppedImage(RecognizedImage dbObj)
        {
            Label = dbObj.Category.CategoryName;
            BBox = dbObj.BBox;
            ImageByteData = dbObj.SerializedImage;
            Id = ImageByteData.GetHashCode();
        }
    }
}
