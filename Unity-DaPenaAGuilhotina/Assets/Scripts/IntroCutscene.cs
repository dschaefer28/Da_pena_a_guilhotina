using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cutscene introdutória (documento de tarefas): toca uma vez, antes de qualquer outra coisa na
/// primeira vez que a cena Jogo carrega, situando o jogador no tempo e espaço da história. Mesmo
/// padrão do Fase1Desfecho (tela preta com legendas, PlayerPrefs para tocar uma vez só); fica ao lado
/// dele no prefab UI. O texto das legendas é editável no Inspector.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    private const string Chave = "intro_cutscene_vista";
    private const string CenaAlvo = "Jogo";

    [Tooltip("Marque para a cutscene tocar de novo a cada teste (ignora o PlayerPrefs).")]
    public bool forcarRepetir = false;

    public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>
    {
        new CutsceneLegendas.Linha { texto = "Paris, 1780. Nove anos antes da Queda da Bastilha.", duracao = 4f },
        new CutsceneLegendas.Linha { texto = "Nas ruas, o descontentamento cresce em silêncio — e a palavra impressa já circula mais rápido que os editos do rei.", duracao = 5f },
        new CutsceneLegendas.Linha { texto = "Numa pequena tipografia, um novo aprendiz está prestes a descobrir o poder de uma página bem escrita.", duracao = 5f },
    };

    private void Start()
    {
        // Só faz sentido na cena Jogo (este objeto existe em todas as cenas via prefab UI).
        if (SceneManager.GetActiveScene().name != CenaAlvo) return;
        if (!forcarRepetir && PlayerPrefs.GetInt(Chave, 0) == 1) return;
        if (CutsceneLegendas.Instance == null || CutsceneLegendas.EmExibicao) return;

        PlayerPrefs.SetInt(Chave, 1);
        PlayerPrefs.Save();
        CutsceneLegendas.Instance.Tocar(legendas);
    }
}
