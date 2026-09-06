using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction; // arraste a action "Move" aqui

    [Header("Áudio (FMOD)")]
    //[SerializeField] private EventReference footstepEvent;

    private Rigidbody2D rb;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 move;
    public GameObject fs;
    public bool walk;

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
            if (fs != null) fs.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= OnMovePerformed;
            moveAction.action.canceled -= OnMovePerformed;
            moveAction.action.Disable();
            if (fs != null) fs.SetActive(false);
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    void Update()
    {
        walk = !MovementBlocked && Mathf.Abs(move.x) > 0.01f;
        if (fs != null && fs.activeSelf != walk) fs.SetActive(walk);
        if (spriteRenderer != null)
        {
            if (move.x > 0) spriteRenderer.flipX = false;
            else if (move.x < 0) spriteRenderer.flipX = true;
        }

        if (anim != null)
        {
            anim.SetBool("isWalking", walk);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(MovementBlocked ? 0 : move.x * moveSpeed, rb.linearVelocity.y);
    }

    private bool MovementBlocked => SceneTransition.IsTransitioning || Time.timeScale == 0 ||
        (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null && GameManager.Instance.dialogueSystem.IsDialogueActive);

}
