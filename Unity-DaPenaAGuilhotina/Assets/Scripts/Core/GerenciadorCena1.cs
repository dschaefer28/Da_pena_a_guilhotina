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

    // Referência a prefab (asset) faria SetActive(false) desativar o asset, não o NPC da cena: troca pelo
    // objeto de mesmo nome na cena.
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

    /// <summary>
    /// Tutorial encerrado: Marie sai do escritório e Dupaty deixa de conversar (na Fase 4 atende pelo
    /// CheckpointDoTribunal). Lê só o GameManager, já preenchido pelo save no Awake, antes de qualquer Start.
    /// </summary>
    public static bool TutorialEncerrado(GameManager gm, CaseData casoTutorial, Item panfleto)
    {
        if (gm == null) return false;
        if (gm.TutorialConcluido) return true;
        if (casoTutorial != null && gm.casosConcluidos.Contains(casoTutorial)) return true;
        // Um caso da mesa já foi aceito: só acontece depois do tutorial.
        if (casoTutorial != null && gm.casoEscolhido != null && gm.casoEscolhido != casoTutorial) return true;
        // Compatibilidade com saves antigos.
        if (panfleto != null && gm.inventarioSalvo != null)
            foreach (Item item in gm.inventarioSalvo)
                if (item != null && item.itemID == panfleto.itemID) return true;
        return false;
    }

    private void VerificarEstadoDaCena()
    {
        GameManager gm = GameManager.Instance;
        if (!TutorialEncerrado(gm, casoTutorial, panfletoCraftado)) return;

        Debug.Log("[CENA 1] Tutorial concluído: escondendo os NPCs do caso tutorial e silenciando o mentor.");
        if (npcsParaSilenciar != null)
            foreach (NPCMovement npc in npcsParaSilenciar)
                if (npc != null) npc.DisableInteraction();
        if (npcsParaSumir != null)
            foreach (GameObject npc in npcsParaSumir)
                if (npc != null) npc.SetActive(false);
    }
}