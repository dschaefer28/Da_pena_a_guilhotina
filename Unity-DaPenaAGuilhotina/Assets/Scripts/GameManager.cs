using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // NOVO: Permite gerenciar eventos de carregamento de cena

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dados da Investigação (Fase Atual)")]
    public CaseData casoEscolhido;

    [Tooltip("Todos os casos que o jogador já escolheu alguma vez (mesmo que não seja mais o atual). " +
             "A Mesa de Casos (CaseSelectionUI) usa isso para bloquear cartões já selecionados.")]
    public List<CaseData> casosJaSelecionados = new List<CaseData>();

    [Header("Status Globais (HUD)")]
    public int capitalAtual = 0;
    public int opiniaoPublicaAtual = 50; 
    public int opiniaoEstadoAtual = 50; 

    [Header("Persistência (Entre Cenas)")]
    public List<Item> inventarioSalvo = new List<Item>();

    [Serializable]
    public class PanfletoPublicado
    {
        public CaseData caso;
        public NivelDoPanfleto nivel;
    }

    [Serializable]
    public class RevelacaoPendente
    {
        public CaseData caso;
        public string texto;
        public int povo;
        public int estado;
    }

    [Header("Fato x Boato")]
    [Tooltip("Histórico de panfletos de caso impressos e a versão de cada um (útil para o Tribunal na Fase 4).")]
    public List<PanfletoPublicado> panfletosPublicados = new List<PanfletoPublicado>();
    [Tooltip("Boatos publicados que ainda vão ser expostos. Consumidos pelo RevelacaoDeBoatos na cena seguinte.")]
    public List<RevelacaoPendente> revelacoesPendentes = new List<RevelacaoPendente>();
    [Tooltip("itemIDs de pistas cuja verdade o jogador já descobriu. Guardado aqui para a ficha não voltar a " +
             "'não verificada' quando o item que confirmou a pista for gasto na prensa.")]
    public List<string> pistasVerificadas = new List<string>();

    [Header("Dependências Globais")]
    public InventoryManager inventoryManager;
    public DialogueSystem dialogueSystem;

    public event Action OnStatusChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Torna o GameManager imortal
        VincularDependenciasLocais();
    }

    // ARQUITETURA: Assina o evento nativo da Unity para troca de cenas
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Gatilho automático disparado sempre que uma nova cena abre (ex: Fase2 -> Porao)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SISTEMA] Cena '{scene.name}' carregada. Sincronizando HUD...");
        // Reencontra as dependências locais da cena recém-carregada (GameManager é persistente entre cenas)
        VincularDependenciasLocais();
        // Força todas as UIs da nova cena a buscarem os valores salvos
        ForcarAtualizacaoUI();
    }

    // Preenche inventoryManager/dialogueSystem com as instâncias da cena atual quando ainda não vinculadas
    private void VincularDependenciasLocais()
    {
        if (inventoryManager == null)
            inventoryManager = FindObjectOfType<InventoryManager>(true);
        if (dialogueSystem == null)
            dialogueSystem = FindObjectOfType<DialogueSystem>(true);
    }

    public void ForcarAtualizacaoUI()
    {
        OnStatusChanged?.Invoke();
    }

    public void ConfirmarCaso(CaseData caso)
    {
        casoEscolhido = caso;
        if (caso != null && !casosJaSelecionados.Contains(caso))
            casosJaSelecionados.Add(caso);
        Debug.Log($"Caso escolhido e salvo: {caso.caseTitle}");

        // Documento (Tarefas Globais): pop-up quando um caso é escolhido e travado.
        if (caso != null) AvisoNaTela.Mostrar($"Caso aceito: {(caso.caseTitle ?? caso.name).Trim()}");

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotificarEvento(TutorialManager.EVENTO_CASO_ESCOLHIDO);
    }

    public void RegistrarPanfletoDeCaso(CaseData caso, NivelDoPanfleto nivel, ReceitaDeCaso.Versao versao)
    {
        panfletosPublicados.Add(new PanfletoPublicado { caso = caso, nivel = nivel });

        if (nivel != NivelDoPanfleto.Fatos && versao != null && versao.TemRevelacao)
        {
            revelacoesPendentes.Add(new RevelacaoPendente
            {
                caso = caso,
                texto = versao.textoRevelacao,
                povo = versao.penalidadePovo,
                estado = versao.penalidadeEstado
            });
        }
        Debug.Log($"[FATO x BOATO] Panfleto de '{(caso != null ? caso.caseTitle : "?")}' publicado como {nivel}.");
    }

    /// <summary>Devolve e limpa as revelações pendentes (quem chama aplica as penalidades).</summary>
    public List<RevelacaoPendente> ConsumirRevelacoes()
    {
        var lista = new List<RevelacaoPendente>(revelacoesPendentes);
        revelacoesPendentes.Clear();
        return lista;
    }

    /// <summary>Usado pela Mesa de Casos (CaseSelectionUI) para bloquear cartões já escolhidos antes.</summary>
    public bool CasoJaFoiSelecionado(CaseData caso) => caso != null && casosJaSelecionados.Contains(caso);

    public void AplicarImpactoPanfleto(int impactoPublico, int impactoEstado, int ouro)
    {
        opiniaoPublicaAtual += impactoPublico;
        opiniaoEstadoAtual += impactoEstado;
        capitalAtual += ouro;

        opiniaoPublicaAtual = Mathf.Clamp(opiniaoPublicaAtual, 0, 100);
        opiniaoEstadoAtual = Mathf.Clamp(opiniaoEstadoAtual, 0, 100);

        Debug.Log($"[PANFLETO] Povo: {opiniaoPublicaAtual} | Estado: {opiniaoEstadoAtual} | Ouro: {capitalAtual}");
        ForcarAtualizacaoUI(); // Atualiza a tela imediatamente após o craft
    }
}