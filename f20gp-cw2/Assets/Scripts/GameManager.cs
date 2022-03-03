using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Range(2, 6)]
    public int NumberOfPlayers = 6;

    public int PlayerTurn;


    [Header("Camera Stuff")]
    public Camera Camera;
    public LayerMask MouseLayerMask;
    public float MouseRaycastDistance = 100.0f;

    public bool IsHoveringOverCell { get; protected set; } = false;
    public Vector3Int CellHoveringOver { get; protected set; } = default;

    public Transform HoverCellPreview;

    [Header("Terrain Stuff")]
    public TerrainGenerator TerrainGenerator;
    public HexMap HexMap;
    public Transform GameObjectPrent;
    public GameObject CityPrefab;

    private void Start()
    {
        StartCoroutine(StartGame());
    }

    public IEnumerator StartGame()
    {
        TerrainGenerator.Generate();

        while(TerrainGenerator.IsGenerating)
        {
            yield return null;
        }

        foreach (KeyValuePair<Vector3Int, HexMap.Hexagon> hex in HexMap.Hexagons)
        {
            if (hex.Value.TerrainType is Terrain.City)
            {
                Instantiate(CityPrefab, hex.Value.CentreOfFaceWorld, Quaternion.identity, GameObjectPrent);
            }
        }

/*        while (true)
        {
            yield return null;
        }*/
    }

    private void Update()
    {
        IsHoveringOverCell = false;
        Vector3 viewport = Camera.ScreenToViewportPoint(Input.mousePosition);

        // Mouse within window
        if (viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1)
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, MouseRaycastDistance, MouseLayerMask))
            {
                Vector3Int cell = HexMap.Grid.WorldToCell(new Vector3(hit.point.x, 0, hit.point.z));
                if (HexMap.Hexagons.ContainsKey(cell))
                {
                    IsHoveringOverCell = true;
                    CellHoveringOver = cell;

                    Vector3 previewPosition = HexMap.Hexagons[cell].CentreOfFaceWorld;
                    previewPosition.y += 0.001f;
                    HoverCellPreview.position = previewPosition;
                }
            }
        }

        HoverCellPreview.gameObject.SetActive(IsHoveringOverCell);
    }

    private void OnDrawGizmos()
    {
        if (IsHoveringOverCell)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(HexMap.Hexagons[CellHoveringOver].CentreOfFaceWorld, 0.25f);
        }
    }
}
