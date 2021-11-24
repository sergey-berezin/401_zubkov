using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Core.ObjectRecognitionComponent.DataStructures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;


namespace Task2.Avalonia.UI.ViewModels {
    public class ImageCarouselConverter: IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is List<Tuple<string, YoloV4Result>> raw_data) {
                List<Image> images = new List<Image>();
                foreach (var (path, predict) in raw_data) {
                    var x1 = System.Convert.ToInt32(predict.BBox[0]);
                    var y1 = System.Convert.ToInt32(predict.BBox[1]);
                    var x2 = System.Convert.ToInt32(predict.BBox[2]);
                    var y2 = System.Convert.ToInt32(predict.BBox[3]);
                    var all_image = new Bitmap(path);

                    images.Add(new Image { Source = new CroppedBitmap(all_image, new PixelRect(x1, y1, x2 - x1, y2 - y1)) });
                }
                return images;
            } else {
                return null;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}
