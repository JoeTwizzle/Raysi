using GLGraphics;
using ImageMagick;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raysi
{
    public class TextureLoader
    {
        static int GetLevels(int width, int height)
        {
            int levels = 1;
            while (((width | height) >> levels) != 0)
            {
                ++levels;
            }
            return levels;
        }

        static PixelFormat GetPixelFormat(MagickImage image)
        {
            switch (image.ChannelCount)
            {
                case 1:
                    return PixelFormat.Red;
                case 2:
                    return PixelFormat.Rg;
                case 3:
                    return PixelFormat.Rgb;
                case 4:
                    return PixelFormat.Rgba;
                default:
                    return PixelFormat.Rgba;
            }
            throw new Exception("WTF? This can't be the case. how did you get here!?!?!");
        }

        static TextureFormat GetTextureFormat(MagickImage image)
        {
            if (image.Format == MagickFormat.Hdr)
            {
                return TextureFormat.Rgb16f;
            }
            return image.ChannelCount switch
            {
                1 => TextureFormat.R8,
                2 => TextureFormat.Rg8,
                3 => TextureFormat.Rgb8,
                4 => TextureFormat.Rgba8,
                _ => TextureFormat.Rgba8,
            };
            throw new Exception("WTF? This can't be the case. how did you get here!?!?!");
        }
        public static Texture2D LoadTexture2D(string path, bool colorTexture)
        {
            Texture2D tex = new Texture2D();
            MagickImage image;
            image = new MagickImage(path);
            if (colorTexture)
            {
                image.TransformColorSpace(ColorProfile.SRGB);
            }
            var pix = image.GetPixelsUnsafe();
            var ptr = pix.GetAreaPointer(0, 0, image.Width, image.Height);
            tex.Init(image.Width, image.Height, GetTextureFormat(image), GetLevels(image.Width, image.Height));
            tex.SetImage(ptr, 0, 0, image.Width, image.Height, GetPixelFormat(image), PixelType.UnsignedShort);
            tex.Filter = TextureFilter.Trilinear;
            tex.SetTextureParams();
            tex.GenMipmaps();
            pix.Dispose();
            image.Dispose();
            return tex;
        }
    }
}
