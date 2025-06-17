using UnityEngine;
using System.Collections.Generic;

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
    
    [Tooltip("Movement step size for left/right movement")]
    public float moveStep = 1f;
    
    private bool isSpeedBoosted = false;
    private float moveCooldown = 0.1f; // Cooldown between moves in seconds
    private float moveTimer = 0f;
    private bool canMove = true;
    
    [Tooltip("Time in seconds before starting to fall")]
    public float startDelay = 1.0f;
    
    private float timer = 0f;
    private bool isGrounded = false;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool canFall = false;
    [HideInInspector]
    public bool shouldStartFalling = false;
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
        // Always initialize for group behavior
        InitializeGroupBehavior();
        
        // Find the TetrisShapeSpawner if it exists
        if (FindObjectOfType<TetrisShapeSpawner>() == null)
        {
            Debug.LogWarning("TetrisShapeSpawner not found in the scene. Make sure to add it to spawn Tetris shapes.");
        }
    }
    
    public void StartFalling()
    {
        Debug.Log("Starting to fall");
        canFall = true;
        shouldStartFalling = true;
        isGrounded = false;
        timer = 0f;
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
            Rigidbody childRb = child.GetComponent<Rigidbody>();
            if (childRb == null)
            {
                childRb = child.gameObject.AddComponent<Rigidbody>();
            }
            // Configure rigidbody for better collision detection
            childRb.isKinematic = true;
            childRb.useGravity = false;
            childRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
        Debug.Log("OnShapeLanded called");
        
        if (isGrounded) 
        {
            Debug.Log("Already grounded, ignoring");
            return; // Prevent multiple landings
        }
        
        isGrounded = true;
        Debug.Log("Shape marked as landed");
        
        // Notify the parent group that we've landed
        if (cubesManager != null)
        {
            Debug.Log("Notifying cubes manager");
            cubesManager.OnShapeLanded?.Invoke();
        }
        else
        {
            Debug.LogWarning("cubesManager is null!");
        }
        
        // Reset the timer through TouchToSpawn
        if (touchToSpawn == null)
        {
            Debug.Log("Finding TouchToSpawn...");
            touchToSpawn = FindObjectOfType<TouchToSpawn>();
        }
        
        if (touchToSpawn != null)
        {
            Debug.Log("Resetting timer");
            touchToSpawn.ResetTimer();
        }
        else
        {
            Debug.LogError("TouchToSpawn not found in scene!");
        }
    }
    
    private bool CheckGroundBelow()
    {
        // Check if there's ground directly below any child cube
        foreach (Transform child in transform)
        {
            RaycastHit hit;
            float rayLength = 1.1f; // Slightly more than 1 unit
            Vector3 rayStart = child.position;
            
            // Cast a ray downward from the bottom of the cube
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayLength))
            {
                // If we hit something that's not part of this shape
                if (hit.collider != null && hit.collider.transform != child && !hit.collider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    void Update()
    {
        if (isGrounded) 
        {
            return;
        }
        
        // Check if we've hit the ground or another shape below us
        if (CheckGroundBelow())
        {
            isGrounded = true;
            
            // Notify the parent group that we've landed
            if (cubesManager != null)
            {
                cubesManager.OnShapeLanded?.Invoke();
            }
            
            // Reset the timer through TouchToSpawn
            if (touchToSpawn == null)
            {
                touchToSpawn = FindObjectOfType<TouchToSpawn>();
            }
            
            if (touchToSpawn != null)
            {
                touchToSpawn.ResetTimer();
            }
            return;
        }
        
        // Handle movement cooldown
        if (!canMove)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveCooldown)
            {
                canMove = true;
                moveTimer = 0f;
            }
        }
        
        // Handle horizontal movement
        if (canMove)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                TryMoveHorizontal(-moveStep);
                canMove = false;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                TryMoveHorizontal(moveStep);
                canMove = false;
            }
        }
        
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
    
    void TryMoveHorizontal(float direction)
    {
        // Calculate the movement vector
        Vector3 movement = new Vector3(direction, 0, 0);
        
        // Check if the movement is valid (no collisions)
        if (CanMoveInDirection(movement))
        {
            // Move the entire group
            transform.position += movement;
            // Also move the target position to prevent snapping back
            targetPosition += movement;
        }
    }
    
    bool CanMoveInDirection(Vector3 direction)
    {
        // Check each child cube to see if it can move in the given direction
        foreach (Transform child in transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider == null) continue;
            
            // Calculate the position to check
            Vector3 checkPos = child.position + direction;
            
            // Check for any colliders at the target position (except other cubes in the same group)
            Collider[] colliders = Physics.OverlapBox(
                checkPos, 
                childCollider.bounds.extents * 0.9f, // Slightly smaller to prevent edge cases
                child.rotation,
                ~LayerMask.GetMask("Ignore Raycast")
            );
            
            foreach (var collider in colliders)
            {
                if (collider != null && !collider.transform.IsChildOf(transform))
                {
                    return false; // Can't move here, there's something in the way
                }
            }
        }
        return true; // No obstacles found, can move
    }
    
    private HashSet<Collider> currentCollisions = new HashSet<Collider>();
    
    void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions with other cubes in the same shape
        if (collision.transform.IsChildOf(transform)) return;
        
        // Check if we already processed this collision
        if (!currentCollisions.Contains(collision.collider))
        {
            currentCollisions.Add(collision.collider);
            Debug.Log($"Collision with {collision.gameObject.name}");
            
            // Stop the shape and reset the timer on any collision
            OnShapeLanded();
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (currentCollisions.Contains(collision.collider))
        {
            currentCollisions.Remove(collision.collider);
        }
    }
    
    bool IsGrounded()
    {
        // Simply check if we have any active collisions
        return currentCollisions.Count > 0;
    }
    
    // Helper method to find the lowest point of the shape
    float GetLowestPoint()
    {
        float lowestPoint = float.MaxValue;
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            Collider col = child.GetComponent<Collider>();
            if (col == null) continue;
            
            float childBottom = child.position.y - col.bounds.extents.y;
            if (childBottom < lowestPoint)
            {
                lowestPoint = childBottom;
            }
        }
        return lowestPoint;
    }
    
    void TryMoveDown()
    {
        Debug.Log($"Trying to move down from position: {transform.position}");
        
        // If we already have active collisions, we're grounded
        if (currentCollisions.Count > 0)
        {
            OnShapeLanded();
            return;
        }
        
        // Otherwise, move down by step size
        targetPosition = transform.position + Vector3.down * stepSize;
        isMoving = true;
    }
}
