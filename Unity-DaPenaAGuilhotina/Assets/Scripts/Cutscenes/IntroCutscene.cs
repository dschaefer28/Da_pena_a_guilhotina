using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cutscene de abertura: toca uma única vez (PlayerPrefs) na primeira carga da cena Jogo, antes da Fase 1.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    public const string Chave = "intro_cutscene_vista";
    private const string CenaAlvo = "Jogo";

    [Tooltip("Marque para a cutscene tocar de novo a cada teste (ignora o PlayerPrefs).")]
    public bool forcarRepetir = false;

    [Tooltip("Um bloco por item. Duração 0 = tempo calculado pelas palavras (CutsceneLegendas).")]
    public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>
    {
        new CutsceneLegendas.Linha { texto = "Paris, 1786. Sob o poder da Coroa, a justiça pesa de forma diferente sobre ricos e pobres.", duracao = 0f },
        new CutsceneLegendas.Linha { texto = "Você é Julien Valois, um jovem advogado que trabalha sob a orientação de Charles Dupaty.", duracao = 0f },
    };

    private void Start()
    {
        // O prefab UI existe em todas as cenas.
        if (SceneManager.GetActiveScene().name != CenaAlvo) return;
        if (!forcarRepetir && PlayerPrefs.GetInt(Chave, 0) == 1) return;
        if (CutsceneLegendas.Instance == null || CutsceneLegendas.EmExibicao) return;

        PlayerPrefs.SetInt(Chave, 1);
        PlayerPrefs.Save();
        // Sem fade de entrada: evita o escritório "piscar" antes de escurecer.
        CutsceneLegendas.Instance.TocarTransicao(legendas, fadeDeEntradaInstantaneo: true);
    }
}
