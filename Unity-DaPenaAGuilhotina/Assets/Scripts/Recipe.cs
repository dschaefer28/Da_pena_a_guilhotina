using UnityEngine;

[CreateAssetMenu(fileName = "NovaReceitaPanfleto", menuName = "ScriptableObjects/Receita de Panfleto")]
public class Recipe : ScriptableObject
{
    public CaseData caso;
    [Header("Evidências Necessárias (Ingredientes)")]
    [Tooltip("Arraste o ScriptableObject da primeira pista aqui.")]
    public Item ingrediente1; 
    
    [Tooltip("Arraste o ScriptableObject da segunda pista aqui.")]
    public Item ingrediente2; 

    [Header("Panfleto Gerado (Resultado)")]
    public Item resultItem; 

    [Header("Impacto Político e Financeiro (Conforme GDD)")]
    [Tooltip("Impacto na baixa burguesia e camponeses. Valores positivos ou negativos.")]
    public int publicOpinionImpact; 
    
    [Tooltip("Impacto na realeza/clero (Fase 1 e 2) ou no Comitê (Fase 3 e 4).")]
    public int stateOpinionImpact;  
    
    [Tooltip("Quantidade de Capital (Ouro) recebida pela publicação deste panfleto.")]
    public int moneyReward;

    /// <summary>Verdadeiro somente se os dois ingredientes e o resultado estiverem configurados com itemID válido.</summary>
    public bool IsValid()
    {
        return ingrediente1 != null && !string.IsNullOrEmpty(ingrediente1.itemID)
            && ingrediente2 != null && !string.IsNullOrEmpty(ingrediente2.itemID)
            && resultItem != null && !string.IsNullOrEmpty(resultItem.itemID);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!IsValid())
            Debug.LogWarning($"[Recipe] '{name}' está incompleta (ingredientes/resultado ausentes ou sem itemID). Ela será ignorada pela prensa.", this);
    }
#endif
}
