using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dados da Investigação (Fase Atual)")]
    public CaseData casoEscolhido;

    [Tooltip("Todos os casos que o jogador já escolheu alguma vez (mesmo que não seja mais o atual). " +
             "A Mesa de Casos (CaseSelectionUI) usa isso para bloquear cartões já selecionados.")]
    public List<CaseData> casosJaSelecionados = new List<CaseData>();

    // Chave da partida no ArquivoDeCasos. NonSerialized para o prefab nunca guardar um ID.
    [NonSerialized] public string idDaPartida;
    public string IdDaPartida
    {
        get
        {
            if (string.IsNullOrEmpty(idDaPartida)) idDaPartida = Guid.NewGuid().ToString("N");
            return idDaPartida;
        }
    }

    [Header("Progressão de Fases")]
    [Tooltip("Fase em que o jogador está: 1 = tutorial, 2 e 3 = investigação, 4 = tribunal. " +
             "Avança no escritório (FimDeFase) quando os casos exigidos da fase forem concluídos.")]
    public int faseAtual = 1;
    public const int UltimaFase = 4;

    [Tooltip("Quantos casos o jogador precisa concluir em cada fase para avançar (elemento 0 = Fase 1). " +
             "Decisão do grupo (27/09): nas Fases 2 e 3 o jogador escolhe um caso entre três; os outros ficam bloqueados.")]
    public int[] casosPorFase = { 1, 1, 1, 1 };

    [Tooltip("Casos cujo panfleto já foi impresso. Um caso concluído libera a Mesa de Casos para o próximo.")]
    public List<CaseData> casosConcluidos = new List<CaseData>();

    [Header("Tempo de Investigação")]
    [Tooltip("Horas que o jogador tem para investigar cada caso, se o CaseData não definir outro valor. " +
             "Cada NPC ou objeto investigado pela primeira vez gasta horas (RelogioDeInvestigacao). Decisão do grupo " +
             "(27/09): 6h nas Fases 2, 3 e 4, aumentadas para 8h (todos os casos delas usam este valor); o tutorial não tem relógio.")]
    [Min(1)] public int horasPorCaso = 8;
    [Tooltip("Horas do caso atual (0 = relógio desligado, ex: tutorial).")]
    public int horasDoCaso = 0;
    public int horasRestantes = 0;
    [Tooltip("Interações pagas, objetos vasculhados e recompensas entregues, por caso e interação (ID estável). " +
             "Falar de novo com o mesmo NPC no mesmo caso não gasta tempo nem repete recompensas.")]
    public RegistroDaInvestigacao registroDaInvestigacao = new RegistroDaInvestigacao();

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
             "Abaixo disso, a rota é a C (equilíbrio). Mínimo 1: com 0, o empate iria para a rota A.")]
    [Min(1)] public int margemDaRota = 12; // 12 mantém todo par de casos com 2+ rotas possíveis

    [Header("Status Globais (HUD)")]
    public int capitalAtual = 0;
    public int opiniaoPublicaAtual = 50; 
    public int opiniaoEstadoAtual = 50; 

    [Header("Persistência (Entre Cenas)")]
    public List<Item> inventarioSalvo = new List<Item>();
    [Tooltip("Itens que não couberam na grade ao restaurar o inventário (grade cheia). Não se perdem: vão junto ao trocar " +
             "de cena e no save, e voltam para a grade assim que houver espaço.")]
    public List<Item> itensForaDaGrade = new List<Item>();

    // Snapshot dos valores: mudar receitas depois não altera publicações feitas. legado = save < v4, sem detalhes.
    [Serializable]
    public class PanfletoPublicado
    {
        public CaseData caso;
        public NivelDoPanfleto nivel;
        public bool legado;
        public List<string> pistas = new List<string>();
        public List<string> suportes = new List<string>();
        public List<string> qualificadores = new List<string>();
        [Tooltip("Valores calculados (versão + apoio + linha editorial), antes do limite 0–100.")]
        public int povo, estado, ouro;
        [Tooltip("Penalidade da revelação já com o agravamento do sensacionalista, se houver.")]
        public int penalidadePovo, penalidadeEstado;
        [Tooltip("O que mudou de fato na HUD (Povo/Estado limitados a 0–100). Falso em publicações de saves < 5.")]
        public bool aplicadoConhecido;
        public int povoAplicado, estadoAplicado, ouroAplicado;
        [Tooltip("Linha editorial escolhida. Nenhuma = tutorial/receita sem linha ou publicação de save < 6 (sem modificador).")]
        public LinhaEditorial linha;
        [Tooltip("Parte de povo/estado/ouro que veio da linha editorial, e parte da penalidade que veio do sensacionalista.")]
        public int editorialPovo, editorialEstado, editorialOuro, agravamentoPovo, agravamentoEstado;
    }

    [Serializable]
    public class RevelacaoPendente
    {
        public CaseData caso;
        public string texto;
        public int povo;
        public int estado;
        [Tooltip("Linha editorial do panfleto desmentido (a cutscene conta quando o sensacionalista agravou a perda).")]
        public LinhaEditorial linha;
        [Tooltip("A penalidade já entrou nas barras (saves do curto período em que ela era cobrada na impressão). " +
                 "Falso = cobrada no fim da fase, que é o normal.")]
        public bool aplicada;
        // Perda efetiva após o limite 0–100, para o aviso pós-cutscene.
        [NonSerialized] public Vector2Int perdaAplicada;
    }

    // Perda já aplicada nos números (a rota depende dela) mas retida na HUD até o fim da cutscene; a HUD exibe
    // valor − isto até ExibirDesmentido. Não vai para o save.
    [NonSerialized] public Vector2Int desmentidoNaoExibido;

    public void ExibirDesmentido()
    {
        if (desmentidoNaoExibido == Vector2Int.zero) return;
        desmentidoNaoExibido = Vector2Int.zero;
        OnStatusChanged?.Invoke();
    }

    [Header("Fato x Boato")]
    [Tooltip("Histórico de panfletos de caso impressos e a versão de cada um (útil para o Tribunal na Fase 4).")]
    public List<PanfletoPublicado> panfletosPublicados = new List<PanfletoPublicado>();
    [Tooltip("Boatos publicados na fase. A penalidade entra nas barras na impressão; a cutscene de fim de fase (EncerrarFase) conta o desmentido.")]
    public List<RevelacaoPendente> revelacoesPendentes = new List<RevelacaoPendente>();
    [Tooltip("itemIDs de pistas cuja verdade o jogador já descobriu. Guardado aqui para a ficha não voltar a " +
             "'não verificada' quando o item que confirmou a pista for gasto na prensa.")]
    public List<string> pistasVerificadas = new List<string>();
    [Tooltip("itemIDs de toda evidência de caso (pista ou documento) que já entrou no inventário, mesmo se gasta " +
             "depois na prensa. Pré-requisito das etapas complementares dos NPCs.")]
    public List<string> evidenciasObtidas = new List<string>();

    [Header("Biblioteca (Fase 3 em diante)")]
    [Tooltip("\"oferta|caso\" das compras feitas (OfertaDaBiblioteca.ChaveDeCompra).")]
    public List<string> comprasDaBiblioteca = new List<string>();

    [Header("Dedução ativa (Prompt 4; Fases 2, 3 e 4 desde o Prompt 7)")]
    [Tooltip("Escolhas do jogador no quadro de dedução: em cada par contraditório, a pista com que ficou e a descartada " +
             "(Deducao). Nunca mudam a verdade das pistas.")]
    public List<EscolhaDeDeducao> escolhasDeDeducao = new List<EscolhaDeDeducao>();

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
        DontDestroyOnLoad(gameObject);
        VincularDependenciasLocais();
        // Aplica o save no Awake, antes de o inventário da cena restaurar os itens no Start.
        SistemaDeSave.AplicarPendente(this);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SISTEMA] Cena '{scene.name}' carregada. Sincronizando HUD...");
        VincularDependenciasLocais();
        IdDeInteracao.ValidarCena(scene);
        ForcarAtualizacaoUI();
    }

    private void VincularDependenciasLocais()
    {
        if (inventoryManager == null)
            inventoryManager = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (dialogueSystem == null)
            dialogueSystem = FindAnyObjectByType<DialogueSystem>(FindObjectsInactive.Include);
    }

    public void ForcarAtualizacaoUI()
    {
        OnStatusChanged?.Invoke();
    }

    // Regra de domínio da Mesa de Casos, independente da UI: um caso por vez, da fase e rota atuais, nunca repetido.
    public bool PodeAceitarCaso(CaseData caso, out string motivo)
    {
        motivo = null;
        if (caso == null) { motivo = "Nenhum caso selecionado."; return false; }
        if (casosConcluidos.Contains(caso)) { motivo = "Este caso já foi concluído."; return false; }
        if (caso == casoEscolhido && CasoAtualEmAndamento) return true;
        if (CasoAtualEmAndamento)
        {
            motivo = $"Termine o caso atual antes de aceitar outro: {TituloDe(casoEscolhido)}";
            return false;
        }
        if (casosJaSelecionados.Contains(caso)) { motivo = "Este caso já foi escolhido antes."; return false; }
        if (caso.fase != faseAtual) { motivo = "Este caso não pertence à fase atual."; return false; }
        if (FaseConcluida) { motivo = "Os casos desta fase já foram concluídos."; return false; }
        if (!CasoDaRotaAtual(caso)) { motivo = "Este caso não pertence a esta rota."; return false; }
        return true;
    }

    // Antes da Fase 4 valem os casos sem rota; na Fase 4, só o da rota travada.
    public bool CasoDaRotaAtual(CaseData caso)
    {
        if (caso == null) return false;
        if (caso.rota == RotaFinal.Nenhuma) return faseAtual < UltimaFase;
        return caso.rota == rotaFinal;
    }

    public bool TutorialConcluido => faseAtual > 1 || casosConcluidos.Exists(c => c != null && c.fase == 1);

    // Reconfirmar o caso em andamento não reinicia o relógio.
    public bool ConfirmarCaso(CaseData caso)
    {
        if (!PodeAceitarCaso(caso, out string motivo))
        {
            Debug.LogWarning($"[CASOS] Caso '{(caso != null ? caso.name : "-")}' recusado: {motivo}");
            AvisoNaTela.Mostrar(motivo);
            return false;
        }
        if (caso == casoEscolhido && CasoAtualEmAndamento)
        {
            Debug.Log($"[CASOS] '{caso.name}' já está em andamento: o relógio não foi reiniciado.");
            return true;
        }

        casoEscolhido = caso;
        if (!casosJaSelecionados.Contains(caso))
            casosJaSelecionados.Add(caso);
        Debug.Log($"Caso escolhido e salvo: {caso.caseTitle}");

        AvisoNaTela.Mostrar($"Caso aceito: {TituloDe(caso)}");
        IniciarRelogio(caso);
        EntregarAlegacoesPendentes();

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotificarEvento(TutorialManager.EVENTO_CASO_ESCOLHIDO);
        return true;
    }

    // Interação fictícia para registrar a entrega das alegações iniciais (uma vez por caso).
    public const string InteracaoAlegacoes = "mesa/alegacoes_do_cliente";

    // Chamado ao aceitar o caso e a cada restauração do inventário: com inventário cheio, fica para depois.
    public void EntregarAlegacoesPendentes()
    {
        CaseData caso = casoEscolhido;
        if (caso == null || !CasoAtualEmAndamento || caso.alegacoesIniciais == null || inventoryManager == null) return;

        bool faltou = false;
        foreach (Item alegacao in caso.alegacoesIniciais)
        {
            if (alegacao == null || string.IsNullOrEmpty(alegacao.itemID)) continue;
            if (registroDaInvestigacao.RecompensaEntregue(caso, InteracaoAlegacoes, RegistroDaInvestigacao.EtapaReacao, alegacao.itemID)) continue;

            Item copia = alegacao.Clone();
            copia.itemAmt = 1;
            if (inventoryManager.AddItem(copia))
                registroDaInvestigacao.RegistrarRecompensa(caso, InteracaoAlegacoes, RegistroDaInvestigacao.EtapaReacao, alegacao.itemID);
            else
                faltou = true;
        }
        if (faltou) AvisoNaTela.Mostrar("Inventário cheio: libere espaço para guardar a carta do cliente.");
    }

    // A grade é limitada e não há descarte: cartas do cliente saem sempre; pistas e documentos só antes da Fase 4
    // (que leva as provas ao tribunal). Panfletos não têm caso e ficam.
    private void ArquivarSobrasDoCaso(CaseData caso)
    {
        if (caso == null || inventoryManager == null) return;
        bool tudoDoCaso = caso.fase < UltimaFase;
        int removidos = inventoryManager.RemoverItens(item => caso.EhAlegacao(item) || (tudoDoCaso && item.caso == caso));
        if (removidos > 0)
            AvisoNaTela.Mostrar(tudoDoCaso ? "Os papéis do caso foram arquivados na tipografia." : "As cartas do cliente foram arquivadas com o caso.");
    }

    private static string TituloDe(CaseData caso) =>
        caso == null ? "-" : (string.IsNullOrWhiteSpace(caso.caseTitle) ? caso.name : caso.caseTitle).Trim();

    // Só o caso em andamento, uma única vez. A prensa checa antes de consumir as pistas.
    public bool PodePublicarCaso(CaseData caso, out string motivo)
    {
        motivo = null;
        if (caso == null) { motivo = "Não há caso em andamento para publicar."; return false; }
        if (casosConcluidos.Contains(caso) || FoiPublicado(caso)) { motivo = "O panfleto deste caso já foi publicado."; return false; }
        if (caso != casoEscolhido) { motivo = "Essas pistas não são do caso que estou investigando."; return false; }
        return true;
    }

    public bool FoiPublicado(CaseData caso) => caso != null && panfletosPublicados.Exists(p => p.caso == caso);

    // Nulo para o tutorial, que não registra publicação.
    public PanfletoPublicado PublicacaoDoPanfleto(Item panfleto)
    {
        if (panfleto == null) return null;
        foreach (PanfletoPublicado p in panfletosPublicados)
        {
            ReceitaDeCaso receita = p != null && p.caso != null ? p.caso.receitaDoPanfleto : null;
            Item impresso = receita != null ? receita.PanfletoDe(receita.VersaoDe(p.nivel)) : null;
            if (impresso != null && impresso.Matches(panfleto)) return p;
        }
        return null;
    }

    // Compatibilidade: registra a partir de uma versão da receita, sem pistas/suportes.
    public void RegistrarPanfletoDeCaso(CaseData caso, NivelDoPanfleto nivel, ReceitaDeCaso.Versao versao)
    {
        RegistrarPanfletoDeCaso(caso, new ResultadoDoPanfleto
        {
            nivel = nivel,
            povo = versao != null ? versao.povo : 0,
            estado = versao != null ? versao.estado : 0,
            ouro = versao != null ? versao.ouro : 0,
            penalidadePovo = versao != null ? versao.penalidadePovo : 0,
            penalidadeEstado = versao != null ? versao.penalidadeEstado : 0,
            textoRevelacao = versao != null ? versao.textoRevelacao : null,
        });
    }

    // aplicado = o que mudou de fato após o limite 0–100.
    public void RegistrarPanfletoDeCaso(CaseData caso, ResultadoDoPanfleto resultado, Vector3Int? aplicado = null)
    {
        if (resultado == null) return;
        if (FoiPublicado(caso))
        {
            Debug.LogWarning($"[FATO x BOATO] '{(caso != null ? caso.name : "?")}' já tinha panfleto publicado: registro ignorado.");
            return;
        }
        panfletosPublicados.Add(new PanfletoPublicado
        {
            caso = caso,
            nivel = resultado.nivel,
            pistas = new List<string>(resultado.pistas),
            suportes = new List<string>(resultado.suportes),
            qualificadores = new List<string>(resultado.qualificadores),
            povo = resultado.povo, estado = resultado.estado, ouro = resultado.ouro,
            penalidadePovo = resultado.penalidadePovo, penalidadeEstado = resultado.penalidadeEstado,
            aplicadoConhecido = aplicado.HasValue,
            povoAplicado = aplicado?.x ?? 0, estadoAplicado = aplicado?.y ?? 0, ouroAplicado = aplicado?.z ?? 0,
            linha = resultado.linha,
            editorialPovo = resultado.editorialPovo, editorialEstado = resultado.editorialEstado, editorialOuro = resultado.editorialOuro,
            agravamentoPovo = resultado.agravamentoPovo, agravamentoEstado = resultado.agravamentoEstado,
        });

        // Fatos nunca gera revelação: o sensacionalista só agrava uma mentira que existe.
        if (resultado.nivel != NivelDoPanfleto.Fatos && resultado.TemRevelacao)
        {
            // Cobrada no EncerrarFase. Ganho e perda valem juntos contra o limite 0–100: se o ganho bateu no teto
            // (80 + 40 = 100), a perda desconta o que o teto comeu (80 → 100 → 75, não 55).
            int perdaPovo = resultado.penalidadePovo, perdaEstado = resultado.penalidadeEstado;
            if (aplicado.HasValue)
            {
                perdaPovo = PerdaContraOLimite(opiniaoPublicaAtual, aplicado.Value.x, resultado.povo, resultado.penalidadePovo);
                perdaEstado = PerdaContraOLimite(opiniaoEstadoAtual, aplicado.Value.y, resultado.estado, resultado.penalidadeEstado);
            }
            revelacoesPendentes.Add(new RevelacaoPendente
            {
                caso = caso,
                texto = resultado.textoRevelacao,
                povo = perdaPovo,
                estado = perdaEstado,
                linha = resultado.linha,
                aplicada = false
            });
        }
        Debug.Log($"[FATO x BOATO] Panfleto de '{(caso != null ? caso.caseTitle : "?")}' publicado como {resultado.nivel}" +
                  (resultado.linha != LinhaEditorial.Nenhuma ? $", linha {resultado.linha}" : string.Empty) +
                  (resultado.suportes.Count > 0 ? $", com apoio de {string.Join(", ", resultado.suportes)}." : "."));
    }

    public const float DuracaoDoAvisoDeDesmentido = 5f;

    // alvo = Clamp(valorAntesDoGanho + ganho + penalidade); retorna a distância do valor atual até o alvo.
    public static int PerdaContraOLimite(int valorAtual, int ganhoAplicado, int ganhoCalculado, int penalidade)
    {
        if (penalidade == 0) return 0;
        int antes = valorAtual - ganhoAplicado;
        return Mathf.Clamp(antes + ganhoCalculado + penalidade, 0, 100) - valorAtual;
    }

    private static string Perda(int valor, string barra) =>
        valor == 0 ? string.Empty : $" ({(valor > 0 ? "+" : string.Empty)}{valor} {barra})";

    // ===== Evidências e Biblioteca =====

    // Chamado pelo InventoryManager.AddItem: o pré-requisito continua cumprido mesmo depois de o item ser gasto.
    public void RegistrarEvidencia(Item item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID)) return;
        if (!evidenciasObtidas.Contains(item.itemID)) evidenciasObtidas.Add(item.itemID);
    }

    // Fallback para saves antigos: o que o jogador tem agora (grade, prensa ou mão).
    public bool EvidenciaObtida(Item item) =>
        item != null && !string.IsNullOrEmpty(item.itemID) &&
        (evidenciasObtidas.Contains(item.itemID) || (inventoryManager != null && inventoryManager.PossuiEmQualquerLugar(item.itemID)));

    public enum EstadoDaOferta
    {
        Disponivel,
        Comprada,
        JaObtida,              // o próprio documento já veio por outro caminho
        ApoioEquivalente,      // outro documento já obtido ativa o mesmo reforço
        CasoNaoAceito,
        OutroCasoEmAndamento,
        CasoConcluido,
        FaseBloqueada,
        Invalida
    }
    public enum ResultadoDaCompra { Comprada, JaComprada, SaldoInsuficiente, Indisponivel, InventarioCheio, Invalida }

    public EstadoDaOferta EstadoDe(OfertaDaBiblioteca oferta)
    {
        if (oferta == null || oferta.caso == null || oferta.item == null || string.IsNullOrEmpty(oferta.id)) return EstadoDaOferta.Invalida;
        if (comprasDaBiblioteca.Contains(oferta.ChaveDeCompra)) return EstadoDaOferta.Comprada;
        if (faseAtual < oferta.faseMinima) return EstadoDaOferta.FaseBloqueada;
        if (casosConcluidos.Contains(oferta.caso)) return EstadoDaOferta.CasoConcluido;
        if (CasoAtualEmAndamento && casoEscolhido != oferta.caso) return EstadoDaOferta.OutroCasoEmAndamento;
        if (!CasoAtualEmAndamento || casoEscolhido != oferta.caso) return EstadoDaOferta.CasoNaoAceito;
        if (EvidenciaObtida(oferta.item)) return EstadoDaOferta.JaObtida;
        if (ReforcoJaGarantido(oferta)) return EstadoDaOferta.ApoioEquivalente;
        return EstadoDaOferta.Disponivel;
    }

    // Todo qualificador que aceita o documento da oferta já é ativado por outro documento que o jogador obteve.
    private bool ReforcoJaGarantido(OfertaDaBiblioteca oferta)
    {
        ReceitaDeCaso receita = oferta.caso.receitaDoPanfleto;
        if (receita == null || receita.qualificadores == null) return false;
        bool algum = false;
        foreach (ReceitaDeCaso.Qualificador q in receita.qualificadores)
        {
            if (q == null || !q.Aceita(oferta.item)) continue;
            algum = true;
            bool garantido = q.suportesAceitos.Exists(s => s != null && s.itemID != oferta.item.itemID && q.Aceita(s) && EvidenciaObtida(s));
            if (!garantido) return false;
        }
        return algum;
    }

    private bool comprando;

    // Transacional: entrega o item e só então cobra. Sem crédito; inventário cheio recusa sem cobrar.
    public ResultadoDaCompra ComprarNaBiblioteca(OfertaDaBiblioteca oferta, Func<Item, bool> entregar = null)
    {
        if (comprando) return ResultadoDaCompra.Indisponivel; // reentrância por clique repetido
        comprando = true;
        try
        {
            switch (EstadoDe(oferta))
            {
                case EstadoDaOferta.Invalida: return ResultadoDaCompra.Invalida;
                case EstadoDaOferta.Comprada: return ResultadoDaCompra.JaComprada;
                case EstadoDaOferta.Disponivel: break;
                default: return ResultadoDaCompra.Indisponivel;
            }
            if (capitalAtual < 0 || capitalAtual < oferta.preco) return ResultadoDaCompra.SaldoInsuficiente;
            if (entregar == null)
            {
                if (inventoryManager == null) return ResultadoDaCompra.Indisponivel;
                entregar = inventoryManager.AddItem;
            }

            Item copia = oferta.item.Clone();
            copia.itemAmt = 1;
            if (!entregar(copia)) return ResultadoDaCompra.InventarioCheio;

            capitalAtual -= oferta.preco;
            comprasDaBiblioteca.Add(oferta.ChaveDeCompra);
            RegistrarEvidencia(oferta.item);
            Debug.Log($"[BIBLIOTECA] '{oferta.id}' comprado por {oferta.preco}. Capital: {capitalAtual}.");
            ForcarAtualizacaoUI();
            return ResultadoDaCompra.Comprada;
        }
        finally
        {
            comprando = false;
        }
    }

    // Quem chama aplica as penalidades.
    public List<RevelacaoPendente> ConsumirRevelacoes()
    {
        var lista = new List<RevelacaoPendente>(revelacoesPendentes);
        revelacoesPendentes.Clear();
        return lista;
    }

    public class EncerramentoDeFase
    {
        public int fase;
        public int despesa;
        public List<RevelacaoPendente> revelacoes = new List<RevelacaoPendente>();
    }

    // Ordem: boatos, despesas, avanço. Penalidades antes do avanço porque entrar na Fase 4 trava a rota pelas barras.
    public EncerramentoDeFase EncerrarFase()
    {
        var resultado = new EncerramentoDeFase { fase = faseAtual };
        foreach (RevelacaoPendente revelacao in ConsumirRevelacoes())
        {
            if (!revelacao.aplicada)
            {
                Vector3Int perda = AplicarImpactoPanfleto(revelacao.povo, revelacao.estado, 0);
                revelacao.perdaAplicada = new Vector2Int(perda.x, perda.y);
                desmentidoNaoExibido += revelacao.perdaAplicada;
            }
            resultado.revelacoes.Add(revelacao);
        }
        resultado.despesa = PagarDespesasDaFase(resultado.fase);
        AvancarFase();
        ArquivoDeCasos.Registrar(this);
        return resultado;
    }

    public bool CasoJaFoiSelecionado(CaseData caso) => caso != null && casosJaSelecionados.Contains(caso);

    // ===== Progressão de Fases =====

    // Caso aceito sem panfleto impresso: trava a mesa e libera a porta do escritório.
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

    public bool ConcluirCaso(CaseData caso)
    {
        if (caso == null || casosConcluidos.Contains(caso)) return false;
        casosConcluidos.Add(caso);
        Debug.Log($"[FASES] Caso '{caso.name}' concluído ({CasosConcluidosNaFase}/{CasosNecessariosNaFase} da Fase {faseAtual}).");

        if (!string.IsNullOrWhiteSpace(caso.caseTitle))
            AvisoNaTela.Mostrar($"Caso concluído: {caso.caseTitle.Trim()}");
        ArquivarSobrasDoCaso(caso);
        ArquivoDeCasos.Registrar(this);
        return true;
    }

    // ===== Tempo de investigação e despesas =====

    // O registroDaInvestigacao é separado por caso: não precisa limpar aqui.
    private void IniciarRelogio(CaseData caso)
    {
        if (caso == null) { horasDoCaso = horasRestantes = 0; return; }

        horasDoCaso = caso.horasDeInvestigacao > 0 ? caso.horasDeInvestigacao : horasPorCaso;
        if (Endividado && horasPerdidasPorDivida > 0)
        {
            horasDoCaso = Mathf.Max(1, horasDoCaso - horasPerdidasPorDivida);
            AvisoNaTela.Mostrar($"Endividado: os credores tomam parte do seu dia (-{horasPerdidasPorDivida}h de investigação).");
        }
        horasRestantes = horasDoCaso;
    }

    // O capital pode ficar negativo (dívida).
    public int PagarDespesasDaFase(int fase)
    {
        int valor = fase >= 1 && fase <= despesasPorFase.Length ? Mathf.Max(0, despesasPorFase[fase - 1]) : 0;
        if (valor == 0) return 0;
        capitalAtual -= valor;
        Debug.Log($"[DESPESAS] Fase {fase}: -{valor} moedas. Capital: {capitalAtual}.");
        ForcarAtualizacaoUI();
        return valor;
    }

    public bool AvancarFase()
    {
        if (faseAtual >= UltimaFase) return false;
        faseAtual++;
        Debug.Log($"[FASES] Início da Fase {faseAtual}.");
        if (faseAtual == UltimaFase) DefinirRota();
        return true;
    }

    // Desnível Povo − Estado ≥ margem → A; ≤ −margem → B; senão C.
    public RotaFinal CalcularRota()
    {
        int margem = MargemValida;
        int desnivel = opiniaoPublicaAtual - opiniaoEstadoAtual;
        if (desnivel >= margem) return RotaFinal.A_Guilhotina;
        if (desnivel <= -margem) return RotaFinal.B_Tirano;
        return RotaFinal.C_Equilibrio;
    }

    // Mínimo 1: com margem 0 o empate cairia na rota A em vez da C.
    public int MargemValida
    {
        get
        {
            if (margemDaRota >= 1) return margemDaRota;
            Debug.LogWarning($"[FASES] Margem da rota inválida ({margemDaRota}): usando 1, para o empate continuar na rota C.");
            return 1;
        }
    }

    public void DefinirRota()
    {
        rotaFinal = CalcularRota();
        Debug.Log($"[FASES] Rota final travada: {rotaFinal} (Povo {opiniaoPublicaAtual} x Estado {opiniaoEstadoAtual}).");
    }

    // Retorna a variação efetiva após o limite 0–100 (x = Povo, y = Estado, z = Ouro).
    public Vector3Int AplicarImpactoPanfleto(int impactoPublico, int impactoEstado, int ouro)
    {
        int povoAntes = opiniaoPublicaAtual, estadoAntes = opiniaoEstadoAtual;
        opiniaoPublicaAtual += impactoPublico;
        opiniaoEstadoAtual += impactoEstado;
        capitalAtual += ouro;

        opiniaoPublicaAtual = Mathf.Clamp(opiniaoPublicaAtual, 0, 100);
        opiniaoEstadoAtual = Mathf.Clamp(opiniaoEstadoAtual, 0, 100);

        Debug.Log($"[PANFLETO] Povo: {opiniaoPublicaAtual} | Estado: {opiniaoEstadoAtual} | Ouro: {capitalAtual}");
        ForcarAtualizacaoUI();
        return new Vector3Int(opiniaoPublicaAtual - povoAntes, opiniaoEstadoAtual - estadoAntes, ouro);
    }
}