using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fecha a fase quando o jogador volta ao escritório com os casos exigidos concluídos (GameManager.casosPorFase):
/// expõe os boatos publicados na fase (penalidades), cobra as despesas, passa o GameManager para a fase seguinte
/// (GameManager.EncerrarFase), toca a cutscene final da fase (tela preta com legendas) e salva.
/// A partir daí a Mesa de Casos mostra os casos da nova fase e a porta leva à cena do caso aceito.
///
/// Fica no prefab UI, ao lado do Fase1Desfecho, e só age na cena do escritório. A Fase 1 não tem legendas aqui
/// porque o Fase1Desfecho já toca a cutscene dela; a Fase 4 termina no tribunal, não no escritório.
/// </summary>
public class FimDeFase : MonoBehaviour
{
    [Serializable]
    public class CutsceneDaFase
    {
        [Range(1, GameManager.UltimaFase)] public int fase;
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
            fase = 2,
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "Paris, 1789. A Bastilha caiu, e os panfletos da tipografia correm de mão em mão.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "O nome da casa já é conhecido nas ruas. E nos gabinetes.", duracao = 4f },
            }
        },
        new CutsceneDaFase
        {
            fase = 3,
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "1793. O Terror governa a França, e o Tribunal Revolucionário não descansa.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "Tudo o que foi impresso até aqui será lembrado. Por uns, como coragem. Por outros, como traição.", duracao = 5f },
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

        var legendas = new List<CutsceneLegendas.Linha>();
        List<CutsceneLegendas.Linha> daFase = LegendasDa(fim.fase);
        if (daFase != null) legendas.AddRange(daFase);

        if (fim.revelacoes.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(introducaoRevelacoes))
                legendas.Add(new CutsceneLegendas.Linha { texto = introducaoRevelacoes, duracao = 4f });
            foreach (GameManager.RevelacaoPendente revelacao in fim.revelacoes)
                legendas.Add(new CutsceneLegendas.Linha { texto = TextoDa(revelacao), duracao = 5f });
        }

        if (fim.despesa > 0)
        {
            string texto = string.Format(textoDespesas, fim.despesa);
            if (gm.Endividado) texto += " " + textoDivida;
            legendas.Add(new CutsceneLegendas.Linha { texto = texto, duracao = 5f });
        }

        if (legendas.Count > 0)
        {
            if (CutsceneLegendas.Instance != null) CutsceneLegendas.Instance.Tocar(legendas);
            else foreach (CutsceneLegendas.Linha linha in legendas) AvisoNaTela.Mostrar(linha.texto);
        }

        StartCoroutine(SalvarNoProximoFrame());
    }

    private string TextoDa(GameManager.RevelacaoPendente revelacao)
    {
        string texto = revelacao.texto;
        if (string.IsNullOrWhiteSpace(texto))
        {
            string titulo = revelacao.caso == null ? "caso"
                : (string.IsNullOrWhiteSpace(revelacao.caso.caseTitle) ? revelacao.caso.name : revelacao.caso.caseTitle).Trim();
            texto = string.Format(textoPadraoRevelacao, titulo);
        }
        if (revelacao.linha == LinhaEditorial.Sensacionalista && !string.IsNullOrWhiteSpace(textoAgravamentoSensacionalista))
            texto = texto.TrimEnd() + " " + textoAgravamentoSensacionalista.Trim();
        return texto;
    }

    private List<CutsceneLegendas.Linha> LegendasDa(int fase)
    {
        foreach (CutsceneDaFase c in cutscenes)
            if (c != null && c.fase == fase) return c.legendas;
        return null;
    }

    // Espera um frame: o inventário da cena só é restaurado no Start dele, e o save lê o inventário da tela.
    private IEnumerator SalvarNoProximoFrame()
    {
        yield return null;
        SistemaDeSave.Salvar();
    }
}
