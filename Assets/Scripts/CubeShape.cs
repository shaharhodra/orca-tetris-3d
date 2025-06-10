using UnityEngine;

public class CubeShape : MonoBehaviour
{
    // Reference to the TouchToSpawn script
    private TouchToSpawn touchToSpawn;
    [Tooltip("Time in seconds between each downward movement")]
    public float fallInterval = 0.5f;
    
    [Tooltip("Distance to move down each step")]
    public float stepSize = 1f;
    
    [Tooltip("Speed multiplier when spacebar is pressed")]
    public float speedBoostMultiplier = 3f;
    
    private bool isSpeedBoosted = false;
    
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
        // Disable physics on the parent since we're handling movement manually
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Destroy(rb);
        }
        
        // Remove any existing collider from the parent
        Collider parentCollider = GetComponent<Collider>();
        if (parentCollider != null)
        {
            Destroy(parentCollider);
        }
        
        // Add colliders to all child cubes
        foreach (Transform child in transform)
        {
            // Make sure the child has a collider
            if (child.GetComponent<Collider>() == null)
            {
                child.gameObject.AddComponent<BoxCollider>();
            }
            
            // Make sure the child has a rigidbody for physics
            if (child.GetComponent<Rigidbody>() == null)
            {
                Rigidbody childRb = child.gameObject.AddComponent<Rigidbody>();
                childRb.isKinematic = true; // We'll handle movement manually
            }
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
    
    private void OnShapeLanded()
    {
        isGrounded = true;
        // Group falling is now handled by the parent object
        Debug.Log("Shape landed");
        
        // Notify the parent group that we've landed
        if (cubesManager != null)
        {
            cubesManager.OnShapeLanded?.Invoke();
        }
    }
    
    void Update()
    {
        if (isGrounded) return;
        
        // Check for spacebar press to speed up falling
        if (Input.GetKeyDown(KeyCode.Space) && !isSpeedBoosted)
        {
            isSpeedBoosted = true;
            fallInterval /= speedBoostMultiplier; // Reduce the interval to make it fall faster
        }
        else if (Input.GetKeyUp(KeyCode.Space) && isSpeedBoosted)
        {
            isSpeedBoosted = false;
            fallInterval *= speedBoostMultiplier; // Reset to normal speed
        }
        
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
                // Move the entire group
                float moveSpeed = isSpeedBoosted ? 20f : 10f; // Faster movement when speed boosted
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * moveSpeed);
                
                // Check if we've reached the target position
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                    isMoving = false;
                    timer = 0f; // Reset timer after completing the move
                    
                    // Check if we should stop falling (hit the ground or another object)
                    if (IsGrounded())
                    {
                        OnShapeLanded();
                    }
                }
            }
        }
    }
    
    bool IsGrounded()
    {
        // Check if any child cube is touching the ground or another object
        foreach (Transform child in transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider == null) continue;
            
            // Cast a ray down from the bottom of the cube
            Vector3 rayStart = child.position - new Vector3(0, childCollider.bounds.extents.y, 0);
            float rayLength = 0.2f; // Small distance to check for ground
            
            // Check for any colliders below this cube (except other cubes in the same group)
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, rayLength, ~LayerMask.GetMask("Ignore Raycast"));
            foreach (var hit in hits)
            {
                if (hit.collider != null && !hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    void TryMoveDown()
    {
        // Check if any child cube would hit something if we move down
        bool wouldHit = false;
        float minDistance = float.MaxValue;
        
        // Check each child cube
        foreach (Transform child in transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider == null) continue;
            
            // Calculate ray start at the bottom of the cube
            Vector3 rayStart = child.position - new Vector3(0, childCollider.bounds.extents.y, 0);
            float rayLength = stepSize + 0.1f; // Small offset to detect surfaces just below
            
            // Cast a ray down from the bottom of this cube
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayLength, ~LayerMask.GetMask("Ignore Raycast")))
            {
                // Ignore hits with other cubes in the same group
                if (hit.transform.IsChildOf(transform)) continue;
                
                wouldHit = true;
                float distance = hit.distance;
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }
        
        if (wouldHit)
        {
            // Move down until we hit something
            float moveDistance = minDistance - 0.1f; // Small offset to prevent sinking
            if (moveDistance > 0)
            {
                targetPosition = transform.position + Vector3.down * moveDistance;
                isMoving = true;
            }
            else
            {
                // We're already touching something below
                OnShapeLanded();
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
