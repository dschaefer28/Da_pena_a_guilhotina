using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Save JSON por slot em persistentDataPath. Guarda GameManager, etapa do tutorial, chaves de progresso e cena;
/// o estado das cenas se reconstrói a partir do RegistroDaInvestigacao. O Continuar entrega os dados ao
/// GameManager/TutorialManager da cena carregada em Awake/Start. A partida grava sempre no slot ativo.
/// </summary>
public static class SistemaDeSave
{
    // Versões:
    // 2: fase atual e casos concluídos
    // 3: registro da investigação por caso + ID estável
    // 4: biblioteca, evidências obtidas e histórico completo de publicação
    // 5: valores efetivamente aplicados (após o limite 0–100)
    // 6: linha editorial da publicação e da revelação
    // 7: dedução por hipóteses (descontinuada na v9)
    // 8: fim de jogo após o tribunal
    // 9: dedução por escolha (pista mantida/descartada por par)
    // 10: ID da partida
    public const int VersaoAtual = 10;
    private const string CenaPadrao = "Jogo";

    [Serializable]
    public class ItemSalvo
    {
        public string itemID;
        public int quantidade;
    }

    [Serializable]
    public class PanfletoSalvo
    {
        public string caso;
        public NivelDoPanfleto nivel;
        // v4 (saves antigos viram "legado")
        public bool legado;
        public List<string> pistas = new List<string>();
        public List<string> suportes = new List<string>();
        public List<string> qualificadores = new List<string>();
        public int povo, estado, ouro, penalidadePovo, penalidadeEstado;
        // v5
        public bool aplicadoConhecido;
        public int povoAplicado, estadoAplicado, ouroAplicado;
        // v6
        public LinhaEditorial linha;
        public int editorialPovo, editorialEstado, editorialOuro, agravamentoPovo, agravamentoEstado;
    }

    [Serializable]
    public class RevelacaoSalva
    {
        public string caso;
        public string texto;
        public int povo;
        public int estado;
        public LinhaEditorial linha; // v6
        public bool aplicada;        // penalidade já cobrada na impressão (saves antigos)
    }

    [Serializable]
    public class DadosDeSave
    {
        public int versao;
        public string cena;
        public string dataHora;

        public int etapaTutorial;
        public List<string> chavesMarcadas = new List<string>();

        public int capital;
        public int opiniaoPublica;
        public int opiniaoEstado;
        public string casoEscolhido;
        public List<string> casosJaSelecionados = new List<string>();
        public int faseAtual;
        public RotaFinal rotaFinal;
        public int horasDoCaso;
        public int horasRestantes;
        [Tooltip("Só saves v1/v2: \"cena/nomeDoGameObject\" pagos no caso atual. Lido para migração; v3 grava vazio.")]
        public List<string> interacoesPagas = new List<string>();
        public List<string> casosConcluidos = new List<string>();
        public List<ItemSalvo> inventario = new List<ItemSalvo>();
        public List<PanfletoSalvo> panfletosPublicados = new List<PanfletoSalvo>();
        public List<RevelacaoSalva> revelacoesPendentes = new List<RevelacaoSalva>();
        public List<string> pistasVerificadas = new List<string>();

        // v3: RegistroDaInvestigacao
        public List<string> interacoesPagasPorCaso = new List<string>();
        public List<string> lootsColetados = new List<string>();
        public List<string> recompensasEntregues = new List<string>();
        public List<string> casosComEstadoLegado = new List<string>();
        public List<string> interacoesPagasLegadas = new List<string>();

        // v4
        public List<string> comprasDaBiblioteca = new List<string>();
        public List<string> evidenciasObtidas = new List<string>();

        // Campos de dedução da v7/v8 (marcacoesDeDeducao, deducoesConfirmadas) foram removidos; o JsonUtility os ignora.

