using UnityEngine;

public class TestSpawner : MonoBehaviour
{
    public TetrisShapeSpawner shapeSpawner;
    public float spawnInterval = 3f;
    
    private float timer = 0f;
    
    void Start()
    {
        if (shapeSpawner == null)
        {
            shapeSpawner = FindObjectOfType<TetrisShapeSpawner>();
            if (shapeSpawner == null)
            {
                Debug.LogError("No TetrisShapeSpawner found in the scene!");
                enabled = false;
                return;
            }
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= spawnInterval)
        {
            shapeSpawner.SpawnNewShape();
            timer = 0f;
        }
    }
}
