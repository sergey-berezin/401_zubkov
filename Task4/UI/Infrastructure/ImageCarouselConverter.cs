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
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null:
                    return null;

                case RecognizedImage predict when targetType.IsAssignableFrom(typeof(Bitmap)):
                    return new Bitmap(new MemoryStream(predict.ImageByteData));

                default:
                    throw new NotSupportedException("Invalid type for the Converter. Use the RecognizedCroppedImage type.");
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => 
            throw new NotSupportedException("Method ConvertBack is not defined for this converter");
    }
}
