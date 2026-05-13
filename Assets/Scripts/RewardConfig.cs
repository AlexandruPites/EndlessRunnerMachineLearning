using UnityEngine;

[CreateAssetMenu(fileName = "RewardConfig", menuName = "ML-Agents/Reward Config")]
public class RewardConfig : ScriptableObject
{
    [Header("Global Rewards")]
    public float surviveReward = 0.01f; 
    public float coinBonus = 1.0f;
    public float doNothingReward = 0.002f;
    
    [Header("Penalties")]
    public float gameOverPenalty = -100f;
    public float stumblePenalty = -0.05f;
    public float laneSwitchPenalty = -0.005f;
    
    [Header("Action Specific")]
    public float successfulJumpReward = 0.1f;
    public float badJumpPenalty = -0.05f;
    public float successfulRollReward = 0.1f;
    public float badRollPenalty = -0.05f;
    public float franticCancelPenalty = -0.1f;
}