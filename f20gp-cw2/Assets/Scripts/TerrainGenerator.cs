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
    public float HeightBetweenEachTerrace => 1.0f / NumberOfTerraces;

    [Header("Biome settings")]
    public TerrainSettings TerrainSettings;

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
        LandOrNone,
        Land
    }

    private void Awake()
    {
        TileTypes.GetComponent<TilemapRenderer>().enabled = false;
    }

    public void Generate(int seed)
    {
        if (!IsGenerating)
        {
            IsGenerating = true;
            StartCoroutine(WaitForGenerate(seed));
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
        BaseType[] cities = new BaseType[width * height];
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
                cities[index] = BaseType.None;

                float CalculateHeight()
                {
                    Vector3 samplePoint = TileTypes.layoutGrid.GetCellCenterWorld(position);
                    float height = Noise.Perlin(NoiseSettings, seed, new Vector2(samplePoint.x, samplePoint.z));
                    return height;
                }

                if (tile == Land)
                {
                    heights[index] = CalculateHeight();
                    terrain[index] = Terrain.LandOrNone;
                }
                else if (tile == AlwaysLand)
                {
                    heights[index] = CalculateHeight();
                    terrain[index] = Terrain.Land;
                }
                else if (tile == PlayerCity)
                {
                    heights[index] = CalculateHeight();
                    terrain[index] = Terrain.Land;
                    cities[index] = BaseType.Player;
                }
                else if (tile == EnemyCity)
                {
                    heights[index] = CalculateHeight();

                    // Do a dice roll to decide if there is a city here
                    if(r.NextDouble() < TerrainSettings.EnemyCityChance)
                    {
                        terrain[index] = Terrain.Land;
                        cities[index] = BaseType.Enemy;
                    }
                    else
                    {
                        terrain[index] = Terrain.LandOrNone;
                    }
                }
                else if (tile == OutOfBounds)
                {
                    heights[index] = 0f;
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

            if (terrain[i] == Terrain.Land)
            {
                heights[i] = Mathf.Clamp(heights[i], HeightBetweenEachTerrace, 1.0f);
            }

            // Add the hexagon to the map
            HexMap.AddHexagon(positions[i], heights[i], CalculateBiome(heights[i], terrain[i]), cities[i]);
        }

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }

    private Biome CalculateBiome(float normalisedHeight, Terrain terrain)
    {
        switch (terrain)
        {
            case Terrain.None:
                return Biome.None;
            case Terrain.LandOrNone:
                return TerrainSettings.GetBiomeForHeight(normalisedHeight);
            case Terrain.Land:
                return TerrainSettings.GetBiomeForHeight(normalisedHeight);
            default:
                return Biome.None;
        }
    }

}
