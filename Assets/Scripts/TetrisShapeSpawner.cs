using UnityEngine;
using System.Collections.Generic;

public class TetrisShapeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject cubePrefab;
    public Transform spawnPoint;
    public float spawnDelay = 1f;
    
    [Header("Spawn Position")]
    [Tooltip("If true, shapes will be centered around spawn point. If false, they'll start from the spawn point.")]
    public bool centerShapes = true;
    [Tooltip("If true, shapes will be aligned to grid.")]
    public bool snapToGrid = true;
    [Tooltip("Grid size for snapping. Only used if snapToGrid is true.")]
    public float gridSize = 1.0f;
    
    [Header("Shape Settings")]
    public int maxWidth = 7;
    public int maxHeight = 4;
    public float cubeSize = 1f;
    [Tooltip("Number of cubes in the shape (2-4)")]
    public int cubesInShape = 3; // Number of cubes in a single shape
    
    [Header("Color Settings")]
    [Tooltip("Possible colors for the shapes")]
    public Color[] possibleColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };
    
    [Tooltip("Color for player-created shapes")]
    public Color playerShapeColor = Color.white;
    
    private List<GameObject> currentShape = new List<GameObject>();
    private TouchToSpawn touchToSpawn;
    
    void Start()
    {
        // Get reference to TouchToSpawn component
        touchToSpawn = FindObjectOfType<TouchToSpawn>();
        if (touchToSpawn == null)
        {
            Debug.LogError("TouchToSpawn component not found in the scene!");
        }
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.position = new Vector3(0, 10, 0);
        }
        
        // Don't spawn shapes automatically at start
        // The TouchToSpawn will handle the timer and call SpawnNewShape when needed
    }
    
    public void SpawnNewShape()
    {
        Debug.Log("Spawning new shape...");
        
        // Create the shape parent object
        GameObject shapeParent = new GameObject("TetrisShape_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        
        // Calculate spawn position
        Vector3 spawnPosition = spawnPoint.position;
        
        // Snap to grid if enabled
        if (snapToGrid)
        {
            spawnPosition.x = Mathf.Round(spawnPosition.x / gridSize) * gridSize;
            spawnPosition.y = Mathf.Round(spawnPosition.y / gridSize) * gridSize;
        }
        
        // Set the shape position
        shapeParent.transform.position = spawnPosition;
        
        // Generate a random shape
        bool[,] shape = GenerateRandomShape();
        
        // Choose a single random color for all cubes in this shape
        Color shapeColor = possibleColors.Length > 0 ? 
            possibleColors[Random.Range(0, possibleColors.Length)] : 
            Color.white;
        
        // Create cubes based on the shape
        for (int y = 0; y < maxHeight; y++)
        {
            for (int x = 0; x < maxWidth; x++)
            {
                if (shape[x, y])
                {
                    // Calculate local position relative to shape center
                    Vector3 localPos = new Vector3(
                        (x - maxWidth/2f) * cubeSize,
                        (-y + maxHeight/2f) * cubeSize,
                        0);
                        
                    // Create cube at calculated local position
                    GameObject cube = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity, shapeParent.transform);
                    cube.transform.localPosition = localPos;
                    
                    // Set the same color for all cubes in this shape
                    var renderer = cube.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = shapeColor;
                    }
                    
                    currentShape.Add(cube);
                }
            }
        }
        
        // Add CubeShape component to the shape
        CubeShape cubeShape = shapeParent.AddComponent<CubeShape>();
        
        // Start the shape falling after a delay
        StartCoroutine(StartFallingAfterDelay(cubeShape, spawnDelay));
    }
    
    private bool[,] GenerateRandomShape()
    {
        bool[,] shape = new bool[maxWidth, maxHeight];
        
        // Start with a single cube in the middle of the width
        int startX = maxWidth / 2;
        int startY = 0; // Start at the top
        shape[startX, startY] = true;
        
        // Use the specified number of cubes
        int numCubes = Mathf.Clamp(cubesInShape, 2, 4);
        Debug.Log($"Generating shape with {numCubes} cubes");
        
        // Add adjacent cubes
        int addedCubes = 1;
        int attempts = 0;
        const int maxAttempts = 50; // Prevent infinite loops
        
        while (addedCubes < numCubes && attempts < maxAttempts)
        {
            attempts++;
            // Find all possible positions to add a cube
            List<Vector2Int> possiblePositions = new List<Vector2Int>();
            
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    if (shape[x, y])
                    {
                        // Check all 4 directions
                        TryAddPosition(x + 1, y, shape, possiblePositions);
                        TryAddPosition(x - 1, y, shape, possiblePositions);
                        TryAddPosition(x, y + 1, shape, possiblePositions);
                        TryAddPosition(x, y - 1, shape, possiblePositions);
                    }
                }
            }
            
            // If no possible positions, try again
            if (possiblePositions.Count == 0)
                continue;
                
            // Add a random position
            Vector2Int pos = possiblePositions[Random.Range(0, possiblePositions.Count)];
            shape[pos.x, pos.y] = true;
            addedCubes++;
            
            // Reset attempts after successful addition
            attempts = 0;
        }
        
        Debug.Log($"Generated shape with {addedCubes} cubes after {attempts} attempts");
        return shape;
    }
    
    private void TryAddPosition(int x, int y, bool[,] shape, List<Vector2Int> positions)
    {
        if (x >= 0 && x < maxWidth && y >= 0 && y < maxHeight && !shape[x, y])
        {
            positions.Add(new Vector2Int(x, y));
        }
    }
    
    private void CheckAndAddPosition(int x, int y, bool[,] shape, List<Vector2Int> positions)
    {
        if (x >= 0 && x < maxWidth && y >= 0 && y < maxHeight && !shape[x, y])
        {
            positions.Add(new Vector2Int(x, y));
        }
    }
    
    private void ClearCurrentShape()
    {
        foreach (var cube in currentShape)
        {
            if (cube != null)
                Destroy(cube);
        }
        currentShape.Clear();
    }
    
    private System.Collections.IEnumerator StartFallingAfterDelay(CubeShape cubeShape, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cubeShape != null)
        {
            cubeShape.enabled = true;
            cubeShape.StartFalling();
        }
    }
}
