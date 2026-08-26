using UnityEngine;

[CreateAssetMenu(fileName = "NovaReceitaPanfleto", menuName = "ScriptableObjects/Receita de Panfleto")]
public class Recipe : ScriptableObject
{
    [Header("Evidências Necessárias (Ingredientes)")]
    public string itemID1; 
    public string itemID2; 

    [Header("Panfleto Gerado (Resultado)")]
    public Item resultItem; 

    [Header("Impacto Político e Financeiro (Conforme GDD)")]
    [Tooltip("Impacto na baixa burguesia e camponeses. Valores positivos ou negativos.")]
    public int publicOpinionImpact; 
    
    [Tooltip("Impacto na realeza/clero (Fase 1 e 2) ou no Comitê (Fase 3 e 4).")]
    public int stateOpinionImpact;  
    
    [Tooltip("Quantidade de Capital (Ouro) recebida pela publicação deste panfleto.")]
    public int moneyReward;         
}