using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;


namespace System.Drawing
{
    internal static class ImageExtensions
    {
		public static Bitmap CropImage(this Image image, Rectangle sourceRectangle, Rectangle? destinationRectangle = null)
        {
            destinationRectangle ??= new Rectangle(Point.Empty, sourceRectangle.Size);

            var croppedImage = new Bitmap(destinationRectangle.Value.Width, destinationRectangle.Value.Height);

            using var graphics = Graphics.FromImage(croppedImage);
            graphics.DrawImage(image, destinationRectangle.Value, sourceRectangle, GraphicsUnit.Pixel);

            return croppedImage;
        }

		public static byte[] ToByteArray(this Image image, ImageFormat format)
        {
            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, format);
            return memoryStream.ToArray();
        }
    }
}
