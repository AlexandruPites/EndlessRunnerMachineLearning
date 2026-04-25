using UnityEngine;

public class TrainingGridSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject environmentPrefab;
    
    public int totalEnvironments = 25;
    
    public float spacing = 60f;

    [ContextMenu("1. Spawn Clone Army")]
    public void SpawnGrid()
    {
        if (environmentPrefab == null)
        {
            Debug.LogError("No Prefab assigned!");
            return;
        }

        int gridWidth = Mathf.CeilToInt(Mathf.Sqrt(totalEnvironments));

        for (int i = 0; i < totalEnvironments; i++)
        {
            int row = i / gridWidth;
            int col = i % gridWidth;

            Vector3 spawnPosition = transform.position + new Vector3(col * spacing, 0, row * spacing);

            GameObject clone = Instantiate(environmentPrefab, spawnPosition, Quaternion.identity, transform);
            clone.name = $"Training_Env_{i + 1}";

            if (i == 0) 
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    CameraFollow camFollow = mainCamera.GetComponent<CameraFollow>();
                    if (camFollow != null)
                    {
                        KinematicRunner agentPlayer = clone.GetComponentInChildren<KinematicRunner>();
                        if (agentPlayer != null)
                        {
                            camFollow.target = agentPlayer.transform;
                            Debug.Log("Camera successfully attached to Training_Env_1!");
                        }
                    }
                }
            }
            else 
            {
                Canvas cloneCanvas = clone.GetComponentInChildren<Canvas>();
                if (cloneCanvas != null)
                {
                    cloneCanvas.gameObject.SetActive(false);
                }
            }
        }

        Debug.Log($"Successfully spawned {totalEnvironments} environments!");
    }

    [ContextMenu("2. Clear Clones (Undo)")]
    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        Debug.Log("Training area cleared.");
    }
}