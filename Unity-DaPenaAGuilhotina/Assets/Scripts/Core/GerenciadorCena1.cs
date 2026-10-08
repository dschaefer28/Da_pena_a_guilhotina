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

    /// <summary>
    /// Regra de progresso (Prompt 7): com o tutorial concluído (caso da Fase 1 impresso, ou o jogo já numa fase
    /// posterior), Marie sai do escritório e Dupaty deixa de conversar (na Fase 4 ele atende pelo CheckpointDoTribunal).
    /// Não depende de o panfleto do tutorial ainda estar no inventário, que continua valendo só como reserva de saves
    /// antigos. Sem disputa de ordem com o save: o Continuar aplica os dados no Awake do GameManager, antes de qualquer
    /// Start, e esta checagem lê só o GameManager (inventarioSalvo, não a grade da cena, que se monta no Start dela).
    /// </summary>
    public static bool TutorialEncerrado(GameManager gm, CaseData casoTutorial, Item panfleto)
    {
        if (gm == null) return false;
        if (gm.TutorialConcluido) return true;
        if (casoTutorial != null && gm.casosConcluidos.Contains(casoTutorial)) return true;
        // Um caso da mesa já foi aceito: só acontece depois do tutorial.
        if (casoTutorial != null && gm.casoEscolhido != null && gm.casoEscolhido != casoTutorial) return true;
        // Reserva para saves antigos: o panfleto do tutorial no inventário salvo.
        if (panfleto != null && gm.inventarioSalvo != null)
            foreach (Item item in gm.inventarioSalvo)
                if (item != null && item.itemID == panfleto.itemID) return true;
        return false;
    }

    private void VerificarEstadoDaCena()
    {
        GameManager gm = GameManager.Instance;
        if (!TutorialEncerrado(gm, casoTutorial, panfletoCraftado)) return;

        // Documento (Interlúdio, "Correção de Despawn"): ao subir do porão com o panfleto, a Marie já deve ter saído da
        // cena — e continua fora depois de salvar e carregar.
        Debug.Log("[CENA 1] Tutorial concluído: escondendo os NPCs do caso tutorial e silenciando o mentor.");
        if (npcsParaSilenciar != null)
            foreach (NPCMovement npc in npcsParaSilenciar)
                if (npc != null) npc.DisableInteraction();
        if (npcsParaSumir != null)
            foreach (GameObject npc in npcsParaSumir)
                if (npc != null) npc.SetActive(false); // desativa o boneco por completo
    }
}