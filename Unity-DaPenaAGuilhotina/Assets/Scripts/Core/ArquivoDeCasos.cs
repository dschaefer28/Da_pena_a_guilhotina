using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Histórico de casos resolvidos do painel "Casos resolvidos". Arquivo separado do save porque sobrevive ao
/// Novo Jogo (até <see cref="MaximoDePartidas"/> partidas). O desfecho só é exibido depois que o jogo o revelou
/// (fim da fase ou veredito), para não entregar a verdade das pistas.
/// </summary>
public static class ArquivoDeCasos
{
    public const int MaximoDePartidas = 10;
    public const string NomeDoArquivo = "arquivo_de_casos.json";

    [Serializable]
    public class CasoArquivado
    {
        public string caso;                       // CaseData.name
        public bool temPanfleto;
        public SistemaDeSave.PanfletoSalvo panfleto;
        public List<string> acreditou = new List<string>(); // itemIDs escolhidos no quadro de dedução

        // Copiada enquanto pendente: após o fim da fase o save não a tem mais.
        public bool temRevelacao;
        public string textoRevelacao;
        public int revelacaoPovo, revelacaoEstado;
        public LinhaEditorial revelacaoLinha;
    }

    [Serializable]
    public class PartidaArquivada
    {
        public string id;
        public string inicio;
        public string atualizada;
        public int faseAtual;
        public bool concluida;
        public string desfechoTitulo;
        public bool reuAbsolvido;
        public RotaFinal rota;
        public List<CasoArquivado> casos = new List<CasoArquivado>();
    }

    [Serializable]
    public class Dados
    {
        public List<PartidaArquivada> partidas = new List<PartidaArquivada>(); // da mais antiga para a mais recente
    }

    public static string CaminhoDoArquivo => Path.Combine(Application.persistentDataPath, NomeDoArquivo);

    // ===== Registrar =====

    public static void Registrar(GameManager gm)
    {
        if (gm == null || !Application.isPlaying) return;
        Registrar(SistemaDeSave.CapturarDados(gm, string.Empty, 0));
    }

