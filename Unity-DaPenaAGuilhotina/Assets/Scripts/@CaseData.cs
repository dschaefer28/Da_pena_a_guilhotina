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

    [Header("Conexão com a Próxima Fase")]
    [Tooltip("O roteiro de diálogo que será ativado no NPC da Fase 2 se este caso for escolhido.")]
    public DialogueData npcDialogueRoute; 
    
    [Tooltip("Nome da cena da Fase 2 para carregar após aceitar o caso (opcional).")]
    public string nextSceneName = "Fase2";
}