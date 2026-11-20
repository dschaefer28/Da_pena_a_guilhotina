using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modo dev (painel ModoDevUI, F1): monta uma partida em qualquer ponto da campanha usando as regras do jogo e
/// oferece atalhos. Qualquer uso marca a partida como demonstração: não grava save nem arquivo de casos até o
/// próximo Novo Jogo/Carregar. Fases passadas recebem o caso só com fatos que mais puxa para a rota pedida.
/// </summary>
public static class ModoDev
{
    public enum Destino { Fase1, Fase2, Fase3, Fase4, Tribunal }

    public const string CenaDoEscritorio = "Jogo";
    public const string CenaDoTribunal = "Tribunal";

    // Suficiente para mostrar a Biblioteca sem dívida.
    public const int OuroMinimo = 100;

    // Lido por SistemaDeSave.Gravar e ArquivoDeCasos.Registrar.
    public static bool PartidaDeDemonstracao { get; private set; }

    private class Pedido
    {
        public Destino destino;
        public RotaFinal rota;
        public bool reuAbsolvido;
        public string cena;
    }

    private static Pedido pendente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        pendente = null;
        PartidaDeDemonstracao = false;
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    public static void EncerrarDemonstracao() => PartidaDeDemonstracao = false;

    public static void MarcarDemonstracao()
    {
        if (PartidaDeDemonstracao) return;
        PartidaDeDemonstracao = true;
        Debug.Log("[MODO DEV] Partida de demonstração: o save e o arquivo de casos não serão gravados.");
    }

    public static int FaseDe(Destino destino)
    {
        switch (destino)
        {
            case Destino.Fase1: return 1;
            case Destino.Fase2: return 2;
            case Destino.Fase3: return 3;
            default: return GameManager.UltimaFase;
        }
    }

    // ===== Ir para um ponto da campanha =====

    // Tribunal: caso da Fase 4 da rota publicado só com fatos (absolvido) ou com boato (condenado).
    public static void IrPara(Destino destino, RotaFinal rota = RotaFinal.Nenhuma, bool reuAbsolvido = true)
    {
        if (FaseDe(destino) >= GameManager.UltimaFase && rota == RotaFinal.Nenhuma)
        {
            Debug.LogError("[MODO DEV] A Fase 4 e o Tribunal precisam de uma rota (A, B ou C).");
            return;
        }
        string cena = destino == Destino.Tribunal ? CenaDoTribunal : CenaDoEscritorio;
        if (!Application.CanStreamedLevelBeLoaded(cena))
        {
            Debug.LogError($"[MODO DEV] Cena '{cena}' não está nas Build Settings.");
            return;
        }

        ProgressoDoJogo.ComecarNovoJogo();
        MarcarDemonstracao();
        MarcarChavesDaPartida(FaseDe(destino));
        pendente = new Pedido { destino = destino, rota = rota, reuAbsolvido = reuAbsolvido, cena = cena };
        Debug.Log($"[MODO DEV] Indo para {destino}{(rota != RotaFinal.Nenhuma ? $", rota {rota}" : string.Empty)}" +
                  (destino == Destino.Tribunal ? $", réu {(reuAbsolvido ? "absolvido" : "condenado")}." : "."));
        CarregarCena(cena);
    }

    // Marca cutscenes, tutorial e dicas que um jogador já teria visto ao chegar nesta fase.
    private static void MarcarChavesDaPartida(int fase)
    {
        var chaves = new List<string>();
        if (fase >= 2) chaves.AddRange(new[] { TutorialManager.CHAVE_TUTORIAL_CONCLUIDO, IntroCutscene.Chave, Fase1Desfecho.Chave });
        if (fase >= 3)
            chaves.AddRange(new[]
            {
                TutorialManager.CHAVE_DICA_FATO_BOATO, TutorialManager.CHAVE_DICA_TEMPO, TutorialManager.CHAVE_DICA_DESPESAS,
                TutorialManager.CHAVE_DICA_DEDUCAO, TutorialManager.CHAVE_DICA_LINHA_EDITORIAL
            });
        if (fase >= GameManager.UltimaFase) chaves.Add(TutorialManager.CHAVE_DICA_BIBLIOTECA);
        foreach (string chave in chaves) PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();
    }

