using System;
using System.Collections;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Grid grid;
    public HexMap HexMap;

    public bool IsGenerating { get; private set; } = false;

    [Header("Noise generation settings")]
    public Noise.PerlinSettings HeightMapSettings;
    public AnimationCurve HeightCurve;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;



    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            IsGenerating = true;
            StartCoroutine(WaitForGenerate(Seed));
        }
    }

    private IEnumerator WaitForGenerate(int seed)
    {
        DateTime before = DateTime.Now;

        // Reset the whole HexMap
        HexMap.ClearAll();


        int size = 15;

        float[] heights = GenerateHeightMap(size, 2);
        yield return null;


        Vector3Int[] positions = new Vector3Int[size * size];

        float min = float.MaxValue, max = float.MinValue;

        // Loop through each height and calculate its position
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;

                positions[index] = new Vector3Int(x, y, 0);



                heights[index] = HeightCurve.Evaluate(heights[index]);

                if (heights[index] < min)
                {
                    min = heights[index];
                }

                if (heights[index] > max)
                {
                    max = heights[index];
                }
            }
        }

        yield return null;

        // Set all the tiles
        HexMap.ConstructTerrainMesh(positions, heights, min, max);
        yield return null;

        HexMap.Recalculate();

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }

    private float[] GenerateHeightMap(int maxArrayDimension, int numberWaterTiles)
    {
        int islandSize = maxArrayDimension - numberWaterTiles * 2;
        float[] originalHeights = GetPerlin(islandSize, islandSize, HeightMapSettings, Seed);
        float[] heights = new float[maxArrayDimension * maxArrayDimension];

        for(int y = 0; y < maxArrayDimension; y++)
        {
            for(int x = 0; x < maxArrayDimension; x++)
            {
                int newX = x - numberWaterTiles;
                int newY = y - numberWaterTiles;

                if(newX >= 0 && newY >= 0 &&  newX < islandSize && newY < islandSize &&
                   Mathf.Abs(newX - islandSize / 2) + Mathf.Abs(newY - islandSize / 2) < islandSize / 2)
                {
                    heights[y * maxArrayDimension + x] = originalHeights[newY * islandSize + newX];
                    continue;
                }

                heights[y * maxArrayDimension + x] = 0.0f;
            }
        }

/*        if (Mathf.Abs(x - size / 2) + Mathf.Abs(y - size / 2) > size / 2)
        {
            heights[index] = 0.0f;
        }*/

        return heights;
    }

    private float[] GetPerlin(int width, int height, Noise.PerlinSettings settings, int seed)
    {
        settings.ValidateValues();

        // Get the height map
        float[] heightMap = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heightMap[y * width + x] = Mathf.Clamp01(Noise.Perlin(HeightMapSettings, seed, new Vector2(x, y)));
            }
        }

        return heightMap;
    }

}
