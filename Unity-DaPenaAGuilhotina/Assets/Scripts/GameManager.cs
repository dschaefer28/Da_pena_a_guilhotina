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
             "Abaixo disso, a rota é a C (equilíbrio).")]
    [Min(0)] public int margemDaRota = 20;

    [Header("Status Globais (HUD)")]
    public int capitalAtual = 0;
    public int opiniaoPublicaAtual = 50; 
    public int opiniaoEstadoAtual = 50; 

    [Header("Persistência (Entre Cenas)")]
    public List<Item> inventarioSalvo = new List<Item>();

    /// <summary>Histórico de uma publicação com SNAPSHOT dos valores aplicados: mudar receitas depois não altera
    /// publicações já feitas. "legado" = veio de um save anterior à versão 4, sem esses detalhes.</summary>
    [Serializable]
    public class PanfletoPublicado
    {
        public CaseData caso;
        public NivelDoPanfleto nivel;
        public bool legado;
        public List<string> pistas = new List<string>();
        public List<string> suportes = new List<string>();
        public List<string> qualificadores = new List<string>();
        public int povo, estado, ouro;
        public int penalidadePovo, penalidadeEstado;
        [Tooltip("O que mudou de fato na HUD (Povo/Estado limitados a 0–100). Falso em publicações de saves < 5.")]
        public bool aplicadoConhecido;
        public int povoAplicado, estadoAplicado, ouroAplicado;
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
    [Tooltip("Boatos publicados que ainda vão ser expostos. Aplicados no fim da fase (EncerrarFase), antes da rota.")]
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
        IdDeInteracao.ValidarCena(scene);
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

    /// <summary>Regra de domínio da Mesa de Casos (vale mesmo se a UI deixar passar um clique): um caso por vez,
    /// da fase atual e da rota travada, nunca um já concluído ou já escolhido antes.</summary>
    public bool PodeAceitarCaso(CaseData caso, out string motivo)
    {
        motivo = null;
        if (caso == null) { motivo = "Nenhum caso selecionado."; return false; }
        if (casosConcluidos.Contains(caso)) { motivo = "Este caso já foi concluído."; return false; }
        if (caso == casoEscolhido && CasoAtualEmAndamento) return true; // mesmo caso: nada a fazer
        if (CasoAtualEmAndamento)
        {
            motivo = $"Termine o caso atual antes de aceitar outro: {TituloDe(casoEscolhido)}";
            return false;
        }
        if (casosJaSelecionados.Contains(caso)) { motivo = "Este caso já foi escolhido antes."; return false; }
        if (caso.fase != faseAtual) { motivo = "Este caso não pertence à fase atual."; return false; }
        if (FaseConcluida) { motivo = "Os casos desta fase já foram concluídos."; return false; }
        if (caso.rota != RotaFinal.Nenhuma && caso.rota != rotaFinal) { motivo = "Este caso não pertence a esta rota."; return false; }
        return true;
    }

    /// <summary>Aceita o caso. Falso se a regra de domínio recusar. Confirmar de novo o caso em andamento não
    /// reinicia o relógio (o orçamento de horas já gasto continua valendo).</summary>
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

        // Documento (Tarefas Globais): pop-up quando um caso é escolhido e travado.
        AvisoNaTela.Mostrar($"Caso aceito: {TituloDe(caso)}");
        IniciarRelogio(caso);
        EntregarAlegacoesPendentes();

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotificarEvento(TutorialManager.EVENTO_CASO_ESCOLHIDO);
        return true;
    }

    /// <summary>Interação fictícia usada para registrar a entrega das alegações iniciais (uma vez por caso).</summary>
    public const string InteracaoAlegacoes = "mesa/alegacoes_do_cliente";

    /// <summary>Entrega as alegações iniciais do caso em andamento que ainda não entraram no inventário. Chamado ao
    /// aceitar o caso e sempre que o inventário de uma cena é restaurado: com o inventário cheio, fica para depois.</summary>
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

    /// <summary>
    /// Ao concluir um caso, o que só servia a ele sai do inventário (a grade tem 24 espaços e não há descarte):
    /// - alegações do cliente: sempre;
    /// - sobras de pistas e documentos de apoio: nos casos antes da Fase 4 (o caso da Fase 4 leva suas provas ao
    ///   tribunal). O histórico (evidenciasObtidas, panfletosPublicados) continua guardando tudo.
    /// Panfletos não têm caso e ficam.
    /// </summary>
    private void ArquivarSobrasDoCaso(CaseData caso)
    {
        if (caso == null || inventoryManager == null) return;
        bool tudoDoCaso = caso.fase < UltimaFase;
        int removidos = inventoryManager.RemoverItens(item => caso.EhAlegacao(item) || (tudoDoCaso && item.caso == caso));
        if (removidos > 0)
            AvisoNaTela.Mostrar(tudoDoCaso ? "Os papéis do caso foram arquivados na tipografia." : "As alegações do cliente foram arquivadas com o caso.");
    }

    private static string TituloDe(CaseData caso) =>
        caso == null ? "-" : (string.IsNullOrWhiteSpace(caso.caseTitle) ? caso.name : caso.caseTitle).Trim();

    /// <summary>Regra de domínio da prensa: só o caso em andamento pode ser publicado, e uma única vez.
    /// A prensa checa isto ANTES de consumir as pistas.</summary>
    public bool PodePublicarCaso(CaseData caso, out string motivo)
    {
        motivo = null;
        if (caso == null) { motivo = "Não há caso em andamento para publicar."; return false; }
        if (casosConcluidos.Contains(caso) || FoiPublicado(caso)) { motivo = "O panfleto deste caso já foi publicado."; return false; }
        if (caso != casoEscolhido) { motivo = "Essas pistas não são do caso que estou investigando."; return false; }
        return true;
    }

    public bool FoiPublicado(CaseData caso) => caso != null && panfletosPublicados.Exists(p => p.caso == caso);

    /// <summary>Compatibilidade: registra a partir de uma versão da receita, sem pistas/suportes.</summary>
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

    /// <summary>Guarda a publicação (snapshot dos valores calculados e, se informado, do que foi de fato aplicado
    /// depois do limite 0–100) e agenda a revelação, uma única vez por caso.</summary>
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
        });

        if (resultado.nivel != NivelDoPanfleto.Fatos && resultado.TemRevelacao)
        {
            revelacoesPendentes.Add(new RevelacaoPendente
            {
                caso = caso,
                texto = resultado.textoRevelacao,
                povo = resultado.penalidadePovo,
                estado = resultado.penalidadeEstado
            });
        }
        Debug.Log($"[FATO x BOATO] Panfleto de '{(caso != null ? caso.caseTitle : "?")}' publicado como {resultado.nivel}" +
                  (resultado.suportes.Count > 0 ? $" com apoio de {string.Join(", ", resultado.suportes)}." : "."));
    }

    // ===== Evidências e Biblioteca =====

    /// <summary>Registra que um item entrou no inventário (chamado pelo InventoryManager.AddItem). Vale para qualquer
    /// item com itemID — assim um pré-requisito continua cumprido mesmo depois de o item ser gasto.</summary>
    public void RegistrarEvidencia(Item item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID)) return;
        if (!evidenciasObtidas.Contains(item.itemID)) evidenciasObtidas.Add(item.itemID);
    }

    /// <summary>O jogador já obteve esta evidência alguma vez (mesmo se a gastou). Saves antigos: vale o que ele tem
    /// (grade, prensa ou mão).</summary>
    public bool EvidenciaObtida(Item item) =>
        item != null && !string.IsNullOrEmpty(item.itemID) &&
        (evidenciasObtidas.Contains(item.itemID) || (inventoryManager != null && inventoryManager.PossuiEmQualquerLugar(item.itemID)));

    public enum EstadoDaOferta
    {
        Disponivel,
        Comprada,
        JaObtida,              // o próprio documento já veio por outro caminho
        ApoioEquivalente,      // outro documento já obtido ativa o mesmo reforço: comprar não mudaria nada
        CasoNaoAceito,         // nenhum caso em andamento
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

    /// <summary>Compra transacional: valida tudo, entrega o item e SÓ ENTÃO cobra. Sem crédito: capital negativo ou
    /// menor que o preço recusa. Inventário cheio recusa sem cobrar. Cada oferta+caso é comprada uma vez.</summary>
    public ResultadoDaCompra ComprarNaBiblioteca(OfertaDaBiblioteca oferta, Func<Item, bool> entregar = null)
    {
        if (comprando) return ResultadoDaCompra.Indisponivel; // clique repetido durante a mesma compra
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

    /// <summary>Devolve e limpa as revelações pendentes (quem chama aplica as penalidades).</summary>
    public List<RevelacaoPendente> ConsumirRevelacoes()
    {
        var lista = new List<RevelacaoPendente>(revelacoesPendentes);
        revelacoesPendentes.Clear();
        return lista;
    }

    /// <summary>O que aconteceu ao fechar uma fase (a cutscene de fim de fase conta isso ao jogador).</summary>
    public class EncerramentoDeFase
    {
        public int fase;
        public int despesa;
        public List<RevelacaoPendente> revelacoes = new List<RevelacaoPendente>();
    }

    /// <summary>
    /// Fecha a fase atual, nesta ordem: os boatos publicados nela são expostos (penalidades aplicadas), as despesas
    /// são cobradas e a fase avança. As penalidades vêm ANTES do avanço porque entrar na Fase 4 trava a rota pelas
    /// barras: a mentira descoberta pesa na Definição de Rota (documento, Fase 4).
    /// </summary>
    public EncerramentoDeFase EncerrarFase()
    {
        var resultado = new EncerramentoDeFase { fase = faseAtual };
        foreach (RevelacaoPendente revelacao in ConsumirRevelacoes())
        {
            AplicarImpactoPanfleto(revelacao.povo, revelacao.estado, 0);
            resultado.revelacoes.Add(revelacao);
        }
        resultado.despesa = PagarDespesasDaFase(resultado.fase);
        AvancarFase();
        return resultado;
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

    /// <summary>Chamado pela prensa ao imprimir o panfleto do caso. Falso se já estava concluído.</summary>
    public bool ConcluirCaso(CaseData caso)
    {
        if (caso == null || casosConcluidos.Contains(caso)) return false;
        casosConcluidos.Add(caso);
        Debug.Log($"[FASES] Caso '{caso.name}' concluído ({CasosConcluidosNaFase}/{CasosNecessariosNaFase} da Fase {faseAtual}).");

        if (!string.IsNullOrWhiteSpace(caso.caseTitle))
            AvisoNaTela.Mostrar($"Caso concluído: {caso.caseTitle.Trim()}");
        ArquivarSobrasDoCaso(caso);
        return true;
    }

    // ===== Tempo de investigação e despesas =====

    // O que já foi pago fica no registroDaInvestigacao, separado por caso: não precisa limpar aqui.
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

    /// <summary>Aplica o impacto (Povo/Estado limitados a 0–100). Devolve o que mudou DE FATO (x = Povo, y = Estado,
    /// z = Ouro), que o histórico da publicação guarda junto do valor calculado.</summary>
    public Vector3Int AplicarImpactoPanfleto(int impactoPublico, int impactoEstado, int ouro)
    {
        int povoAntes = opiniaoPublicaAtual, estadoAntes = opiniaoEstadoAtual;
        opiniaoPublicaAtual += impactoPublico;
        opiniaoEstadoAtual += impactoEstado;
        capitalAtual += ouro;

        opiniaoPublicaAtual = Mathf.Clamp(opiniaoPublicaAtual, 0, 100);
        opiniaoEstadoAtual = Mathf.Clamp(opiniaoEstadoAtual, 0, 100);

        Debug.Log($"[PANFLETO] Povo: {opiniaoPublicaAtual} | Estado: {opiniaoEstadoAtual} | Ouro: {capitalAtual}");
        ForcarAtualizacaoUI(); // Atualiza a tela imediatamente após o craft
        return new Vector3Int(opiniaoPublicaAtual - povoAntes, opiniaoEstadoAtual - estadoAntes, ouro);
    }
}