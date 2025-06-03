using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            // Calculate spawn position (hit point + offset)
            Vector3 spawnPosition = hit.point + spawnOffset;
            
            // Instantiate the prefab at the hit position
            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            
            Debug.Log("Spawned " + prefabToSpawn.name + " at " + spawnPosition);
        }
    }
}
