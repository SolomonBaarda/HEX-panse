using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class HexMap : MonoBehaviour
{
    // Constants
    public const float GridSize = 1.0f;
    public const float HexagonEdgeLength = GridSize / 2;
    private Grid Grid;

    [Header("Grid display settings")]
    [Min(0)] public float HeightOffset = 1.0f;

    public Dictionary<Vector3Int, Hexagon> Hexagons { get; protected set; } = new Dictionary<Vector3Int, Hexagon>();

    [Header("Object references")]
    public Transform TerrainParent;
    public Material GroundMaterial;
    public GameObject TerrainLayerPrefab;

    private void Awake()
    {
        Grid = GetComponent<Grid>();
        Grid.cellSize = new Vector3(GridSize, GridSize, GridSize);
    }

    public void Clear()
    {
        // Destroy all the children
        for (int i = 0; i < TerrainParent.childCount; i++)
        {
            Destroy(TerrainParent.GetChild(i).gameObject);
        }

        Hexagons.Clear();
    }

    public void AddHexagon(Vector3Int cell, float height, Terrain type)
    {
        Vector3 centreOfFaceWorld = Grid.GetCellCenterWorld(cell) + new Vector3(0, height * HeightOffset, 0);
        Hexagons[cell] = new Hexagon(height, centreOfFaceWorld, type);
    }

    public void GenerateMeshFromHexagons()
    {
        Dictionary<float, List<CombineInstance>> terrainLayers = new Dictionary<float, List<CombineInstance>>();

        // Calculate meshes for layer of the terrain
        foreach (KeyValuePair<Vector3Int, Hexagon> h in Hexagons)
        {
            // Ensure that the entry exists
            if (!terrainLayers.ContainsKey(h.Value.Height))
            {
                terrainLayers[h.Value.Height] = new List<CombineInstance>();
            }

            // Add the face of the hexagon
            terrainLayers[h.Value.Height].Add(new CombineInstance() { mesh = h.Value.GenerateFaceMesh(), transform = transform.worldToLocalMatrix });

            // Add all edges of this hexagon
            List<Hexagon> neighbours = new List<Hexagon>();
            foreach ((Vector3Int, Hexagon.NeighbourDirection) neighbour in Hexagon.CalculateAllPossibleNeighbourCells(h.Key))
            {
                if (Hexagons.ContainsKey(neighbour.Item1) && h.Value.CentreOfFaceWorld.y > Hexagons[neighbour.Item1].CentreOfFaceWorld.y)
                {
                    Mesh edge = h.Value.GenerateMeshEdgeForNeighbour(Hexagons[neighbour.Item1], neighbour.Item2);
                    terrainLayers[h.Value.Height].Add(new CombineInstance() { mesh = edge, transform = transform.worldToLocalMatrix });
                }
            }
        }

        // Loop through all terrain layers and combine them into a single mesh
        foreach (float height in terrainLayers.Keys)
        {
            // Combine all the meshes for this layer
            Mesh m = new Mesh();
            m.CombineMeshes(terrainLayers[height].ToArray(), true);

            // Optimise the mesh
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();
            m.Optimize();

            // Instantiate the mesh in the scene
            GameObject g = Instantiate(TerrainLayerPrefab, TerrainParent);
            g.name = height.ToString();

            g.GetComponent<MeshFilter>().mesh = m;
            MeshRenderer r = g.GetComponent<MeshRenderer>();
            r.material = GroundMaterial;
            r.material.SetFloat("Height", height);
        }
    }

    public class Hexagon
    {
        public readonly float Height;
        public readonly Vector3 CentreOfFaceWorld;
        public Terrain TerrainType;

        // Positions of the face vertexes in world space
        // NOTE: our hexagon's aren't mathematically correct as they use the Unity hex grid for positioning
        public Vector3 TopCentreVertex => CentreOfFaceWorld + new Vector3(0, 0, HexagonEdgeLength);
        public Vector3 BottomCentreVertex => CentreOfFaceWorld + new Vector3(0, 0, -HexagonEdgeLength);
        public Vector3 TopLeftVertex => CentreOfFaceWorld + new Vector3(-HexagonEdgeLength, 0, HexagonEdgeLength / 2);
        public Vector3 TopRightVertex => CentreOfFaceWorld + new Vector3(HexagonEdgeLength, 0, HexagonEdgeLength / 2);
        public Vector3 BottomLeftVertex => CentreOfFaceWorld + new Vector3(-HexagonEdgeLength, 0, -HexagonEdgeLength / 2);
        public Vector3 BottomRightVertex => CentreOfFaceWorld + new Vector3(HexagonEdgeLength, 0, -HexagonEdgeLength / 2);

        public Hexagon(float height, Vector3 centreOfFaceWorld, Terrain terrain)
        {
            Height = height;
            CentreOfFaceWorld = centreOfFaceWorld;
            TerrainType = terrain;
        }

        public enum NeighbourDirection
        {
            UpLeft, UpRight,
            Left, Right,
            DownLeft, DownRight
        }

        public Mesh GenerateFaceMesh()
        {
            // Create the mesh
            Mesh m = new Mesh
            {
                // Add vertices
                vertices = new Vector3[] { TopCentreVertex, TopRightVertex, BottomRightVertex, BottomCentreVertex, BottomLeftVertex, TopLeftVertex, CentreOfFaceWorld },
                // Add the triangles of the mesh
                triangles = new int[]
                {
                    6, 0, 1,
                    6, 1, 2,
                    6, 2, 3,
                    6, 3, 4,
                    6, 4, 5,
                    6, 5, 0,
                },
                // Add the normals (this is the face so normal is up)
                normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up },
            };

            return m;
        }

        public Mesh GenerateMeshEdgeForNeighbour(Hexagon neighbour, NeighbourDirection direction)
        {
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[] { 0, 1, 3, 0, 3, 2 };

            // Face normal
            Vector3 normal = neighbour.CentreOfFaceWorld - CentreOfFaceWorld;
            normal.y = 0;
            normal.Normalize();

            Vector3[] normals = { normal, normal, normal, normal };

            switch (direction)
            {
                case NeighbourDirection.UpLeft:
                    vertices[0] = TopLeftVertex;
                    vertices[1] = neighbour.BottomCentreVertex;
                    vertices[2] = TopCentreVertex;
                    vertices[3] = neighbour.BottomRightVertex;
                    break;
                case NeighbourDirection.UpRight:
                    vertices[0] = TopCentreVertex;
                    vertices[1] = neighbour.BottomLeftVertex;
                    vertices[2] = TopRightVertex;
                    vertices[3] = neighbour.BottomCentreVertex;
                    break;
                case NeighbourDirection.Left:
                    vertices[0] = BottomLeftVertex;
                    vertices[1] = neighbour.BottomRightVertex;
                    vertices[2] = TopLeftVertex;
                    vertices[3] = neighbour.TopRightVertex;
                    break;
                case NeighbourDirection.Right:
                    vertices[0] = TopRightVertex;
                    vertices[1] = neighbour.TopLeftVertex;
                    vertices[2] = BottomRightVertex;
                    vertices[3] = neighbour.BottomLeftVertex;
                    break;
                case NeighbourDirection.DownLeft:
                    vertices[0] = BottomCentreVertex;
                    vertices[1] = neighbour.TopRightVertex;
                    vertices[2] = BottomLeftVertex;
                    vertices[3] = neighbour.TopCentreVertex;
                    break;
                case NeighbourDirection.DownRight:
                    vertices[0] = BottomRightVertex;
                    vertices[1] = neighbour.TopCentreVertex;
                    vertices[2] = BottomCentreVertex;
                    vertices[3] = neighbour.TopLeftVertex;
                    break;
            }

            return new Mesh() { vertices = vertices, triangles = triangles, normals = normals };
        }

        public static List<(Vector3Int, NeighbourDirection)> CalculateAllPossibleNeighbourCells(in Vector3Int current)
        {
            // Left and right movement is simple on a pointy hex grid
            Vector3Int right = current + new Vector3Int(1, 0, 0);
            Vector3Int left = current + new Vector3Int(-1, 0, 0);

            // Other direction movement has odd and even cases for y values
            // ODD CASE
            Vector3Int upRight = current + new Vector3Int(1, 1, 0);
            Vector3Int upLeft = current + new Vector3Int(0, 1, 0);
            Vector3Int downRight = current + new Vector3Int(1, -1, 0);
            Vector3Int downLeft = current + new Vector3Int(0, -1, 0);

            // EVEN CASE
            if (current.y % 2 == 0)
            {
                upRight = current + new Vector3Int(0, 1, 0);
                upLeft = current + new Vector3Int(-1, 1, 0);
                downRight = current + new Vector3Int(0, -1, 0);
                downLeft = current + new Vector3Int(-1, -1, 0);
            }

            return new List<(Vector3Int, NeighbourDirection)>(new (Vector3Int, NeighbourDirection)[]
            { (upLeft, NeighbourDirection.UpLeft), (upRight, NeighbourDirection.UpRight), (right, NeighbourDirection.Right),
                (downRight, NeighbourDirection.DownRight), (downLeft, NeighbourDirection.DownLeft), (left, NeighbourDirection.Left) });
        }
    }

}
