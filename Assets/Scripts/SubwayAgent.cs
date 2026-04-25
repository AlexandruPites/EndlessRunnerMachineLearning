using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(KinematicRunner))]
public class SubwayAgent : Agent
{
    public float reward_left, reward_right, reward_up, reward_down, reward_survive;
    
    private KinematicRunner player;
    
    private int bufferedAction = 0;
    
    [SerializeField] private GameManager gameManager;

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

        switch (action)
        {
            case 1: // Left
                player.SwitchLane(-1);
                AddReward(-0.005f);
                break;
                
            case 2: // Right
                player.SwitchLane(1);
                AddReward(-0.005f);
                break;
                
            case 3: // Jump
                if (upcomingThreat == ObstacleType.Hurdle || upcomingThreat == ObstacleType.Ramp)
                {
                    AddReward(0.1f);
                }
                else
                {
                    AddReward(-0.05f);
                }
                player.Jump();
                break;
                
            case 4: // Crouch/Roll
                if (upcomingThreat == ObstacleType.Barrier)
                {
                    AddReward(0.1f);
                }
                else
                {
                    AddReward(-0.05f);
                }
                player.StartRoll();
                break;
                
            case 0: // None
            default:
                break;
        }

        AddReward(reward_survive * (gameManager.currentSpeed / gameManager.maxSpeed));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = bufferedAction; 
        bufferedAction = 0; 
    }
}