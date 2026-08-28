using UnityEngine;

public class GerenciadorCena1 : MonoBehaviour
{
    [Header("Progresso: Fim do Tutorial")]
    [Tooltip("O panfleto que prova que o jogador já voltou do porão.")]
    public Item panfletoCraftado;
    
    [Tooltip("Arraste o Caso Fantasma/Tutorial aqui para o script saber reconhecê-lo.")]
    public CaseData casoTutorial;

    [Header("Ação: Silenciar (Continuam na Cena)")]
    [Tooltip("Arraste NPCs como o Dupatch para cá.")]
    public NPCMovement[] npcsParaSilenciar;

    [Header("Ação: Desaparecer (Fase 2 em diante)")]
    [Tooltip("Arraste o GameObject da Mary Bradier aqui para ela sumir fisicamente.")]
    public GameObject[] npcsParaSumir;

    void Start()
    {
        VerificarEstadoDaCena();
    }

    private void VerificarEstadoDaCena()
    {
        if (GameManager.Instance == null) return;

        // 1. CHECAGEM DE FASE 2 (Sumir com a Mary)
        // Se o caso atual NÃO é o Tutorial, significa que o jogador usou a mesa e avançou no jogo.
        if (casoTutorial != null && GameManager.Instance.casoEscolhido != null)
        {
            if (GameManager.Instance.casoEscolhido != casoTutorial)
            {
                Debug.Log("[CENA 1] Fase 2 detectada! Escondendo NPCs antigos...");
                foreach (GameObject npc in npcsParaSumir)
                {
                    if (npc != null) npc.SetActive(false); // Desativa o boneco por completo
                }
            }
        }

        // 2. CHECAGEM DO PORÃO (Silenciar o Dupatch)
        if (panfletoCraftado != null)
        {
            bool temPanfleto = false;
            
            if (GameManager.Instance.inventarioSalvo != null)
            {
                foreach (Item item in GameManager.Instance.inventarioSalvo)
                {
                    if (item != null && item.itemID == panfletoCraftado.itemID)
                    {
                        temPanfleto = true;
                        break;
                    }
                }
            }

            if (temPanfleto)
            {
                foreach (NPCMovement npc in npcsParaSilenciar)
                {
                    if (npc != null) npc.DisableInteraction();
                }
            }
        }
    }
}