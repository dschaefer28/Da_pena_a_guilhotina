using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Modo dev (apresentação do TCC na banca): monta na hora uma partida em qualquer ponto da campanha (início de uma fase
/// ou o Tribunal de cada final) e oferece atalhos para a partida atual. O painel é o ModoDevUI (tecla F1).
///
/// Partida de demonstração: o que é aberto por aqui, e qualquer atalho usado numa partida normal, NÃO grava save nem
/// entra no arquivo de casos resolvidos. Os slots ficam como estavam no último ponto de save. A partida volta a gravar no
/// próximo Novo Jogo ou Carregar (ProgressoDoJogo.ComecarNovoJogo chama EncerrarDemonstracao).
///
/// O estado é montado com as regras do jogo, sem mudar nenhum asset: as pistas de cada caso vêm do CatalogoDeSave, o
/// panfleto é calculado pela CalculadoraDePanfleto, as despesas e a rota vêm do GameManager. Nas fases já passadas o
/// panfleto sai só com fatos, do caso que mais puxa para a rota pedida.
/// </summary>
public static class ModoDev
{
    public enum Destino { Fase1, Fase2, Fase3, Fase4, Tribunal }

    public const string CenaDoEscritorio = "Jogo";
    public const string CenaDoTribunal = "Tribunal";

    /// <summary>Ouro mínimo de uma partida montada a partir da Fase 2 (dá para mostrar a Biblioteca sem dívida).</summary>
    public const int OuroMinimo = 100;

    /// <summary>A partida atual não grava save nem arquivo de casos (SistemaDeSave.Gravar e ArquivoDeCasos.Registrar).</summary>
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

    /// <summary>Novo Jogo e Carregar: a partida volta a gravar.</summary>
    public static void EncerrarDemonstracao() => PartidaDeDemonstracao = false;

    /// <summary>Daqui em diante a partida não grava mais (o save do slot fica no último ponto de save).</summary>
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

    /// <summary>Descarta a partida atual e abre uma de demonstração no destino: o escritório no início da fase ou o
    /// Tribunal com o caso da Fase 4 da rota já publicado (só com fatos = réu absolvido; com boato = condenado).</summary>
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

        ProgressoDoJogo.ComecarNovoJogo(); // zera cutscenes/tutorial e descarta o GameManager e o TutorialManager atuais
        MarcarDemonstracao();
        MarcarChavesDaPartida(FaseDe(destino));
        pendente = new Pedido { destino = destino, rota = rota, reuAbsolvido = reuAbsolvido, cena = cena };
        Debug.Log($"[MODO DEV] Indo para {destino}{(rota != RotaFinal.Nenhuma ? $", rota {rota}" : string.Empty)}" +
                  (destino == Destino.Tribunal ? $", réu {(reuAbsolvido ? "absolvido" : "condenado")}." : "."));
        CarregarCena(cena);
    }

    // O que um jogador de verdade já teria visto ao chegar nesta fase: a cutscene de abertura e o tutorial (Fase 2+),
    // as dicas das Fases 2 e 3 (Fase 3+) e a da Biblioteca (Fase 4). As chaves voltam ao normal no próximo Novo Jogo ou
    // Carregar, que as apagam e restauram as do save.
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

    // Depois do Awake/OnEnable dos objetos da cena nova e antes do Start: o inventário, a mesa, o FimDeFase e o tribunal
    // já nascem com a partida montada.
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

    /// <summary>
    /// Monta a partida num GameManager recém-criado, sem tocar em disco nem na cena: o tutorial concluído, um caso por fase
    /// já passada publicado só com fatos (o que mais puxa para a rota), as despesas de cada fase, a rota travada ao entrar
    /// na Fase 4 e, no Tribunal, o caso da rota publicado na versão pedida. Devolve nulo ou o motivo da falha.
    /// </summary>
    public static string Preparar(GameManager gm, CatalogoDeSave catalogo, Destino destino, RotaFinal rota, bool reuAbsolvido)
    {
        if (gm == null || catalogo == null) return "sem GameManager ou catálogo.";
        int fase = FaseDe(destino);
        if (fase >= GameManager.UltimaFase && rota == RotaFinal.Nenhuma) return "a Fase 4 e o Tribunal precisam de uma rota.";
        if (fase < 2) return null; // Fase 1: o jogo novo, como o Novo Jogo do menu (só não grava)

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
            // Os casos escolhidos já puxam para a rota; se ainda assim as barras não chegarem lá, ficam no meio da faixa dela.
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

    /// <summary>Barras bem dentro da faixa da rota: A = Povo 75 × Estado 35; B = 35 × 75; C = 55 × 55. Com uma margem
    /// maior que a padrão (20), A e B são empurradas até ela.</summary>
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

    // Quanto um desnível Povo − Estado serve à rota: A quer Povo alto, B quer Estado alto, C (e nenhuma) quer equilíbrio.
    private static int Nota(int desnivel, RotaFinal rota)
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return desnivel;
            case RotaFinal.B_Tirano: return -desnivel;
            default: return -Mathf.Abs(desnivel);
        }
    }

    /// <summary>Caso da fase (sem rota própria, com receita) cuja versão só com fatos mais puxa para a rota. Empate: pelo nome.</summary>
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

    /// <summary>Pistas do caso no catálogo (sem cartas do cliente nem documentos de apoio), sem repetir itemID.</summary>
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

    /// <summary>Duas pistas que a prensa aceitaria juntas (nunca as duas de um mesmo par contraditório) e que dão a versão
    /// pedida. Sem par exato para uma versão com mentira, aceita qualquer outra com mentira (boato ou calúnia).</summary>
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

    // A linha editorial que mais puxa para a rota (receitas sem linha: neutra).
    private static LinhaEditorial LinhaParaARota(ReceitaDeCaso receita, Item a, Item b, RotaFinal rota)
    {
        if (!receita.ExigeLinhaEditorial) return LinhaEditorial.Neutra;
        LinhaEditorial melhor = LinhaEditorial.DefesaDoPovo;
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

    /// <summary>
    /// Publica o panfleto do caso como a prensa faria com as duas pistas que dão a versão pedida: as cartas e todas as
    /// pistas do caso contam como achadas, cada par fica decidido pela pista impressa, o impacto vai para as barras e a
    /// publicação (com a revelação, se for mentira) fica registrada. O panfleto vai para o inventário da cena, ou para o
    /// inventário salvo se não houver cena. Não conclui o caso. Nulo se não houver receita ou pistas.
    /// </summary>
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

    // Cada par fica com a pista impressa dele (ou, se nenhuma das duas foi impressa, com o fato) e descarta a outra.
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

    /// <summary>O caso em andamento; sem um, aceita na mesa o caso da fase que mais puxa para a rota (como o jogador faria).
    /// Nulo, com o motivo, se não houver caso para aceitar.</summary>
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

    /// <summary>Entrega as pistas e os documentos de apoio do caso atual que o jogador ainda não achou. Devolve a mensagem.</summary>
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

    /// <summary>Publica e conclui o caso atual sem passar pela prensa (aceita um na mesa, se preciso). Devolve a mensagem.</summary>
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

        // O que a prensa teria consumido (as duas pistas impressas) e o que o quadro teria descartado sai do inventário.
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

    /// <summary>Leva ao escritório (onde a fase concluída é fechada pelo FimDeFase) guardando o inventário, como a porta.</summary>
    public static void VoltarAoEscritorio()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.inventoryManager != null) gm.inventoryManager.SalvarEstadoAtual();
        CarregarCena(CenaDoEscritorio);
    }

    /// <summary>Vai ao Tribunal sem passar pelo Dupaty. Só na Fase 4, com o caso da rota publicado.</summary>
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

    /// <summary>Situação da partida em duas linhas (o painel mostra isto).</summary>
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
