using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 move;
    
    private Animator anim;
    
    // 1. Criamos a variável para guardar o SpriteRenderer
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        
        // 2. Pegamos o componente automaticamente no início do jogo
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // --- Lógica de Virar o Personagem (Flip) ---
        if (spriteRenderer != null)
        {
            if (move.x > 0)
            {
                // Se está indo para a direita, desativa o espelhamento (imagem original)
                spriteRenderer.flipX = false;
            }
            else if (move.x < 0)
            {
                // Se está indo para a esquerda, ativa o espelhamento
                spriteRenderer.flipX = true;
            }
        }

        // --- Lógica de Animação ---
        if (anim != null) 
        {
            if (move != Vector2.zero) 
            {
                anim.SetBool("isWalking", true);
            } 
            else 
            {
                anim.SetBool("isWalking", false);
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * moveSpeed; // Nota: A Unity atualizou 'velocity' para 'linearVelocity' no Rigidbody2D
    }
}