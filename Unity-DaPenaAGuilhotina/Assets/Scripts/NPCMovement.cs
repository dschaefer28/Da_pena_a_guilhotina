using UnityEngine;

public class NPCMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public Transform insidePosition;
    public float moveSpeed = 3f;

    public enum NPCState { WaitingAtDoor, MovingInside, ReadyToTalk }
    public NPCState currentState = NPCState.WaitingAtDoor;

    // Referência para o Animator
    private Animator anim;

    void Awake()
    {
        // Pega o componente Animator anexado ao NPC
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (currentState == NPCState.MovingInside)
        {
            // Ativa a animação de andar enquanto estiver se movendo
            if (anim != null) anim.SetBool("isWalking", true);

            transform.position = Vector2.MoveTowards(transform.position, insidePosition.position, moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, insidePosition.position) < 0.1f)
            {
                currentState = NPCState.ReadyToTalk;
                
                // Desativa a animação de andar ao chegar no destino
                if (anim != null) anim.SetBool("isWalking", false);
            }
        }
    }

    public void EnterHouse()
    {
        if (currentState == NPCState.WaitingAtDoor)
        {
            currentState = NPCState.MovingInside;
        }
    }
}