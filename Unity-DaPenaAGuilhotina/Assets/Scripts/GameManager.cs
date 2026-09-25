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

    [Header("Progressão de Fases")]
    [Tooltip("Fase em que o jogador está: 1 = tutorial, 2 e 3 = investigação, 4 = tribunal. " +
             "Avança no escritório (FimDeFase) quando os casos exigidos da fase forem concluídos.")]
    public int faseAtual = 1;
    public const int UltimaFase = 4;

    [Tooltip("Quantos casos o jogador precisa concluir em cada fase para avançar (elemento 0 = Fase 1). " +
             "Documento: a partir da Fase 3 o jogador escolhe mais de um caso por fase.")]
    public int[] casosPorFase = { 1, 1, 2, 1 };

    [Tooltip("Casos cujo panfleto já foi impresso. Um caso concluído libera a Mesa de Casos para o próximo.")]
    public List<CaseData> casosConcluidos = new List<CaseData>();

    [Header("Tempo de Investigação")]
    [Tooltip("Horas que o jogador tem para investigar cada caso, se o CaseData não definir outro valor. " +
             "Cada NPC ou objeto investigado pela primeira vez gasta horas (RelogioDeInvestigacao).")]
    [Min(1)] public int horasPorCaso = 4;
    [Tooltip("Horas do caso atual (0 = relógio desligado, ex: tutorial).")]
    public int horasDoCaso = 0;
    public int horasRestantes = 0;
    [Tooltip("NPCs/objetos já pagos no caso atual: falar de novo com eles não gasta tempo.")]
    public List<string> interacoesPagas = new List<string>();

    [Header("Despesas da Tipografia")]
    [Tooltip("Moedas cobradas ao fim de cada fase (elemento 0 = Fase 1): aluguel, papel e tinta.")]
    public int[] despesasPorFase = { 0, 25, 50, 0 };
    [Tooltip("Horas de investigação perdidas por caso enquanto o jogador estiver endividado (capital negativo).")]
    [Min(0)] public int horasPerdidasPorDivida = 1;

    public bool Endividado => capitalAtual < 0;

    [Header("Rota Final (Fase 4)")]
    [Tooltip("Travada ao entrar na Fase 4 pelo desnível entre Povo e Estado. Define o caso da Fase 4 e o final.")]
    public RotaFinal rotaFinal = RotaFinal.Nenhuma;
    [Tooltip("Diferença mínima entre Opinião Pública e Opinião do Estado para a rota pender para um lado. " +
             "Abaixo disso, a rota é a C (equilíbrio).")]
    [Min(0)] public int margemDaRota = 20;

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
        // Continuar do menu: preenche os dados do save antes de o inventário da cena restaurar os itens (Start).
        SistemaDeSave.AplicarPendente(this);
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
        IniciarRelogio(caso);

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

    // ===== Progressão de Fases =====

    /// <summary>Há um caso aceito cujo panfleto ainda não foi impresso: trava a mesa e libera a porta do escritório.</summary>
    public bool CasoAtualEmAndamento => casoEscolhido != null && !casosConcluidos.Contains(casoEscolhido);

    public int CasosNecessariosNaFase =>
        faseAtual >= 1 && faseAtual <= casosPorFase.Length ? Mathf.Max(1, casosPorFase[faseAtual - 1]) : 1;

    public int CasosConcluidosNaFase
    {
        get
        {
            int total = 0;
            foreach (CaseData caso in casosConcluidos)
                if (caso != null && caso.fase == faseAtual) total++;
            return total;
        }
    }

    public bool FaseConcluida => CasosConcluidosNaFase >= CasosNecessariosNaFase;

    /// <summary>Chamado pela prensa ao imprimir o panfleto do caso.</summary>
    public void ConcluirCaso(CaseData caso)
    {
        if (caso == null || casosConcluidos.Contains(caso)) return;
        casosConcluidos.Add(caso);
        Debug.Log($"[FASES] Caso '{caso.name}' concluído ({CasosConcluidosNaFase}/{CasosNecessariosNaFase} da Fase {faseAtual}).");

        if (!string.IsNullOrWhiteSpace(caso.caseTitle))
            AvisoNaTela.Mostrar($"Caso concluído: {caso.caseTitle.Trim()}");
    }

    // ===== Tempo de investigação e despesas =====

    private void IniciarRelogio(CaseData caso)
    {
        interacoesPagas.Clear();
        if (caso == null) { horasDoCaso = horasRestantes = 0; return; }

        horasDoCaso = caso.horasDeInvestigacao > 0 ? caso.horasDeInvestigacao : horasPorCaso;
        if (Endividado && horasPerdidasPorDivida > 0)
        {
            horasDoCaso = Mathf.Max(1, horasDoCaso - horasPerdidasPorDivida);
            AvisoNaTela.Mostrar($"Endividado: os credores tomam parte do seu dia (-{horasPerdidasPorDivida}h de investigação).");
        }
        horasRestantes = horasDoCaso;
    }

    /// <summary>Cobra as despesas da fase que terminou. Devolve o valor cobrado (o capital pode ficar negativo = dívida).</summary>
    public int PagarDespesasDaFase(int fase)
    {
        int valor = fase >= 1 && fase <= despesasPorFase.Length ? Mathf.Max(0, despesasPorFase[fase - 1]) : 0;
        if (valor == 0) return 0;
        capitalAtual -= valor;
        Debug.Log($"[DESPESAS] Fase {fase}: -{valor} moedas. Capital: {capitalAtual}.");
        ForcarAtualizacaoUI();
        return valor;
    }

    /// <summary>Passa para a próxima fase. Falso se já está na última.</summary>
    public bool AvancarFase()
    {
        if (faseAtual >= UltimaFase) return false;
        faseAtual++;
        Debug.Log($"[FASES] Início da Fase {faseAtual}.");
        if (faseAtual == UltimaFase) DefinirRota();
        return true;
    }

    /// <summary>Documento (Fase 4, "Definição de Rota"): Povo alto e Estado baixo = A; o contrário = B; equilíbrio = C.</summary>
    public RotaFinal CalcularRota()
    {
        int desnivel = opiniaoPublicaAtual - opiniaoEstadoAtual;
        if (desnivel >= margemDaRota) return RotaFinal.A_Guilhotina;
        if (desnivel <= -margemDaRota) return RotaFinal.B_Tirano;
        return RotaFinal.C_Equilibrio;
    }

    public void DefinirRota()
    {
        rotaFinal = CalcularRota();
        Debug.Log($"[FASES] Rota final travada: {rotaFinal} (Povo {opiniaoPublicaAtual} x Estado {opiniaoEstadoAtual}).");
    }

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