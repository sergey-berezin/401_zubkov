using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using UI.Models;


namespace UI.Infrastructure
{
    internal class ImageCarouselConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;

                case RecognizedCroppedImage predict when targetType.IsAssignableFrom(typeof(Bitmap)):
                {
                    //var x1 = System.Convert.ToInt32(predict.BBox[0]);
                    //var y1 = System.Convert.ToInt32(predict.BBox[1]);
                    //var x2 = System.Convert.ToInt32(predict.BBox[2]);
                    //var y2 = System.Convert.ToInt32(predict.BBox[3]);
                    //var image = new Bitmap(predict.ImagePath);

                    //return new CroppedBitmap(image, new PixelRect(x1, y1, x2 - x1, y2 - y1));
                    return new Bitmap(new MemoryStream(predict.ImageByteData));
                }

                default:
                    throw new NotSupportedException("Invalid type for the Converter. Use the RecognizedCroppedImage type.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
            throw new NotSupportedException("Method ConvertBack is not defined for this converter");
    }
}
