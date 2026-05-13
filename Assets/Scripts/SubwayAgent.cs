using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(KinematicRunner))]
public class SubwayAgent : Agent
{
    public RewardConfig rewardConfig;
    
    private KinematicRunner player;
    
    private int bufferedAction = 0;
    
    [SerializeField] private GameManager gameManager;
    
    private int actionCooldownSteps = 0;

    private TrackSegment currentSegment;
    private int lastRewardedRow = -1;

    public override void Initialize()
    {
        player = GetComponent<KinematicRunner>();
    }

    public override void OnEpisodeBegin()
    {
        if (!gameManager.isGameActive)
        {
            gameManager.ResetGame();
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) bufferedAction = 1;
        else if (Input.GetKeyDown(KeyCode.D)) bufferedAction = 2;
        else if (Input.GetKeyDown(KeyCode.W)) bufferedAction = 3;
        else if (Input.GetKeyDown(KeyCode.S)) bufferedAction = 4;
    }
    
    void FixedUpdate() 
    {
        if (actionCooldownSteps > 0) actionCooldownSteps--;
        
        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();
        if (segment != currentSegment)
        {
            currentSegment = segment;
            lastRewardedRow = -1;
        }
    }
    
    private ObstacleType GetUpcomingObstacle(TrackSegment segment, int lane)
    {
        if (segment == null || segment.activePattern == null || segment.activePattern.grid == null) 
        {
            return ObstacleType.None;
        }

        float localZ = 0f - segment.transform.localPosition.z;
        float zOffset = localZ + 5f; 
        
        int currentRowIndex = 4 - Mathf.Clamp(Mathf.FloorToInt((zOffset + 2f) / 2f), 0, 4); 
        
        return segment.activePattern.grid[currentRowIndex, lane];
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(player.isJumping ? 1f : 0f);
        sensor.AddObservation(player.isRolling ? 1f : 0f);
        
        sensor.AddObservation((float)player.currentLane / 2f);
        sensor.AddObservation(player.transform.localPosition.y / player.defaultHeight);
        sensor.AddObservation(gameManager.isStumbling ? 1f : 0f);
        sensor.AddObservation(gameManager.currentSpeed / gameManager.maxSpeed);

        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();
        if (segment != null && segment.activePattern != null)
        {
            sensor.AddObservation(segment.transform.localPosition.z / 10f); 
            
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int obsType = (int)segment.activePattern.grid[r, c];
                    sensor.AddOneHotObservation(obsType, 6); 
                }
            }

            CoinData? nextCoin = segment.GetNextCoinData();
            if (nextCoin.HasValue)
            {
                sensor.AddObservation((float)nextCoin.Value.lane / 2f);
                sensor.AddObservation(nextCoin.Value.heightY / 2f);
                float distToCoin = (nextCoin.Value.localZ - player.transform.localPosition.z) / 10f;
                sensor.AddObservation(distToCoin);
            }
            else
            {
                sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            }
        }
        else
        {
            sensor.AddObservation(0f);
            for (int i = 0; i < 15; i++) sensor.AddOneHotObservation(0, 6);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!gameManager.isGameActive) return;

        int action = actions.DiscreteActions[0];
        
        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();
        ObstacleType upcomingThreat = GetUpcomingObstacle(segment, (int)player.currentLane);
        
        CoinData? nextCoin = segment != null ? segment.GetNextCoinData() : null;

        int currentRowIndex = -1;
        if (segment != null)
        {
            float localZ = 0f - segment.transform.localPosition.z;
            currentRowIndex = 4 - Mathf.Clamp(Mathf.FloorToInt(((localZ + 5f) + 2f) / 2f), 0, 4);
        }
        
        switch (action)
        {
            case 1: // Left
                player.SwitchLane(-1);
                AddReward(rewardConfig.laneSwitchPenalty);
                break;
                
            case 2: // Right
                player.SwitchLane(1);
                AddReward(rewardConfig.laneSwitchPenalty);
                break;
                
            case 3: // Jump
                if (player.isJumping) break;
                
                if (player.isRolling) AddReward(rewardConfig.franticCancelPenalty);
                
                bool isCoinAbove = nextCoin.HasValue && 
                                   nextCoin.Value.lane == (int)player.currentLane && 
                                   nextCoin.Value.heightY >= 1.0f && 
                                   (nextCoin.Value.localZ - player.transform.localPosition.z) < 15f;
                
                if (upcomingThreat == ObstacleType.Hurdle || upcomingThreat == ObstacleType.Ramp || isCoinAbove)
                {
                    if (currentRowIndex != lastRewardedRow) 
                    {
                        AddReward(rewardConfig.successfulJumpReward);
                        lastRewardedRow = currentRowIndex;
                    }
                }
                else
                {
                    AddReward(rewardConfig.badJumpPenalty);
                }
                player.Jump();
                break;
                
            case 4: // Crouch/Roll
                if (player.isRolling) break;
                
                if (player.isJumping) AddReward(rewardConfig.franticCancelPenalty);
                
                if (upcomingThreat == ObstacleType.Barrier)
                {
                    if (currentRowIndex != lastRewardedRow && currentRowIndex != -1)
                    {
                        AddReward(rewardConfig.successfulRollReward);
                        lastRewardedRow = currentRowIndex;
                    }
                }
                else
                {
                    AddReward(rewardConfig.badRollPenalty);
                }
                player.StartRoll();
                break;
                
            case 0: // None
                if (upcomingThreat == ObstacleType.None)
                {
                    AddReward(rewardConfig.doNothingReward); 
                }
                break;
            default:
                break;
        }

        // AddReward(rewardConfig.surviveReward * (gameManager.currentSpeed / gameManager.maxSpeed));
        
        if (action != 0) 
        {
            actionCooldownSteps = 10;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = bufferedAction; 
        bufferedAction = 0; 
    }
    
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // if (actionCooldownSteps > 0)
        // {
        //     actionMask.SetActionEnabled(0, 1, false);
        //     actionMask.SetActionEnabled(0, 2, false);
        //     actionMask.SetActionEnabled(0, 3, false);
        //     actionMask.SetActionEnabled(0, 4, false);
        // }
        
        if (player.isJumping)
        {
            actionMask.SetActionEnabled(0, 3, false); 
        }

        if (player.isRolling)
        {
            actionMask.SetActionEnabled(0, 4, false);
        }
        
        if (player.isSwitchingLanes)
        {
            actionMask.SetActionEnabled(0, 1, false);
            actionMask.SetActionEnabled(0, 2, false);
        }
        else
        {
            if ((int)player.currentLane == 0)
            {
                actionMask.SetActionEnabled(0, 1, false);
            }
            else if ((int)player.currentLane == 2)
            {
                actionMask.SetActionEnabled(0, 2, false);
            }
        }
        
        
    }
}