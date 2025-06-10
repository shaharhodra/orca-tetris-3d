using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TouchToSpawn : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("The prefab to instantiate when touching an object with the specified tag")]
    public GameObject prefabToSpawn;
    
    [Tooltip("List of possible shapes to spawn randomly")]
    public GameObject[] randomShapes;
    
    [Tooltip("The tag of the object that can be touched to spawn the prefab")]
    public string touchableTag = "Touchable";
    
    [Tooltip("Offset from the touch position where the prefab will be instantiated")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Timer Settings")]
    [Tooltip("Duration of the countdown in seconds")]
    public float countdownDuration = 5f;
    
    [Tooltip("Text component to display the countdown")]
    public TMP_Text countdownText;
    
    private float currentTime;
    private bool isTimerRunning = false;
    private bool canSpawn = false;

    private ConnectedCubesManager cubesManager;

    private void Start()
    {
        // Get or create the ConnectedCubesManager
        cubesManager = FindObjectOfType<ConnectedCubesManager>();
        if (cubesManager == null)
        {
            GameObject managerObj = new GameObject("ConnectedCubesManager");
            cubesManager = managerObj.AddComponent<ConnectedCubesManager>();
        }
        
        // Subscribe to the shape landed event
        cubesManager.OnShapeLanded += OnShapeLanded;
        
        // Start the countdown immediately when the game begins
        StartCountdown();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (cubesManager != null)
        {
            cubesManager.OnShapeLanded -= OnShapeLanded;
        }
    }

    void Update()
    {
        // Handle timer
        if (isTimerRunning)
        {
            // Update the timer
            currentTime -= Time.deltaTime;
            
            // Update the countdown text
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(currentTime).ToString();
            }
            
            // Check if timer has finished
            if (currentTime <= 0)
            {
                TimerFinished();
            }
        }
        
        // Check for touches or mouse clicks if we can spawn
        if (canSpawn && isTimerRunning)
        {
            // Handle touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0); // Get the first touch

                if (touch.phase == TouchPhase.Began)
                {
                    HandleSpawnInput(touch.position);
                }
            }
            // Handle mouse input (for testing in editor)
            else if (Input.GetMouseButtonDown(0)) // Left mouse button
            {
                HandleSpawnInput(Input.mousePosition);
            }
        }
    }
    
    public void StartCountdown()
    {
        Debug.Log("Starting countdown");
        currentTime = countdownDuration;
        isTimerRunning = true;
        canSpawn = true;
        canSpawn = true;  // Enable spawning when countdown starts
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = Mathf.CeilToInt(currentTime).ToString();
        }
    }
    
    // Public method to check if the timer is still running
    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }
    
    private void TimerFinished()
    {
        if (!isTimerRunning) return; // Prevent multiple triggers
        
        Debug.Log("Timer finished, grouping cubes");
        isTimerRunning = false;
        
        // Check if any cubes were placed
        if (cubesManager != null && cubesManager.GetCubeCount() == 0)
        {
            // No cubes placed, spawn a random shape
            SpawnRandomShape();
        }
        else
        {
            canSpawn = false;  // Disable spawning when time's up
            
            if (countdownText != null)
            {
                countdownText.text = "Time's Up!";
            }
            
            // Notify ConnectedCubesManager that time is up
            if (cubesManager != null)
            {
                cubesManager.OnGameTimeEnded();
            }
            else
            {
                Debug.LogError("No ConnectedCubesManager found!");
            }
        }
    }
    
    private void OnShapeLanded()
    {
        Debug.Log("Shape landed, preparing for new shape");
        
        // Reset the manager for new shape
        if (cubesManager != null)
        {
            cubesManager.ClearAllCubes();
        }
        
        // Reset the timer
        StartCountdown();
        
        // Enable spawning for new shape
        canSpawn = true;
        isTimerRunning = true;
        
        if (countdownText != null)
        {
            countdownText.text = Mathf.CeilToInt(currentTime).ToString();
        }
    }
    
    // Handle both touch and mouse input for spawning
    private void HandleSpawnInput(Vector2 screenPosition)
    {
        Debug.Log("HandleSpawnInput called");
        
        // Create a ray from the screen position
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        Debug.Log("Casting ray from " + screenPosition);
        
        // Check if the ray hits an object with the specified tag
        if (!Physics.Raycast(ray, out hit))
        {
            Debug.Log("No hit detected");
            return;
        }

        Debug.Log("Hit: " + hit.collider.name + " with tag: " + hit.collider.tag);
        
        if (!hit.collider.CompareTag(touchableTag))
        {
            Debug.Log("Hit object is not touchable");
            return;
        }

        Debug.Log("Hit touchable object");
        
        // Use the existing manager reference
        if (cubesManager == null)
        {
            Debug.LogError("cubesManager is null!");
            return;
        }

        int cubeCount = cubesManager.GetCubeCount();
        Debug.Log("Current cube count: " + cubeCount);

        // If there are no cubes yet, allow placing the first cube anywhere
        if (cubeCount == 0)
        {
            Debug.Log("Placing first cube");
            // Instantiate the first cube at the hit point
            Vector3 firstCubePosition = hit.point + spawnOffset;
            // Round to nearest whole number for grid alignment
            firstCubePosition = new Vector3(
                Mathf.Round(firstCubePosition.x),
                Mathf.Round(firstCubePosition.y),
                Mathf.Round(firstCubePosition.z)
            );
            Debug.Log("First cube position: " + firstCubePosition);
            SpawnCube(cubesManager, firstCubePosition, true);
            return;
        }

        // Calculate grid-aligned position for the touch point using whole numbers
        Vector3 touchGridPos = new Vector3(
            Mathf.Round(hit.point.x),
            Mathf.Round(hit.point.y),
            Mathf.Round(hit.point.z)
        );
        
        Debug.Log("Touch grid position: " + touchGridPos);
        
        // Check all existing cubes to see if the touch is adjacent to any of them
        bool isAdjacentToAnyCube = false;
        bool positionOccupied = false;
        Vector3 spawnPosition = touchGridPos;
        
        Debug.Log("Checking position occupation...");
            
        // First check if the position is already occupied
        Debug.Log("Checking for colliders at " + spawnPosition);
        Collider[] colliders = Physics.OverlapBox(spawnPosition, Vector3.one * 0.4f);
        Debug.Log("Found " + colliders.Length + " colliders at position");
        
        foreach (var collider in colliders)
        {
            if (collider != null)
            {
                Debug.Log("Found collider: " + collider.name + " with tag: " + collider.tag);
                if (collider.CompareTag(touchableTag))
                {
                    positionOccupied = true;
                    Debug.Log("Position occupied by: " + collider.name);
                    break;
                }
            }
        }
            
        if (positionOccupied)
        {
            Debug.Log("Position already occupied - aborting");
            return;
        }
        
        Debug.Log("Position is free. Checking adjacency...");
            
        // Check all existing cubes
        foreach (GameObject cube in cubesManager.GetAllCubes())
        {
            if (cube == null) continue;
            
            Vector3 cubePos = new Vector3(
                Mathf.Round(cube.transform.position.x),
                Mathf.Round(cube.transform.position.y),
                Mathf.Round(cube.transform.position.z)
            );
            
            // Calculate the distance on each axis
            float xDist = Mathf.Abs(touchGridPos.x - cubePos.x);
            float yDist = Mathf.Abs(touchGridPos.y - cubePos.y);
            float zDist = Mathf.Abs(touchGridPos.z - cubePos.z);
            
            // Cubes are adjacent if they share two coordinates and the third differs by exactly 1
            int matchingAxes = 0;
            int differentByOne = 0;
            
            if (Mathf.Approximately(xDist, 0f)) matchingAxes++;
            else if (Mathf.Approximately(xDist, 1f)) differentByOne++;
            
            if (Mathf.Approximately(yDist, 0f)) matchingAxes++;
            else if (Mathf.Approximately(yDist, 1f)) differentByOne++;
            
            if (Mathf.Approximately(zDist, 0f)) matchingAxes++;
            else if (Mathf.Approximately(zDist, 1f)) differentByOne++;
            
            // If two axes match and one differs by 1, they're adjacent
            if (matchingAxes == 2 && differentByOne == 1)
            {
                isAdjacentToAnyCube = true;
                Debug.Log($"Adjacent to cube at {cubePos} (touch at {touchGridPos})");
                break;
            }
        }
        
        if (!isAdjacentToAnyCube)
        {
            Debug.Log("Can only place cubes adjacent to existing cubes");
            return;
        }
        
        Debug.Log("Position is valid and adjacent. Spawning cube at: " + spawnPosition);
        SpawnCube(cubesManager, spawnPosition, false);
        Debug.Log($"Successfully placed adjacent cube at {spawnPosition}");
    }
    
    void SpawnRandomShape()
    {
        if (randomShapes == null || randomShapes.Length == 0)
        {
            Debug.LogError("No random shapes assigned in the inspector!");
            return;
        }
        
        // Select a random shape from the array
        int randomIndex = Random.Range(0, randomShapes.Length);
        GameObject randomShapePrefab = randomShapes[randomIndex];
        
        // Spawn the shape at position (0, 10, 0) to make it fall from above
        Vector3 spawnPosition = new Vector3(0, 10, 0);
        
        // Instantiate the shape
        GameObject newShape = Instantiate(randomShapePrefab, spawnPosition, Quaternion.identity);
        
        // Make sure it has a CubeShape component
        CubeShape cubeShape = newShape.GetComponent<CubeShape>();
        if (cubeShape == null)
        {
            cubeShape = newShape.AddComponent<CubeShape>();
        }
        
        // Initialize the shape
        cubeShape.shouldStartFalling = true;
        
        // Reset the timer for the next shape
        StartCountdown();
    }
    
    void SpawnCube(ConnectedCubesManager manager, Vector3 position, bool isFirstCube)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("Cannot spawn cube: prefabToSpawn is not assigned in the inspector!");
            return;
        }

        if (manager == null)
        {
            Debug.LogError("Cannot spawn cube: ConnectedCubesManager is null!");
            return;
        }

        Debug.Log($"Attempting to spawn cube at {position} (first cube: {isFirstCube})");
        
        try
        {
            // Instantiate the prefab at the calculated position
            GameObject newCube = Instantiate(prefabToSpawn, position, Quaternion.identity);
            
            if (newCube == null)
            {
                Debug.LogError("Failed to instantiate cube prefab!");
                return;
            }
            
            // Set initial color to gray
            Renderer cubeRenderer = newCube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = Color.gray;
            }
            else
            {
                Debug.LogWarning("New cube has no Renderer component!");
            }
            
            // Register the new cube with the manager
            manager.RegisterCube(newCube, isFirstCube);
            
            Debug.Log($"Successfully spawned {prefabToSpawn.name} at {position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning cube: {e.Message}");
            Debug.LogException(e);
        }
    }
    
}
