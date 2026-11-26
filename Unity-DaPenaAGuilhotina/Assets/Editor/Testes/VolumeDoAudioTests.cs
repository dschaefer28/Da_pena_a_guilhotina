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
            CollectionAssert.Contains(nomesEfeitos, "event:/clique2", "som de clique dos botões é efeito");
            CollectionAssert.Contains(nomesEfeitos, "event:/Papel", "som de papel dos quadros da rua é efeito");
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

    [Test]
    public void TodosOsBotoesDePrefabsEMenu_TemSomDeClique1Configurado()
    {
        FMOD.GUID guidEsperado = FMOD.GUID.Parse(SomDeCliqueDoBotao.GuidPadrao);
        string[] prefabs =
        {
            "Assets/Prefabs/ButtonChoise.prefab",
            "Assets/Prefabs/CartaoPrefab.prefab",
            "Assets/Prefabs/MobileControlsUI.prefab",
            "Assets/Prefabs/UI_Inventory.prefab",
            "Assets/Prefabs/UI.prefab"
        };

        foreach (string caminho in prefabs)
        {
            var raiz = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            Assert.IsNotNull(raiz, caminho);
            foreach (Button b in raiz.GetComponentsInChildren<Button>(true))
            {
                bool ehAreaDeDialogo = (b.name == "ButtonAvancar" || b.name == "TalkTextScrollView")
                                       && b.transform.parent != null && b.transform.parent.name == "DialogePanel";
                var som = b.GetComponent<SomDeCliqueDoBotao>();
                if (ehAreaDeDialogo)
                {
                    Assert.IsNull(som, $"Área invisível {b.name} em {caminho} não deve tocar som de botão.");
                }
                else
                {
                    Assert.IsNotNull(som, $"Botão {b.name} em {caminho} está sem SomDeCliqueDoBotao.");
                    Assert.IsTrue(som.enabled, $"SomDeCliqueDoBotao desativado em {b.name} ({caminho}).");
                    Assert.AreEqual(guidEsperado, som.EventoClique.Guid, $"GUID incorreto em {b.name} ({caminho}).");
                }
            }
        }

        const string cenaMenu = "Assets/Scenes/menu principal.unity";
        Scene cena = SceneManager.GetSceneByPath(cenaMenu);
        bool abriu = false;
        if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(cenaMenu, OpenSceneMode.Additive); abriu = true; }
        try
        {
            int contados = 0;
            foreach (GameObject r in cena.GetRootGameObjects())
            {
                foreach (Button b in r.GetComponentsInChildren<Button>(true))
                {
                    contados++;
                    var som = b.GetComponent<SomDeCliqueDoBotao>();
                    Assert.IsNotNull(som, $"Botão {b.name} no menu principal está sem SomDeCliqueDoBotao.");
                    Assert.AreEqual(guidEsperado, som.EventoClique.Guid, $"GUID incorreto em {b.name} no menu principal.");
                }
            }
            Assert.GreaterOrEqual(contados, 31, "Quantidade esperada de botões no menu principal.");
        }
        finally
        {
            if (abriu) EditorSceneManager.CloseScene(cena, true);
        }
    }

    [Test]
    public void QuadrosDeAvisoDaRua_TemSomDePapelConfigurado_EMesasNao()
    {
        FMOD.GUID guidPapel = FMOD.GUID.Parse("{d8aab54c-b996-4c55-bfc5-3a7b9cb45522}");
        string[] cenasDeRua =
        {
            "Assets/Scenes/Fase2.unity",
            "Assets/Scenes/Fase3.unity",
            "Assets/Scenes/Fase4.unity"
        };

        foreach (string caminho in cenasDeRua)
        {
            Scene cena = SceneManager.GetSceneByPath(caminho);
            bool abriu = false;
            if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive); abriu = true; }
            try
            {
                int quadros = 0;
                int mesas = 0;
                foreach (GameObject r in cena.GetRootGameObjects())
                {
                    foreach (LootInteractable loot in r.GetComponentsInChildren<LootInteractable>(true))
                    {
                        var sr = loot.GetComponentInChildren<SpriteRenderer>(true);
                        bool ehQuadro = sr != null && sr.sprite != null && sr.sprite.name.ToLowerInvariant().Contains("quadro");
                        if (ehQuadro)
                        {
                            quadros++;
                            Assert.AreEqual(guidPapel, loot.somColeta.Guid, $"Quadro '{loot.name}' em {caminho} está sem o som event:/Papel.");
                        }
                        else
                        {
                            mesas++;
                            Assert.IsTrue(loot.somColeta.IsNull, $"Objeto '{loot.name}' (não-quadro) em {caminho} não deve ter som de papel.");
                        }
                    }
                }
                Assert.AreEqual(2, quadros, $"Esperados 2 quadros de aviso na rua em {caminho}.");
                Assert.GreaterOrEqual(mesas, 1, $"Esperada pelo menos 1 mesa/objeto não-quadro em {caminho}.");
            }
            finally
            {
                if (abriu) EditorSceneManager.CloseScene(cena, true);
            }
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
