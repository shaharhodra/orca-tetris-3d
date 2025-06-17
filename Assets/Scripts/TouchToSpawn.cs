using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TouchToSpawn : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("The prefab to instantiate when touching an object with the specified tag")]
    public GameObject prefabToSpawn;
    
    [Tooltip("The tag of the object that can be touched to spawn the prefab")]
    public string touchableTag = "Touchable";
    
    [Tooltip("Offset from the touch position where the prefab will be instantiated")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Timer Settings")]
    [Tooltip("Duration of the countdown in seconds")]
    public float countdownDuration = 5f;
    
    [Tooltip("Text component to display the countdown")]
    public TMP_Text countdownText;
    public Countdown3DDisplay countdown3DDisplay;  // Reference to the 3D countdown display
    
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
            
            // Update the countdown display
            int countdownValue = Mathf.CeilToInt(currentTime);
            
            if (countdown3DDisplay != null)
            {
                countdown3DDisplay.ShowNumber(countdownValue);
            }
            else if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = countdownValue.ToString();
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
        
        int countdownValue = Mathf.CeilToInt(currentTime);
        
        if (countdown3DDisplay != null)
        {
            countdown3DDisplay.gameObject.SetActive(true);
            countdown3DDisplay.ShowNumber(countdownValue);
        }
        else if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = countdownValue.ToString();
        }
    }
    
    // Public method to check if the timer is still running
    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }
    
    public void StopTimer()
    {
        isTimerRunning = false;
        canSpawn = false;
        if (countdownText != null) countdownText.text = "Timer Stopped";
    }
    
    public void ResetTimer()
    {
        // Stop any existing timer
        CancelInvoke("StartCountdown");
        
        // Reset the timer state
        isTimerRunning = false;
        canSpawn = false;
        
        // Start a new countdown after a short delay
        Invoke("StartCountdown", 1f);
        
        Debug.Log("Timer reset - new countdown starting soon");
    }
    
    private void TimerFinished()
    {
        if (!isTimerRunning) return; // Prevent multiple triggers
        
        Debug.Log("Timer finished, finalizing player shape or spawning random shape");
        isTimerRunning = false;
        
        // Check if player has placed any cubes
        if (cubesManager != null && cubesManager.GetCubeCount() > 0)
        {
            // Player has drawn a shape - group it and make it fall
            FinalizePlayerShape();
        }
        else
        {
            // Player didn't draw anything - spawn a random shape
            TetrisShapeSpawner shapeSpawner = FindObjectOfType<TetrisShapeSpawner>();
            if (shapeSpawner != null)
            {
                shapeSpawner.SpawnNewShape();
                
                if (countdownText != null)
                {
                    countdownText.text = "Shape Spawned!";
                }
            }
            else
            {
                Debug.LogError("TetrisShapeSpawner not found in the scene!");
            }
        }
    }
    
    private void FinalizePlayerShape()
    {
        Debug.Log("Finalizing player shape");
        
        // Create a parent object for the shape
        GameObject shapeParent = new GameObject("PlayerShape");
        CubeShape cubeShape = shapeParent.AddComponent<CubeShape>();
        cubeShape.shouldStartFalling = true;
        
        // Get all cubes from the manager and parent them to the shape
        var cubes = cubesManager.GetAllCubes();
        foreach (GameObject cube in cubes)
        {
            if (cube != null)
            {
                cube.transform.SetParent(shapeParent.transform);
                // Change color to indicate it's now a falling shape
                Renderer renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.blue; // Or any color you prefer
                }
            }
        }
        
        // Clear the manager for new shapes
        cubesManager.ClearAllCubes(false);
        
        // Start the shape falling
        cubeShape.StartFalling();
        
        if (countdownText != null)
        {
            countdownText.text = "Shape Falling!";
        }
    }
    
    public void OnShapeLanded()
    {
        Debug.Log("Shape landed - resetting timer and enabling spawning");
        
        // Reset the timer immediately
        currentTime = countdownDuration;
        isTimerRunning = true;
        canSpawn = true;
        
        // Update the UI
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = Mathf.CeilToInt(currentTime).ToString();
        }
        
        Debug.Log("Timer reset and ready for next shape");
    }
    
    // Handle both touch and mouse input for spawning
    private void HandleSpawnInput(Vector2 screenPosition)
    {
        Debug.Log("HandleSpawnInput called");
        
        // Check if we can spawn new cubes
        if (!canSpawn || !isTimerRunning)
        {
            Debug.Log("Cannot spawn right now - waiting for timer or shape to land");
            return;
        }
        
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

        // Calculate grid position
        Vector3 gridPosition = hit.point + spawnOffset;
        gridPosition = new Vector3(
            Mathf.Round(gridPosition.x),
            Mathf.Round(gridPosition.y),
            Mathf.Round(gridPosition.z)
        );
        
        // Check if position is already occupied
        if (cubesManager.IsPositionOccupied(gridPosition))
        {
            Debug.Log("Position already occupied");
            return;
        }
        
        // If there are no cubes yet, allow placing the first cube
        if (cubesManager.GetCubeCount() == 0)
        {
            Debug.Log("Placing first cube at " + gridPosition);
            SpawnCube(cubesManager, gridPosition, true);
            return;
        }

        // Calculate grid-aligned position for the touch point using whole numbers
        Vector3 touchGridPos = new Vector3(
            Mathf.Round(hit.point.x),
            Mathf.Round(hit.point.y),
            Mathf.Round(hit.point.z)
        );
        
        Debug.Log("Touch grid position: " + touchGridPos);
        
        // Find the active shape (if any)
        CubeShape activeShape = FindObjectOfType<CubeShape>();
        
        // Check all existing cubes to see if the touch is adjacent to any of them
        bool isAdjacentToAnyCube = false;
        bool positionOccupied = false;
        Vector3 spawnPosition = touchGridPos;
        
        // If we have an active shape, use it as the parent for new cubes
        if (activeShape != null)
        {
            // Check if the position is already occupied by a child of the active shape
            foreach (Transform child in activeShape.transform)
            {
                if (Vector3.Distance(child.position, spawnPosition) < 0.1f)
                {
                    positionOccupied = true;
                    break;
                }
            }
        }
        
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
        
        // If we have an active shape, parent the new cube to it
        if (activeShape != null)
        {
            // Spawn the cube as a child of the active shape
            GameObject newCube = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, activeShape.transform);
            
            // Set initial color to match the shape
            Renderer cubeRenderer = newCube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material.color = Color.gray;
            }
            
            // Register the cube with the manager
            cubesManager.RegisterCube(newCube, false);
            Debug.Log($"Successfully placed adjacent cube at {spawnPosition} as part of existing shape");
        }
        else
        {
            // If no active shape, spawn a new one with this cube
            SpawnCube(cubesManager, spawnPosition, true);
            Debug.Log($"Successfully started new shape at {spawnPosition}");
        }
    }
    
    void SpawnCubeFromTop()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogError("No prefab assigned in the inspector!");
            return;
        }
        
        // Spawn the cube at position (0, 10, 0) to make it fall from above
        Vector3 spawnPosition = new Vector3(0, 10, 0);
        
        // Instantiate the cube
        GameObject newCube = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        
        // Make sure it has a CubeShape component
        CubeShape cubeShape = newCube.GetComponent<CubeShape>();
        if (cubeShape == null)
        {
            cubeShape = newCube.AddComponent<CubeShape>();
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
