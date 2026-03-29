using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f;
    public float smoothTime = .3f;

    private Vector2 startPositionn;
    private Vector3 velocity;
    void Start()
    {
        startPositionn = transform.position;
    }

    void Update()
    {
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        transform.position = Vector3.SmoothDamp(transform.position, startPositionn + (offset * offsetMultiplier), ref velocity, smoothTime);
    }
}
