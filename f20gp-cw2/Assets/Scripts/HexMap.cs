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
        Dictionary<Vector3Int, HexagonMesh> hexMeshes = new Dictionary<Vector3Int, HexagonMesh>();

        foreach (KeyValuePair<Vector3Int, Hexagon> pair in Hexagons)
        {
            hexMeshes[pair.Key] = new HexagonMesh(pair.Key, pair.Value.CentreOfFaceWorld, pair.Value.Height, pair.Value.TerrainType);
        }

        // Recalculate each edge for all the hexagons in this chunk
        foreach (HexagonMesh h in hexMeshes.Values)
        {
            // Get all neighbour hexagons
            List<HexagonMesh> neighbours = new List<HexagonMesh>();
            foreach (Vector3Int neighbourCell in HexagonMesh.CalculatePossibleNeighbourCells(h.Cell))
            {
                // Get the valid chunk
                if (hexMeshes.ContainsKey(neighbourCell))
                {
                    neighbours.Add(hexMeshes[neighbourCell]);
                }
            }

            // Now recalculate all the edges for this hexagon
            h.CalculateMeshForEdges(neighbours, transform.localToWorldMatrix);
        }

        // Now combine all hexagons of the same height
        Dictionary<float, List<CombineInstance>> hexagonLayers = new Dictionary<float, List<CombineInstance>>();
        foreach (HexagonMesh h in hexMeshes.Values)
        {
            // Ensure that the entry exists
            if (!hexagonLayers.ContainsKey(h.Height))
            {
                hexagonLayers[h.Height] = new List<CombineInstance>();
            }

            // Add the face of the hexagon
            hexagonLayers[h.Height].Add(new CombineInstance() { mesh = h.Face, transform = transform.worldToLocalMatrix });

            // Add all the hexagons edges
            foreach (CombineInstance c in h.Edges)
            {
                hexagonLayers[h.Height].Add(c);
            }
        }

        // Loop through all meshes for each height
        foreach (float height in hexagonLayers.Keys)
        {
            // Combine all the meshes for this layer
            Mesh m = new Mesh();
            m.CombineMeshes(hexagonLayers[height].ToArray(), true);

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

        public Hexagon(float height, Vector3 centreOfFaceWorld, Terrain terrain)
        {
            Height = height;
            CentreOfFaceWorld = centreOfFaceWorld;
            TerrainType = terrain;
        }
    }

    private class HexagonMesh
    {
        public readonly Vector3Int Cell;
        public readonly float Height;
        public readonly Vector3 CentreOfFaceWorld;

        public Mesh Face;

        public List<CombineInstance> Edges = new List<CombineInstance>();

        public Terrain TerrainType;




        // Don't use this as our hexagons aren't EXACTLY mathematically perfect, but good enough for the grid
        /*
        float sqrt3 = Mathf.Sqrt(3);
        float xOffset = (sqrt3 * HexagonEdgeLength) / 2;
        */

        public Vector3 TopFaceVertex => CentreOfFaceWorld + new Vector3(0, 0, HexagonEdgeLength);
        public Vector3 BottomFaceVertex => CentreOfFaceWorld + new Vector3(0, 0, -HexagonEdgeLength);

        public Vector3 TopLeftFaceVertex => CentreOfFaceWorld + new Vector3(-HexagonEdgeLength, 0, HexagonEdgeLength / 2);
        public Vector3 TopRightFaceVertex => CentreOfFaceWorld + new Vector3(HexagonEdgeLength, 0, HexagonEdgeLength / 2);

        public Vector3 BottomLeftFaceVertex => CentreOfFaceWorld + new Vector3(-HexagonEdgeLength, 0, -HexagonEdgeLength / 2);
        public Vector3 BottomRightFaceVertex => CentreOfFaceWorld + new Vector3(HexagonEdgeLength, 0, -HexagonEdgeLength / 2);



        public HexagonMesh(Vector3Int cell, Vector3 centreOfFace, float heightMultiplier, Terrain terrainType)
        {
            Cell = cell;
            Height = heightMultiplier;

            TerrainType = terrainType;
            CentreOfFaceWorld = centreOfFace;

            Face = GenerateFaceMesh();
        }


        private Mesh GenerateFaceMesh()
        {
            // Create the mesh
            Mesh m = new Mesh
            {
                // Add vertices
                vertices = new Vector3[] { TopFaceVertex, TopRightFaceVertex, BottomRightFaceVertex, BottomFaceVertex, BottomLeftFaceVertex, TopLeftFaceVertex, CentreOfFaceWorld },
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

        public void CalculateMeshForEdges(List<HexagonMesh> neighbours, Matrix4x4 transform)
        {
            List<CombineInstance> newMeshesToAdd = new List<CombineInstance>();

            // Check each neighbour
            foreach (HexagonMesh neighbour in neighbours)
            {
                // Don't check it's self and ensure we actually want to create edges here
                if (!neighbour.Cell.Equals(Cell) && Height > neighbour.Height)
                {
                    List<(Vector3, Vector3)> sharedVertices = new List<(Vector3, Vector3)>();

                    // Loop through all possibilities
                    foreach (Vector3 neighbourVertex in neighbour.Face.vertices)
                    {
                        foreach (Vector3 vertex in Face.vertices)
                        {
                            // Hexagons are next to each other and current is above the neighbour
                            
                            if (Mathf.Approximately(neighbourVertex.x, vertex.x) && Mathf.Approximately(neighbourVertex.z, vertex.z))
                            {
                                sharedVertices.Add((vertex, neighbourVertex));
                            }
                        }
                    }

                    // Should have 2 shared vertex pairs if hexagons are neighbours and on different heights
                    if (sharedVertices.Count == 2)
                    {
                        // Make sure to assign the triangles in clockwise order
                        sharedVertices.Sort((x, y) => Clockwise.Compare(x.Item1, y.Item1, CentreOfFaceWorld));
                        Vector3 a = sharedVertices[0].Item1, b = sharedVertices[0].Item2, c = sharedVertices[1].Item1, d = sharedVertices[1].Item2;

                        // Calculate the normal for that face
                        Vector3 midpointOfEdge = a + ((c - a) / 2);
                        Vector3 normal = Vector3.Normalize(midpointOfEdge - CentreOfFaceWorld);

                        // Once we have all shared vertices, construct the mesh
                        Mesh m = new Mesh
                        {
                            vertices = new Vector3[] { a, b, c, d },
                            triangles = new int[]
                            {
                                0, 1, 3,
                                0, 3, 2
                            },
                            normals = new Vector3[] { normal, normal, normal, normal },
                        };

                        // Add the mesh to the list of meshes to be merged
                        newMeshesToAdd.Add(new CombineInstance() { mesh = m, transform = transform });
                    }
                    else
                    {
                        Debug.LogError("Failed to generate hexagon edge mesh");
                        Debug.LogError(sharedVertices.Count);
                    }
                }
            }

            // Once we get here all neighbours have been checked
            Edges = newMeshesToAdd;
        }

        public static List<Vector3Int> CalculatePossibleNeighbourCells(in Vector3Int current)
        {
            // Convoluted way of calculating the neighbour positions of a pointy hex grid cell
            Vector3Int upLeft = current + new Vector3Int(-1, -1, 0);
            Vector3Int upRight = current + new Vector3Int(0, -1, 0);

            Vector3Int downLeft = current + new Vector3Int(-1, 1, 0);
            Vector3Int downRight = current + new Vector3Int(0, 1, 0);

            Vector3Int left = current + new Vector3Int(-1, 0, 0);
            Vector3Int right = current + new Vector3Int(1, 0, 0);

            // Y is an odd number so need to move to right instead of left
            if (current.y % 2 == 1)
            {
                upLeft = current + new Vector3Int(1, -1, 0);
                downLeft = current + new Vector3Int(1, 1, 0);
            }

            return new List<Vector3Int>(new Vector3Int[] { upLeft, upRight, right, downRight, downLeft, left });
        }

        private static class Clockwise
        {
            public static int Compare(Vector3 first, Vector3 second, Vector3 centre)
            {
                Vector3 firstOffset = first - centre;
                Vector3 secondOffset = second - centre;

                // Get the angles in degrees
                float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.z) * Mathf.Rad2Deg;
                float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.z) * Mathf.Rad2Deg;

                // Ensure we always have positive angles (go clockwise)
                while (angle1 < 0) angle1 += 360;
                while (angle2 < 0) angle2 += 360;


                // For some reason it my gen code does not like it when the angle is 0. 
                // Janky fix for now
                if (angle1 == 0)
                {
                    float temp = angle1;
                    angle1 = angle2;
                    angle2 = temp;
                }

                // Compare them
                return angle1.CompareTo(angle2);
            }
        }
    }
}


