using UnityEngine;
using System.Collections.Generic;

public class TetrisShape : MonoBehaviour
{
    public float fallInterval = 1f; // Time between each fall step
    public float moveStep = 1f;      // Distance to move each step
    
    private float fallTimer = 0f;
    private bool isFalling = false;
    private bool isPlaced = false;
    private float gridSize = 1f; // Size of the grid cells
    
    void Update()
    {
        if (!isFalling || isPlaced) return;
        
        // Handle horizontal movement
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryMove(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryMove(Vector3.right);
        }
        
        // Handle fast drop
        float currentFallInterval = Input.GetKey(KeyCode.Space) ? fallInterval * 0.1f : fallInterval;
        
        // Handle falling
        fallTimer += Time.deltaTime;
        if (fallTimer >= currentFallInterval)
        {
            fallTimer = 0f;
            if (!TryMove(Vector3.down))
            {
                // If we can't move down, place the shape
                PlaceShape();
            }
        }
    }
    
    bool TryMove(Vector3 direction)
    {
        // Calculate new position
        Vector3 newPosition = transform.position + direction * gridSize;
        
        // Check if the new position is valid
        if (IsValidPosition(newPosition))
        {
            transform.position = newPosition;
            return true;
        }
        return false;
    }
    
    bool IsValidPosition(Vector3 position)
    {
        // Check each child cube
        foreach (Transform child in transform)
        {
            Vector3 childPos = position + child.localPosition;
            
            // Check if position is outside boundaries
            if (childPos.x < 0 || childPos.x >= 10 || childPos.y < 0) // Adjust grid size as needed
                return false;
                
            // Check for collisions with other shapes
            Collider[] colliders = Physics.OverlapBox(
                childPos, 
                Vector3.one * 0.4f, 
                Quaternion.identity,
                LayerMask.GetMask("PlacedShape"));
                
            if (colliders.Length > 0)
                return false;
        }
        return true;
    }
    
    void PlaceShape()
    {
        isPlaced = true;
        isFalling = false;
        
        // Change layer to "PlacedShape" for collision detection
        foreach (Transform child in transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("PlacedShape");
        }
        
        // Notify the TouchToSpawn that this shape has been placed
        TouchToSpawn spawner = FindObjectOfType<TouchToSpawn>();
        if (spawner != null)
        {
            spawner.OnShapeLanded();
        }
    }
    
    public void StartFalling()
    {
        isFalling = true;
        isPlaced = false;
    }
}
