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
        CorrigirReferenciasDeCena();
        VerificarEstadoDaCena();
    }

    // Se alguém arrastar o PREFAB da Marie (asset) em vez do objeto da cena, SetActive(false) desativaria o
    // asset no projeto e a Marie da cena continuaria lá. Troca pelo objeto de mesmo nome que está na cena.
    private void CorrigirReferenciasDeCena()
    {
        if (npcsParaSumir == null) return;
        for (int i = 0; i < npcsParaSumir.Length; i++)
        {
            GameObject alvo = npcsParaSumir[i];
            if (alvo == null || alvo.scene.IsValid()) continue;

            GameObject naCena = null;
            foreach (NPCMovement npc in FindObjectsByType<NPCMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (npc.gameObject.scene.IsValid() && npc.name == alvo.name) { naCena = npc.gameObject; break; }

            if (naCena != null)
            {
                Debug.LogWarning($"[CENA 1] 'Npcs Para Sumir' apontava para o prefab '{alvo.name}' (asset), não para o objeto da cena. Usando o da cena; corrija a referência no Inspector.", this);
                npcsParaSumir[i] = naCena;
            }
            else
            {
                Debug.LogWarning($"[CENA 1] 'Npcs Para Sumir' aponta para o asset '{alvo.name}' e não há objeto com esse nome na cena.", this);
                npcsParaSumir[i] = null;
            }
        }
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

                // Documento (Interlúdio, "Correção de Despawn"): ao subir do porão com o panfleto, a
                // Marie já deve ter saído da cena — não só a partir da escolha de um caso novo.
                Debug.Log("[CENA 1] Panfleto impresso: escondendo os NPCs do caso tutorial.");
                foreach (GameObject npc in npcsParaSumir)
                {
                    if (npc != null) npc.SetActive(false);
                }
            }
        }
    }
}