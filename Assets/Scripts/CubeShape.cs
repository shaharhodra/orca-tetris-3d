using UnityEngine;

public class CubeShape : MonoBehaviour
{
    // Reference to the TouchToSpawn script
    private TouchToSpawn touchToSpawn;
    [Tooltip("Time in seconds between each downward movement")]
    public float fallInterval = 0.5f;
    
    [Tooltip("Distance to move down each step")]
    public float stepSize = 1f;
    
    [Tooltip("Time in seconds before starting to fall")]
    public float startDelay = 1.0f;
    
    private float timer = 0f;
    private bool isGrounded = false;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool canFall = false;
    private bool shouldStartFalling = false;
    private ConnectedCubesManager cubesManager;

    // Check if the TouchToSpawn timer has finished
    private bool HasTimerReachedZero()
    {
        if (touchToSpawn == null)
        {
            // If we couldn't find TouchToSpawn, try to find it again
            touchToSpawn = FindObjectOfType<TouchToSpawn>();
            if (touchToSpawn == null) return false;
        }
        
        // Alternative way to check if timer is done
        // We'll use reflection to access the private field if needed
        var timerField = touchToSpawn.GetType().GetField("isTimerRunning", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
            
        if (timerField != null)
        {
            bool isRunning = (bool)timerField.GetValue(touchToSpawn);
            return !isRunning;
        }
        
        return false;
    }

    void Start()
    {
        // Check if this is the group parent (has children)
        if (transform.childCount > 0)
        {
            // This is the group parent, initialize for group behavior
            InitializeGroupBehavior();
        }
        else
        {
            // This is an individual cube, disable this component
            enabled = false;
            return;
        }
    }
    
    private void InitializeGroupBehavior()
    {
        // Disable physics since we're handling movement manually
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Destroy(rb);
        }
        
        // Make sure we have a collider
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        // Initialize target position
        targetPosition = transform.position;
        shouldStartFalling = true; // Start falling immediately for the group
        
        // Find the ConnectedCubesManager
        cubesManager = FindObjectOfType<ConnectedCubesManager>();
        if (cubesManager == null)
        {
            Debug.LogError("ConnectedCubesManager not found in the scene!");
        }
    }
    
    void Update()
    {
        if (isGrounded) return;
        
        // For the group, we don't need to wait for timer
        if (!shouldStartFalling) return;
        
        // Timer has reached zero, start the falling sequence
        timer += Time.deltaTime;
        
        // Only try to move when timer reaches fallInterval
        if (timer >= fallInterval)
        {
            // If we're not already moving, start moving down
            if (!isMoving)
            {
                TryMoveDown();
            }
            
            // Move towards target position if moving
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 10f);
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                    timer = 0f; // Reset timer after completing the move
                }
            }
        }
    }
    
    void TryMoveDown()
    {
        // Calculate the bottom of the object
        float objectHeight = GetComponent<Collider>().bounds.size.y;
        float rayLength = stepSize + (objectHeight / 2f);
        
        // Cast a ray down to detect surfaces
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayLength, ~LayerMask.GetMask("Ignore Raycast")))
        {
            // Calculate exact position above the hit point
            float targetY = hit.point.y + (objectHeight / 2f);
            targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);
            isMoving = true;
            
            // If we're very close to the surface, just snap to it
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                isMoving = false;
                isGrounded = true;
                // Group falling is now handled by the parent object
                Debug.Log("Shape landed on " + hit.collider.gameObject.name);
            }
        }
        else
        {
            // No surface detected, move down normally
            targetPosition = transform.position + Vector3.down * stepSize;
            isMoving = true;
        }
    }
}
