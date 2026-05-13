using System.Collections;
using UnityEngine;

public class KinematicRunner : MonoBehaviour
{
    public enum Lane { Left = 0, Center = 1, Right = 2 }
    
    [SerializeField] private GameManager gameManager;

    [Header("Lane Settings")]
    public Lane currentLane = Lane.Center;
    public float laneWidth = 2.5f;
    public float snapSpeed = 25f;
    
    [Header("Virtual Dimensions (Math Physics)")]
    public float defaultHeight = 2.0f;
    public float rollHeight = 1.0f;
    public float playerZThickness = 0.5f;
    private float currentVirtualHeight;
    
    [Header("Bump / Stumble Settings")]
    public float bumpDistance = 0.75f;
    public float bumpRecoverySpeed = 10f;
    private float bumpOffset = 0f;

    [Header("Jump & Physics Settings")]
    public float jumpPower = 10f;
    public float gravity = -30f;
    public float feetOffset = 1.0f;
    
    public bool isJumping { get; private set; } = false;
    private float verticalVelocity = 0f;
    private float initialY;

    [Header("Roll Settings")]
    public float rollDuration = 0.8f;
    [Range(0.1f, 1f)] public float colliderHeightMultiplier = 0.5f;
    public bool isRolling { get; private set; } = false;
    private Coroutine rollCoroutine; 

    [Header("Visual Representation")]
    public Transform playerVisualMesh; 
    [Range(0.1f, 1f)] public float visualScaleMultiplier = 0.4f; 
    private Vector3 originalMeshScale;
    private Vector3 originalMeshPosition;
    
    public bool isSwitchingLanes => Mathf.Abs(transform.localPosition.x - ((((int)currentLane - 1) * laneWidth) + bumpOffset)) > 0.05f;

    void Start()
    {
        currentVirtualHeight = defaultHeight;
        originalMeshScale = playerVisualMesh.localScale;
        originalMeshPosition = playerVisualMesh.localPosition;

        initialY = transform.localPosition.y;
        
    }

    void Update()
    {
        // if (gameManager != null && !gameManager.isGameActive) return;
        // HandleInput();
    }
    
    void FixedUpdate()
    {
        if (gameManager != null && !gameManager.isGameActive) return;
        
        ApplyMovement();
        CheckGridCollisions();
    }
    
