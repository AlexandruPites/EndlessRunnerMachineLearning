using System;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Treadmill Settings")]
    public float initialSpeed = 10f;
    public float currentSpeed = 10f;
    private float targetSpeed = 10f;
    public bool isGameActive = true;
    public bool isStumbling { get; private set; }

    [Header("Difficulty Curve")]
    public float speedIncreaseRate = 0.1f; 
    public float maxSpeed = 30f; 

    [Header("Stumble Mechanics")]
    [Range(0.25f, 0.40f)]
    public float stumbleSpeedDropPercentage = 0.3f;
    public float speedRecoveryRate = 8f;

    [Header("Scoring System")]
    public float score = 0f;
    public float pointsPerUnit = 1f; 
    public float coinBonus = 50f;
    
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    
    [Header("System References")]
    public TrackSpawner trackSpawner;
    public KinematicRunner playerScript;
    public SubwayAgent agent;
    
    [Header("Training Configuration")]
    public RewardConfig rewardConfig;
    public bool isTrainingMode = true;
    
    
    
    void Update()
    {
        if (!isGameActive)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResetGame();
            }
        }
    }
    
    void FixedUpdate()
    {
        if (isGameActive)
        {
            if (targetSpeed < maxSpeed)
            {
                targetSpeed += speedIncreaseRate * Time.fixedDeltaTime;
                if (targetSpeed > maxSpeed) targetSpeed = maxSpeed;
            }

            if (isStumbling)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedRecoveryRate * Time.fixedDeltaTime);
                
                if (currentSpeed >= targetSpeed)
                {
                    isStumbling = false;
                    currentSpeed = targetSpeed;
                }
            }
            else
            {
                currentSpeed = targetSpeed;
            }

            score += currentSpeed * pointsPerUnit * Time.fixedDeltaTime;
            UpdateScoreUI();
        } 
    }
    
    public void ApplyStumblePenalty()
    {
        if (!isGameActive) return;

        if (isStumbling)
        {
            GameOver();
            return;
        }

        float speedLost = currentSpeed * stumbleSpeedDropPercentage;
        currentSpeed -= speedLost;
        
        if (playerScript.TryGetComponent(out Agent agent)) agent.AddReward(rewardConfig.stumblePenalty);
        
        isStumbling = true; 
        
    }

    public void AddCoinBonus()
    {
        score += coinBonus;
        if (playerScript.TryGetComponent(out Agent agent)) agent.AddReward(rewardConfig.coinBonus);
    }
    
    private void UpdateScoreUI()
    {
        if (scoreText != null) 
        {
            scoreText.text = "Reward: " + agent.GetCumulativeReward().ToString();
        }
    }
    
    public void GameOver()
    {
        if (!isGameActive) return;

        isGameActive = false;
        UpdateScoreUI();
        
        if (playerScript.TryGetComponent(out Agent agent)) 
        {
            agent.SetReward(rewardConfig.gameOverPenalty);
            agent.EndEpisode();
        }
        
    }

    public void ResetGame()
    {
        score = 0f;
        if (isTrainingMode)
        {
            float randomStartSpeed = UnityEngine.Random.Range(initialSpeed, maxSpeed);
            targetSpeed = randomStartSpeed;
            currentSpeed = randomStartSpeed;
        }
        else
        {
            targetSpeed = initialSpeed;
            currentSpeed = initialSpeed;
        }
        isStumbling = false;

        if (trackSpawner != null) trackSpawner.ResetSpawner();
        if (playerScript != null) playerScript.ResetPlayer();

        isGameActive = true;
    }
}