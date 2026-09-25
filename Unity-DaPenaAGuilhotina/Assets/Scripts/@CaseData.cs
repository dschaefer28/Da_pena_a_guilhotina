using UnityEngine;

[CreateAssetMenu(fileName = "NovoCaso", menuName = "ScriptableObject/Caso de Investigacao")]
public class CaseData : ScriptableObject
{
    [Header("Textos da Carta (Mesa)")]
    public string caseTitle; // Ex: "O Motim na Fábrica"
    [TextArea(4, 6)]
    public string caseDescription; // O relato do contratante
    public string objectiveText; // Ex: "Descobrir quem ordenou o ataque."

    [Header("Impacto nos Status (Ganhos e Perdas)")]
    public int moneyReward; // Ex: +50 para Réveillon, +10 para o Operário
    public int publicOpinionReward; // Ex: -20 (fama de mercenário) ou +30 (herói do povo)
    [Tooltip("Impacto estimado na Opinião do Estado, exibido na HUD de seleção de casos (Mesa de Casos). " +
             "Mesmo padrão de sinal do publicOpinionReward: positivo = tendência de aumento, negativo = queda.")]
    public int stateOpinionReward; // Ex: -30 (desagrada a coroa) ou +20 (serve bem ao Estado)

    [Header("Progressão")]
    [Tooltip("Fase em que este caso aparece na Mesa de Casos (1 = tutorial, 2, 3 ou 4).")]
    [Range(1, GameManager.UltimaFase)]
    public int fase = 2;

    [Tooltip("Só para casos da Fase 4: a mesa entrega apenas o caso da rota travada (Povo x Estado). " +
             "Nenhuma = aparece em qualquer rota.")]
    public RotaFinal rota = RotaFinal.Nenhuma;

    [Header("Conexão com a Próxima Fase")]
    [Tooltip("O roteiro de diálogo que será ativado no NPC da Fase 2 se este caso for escolhido.")]
    public DialogueData npcDialogueRoute; 
    
    [Tooltip("Cena da investigação deste caso (ex: Fase2, Fase3). A porta do escritório leva para ela enquanto o caso estiver em andamento.")]
    public string nextSceneName = "Fase2";

    [Header("Panfleto do Caso (Fato x Boato)")]
    [Tooltip("Define o que a prensa imprime com as pistas deste caso, conforme sejam fatos ou boatos.")]
    public ReceitaDeCaso receitaDoPanfleto;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(caseTitle))
            Debug.LogWarning($"[CaseData] '{name}' está sem caseTitle.", this);

        if (npcDialogueRoute == null)
            Debug.LogWarning($"[CaseData] '{name}' não tem rota de diálogo (npcDialogueRoute) definida.", this);
    }
#endif
}