    public static void Registrar(SistemaDeSave.DadosDeSave dados)
    {
        // isPlaying: testes de EditMode montam GameManagers e saves falsos.
        if (dados == null || !Application.isPlaying || string.IsNullOrEmpty(dados.idDaPartida)) return;
        if (ModoDev.PartidaDeDemonstracao) return;
        if (dados.casosConcluidos == null || dados.casosConcluidos.Count == 0) return;

        try
        {
            Dados arquivo = Ler() ?? new Dados();
            int indice = arquivo.partidas.FindIndex(p => p != null && p.id == dados.idDaPartida);
            PartidaArquivada anterior = indice >= 0 ? arquivo.partidas[indice] : null;
            PartidaArquivada nova = Montar(dados, anterior, CatalogoDeSave.Instancia);

            if (indice >= 0) arquivo.partidas.RemoveAt(indice);
            arquivo.partidas.Add(nova);
            while (arquivo.partidas.Count > MaximoDePartidas) arquivo.partidas.RemoveAt(0);

            string temporario = CaminhoDoArquivo + ".tmp";
            File.WriteAllText(temporario, JsonUtility.ToJson(arquivo, true));
            if (File.Exists(CaminhoDoArquivo)) File.Delete(CaminhoDoArquivo);
            File.Move(temporario, CaminhoDoArquivo);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ArquivoDeCasos] Não foi possível atualizar o arquivo de casos: {e.Message}");
        }
    }

    // Revelação: pendente no save > já arquivada > derivada da receita do caso.
    public static PartidaArquivada Montar(SistemaDeSave.DadosDeSave dados, PartidaArquivada anterior, CatalogoDeSave catalogo)
    {
        string agora = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        var partida = new PartidaArquivada
        {
            id = dados.idDaPartida,
            inicio = anterior != null && !string.IsNullOrEmpty(anterior.inicio) ? anterior.inicio : agora,
            atualizada = agora,
            faseAtual = dados.faseAtual,
            concluida = dados.jogoConcluido,
            desfechoTitulo = dados.desfechoTitulo,
            reuAbsolvido = dados.desfechoReuAbsolvido,
            rota = dados.jogoConcluido ? dados.desfechoRota : dados.rotaFinal
        };

        foreach (string nome in dados.casosConcluidos)
        {
            if (string.IsNullOrEmpty(nome) || partida.casos.Exists(c => c.caso == nome)) continue;
            var caso = new CasoArquivado { caso = nome };

            SistemaDeSave.PanfletoSalvo panfleto = dados.panfletosPublicados?.Find(p => p != null && p.caso == nome);
            caso.temPanfleto = panfleto != null;
            caso.panfleto = panfleto;

            if (dados.escolhasDeDeducao != null)
                foreach (EscolhaDeDeducao escolha in dados.escolhasDeDeducao)
                    if (escolha != null && escolha.caso == nome && !string.IsNullOrEmpty(escolha.escolhida))
                        caso.acreditou.Add(escolha.escolhida);

            SistemaDeSave.RevelacaoSalva pendente = dados.revelacoesPendentes?.Find(r => r != null && r.caso == nome);
            CasoArquivado antigo = anterior?.casos?.Find(c => c != null && c.caso == nome);
            if (pendente != null)
            {
                caso.temRevelacao = true;
                caso.textoRevelacao = pendente.texto;
                caso.revelacaoPovo = pendente.povo;
                caso.revelacaoEstado = pendente.estado;
                caso.revelacaoLinha = pendente.linha;
            }
            else if (antigo != null && antigo.temRevelacao)
            {
                caso.temRevelacao = true;
                caso.textoRevelacao = antigo.textoRevelacao;
                caso.revelacaoPovo = antigo.revelacaoPovo;
                caso.revelacaoEstado = antigo.revelacaoEstado;
                caso.revelacaoLinha = antigo.revelacaoLinha;
            }
            else if (panfleto != null && panfleto.nivel != NivelDoPanfleto.Fatos)
            {
                // Arquivo criado após o fim da fase (ou save antigo).
                CaseData dadosDoCaso = catalogo != null ? catalogo.BuscarCaso(nome) : null;
                ReceitaDeCaso.Versao versao = dadosDoCaso != null && dadosDoCaso.receitaDoPanfleto != null
                    ? dadosDoCaso.receitaDoPanfleto.VersaoDe(panfleto.nivel) : null;
                if ((versao != null && versao.TemRevelacao) || panfleto.penalidadePovo != 0 || panfleto.penalidadeEstado != 0)
                {
                    caso.temRevelacao = true;
                    caso.textoRevelacao = versao != null ? versao.textoRevelacao : null;
                    caso.revelacaoPovo = panfleto.penalidadePovo;
                    caso.revelacaoEstado = panfleto.penalidadeEstado;
                    caso.revelacaoLinha = panfleto.linha;
                }
            }
            partida.casos.Add(caso);
        }
        return partida;
    }

    // ===== Ler =====

    public static List<PartidaArquivada> Partidas()
    {
        Dados arquivo = Ler();
        var lista = new List<PartidaArquivada>();
        if (arquivo?.partidas != null)
            foreach (PartidaArquivada p in arquivo.partidas)
                if (p != null && p.casos != null && p.casos.Count > 0) lista.Add(p);
        return lista;
    }

    private static Dados Ler()
    {
        try
        {
            if (!File.Exists(CaminhoDoArquivo)) return null;
            return JsonUtility.FromJson<Dados>(File.ReadAllText(CaminhoDoArquivo));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ArquivoDeCasos] Não foi possível ler o arquivo de casos: {e.Message}");
            return null;
        }
    }

    // ===== Textos do painel =====

    public static string TituloDo(CaseData caso, string nomeDoAsset)
    {
        if (caso != null && !string.IsNullOrWhiteSpace(caso.tituloNoArquivo)) return caso.tituloNoArquivo.Trim();
        if (caso != null && !string.IsNullOrWhiteSpace(caso.caseTitle)) return caso.caseTitle.Trim();
        return caso != null && caso.fase == 1 ? "Tutorial" : nomeDoAsset; // o caso do tutorial não tem título na mesa
    }

    public static string NomeDaFase(int fase)
    {
        switch (fase)
        {
            case 1: return "Fase 1 · Tutorial";
            case GameManager.UltimaFase: return $"Fase {fase} · Tribunal";
            default: return $"Fase {fase}";
        }
    }

    public static string RotuloDaPartida(PartidaArquivada partida, int numero, int total)
    {
        string situacao = partida.concluida ? "concluída" : "em andamento";
        return $"Partida {numero} de {total} · iniciada em {partida.inicio} · {situacao}";
    }

    // Fase do caso encerrada ou veredito dado; antes disso a história entregaria qual afirmação era verdadeira.
    public static bool DesfechoConhecido(PartidaArquivada partida, int fase) =>
        partida.concluida || (fase > 0 && fase < GameManager.UltimaFase && partida.faseAtual > fase);

    public static string TextoDoCaso(PartidaArquivada partida, CasoArquivado arquivado, CatalogoDeSave catalogo)
    {
        CaseData caso = catalogo != null ? catalogo.BuscarCaso(arquivado.caso) : null;
        int fase = caso != null ? caso.fase : 0;
        bool conhecido = DesfechoConhecido(partida, fase);
        var t = new StringBuilder();

        t.Append("<size=125%>").Append(TituloDo(caso, arquivado.caso)).Append("</size>\n");
        if (fase > 0) t.Append("<i>").Append(NomeDaFase(fase)).Append("</i>\n");

        bool temHistoria = caso != null && !string.IsNullOrWhiteSpace(caso.historia);
        if (conhecido && temHistoria)
        {
            Secao(t, "Personagens", Personagens(caso.personagens));
            Secao(t, "O que aconteceu", caso.historia.Trim());
        }
        else if (caso != null && !string.IsNullOrWhiteSpace(caso.caseDescription))
            Secao(t, "O pedido", caso.caseDescription.Trim());

        Secao(t, "O seu panfleto", Panfleto(arquivado, caso, catalogo));
        Secao(t, "Desfecho", Desfecho(partida, arquivado, caso, fase, conhecido));
        if (conhecido && caso != null && !string.IsNullOrWhiteSpace(caso.notaHistorica))
            Secao(t, "Na história real", caso.notaHistorica.Trim());
        return t.ToString();
    }

    private static void Secao(StringBuilder t, string titulo, string corpo)
    {
        if (string.IsNullOrWhiteSpace(corpo)) return;
        // A fonte não tem negrito: o título da seção se destaca pela cor e pelas maiúsculas.
        t.Append("\n<color=#6B2E14><size=90%>").Append(titulo.ToUpperInvariant()).Append("</size></color>\n").Append(corpo).Append('\n');
    }

    // "Nome — papel" por linha vira uma lista com marcadores.
    private static string Personagens(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var t = new StringBuilder();
        foreach (string linha in texto.Split('\n'))
        {
            string limpa = linha.Trim().TrimStart('•', '-', '*').Trim();
            if (limpa.Length > 0) t.Append("• ").Append(limpa).Append('\n');
        }
        return t.ToString().TrimEnd('\n');
    }

    private static string Panfleto(CasoArquivado arquivado, CaseData caso, CatalogoDeSave catalogo)
    {
        var t = new StringBuilder();
        SistemaDeSave.PanfletoSalvo p = arquivado.temPanfleto ? arquivado.panfleto : null;

        string titulo = TituloDoPanfleto(caso, p);
        if (!string.IsNullOrWhiteSpace(titulo)) t.Append("<i>").Append(titulo.Trim()).Append("</i>\n");
        if (caso != null && !string.IsNullOrWhiteSpace(caso.teorDoPanfleto)) t.Append(caso.teorDoPanfleto.Trim()).Append('\n');
        if (p == null) return t.Length > 0 ? t.ToString().TrimEnd('\n') : "O panfleto do caso saiu da prensa.";

        AnexarPublicacao(t, p, caso, catalogo);
        return t.ToString().TrimEnd('\n');
    }

    // Também usado pela FichaDaPista. Não revela qual afirmação era verdadeira.
    public static string DetalhesDaPublicacao(SistemaDeSave.PanfletoSalvo p, CaseData caso, CatalogoDeSave catalogo)
    {
        if (p == null) return string.Empty;
        var t = new StringBuilder();
        AnexarPublicacao(t, p, caso, catalogo);
        return t.ToString().Trim('\n');
    }

    private static void AnexarPublicacao(StringBuilder t, SistemaDeSave.PanfletoSalvo p, CaseData caso, CatalogoDeSave catalogo)
    {
        if (p.pistas != null && p.pistas.Count > 0)
        {
            t.Append("\nO panfleto afirmava:\n");
            foreach (string id in p.pistas)
            {
                if (string.IsNullOrEmpty(id)) continue;
                Item pista = catalogo != null ? catalogo.BuscarItem(id) : null;
                string frase = pista != null && !string.IsNullOrWhiteSpace(pista.afirmacaoNoPanfleto)
                    ? pista.afirmacaoNoPanfleto.Trim()
                    : (pista != null ? pista.NomeExibicao : id);
                t.Append("• ").Append(frase).Append('\n');
            }
        }
        if (p.suportes != null && p.suportes.Count > 0)
            t.Append("Como apoio, citava: ").Append(Nomes(p.suportes, catalogo)).Append(".\n");

        string tom = TomDaLinha(p.linha, caso);
        if (!string.IsNullOrEmpty(tom)) t.Append('\n').Append(tom).Append('\n');
        if (p.aplicadoConhecido)
            t.Append("Efeito da publicação: ").Append(Valores(p.povoAplicado, p.estadoAplicado, p.ouroAplicado)).Append('.');
    }

    private static string TituloDoPanfleto(CaseData caso, SistemaDeSave.PanfletoSalvo p)
    {
        if (caso == null) return null;
        if (!string.IsNullOrWhiteSpace(caso.tituloDoPanfleto)) return caso.tituloDoPanfleto;
        ReceitaDeCaso receita = caso.receitaDoPanfleto;
        if (receita == null) return null;
        Item item = p != null ? receita.PanfletoDe(receita.VersaoDe(p.nivel)) : receita.panfletoPadrao;
        return item != null ? item.NomeExibicao : null;
    }

    private static string TomDaLinha(LinhaEditorial linha, CaseData caso)
    {
        switch (linha)
        {
            case LinhaEditorial.Neutro:
                return "Tom: neutro. O texto contava o caso sem exagero, sem manchetes carregadas.";
            case LinhaEditorial.DefesaDoPovo: // legado: saves antigos
                return "Tom: defesa do povo. O texto falava aos pobres e aos trabalhadores de Paris e apontava o dedo para os poderosos.";
            case LinhaEditorial.AgradarOPoder: // legado
                string rotulo = LinhasEditoriais.Rotulo(linha, caso != null ? caso.receitaDoPanfleto : null);
                rotulo = rotulo.Length > 0 ? char.ToLowerInvariant(rotulo[0]) + rotulo.Substring(1) : rotulo;
                return $"Tom: {rotulo}. O texto pedia ordem, poupava as autoridades e evitava acusar quem estava no poder.";
            case LinhaEditorial.Sensacionalista:
                return "Tom: sensacionalista. Manchetes fortes e detalhes carregados, escritos para vender o máximo possível.";
            default:
                return null;
        }
    }

    private static string Desfecho(PartidaArquivada partida, CasoArquivado arquivado, CaseData caso, int fase, bool conhecido)
    {
        if (!conhecido)
            return fase >= GameManager.UltimaFase
                ? "O julgamento ainda não aconteceu. A história do caso e o desfecho aparecem aqui depois do veredito."
                : "A história do caso e o desfecho aparecem aqui quando esta fase terminar.";

        var t = new StringBuilder();
        // Sem registro da publicação (tutorial), conta como só fatos.
        NivelDoPanfleto nivel = arquivado.temPanfleto && arquivado.panfleto != null ? arquivado.panfleto.nivel : NivelDoPanfleto.Fatos;
        string texto = caso != null ? caso.DesfechoDa(nivel) : null;
        if (!string.IsNullOrWhiteSpace(texto)) t.Append(texto.Trim()).Append('\n');

        if (arquivado.temRevelacao)
        {
            string revelacao = string.IsNullOrWhiteSpace(arquivado.textoRevelacao)
                ? $"Parte do que o panfleto de \"{TituloDo(caso, arquivado.caso)}\" afirmava foi desmentida nas ruas."
                : arquivado.textoRevelacao.Trim();
            t.Append("\n<i>").Append(revelacao).Append("</i>\n");
            string perda = Valores(arquivado.revelacaoPovo, arquivado.revelacaoEstado, 0);
            if (!string.IsNullOrEmpty(perda)) t.Append("Consequência: ").Append(perda).Append(".\n");
        }
        else if (fase < GameManager.UltimaFase && string.IsNullOrWhiteSpace(texto))
            t.Append("Nenhuma informação do panfleto foi desmentida depois da publicação.\n");

        if (fase >= GameManager.UltimaFase)
        {
            string titulo = string.IsNullOrWhiteSpace(partida.desfechoTitulo) ? "Fim do jogo" : partida.desfechoTitulo.Trim();
            t.Append("\nNo tribunal, o réu foi ").Append(partida.reuAbsolvido ? "absolvido" : "condenado")
             .Append(". ").Append(titulo).Append('.');
        }
        return t.ToString().TrimEnd('\n');
    }

    private static string Nomes(List<string> itemIDs, CatalogoDeSave catalogo)
    {
        var nomes = new List<string>();
        foreach (string id in itemIDs)
        {
            if (string.IsNullOrEmpty(id)) continue;
            Item item = catalogo != null ? catalogo.BuscarItem(id) : null;
            nomes.Add(item != null ? item.NomeExibicao : id);
        }
        if (nomes.Count <= 1) return string.Join("", nomes);
        return string.Join(", ", nomes.GetRange(0, nomes.Count - 1)) + " e " + nomes[nomes.Count - 1];
    }

    private static string Valores(int povo, int estado, int ouro)
    {
        var partes = new List<string>();
        if (povo != 0) partes.Add($"Povo {Sinal(povo)}");
        if (estado != 0) partes.Add($"Estado {Sinal(estado)}");
        if (ouro != 0) partes.Add($"Ouro {Sinal(ouro)}");
        return string.Join(", ", partes);
    }

    private static string Sinal(int v) => v > 0 ? "+" + v : v.ToString();
}
