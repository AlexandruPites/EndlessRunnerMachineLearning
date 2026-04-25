using System.Collections.Generic;
using UnityEngine;

public class TrackSpawner : MonoBehaviour
{
    [Header("Track Pooling Settings")]
    public GameObject trackPrefab;
    
    public int poolSize = 10;
    
    public float trackLength = 10f;
    
    public float despawnZ = -15f;

    private List<GameObject> activeTracks = new List<GameObject>();
    
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PatternManager patternManager;

    void Start()
    {
        if (trackPrefab == null)
        {
            Debug.LogError("TrackSpawner: No Track Prefab assigned!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            float spawnZ = (i * trackLength) - trackLength; 
            
            GameObject trackSegment = Instantiate(trackPrefab, transform);
            trackSegment.transform.localPosition = new Vector3(0, 0, spawnZ);
            trackSegment.GetComponent<TrackMover>().speed = gameManager.currentSpeed;
            trackSegment.GetComponent<TrackSegment>().patternManager = patternManager;
            trackSegment.GetComponent<TrackSegment>().gameManager = gameManager;
            
            activeTracks.Add(trackSegment);
            
            if (i > 2) 
            {
                TrackSegment segmentScript = trackSegment.GetComponent<TrackSegment>();
                if (segmentScript != null)
                {
                    segmentScript.GenerateFromTextPattern();
                }
            }
        }
    }

    void Update()
    {
        if (activeTracks.Count == 0) return;

        GameObject oldestTrack = activeTracks[0];

        if (oldestTrack.transform.localPosition.z < despawnZ)
        {
            activeTracks.RemoveAt(0);

            GameObject newestTrack = activeTracks[activeTracks.Count - 1];
            
            float newZ = newestTrack.transform.localPosition.z + trackLength;

            oldestTrack.transform.localPosition = new Vector3(0, 0, newZ);
            
            TrackSegment segmentScript = oldestTrack.GetComponent<TrackSegment>();
            if (segmentScript != null)
            {
                segmentScript.GenerateFromTextPattern();
            }

            activeTracks.Add(oldestTrack);
        }
    }
    
    public void ResetSpawner()
    {
        for (int i = 0; i < activeTracks.Count; i++)
        {
            GameObject trackSegment = activeTracks[i];
            
            float spawnZ = (i * trackLength) - trackLength; 
            trackSegment.transform.localPosition = new Vector3(0, 0, spawnZ);

            TrackSegment segmentScript = trackSegment.GetComponent<TrackSegment>();
            if (segmentScript != null)
            {
                segmentScript.ClearObstacles();
                
                if (i > 2) 
                {
                    segmentScript.GenerateFromTextPattern();
                }
            }
        }
    }
    
    public TrackSegment GetSegmentUnderPlayer()
    {
        foreach (GameObject track in activeTracks)
        {
            float z = track.transform.localPosition.z;
            
            if (z <= (trackLength / 2f) && z > -(trackLength / 2f))
            {
                return track.GetComponent<TrackSegment>();
            }
        }
        return null;
    }
}