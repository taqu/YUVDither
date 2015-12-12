/**
@file ImporterYUVDither.cs
@author t-sakai
@date 2015/12/13 create

Compress a texture, the userData option of which in the meta file
contains a string "YUVDITHER". e.g.) "userData: YUVDITHER"

Copyright (c) 2015 Takurou Sakai
*/
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text;

class ImporterYUVDither : AssetPostprocessor
{
    const string Tag = "YUVDITHER";

    void OnPreprocessTexture()
    {
        TextureImporter importer = assetImporter as TextureImporter;

        if(importer.userData != Tag) {
            return;
        }
        importer.alphaIsTransparency = true;
        importer.compressionQuality = (int)TextureCompressionQuality.Best;
        importer.textureFormat = TextureImporterFormat.ARGB32;
    }

    void OnPostprocessTexture(Texture2D texture)
    {
        TextureImporter importer = assetImporter as TextureImporter;
        if(importer.userData != Tag) {
            return;
        }

        //resize to even
        int width = (int)((texture.width + 1)&0xFFFFFFFEU);
        int height = (int)((texture.height + 1)&0xFFFFFFFEU);
        if(width != texture.width || height != texture.height) {
            resize(texture, width, height);
        }

        //Decomposite YUV
        packRGBA(texture);

        //Error diffusion
        FloydSteinbergErrorDiffusion.convert(texture);

        importer.alphaIsTransparency = false;
        importer.compressionQuality = (int)TextureCompressionQuality.Best;
        EditorUtility.CompressTexture(texture, TextureFormat.RGB565, TextureCompressionQuality.Best);
        importer.textureFormat = TextureImporterFormat.RGB16;
        importer.isReadable = false;
    }


    void resize(Texture2D texture, int width, int height)
    {
        float iw = 1.0f/texture.width;
        float ih = 1.0f/texture.height;
        float rw = (float)texture.width/width;
        float rh = (float)texture.height/height;

        Texture2D temp = new Texture2D(texture.width, texture.height, texture.format, false);
        for(int i=0; i<texture.height; ++i){
            for(int j=0; j<texture.width; ++j){
                temp.SetPixel(j, i, texture.GetPixel(j,i));
            }
        }

        texture.Resize(width, height);
        for(int i = 0; i < height; ++i) {
            float v = ih*i*rh;
            for(int j = 0; j < width; ++j) {
                float u = iw*j*rw;

                texture.SetPixel(j, i, temp.GetPixelBilinear(u, v));
            }
        }

        texture.Apply();
    }

    static readonly Color Clear = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    static void clear(Color[,] colors)
    {
        for(int i=0; i<colors.GetLength(0); ++i) {
            for(int j=0; j<colors.GetLength(1); ++j) {
                colors[i, j] = Clear;
            }
        }
    }

    static Color rgbaToYuva(Color c)
    {
        Color ret;
        ret.r =  0.299f*c.r + 0.587f*c.g + 0.114f*c.b;
        ret.g = -0.169f*c.r - 0.331f*c.g + 0.500f*c.b + 0.5f;
        ret.b =  0.500f*c.r - 0.419f*c.g - 0.081f*c.b + 0.5f;
        ret.a = c.a;
        return ret;
    }

    static void packRGBA(Texture2D texture)
    {
        int hw = texture.width>>1;
        Color[,] samples = new Color[2, 2] { { Clear, Clear }, { Clear, Clear } };
        Color[,] result = new Color[texture.height, texture.width<<1];
        clear(result);

        for(int i=0; i<texture.height; i+=2) {
            for(int j=0; j<texture.width; j+=2) {
                int x0 = j>>1;
                int x1 = x0+hw;

                Color yuva00;
                for(int dy=0; dy<2; ++dy) {
                    int dy0 = i+dy;
                    for(int dx=0; dx<2; ++dx) {
                        int dx0 = j+dx;

                        yuva00 = rgbaToYuva(texture.GetPixel(dx0, dy0));

                        samples[dy, dx] = yuva00;
                    }
                }

                result[i+0, j+0].g = samples[0, 0].r;
                result[i+0, j+1].g = samples[0, 1].r;
                result[i+1, j+0].g = samples[1, 0].r;
                result[i+1, j+1].g = samples[1, 1].r;

                result[i+0, x0].b = 0.5f*(samples[0, 0].g + samples[0, 1].g);
                result[i+1, x0].b = 0.5f*(samples[1, 0].g + samples[1, 1].g);

                result[i+0, x1].b = 0.5f*(samples[0, 0].b + samples[0, 1].b);
                result[i+1, x1].b = 0.5f*(samples[1, 0].b + samples[1, 1].b);

                result[i+0, j+0].r = samples[0, 0].a;
                result[i+0, j+1].r = samples[0, 1].a;
                result[i+1, j+0].r = samples[1, 0].a;
                result[i+1, j+1].r = samples[1, 1].a;
            }
        }

        int width = texture.width;
        int height = texture.height;

        for(int i=0; i<height; ++i) {
            for(int j=0; j< width; ++j) {
                texture.SetPixel(j, i, result[i, j]);
            }
        }

        texture.Apply();
    }
}

