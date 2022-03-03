using System;
using System.IO.Pipes;
using UnityEngine;

public class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public const float MoveAmount = 0.33f;

    /// <summary>
    /// Calculates a Perlin value at position additionalOffset using settings. Note values are not always between 0 and 1.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="seed"></param>
    /// <param name="additionalOffset"></param>
    /// <returns></returns>
    public static float Perlin(PerlinSettings settings, int seed, Vector2 additionalOffset = default)
    {
        float perlin;

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x + additionalOffset.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y + additionalOffset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        // Loop through each octave
        for (int octave = 0; octave < settings.octaves; octave++)
        {
            // Calculate the position to sample the noise from
            float sampleX = octaveOffsets[octave].x / settings.scale * frequency;
            float sampleY = octaveOffsets[octave].y / settings.scale * frequency;

            // Get perlin in the range -1 to 1
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }

        // Assign the perlin value and normalize it roughly
        perlin = (noiseHeight / 2.5f) + 0.5f;

        return perlin;
    }

    [Serializable]
    public class PerlinSettings
    {
        public float scale = 50;

        public int octaves = 4;
        [Range(0, 1)]
        public float persistance = .6f;
        public float lacunarity = 2;

        public Vector2 offset;

        public void ValidateValues()
        {
            scale = Mathf.Max(scale, 0.01f);
            octaves = Mathf.Max(octaves, 1);
            lacunarity = Mathf.Max(lacunarity, 1);
            persistance = Mathf.Clamp01(persistance);
        }
    }
}
