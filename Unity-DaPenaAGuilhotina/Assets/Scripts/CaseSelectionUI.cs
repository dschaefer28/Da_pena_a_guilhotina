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
    [Tooltip("Casos de todas as fases. A mesa mostra só os da fase atual (CaseData.fase).")]
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
            // Destroy só age no fim do frame; desativar antes tira o cartão velho do layout já neste frame.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        GameManager gm = GameManager.Instance;
        // Um caso por vez: enquanto o panfleto do caso aceito não for impresso, a mesa só mostra as cartas.
        bool casoEmAndamento = gm != null && gm.CasoAtualEmAndamento;
        if (casoEmAndamento)
            AvisoNaTela.Mostrar($"Termine o caso atual antes de aceitar outro: {(gm.casoEscolhido.caseTitle ?? gm.casoEscolhido.name).Trim()}");

        // 2. Loop de Criação: Roda uma vez para cada caso da fase atual
        foreach (CaseData caso in availableCases)
        {
            if (!CasoVisivel(caso, gm)) continue;

            // Tira a cópia do prefab e joga dentro da AreaDosCartoes
            GameObject novoCartao = Instantiate(cardPrefab, cardsContainer);

            // 3. Busca os componentes dentro da cópia exata que acabamos de criar
            // ATENÇÃO: Os nomes entre aspas devem ser exatamente iguais aos nomes na Hierarchy!
            TextMeshProUGUI titulo = novoCartao.transform.Find("TituloTexto").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descricao = novoCartao.transform.Find("DescricaoTexto").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI objetivo = novoCartao.transform.Find("ObjetivoTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI recompensa = novoCartao.transform.Find("RecompensaTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI opiniaoPopular = novoCartao.transform.Find("OpiniaoPopularTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI opiniaoEstado = novoCartao.transform.Find("OpiniaoEstadoTexto")?.GetComponent<TextMeshProUGUI>();
            Button botaoAceitar = novoCartao.transform.Find("ButtonRow/BotaoAceitar").GetComponent<Button>();

            // 4. Preenche os textos com os dados do ScriptableObject
            if (titulo != null) titulo.text = caso.caseTitle;
            if (descricao != null) descricao.text = caso.caseDescription;

            // Objetivo é opcional (nem todo CaseData tem — ex. Caso_Tutorial): esconde a linha se vazio,
            // em vez de mostrar "Objetivo: " sem nada depois.
            if (objetivo != null)
            {
                bool temObjetivo = !string.IsNullOrWhiteSpace(caso.objectiveText);
                objetivo.gameObject.SetActive(temObjetivo);
                if (temObjetivo) objetivo.text = $"Objetivo: {caso.objectiveText}";
            }

            // "Consequências estimadas": valores já cadastrados no CaseData (moneyReward,
            // publicOpinionReward, stateOpinionReward), apresentados como estimativa — não são a
            // aplicação real de recompensa (isso continua acontecendo só na prensa, ver Recipe/
            // CraftingPress). Ícones = texto/símbolo (sem depender de sprites novos).
            if (recompensa != null)
            {
                string sinalOuro = caso.moneyReward >= 0 ? "+" : "";
                recompensa.text = $"Recompensa estimada: {sinalOuro}{caso.moneyReward} de ouro";
            }
            if (opiniaoPopular != null) opiniaoPopular.text = FormatarTendencia("Opinião Popular", caso.publicOpinionReward);
            if (opiniaoEstado != null) opiniaoEstado.text = FormatarTendencia("Opinião do Estado", caso.stateOpinionReward);

            // 5. Documento (Interlúdio, "Mesa de Casos"): bloquear visualmente casos já selecionados antes.
            bool jaSelecionado = GameManager.Instance != null && GameManager.Instance.CasoJaFoiSelecionado(caso);
            if (jaSelecionado)
            {
                if (botaoAceitar != null) botaoAceitar.interactable = false;
                if (titulo != null) titulo.text += " (já escolhido)";
                CanvasGroup grupoDoCartao = novoCartao.GetComponent<CanvasGroup>();
                if (grupoDoCartao == null) grupoDoCartao = novoCartao.AddComponent<CanvasGroup>();
                grupoDoCartao.alpha = 0.45f;
            }
            else if (botaoAceitar != null && casoEmAndamento)
            {
                botaoAceitar.interactable = false;
            }
            else if (botaoAceitar != null)
            {
                // Adiciona a ação de clique via código
                botaoAceitar.onClick.AddListener(() => ConfirmarEscolha(caso));
            }
        }
    }

    // A mesa mostra só os casos da fase atual; na Fase 4, só o da rota travada (documento, "Filtro Dinâmico de Casos").
    private static bool CasoVisivel(CaseData caso, GameManager gm)
    {
        if (caso == null) return false;
        if (gm == null) return true;
        if (caso.fase != gm.faseAtual) return false;
        return caso.rota == RotaFinal.Nenhuma || caso.rota == gm.rotaFinal;
    }

    /// <summary>Falso quando nenhum caso da lista pertence à fase atual: a mesa avisa em vez de abrir vazia.</summary>
    public bool TemCasosNaFase()
    {
        GameManager gm = GameManager.Instance;
        foreach (CaseData caso in availableCases)
            if (CasoVisivel(caso, gm)) return true;
        return false;
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

    // Documento (reformulação "Mesa de Casos"): formata a linha de tendência de uma barra de opinião
    // (Popular/Estado) a partir do valor estimado do CaseData. Não indica valor exato — só a direção
    // (aumento/queda/sem alteração), igual ao exemplo pedido ("tendência de aumento"), já que o
    // resultado real pode variar conforme o sistema de casos.
    private static string FormatarTendencia(string rotulo, int valorEstimado)
    {
        if (valorEstimado > 0) return $"<color=#4CAF50>▲</color> {rotulo}: tendência de aumento";
        if (valorEstimado < 0) return $"<color=#E53935>▼</color> {rotulo}: tendência de queda";
        return $"<color=#9E9E9E>●</color> {rotulo}: sem alteração estimada";
    }
}