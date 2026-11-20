using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cutscene de passagem para a Fase 2, tocada uma vez (PlayerPrefs) quando o jogador volta do porão com
/// o primeiro panfleto e o tutorial chega à etapaGatilho. Fica no prefab UI.
/// </summary>
public class Fase1Desfecho : MonoBehaviour
{
    public const string Chave = "fase1_cutscene_vista";

    [Tooltip("Marque para a cutscene tocar de novo a cada teste (ignora o PlayerPrefs).")]
    public bool forcarRepetir = false;

    [Tooltip("Etapa do tutorial em que o jogador está ao voltar do porão com o panfleto. A cutscene toca quando " +
             "o tutorial chega nela (ou já terminou).")]
    public string etapaGatilho = "mesa_de_casos";

    [Tooltip("Um bloco por item. Duração 0 = tempo calculado pelas palavras (CutsceneLegendas).")]
    public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>
    {
        new CutsceneLegendas.Linha { texto = "Nos anos seguintes, colheitas ruins e invernos rigorosos agravam a miséria na França.", duracao = 0f },
        new CutsceneLegendas.Linha { texto = "O pão fica mais caro. Para muitas famílias, sobreviver se torna uma luta diária.", duracao = 0f },
        new CutsceneLegendas.Linha { texto = "Enquanto a corte preserva seu luxo, a fome e os impostos alimentam a revolta.", duracao = 0f },
        new CutsceneLegendas.Linha { texto = "Em 1789, a crise força a convocação dos Estados Gerais. A população exige ser ouvida.", duracao = 0f },
    };

    private void Start()
    {
        // Decide no Start, não no sceneLoaded: lá a cópia da cena antiga (Porão) ainda existe e seria
        // destruída no meio da cutscene.
        if (TutorialManager.Instance != null && TutorialManager.Instance.EtapaJaFoiAlcancada(etapaGatilho) && ChegouDoPorao())
            TentarTocar();
    }

    // Exige o primeiro panfleto: evita tocar para quem pulou o tutorial num save antigo.
    private bool ChegouDoPorao()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.inventarioSalvo == null) return false;
        // Caso já escolhido = fora da Fase 1 (panfletos de casos também contêm "panfleto" no itemID).
        if (gm.casosJaSelecionados.Count > 0) return false;
        foreach (var item in gm.inventarioSalvo)
            if (item != null && !string.IsNullOrEmpty(item.itemID) && item.itemID.ToLowerInvariant().Contains("panfleto")) return true;
        return false;
    }

    private void TentarTocar()
    {
        if (!forcarRepetir && PlayerPrefs.GetInt(Chave, 0) == 1) return;
        if (CutsceneLegendas.Instance == null || CutsceneLegendas.EmExibicao) return;

        PlayerPrefs.SetInt(Chave, 1);
        PlayerPrefs.Save();
        CutsceneLegendas.Instance.TocarTransicao(legendas);
    }
}
