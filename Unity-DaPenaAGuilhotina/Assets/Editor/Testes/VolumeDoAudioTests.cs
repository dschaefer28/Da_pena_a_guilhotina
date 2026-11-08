using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Volumes separados de Música e Efeitos (VolumeDoAudio). Os banks usados são os reais de StreamingAssets: se quem
/// cuida do áudio renomear o bank Musicas ou colocar um som nele por engano, o teste avisa.
/// </summary>
public class VolumeDoAudioTests
{
    private readonly Dictionary<string, float?> prefsGuardadas = new Dictionary<string, float?>();
    private static readonly string[] Chaves = { VolumeDoAudio.ChaveMusica, VolumeDoAudio.ChaveEfeitos, VolumeDoAudio.ChaveAntiga };

    [SetUp]
    public void GuardarOpcoes()
    {
        foreach (string chave in Chaves)
            prefsGuardadas[chave] = PlayerPrefs.HasKey(chave) ? PlayerPrefs.GetFloat(chave) : (float?)null;
    }

    [TearDown]
    public void DevolverOpcoes()
    {
        foreach (var par in prefsGuardadas)
        {
            if (par.Value.HasValue) PlayerPrefs.SetFloat(par.Key, par.Value.Value);
            else PlayerPrefs.DeleteKey(par.Key);
        }
        PlayerPrefs.Save();
    }

    [Test]
    public void Regra_SoOBankMusicasEhMusica()
    {
        Assert.IsTrue(VolumeDoAudio.EhMusica(new[] { "bank:/Musicas" }));
        Assert.IsFalse(VolumeDoAudio.EhMusica(new[] { "bank:/Musicas", "bank:/Sons", "bank:/Master" }), "som que também está em Sons é efeito");
        Assert.IsFalse(VolumeDoAudio.EhMusica(new[] { "bank:/Sons" }));
        Assert.IsFalse(VolumeDoAudio.EhMusica(new[] { "bank:/Dialogo" }));
    }

    [Test]
    public void BanksReais_MusicaSoAsTrilhas_AmbienteEVozesSaoEfeitos()
    {
        FMOD.Studio.System sistema;
        Assert.AreEqual(FMOD.RESULT.OK, FMOD.Studio.System.create(out sistema));
        try
        {
            sistema.getCoreSystem(out FMOD.System nucleo);
            nucleo.setOutput(FMOD.OUTPUTTYPE.NOSOUND);
            Assert.AreEqual(FMOD.RESULT.OK, sistema.initialize(64, FMOD.Studio.INITFLAGS.NORMAL, FMOD.INITFLAGS.NORMAL, System.IntPtr.Zero));
            foreach (string arquivo in System.IO.Directory.GetFiles(Application.streamingAssetsPath, "*.bank"))
                Assert.AreEqual(FMOD.RESULT.OK, sistema.loadBankFile(arquivo, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out FMOD.Studio.Bank _), arquivo);

            var musicas = new List<FMOD.Studio.EventDescription>();
            var efeitos = new List<FMOD.Studio.EventDescription>();
            Assert.IsTrue(VolumeDoAudio.Classificar(sistema, musicas, efeitos));

            var nomesMusicas = Caminhos(musicas);
            var nomesEfeitos = Caminhos(efeitos);
            Assert.IsNotEmpty(nomesMusicas, "nenhuma música: o bank \"Musicas\" mudou de nome? (VolumeDoAudio.BankDeMusicas)");
            foreach (string m in nomesMusicas)
                StringAssert.Contains("musica", m.ToLowerInvariant(), "só trilha no volume da música");
            CollectionAssert.Contains(nomesEfeitos, "event:/crowdAmbience", "ambiente de multidão é efeito");
            CollectionAssert.Contains(nomesEfeitos, "event:/clique1");
            CollectionAssert.Contains(nomesEfeitos, "event:/P1Bravo", "vozes do Dialogo são efeito");
        }
        finally
        {
            sistema.release();
        }
    }

