using UnityEngine;
using System.Collections.Generic;

public class CubeShape : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Time in seconds between each downward movement")]
    public float fallInterval = 0.5f;
    
    [Tooltip("Grid size for snapping movement")]
    public float gridSize = 1f;
    
    [Tooltip("Time in seconds before starting to fall")]
    public float startDelay = 1.0f;
    
    [Header("Input Settings")]
    public float moveCooldown = 0.1f;
    public float fastFallMultiplier = 0.2f;
    
    // State variables
    private bool isPlaced = false;
    private bool canFall = false;
    private bool canMove = true;
    private bool isMoving = false;
    private bool isSpeedBoosted = false;
    private float timer = 0f;
    private float fallTimer = 0f;
    private float moveTimer = 0f;
    private float lastFallTime = 0f;
    private float fastFallInterval = 0.1f;
    private float normalFallInterval = 0.5f;
    private bool isFastFalling = false;
    private bool isGrounded = false;
    public bool shouldStartFalling = false;
    private Vector3 targetPosition;
    public float stepSize = 1.0f; // Grid size for movement steps
    
    // References
    private TouchToSpawn touchToSpawn;
    private ConnectedCubesManager cubesManager;
    private Transform myTransform;
    
    // Touch controls
    private float touchStartTime;
    private Vector2 touchStartPos;
    private bool isTouchMoving = false;
    private float lastTapTime;
    private float doubleTapTime = 0.3f;
    


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
        myTransform = transform;
        
        // Disable physics for grid-based movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Find references
        touchToSpawn = FindObjectOfType<TouchToSpawn>();
        cubesManager = FindObjectOfType<ConnectedCubesManager>();
        
        // Snap to grid on start
        SnapToGrid();
        
        // Initialize group behavior
        InitializeGroupBehavior();
    }
    
    public void StartFalling()
    {
        Debug.Log("Starting to fall");
        canFall = true;
        shouldStartFalling = true;
        isGrounded = false;
        timer = 0f;
    }
    
    void Update()
    {
        if (isGrounded || isPlaced) 
        {
            return;
        }
        
        // Handle touch and keyboard input
        HandleTouchInput();
        HandleKeyboardInput();
        
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
        
        // Handle falling
        if (shouldStartFalling)
        {
            // Handle fast falling
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || isFastFalling)
            {
                fallInterval = fastFallInterval;
                
                // Move down immediately when fast falling
                if (Time.time - lastFallTime >= fallInterval / 2f)
                {
                    if (TryMoveDown())
                    {
                        lastFallTime = Time.time;
                    }
                }
            }
            else
            {
                fallInterval = normalFallInterval;
                
                // Normal falling speed
                if (Time.time - lastFallTime >= fallInterval)
                {
                    if (TryMoveDown())
                    {
                        lastFallTime = Time.time;
                    }
                }
            }
            
            // Timer-based falling sequence
            timer += Time.deltaTime;
            
            if (timer >= fallInterval)
            {
                if (!isMoving)
                {
                    TryMoveDown();
                }
                
                if (isMoving)
                {
                    // Move the entire group
                    float moveSpeed = isSpeedBoosted ? 20f : 10f;
                    transform.position = Vector3.MoveTowards(
                        transform.position, 
                        targetPosition, 
                        Time.deltaTime * moveSpeed);
                    
                    if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                    {
                        transform.position = targetPosition;
                        isMoving = false;
                        timer = 0f;
                        
                        if (IsGrounded())
                        {
                            OnShapeLanded();
                        }
                    }
                }
            }
        }
        
        // Check for ground collision
        if (CheckGroundBelow())
        {
            isGrounded = true;
            
            if (cubesManager != null)
            {
                cubesManager.OnShapeLanded?.Invoke();
            }
            
            if (touchToSpawn == null)
            {
                touchToSpawn = FindObjectOfType<TouchToSpawn>();
            }
            
            touchToSpawn?.ResetTimer();
        }
    }
    
    bool TryMove(Vector3 direction)
    {
        // Calculate new position based on grid
        Vector3 newPosition = myTransform.position + direction * gridSize;
        
        // Check if the new position is valid
        if (IsValidPosition(newPosition))
        {
            myTransform.position = newPosition;
            return true;
        }
        return false;
    }
    
    bool IsValidPosition(Vector3 position)
    {
        // Check each child cube
        foreach (Transform child in myTransform)
        {
            Vector3 childPos = position + child.localPosition;
            
            // Check if position is outside boundaries (adjust grid bounds as needed)
            if (childPos.x < 0 || childPos.x >= 10 || childPos.y < 0)
                return false;
                
            // Check for collisions with other shapes or ground
            Collider[] colliders = Physics.OverlapBox(
                childPos, 
                Vector3.one * (gridSize * 0.4f), 
                Quaternion.identity,
                LayerMask.GetMask("PlacedShape", "Ground"));
                
            // If we hit something that's not part of this shape
            foreach (Collider col in colliders)
            {
                if (col != null && col.transform != child && !col.transform.IsChildOf(myTransform))
                {
                    return false;
                }
            }
        }
        return true;
    }
    
    void SnapToGrid()
    {
        if (myTransform == null) return;
        
        Vector3 pos = myTransform.position;
        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
        pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
        pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
        myTransform.position = pos;
    }
    
    void PlaceShape()
    {
        isPlaced = true;
        canFall = false;
        
        // Change layer to "PlacedShape" for collision detection
        foreach (Transform child in myTransform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("PlacedShape");
        }
        
        // Notify the TouchToSpawn that this shape has been placed
        if (touchToSpawn == null)
        {
            touchToSpawn = FindObjectOfType<TouchToSpawn>();
        }
        
        if (touchToSpawn != null)
        {
            touchToSpawn.OnShapeLanded();
        }
    }
    
    void RotateShape()
    {
        // Store original rotation
        Quaternion originalRotation = myTransform.rotation;
        
        // Rotate around the center of the shape
        myTransform.Rotate(Vector3.forward * 90f, Space.World);
        
        // After rotating, check if the new position is valid
        if (!IsValidPosition(myTransform.position))
        {
            // If not valid, rotate back
            myTransform.rotation = originalRotation;
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
        
        // Find the center position of all children
        Vector3 center = Vector3.zero;
        int childCount = 0;
        
        foreach (Transform child in transform)
        {
            center += child.position;
            childCount++;
        }
        
        if (childCount > 0)
        {
            center /= childCount; // Calculate average position
            
            // Move the parent to the center of the children
            // and adjust children's local positions accordingly
            Vector3 parentPosition = transform.position;
            Vector3 offset = parentPosition - center;
            
            // Move parent to center of children
            transform.position = center;
            
            // Adjust children's local positions to maintain their world positions
            foreach (Transform child in transform)
            {
                child.position += offset;
            }
        }
        
        // Set up colliders and rigidbodies for all child cubes
        foreach (Transform child in transform)
        {
            // Add or get BoxCollider
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = child.gameObject.AddComponent<BoxCollider>();
            }
            collider.isTrigger = false;
            collider.size = Vector3.one * 0.95f; // Slightly smaller to prevent edge cases
            
            // Add or get Rigidbody
            Rigidbody childRb = child.GetComponent<Rigidbody>();
            if (childRb == null)
            {
                childRb = child.gameObject.AddComponent<Rigidbody>();
            }
            
            // Configure Rigidbody
            childRb.isKinematic = true; // We'll handle movement manually
            childRb.useGravity = false;
            
            // Set layer and tag
            child.gameObject.layer = LayerMask.NameToLayer("Default");
            child.gameObject.tag = gameObject.CompareTag("PlayerShape") ? "PlayerCube" : "Cube";
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
    
    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Check for double tap
                    if (Time.time - lastTapTime < doubleTapTime)
                    {
                        RotateShape();
                    }
                    lastTapTime = Time.time;
                    
                    // Start tracking touch position
                    touchStartPos = touch.position;
                    touchStartTime = Time.time;
                    isTouchMoving = false;
                    break;
                    
                case TouchPhase.Moved:
                    // Check for swipe
                    if (!isTouchMoving && Vector2.Distance(touch.position, touchStartPos) > 20f)
                    {
                        isTouchMoving = true;
                        
                        // Check if it's a horizontal swipe
                        Vector2 swipeDelta = touch.position - touchStartPos;
                        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                        {
                            // Horizontal swipe
                            if (swipeDelta.x > 0)
                                TryMove(Vector3.right);
                            else
                                TryMove(Vector3.left);
                        }
                        else
                        {
                            // Vertical swipe down for fast fall
                            if (swipeDelta.y < 0)
                                isFastFalling = true;
                        }
                    }
                    break;
                    
                case TouchPhase.Ended:
                    isTouchMoving = false;
                    isFastFalling = false;
                    break;
            }
        }
    }
    
    // Removed duplicate methods to use the grid-based versions above
    
    private void HandleKeyboardInput()
    {
        // Handle keyboard input for testing
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            TryMove(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            TryMove(Vector3.right);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            RotateShape();
        }
    }
    
    // Removed duplicate Update method - functionality merged into the first Update method
    
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
        // Try to move each child and check for collisions
        foreach (Transform child in transform)
        {
            // Save original position
            Vector3 originalPos = child.position;
            
            // Move the child temporarily
            child.position += direction;
            
            // Check for any collisions
            Collider[] colliders = Physics.OverlapBox(
                child.position,
                Vector3.one * 0.45f, // Slightly smaller than the cube
                child.rotation
            );
            
            // Move back
            child.position = originalPos;
            
            // Check if any colliders were found that aren't part of this shape
            foreach (var collider in colliders)
            {
                if (collider != null && 
                    !collider.transform.IsChildOf(transform) &&
                    (collider.CompareTag("Cube") || collider.CompareTag("PlayerCube")))
                {
                    return false; // Collision detected
                }
            }
        }
        return true; // No collisions detected
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
    
    bool TryMoveDown()
    {
        Debug.Log($"Trying to move down from position: {transform.position}");
        
        // If we already have active collisions, we're grounded
        if (currentCollisions.Count > 0 || CheckGroundBelow())
        {
            OnShapeLanded();
            return false;
        }
        
        // Otherwise, move down by step size
        targetPosition = transform.position + Vector3.down * stepSize;
        isMoving = true;
        return true;
    }
}
