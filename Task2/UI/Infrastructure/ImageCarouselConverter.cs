using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Core.ObjectRecognitionComponent.DataStructures;
using System;
using System.Globalization;


namespace UI.Infrastructure
{
    public class ImageCarouselConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            } 
            
            if (value is ResultRecognition predict && targetType.IsAssignableFrom(typeof(Bitmap)))
            {
                var x1 = System.Convert.ToInt32(predict.BBox[0]);
                var y1 = System.Convert.ToInt32(predict.BBox[1]);
                var x2 = System.Convert.ToInt32(predict.BBox[2]);
                var y2 = System.Convert.ToInt32(predict.BBox[3]);
                var image = new Bitmap(predict.ImagePath);

                return new CroppedBitmap(image, new PixelRect(x1, y1, x2 - x1, y2 - y1));
            } else {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
