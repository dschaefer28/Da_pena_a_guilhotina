using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Separa o slider único de Volume das telas de Opções (menu principal e pause do UI.prefab) em Música e Efeitos
/// (VolumeDoAudio): o slider antigo vira o de Música (rótulo MÚSICA), uma cópia dele vira o de Efeitos, e as três
/// linhas (Música, Efeitos, Velocidade do texto) são redistribuídas no espaço entre o título e o Voltar.
/// Menu: Ferramentas > Áudio > Separar volume de Música e Efeitos. Reexecutável.
/// </summary>
public static class VolumeSeparadoSetupTool
{
    private const string CaminhoPrefabUI = "Assets/Prefabs/UI.prefab";
    private const string CenaMenu = "Assets/Scenes/menu principal.unity";
    private const float Passo = 120f;       // distância entre uma linha e a próxima
    private const float RotuloAcima = 50f;  // o rótulo fica acima do slider, como já estava
    private const float SubirNaPrimeiraVez = 30f;

    [MenuItem("Ferramentas/Áudio/Separar volume de Música e Efeitos")]
    public static void Aplicar()
    {
        AplicarNoPause();
        AplicarNoMenu();
        Debug.Log("[VolumeSeparadoSetupTool] Opções do pause (UI.prefab) e do menu principal com sliders de Música e Efeitos.");
    }

    private static void AplicarNoPause()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefabUI);
        try
        {
            PauseMenu pause = raiz.GetComponentInChildren<PauseMenu>(true);
            if (pause == null || pause.sliderMusica == null || pause.sliderVelocidadeTexto == null)
            {
                Debug.LogError("[VolumeSeparadoSetupTool] PauseMenu (com os sliders de volume e de velocidade) não encontrado no UI.prefab.");
                return;
            }
            pause.sliderEfeitos = Separar(pause.sliderMusica, pause.sliderEfeitos, pause.sliderVelocidadeTexto,
                pause.OnMusicaSliderChanged, pause.OnEfeitosSliderChanged);
            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefabUI);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void AplicarNoMenu()
    {
        Scene cena = SceneManager.GetSceneByPath(CenaMenu);
        bool abriu = false;
        if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Additive); abriu = true; }
        try
        {
            MenuPrincipalManager menu = null;
            foreach (GameObject r in cena.GetRootGameObjects())
                if (menu == null) menu = r.GetComponentInChildren<MenuPrincipalManager>(true);
            var so = menu != null ? new SerializedObject(menu) : null;
            var musica = so?.FindProperty("sliderMusica").objectReferenceValue as Slider;
            var velocidade = so?.FindProperty("sliderVelocidadeTexto").objectReferenceValue as Slider;
            if (musica == null || velocidade == null)
            {
                Debug.LogError("[VolumeSeparadoSetupTool] MenuPrincipalManager (com os sliders de volume e de velocidade) não encontrado no menu principal.");
                return;
            }
            SerializedProperty campoEfeitos = so.FindProperty("sliderEfeitos");
            campoEfeitos.objectReferenceValue = Separar(musica, campoEfeitos.objectReferenceValue as Slider, velocidade,
                menu.OnMusicaSliderChanged, menu.OnEfeitosSliderChanged);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
        }
        finally
        {
            if (abriu) EditorSceneManager.CloseScene(cena, true);
        }
    }

    /// <summary>Monta as três linhas e devolve o slider de Efeitos (criado na primeira vez, reaproveitado depois).</summary>
    private static Slider Separar(Slider musica, Slider efeitos, Slider velocidade,
        UnityAction<float> aoMudarMusica, UnityAction<float> aoMudarEfeitos)
    {
        Transform painel = musica.transform.parent;
        TMP_Text rotuloMusica = Rotulo(painel, "MusicaTexto", "VolumeTexto");
        TMP_Text rotuloVelocidade = Rotulo(painel, "VelocidadeTexto");
        TMP_Text rotuloEfeitos = Rotulo(painel, "EfeitosTexto");

        // Na primeira vez o bloco sobe um pouco para caber a linha nova; depois a posição da Música é mantida.
        bool primeiraVez = efeitos == null;
        float topo = ((RectTransform)musica.transform).anchoredPosition.y + (primeiraVez ? SubirNaPrimeiraVez : 0f);

        if (efeitos == null)
        {
            efeitos = Object.Instantiate(musica, painel);
            efeitos.transform.SetSiblingIndex(musica.transform.GetSiblingIndex() + 1);
        }
        if (rotuloEfeitos == null && rotuloMusica != null)
        {
            rotuloEfeitos = Object.Instantiate(rotuloMusica, painel);
            rotuloEfeitos.transform.SetSiblingIndex(rotuloMusica.transform.GetSiblingIndex() + 1);
        }

        musica.name = "SliderMusica";
        efeitos.name = "SliderEfeitos";
        if (rotuloMusica != null) { rotuloMusica.name = "MusicaTexto"; rotuloMusica.text = "MÚSICA"; }
        if (rotuloEfeitos != null) { rotuloEfeitos.name = "EfeitosTexto"; rotuloEfeitos.text = "EFEITOS"; }

        Ligar(musica, aoMudarMusica);
        Ligar(efeitos, aoMudarEfeitos);

        Posicionar(musica, rotuloMusica, topo);
        Posicionar(efeitos, rotuloEfeitos, topo - Passo);
        Posicionar(velocidade, rotuloVelocidade, topo - 2f * Passo);
        return efeitos;
    }

    private static TMP_Text Rotulo(Transform painel, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            Transform t = painel.Find(nome);
            if (t != null && t.GetComponent<TMP_Text>() != null) return t.GetComponent<TMP_Text>();
        }
        return null;
    }

    // Tira o que o slider chamava no menu/pause (o antigo OnVolumeSliderChanged veio junto na cópia) e liga o método certo.
    private static void Ligar(Slider slider, UnityAction<float> acao)
    {
        for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
            if (slider.onValueChanged.GetPersistentTarget(i) == (Object)acao.Target)
                UnityEventTools.RemovePersistentListener(slider.onValueChanged, i);
        UnityEventTools.AddPersistentListener(slider.onValueChanged, acao);
    }

    private static void Posicionar(Slider slider, TMP_Text rotulo, float y)
    {
        var rt = (RectTransform)slider.transform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);
        if (rotulo == null) return;
        rotulo.rectTransform.anchoredPosition = new Vector2(rotulo.rectTransform.anchoredPosition.x, y + RotuloAcima);
    }
}
