using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Save em um único arquivo JSON (Application.persistentDataPath/save.json — funciona igual no Windows e no Android).
///
/// Guarda o que sobrevive entre cenas: os dados do GameManager, a etapa do tutorial, as cutscenes/dicas já
/// vistas e a cena onde o jogador estava. O estado dos NPCs e objetos investigados (horas pagas, pistas recolhidas,
/// recompensas entregues) vem do RegistroDaInvestigacao; o resto da cena (portas...) se monta sozinho a partir disso,
/// do mesmo jeito que acontece ao subir do porão para o escritório.
///
/// Salvar: SistemaDeSave.Salvar() com a cena já rodando (ex: o TutorialStep com "Salvar Ao Concluir").
/// Carregar: botão Continuar do menu (MenuPrincipalManager.Continuar). O GameManager e o TutorialManager da cena
/// carregada puxam os dados em Awake/Start.
/// </summary>
public static class SistemaDeSave
{
    // 2: fase atual e casos concluídos
    // 3: registro da investigação por caso + ID estável (horas pagas, loot, recompensas de NPC)
    // 4: biblioteca (compras), evidências obtidas e histórico completo de cada publicação
    // 5: histórico guarda também o que foi aplicado de fato (depois do limite 0–100)
    public const int VersaoAtual = 5;
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
        // v4: snapshot da publicação (saves antigos chegam sem isto e viram "legado")
        public bool legado;
        public List<string> pistas = new List<string>();
        public List<string> suportes = new List<string>();
        public List<string> qualificadores = new List<string>();
        public int povo, estado, ouro, penalidadePovo, penalidadeEstado;
        // v5
        public bool aplicadoConhecido;
        public int povoAplicado, estadoAplicado, ouroAplicado;
    }

    [Serializable]
    public class RevelacaoSalva
    {
        public string caso;
        public string texto;
        public int povo;
        public int estado;
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
    }

    public static string CaminhoDoArquivo => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool ExisteSave => File.Exists(CaminhoDoArquivo);

    // Save lido pelo Continuar, esperando o GameManager/TutorialManager da cena nova nascerem.
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

        // O inventário da tela é a fonte mais atual (o inventarioSalvo só é atualizado ao trocar de cena).
        if (gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();

        DadosDeSave dados = CapturarDados(gm, SceneManager.GetActiveScene().name,
            TutorialManager.Instance != null ? TutorialManager.Instance.IndiceAtual : 0);

        foreach (string chave in ProgressoDoJogo.ChavesDaPartida)
            if (PlayerPrefs.GetInt(chave, 0) == 1) dados.chavesMarcadas.Add(chave);

        try
        {
            // Grava num arquivo temporário e só depois troca: se o jogo fechar no meio, o save antigo continua inteiro.
            string temporario = CaminhoDoArquivo + ".tmp";
            File.WriteAllText(temporario, JsonUtility.ToJson(dados, true));
            if (File.Exists(CaminhoDoArquivo)) File.Delete(CaminhoDoArquivo);
            File.Move(temporario, CaminhoDoArquivo);
            Debug.Log($"[SistemaDeSave] Jogo salvo na cena '{dados.cena}' ({CaminhoDoArquivo}).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Falha ao salvar: {e.Message}");
        }
    }

    /// <summary>Monta os dados do save a partir do GameManager (sem tocar em disco, PlayerPrefs nem cena).</summary>
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
            evidenciasObtidas = new List<string>(gm.evidenciasObtidas)
        };

        foreach (CaseData caso in gm.casosJaSelecionados)
            if (caso != null) dados.casosJaSelecionados.Add(caso.name);

        foreach (CaseData caso in gm.casosConcluidos)
            if (caso != null) dados.casosConcluidos.Add(caso.name);

        foreach (Item item in gm.inventarioSalvo)
            if (item != null) dados.inventario.Add(new ItemSalvo { itemID = item.itemID, quantidade = item.itemAmt });

        foreach (var p in gm.panfletosPublicados)
            dados.panfletosPublicados.Add(new PanfletoSalvo
            {
                caso = p.caso != null ? p.caso.name : null, nivel = p.nivel, legado = p.legado,
                pistas = Copia(p.pistas), suportes = Copia(p.suportes), qualificadores = Copia(p.qualificadores),
                povo = p.povo, estado = p.estado, ouro = p.ouro, penalidadePovo = p.penalidadePovo, penalidadeEstado = p.penalidadeEstado,
                aplicadoConhecido = p.aplicadoConhecido, povoAplicado = p.povoAplicado, estadoAplicado = p.estadoAplicado, ouroAplicado = p.ouroAplicado
            });

        foreach (var r in gm.revelacoesPendentes)
            dados.revelacoesPendentes.Add(new RevelacaoSalva
            {
                caso = r.caso != null ? r.caso.name : null, texto = r.texto, povo = r.povo, estado = r.estado
            });

        return dados;
    }

    // ===== Carregar =====

    /// <summary>Descarta a partida atual, restaura o save e carrega a cena salva. Falso se não houver save válido.</summary>
    public static bool Carregar()
    {
        DadosDeSave dados = Ler();
        if (dados == null) return false;

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

    private static DadosDeSave Ler()
    {
        if (!ExisteSave) return null;
        try
        {
            DadosDeSave dados = DesserializarDados(File.ReadAllText(CaminhoDoArquivo));
            if (dados == null) throw new Exception("arquivo vazio ou inválido");
            return dados;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SistemaDeSave] Não foi possível ler o save ({CaminhoDoArquivo}): {e.Message}");
            return null;
        }
    }

    /// <summary>Chamado pelo GameManager.Awake: se há um save sendo carregado, preenche os dados dele.</summary>
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

    /// <summary>Lê um JSON de save (qualquer versão). Nulo se estiver vazio ou inválido.</summary>
    public static DadosDeSave DesserializarDados(string json)
    {
        DadosDeSave dados = JsonUtility.FromJson<DadosDeSave>(json);
        return dados != null && dados.versao > 0 ? dados : null;
    }

    /// <summary>Preenche o GameManager com os dados do save, migrando versões antigas sem apagar progresso.</summary>
    public static void AplicarDados(DadosDeSave dados, GameManager gm, CatalogoDeSave catalogo)
    {
        if (dados.versao > VersaoAtual)
            Debug.LogWarning($"[SistemaDeSave] Save versão {dados.versao} é mais novo que o jogo (versão {VersaoAtual}); campos desconhecidos serão ignorados.");

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
                // Publicações de saves < 4 não guardavam pistas nem valores: ficam marcadas, sem nada inventado.
                legado = p.legado || dados.versao < 4,
                pistas = Copia(p.pistas), suportes = Copia(p.suportes), qualificadores = Copia(p.qualificadores),
                povo = p.povo, estado = p.estado, ouro = p.ouro, penalidadePovo = p.penalidadePovo, penalidadeEstado = p.penalidadeEstado,
                // v < 5 não guardava o aplicado: fica como desconhecido (nada é recalculado nem inventado).
                aplicadoConhecido = dados.versao >= 5 && p.aplicadoConhecido,
                povoAplicado = p.povoAplicado, estadoAplicado = p.estadoAplicado, ouroAplicado = p.ouroAplicado
            });
        gm.comprasDaBiblioteca = Copia(dados.comprasDaBiblioteca);
        gm.evidenciasObtidas = Copia(dados.evidenciasObtidas);
        if (dados.versao < 4) // save antigo: o que está no inventário foi obtido, com certeza (nada além disso é deduzido)
            foreach (Item item in gm.inventarioSalvo) gm.RegistrarEvidencia(item);

        gm.revelacoesPendentes = new List<GameManager.RevelacaoPendente>();
        foreach (RevelacaoSalva r in dados.revelacoesPendentes)
            gm.revelacoesPendentes.Add(new GameManager.RevelacaoPendente
            {
                caso = BuscarCaso(catalogo, r.caso), texto = r.texto, povo = r.povo, estado = r.estado
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

    /// <summary>
    /// Save v1/v2: as horas pagas eram "cena/nomeDoGameObject" do caso em andamento, sem registro de loot nem de
    /// recompensas. Migração determinística, sem reset: as chaves antigas continuam valendo para aquele caso
    /// (RelogioDeInvestigacao as converte para o ID novo na primeira interação, sem cobrar), e o caso é marcado como
    /// "estado legado" — lá, uma pista que o jogador ainda tem conta como já entregue, como na regra antiga.
    /// Homônimos na mesma cena dividem a chave antiga: todos contam como pagos (o jogo antigo cobrava assim).
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

        // Save v1 não tinha relógio: um caso em andamento chegaria sem orçamento (tempo ilimitado). Recebe o
        // orçamento do caso, uma vez, sem a perda por dívida (que só vale ao aceitar um caso novo).
        if (dados.versao < 2 && gm.horasDoCaso <= 0)
        {
            gm.horasDoCaso = gm.horasRestantes = caso.horasDeInvestigacao > 0 ? caso.horasDeInvestigacao : gm.horasPorCaso;
            Debug.Log($"[SistemaDeSave] Save v1: caso '{caso.name}' em andamento recebeu o orçamento de {gm.horasDoCaso}h.");
        }

        Debug.Log($"[SistemaDeSave] Migração v{dados.versao}→v{VersaoAtual}: caso '{caso.name}' em andamento; chaves antigas " +
                  $"(cena/nome) mantidas como pagas: [{string.Join(", ", antigas)}]. Homônimos que dividirem uma chave serão " +
                  $"listados na primeira interação. Horas restantes preservadas: {gm.horasRestantes}.");
    }

    /// <summary>Chamado pelo TutorialManager.Start: a etapa salva substitui o PlayerPrefs "tutorial concluído".</summary>
    public static bool ConsumirEtapaDoTutorial(out int indice)
    {
        indice = dadosPendentesTutorial != null ? dadosPendentesTutorial.etapaTutorial : 0;
        bool havia = dadosPendentesTutorial != null;
        dadosPendentesTutorial = null;
        return havia;
    }

    /// <summary>Save da versão 1 (sem fases): deduz a fase pelos casos escolhidos e conclui os que já têm panfleto.</summary>
    private static void MigrarProgressaoDeFases(GameManager gm, CatalogoDeSave catalogo)
    {
        foreach (var p in gm.panfletosPublicados)
            if (p.caso != null && !gm.casosConcluidos.Contains(p.caso)) gm.casosConcluidos.Add(p.caso);

        bool saiuDoTutorial = gm.casosJaSelecionados.Count > 0;
        gm.faseAtual = saiuDoTutorial ? 2 : 1;
        // O panfleto do tutorial não fica em panfletosPublicados: quem já escolheu um caso concluiu o tutorial.
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
