using UnityEngine;

public class TrackMover : MonoBehaviour
{

    public float speed;
    void FixedUpdate()
    {
        transform.Translate(Vector3.back * (speed * Time.fixedDeltaTime));
    }
}