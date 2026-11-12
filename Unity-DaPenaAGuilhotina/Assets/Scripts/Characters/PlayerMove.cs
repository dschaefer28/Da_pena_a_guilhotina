using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using Unity.VisualScripting;

public class PlayerMove : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Ao mudar, ajuste junto a velocidade do estado Walk no PlayerAnimator (5 = 1x; hoje 6 = 1.2x) para os pés não deslizarem. O som dos passos segue a animação (PassosDoJogador).")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector2 move;

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
        bool isMoving = PodeMover() && move != Vector2.zero;

        if (spriteRenderer != null)
        {
            if (move.x > 0) spriteRenderer.flipX = false;
            else if (move.x < 0) spriteRenderer.flipX = true;
        }

        // O som dos passos vem dos Animation Events do clipe walk (PassosDoJogador), então só toca enquanto anda.
        if (anim != null)
        {
            anim.SetBool("isWalking", isMoving);
        }
    }

    void FixedUpdate()
    {
        // Diálogo não pausa Time.timeScale (a digitação do texto depende dele rodando), então sem essa
        // trava o jogador andava livremente durante uma conversa com um NPC.
        float velocidadeX = PodeMover() ? move.x * moveSpeed : 0f;
        rb.linearVelocity = new Vector2(velocidadeX, rb.linearVelocity.y);
    }

    private bool PodeMover()
    {
        if (JanelasModais.AlgumaAberta) return false;
        var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : null;
        return dialogo == null || !dialogo.IsDialogueActive;
    }
}
