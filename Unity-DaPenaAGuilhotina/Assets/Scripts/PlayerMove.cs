using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 move;
    public Transform npc;

    DialogueSystem dialogueSystem;
    
    // Referência para o Animator
    private Animator anim;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dialogueSystem = FindObjectOfType<DialogueSystem>();
        
        // Pega o componente Animator anexado ao Player
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // --- Lógica de Interação com a NPC ---
        if(Mathf.Abs(transform.position.x - npc.position.x) < 2.0f) {
            if(Input.GetKeyDown(KeyCode.E)) {
                NPCMovement npcMovement = npc.GetComponent<NPCMovement>();

                if (npcMovement != null) {
                    if (npcMovement.currentState == NPCMovement.NPCState.WaitingAtDoor) {
                        npcMovement.EnterHouse();
                    }
                    else if (npcMovement.currentState == NPCMovement.NPCState.ReadyToTalk) {
                        dialogueSystem.Next();
                    }
                }
            }
        }

        // --- Lógica de Animação ---
        if (anim != null) {
            // Se o Vector2 de movimento não for zero, significa que estamos andando
            if (move != Vector2.zero) {
                anim.SetBool("isWalking", true);
            } else {
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
        rb.linearVelocity = move * moveSpeed;
    }
}