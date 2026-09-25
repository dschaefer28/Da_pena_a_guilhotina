using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Save em um único arquivo JSON (Application.persistentDataPath/save.json — funciona igual no Windows e no Android).
///
/// Guarda o que sobrevive entre cenas: os dados do GameManager, a etapa do tutorial, as cutscenes/dicas já
/// vistas e a cena onde o jogador estava. O resto da cena (NPCs, portas...) já se monta sozinho a partir disso,
/// do mesmo jeito que acontece ao subir do porão para o escritório.
///
/// Salvar: SistemaDeSave.Salvar() com a cena já rodando (ex: o TutorialStep com "Salvar Ao Concluir").
/// Carregar: botão Continuar do menu (MenuPrincipalManager.Continuar). O GameManager e o TutorialManager da cena
/// carregada puxam os dados em Awake/Start.
/// </summary>
public static class SistemaDeSave
{
    private const int VersaoAtual = 2; // 2: fase atual e casos concluídos
    private const string CenaPadrao = "Jogo";

    [Serializable]
    private class ItemSalvo
    {
        public string itemID;
        public int quantidade;
    }

    [Serializable]
    private class PanfletoSalvo
    {
        public string caso;
        public NivelDoPanfleto nivel;
    }

    [Serializable]
    private class RevelacaoSalva
    {
        public string caso;
        public string texto;
        public int povo;
        public int estado;
    }

    [Serializable]
    private class DadosDeSave
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
        public List<string> casosConcluidos = new List<string>();
        public List<ItemSalvo> inventario = new List<ItemSalvo>();
        public List<PanfletoSalvo> panfletosPublicados = new List<PanfletoSalvo>();
        public List<RevelacaoSalva> revelacoesPendentes = new List<RevelacaoSalva>();
        public List<string> pistasVerificadas = new List<string>();
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

        var dados = new DadosDeSave
        {
            versao = VersaoAtual,
            cena = SceneManager.GetActiveScene().name,
            dataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            etapaTutorial = TutorialManager.Instance != null ? TutorialManager.Instance.IndiceAtual : 0,
            capital = gm.capitalAtual,
            opiniaoPublica = gm.opiniaoPublicaAtual,
            opiniaoEstado = gm.opiniaoEstadoAtual,
            casoEscolhido = gm.casoEscolhido != null ? gm.casoEscolhido.name : null,
            faseAtual = gm.faseAtual,
            rotaFinal = gm.rotaFinal,
            pistasVerificadas = new List<string>(gm.pistasVerificadas)
        };

        foreach (string chave in ProgressoDoJogo.ChavesDaPartida)
            if (PlayerPrefs.GetInt(chave, 0) == 1) dados.chavesMarcadas.Add(chave);

        foreach (CaseData caso in gm.casosJaSelecionados)
            if (caso != null) dados.casosJaSelecionados.Add(caso.name);

        foreach (CaseData caso in gm.casosConcluidos)
            if (caso != null) dados.casosConcluidos.Add(caso.name);

        foreach (Item item in gm.inventarioSalvo)
            if (item != null) dados.inventario.Add(new ItemSalvo { itemID = item.itemID, quantidade = item.itemAmt });

        foreach (var p in gm.panfletosPublicados)
            dados.panfletosPublicados.Add(new PanfletoSalvo { caso = p.caso != null ? p.caso.name : null, nivel = p.nivel });

        foreach (var r in gm.revelacoesPendentes)
            dados.revelacoesPendentes.Add(new RevelacaoSalva
            {
                caso = r.caso != null ? r.caso.name : null, texto = r.texto, povo = r.povo, estado = r.estado
            });

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
            DadosDeSave dados = JsonUtility.FromJson<DadosDeSave>(File.ReadAllText(CaminhoDoArquivo));
            if (dados == null || dados.versao <= 0) throw new Exception("arquivo vazio ou inválido");
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
        if (catalogo == null) return;

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
            gm.panfletosPublicados.Add(new GameManager.PanfletoPublicado { caso = BuscarCaso(catalogo, p.caso), nivel = p.nivel });

        gm.revelacoesPendentes = new List<GameManager.RevelacaoPendente>();
        foreach (RevelacaoSalva r in dados.revelacoesPendentes)
            gm.revelacoesPendentes.Add(new GameManager.RevelacaoPendente
            {
                caso = BuscarCaso(catalogo, r.caso), texto = r.texto, povo = r.povo, estado = r.estado
            });

        if (dados.versao < 2) MigrarProgressaoDeFases(gm);
        else gm.faseAtual = Mathf.Clamp(dados.faseAtual, 1, GameManager.UltimaFase);
        gm.rotaFinal = dados.rotaFinal;
        if (gm.faseAtual == GameManager.UltimaFase && gm.rotaFinal == RotaFinal.Nenhuma) gm.DefinirRota();

        Debug.Log($"[SistemaDeSave] Save de {dados.dataHora} carregado na cena '{dados.cena}'.");
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
    private static void MigrarProgressaoDeFases(GameManager gm)
    {
        foreach (var p in gm.panfletosPublicados)
            if (p.caso != null && !gm.casosConcluidos.Contains(p.caso)) gm.casosConcluidos.Add(p.caso);

        bool saiuDoTutorial = gm.casosJaSelecionados.Count > 0;
        gm.faseAtual = saiuDoTutorial ? 2 : 1;
        // O panfleto do tutorial não fica em panfletosPublicados: quem já escolheu um caso concluiu o tutorial.
        if (saiuDoTutorial)
            foreach (CaseData caso in CatalogoDeSave.Instancia.casos)
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
