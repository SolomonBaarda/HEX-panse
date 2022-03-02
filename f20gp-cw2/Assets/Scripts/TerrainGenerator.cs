using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainGenerator : MonoBehaviour
{
    public HexMap HexMap;

    public bool IsGenerating { get; private set; } = false;

    [Header("Noise generation settings")]
    public Noise.PerlinSettings Settings;
    public AnimationCurve HeightCurve;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    [Space]
    public Tilemap TileTypes;

    public TileBase Land;
    public TileBase AlwaysLand;
    public TileBase Water;
    public TileBase City;


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

        Settings.ValidateValues();
        TileTypes.CompressBounds();

        int width = TileTypes.cellBounds.xMax - TileTypes.cellBounds.xMin;
        int height = TileTypes.cellBounds.yMax - TileTypes.cellBounds.yMin;

        float[] heights = new float[width * height];
        Vector3Int[] positions = new Vector3Int[width * height];
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

                if (tile == Land)
                {
                    Vector3 samplePoint = TileTypes.layoutGrid.GetCellCenterWorld(position);
                    heights[index] = Noise.Perlin(Settings, seed, new Vector2(samplePoint.x, samplePoint.z));
                }
                else if (tile == AlwaysLand)
                {
                    heights[index] = 0.5f;
                }
                else if (tile == Water)
                {
                    heights[index] = 0.0f;
                }
                else
                {
                    Debug.Log("Missing case for tile type island generation");
                    continue;
                }

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

        yield return null;

        // Normalise all values so that they are in range 0 - 1
        for (int i = 0; i < width * height; i++)
        {
            heights[i] = HeightCurve.Evaluate((heights[i] - min) / (max - min));
        }


        // Set all the tiles
        HexMap.ConstructTerrainMesh(positions, heights, 0.0f, 1.0f);
        yield return null;

        HexMap.Recalculate();

        TileTypes.GetComponent<TilemapRenderer>().enabled = false;

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }


}