        // v8
        public bool jogoConcluido;
        public RotaFinal desfechoRota;
        public bool desfechoReuAbsolvido;
        public string desfechoTitulo;

        // v9
        public List<EscolhaDeDeducao> escolhasDeDeducao = new List<EscolhaDeDeducao>();

        // v10 (ausente = ID novo ao continuar)
        public string idDaPartida;
    }

    // ===== Slots =====

    public const int NumeroDeSlots = 3;
    private const string ChaveDoSlotAtivo = "save_slot_ativo"; // fora de ProgressoDoJogo: o Novo Jogo não o apaga
    private const string ArquivoAntigo = "save.json";

    public static int SlotAtivo
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ChaveDoSlotAtivo, 1), 1, NumeroDeSlots);
        set
        {
            PlayerPrefs.SetInt(ChaveDoSlotAtivo, Mathf.Clamp(value, 1, NumeroDeSlots));
            PlayerPrefs.Save();
        }
    }

    public static string CaminhoDoSlot(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{Mathf.Clamp(slot, 1, NumeroDeSlots)}.json");

    public static string CaminhoDoArquivo => CaminhoDoSlot(SlotAtivo);

    public static bool ExisteSlot(int slot)
    {
        MigrarSaveAntigo();
        return File.Exists(CaminhoDoSlot(slot));
    }

    public static bool ExisteSave
    {
        get
        {
            for (int slot = 1; slot <= NumeroDeSlots; slot++)
                if (ExisteSlot(slot)) return true;
            return false;
        }
    }

    public static bool ExistePartidaEmAndamento => SlotMaisRecenteEmAndamento() > 0;

    // 0 se todos estão ocupados.
    public static int PrimeiroSlotVazio()
    {
        for (int slot = 1; slot <= NumeroDeSlots; slot++)
            if (!ExisteSlot(slot)) return slot;
        return 0;
    }

    // Slot que o Continuar abre, ou 0.
    public static int SlotMaisRecenteEmAndamento()
    {
        int melhor = 0;
        DateTime maisRecente = DateTime.MinValue;
        for (int slot = 1; slot <= NumeroDeSlots; slot++)
        {
            if (!ExisteSlot(slot) || !PodeContinuar(LerSlot(slot))) continue;
            DateTime quando = File.GetLastWriteTimeUtc(CaminhoDoSlot(slot));
            if (melhor == 0 || quando > maisRecente) { melhor = slot; maisRecente = quando; }
        }
        return melhor;
    }

    public static DadosDeSave LerSlot(int slot)
    {
        if (!ExisteSlot(slot)) return null;
        string caminho = CaminhoDoSlot(slot);
        try
        {
            DadosDeSave dados = DesserializarDados(File.ReadAllText(caminho));
            if (dados == null) throw new Exception("arquivo vazio ou inválido");
            return dados;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Não foi possível ler o save ({caminho}): {e.Message}");
            return null;
        }
    }

    // Não afeta o ArquivoDeCasos.
    public static bool ExcluirSlot(int slot)
    {
        if (!ExisteSlot(slot)) return false;
        try
        {
            File.Delete(CaminhoDoSlot(slot));
            Debug.Log($"[SistemaDeSave] Save do slot {slot} excluído.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Não foi possível excluir o slot {slot}: {e.Message}");
            return false;
        }
    }

    // O save.json anterior aos slots vira o primeiro slot vazio.
    private static bool migracaoFeita;
    private static void MigrarSaveAntigo()
    {
        if (migracaoFeita) return;
        migracaoFeita = true;
        string antigo = Path.Combine(Application.persistentDataPath, ArquivoAntigo);
        if (!File.Exists(antigo)) return;
        try
        {
            for (int slot = 1; slot <= NumeroDeSlots; slot++)
            {
                string destino = CaminhoDoSlot(slot);
                if (File.Exists(destino)) continue;
                File.Move(antigo, destino);
                SlotAtivo = slot;
                Debug.Log($"[SistemaDeSave] save.json antigo movido para o slot {slot}.");
                return;
            }
            Debug.LogWarning("[SistemaDeSave] save.json antigo mantido: todos os slots já estão ocupados.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Não foi possível mover o save.json antigo para um slot: {e.Message}");
        }
    }

    public static bool PodeContinuar(DadosDeSave dados) => dados != null && !dados.jogoConcluido;

    // Aguardam o GameManager/TutorialManager da cena nova.
    private static DadosDeSave dadosPendentesGameManager;
    private static DadosDeSave dadosPendentesTutorial;

    // ===== Salvar =====

    public static void Salvar()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SistemaDeSave] Sem GameManager na cena: nada foi salvo.");
            return;
        }

        // inventarioSalvo só é atualizado ao trocar de cena.
        if (gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();

        Gravar(DadosDaPartidaAtual(gm));
    }

    // Grava o desfecho para o Continuar não reabrir um jogo já julgado.
    public static void RegistrarFimDeJogo(RotaFinal rota, bool reuAbsolvido, string tituloDoDesfecho)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[SistemaDeSave] Sem GameManager na cena: o fim de jogo não foi registrado.");
            return;
        }
        if (gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();
        DadosDeSave dados = DadosDaPartidaAtual(gm);
        MarcarFimDeJogo(dados, rota, reuAbsolvido, tituloDoDesfecho);
        Gravar(dados);
    }

    public static void MarcarFimDeJogo(DadosDeSave dados, RotaFinal rota, bool reuAbsolvido, string tituloDoDesfecho)
    {
        dados.jogoConcluido = true;
        dados.desfechoRota = rota;
        dados.desfechoReuAbsolvido = reuAbsolvido;
        dados.desfechoTitulo = tituloDoDesfecho;
    }

    private static DadosDeSave DadosDaPartidaAtual(GameManager gm)
    {
        DadosDeSave dados = CapturarDados(gm, SceneManager.GetActiveScene().name,
            TutorialManager.Instance != null ? TutorialManager.Instance.IndiceAtual : 0);
        foreach (string chave in ProgressoDoJogo.ChavesDaPartida)
            if (PlayerPrefs.GetInt(chave, 0) == 1) dados.chavesMarcadas.Add(chave);
        return dados;
    }

    private static void Gravar(DadosDeSave dados)
    {
        if (ModoDev.PartidaDeDemonstracao)
        {
            Debug.Log("[SistemaDeSave] Partida de demonstração (modo dev): nada foi gravado.");
            return;
        }
        MigrarSaveAntigo(); // antes de gravar, para o save.json antigo não cair depois neste slot
        try
        {
            // Escrita via .tmp: se o jogo fechar no meio, o save anterior fica íntegro.
            string temporario = CaminhoDoArquivo + ".tmp";
            File.WriteAllText(temporario, JsonUtility.ToJson(dados, true));
            if (File.Exists(CaminhoDoArquivo)) File.Delete(CaminhoDoArquivo);
            File.Move(temporario, CaminhoDoArquivo);
            ArquivoDeCasos.Registrar(dados);
            Debug.Log(dados.jogoConcluido
                ? $"[SistemaDeSave] Fim de jogo registrado (rota {dados.desfechoRota}, réu {(dados.desfechoReuAbsolvido ? "absolvido" : "condenado")}) em {CaminhoDoArquivo}."
                : $"[SistemaDeSave] Jogo salvo no slot {SlotAtivo}, cena '{dados.cena}' ({CaminhoDoArquivo}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Falha ao salvar: {e.Message}");
        }
    }

    // Puro: não toca em disco, PlayerPrefs nem cena.
    public static DadosDeSave CapturarDados(GameManager gm, string cena, int etapaTutorial)
    {
        RegistroDaInvestigacao registro = gm.registroDaInvestigacao ?? new RegistroDaInvestigacao();
        var dados = new DadosDeSave
        {
            versao = VersaoAtual,
            cena = cena,
            dataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            etapaTutorial = etapaTutorial,
            capital = gm.capitalAtual,
            opiniaoPublica = gm.opiniaoPublicaAtual,
            opiniaoEstado = gm.opiniaoEstadoAtual,
            casoEscolhido = gm.casoEscolhido != null ? gm.casoEscolhido.name : null,
            faseAtual = gm.faseAtual,
            rotaFinal = gm.rotaFinal,
            horasDoCaso = gm.horasDoCaso,
            horasRestantes = gm.horasRestantes,
            pistasVerificadas = new List<string>(gm.pistasVerificadas),
            interacoesPagasPorCaso = new List<string>(registro.interacoesPagas),
            lootsColetados = new List<string>(registro.lootsColetados),
            recompensasEntregues = new List<string>(registro.recompensasEntregues),
            casosComEstadoLegado = new List<string>(registro.casosComEstadoLegado),
            interacoesPagasLegadas = new List<string>(registro.interacoesPagasLegadas),
            comprasDaBiblioteca = new List<string>(gm.comprasDaBiblioteca),
            evidenciasObtidas = new List<string>(gm.evidenciasObtidas),
            escolhasDeDeducao = CopiaDasEscolhas(gm.escolhasDeDeducao),
            idDaPartida = gm.IdDaPartida
        };

        foreach (CaseData caso in gm.casosJaSelecionados)
            if (caso != null) dados.casosJaSelecionados.Add(caso.name);

        foreach (CaseData caso in gm.casosConcluidos)
            if (caso != null) dados.casosConcluidos.Add(caso.name);

        foreach (Item item in gm.inventarioSalvo)
            if (item != null) dados.inventario.Add(new ItemSalvo { itemID = item.itemID, quantidade = item.itemAmt });

        foreach (var p in gm.panfletosPublicados)
            if (p != null) dados.panfletosPublicados.Add(ParaSalvar(p));

        foreach (var r in gm.revelacoesPendentes)
            dados.revelacoesPendentes.Add(new RevelacaoSalva
            {
                caso = r.caso != null ? r.caso.name : null, texto = r.texto, povo = r.povo, estado = r.estado, linha = r.linha,
                aplicada = r.aplicada
            });

        return dados;
    }

    // ===== Carregar =====

    public static bool Carregar()
    {
        int slot = SlotMaisRecenteEmAndamento();
        return slot > 0 && Carregar(slot);
    }

    public static bool Carregar(int slot)
    {
        DadosDeSave dados = LerSlot(slot);
        if (!PodeContinuar(dados)) return false;
        SlotAtivo = slot;

        ProgressoDoJogo.ComecarNovoJogo();
        foreach (string chave in dados.chavesMarcadas) PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();

        dadosPendentesGameManager = dados;
        dadosPendentesTutorial = dados;

        string cena = string.IsNullOrEmpty(dados.cena) ? CenaPadrao : dados.cena;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(cena);
        else
            SceneManager.LoadScene(cena);
        return true;
    }

    // Chamado pelo GameManager.Awake.
    public static void AplicarPendente(GameManager gm)
    {
        DadosDeSave dados = dadosPendentesGameManager;
        if (dados == null || gm == null) return;
        dadosPendentesGameManager = null;

        CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
        if (catalogo == null)
        {
            Debug.LogError("[SistemaDeSave] Sem CatalogoDeSave: o save não pôde ser aplicado e a partida começa com os valores iniciais.");
            return;
        }

        AplicarDados(dados, gm, catalogo);
        Debug.Log($"[SistemaDeSave] Save de {dados.dataHora} carregado na cena '{dados.cena}'.");
    }

    public static DadosDeSave DesserializarDados(string json)
    {
        DadosDeSave dados = JsonUtility.FromJson<DadosDeSave>(json);
        return dados != null && dados.versao > 0 ? dados : null;
    }

    // Migra versões antigas sem apagar progresso nem inventar valores.
    public static void AplicarDados(DadosDeSave dados, GameManager gm, CatalogoDeSave catalogo)
    {
        if (dados.versao > VersaoAtual)
            Debug.LogWarning($"[SistemaDeSave] Save versão {dados.versao} é mais novo que o jogo (versão {VersaoAtual}); campos desconhecidos serão ignorados.");

        if (!string.IsNullOrEmpty(dados.idDaPartida)) gm.idDaPartida = dados.idDaPartida;
        gm.capitalAtual = dados.capital;
        gm.opiniaoPublicaAtual = dados.opiniaoPublica;
        gm.opiniaoEstadoAtual = dados.opiniaoEstado;
        gm.casoEscolhido = string.IsNullOrEmpty(dados.casoEscolhido) ? null : BuscarCaso(catalogo, dados.casoEscolhido);
        gm.pistasVerificadas = new List<string>(dados.pistasVerificadas);

        gm.casosJaSelecionados = new List<CaseData>();
        foreach (string nome in dados.casosJaSelecionados)
        {
            CaseData caso = BuscarCaso(catalogo, nome);
            if (caso != null) gm.casosJaSelecionados.Add(caso);
        }

        gm.casosConcluidos = new List<CaseData>();
        foreach (string nome in dados.casosConcluidos)
        {
            CaseData caso = BuscarCaso(catalogo, nome);
            if (caso != null) gm.casosConcluidos.Add(caso);
        }

        gm.inventarioSalvo = new List<Item>();
        foreach (ItemSalvo salvo in dados.inventario)
        {
            Item original = catalogo.BuscarItem(salvo.itemID);
            if (original == null)
            {
                Debug.LogWarning($"[SistemaDeSave] Item '{salvo.itemID}' do save não está no CatalogoDeSave: ignorado.");
                continue;
            }
            Item item = original.Clone();
            item.itemAmt = salvo.quantidade;
            gm.inventarioSalvo.Add(item);
        }

        gm.panfletosPublicados = new List<GameManager.PanfletoPublicado>();
        foreach (PanfletoSalvo p in dados.panfletosPublicados)
            gm.panfletosPublicados.Add(new GameManager.PanfletoPublicado
            {
                caso = BuscarCaso(catalogo, p.caso), nivel = p.nivel,
                legado = p.legado || dados.versao < 4,
                pistas = Copia(p.pistas), suportes = Copia(p.suportes), qualificadores = Copia(p.qualificadores),
                povo = p.povo, estado = p.estado, ouro = p.ouro, penalidadePovo = p.penalidadePovo, penalidadeEstado = p.penalidadeEstado,
                aplicadoConhecido = dados.versao >= 5 && p.aplicadoConhecido,
                povoAplicado = p.povoAplicado, estadoAplicado = p.estadoAplicado, ouroAplicado = p.ouroAplicado,
                linha = dados.versao >= 6 ? p.linha : LinhaEditorial.Nenhuma,
                editorialPovo = dados.versao >= 6 ? p.editorialPovo : 0, editorialEstado = dados.versao >= 6 ? p.editorialEstado : 0,
                editorialOuro = dados.versao >= 6 ? p.editorialOuro : 0,
                agravamentoPovo = dados.versao >= 6 ? p.agravamentoPovo : 0, agravamentoEstado = dados.versao >= 6 ? p.agravamentoEstado : 0
            });
        gm.comprasDaBiblioteca = Copia(dados.comprasDaBiblioteca);
        gm.evidenciasObtidas = Copia(dados.evidenciasObtidas);
        if (dados.versao < 4) // só o que está no inventário conta como obtido
            foreach (Item item in gm.inventarioSalvo) gm.RegistrarEvidencia(item);

        // v < 9: nenhum par decidido. Pista descartada nunca volta ao inventário, mesmo que o save a traga.
        gm.escolhasDeDeducao = dados.versao >= 9 ? CopiaDasEscolhas(dados.escolhasDeDeducao) : new List<EscolhaDeDeducao>();
        gm.inventarioSalvo.RemoveAll(item => Deducao.Descartada(gm, item));

        gm.revelacoesPendentes = new List<GameManager.RevelacaoPendente>();
        foreach (RevelacaoSalva r in dados.revelacoesPendentes)
            gm.revelacoesPendentes.Add(new GameManager.RevelacaoPendente
            {
                caso = BuscarCaso(catalogo, r.caso), texto = r.texto, povo = r.povo, estado = r.estado,
                linha = dados.versao >= 6 ? r.linha : LinhaEditorial.Nenhuma,
                aplicada = r.aplicada
            });

        if (dados.versao < 2) MigrarProgressaoDeFases(gm, catalogo);
        else gm.faseAtual = Mathf.Clamp(dados.faseAtual, 1, GameManager.UltimaFase);
        gm.rotaFinal = dados.rotaFinal;
        gm.horasDoCaso = dados.horasDoCaso;
        gm.horasRestantes = dados.horasRestantes;

        gm.registroDaInvestigacao = new RegistroDaInvestigacao
        {
            interacoesPagas = Copia(dados.interacoesPagasPorCaso),
            lootsColetados = Copia(dados.lootsColetados),
            recompensasEntregues = Copia(dados.recompensasEntregues),
            casosComEstadoLegado = Copia(dados.casosComEstadoLegado),
            interacoesPagasLegadas = Copia(dados.interacoesPagasLegadas)
        };
        if (dados.versao < 3) MigrarRegistroDaInvestigacao(dados, gm);

        if (gm.faseAtual == GameManager.UltimaFase && gm.rotaFinal == RotaFinal.Nenhuma) gm.DefinirRota();
    }

    private static List<string> Copia(List<string> lista) => lista != null ? new List<string>(lista) : new List<string>();

    // Também usado pela ficha do panfleto.
    public static PanfletoSalvo ParaSalvar(GameManager.PanfletoPublicado p) => p == null ? null : new PanfletoSalvo
    {
        caso = p.caso != null ? p.caso.name : null, nivel = p.nivel, legado = p.legado,
        pistas = Copia(p.pistas), suportes = Copia(p.suportes), qualificadores = Copia(p.qualificadores),
        povo = p.povo, estado = p.estado, ouro = p.ouro, penalidadePovo = p.penalidadePovo, penalidadeEstado = p.penalidadeEstado,
        aplicadoConhecido = p.aplicadoConhecido, povoAplicado = p.povoAplicado, estadoAplicado = p.estadoAplicado, ouroAplicado = p.ouroAplicado,
        linha = p.linha, editorialPovo = p.editorialPovo, editorialEstado = p.editorialEstado, editorialOuro = p.editorialOuro,
        agravamentoPovo = p.agravamentoPovo, agravamentoEstado = p.agravamentoEstado
    };

    private static List<EscolhaDeDeducao> CopiaDasEscolhas(List<EscolhaDeDeducao> lista)
    {
        var copia = new List<EscolhaDeDeducao>();
        if (lista == null) return copia;
        foreach (EscolhaDeDeducao e in lista)
            if (e != null && !string.IsNullOrEmpty(e.caso) && !string.IsNullOrEmpty(e.escolhida) && !string.IsNullOrEmpty(e.descartada))
                copia.Add(new EscolhaDeDeducao { caso = e.caso, escolhida = e.escolhida, descartada = e.descartada });
        return copia;
    }

    /// <summary>
    /// v1/v2 pagavam horas por "cena/nomeDoGameObject". As chaves antigas seguem valendo para o caso em andamento
    /// (RelogioDeInvestigacao converte para o ID novo sem cobrar) e o caso fica em "estado legado". Homônimos na
    /// mesma cena dividem a chave: todos contam como pagos.
    /// </summary>
    private static void MigrarRegistroDaInvestigacao(DadosDeSave dados, GameManager gm)
    {
        List<string> antigas = dados.interacoesPagas ?? new List<string>();
        CaseData caso = gm.casoEscolhido;
        if (caso == null || !gm.CasoAtualEmAndamento)
        {
            if (antigas.Count > 0)
                Debug.Log($"[SistemaDeSave] Migração v{dados.versao}→v{VersaoAtual}: {antigas.Count} interação(ões) paga(s) " +
                          "de um caso já encerrado foram descartadas (o relógio só vale para o caso em andamento).");
            return;
        }

        RegistroDaInvestigacao registro = gm.registroDaInvestigacao;
        if (!registro.casosComEstadoLegado.Contains(caso.name)) registro.casosComEstadoLegado.Add(caso.name);
        foreach (string antiga in antigas)
        {
            if (string.IsNullOrEmpty(antiga)) continue;
            string chave = RegistroDaInvestigacao.Chave(caso, antiga);
            if (!registro.interacoesPagasLegadas.Contains(chave)) registro.interacoesPagasLegadas.Add(chave);
        }

        // v1 não tinha relógio: concede o orçamento uma vez, sem a perda por dívida (só vale ao aceitar caso novo).
        if (dados.versao < 2 && gm.horasDoCaso <= 0)
        {
            gm.horasDoCaso = gm.horasRestantes = caso.horasDeInvestigacao > 0 ? caso.horasDeInvestigacao : gm.horasPorCaso;
            Debug.Log($"[SistemaDeSave] Save v1: caso '{caso.name}' em andamento recebeu o orçamento de {gm.horasDoCaso}h.");
        }

        Debug.Log($"[SistemaDeSave] Migração v{dados.versao}→v{VersaoAtual}: caso '{caso.name}' em andamento; chaves antigas " +
                  $"(cena/nome) mantidas como pagas: [{string.Join(", ", antigas)}]. Homônimos que dividirem uma chave serão " +
                  $"listados na primeira interação. Horas restantes preservadas: {gm.horasRestantes}.");
    }

    // Chamado pelo TutorialManager.Start: a etapa salva prevalece sobre o PlayerPrefs "tutorial concluído".
    public static bool ConsumirEtapaDoTutorial(out int indice)
    {
        indice = dadosPendentesTutorial != null ? dadosPendentesTutorial.etapaTutorial : 0;
        bool havia = dadosPendentesTutorial != null;
        dadosPendentesTutorial = null;
        return havia;
    }

    // v1 não tinha fases: deduz pela escolha de casos e conclui os que já têm panfleto.
    private static void MigrarProgressaoDeFases(GameManager gm, CatalogoDeSave catalogo)
    {
        foreach (var p in gm.panfletosPublicados)
            if (p.caso != null && !gm.casosConcluidos.Contains(p.caso)) gm.casosConcluidos.Add(p.caso);

        bool saiuDoTutorial = gm.casosJaSelecionados.Count > 0;
        gm.faseAtual = saiuDoTutorial ? 2 : 1;
        // O tutorial não fica em panfletosPublicados: escolher um caso implica tê-lo concluído.
        if (saiuDoTutorial)
            foreach (CaseData caso in catalogo.casos)
                if (caso != null && caso.fase == 1 && !gm.casosConcluidos.Contains(caso)) gm.casosConcluidos.Add(caso);
    }

    private static CaseData BuscarCaso(CatalogoDeSave catalogo, string nome)
    {
        if (string.IsNullOrEmpty(nome)) return null;
        CaseData caso = catalogo.BuscarCaso(nome);
        if (caso == null) Debug.LogWarning($"[SistemaDeSave] Caso '{nome}' do save não está no CatalogoDeSave.");
        return caso;
    }
}
