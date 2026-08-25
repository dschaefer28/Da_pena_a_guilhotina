using UnityEngine;

public class NPCInvestigacao : MonoBehaviour
{
    // Agora referenciamos o seu script NPCMovement exato
    private NPCMovement npcBase; 

    void Start()
    {
        npcBase = GetComponent<NPCMovement>();

        // Verifica se o jogador veio da Fase 1 com um caso na mão
        if (GameManager.Instance != null && GameManager.Instance.casoEscolhido != null)
        {
            // Usa o método que já existe no seu NPCMovement para injetar o novo roteiro
            npcBase.ChangeDialogue(GameManager.Instance.casoEscolhido.npcDialogueRoute);
            Debug.Log("O NPC da Fase 2 recebeu o roteiro correto para este caso!");
        }
    }
}