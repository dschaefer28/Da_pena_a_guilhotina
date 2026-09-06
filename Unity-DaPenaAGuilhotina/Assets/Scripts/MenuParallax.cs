using UnityEngine;
using UnityEngine.InputSystem;

public class MenuParallax : MonoBehaviour
{
    public float offsetMultiplier = 1f;
    public float smoothTime = .3f;

    private Vector2 startPosition;
    private Vector3 velocity;
    private Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        startPosition = transform.position;
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    private void Update()
    {
        if (mainCamera == null || Pointer.current == null) return;

        Vector2 offset = mainCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
        transform.position = Vector3.SmoothDamp(transform.position, startPosition + (offset * offsetMultiplier), ref velocity, smoothTime);
    }
}
