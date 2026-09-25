using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fecha a fase quando o jogador volta ao escritório com os casos exigidos concluídos (GameManager.casosPorFase):
/// toca a cutscene final da fase (tela preta com legendas), passa o GameManager para a fase seguinte e salva.
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

        int faseEncerrada = gm.faseAtual;
        // Avança já, antes da cutscene: se a cena trocar no meio dela, a fase não fica pendente.
        gm.AvancarFase();

        List<CutsceneLegendas.Linha> legendas = LegendasDa(faseEncerrada);
        if (legendas != null && legendas.Count > 0 && CutsceneLegendas.Instance != null)
            CutsceneLegendas.Instance.Tocar(legendas);

        StartCoroutine(SalvarNoProximoFrame());
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
