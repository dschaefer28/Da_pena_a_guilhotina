using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cutscene introdutória (documento de tarefas): toca uma vez, antes de qualquer outra coisa na
/// primeira vez que a cena Jogo carrega, situando o jogador no tempo e espaço da história; ao terminar,
/// começa a Fase 1 (o tutorial). Mesmo padrão do Fase1Desfecho (legendas de transição sobre tela preta,
/// PlayerPrefs para tocar uma vez só); fica ao lado dele no prefab UI. O texto das legendas é editável no
/// Inspector; os tempos ficam no CutsceneLegendas.
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
        // Só faz sentido na cena Jogo (este objeto existe em todas as cenas via prefab UI).
        if (SceneManager.GetActiveScene().name != CenaAlvo) return;
        if (!forcarRepetir && PlayerPrefs.GetInt(Chave, 0) == 1) return;
        if (CutsceneLegendas.Instance == null || CutsceneLegendas.EmExibicao) return;

        PlayerPrefs.SetInt(Chave, 1);
        PlayerPrefs.Save();
        // Sem fade de entrada: é a primeira coisa da cena, não há cenário pra esmaecer (evita o
        // escritório "piscar" por trás do fade antes de escurecer).
        CutsceneLegendas.Instance.TocarTransicao(legendas, fadeDeEntradaInstantaneo: true);
    }
}
