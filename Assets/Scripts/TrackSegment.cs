using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    private struct ActiveObstacle
    {
        public GameObject gameObject;
        public ObstacleType type;
    }

    private class ActiveCoin
    {
        public CoinData data;
        public GameObject gameObject;
        public bool isCollected;
    }

    private List<ActiveObstacle> spawnedObstacles = new List<ActiveObstacle>();
    private List<ActiveCoin> spawnedCoins = new List<ActiveCoin>();
    private int nextCoinIndex = 0; 
    
    
    
    public PatternManager patternManager;
    public GameManager gameManager;

    public PatternData activePattern { get; private set; }
    
    


    public void GenerateFromTextPattern()
    {
        ClearObstacles();
        activePattern = patternManager.GetRandomPattern();
        if (activePattern == null) return; 

        float rowSpacing = patternManager.trackLength / 5f;
        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                ObstacleType type = activePattern.grid[row, col];
                if (type == ObstacleType.None) continue;

                GameObject obstacle = patternManager.GetObstacle(type);
                if (obstacle != null)
                {
                    float targetX = (col - 1) * patternManager.laneWidth;
                    float targetZ = ((4 - row) * rowSpacing) - (patternManager.trackLength / 2f) + (rowSpacing / 2f);

                    obstacle.transform.SetParent(this.transform);
                    obstacle.transform.localPosition = new Vector3(targetX, 0f, targetZ);
                    spawnedObstacles.Add(new ActiveObstacle { gameObject = obstacle, type = type });
                }
            }
        }

        nextCoinIndex = 0;
        var sortedCoins = activePattern.coins.OrderBy(c => c.localZ).ToList(); 

        foreach (CoinData coin in sortedCoins)
        {
            GameObject coinObj = patternManager.GetCoin(); 
            if (coinObj != null)
            {
                float targetX = (coin.lane - 1) * patternManager.laneWidth;
                float targetZ = coin.localZ - (patternManager.trackLength / 2f);

                coinObj.transform.SetParent(this.transform);
                coinObj.transform.localPosition = new Vector3(targetX, coin.heightY, targetZ);
                
                spawnedCoins.Add(new ActiveCoin { data = coin, gameObject = coinObj, isCollected = false });
            }
        }
    }

    public void ProcessCoinQueue(float playerLocalZ, int playerLane, float playerFeetY, float playerHeadY)
    {
        while (nextCoinIndex < spawnedCoins.Count)
        {
            ActiveCoin nextCoin = spawnedCoins[nextCoinIndex];
            float coinZ = nextCoin.data.localZ;
            float coinZThickness = 0.5f; 

            if (playerLocalZ < coinZ - coinZThickness) return; 

            if (playerLocalZ > coinZ + coinZThickness)
            {
                nextCoinIndex++;
                continue;
            }

            if (!nextCoin.isCollected && playerLane == nextCoin.data.lane)
            {
                if (nextCoin.data.heightY >= playerFeetY && nextCoin.data.heightY <= playerHeadY)
                {
                    nextCoin.isCollected = true;
                    nextCoin.gameObject.SetActive(false);
                    gameManager.AddCoinBonus();
                }
            }
            break; 
        }
    }

    public void ClearObstacles()
    {
        foreach (var activeObs in spawnedObstacles) patternManager.ReturnObstacle(activeObs.gameObject, activeObs.type);
        foreach (var activeCoin in spawnedCoins) patternManager.ReturnCoin(activeCoin.gameObject);
        
        spawnedObstacles.Clear();
        spawnedCoins.Clear();
        activePattern = null;
    }
    
    public CoinData? GetNextCoinData()
    {
        if (nextCoinIndex < spawnedCoins.Count)
        {
            return spawnedCoins[nextCoinIndex].data;
        }
        return null;
    }
}