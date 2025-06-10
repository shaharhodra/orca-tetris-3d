using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject cubePrefab;  // Assign this in the Inspector
    
    private void Awake()
    {
        // Ensure only one instance exists
        if (FindObjectsOfType<GameInitializer>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        
        DontDestroyOnLoad(gameObject);
        InitializeManagers();
    }
    
    private void InitializeManagers()
    {
        // Initialize ConnectedCubesManager
        ConnectedCubesManager cubesManager = FindObjectOfType<ConnectedCubesManager>();
        if (cubesManager == null)
        {
            GameObject managerObj = new GameObject("ConnectedCubesManager");
            cubesManager = managerObj.AddComponent<ConnectedCubesManager>();
            Debug.Log("Created ConnectedCubesManager");
        }
        
        // Initialize TouchToSpawn if it exists in the scene
        TouchToSpawn touchToSpawn = FindObjectOfType<TouchToSpawn>();
        if (touchToSpawn != null && touchToSpawn.prefabToSpawn == null && cubePrefab != null)
        {
            touchToSpawn.prefabToSpawn = cubePrefab;
            Debug.Log("Assigned cube prefab to TouchToSpawn");
        }
    }
}
