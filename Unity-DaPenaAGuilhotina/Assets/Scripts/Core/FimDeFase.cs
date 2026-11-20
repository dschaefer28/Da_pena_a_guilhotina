using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fecha a fase ao voltar ao escritório com os casos exigidos concluídos: expõe boatos, cobra despesas,
/// avança a fase (GameManager.EncerrarFase), toca a cutscene de transição e salva.
/// Fase 1 usa o Fase1Desfecho; a Fase 4 termina no tribunal.
/// </summary>
public class FimDeFase : MonoBehaviour
{
    [Serializable]
    public class CutsceneDaFase
    {
        [Tooltip("Fase que acabou de terminar: 2 = passagem da Fase 2 para a 3.")]
        [Range(1, GameManager.UltimaFase)] public int fase;
        [Tooltip("Um bloco por item. Duração 0 = tempo calculado pelas palavras (CutsceneLegendas).")]
        public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>();
    }

    [Tooltip("Cena onde a fase é encerrada (o escritório, para onde o jogador volta do porão).")]
    public string cenaDoEscritorio = "Jogo";

    [Header("Despesas (GameManager.despesasPorFase)")]
    [Tooltip("Legenda das despesas cobradas ao fim da fase. {0} = valor.")]
    public string textoDespesas = "Fim do período. Aluguel, papel e tinta custaram {0} moedas à tipografia.";
    [Tooltip("Acrescentado quando o dinheiro não bastou.")]
    public string textoDivida = "Sem dinheiro para tudo, você agora deve aos credores, e eles vão cobrar em tempo.";

    [Header("Fato x Boato: boatos publicados na fase são expostos")]
    [TextArea(2, 4)]
    public string introducaoRevelacoes = "Alguns dias depois, a verdade chega às ruas de Paris.";
    [TextArea(2, 4)]
    [Tooltip("Usado quando a versão do panfleto não tem texto de revelação próprio. {0} = título do caso.")]
    public string textoPadraoRevelacao = "O panfleto sobre o {0} foi desmentido. A tipografia perdeu a confiança de quem o leu.";
    [TextArea(2, 4)]
    [Tooltip("Acrescentado à revelação de um panfleto sensacionalista (a perda foi agravada pela linha editorial).")]
    public string textoAgravamentoSensacionalista = "O exagero da manchete fez o desmentido correr ainda mais depressa.";

