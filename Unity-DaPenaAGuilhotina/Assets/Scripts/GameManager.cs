using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Dados da Investigação")]
    public CaseData casoEscolhido;

    [Header("Status Globais")]
    public int dinheiroAtual = 0;
    public int opiniaoPublicaAtual = 50; // Começa neutro (50)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConfirmarCaso(CaseData caso)
    {
        casoEscolhido = caso;
        Debug.Log($"Caso escolhido e salvo: {caso.caseTitle}");
    }

    // Chamaremos esta função no fim da Fase 2, quando o caso for resolvido!
    public void ConcluirCasoAtual()
    {
        if (casoEscolhido != null)
        {
            dinheiroAtual += casoEscolhido.moneyReward;
            opiniaoPublicaAtual += casoEscolhido.publicOpinionReward;
            Debug.Log($"Caso Concluído! Dinheiro: {dinheiroAtual} | Opinião: {opiniaoPublicaAtual}");
            
            // Limpa o caso atual para o jogador poder pegar outro na mesa depois
            casoEscolhido = null; 
        }
    }
}