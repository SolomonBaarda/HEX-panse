using System;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour
{
    // Constants
    public const float GridSize = 1.0f;
    public const float HexagonEdgeLength = GridSize / 2;

    [Header("Grid display settings")]
    [Min(0)] public float HeightOffset = 25f * GridSize;
    [Min(1)] public int MaxTerraces = 10;

    [Header("Chunk settings")]
    public Transform TerrainParent;

    protected Dictionary<Vector3Int, Hexagon> Hexagons = new Dictionary<Vector3Int, Hexagon>();

    [Header("Object references")]
    public Grid Grid;

    [Space]
    public Material GroundMaterial;
    public Material WaterMaterial;

    private void Awake()
    {
        Grid.cellSize = new Vector3(GridSize, GridSize, GridSize);
    }

    public void ClearAll()
    {
        // Destroy all the children
        for (int i = 0; i < TerrainParent.childCount; i++)
        {
            Destroy(TerrainParent.GetChild(i).gameObject);
        }

        // Clear the dictionary
        Hexagons.Clear();
    }


    public void Recalculate()
    {
        // Recalculate each edge for all the hexagons in this chunk
        foreach (Hexagon h in Hexagons.Values)
        {
            RecalculateEdges(h);
        }

        // Merge all the meshes
        MergeAllMeshes();
    }


    private void RecalculateEdges(Hexagon h)
    {
        // Get all neighbour hexagons
        List<Hexagon> neighbours = new List<Hexagon>();
        foreach (Vector3Int neighbourCell in Hexagon.CalculatePossibleNeighbourCells(h.Cell))
        {
            // Get the valid chunk
            if (Hexagons.ContainsKey(neighbourCell))
            {
                neighbours.Add(Hexagons[neighbourCell]);
            }

        }

        // Now recalculate all the edges for this hexagon
        h.RecalculateEdges(neighbours, transform.localToWorldMatrix);
    }


    private void AddHexagon(Vector3Int worldCellPosition, float heightMultiplier)
    {
        // Now round the height 
        float roundedMultiplier = (float)Mathf.RoundToInt(heightMultiplier * MaxTerraces) / MaxTerraces;

        // Calculate the height
        Vector3 worldHeight = Grid.GetCellCenterWorld(worldCellPosition) + new Vector3(0, roundedMultiplier * HeightOffset, 0);

        Hexagons[worldCellPosition] = new Hexagon(worldCellPosition, transform.localToWorldMatrix, worldHeight, roundedMultiplier, Hexagon.Terrain.Land);
    }


    public void ConstructTerrainMesh(Vector3Int[] worldCellPositions, float[] heightMultipliers, float min, float max)
    {
        for (int i = 0; i < worldCellPositions.Length; i++)
        {
            AddHexagon(worldCellPositions[i], heightMultipliers[i]);
        }
    }



    private static void OptimiseMesh(Mesh m)
    {
        // Apply all optimisations
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        m.Optimize();
    }




    /// <summary>
    /// Merges all sub meshes and returns the number of meshes created.
    /// </summary>
    /// <returns></returns>
    public int MergeAllMeshes()
    {
        // Get all the meshes
        Dictionary<float, List<CombineInstance>> heightToMesh = new Dictionary<float, List<CombineInstance>>();

        // Sort the meshes by height
        foreach (Hexagon h in Hexagons.Values)
        {
            // Check that the entry exists
            if (!heightToMesh.TryGetValue(h.HeightMultiplier, out List<CombineInstance> meshes))
            {
                // Create a new list and add it as a value
                meshes = new List<CombineInstance>();
                heightToMesh[h.HeightMultiplier] = meshes;
            }

            // Add the face of the hexagon
            meshes.Add(h.FaceCombineInstance);

            // Add any other meshes now

            if (h.Edges != null)
            {
                // Add all the hexagons edges
                foreach (CombineInstance c in h.Edges)
                {
                    meshes.Add(c);
                }
            }
        }

        List<MeshFilter> allSubMeshes = new List<MeshFilter>();


        // Here make a seperate mesh for each height layer

        // Loop through all meshes for each height
        foreach (float key in heightToMesh.Keys)
        {
            heightToMesh.TryGetValue(key, out List<CombineInstance> meshes);

            // Combine all the meshes
            Mesh m = new Mesh();
            m.CombineMeshes(meshes.ToArray(), true);

            // Optimise the mesh
            OptimiseMesh(m);

            // Create the new mesh filter object
            allSubMeshes.Add(InstantiateMeshFilter(m, key, GroundMaterial));
        }



        return allSubMeshes.Count;
    }



    private MeshFilter InstantiateMeshFilter(Mesh m, float heightMultiplier, Material material)
    {
        // Create the new GameObject
        GameObject g = new GameObject("Mesh Layer " + heightMultiplier.ToString());
        g.transform.parent = TerrainParent;

        MeshFilter f = g.AddComponent<MeshFilter>();
        f.mesh = m;

        MeshRenderer r = g.AddComponent<MeshRenderer>();

        r.sharedMaterial = material;
        r.material.SetFloat("Height", heightMultiplier);



        return f;
    }

    public class Hexagon
    {
        public readonly Vector3Int Cell;
        public readonly float HeightMultiplier;
        public readonly Vector3 CentreOfFaceWorld;

        public Mesh Face;
        private readonly Matrix4x4 transform;
        public CombineInstance FaceCombineInstance { get { return new CombineInstance() { mesh = Face, transform = transform, }; } }

        public List<CombineInstance> Edges = new List<CombineInstance>();

        public Terrain TerrainType;

        public Hexagon(Vector3Int cell, Matrix4x4 transform, Vector3 centreOfFace, float heightMultiplier, Terrain terrainType)
        {
            Cell = cell;
            HeightMultiplier = heightMultiplier;

            this.transform = transform;
            TerrainType = terrainType;
            CentreOfFaceWorld = centreOfFace;

            Face = GenerateFaceMesh(centreOfFace);
        }


        private Mesh GenerateFaceMesh(Vector3 centreOfFace)
        {
            // Don't use this as our hexagons aren't EXACTLY mathematically perfect, but good enough for the grid
            /*
            float sqrt3 = Mathf.Sqrt(3);
            float xOffset = (sqrt3 * HexagonEdgeLength) / 2;
            */

            // Get the positions of the vertices of the face of the hex
            Vector3 top = centreOfFace + new Vector3(0, 0, HexagonEdgeLength);
            Vector3 bottom = centreOfFace + new Vector3(0, 0, -HexagonEdgeLength);

            Vector3 topLeft = centreOfFace + new Vector3(-HexagonEdgeLength, 0, HexagonEdgeLength / 2);
            Vector3 topRight = centreOfFace + new Vector3(HexagonEdgeLength, 0, HexagonEdgeLength / 2);

            Vector3 bottomLeft = centreOfFace + new Vector3(-HexagonEdgeLength, 0, -HexagonEdgeLength / 2);
            Vector3 bottomRight = centreOfFace + new Vector3(HexagonEdgeLength, 0, -HexagonEdgeLength / 2);

            // Create the mesh
            Mesh m = new Mesh
            {
                // Add vertices
                vertices = new Vector3[] { top, topRight, bottomRight, bottom, bottomLeft, topLeft, centreOfFace },
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

        public void RecalculateEdges(List<Hexagon> neighbours, Matrix4x4 transform)
        {
            // Re-assign the edges variable
            Edges = GenerateEdgeMeshesForNeighbours(neighbours, transform);
        }

        private List<CombineInstance> GenerateEdgeMeshesForNeighbours(List<Hexagon> neighbours, Matrix4x4 transform)
        {
            List<CombineInstance> newMeshesToAdd = new List<CombineInstance>();

            // Check each neighbour
            foreach (Hexagon neighbour in neighbours)
            {
                // Don't check it's self and ensure we actually want to create edges here
                if (!neighbour.Cell.Equals(Cell) && HeightMultiplier > neighbour.HeightMultiplier)
                {
                    List<(Vector3, Vector3)> sharedVertices = new List<(Vector3, Vector3)>();

                    // Loop through all possibilities
                    foreach (Vector3 neighbourVertex in neighbour.Face.vertices)
                    {
                        foreach (Vector3 vertex in Face.vertices)
                        {
                            // Hexagons are next to each other and current is above the neighbour

                            if (Mathf.Approximately(neighbourVertex.x, vertex.x) && Mathf.Approximately(neighbourVertex.z, vertex.z) && HeightMultiplier > neighbour.HeightMultiplier)
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
                        CombineInstance com = new CombineInstance
                        {
                            mesh = m,
                            transform = transform
                        };

                        newMeshesToAdd.Add(com);
                    }
                }
            }

            // Once we get here all neighbours have been checked
            return newMeshesToAdd;
        }


        public enum Terrain
        {
            Land,
            Water,
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


