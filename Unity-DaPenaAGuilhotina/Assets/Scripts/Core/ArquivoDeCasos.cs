using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Arquivo dos casos resolvidos, lido pelo painel "Casos resolvidos" do menu principal: o jogador relê o caso de cada
/// fase e o desfecho dele sem precisar rejogar. Fica num arquivo separado do save
/// (Application.persistentDataPath/arquivo_de_casos.json) porque sobrevive ao Novo Jogo: cada partida é guardada à
/// parte (até <see cref="MaximoDePartidas"/>, as mais antigas saem primeiro).
///
/// Cada partida é um retrato dos dados do save (DadosDeSave), atualizado quando o jogo salva, quando um caso é concluído
/// na prensa e quando a fase fecha. O desfecho só aparece depois que o jogo o revelou: a revelação de um boato no fim
/// da fase e o veredito do tribunal no fim do jogo. Antes disso, o painel não diz nada sobre a verdade das pistas.
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
        public List<string> acreditou = new List<string>(); // itemIDs das pistas escolhidas no quadro de dedução

        // Revelação do boato publicado (guardada enquanto estava pendente: depois do fim da fase o save não a tem mais).
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

    /// <summary>Atualiza a partida em andamento a partir do GameManager (caso concluído, fim de fase).</summary>
    public static void Registrar(GameManager gm)
    {
        if (gm == null || !Application.isPlaying) return;
        Registrar(SistemaDeSave.CapturarDados(gm, string.Empty, 0));
    }

    /// <summary>Atualiza a partida a partir dos dados que acabaram de ir para o save (inclui o fim de jogo).</summary>
    public static void Registrar(SistemaDeSave.DadosDeSave dados)
    {
        // Só no jogo de verdade: os testes de EditMode montam GameManagers e saves falsos.
        if (dados == null || !Application.isPlaying || string.IsNullOrEmpty(dados.idDaPartida)) return;
        if (dados.casosConcluidos == null || dados.casosConcluidos.Count == 0) return; // nada para mostrar ainda

        try
        {
            Dados arquivo = Ler() ?? new Dados();
            int indice = arquivo.partidas.FindIndex(p => p != null && p.id == dados.idDaPartida);
            PartidaArquivada anterior = indice >= 0 ? arquivo.partidas[indice] : null;
            PartidaArquivada nova = Montar(dados, anterior, CatalogoDeSave.Instancia);

            if (indice >= 0) arquivo.partidas.RemoveAt(indice);
            arquivo.partidas.Add(nova); // a mais recente fica no fim
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

    /// <summary>Monta o retrato da partida (sem tocar em disco). A revelação de um boato vem do save enquanto está
    /// pendente; depois do fim da fase, fica a que já estava no arquivo ou, em último caso, a da receita do caso.</summary>
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
                // Arquivo começou depois do fim da fase (ou save antigo): usa o que a receita define para essa versão.
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

    /// <summary>Partidas guardadas, da mais antiga para a mais recente (lista vazia se não houver arquivo).</summary>
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

    /// <summary>Rótulo da partida: "Partida 2 de 3 · iniciada em 07/11/2026 14:30 · em andamento".</summary>
    public static string RotuloDaPartida(PartidaArquivada partida, int numero, int total)
    {
        string situacao = partida.concluida ? "concluída" : "em andamento";
        return $"Partida {numero} de {total} · iniciada em {partida.inicio} · {situacao}";
    }

    /// <summary>Texto completo do caso (rich text do TextMeshPro).</summary>
    public static string TextoDoCaso(PartidaArquivada partida, CasoArquivado arquivado, CatalogoDeSave catalogo)
    {
        CaseData caso = catalogo != null ? catalogo.BuscarCaso(arquivado.caso) : null;
        int fase = caso != null ? caso.fase : 0;
        var t = new StringBuilder();

        t.Append("<size=125%>").Append(TituloDo(caso, arquivado.caso)).Append("</size>\n");
        if (fase > 0) t.Append("<i>").Append(NomeDaFase(fase)).Append("</i>\n");

        if (caso != null && !string.IsNullOrWhiteSpace(caso.caseDescription))
            Secao(t, "O caso", caso.caseDescription.Trim());
        if (caso != null && !string.IsNullOrWhiteSpace(caso.objectiveText))
            Secao(t, "Objetivo", caso.objectiveText.Trim());

        Secao(t, "Sua investigação", Investigacao(arquivado, caso, catalogo));
        Secao(t, "Desfecho", Desfecho(partida, arquivado, caso, fase));
        return t.ToString();
    }

    private static void Secao(StringBuilder t, string titulo, string corpo)
    {
        if (string.IsNullOrWhiteSpace(corpo)) return;
        // A fonte não tem negrito: o título da seção se destaca pela cor e pelas maiúsculas.
        t.Append("\n<color=#6B2E14><size=90%>").Append(titulo.ToUpperInvariant()).Append("</size></color>\n").Append(corpo).Append('\n');
    }

    private static string Investigacao(CasoArquivado arquivado, CaseData caso, CatalogoDeSave catalogo)
    {
        var t = new StringBuilder();
        if (arquivado.acreditou != null && arquivado.acreditou.Count > 0)
            t.Append("No quadro de dedução, você acreditou em: ").Append(Nomes(arquivado.acreditou, catalogo)).Append(".\n");

        SistemaDeSave.PanfletoSalvo p = arquivado.temPanfleto ? arquivado.panfleto : null;
        if (p == null)
        {
            t.Append("O panfleto do caso saiu da prensa.");
            return t.ToString();
        }

        if (p.pistas != null && p.pistas.Count > 0)
            t.Append("Pistas impressas no panfleto: ").Append(Nomes(p.pistas, catalogo)).Append(".\n");
        if (p.suportes != null && p.suportes.Count > 0)
            t.Append("Documentos de apoio: ").Append(Nomes(p.suportes, catalogo)).Append(".\n");
        if (LinhasEditoriais.Escolhivel(p.linha))
            t.Append("Linha editorial: ").Append(LinhasEditoriais.Rotulo(p.linha, caso != null ? caso.receitaDoPanfleto : null)).Append(".\n");
        if (p.aplicadoConhecido)
            t.Append("Efeito da publicação: ").Append(Valores(p.povoAplicado, p.estadoAplicado, p.ouroAplicado)).Append('.');
        return t.ToString().TrimEnd('\n');
    }

    private static string Desfecho(PartidaArquivada partida, CasoArquivado arquivado, CaseData caso, int fase)
    {
        if (fase >= GameManager.UltimaFase)
        {
            if (!partida.concluida) return "O julgamento ainda não aconteceu.";
            string titulo = string.IsNullOrWhiteSpace(partida.desfechoTitulo) ? "Fim do jogo" : partida.desfechoTitulo.Trim();
            return $"{titulo}. No tribunal, o réu foi {(partida.reuAbsolvido ? "absolvido" : "condenado")}.";
        }

        bool faseEncerrada = partida.concluida || (fase > 0 && partida.faseAtual > fase);
        if (!faseEncerrada) return "O que o panfleto vai provocar só será conhecido no fim desta fase.";
        if (!arquivado.temRevelacao) return "Nenhuma informação do panfleto foi desmentida depois da publicação.";

        string texto = string.IsNullOrWhiteSpace(arquivado.textoRevelacao)
            ? $"Parte do que o panfleto de \"{TituloDo(caso, arquivado.caso)}\" afirmava foi desmentida nas ruas."
            : arquivado.textoRevelacao.Trim();
        string perda = Valores(arquivado.revelacaoPovo, arquivado.revelacaoEstado, 0);
        return string.IsNullOrEmpty(perda) ? texto : $"{texto}\nConsequência: {perda}.";
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
