using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ConnectedCubesManager : MonoBehaviour
{
    public static ConnectedCubesManager Instance { get; private set; }
    
    [Tooltip("Minimum distance between cubes to consider them connected")]
    public float connectionDistance = 1.1f;
    
    private List<GameObject> allCubes = new List<GameObject>();
    private GameObject firstCube = null;
    private GameObject cubesParent = null;
    private bool gameEnded = false;
    private bool isFalling = false;
    private Vector3 fallTargetPosition;
    private float fallSpeed = 10f;
    
    // Event for when a shape has finished landing
    public System.Action OnShapeLanded;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void RegisterCube(GameObject cube, bool isFirstCube = false)
    {
        if (!allCubes.Contains(cube))
        {
            allCubes.Add(cube);
            
            if (isFirstCube)
            {
                firstCube = cube;
            }
        }
    }
    
    public int GetCubeCount()
    {
        return allCubes.Count;
    }
    
    public List<GameObject> GetAllCubes()
    {
        return new List<GameObject>(allCubes);
    }
    
    public bool IsPositionOccupied(Vector3 position)
    {
        foreach (GameObject cube in allCubes)
        {
            if (cube != null && Vector3.Distance(cube.transform.position, position) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }
    
    public void ClearAllCubes(bool clearAll = true)
    {
        Debug.Log("Resetting cube manager for new shape");
        
        if (clearAll)
        {
            // Clear all cubes
            foreach (GameObject cube in allCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
        }
        
        // Create a new list for the next shape
        allCubes = new List<GameObject>();
        firstCube = null;
        cubesParent = null;
        
        // Reset game state
        gameEnded = false;
        isFalling = false;
    }
    
    public GameObject GetFirstCube()
    {
        return firstCube;
    }
    
    public void UnregisterCube(GameObject cube)
    {
        allCubes.Remove(cube);
    }
    
    public void OnGameTimeEnded()
    {
        if (gameEnded) return;
        gameEnded = true;
        
        // Create a parent object for all cubes if it doesn't exist
        if (cubesParent == null)
        {
            cubesParent = new GameObject("AllCubes");
            // Add CubeShape component to the parent
            var groupCubeShape = cubesParent.AddComponent<CubeShape>();
            
            // Configure the cube shape
            groupCubeShape.fallInterval = 0.5f;
            groupCubeShape.stepSize = 0.5f;
        }
        
        // Generate a random color
        Color randomColor = new Color(
            Random.Range(0.2f, 0.9f),
            Random.Range(0.2f, 0.9f),
            Random.Range(0.2f, 0.9f)
        );
        
        // Make all cubes children of the parent and update their components
        foreach (GameObject cube in allCubes.ToArray())
        {
            if (cube != null)
            {
                // Remove CubeShape from individual cubes
                var cubeShape = cube.GetComponent<CubeShape>();
                if (cubeShape != null)
                {
                    Destroy(cubeShape);
                }
                
                // Ensure the cube has a collider
                if (cube.GetComponent<Collider>() == null)
                {
                    cube.AddComponent<BoxCollider>();
                }
                
                // Ensure the cube has a kinematic rigidbody
                Rigidbody rb = cube.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = cube.AddComponent<Rigidbody>();
                }
                rb.isKinematic = true;
                
                // Change parent
                cube.transform.SetParent(cubesParent.transform);
                
                // Change color
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                if (cubeRenderer != null)
                {
                    cubeRenderer.material.color = randomColor;
                }
            }
        }
        
        // Start group falling
        StartGroupFall();
        
        Debug.Log($"Game ended! All {allCubes.Count} cubes have been grouped and colored.");
    }
    
    public Vector3? FindNearestConnectionPoint(Vector3 position)
    {
        if (allCubes.Count == 0) return null;
        
        Vector3 nearestPoint = Vector3.zero;
        float minDistance = float.MaxValue;
        
        // Check all cubes for the nearest connection point
        foreach (var cube in allCubes)
        {
            if (cube == null) continue;
            
            // Check all 6 faces of the cube
            Vector3[] faceDirections = {
                Vector3.right, Vector3.left,
                Vector3.up, Vector3.down,
                Vector3.forward, Vector3.back
            };
            
            foreach (var dir in faceDirections)
            {
                Vector3 facePosition = cube.transform.position + dir;
                float distance = Vector3.Distance(position, facePosition);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPoint = facePosition;
                }
            }
        }
        
        return minDistance < connectionDistance ? (Vector3?)nearestPoint : null;
    }
    
    private void Update()
    {
        if (isFalling && cubesParent != null)
        {
            // Move the entire group down
            Vector3 newPosition = Vector3.MoveTowards(
                cubesParent.transform.position,
                fallTargetPosition,
                fallSpeed * Time.deltaTime
            );
            
            cubesParent.transform.position = newPosition;
            
            // Check if we've reached the target position
            if (Vector3.Distance(cubesParent.transform.position, fallTargetPosition) < 0.01f)
            {
                cubesParent.transform.position = fallTargetPosition;
                isFalling = false;
                SnapGroupToGrid();
                // Notify that the shape has landed
                OnShapeLanded?.Invoke();
            }
        }
    }
    
    void SnapGroupToGrid()
    {
        if (cubesParent == null) return;
        
        // Snap all cubes to grid
        foreach (Transform child in cubesParent.transform)
        {
            if (child != null)
            {
                Vector3 pos = child.position;
                child.position = new Vector3(
                    Mathf.Round(pos.x * 2) / 2f, // Snap to 0.5 increments
                    Mathf.Round(pos.y * 2) / 2f, // Snap to 0.5 increments
                    Mathf.Round(pos.z * 2) / 2f  // Snap to 0.5 increments
                );
            }
        }
        
        Debug.Log("Group snapped to grid");
    }
    
    void StartGroupFall()
    {
        if (cubesParent == null || allCubes.Count == 0) return;
        
        // Find the lowest point in the group
        float lowestY = allCubes.Min(cube => cube.transform.position.y);
        
        // Cast a box to find the ground below the group
        Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f); // Small box for precise detection
        float maxDistance = 20f; // Maximum distance to check for ground
        
        // Calculate the start position for the ray (slightly above the lowest point)
        Vector3 rayStart = new Vector3(
            cubesParent.transform.position.x,
            lowestY + 0.1f, // Start slightly above the lowest point
            cubesParent.transform.position.z
        );
        
        RaycastHit hit;
        if (Physics.BoxCast(
            rayStart,
            boxSize / 2f,
            Vector3.down,
            out hit,
            Quaternion.identity,
            maxDistance,
            ~0, // Check all layers
            QueryTriggerInteraction.Ignore))
        {
            // Calculate the bottom of the group
            float objectBottomY = lowestY - 0.5f; // Half cube height
            
            // Calculate how far we need to move up to be exactly on the hit point
            float distanceToGround = objectBottomY - hit.point.y;
            
            if (Mathf.Abs(distanceToGround) > 0.01f) // Only move if we need to
            {
                // Move the group up by the distance to the ground
                fallTargetPosition = cubesParent.transform.position - new Vector3(0, distanceToGround, 0);
                isFalling = true;
                Debug.Log($"Moving group to Y: {fallTargetPosition.y} (distance: {distanceToGround})");
            }
            else
            {
                // Already on the ground, just snap to grid
                SnapGroupToGrid();
                // Notify that the shape has landed
                OnShapeLanded?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning("No ground found below the group!");
            // If no ground found, just snap to grid at current position
            SnapGroupToGrid();
        }
    }
}