    private static List<string> Caminhos(List<FMOD.Studio.EventDescription> eventos)
    {
        var lista = new List<string>();
        foreach (var ev in eventos) { ev.getPath(out string caminho); lista.Add(caminho); }
        return lista;
    }

    [Test]
    public void SemAjusteNovo_OsDoisComecamNoVolumeAntigo()
    {
        PlayerPrefs.DeleteKey(VolumeDoAudio.ChaveMusica);
        PlayerPrefs.DeleteKey(VolumeDoAudio.ChaveEfeitos);
        PlayerPrefs.SetFloat(VolumeDoAudio.ChaveAntiga, 0.4f);
        Assert.AreEqual(0.4f, VolumeDoAudio.Musica, 1e-4f);
        Assert.AreEqual(0.4f, VolumeDoAudio.Efeitos, 1e-4f);

        VolumeDoAudio.DefinirMusica(0.2f);
        Assert.AreEqual(0.2f, VolumeDoAudio.Musica, 1e-4f);
        Assert.AreEqual(0.4f, VolumeDoAudio.Efeitos, 1e-4f, "mexer na música não mexe nos efeitos");

        PlayerPrefs.DeleteKey(VolumeDoAudio.ChaveAntiga);
        PlayerPrefs.DeleteKey(VolumeDoAudio.ChaveEfeitos);
        Assert.AreEqual(1f, VolumeDoAudio.Efeitos, 1e-4f, "sem nada salvo, volume cheio");
    }

    [Test]
    public void Pause_TemSlidersDeMusicaEEfeitosLigados()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI.prefab");
        try
        {
            PauseMenu pause = raiz.GetComponentInChildren<PauseMenu>(true);
            Assert.IsNotNull(pause, "PauseMenu não está no UI.prefab.");
            ConferirSliders(pause.sliderMusica, pause.sliderEfeitos, pause,
                nameof(PauseMenu.OnMusicaSliderChanged), nameof(PauseMenu.OnEfeitosSliderChanged));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    [Test]
    public void MenuPrincipal_TemSlidersDeMusicaEEfeitosLigados()
    {
        const string caminho = "Assets/Scenes/menu principal.unity";
        Scene cena = SceneManager.GetSceneByPath(caminho);
        bool abriu = false;
        if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive); abriu = true; }
        try
        {
            MenuPrincipalManager menu = null;
            foreach (GameObject r in cena.GetRootGameObjects())
                if (menu == null) menu = r.GetComponentInChildren<MenuPrincipalManager>(true);
            Assert.IsNotNull(menu, "MenuPrincipalManager não está na cena do menu.");
            var so = new SerializedObject(menu);
            ConferirSliders(so.FindProperty("sliderMusica").objectReferenceValue as Slider,
                so.FindProperty("sliderEfeitos").objectReferenceValue as Slider, menu,
                nameof(MenuPrincipalManager.OnMusicaSliderChanged), nameof(MenuPrincipalManager.OnEfeitosSliderChanged));
        }
        finally
        {
            if (abriu) EditorSceneManager.CloseScene(cena, true);
        }
    }

    private static void ConferirSliders(Slider musica, Slider efeitos, Object dono, string metodoMusica, string metodoEfeitos)
    {
        Assert.IsNotNull(musica, "campo do slider de Música vazio");
        Assert.IsNotNull(efeitos, "campo do slider de Efeitos vazio");
        Assert.AreNotSame(musica, efeitos);
        Assert.AreEqual(new[] { metodoMusica }, Metodos(musica, dono), "slider de Música");
        Assert.AreEqual(new[] { metodoEfeitos }, Metodos(efeitos, dono), "slider de Efeitos");
    }

    private static string[] Metodos(Slider slider, Object dono)
    {
        var metodos = new List<string>();
        for (int i = 0; i < slider.onValueChanged.GetPersistentEventCount(); i++)
            if (slider.onValueChanged.GetPersistentTarget(i) == dono &&
                slider.onValueChanged.GetPersistentListenerState(i) != UnityEventCallState.Off)
                metodos.Add(slider.onValueChanged.GetPersistentMethodName(i));
        return metodos.ToArray();
    }
}