    // sceneLoaded roda após Awake/OnEnable e antes do Start: inventário, mesa, FimDeFase e tribunal já veem a partida montada.
    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (pendente == null || cena.name != pendente.cena) return;
        Pedido pedido = pendente;
        pendente = null;

        GameManager gm = GameManager.Instance;
        CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
        string erro = gm == null ? "sem GameManager na cena." : catalogo == null ? "sem CatalogoDeSave." :
            Preparar(gm, catalogo, pedido.destino, pedido.rota, pedido.reuAbsolvido);
        if (erro != null)
        {
            Debug.LogError($"[MODO DEV] Não foi possível montar a partida: {erro}");
            AvisoNaTela.Mostrar("Modo dev: " + erro);
            return;
        }
        gm.ForcarAtualizacaoUI();
        Debug.Log($"[MODO DEV] Partida montada: {Resumo(gm).Replace('\n', ' ')}");
    }

    // Não toca em disco nem na cena. Retorna null em sucesso ou o motivo da falha.
    public static string Preparar(GameManager gm, CatalogoDeSave catalogo, Destino destino, RotaFinal rota, bool reuAbsolvido)
    {
        if (gm == null || catalogo == null) return "sem GameManager ou catálogo.";
        int fase = FaseDe(destino);
        if (fase >= GameManager.UltimaFase && rota == RotaFinal.Nenhuma) return "a Fase 4 e o Tribunal precisam de uma rota.";
        if (fase < 2) return null;

        CaseData tutorial = catalogo.casos.Find(c => c != null && c.fase == 1);
        if (tutorial != null) Concluir(gm, tutorial);

        for (int anterior = 2; anterior < fase; anterior++)
        {
            gm.faseAtual = anterior;
            CaseData caso = CasoParaARota(catalogo, anterior, rota);
            if (caso == null) return $"nenhum caso da Fase {anterior} com receita no catálogo.";
            gm.casoEscolhido = caso;
            if (Publicar(gm, catalogo, caso, NivelDoPanfleto.Fatos, rota, null) == null)
                return $"não deu para montar o panfleto de '{caso.name}'.";
            Concluir(gm, caso);
            gm.PagarDespesasDaFase(anterior);
        }

        gm.faseAtual = fase;
        if (fase >= GameManager.UltimaFase)
        {
            // Se os casos escolhidos não bastarem, força as barras para dentro da faixa da rota.
            if (gm.CalcularRota() != rota) DefinirBarrasDaRota(gm, rota);
            gm.DefinirRota();
            if (gm.rotaFinal != rota) return $"a rota travou em {gm.rotaFinal}, não em {rota}.";
        }
        if (gm.capitalAtual < OuroMinimo) gm.capitalAtual = OuroMinimo;

        if (destino == Destino.Tribunal)
        {
            CaseData julgado = catalogo.casos.Find(c => c != null && c.fase == GameManager.UltimaFase && c.rota == rota);
            if (julgado == null) return $"nenhum caso da Fase 4 para a rota {rota}.";
            gm.casoEscolhido = julgado;
            ResultadoDoPanfleto publicado = Publicar(gm, catalogo, julgado, reuAbsolvido ? NivelDoPanfleto.Fatos : NivelDoPanfleto.ComBoato, rota, null);
            if (publicado == null) return $"não deu para montar o panfleto de '{julgado.name}'.";
            if ((publicado.nivel == NivelDoPanfleto.Fatos) != reuAbsolvido)
                return $"o caso '{julgado.name}' não tem pistas para um panfleto {(reuAbsolvido ? "só com fatos" : "com boato")}.";
            Concluir(gm, julgado);
        }

        gm.horasDoCaso = gm.horasRestantes = 0;
        return null;
    }

    private static void Concluir(GameManager gm, CaseData caso)
    {
        if (!gm.casosJaSelecionados.Contains(caso)) gm.casosJaSelecionados.Add(caso);
        if (!gm.casosConcluidos.Contains(caso)) gm.casosConcluidos.Add(caso);
        gm.casoEscolhido = caso;
    }

    // A = Povo 75 × Estado 35; B = 35 × 75; C = 55 × 55. Margem acima da padrão (20) amplia A e B.
    public static void DefinirBarrasDaRota(GameManager gm, RotaFinal rota)
    {
        int margem = gm.MargemValida;
        switch (rota)
        {
            case RotaFinal.A_Guilhotina:
                gm.opiniaoEstadoAtual = 35;
                gm.opiniaoPublicaAtual = Mathf.Min(100, Mathf.Max(75, 35 + margem));
                break;
            case RotaFinal.B_Tirano:
                gm.opiniaoPublicaAtual = 35;
                gm.opiniaoEstadoAtual = Mathf.Min(100, Mathf.Max(75, 35 + margem));
                break;
            default:
                gm.opiniaoPublicaAtual = gm.opiniaoEstadoAtual = 55;
                break;
        }
    }

    // ===== Casos, pistas e panfleto =====

    // A quer Povo alto, B quer Estado alto, C (e nenhuma) quer equilíbrio.
    private static int Nota(int desnivel, RotaFinal rota)
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return desnivel;
            case RotaFinal.B_Tirano: return -desnivel;
            default: return -Mathf.Abs(desnivel);
        }
    }

    // Caso sem rota própria cuja versão só com fatos mais puxa para a rota; empate desfeito pelo nome.
    public static CaseData CasoParaARota(CatalogoDeSave catalogo, int fase, RotaFinal rota)
    {
        CaseData melhor = null;
        int melhorNota = int.MinValue;
        var casos = catalogo.casos.FindAll(c => c != null && c.fase == fase && c.rota == RotaFinal.Nenhuma && c.receitaDoPanfleto != null);
        casos.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
        foreach (CaseData caso in casos)
        {
            ReceitaDeCaso.Versao fatos = caso.receitaDoPanfleto.soFatos;
            int nota = Nota(fatos != null ? fatos.povo - fatos.estado : 0, rota);
            if (melhor == null || nota > melhorNota) { melhor = caso; melhorNota = nota; }
        }
        return melhor;
    }

    public static List<Item> PistasDoCaso(CatalogoDeSave catalogo, CaseData caso)
    {
        var lista = new List<Item>();
        if (catalogo == null || caso == null) return lista;
        foreach (Item item in catalogo.itens)
            if (item != null && item.caso == caso && item.EhPista && !item.EhSuporte && !string.IsNullOrEmpty(item.itemID) &&
                !lista.Exists(i => i.itemID == item.itemID))
                lista.Add(item);
        lista.Sort((x, y) => string.CompareOrdinal(x.itemID, y.itemID));
        return lista;
    }

    // Nunca as duas de um mesmo par contraditório. Sem par exato para versão com mentira, aceita boato ou calúnia.
    public static bool EscolherPistas(CaseData caso, List<Item> pistas, NivelDoPanfleto alvo, out Item a, out Item b)
    {
        a = b = null;
        Item reservaA = null, reservaB = null;
        for (int i = 0; i < pistas.Count; i++)
            for (int j = i + 1; j < pistas.Count; j++)
            {
                Item x = pistas[i], y = pistas[j];
                if (Deducao.Aderente(caso) && caso.deducao.ParDe(x) != null && caso.deducao.ParDe(x) == caso.deducao.ParDe(y)) continue;
                NivelDoPanfleto nivel = ReceitaDeCaso.Classificar(x, y);
                if (nivel == alvo) { a = x; b = y; return true; }
                if (reservaA == null && alvo != NivelDoPanfleto.Fatos && nivel != NivelDoPanfleto.Fatos) { reservaA = x; reservaB = y; }
            }
        a = reservaA; b = reservaB;
        return a != null;
    }

    private static LinhaEditorial LinhaParaARota(ReceitaDeCaso receita, Item a, Item b, RotaFinal rota)
    {
        if (!receita.ExigeLinhaEditorial) return LinhaEditorial.Nenhuma;
        LinhaEditorial melhor = LinhasEditoriais.Opcoes[0];
        int melhorNota = int.MinValue;
        foreach (LinhaEditorial linha in LinhasEditoriais.Opcoes)
        {
            ResultadoDoPanfleto r = CalculadoraDePanfleto.Calcular(receita, a, b, new List<Item>(), linha);
            if (r == null) continue;
            int nota = Nota(r.povo - r.estado, rota);
            if (nota > melhorNota) { melhor = linha; melhorNota = nota; }
        }
        return melhor;
    }

    // Simula a prensa: registra todas as pistas como achadas, decide os pares e aplica o impacto. Não conclui o caso.
    public static ResultadoDoPanfleto Publicar(GameManager gm, CatalogoDeSave catalogo, CaseData caso, NivelDoPanfleto alvo,
                                               RotaFinal rota, InventoryManager inventario)
    {
        ReceitaDeCaso receita = caso != null ? caso.receitaDoPanfleto : null;
        if (gm == null || receita == null || gm.FoiPublicado(caso)) return null;

        List<Item> pistas = PistasDoCaso(catalogo, caso);
        if (!EscolherPistas(caso, pistas, alvo, out Item a, out Item b)) return null;
        ResultadoDoPanfleto resultado = CalculadoraDePanfleto.Calcular(receita, a, b, new List<Item>(), LinhaParaARota(receita, a, b, rota));
        if (resultado == null || resultado.panfleto == null) return null;

        if (caso.alegacoesIniciais != null)
            foreach (Item carta in caso.alegacoesIniciais) gm.RegistrarEvidencia(carta);
        foreach (Item pista in pistas) gm.RegistrarEvidencia(pista);
        DecidirPares(gm, caso, a, b);

        Vector3Int aplicado = gm.AplicarImpactoPanfleto(resultado.povo, resultado.estado, resultado.ouro);
        gm.RegistrarPanfletoDeCaso(caso, resultado, aplicado);

        Item panfleto = resultado.panfleto.Clone();
        panfleto.itemAmt = 1;
        if (inventario == null || !inventario.AddItem(panfleto)) gm.inventarioSalvo.Add(panfleto);
        return resultado;
    }

    // Cada par mantém a pista impressa (ou, se nenhuma, o fato) e descarta a outra.
    private static void DecidirPares(GameManager gm, CaseData caso, Item a, Item b)
    {
        if (!Deducao.Aderente(caso)) return;
        foreach (ParContraditorio par in caso.deducao.pares)
        {
            if (par == null || par.a == null || par.b == null || Deducao.ParDecidido(gm, par.a)) continue;
            Item fica = par.Contem(a) ? a : par.Contem(b) ? b : par.a.confiabilidade == Confiabilidade.Fato ? par.a : par.b;
            Item sai = par.OutraDe(fica);
            gm.escolhasDeDeducao.Add(new EscolhaDeDeducao { caso = caso.name, escolhida = fica.itemID, descartada = sai.itemID });
        }
    }

    // ===== Atalhos na partida atual =====

    // Sem caso em andamento, aceita o que mais puxa para a rota.
    public static CaseData CasoAtualOuAceitar(GameManager gm, CatalogoDeSave catalogo, out string motivo)
    {
        motivo = null;
        if (gm.CasoAtualEmAndamento) return gm.casoEscolhido;
        if (gm.faseAtual <= 1) { motivo = "No tutorial não há caso da mesa: use \"Começar em: Fase 2\"."; return null; }

        CaseData melhor = null;
        int melhorNota = int.MinValue;
        foreach (CaseData caso in catalogo.casos)
        {
            if (caso == null || caso.receitaDoPanfleto == null || !gm.PodeAceitarCaso(caso, out _)) continue;
            ReceitaDeCaso.Versao fatos = caso.receitaDoPanfleto.soFatos;
            int nota = Nota(fatos != null ? fatos.povo - fatos.estado : 0, gm.rotaFinal);
            if (melhor == null || nota > melhorNota || (nota == melhorNota && string.CompareOrdinal(caso.name, melhor.name) < 0))
            { melhor = caso; melhorNota = nota; }
        }
        if (melhor == null)
        {
            motivo = gm.FaseConcluida && gm.faseAtual < GameManager.UltimaFase
                ? "Os casos desta fase já foram concluídos: volte ao escritório para fechar a fase."
                : "Não há caso para aceitar agora.";
            return null;
        }
        return gm.ConfirmarCaso(melhor) ? melhor : null;
    }

    public static string ReceberPistas()
    {
        GameManager gm = GameManager.Instance;
        CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
        if (gm == null || catalogo == null) return "Sem partida em andamento.";
        CaseData caso = CasoAtualOuAceitar(gm, catalogo, out string motivo);
        if (caso == null) return motivo;
        MarcarDemonstracao();

        var itens = PistasDoCaso(catalogo, caso);
        foreach (Item item in catalogo.itens)
            if (item != null && item.EhSuporte && item.caso == caso && !itens.Exists(i => i.itemID == item.itemID)) itens.Add(item);

        int entregues = 0;
        foreach (Item item in itens)
        {
            if (gm.EvidenciaObtida(item) || Deducao.Descartada(gm, item)) continue;
            Item copia = item.Clone();
            copia.itemAmt = 1;
            bool entrou = gm.inventoryManager != null ? gm.inventoryManager.AddItem(copia) : AdicionarNoSalvo(gm, copia);
            if (entrou) entregues++;
        }
        return entregues > 0 ? $"Modo dev: {entregues} pista(s)/documento(s) de '{Titulo(caso)}' no inventário."
                             : $"Modo dev: você já tem tudo de '{Titulo(caso)}' (ou o inventário está cheio).";
    }

    private static bool AdicionarNoSalvo(GameManager gm, Item item)
    {
        gm.inventarioSalvo.Add(item);
        gm.RegistrarEvidencia(item);
        return true;
    }

    public static string ConcluirCasoAtual(bool soComFatos)
    {
        GameManager gm = GameManager.Instance;
        CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
        if (gm == null || catalogo == null) return "Sem partida em andamento.";
        CaseData caso = CasoAtualOuAceitar(gm, catalogo, out string motivo);
        if (caso == null) return motivo;
        if (caso.receitaDoPanfleto == null) return $"'{Titulo(caso)}' não tem receita de panfleto.";
        MarcarDemonstracao();

        InventoryManager inventario = gm.inventoryManager;
        ResultadoDoPanfleto resultado = Publicar(gm, catalogo, caso, soComFatos ? NivelDoPanfleto.Fatos : NivelDoPanfleto.ComBoato, gm.rotaFinal, inventario);
        if (resultado == null) return $"Não deu para montar o panfleto de '{Titulo(caso)}'.";

        // Remove o que a prensa teria consumido e o que o quadro teria descartado.
        List<string> impressas = resultado.pistas;
        if (inventario != null)
            inventario.RemoverItens(i => i != null && i.caso == caso && (impressas.Contains(i.itemID) || Deducao.Descartada(gm, i)));
        else
            gm.inventarioSalvo.RemoveAll(i => i != null && i.caso == caso && (impressas.Contains(i.itemID) || Deducao.Descartada(gm, i)));
        gm.ConcluirCaso(caso);

        string versao = resultado.nivel == NivelDoPanfleto.Fatos ? "só com fatos" : resultado.nivel == NivelDoPanfleto.ComBoato ? "com boato" : "com calúnia";
        string proximo = gm.faseAtual >= GameManager.UltimaFase ? "Fale com o Dupaty ou use \"Ir ao Tribunal\"." : "Volte ao escritório para fechar a fase.";
        return $"Modo dev: '{Titulo(caso)}' publicado {versao}. {proximo}";
    }

    public static void MudarBarras(int povo, int estado, int ouro)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        MarcarDemonstracao();
        gm.opiniaoPublicaAtual = Mathf.Clamp(gm.opiniaoPublicaAtual + povo, 0, 100);
        gm.opiniaoEstadoAtual = Mathf.Clamp(gm.opiniaoEstadoAtual + estado, 0, 100);
        gm.capitalAtual += ouro;
        gm.ForcarAtualizacaoUI();
    }

    public static string ReporHoras()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.CasoAtualEmAndamento) return "Não há caso em andamento.";
        MarcarDemonstracao();
        if (gm.horasDoCaso <= 0) gm.horasDoCaso = gm.horasPorCaso;
        gm.horasRestantes = gm.horasDoCaso;
        gm.ForcarAtualizacaoUI();
        return $"Modo dev: {gm.horasRestantes}h de investigação.";
    }

    // Guarda o inventário como a porta faria; o FimDeFase fecha a fase no escritório.
    public static void VoltarAoEscritorio()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();
        CarregarCena(CenaDoEscritorio);
    }

    public static string IrAoTribunal()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.faseAtual < GameManager.UltimaFase) return "O Tribunal é só na Fase 4.";
        CaseData julgado = gm.casosConcluidos.Find(c => c != null && c.fase == GameManager.UltimaFase && gm.CasoDaRotaAtual(c));
        if (julgado == null || !gm.FoiPublicado(julgado)) return "Publique o caso da Fase 4 antes (\"Concluir caso\").";
        gm.casoEscolhido = julgado;
        if (gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();
        CarregarCena(CenaDoTribunal);
        return null;
    }

    // ===== Auxiliares =====

    private static void CarregarCena(string cena)
    {
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.LoadScene(cena);
        else SceneManager.LoadScene(cena);
    }

    public static string Titulo(CaseData caso) =>
        caso == null ? "-" : (string.IsNullOrWhiteSpace(caso.caseTitle) ? caso.name : caso.caseTitle).Trim();

    public static string NomeDaRota(RotaFinal rota)
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return "A (Guilhotina)";
            case RotaFinal.B_Tirano: return "B (Tirano)";
            case RotaFinal.C_Equilibrio: return "C (Esquecido)";
            default: return "nenhuma";
        }
    }

    public static string Resumo(GameManager gm)
    {
        if (gm == null) return "Sem partida em andamento.";
        string caso = gm.casoEscolhido == null ? "nenhum" : Titulo(gm.casoEscolhido) + (gm.CasoAtualEmAndamento ? " (em andamento)" : " (concluído)");
        string horas = gm.CasoAtualEmAndamento && gm.horasDoCaso > 0 ? $" · Horas {gm.horasRestantes}/{gm.horasDoCaso}" : string.Empty;
        string rota = gm.rotaFinal != RotaFinal.Nenhuma
            ? $"Rota travada: {NomeDaRota(gm.rotaFinal)}"
            : $"Rota ainda não travada (trava ao entrar na Fase 4; as barras de agora dariam {NomeDaRota(gm.CalcularRota())})";
        return $"Fase {gm.faseAtual} · Caso: {caso}{horas}\nPovo {gm.opiniaoPublicaAtual} · Estado {gm.opiniaoEstadoAtual} · Ouro {gm.capitalAtual} · {rota}" +
               (PartidaDeDemonstracao ? " · <color=#E0A060>demonstração: não grava</color>" : string.Empty);
    }
}
