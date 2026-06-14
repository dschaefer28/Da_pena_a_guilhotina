using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 move;

    // Referência para o Animator
    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Pega o componente Animator anexado ao Player
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // --- Lógica de Animação ---
        if (anim != null)
        {
            // Se o Vector2 de movimento não for zero, significa que estamos andando
            anim.SetBool("isWalking", move != Vector2.zero);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = move * moveSpeed;
    }
}