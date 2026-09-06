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
        PauseManager.RequestPause(true);
        GerarCartoesNaTela();
    }

    void OnDisable() { PauseManager.RequestPause(false); }

    private void GerarCartoesNaTela()
    {
        if (cardsContainer == null || cardPrefab == null)
        {
            Debug.LogError("[CaseSelectionUI] cardsContainer ou cardPrefab não atribuído.", this);
            return;
        }

        // 1. Limpeza de Segurança: Destrói cartões velhos caso o jogador feche e abra a mesa de novo
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Loop de Criação: Roda uma vez para cada caso na nossa lista
        if (availableCases == null) return;

        foreach (CaseData caso in availableCases)
        {
            if (caso == null) continue;

            // Tira a cópia do prefab e joga dentro da AreaDosCartoes
            GameObject novoCartao = Instantiate(cardPrefab, cardsContainer);

            // 3. Busca os componentes dentro da cópia exata que acabamos de criar
            // ATENÇÃO: Os nomes entre aspas devem ser exatamente iguais aos nomes na Hierarchy!
            Transform tituloTransform = novoCartao.transform.Find("TituloTexto");
            Transform descricaoTransform = novoCartao.transform.Find("DescricaoTexto");
            Transform botaoTransform = novoCartao.transform.Find("BotaoAceitar");

            TextMeshProUGUI titulo = tituloTransform != null ? tituloTransform.GetComponent<TextMeshProUGUI>() : null;
            TextMeshProUGUI descricao = descricaoTransform != null ? descricaoTransform.GetComponent<TextMeshProUGUI>() : null;
            Button botaoAceitar = botaoTransform != null ? botaoTransform.GetComponent<Button>() : null;

            if (titulo == null || descricao == null || botaoAceitar == null)
            {
                Debug.LogError("[CaseSelectionUI] O prefab do cartão não possui TituloTexto, DescricaoTexto ou BotaoAceitar corretamente configurado.", novoCartao);
                Destroy(novoCartao);
                continue;
            }

            // 4. Preenche os textos com os dados do ScriptableObject
            if (titulo != null) titulo.text = caso.caseTitle;
            if (descricao != null) descricao.text = caso.caseDescription;
            titulo.fontStyle &= ~(FontStyles.UpperCase | FontStyles.SmallCaps);
            descricao.fontStyle &= ~(FontStyles.UpperCase | FontStyles.SmallCaps);
            if (GameManager.Instance != null && GameManager.Instance.casoEscolhido == caso)
            {
                var buttonLabel = botaoAceitar.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonLabel != null) buttonLabel.text = "Caso aceito";
                botaoAceitar.interactable = false;
            }

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
        if (SceneTransition.IsTransitioning) return;
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado na cena!");
            FecharPainel();
            return;
        }

        string nextScene = casoEscolhido != null ? casoEscolhido.nextSceneName : null;
        if (!string.IsNullOrWhiteSpace(nextScene) && !Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"[CaseSelectionUI] Cena '{nextScene}' não está nas Build Settings.", casoEscolhido);
            return;
        }

        if (GameManager.Instance.ConfirmarCaso(casoEscolhido))
        {
            GameFeedback.Show($"Caso aceito: {casoEscolhido.caseTitle}\n{casoEscolhido.objectiveText}");
            InventoryManager inventory = GameManager.Instance.inventoryManager;
            if (inventory != null) inventory.SalvarEstadoAtual();

            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                PauseManager.ForceReset();
                FecharPainel();
                SceneTransition.Load(nextScene);
                return;
            }
        }
        FecharPainel();
    }

    // Função para o botão "X" fechar a tela sem escolher nada
    public void FecharPainel()
    {
        gameObject.SetActive(false);
    }
}
