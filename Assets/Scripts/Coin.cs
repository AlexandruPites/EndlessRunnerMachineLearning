using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("Visuals")]
    public float rotationSpeed = 150f;

    void Update()
    {
        transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime), Space.World);
    }
    
}