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
    [Min(1)]
    public int NumberOfTerraces = 6;

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
            //TileTypes.ClearAllTiles();
            //TileTypes.SetTile(new Vector3Int(0, 0, 0), Land);
            //TileTypes.SetTile(new Vector3Int(0, 1, 0), Water);
            //TileTypes.SetTile(new Vector3Int(0, -1, 0), City);


            //return;

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
        HexMap.Clear();

        Settings.ValidateValues();
        TileTypes.CompressBounds();

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
                terrain[index] = Terrain.Water;

                if (tile == Land)
                {
                    Vector3 samplePoint = TileTypes.layoutGrid.GetCellCenterWorld(position);
                    heights[index] = Noise.Perlin(Settings, seed, new Vector2(samplePoint.x, samplePoint.z));
                    terrain[index] = Terrain.Land;
                }
                else if (tile == AlwaysLand)
                {
                    heights[index] = 0.5f;
                    terrain[index] = Terrain.Land;
                }
                else if (tile == Water)
                {
                    heights[index] = 0.0f;
                    terrain[index] = Terrain.Water;
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

        for (int i = 0; i < width * height; i++)
        {
            // Normalise all values so that they are in range 0 - 1
            heights[i] = HeightCurve.Evaluate((heights[i] - min) / (max - min));

            // Then round values so that they appear as terraces
            heights[i] = (float)Mathf.RoundToInt(heights[i] * NumberOfTerraces) / NumberOfTerraces;

            // Add the hexagon to the map
            HexMap.AddHexagon(positions[i], heights[i], terrain[i]);
        }

        yield return null;

        HexMap.GenerateMeshFromHexagons();

        TileTypes.GetComponent<TilemapRenderer>().enabled = false;

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }


}
