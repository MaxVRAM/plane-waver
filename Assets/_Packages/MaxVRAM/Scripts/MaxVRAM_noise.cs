using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxVRAM
{
    public struct Noise
    {
        public static float Perlin(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }
        
        public struct Perlin2D
        {
            public float X;
            public float Y;
            public float Scale;
            public float OffsetX;
            public float OffsetY;
            public float Value;
            
            public Perlin2D(float x, float y, float scale, float offsetX, float offsetY)
            {
                X = x;
                Y = y;
                Scale = scale;
                OffsetX = offsetX;
                OffsetY = offsetY;
                Value = 0;
            }
            
            public void ProcessNoise()
            {
                Value = Perlin(X * Scale + OffsetX, Y * Scale + OffsetY);
            }
        }
        
        public struct PerlinArray
        {
            private float[] _seedValues;
            public float Scale { get; set; }

            public PerlinArray(int arraySize, float scale = 1f)
            {
                Scale = scale;
                _seedValues = PerlinSeedArray(arraySize);
            }
            
            public void RebuildSeedArray(int arraySize)
            { 
                _seedValues = PerlinSeedArray(arraySize);
            }

            public float GetValueAtOffset(int index, float offset)
            {
                offset *= Scale;
                return Mathf.PerlinNoise(offset + _seedValues[index], (offset + _seedValues[index]) * 0.5f);
            }
            
            public float GetValueAtOffset(int index, float offset, float scale)
            {
                Scale = scale;
                offset *= Scale;
                return Mathf.PerlinNoise(offset + _seedValues[index], (offset + _seedValues[index]) * 0.5f);
            }
        }
        
        public static float[] PerlinSeedArray(int arraySize)
        {
            var seedArray = new float[arraySize];
            for (var i = 0; i < seedArray.Length; i++)
            {
                float offset = Random.Range(0, 1000);
                seedArray[i] = Mathf.PerlinNoise(offset, offset * 0.5f);
            }

            return seedArray;
        }
    }
}