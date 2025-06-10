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
    
    private float currentTime;
    private bool isTimerRunning = false;
    private bool canSpawn = false;

    private void Start()
    {
        // Start the countdown immediately when the game begins
        StartCountdown();
    }

    void Update()
    {
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
        if (canSpawn)
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
        currentTime = countdownDuration;
        isTimerRunning = true;
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
        isTimerRunning = false;
        canSpawn = false;  // Disable spawning when time's up
        
        if (countdownText != null)
        {
            countdownText.text = "Time's Up!";
        }
        
        // Notify ConnectedCubesManager that time is up
        ConnectedCubesManager manager = FindObjectOfType<ConnectedCubesManager>();
        if (manager != null)
        {
            manager.OnGameTimeEnded();
        }
    }
    
    // Handle both touch and mouse input for spawning
    private void HandleSpawnInput(Vector2 screenPosition)
    {
        // Create a ray from the screen position
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Check if the ray hits an object with the specified tag
        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag(touchableTag))
        {
            // Get or create the ConnectedCubesManager
            ConnectedCubesManager manager = FindObjectOfType<ConnectedCubesManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("ConnectedCubesManager");
                manager = managerObj.AddComponent<ConnectedCubesManager>();
            }

            // If there are no cubes yet, allow placing the first cube anywhere
            if (manager.GetCubeCount() == 0)
            {
                // Instantiate the first cube at the hit point
                Vector3 firstCubePosition = hit.point + spawnOffset;
                // Round to nearest whole number for grid alignment
                firstCubePosition = new Vector3(
                    Mathf.Round(firstCubePosition.x),
                    Mathf.Round(firstCubePosition.y),
                    Mathf.Round(firstCubePosition.z)
                );
                SpawnCube(manager, firstCubePosition, true);
                return;
            }

            // Calculate grid-aligned position for the touch point using whole numbers
            Vector3 touchGridPos = new Vector3(
                Mathf.Round(hit.point.x),
                Mathf.Round(hit.point.y),
                Mathf.Round(hit.point.z)
            );
            
            // Check all existing cubes to see if the touch is adjacent to any of them
            bool isAdjacentToAnyCube = false;
            bool positionOccupied = false;
            Vector3 spawnPosition = touchGridPos;
            
            // First check if the position is already occupied
            Collider[] colliders = Physics.OverlapBox(spawnPosition, Vector3.one * 0.4f);
            foreach (var collider in colliders)
            {
                if (collider != null && collider.CompareTag(touchableTag))
                {
                    positionOccupied = true;
                    break;
                }
            }
            
            if (positionOccupied)
            {
                Debug.Log("Position already occupied");
                return;
            }
            
            // Check all existing cubes
            foreach (GameObject cube in manager.GetAllCubes())
            {
                if (cube == null) continue;
                
                Vector3 cubePos = new Vector3(
                    Mathf.Round(cube.transform.position.x),
                    Mathf.Round(cube.transform.position.y),
                    Mathf.Round(cube.transform.position.z)
                );
                
                // Check if the touch position is adjacent to this cube
                if (Vector3.Distance(touchGridPos, cubePos) < 1.1f) // Slightly more than 1 to account for floating point errors
                {
                    isAdjacentToAnyCube = true;
                    break;
                }
            }
            
            if (!isAdjacentToAnyCube)
            {
                Debug.Log("Can only place cubes adjacent to existing cubes");
                return;
            }
            
            // If we got here, the position is valid - spawn the cube
            SpawnCube(manager, spawnPosition, false);
            Debug.Log($"Placed adjacent cube at {spawnPosition}");
        }
    }
    
    private void SpawnCube(ConnectedCubesManager manager, Vector3 position, bool isFirstCube)
    {
        // Instantiate the prefab at the calculated position
        GameObject newCube = Instantiate(prefabToSpawn, position, Quaternion.identity);
        
        // Set initial color to gray
        Renderer cubeRenderer = newCube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.material.color = Color.gray;
        }
        
        // Register the new cube with the manager
        manager.RegisterCube(newCube, isFirstCube);
        
        Debug.Log("Spawned " + prefabToSpawn.name + " at " + position);
    }
    
}
