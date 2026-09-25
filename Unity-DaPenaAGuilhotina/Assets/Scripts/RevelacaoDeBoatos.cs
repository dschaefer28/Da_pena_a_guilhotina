using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fato x Boato, a cobrança: coloque em qualquer objeto da cena de ABERTURA da fase seguinte (ex: Fase3).
/// Se o jogador publicou boatos ou calúnias, toca uma cutscene de legendas expondo cada mentira e aplica as
/// penalidades definidas na ReceitaDeCaso. Sem boatos pendentes, não faz nada.
/// </summary>
public class RevelacaoDeBoatos : MonoBehaviour
{
    [TextArea(2, 4)]
    public string introducao = "Alguns dias depois, a verdade chega às ruas de Paris.";
    [TextArea(2, 4)]
    [Tooltip("Usado quando a versão do panfleto não tem texto de revelação próprio. {0} = título do caso.")]
    public string textoPadrao = "O panfleto sobre o {0} foi desmentido. A tipografia perdeu a confiança de quem o leu.";
    [Min(0f)] public float duracaoPorLinha = 5f;

    private void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.revelacoesPendentes.Count == 0) return;

        var linhas = new List<CutsceneLegendas.Linha>();
        if (!string.IsNullOrWhiteSpace(introducao))
            linhas.Add(new CutsceneLegendas.Linha { texto = introducao, duracao = duracaoPorLinha });

        // Aplica antes da cutscene: se a cena trocar no meio dela, a penalidade não se perde.
        foreach (var revelacao in gm.ConsumirRevelacoes())
        {
            gm.AplicarImpactoPanfleto(revelacao.povo, revelacao.estado, 0);

            string titulo = revelacao.caso != null ? (revelacao.caso.caseTitle ?? revelacao.caso.name).Trim() : "caso";
            string texto = string.IsNullOrWhiteSpace(revelacao.texto) ? string.Format(textoPadrao, titulo) : revelacao.texto;
            linhas.Add(new CutsceneLegendas.Linha { texto = texto, duracao = duracaoPorLinha });
        }

        if (CutsceneLegendas.Instance != null) CutsceneLegendas.Instance.Tocar(linhas);
        else foreach (var l in linhas) AvisoNaTela.Mostrar(l.texto);
    }
}
