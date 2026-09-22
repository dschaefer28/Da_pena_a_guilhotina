using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Desfecho da Fase 1 (documento de tarefas): depois que o jogador imprime o primeiro panfleto e volta
/// ao escritório, toca a cutscene (tela preta com legendas) explicando as consequências da publicação.
/// Roda uma vez só (PlayerPrefs). Fica no prefab UI; o texto das legendas é editável no Inspector.
/// O gatilho é o fim do tutorial (TutorialManager.OnTutorialConcluido), que acontece ao carregar a
/// cena Jogo vindo do porão com o panfleto.
/// </summary>
public class Fase1Desfecho : MonoBehaviour
{
    private const string Chave = "fase1_cutscene_vista";

    [Tooltip("Marque para a cutscene tocar de novo a cada teste (ignora o PlayerPrefs).")]
    public bool forcarRepetir = false;

    public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>
    {
        new CutsceneLegendas.Linha { texto = "Paris, 1780. O panfleto sobre o caso Bradier circula pelas ruas antes do amanhecer.", duracao = 5f },
        new CutsceneLegendas.Linha { texto = "Nas tavernas, o povo lê em voz alta. Nos salões, o Estado toma nota do nome da tipografia.", duracao = 5f },
        new CutsceneLegendas.Linha { texto = "Cada página impressa agora tem um preço. E alguém sempre paga.", duracao = 4f },
    };

    private void Start()
    {
        // Só a instância da cena recém-carregada decide: o fim do tutorial dispara no sceneLoaded, quando a
        // cópia deste objeto na cena antiga (Porão) ainda existe — se ela tocasse a cutscene, seria destruída
        // no meio da rotina. Aqui, no Start da cena nova, o estado já está completo.
        if (TutorialManager.Instance != null && TutorialManager.Instance.TutorialConcluido && ChegouDoPorao())
            TentarTocar();
    }

    // Só faz sentido depois do primeiro panfleto: evita tocar em quem pulou o tutorial num save antigo.
    private bool ChegouDoPorao()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.inventarioSalvo == null) return false;
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
        CutsceneLegendas.Instance.Tocar(legendas);
    }
}
