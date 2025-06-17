using UnityEngine;
using System.Collections.Generic;

public class TetrisShapeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject cubePrefab;
    public Transform spawnPoint;
    public float spawnDelay = 1f;
    
    [Header("Shape Settings")]
    public int maxWidth = 7;
    public int maxHeight = 4;
    public float cubeSize = 1f;
    public int minShapesInGroup = 2;
    public int maxShapesInGroup = 4;
    public float shapeSpacing = 2f; // Space between shapes in the group
    
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
        Debug.Log("Spawning new shape group...");
        
        // Create the group parent object
        GameObject groupParent = new GameObject("ShapeGroup_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        groupParent.transform.position = spawnPoint.position;
        
        // Determine how many shapes to create in this group
        int numShapes = Random.Range(minShapesInGroup, maxShapesInGroup + 1);
        Debug.Log($"Creating {numShapes} shapes in group");
        float totalWidth = 0f;
        
        // Create multiple shapes in the group
        for (int i = 0; i < numShapes; i++)
        {
            // Generate a random shape
            bool[,] shape = GenerateRandomShape();
            
            // Create a parent for this shape at the exact spawn point
            GameObject shapeParent = new GameObject($"TetrisShape_{i}");
            shapeParent.transform.position = spawnPoint.position; // Set exact spawn position
            shapeParent.transform.SetParent(groupParent.transform);
            

            
            // Calculate position for this shape in the group
            float shapeWidth = maxWidth * cubeSize;
            float xPos = totalWidth + (i * (shapeWidth + shapeSpacing));
            
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
                        currentShape.Add(cube);
                    }
                }
            }
            
            // Update total width for next shape
            totalWidth += shapeWidth + shapeSpacing;
        }
        
        // Center the group horizontally around spawn point
        groupParent.transform.position = spawnPoint.position - new Vector3(totalWidth / 2, 0, 0);
        
        // Add CubeShape component to the group parent
        CubeShape cubeShape = groupParent.AddComponent<CubeShape>();
        
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
        
        // Determine number of cubes in this shape (between 2 and 4 for Tetris-like shapes)
        int numCubes = Random.Range(2, 5);
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