    public List<CutsceneDaFase> cutscenes = new List<CutsceneDaFase>
    {
        new CutsceneDaFase
        {
            fase = 2, // A Queda e a Ordem
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "Em julho de 1789, a Bastilha cai. As ruas desafiam o poder absoluto do rei.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "A Declaração dos Direitos do Homem e do Cidadão proclama a liberdade e a igualdade perante a lei.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "Em 1791, uma Constituição limita o poder de Luís XVI. A monarquia passa a obedecer a novas regras.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "Mas a crise continua. Às portas de 1792, o futuro da monarquia permanece incerto.", duracao = 0f },
            }
        },
        new CutsceneDaFase
        {
            fase = 3, // O Sangue da República
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "A República nasce em 1792, mas a França permanece dividida. A revolução enfrenta novos inimigos.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "Em 1793, a revolta na Vendeia mergulha parte do país em uma guerra sangrenta.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "Em Paris, os jacobinos ampliam seu poder. A defesa da República passa a justificar a repressão.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "A suspeita invade as ruas. Uma denúncia pode transformar qualquer cidadão em inimigo da Revolução.", duracao = 0f },
                new CutsceneLegendas.Linha { texto = "O poder da Coroa caiu. Sob o Terror, sua próxima defesa pode colocar sua própria vida em risco.", duracao = 0f },
            }
        },
    };

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != cenaDoEscritorio) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.faseAtual >= GameManager.UltimaFase || !gm.FaseConcluida) return;

        // Aplica tudo já, antes da cutscene: se a cena trocar no meio dela, nada fica pendente.
        GameManager.EncerramentoDeFase fim = gm.EncerrarFase();

        // Ordem: revelações, despesas e por fim a passagem histórica para a fase nova.
        var legendas = new List<CutsceneLegendas.Linha>();
        if (fim.revelacoes.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(introducaoRevelacoes))
                legendas.Add(new CutsceneLegendas.Linha { texto = introducaoRevelacoes, duracao = 0f });
            foreach (GameManager.RevelacaoPendente revelacao in fim.revelacoes)
                legendas.Add(new CutsceneLegendas.Linha { texto = TextoDa(revelacao), duracao = 0f });
        }

        if (fim.despesa > 0)
        {
            string texto = string.Format(textoDespesas, fim.despesa);
            if (gm.Endividado) texto += " " + textoDivida;
            legendas.Add(new CutsceneLegendas.Linha { texto = texto, duracao = 0f });
        }

        List<CutsceneLegendas.Linha> daFase = LegendasDa(fim.fase);
        if (daFase != null) legendas.AddRange(daFase);

        revelacoesParaAvisar = fim.revelacoes;
        if (legendas.Count > 0 && CutsceneLegendas.Instance != null)
            CutsceneLegendas.Instance.TocarTransicao(legendas, MostrarDesmentidos);
        else
        {
            foreach (CutsceneLegendas.Linha linha in legendas) AvisoNaTela.Mostrar(linha.texto);
            MostrarDesmentidos();
        }

        // Pedida antes do save do próximo frame, para o Continuar não repetir a dica.
        if (fim.despesa > 0 && TutorialManager.Instance != null)
            TutorialManager.Instance.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_DESPESAS, TutorialManager.Instance.dicaDespesas);

        StartCoroutine(SalvarNoProximoFrame());
    }

    [Header("Aviso depois da cutscene")]
    [Tooltip("Aviso de cada panfleto desmentido, mostrado depois da cutscene enquanto a barra desce. {0} = título do caso, " +
             "{1} = o que mudou (ex.: \"a opinião do povo caiu 22 pontos\").")]
    public string textoAvisoDesmentido = "O panfleto sobre \"{0}\" foi desmentido nas ruas. Por causa disso, {1}.";

    private List<GameManager.RevelacaoPendente> revelacoesParaAvisar;

    // A HUD segura a perda até aqui; ExibirDesmentido libera a barra.
    private void MostrarDesmentidos()
    {
        GameManager gm = GameManager.Instance;
        if (revelacoesParaAvisar != null)
            foreach (GameManager.RevelacaoPendente r in revelacoesParaAvisar)
            {
                var partes = new List<string>();
                if (r.perdaAplicada.x != 0) partes.Add("a opinião do povo " + Mudanca(r.perdaAplicada.x));
                if (r.perdaAplicada.y != 0) partes.Add((partes.Count > 0 ? "a do Estado " : "a opinião do Estado ") + Mudanca(r.perdaAplicada.y));
                if (partes.Count == 0) continue;
                AvisoNaTela.Mostrar(string.Format(textoAvisoDesmentido, TituloDo(r), string.Join(" e ", partes)),
                                    null, GameManager.DuracaoDoAvisoDeDesmentido);
            }
        revelacoesParaAvisar = null;
        if (gm != null) gm.ExibirDesmentido();
    }

    // Saída de cena no meio da cutscene: não deixa a HUD segurando a perda.
    private void OnDestroy()
    {
        if (revelacoesParaAvisar != null && GameManager.Instance != null) GameManager.Instance.ExibirDesmentido();
    }

    private static string Mudanca(int valor)
    {
        int pontos = Mathf.Abs(valor);
        return $"{(valor < 0 ? "caiu" : "subiu")} {pontos} {(pontos == 1 ? "ponto" : "pontos")}";
    }

    private static string TituloDo(GameManager.RevelacaoPendente revelacao) =>
        revelacao.caso == null ? "caso"
            : (string.IsNullOrWhiteSpace(revelacao.caso.caseTitle) ? revelacao.caso.name : revelacao.caso.caseTitle).Trim();

    private string TextoDa(GameManager.RevelacaoPendente revelacao)
    {
        string texto = revelacao.texto;
        if (string.IsNullOrWhiteSpace(texto))
        {
            string titulo = revelacao.caso == null ? "caso"
                : (string.IsNullOrWhiteSpace(revelacao.caso.caseTitle) ? revelacao.caso.name : revelacao.caso.caseTitle).Trim();
            texto = string.Format(textoPadraoRevelacao, titulo);
        }
        // Só com agravamento > 0 no caso (padrão 0%: o sensacionalista já paga na publicação).
        ReceitaDeCaso receita = revelacao.caso != null ? revelacao.caso.receitaDoPanfleto : null;
        bool agravada = receita != null && receita.linhaEditorial != null && receita.linhaEditorial.agravamentoPercentual > 0;
        if (revelacao.linha == LinhaEditorial.Sensacionalista && agravada && !string.IsNullOrWhiteSpace(textoAgravamentoSensacionalista))
            texto = texto.TrimEnd() + " " + textoAgravamentoSensacionalista.Trim();
        return texto;
    }

    private List<CutsceneLegendas.Linha> LegendasDa(int fase)
    {
        foreach (CutsceneDaFase c in cutscenes)
            if (c != null && c.fase == fase) return c.legendas;
        return null;
    }

    // Espera um frame: o save lê o inventário da tela, restaurado só no Start dele.
    private IEnumerator SalvarNoProximoFrame()
    {
        yield return null;
        SistemaDeSave.Salvar();
    }
}
