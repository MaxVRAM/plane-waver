using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxVRAM.Noise
{
    public static class PerlinNoise
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
        
        public class Array
        {
            private readonly float[] _seeds;
            private float[] _offsets;
    
            public Array(int arraySize, float scale = 1f)
            {
                _seeds = BuildPerlinSeedArray(arraySize, out _offsets);
            }
    
            public float ValueAtOffset(int index, float offset)
            {
                offset *= _offsets[index];
                return Mathf.PerlinNoise(offset + _seeds[index], (offset + _seeds[index]) * 0.5f);
            }
    
            public float AccumulatedOffsetValue(int index, float offset)
            {
                if (index >= _offsets.Length || index < 0)
                    return 0f;
                
                _offsets[index] += offset;
                
                return Mathf.PerlinNoise(
                    _offsets[index] + _seeds[index],
                    (_offsets[index] + _seeds[index]) * 0.5f);
            }
            
            private static float[] BuildPerlinSeedArray(int arraySize, out float[] offsetArray)
            {
                var seedArray = new float[arraySize];
                offsetArray = new float[arraySize];
                for (var i = 0; i < seedArray.Length; i++)
                {
                    offsetArray[i] = 0f;
                    float offset = Random.Range(0, 1000);
                    seedArray[i] = Mathf.PerlinNoise(offset, offset * 0.5f);
                }
    
                return seedArray;
            }
        }
    }
}