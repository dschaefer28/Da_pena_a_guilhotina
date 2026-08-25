using UnityEngine;
using TMPro; // Necessário para acessar os textos
using UnityEngine.UI; // Necessário para acessar os botões
using System.Collections.Generic;

public class CaseSelectionUI : MonoBehaviour
{
    [Header("Referências Visuais")]
    [Tooltip("Arraste o Prefab do Cartão que está na sua pasta Prefabs")]
    public GameObject cardPrefab; 
    [Tooltip("Arraste a AreaDosCartoes da sua cena")]
    public Transform cardsContainer; 

    [Header("Dados")]
    [Tooltip("Coloque aqui os 3 ScriptableObjects dos casos que criamos")]
    public List<CaseData> availableCases;

    // Roda automaticamente quando o painel for ativado pelo TableInteractable
    void OnEnable()
    {
        GerarCartoesNaTela();
    }

    private void GerarCartoesNaTela()
    {
        // 1. Limpeza de Segurança: Destrói cartões velhos caso o jogador feche e abra a mesa de novo
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Loop de Criação: Roda uma vez para cada caso na nossa lista
        foreach (CaseData caso in availableCases)
        {
            // Tira a cópia do prefab e joga dentro da AreaDosCartoes
            GameObject novoCartao = Instantiate(cardPrefab, cardsContainer);

            // 3. Busca os componentes dentro da cópia exata que acabamos de criar
            // ATENÇÃO: Os nomes entre aspas devem ser exatamente iguais aos nomes na Hierarchy!
            TextMeshProUGUI titulo = novoCartao.transform.Find("TituloTexto").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descricao = novoCartao.transform.Find("DescricaoTexto").GetComponent<TextMeshProUGUI>();
            Button botaoAceitar = novoCartao.transform.Find("BotaoAceitar").GetComponent<Button>();

            // 4. Preenche os textos com os dados do ScriptableObject
            if (titulo != null) titulo.text = caso.caseTitle;
            if (descricao != null) descricao.text = caso.caseDescription;

            // 5. Configura o botão dinamicamente para avisar qual caso ele representa
            if (botaoAceitar != null)
            {
                // Adiciona a ação de clique via código
                botaoAceitar.onClick.AddListener(() => ConfirmarEscolha(caso));
            }
        }
    }

    // Função chamada quando o botão "Aceitar" de um cartão é clicado
    private void ConfirmarEscolha(CaseData casoEscolhido)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ConfirmarCaso(casoEscolhido);
        }
        else
        {
            Debug.LogError("GameManager não encontrado na cena!");
        }

        FecharPainel();
    }

    // Função para o botão "X" fechar a tela sem escolher nada
    public void FecharPainel()
    {
        gameObject.SetActive(false);
    }
}