    public void StartRoll()
    {
        if (isRolling) return;
        
        if (isJumping) CancelJump();
        
        rollCoroutine = StartCoroutine(RollRoutine());
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A)) SwitchLane(-1);
        else if (Input.GetKeyDown(KeyCode.D)) SwitchLane(1);

        if (Input.GetKeyDown(KeyCode.W) && !isJumping)
        {
            if (isRolling) CancelRoll();
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.S) && !isRolling)
        {
            if (isJumping) CancelJump();
            rollCoroutine = StartCoroutine(RollRoutine());
        }
    }

    public void SwitchLane(int direction)
    {
        int targetLaneIndex = Mathf.Clamp((int)currentLane + direction, 0, 2);
        if (targetLaneIndex == (int)currentLane) return; 

        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();
        if (segment != null && segment.activePattern != null)
        {
            float localZ = 0f - segment.transform.localPosition.z;
            float zOffset = localZ + 5f; 
            int rowIndex = 4 - Mathf.Clamp(Mathf.FloorToInt(zOffset / 2f), 0, 4);

            ObstacleType targetObstacle = segment.activePattern.grid[rowIndex, targetLaneIndex];

            if (targetObstacle != ObstacleType.None && IsOverlappingZ(zOffset, rowIndex, targetObstacle))
            {
                if (IsOverlappingY(targetObstacle))
                {
                    bumpOffset = direction * bumpDistance;
                    gameManager.ApplyStumblePenalty();
                    return; 
                }
            }
        }
        currentLane = (Lane)targetLaneIndex;
    }

    public void Jump()
    {
        if (isJumping) return;
        
        if (isRolling) CancelRoll();
        
        isJumping = true;
        verticalVelocity = jumpPower;
    }

    private IEnumerator RollRoutine()
    {
        isRolling = true;
        currentVirtualHeight = rollHeight; 

        playerVisualMesh.localScale = new Vector3(playerVisualMesh.localScale.x, originalMeshScale.y * visualScaleMultiplier, playerVisualMesh.localScale.z);
        float yOffset = (originalMeshScale.y - playerVisualMesh.localScale.y) / 2f; 
        playerVisualMesh.localPosition = new Vector3(playerVisualMesh.localPosition.x, originalMeshPosition.y - yOffset, playerVisualMesh.localPosition.z);

        yield return new WaitForSeconds(rollDuration);

        CancelRoll();
    }

    private void CancelJump()
    {
        isJumping = false;
        verticalVelocity = -15f; 
    }

    private void CancelRoll()
    {
        if (rollCoroutine != null) StopCoroutine(rollCoroutine);

        currentVirtualHeight = defaultHeight; 

        if (playerVisualMesh != null)
        {
            playerVisualMesh.localScale = new Vector3(playerVisualMesh.localScale.x, originalMeshScale.y, playerVisualMesh.localScale.z);
            playerVisualMesh.localPosition = new Vector3(playerVisualMesh.localPosition.x, originalMeshPosition.y, playerVisualMesh.localPosition.z);
        }
        
        isRolling = false;
    }
    
    private void CheckGridCollisions()
    {
        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();
        if (segment == null || segment.activePattern == null) return;

        float localZ = 0f - segment.transform.localPosition.z;
        float zOffset = localZ + 5f; 
        int currentRowIndex = 4 - Mathf.Clamp(Mathf.FloorToInt(zOffset / 2f), 0, 4);
        int currentColIndex = (int)currentLane;

        ObstacleType currentObstacle = segment.activePattern.grid[currentRowIndex, currentColIndex];
        
        float playerFeetY = transform.localPosition.y - feetOffset; 
        float playerHeadY = playerFeetY + currentVirtualHeight;

        if (currentObstacle != ObstacleType.None)
        {
            if (!IsOverlappingZ(zOffset, currentRowIndex, currentObstacle)) return;

            if (currentObstacle == ObstacleType.Wall || 
                currentObstacle == ObstacleType.Barrier || 
                currentObstacle == ObstacleType.Hurdle || 
                currentObstacle == ObstacleType.Train)
            {
                if (IsOverlappingY(currentObstacle))
                {
                    gameManager.GameOver();
                    return; 
                }
            }
        }
        
        segment.ProcessCoinQueue(zOffset, (int)currentLane, playerFeetY, playerHeadY);
    }
    
    private bool IsOverlappingY(ObstacleType type)
    {
        float playerFeetY = transform.localPosition.y - feetOffset;
        float playerHeadY = playerFeetY + currentVirtualHeight;

        float obstacleMinY = 0f;
        float obstacleMaxY = 0f;

        switch (type)
        {
            case ObstacleType.Wall:
            case ObstacleType.Train:
                obstacleMinY = 0.0f;
                obstacleMaxY = 2.0f; 
                break;
            case ObstacleType.Barrier:
                obstacleMinY = 1.2f; 
                obstacleMaxY = 2.0f; 
                break;
            case ObstacleType.Hurdle:
                obstacleMinY = 0.0f;
                obstacleMaxY = 0.8f; 
                break;
            case ObstacleType.Ramp:
                obstacleMinY = 0.0f;
                obstacleMaxY = 2.0f;
                break;
            default:
                return false; 
        }

        return (playerHeadY > obstacleMinY && playerFeetY < obstacleMaxY);
    }

    private void ApplyMovement()
    {
        if (bumpOffset != 0f)
        {
            bumpOffset = Mathf.MoveTowards(bumpOffset, 0f, bumpRecoverySpeed * Time.fixedDeltaTime);
        }
        
        float targetX = (((int)currentLane - 1) * laneWidth) + bumpOffset;
        Vector3 targetPosition = new Vector3(targetX, transform.localPosition.y, transform.localPosition.z);
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, snapSpeed * Time.fixedDeltaTime);

        
        float calculatedFloorY = 0f;
        TrackSegment segment = gameManager.trackSpawner.GetSegmentUnderPlayer();

        if (segment != null && segment.activePattern != null)
        {
            float localZ = 0f - segment.transform.localPosition.z;
            float zOffset = localZ + 5f; 

            int rowIndex = 4 - Mathf.Clamp(Mathf.FloorToInt(zOffset / 2f), 0, 4);
            int colIndex = (int)currentLane;

            ObstacleType floorType = segment.activePattern.grid[rowIndex, colIndex];

            if (floorType == ObstacleType.Train)
            {
                if (transform.localPosition.y >= 1.5f + feetOffset) 
                {
                    calculatedFloorY = 2.0f; 
                }
            }
            else if (floorType == ObstacleType.Ramp)
            {
                float progress = (zOffset % 2f) / 2f; 
                calculatedFloorY = Mathf.Lerp(0f, 2.0f, progress); 
            }
        }

        float targetY = calculatedFloorY + feetOffset;

        if (isJumping || transform.localPosition.y > targetY + 0.1f)
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
            float newY = transform.localPosition.y + (verticalVelocity * Time.fixedDeltaTime);

            if (newY <= targetY)
            {
                newY = targetY;
                isJumping = false;
                verticalVelocity = 0f;
            }

            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
        else
        {
            transform.localPosition = new Vector3(transform.localPosition.x, targetY, transform.localPosition.z);
            verticalVelocity = 0f;
        }
    }
    
    private bool IsOverlappingZ(float playerZOffset, int rowIndex, ObstacleType type)
    {
        float rowCenterZ = ((4 - rowIndex) * 2f) + 1f;
        float obstacleZThickness = 2.0f; 
        
        if (type == ObstacleType.Wall || type == ObstacleType.Barrier || type == ObstacleType.Hurdle)
        {
            obstacleZThickness = 1f; 
        }

        float playerZStart = playerZOffset - (playerZThickness / 2f);
        float playerZEnd = playerZOffset + (playerZThickness / 2f);

        float obstacleZStart = rowCenterZ - (obstacleZThickness / 2f);
        float obstacleZEnd = rowCenterZ + (obstacleZThickness / 2f);

        return (playerZStart <= obstacleZEnd && playerZEnd >= obstacleZStart);
    }

    public void ResetPlayer()
    {
        CancelRoll();
        isJumping = false;
        verticalVelocity = 0f;
        bumpOffset = 0f;
        currentLane = Lane.Center;
        transform.localPosition = new Vector3(0, feetOffset, 0);
    }
}