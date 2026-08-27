using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction; // arraste a action "Move" aqui

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 move;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
            moveAction.action.performed += OnMovePerformed;
            moveAction.action.canceled += OnMovePerformed;
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMovePerformed;
            moveAction.action.Disable();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if (spriteRenderer != null)
        {
            if (move.x > 0) spriteRenderer.flipX = false;
            else if (move.x < 0) spriteRenderer.flipX = true;
        }

        if (anim != null)
        {
            anim.SetBool("isWalking", move != Vector2.zero);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(move.x * moveSpeed, rb.linearVelocity.y);
    }
}