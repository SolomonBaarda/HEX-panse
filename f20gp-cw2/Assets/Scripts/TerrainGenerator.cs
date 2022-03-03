using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainGenerator : MonoBehaviour
{
    public HexMap HexMap;

    public bool IsGenerating { get; private set; } = false;

    [Header("Noise generation settings")]
    public Noise.PerlinSettings NoiseSettings;
    public AnimationCurve HeightCurve;
    [Min(1)]
    public int NumberOfTerraces = 6;

    [Header("Biome settings")]
    public TerrainSettings TerrainSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    [Space]
    public Tilemap TileTypes;

    public TileBase Land;
    public TileBase AlwaysLand;
    public TileBase OutOfBounds;
    public TileBase PlayerCity;
    public TileBase EnemyCity;

    private enum Terrain
    {
        None,
        Land,
        PlayerCity,
        EnemyCity
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

        HexMap.Clear();
        TileTypes.CompressBounds();

        NoiseSettings.ValidateValues();
        TerrainSettings.ValidateValues();

        System.Random r = new System.Random(seed);

        int width = TileTypes.cellBounds.xMax - TileTypes.cellBounds.xMin;
        int height = TileTypes.cellBounds.yMax - TileTypes.cellBounds.yMin;

        float[] heights = new float[width * height];
        Vector3Int[] positions = new Vector3Int[width * height];
        Terrain[] terrain = new Terrain[width * height];
        float min = float.MaxValue, max = float.MinValue;

        foreach (Vector3Int position in TileTypes.cellBounds.allPositionsWithin)
        {
            TileBase tile = TileTypes.GetTile(position);
            if (tile != null)
            {
                int x = position.x - TileTypes.cellBounds.xMin;
                int y = position.y - TileTypes.cellBounds.yMin;

                int index = y * width + x;

                positions[index] = position;
                heights[index] = 0.0f;
                terrain[index] = Terrain.None;

                if (tile == Land)
                {
                    Vector3 samplePoint = TileTypes.layoutGrid.GetCellCenterWorld(position);
                    heights[index] = Noise.Perlin(NoiseSettings, seed, new Vector2(samplePoint.x, samplePoint.z));
                    terrain[index] = Terrain.Land;
                }
                else if (tile == AlwaysLand)
                {
                    heights[index] = 0.5f;
                    terrain[index] = Terrain.Land;
                }
                else if (tile == PlayerCity)
                {
                    heights[index] = 0.5f;
                    terrain[index] = Terrain.PlayerCity;
                }
                else if (tile == EnemyCity)
                {
                    // Should be a city here
                    if (r.NextDouble() < TerrainSettings.EnemyCityChance)
                    {
                        heights[index] = 0.5f;
                        terrain[index] = Terrain.EnemyCity;
                    }
                    // Otherwise just do normal terrain
                    else
                    {
                        Vector3 samplePoint = TileTypes.layoutGrid.GetCellCenterWorld(position);
                        heights[index] = Noise.Perlin(NoiseSettings, seed, new Vector2(samplePoint.x, samplePoint.z));
                        terrain[index] = Terrain.Land;
                    }
                }
                else if (tile == OutOfBounds)
                {
                    heights[index] = 0.0f;
                    terrain[index] = Terrain.None;
                }
                else
                {
                    Debug.LogError("Missing case for tile type island generation");
                    continue;
                }

                if (terrain[index] != Terrain.None)
                {
                    if (heights[index] > max)
                    {
                        max = heights[index];
                    }

                    if (heights[index] < min)
                    {
                        min = heights[index];
                    }
                }
            }
        }

        yield return null;

        for (int i = 0; i < width * height; i++)
        {
            // Normalise all values so that they are in range 0 - 1
            heights[i] = HeightCurve.Evaluate((heights[i] - min) / (max - min));

            // Then round values so that they appear as terraces
            heights[i] = (float)Mathf.RoundToInt(heights[i] * NumberOfTerraces) / NumberOfTerraces;

            // Add the hexagon to the map
            HexMap.AddHexagon(positions[i], heights[i], CalculateBiome(heights[i], terrain[i]));
        }

        yield return null;

        HexMap.GenerateMeshFromHexagons();

        TileTypes.GetComponent<TilemapRenderer>().enabled = false;

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }

    private Biome CalculateBiome(float normalisedHeight, Terrain terrain)
    {
        switch (terrain)
        {
            case Terrain.None:
                return Biome.None;
            case Terrain.Land:
                return TerrainSettings.GetBiomeForHeight(normalisedHeight);
            case Terrain.PlayerCity:
                return Biome.PlayerCity;
            case Terrain.EnemyCity:
                return Biome.EnemyCity;
            default:
                return Biome.None;
        }
    }

}
