using System.Collections.Generic;
using UnityEngine;
using System;

public enum ObstacleType { None, Hurdle, Barrier, Wall, Ramp, Train }

public struct CoinData
{
    public int lane;       
    public float localZ;   
    public float heightY;  
}

public class PatternData
{
    public string patternName;
    public ObstacleType[,] grid = new ObstacleType[5, 3]; 
    public List<CoinData> coins = new List<CoinData>(); 
}

public class PatternManager : MonoBehaviour
{

    [Header("Configuration")]
    public string textFileName = "LevelPatterns";
    public float laneWidth = 2.5f;
    public float trackLength = 10f;

    [Header("Prefabs")]
    public GameObject hurdlePrefab;
    public GameObject barrierPrefab;
    public GameObject wallPrefab;
    public GameObject rampPrefab;
    public GameObject trainPrefab;
    public GameObject coinPrefab; 

    private List<PatternData> allPatterns = new List<PatternData>();
    private Dictionary<ObstacleType, Queue<GameObject>> objectPools;

    [SerializeField] public GameManager gameManager;
    
    private Queue<GameObject> coinPool = new Queue<GameObject>();

    void Awake()
    {
        InitializePools();
        ParseTextFile();
    }

    private void InitializePools()
    {
        objectPools = new Dictionary<ObstacleType, Queue<GameObject>>
        {
            { ObstacleType.Hurdle, new Queue<GameObject>() },
            { ObstacleType.Barrier, new Queue<GameObject>() },
            { ObstacleType.Wall, new Queue<GameObject>() },
            { ObstacleType.Ramp, new Queue<GameObject>() },
            { ObstacleType.Train, new Queue<GameObject>() }
        };
    }

    private void ParseTextFile()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(textFileName);
        if (textAsset == null) return;

        string[] lines = textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        PatternData currentPattern = null;
        int currentRow = 0;

        foreach (string line in lines)
        {
            string cleanLine = line.Trim();

            if (cleanLine == "[Pattern]")
            {
                currentPattern = new PatternData();
                allPatterns.Add(currentPattern);
                currentRow = 0;
            }
            else if (cleanLine.StartsWith("Name:") && currentPattern != null)
            {
                currentPattern.patternName = cleanLine.Substring(5).Trim();
            }
            else if (cleanLine.StartsWith("CoinLine:") && currentPattern != null)
            {
                string[] parts = cleanLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6)
                {
                    int lane = int.Parse(parts[1]);
                    float startZ = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    int count = int.Parse(parts[3]);
                    float spacing = float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture);
                    float heightY = float.Parse(parts[5], System.Globalization.CultureInfo.InvariantCulture);

                    for (int i = 0; i < count; i++)
                    {
                        currentPattern.coins.Add(new CoinData { lane = lane, localZ = startZ + (i * spacing), heightY = heightY });
                    }
                }
            }
            else if (currentPattern != null && currentRow < 5 && !cleanLine.StartsWith("//"))
            {
                string[] slots = cleanLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int col = 0; col < Mathf.Min(slots.Length, 3); col++)
                {
                    currentPattern.grid[currentRow, col] = ParseCharToType(slots[col]);
                }
                currentRow++;
            }
        }
    }

    private ObstacleType ParseCharToType(string c)
    {
        switch (c.ToUpper())
        {
            case "H": return ObstacleType.Hurdle;
            case "B": return ObstacleType.Barrier;
            case "W": return ObstacleType.Wall;
            case "R": return ObstacleType.Ramp;
            case "T": return ObstacleType.Train;
            default: return ObstacleType.None;
        }
    }

    public GameObject GetObstacle(ObstacleType type)
    {
        if (type == ObstacleType.None) return null;
        if (objectPools[type].Count > 0)
        {
            GameObject obj = objectPools[type].Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        switch (type)
        {
            case ObstacleType.Hurdle: return Instantiate(hurdlePrefab);
            case ObstacleType.Barrier: return Instantiate(barrierPrefab);
            case ObstacleType.Wall: return Instantiate(wallPrefab);
            case ObstacleType.Ramp: return Instantiate(rampPrefab);
            case ObstacleType.Train: return Instantiate(trainPrefab);
        }
        return null;
    }

    public void ReturnObstacle(GameObject obj, ObstacleType type)
    {
        obj.SetActive(false);
        obj.transform.SetParent(this.transform); 
        objectPools[type].Enqueue(obj);
    }

    public GameObject GetCoin()
    {
        if (coinPool.Count > 0)
        {
            GameObject obj = coinPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(coinPrefab); 
    }

    public void ReturnCoin(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(this.transform);
        coinPool.Enqueue(obj);
    }

    public PatternData GetRandomPattern()
    {
        if (allPatterns.Count == 0) return null;
        return allPatterns[UnityEngine.Random.Range(0, allPatterns.Count)];
    }
}