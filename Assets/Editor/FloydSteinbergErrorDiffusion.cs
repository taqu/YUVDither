/**
@file FloydSteinbergErrorDiffusion.cs
@author t-sakai
@date 2015/11/24 create
@brief Floyd-Steinberg Error Diffusion

Copyright (c) 2015 Takurou Sakai
*/
using UnityEngine;

public class FloydSteinbergErrorDiffusion
{
    const float K12 = 7.0f / 16.0f;
    const float K20 = 3.0f / 16.0f;
    const float K21 = 5.0f / 16.0f;
    const float K22 = 1.0f / 16.0f;

    public static void convert(Texture2D texture)
    {
        Color[,] color = new Color[texture.height, texture.width];

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                color[i,j] = texture.GetPixel(j, i);
            }
        }

        int hw = texture.width>>1;

        processRG(color, 0, 0, texture.width, texture.height);

        processB(color, 0, 0, hw, texture.height);
        processB(color, hw, 0, texture.width, texture.height);

        for(int i = 0; i < texture.height; ++i) {
            for(int j = 0; j < texture.width; ++j) {
                color[i,j].r = Mathf.Clamp01(color[i,j].r);
                color[i,j].g = Mathf.Clamp01(color[i,j].g);
                color[i,j].b = Mathf.Clamp01(color[i,j].b);
                color[i,j].a = Mathf.Clamp01(color[i,j].a);

                texture.SetPixel(j, i, color[i,j]);
            }
        }
        texture.Apply();
    }


    private static float error4bit(ref float v)
    {
        float rv = quantize4bit(v);
        float error = v - rv;
        v = rv;
        return error;
    }

    private static float error5bit(ref float v)
    {
        float rv = quantize5bit(v);
        float error = v - rv;
        v = rv;
        return error;
    }

    private static float error6bit(ref float v)
    {
        float rv = quantize6bit(v);
        float error = v - rv;
        v = rv;
        return error;
    }


    private static float quantize4bit(float v)
    {
        return Mathf.Round(v * 15.0f) / 15.0f;
    }

    private static float quantize5bit(float v)
    {
        return Mathf.Round(v * 31.0f) / 31.0f;
    }

    private static float quantize6bit(float v)
    {
        return Mathf.Round(v * 63.0f) / 63.0f;
    }

    private static void processRightRG(Color[,] v, int y, int startx, int endx, int endy)
    {
        for(int j = startx; j < endx; ++j) {
            float error_r = error5bit(ref v[y,j].r);
            float error_g = error6bit(ref v[y,j].g);

            if(j < endx - 1) {
                v[y,j+1].r += error_r * K12;
                v[y,j+1].g += error_g * K12;
            }

            if(y < endy - 1) {

                if(0 < j) {
                    v[y+1, j-1].r += error_r * K20;
                    v[y+1, j-1].g += error_g * K20;
                }

                v[y+1, j].r += error_r * K21;
                v[y+1, j].g += error_g * K21;

                if(j < endx - 1) {
                    v[y+1, j+1].r += error_r * K22;
                    v[y+1, j+1].g += error_g * K22;
                }
            }

        }//for(int j = 0;
    }

    private static void processLeftRG(Color[,] v, int y, int startx, int endx, int endy)
    {
        for(int j = endx - 1; 0 <= j; --j) {

            float error_r = error5bit(ref v[y,j].r);
            float error_g = error6bit(ref v[y,j].g);

            if(0 < j) {
                v[y,j-1].r += error_r * K12;
                v[y,j-1].g += error_g * K12;
            }

            if(y < endy - 1) {

                if(0 < j) {
                    v[y+1,j-1].r += error_r * K22;
                    v[y+1,j-1].g += error_g * K22;
                }

                v[y+1,j].r += error_r * K21;
                v[y+1,j].g += error_g * K21;

                if(j < endx - 1) {
                    v[y+1,j+1].r += error_r * K20;
                    v[y+1,j+1].g += error_g * K20;
                }
            }

        }//for(int j = 0;
    }

    private static void processRightB(Color[,] v, int y, int startx, int endx, int endy)
    {
        for(int j = startx; j < endx; ++j) {
            float error_b = error5bit(ref v[y,j].b);

            if(j < endx - 1) {
                v[y,j+1].b += error_b * K12;
            }

            if(y < endy - 1) {

                if(startx < j) {
                    v[y+1, j-1].b += error_b * K20;
                }

                v[y+1, j].b += error_b * K21;

                if(j < endx - 1) {
                    v[y+1, j+1].b += error_b * K22;
                }
            }

        }//for(int j = 0;
    }

    private static void processLeftB(Color[,] v, int y, int startx, int endx, int endy)
    {
        for(int j = endx - 1; startx <= j; --j) {
            float error_b = error5bit(ref v[y,j].b);

            if(startx < j) {
                v[y,j-1].b += error_b * K12;
            }

            if(y < endy - 1) {

                if(startx < j) {
                    v[y+1,j-1].b += error_b * K22;
                }

                v[y+1,j].b += error_b * K21;

                if(j < endx - 1) {
                    v[y+1,j+1].b += error_b * K20;
                }
            }

        }//for(int j = 0;
    }

    private static void processRG(Color[,] v, int startx, int starty, int endx, int endy)
    {
        for(int i = starty; i < endy; ++i) {
            bool scanline = (i & 0x01) == 0;
            if(scanline) {
                processRightRG(v, i, startx, endx, endy);
            } else {
                processLeftRG(v, i, startx, endx, endy);
            }
        }//for(int i = 0;
    }

    private static void processB(Color[,] v, int startx, int starty, int endx, int endy)
    {
        for(int i = starty; i < endy; ++i) {
            bool scanline = (i & 0x01) == 0;
            if(scanline) {
                processRightB(v, i, startx, endx, endy);
            } else {
                processLeftB(v, i, startx, endx, endy);
            }
        }//for(int i = 0;
    }
}
