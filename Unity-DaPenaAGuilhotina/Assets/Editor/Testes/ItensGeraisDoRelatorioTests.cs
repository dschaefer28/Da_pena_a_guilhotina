using System.Collections.Generic;
using FMODUnity;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Relatório de testes de 28/09, itens 7, 12, 20, 21 e 23: retomada do tutorial ao carregar um save antigo, fim de jogo
/// registrado no save, nome da cena do menu, limite da câmera do porão e ouvinte do FMOD nas câmeras.
/// </summary>
public class ItensGeraisDoRelatorioTests
{
    private readonly List<Object> criados = new List<Object>();

    [TearDown]
    public void Limpar()
    {
        foreach (Object objeto in criados) if (objeto != null) Object.DestroyImmediate(objeto);
        criados.Clear();
    }

    // ===== Item 7: tutorial ao carregar =====

    [Test]
    public void Tutorial_SaveComTutorialConcluido_VaiAoMenosAteAMesa()
    {
        // Save v6 real de 26/09: etapa 7 ("desça ao porão") gravada na Fase 2, com o caso do tutorial já concluído.
        Assert.AreEqual(14, TutorialManager.EtapaAoCarregar(7, 15, 14, tutorialConcluido: true));
        // Etapa salva depois da mesa (tutorial terminado) não volta para trás.
        Assert.AreEqual(15, TutorialManager.EtapaAoCarregar(15, 15, 14, tutorialConcluido: true));
        // Tutorial ainda em andamento: a etapa salva vale como está.
        Assert.AreEqual(7, TutorialManager.EtapaAoCarregar(7, 15, 14, tutorialConcluido: false));
        // Roteiro sem a etapa da mesa: nada é inventado; valores fora da lista são limitados.
        Assert.AreEqual(7, TutorialManager.EtapaAoCarregar(7, 15, -1, tutorialConcluido: true));
        Assert.AreEqual(15, TutorialManager.EtapaAoCarregar(99, 15, 14, tutorialConcluido: false));
    }

    [Test]
    public void Tutorial_RoteiroDaCenaJogoTemAEtapaDaMesa()
    {
        ComCena("Assets/_Project/Scenes/Jogo.unity", cena =>
        {
            TutorialManager tm = null;
            foreach (GameObject raiz in cena.GetRootGameObjects()) if (tm == null) tm = raiz.GetComponentInChildren<TutorialManager>(true);
            Assert.IsNotNull(tm, "TutorialManager não está na cena Jogo.");
            Assert.IsTrue(tm.etapas.Exists(e => e != null && e.etapaId == TutorialManager.EtapaMesaDeCasos),
                $"Sem a etapa '{TutorialManager.EtapaMesaDeCasos}', um save antigo não tem para onde avançar.");
        });
    }

    // ===== Item 21: fim de jogo =====

    [Test]
    public void Save_FimDeJogo_RegistradoENaoContinua()
    {
        var go = new GameObject("GameManager_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        gm.faseAtual = GameManager.UltimaFase;
        gm.rotaFinal = RotaFinal.B_Tirano;

        SistemaDeSave.DadosDeSave dados = SistemaDeSave.CapturarDados(gm, "Tribunal", 15);
        Assert.IsTrue(SistemaDeSave.PodeContinuar(dados), "Antes do veredito, a partida continua.");

        SistemaDeSave.MarcarFimDeJogo(dados, RotaFinal.B_Tirano, reuAbsolvido: false, "O Tirano");
        SistemaDeSave.DadosDeSave lido = SistemaDeSave.DesserializarDados(JsonUtility.ToJson(dados));
        Assert.AreEqual(SistemaDeSave.VersaoAtual, lido.versao);
        Assert.IsTrue(lido.jogoConcluido);
        Assert.AreEqual(RotaFinal.B_Tirano, lido.desfechoRota);
        Assert.IsFalse(lido.desfechoReuAbsolvido);
        Assert.AreEqual("O Tirano", lido.desfechoTitulo);
        Assert.IsFalse(SistemaDeSave.PodeContinuar(lido), "Jogo terminado não volta para antes do tribunal.");
    }

    [Test]
    public void Save_V7SemCamposNovos_ContinuaComoPartidaEmAndamento()
    {
        const string jsonV7 = "{\"versao\":7,\"cena\":\"Fase4\",\"faseAtual\":4,\"rotaFinal\":1}";
        SistemaDeSave.DadosDeSave dados = SistemaDeSave.DesserializarDados(jsonV7);
        Assert.IsNotNull(dados);
        Assert.IsFalse(dados.jogoConcluido);
        Assert.IsTrue(SistemaDeSave.PodeContinuar(dados));
        Assert.IsFalse(SistemaDeSave.PodeContinuar(null));
    }

    // ===== Item 23: nome da cena do menu =====

    [Test]
    public void Menu_NomeDaCenaIgualAoDoBuild()
    {
        bool achou = false;
        foreach (EditorBuildSettingsScene cena in EditorBuildSettings.scenes)
            if (cena.enabled && System.IO.Path.GetFileNameWithoutExtension(cena.path) == PauseMenu.CenaDoMenu) achou = true; // maiúsculas contam
        Assert.IsTrue(achou, $"Nenhuma cena do build se chama exatamente '{PauseMenu.CenaDoMenu}'.");
    }

    // ===== Itens 12 e 20: câmeras =====

    [Test]
    public void Porao_CameraLimitadaASala()
    {
        ComCena("Assets/_Project/Scenes/Porao.unity", cena =>
        {
            CinemachineConfiner2D confinador = null;
            foreach (GameObject raiz in cena.GetRootGameObjects()) if (confinador == null) confinador = raiz.GetComponentInChildren<CinemachineConfiner2D>(true);
            Assert.IsNotNull(confinador, "A câmera do porão não tem limite.");
            var limite = confinador.BoundingShape2D;
            Assert.IsNotNull(limite, "O limite da câmera não aponta para nada.");
            Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(limite) && PrefabUtility.IsPrefabAssetMissing(limite.gameObject),
                "O limite é instância de um prefab que não existe mais.");

            Assert.IsTrue(CamerasECenasTool.RetanguloDaSala(cena, out Rect sala), "Paredes e chão do porão não encontrados.");
            Bounds b = limite.bounds;
            const float folga = 0.05f;
            Assert.LessOrEqual(b.max.x, sala.xMax + folga, "A câmera passa da parede direita.");
            Assert.GreaterOrEqual(b.min.x, sala.xMin - folga, "A câmera passa da parede esquerda.");
            Assert.LessOrEqual(b.max.y, sala.yMax + folga, "A câmera passa do topo das paredes.");
            Assert.GreaterOrEqual(b.min.y, sala.yMin - folga, "A câmera passa do chão.");
        });
    }

    [Test]
    public void Cenas_MenuETribunal_TemFmodStudioListener()
    {
        foreach (string caminho in new[] { "Assets/_Project/Scenes/menu principal.unity", "Assets/_Project/Scenes/Tribunal.unity" })
            ComCena(caminho, cena =>
            {
                bool temOuvinte = false;
                foreach (GameObject raiz in cena.GetRootGameObjects())
                    foreach (Camera camera in raiz.GetComponentsInChildren<Camera>(true))
                        temOuvinte |= camera.GetComponent<StudioListener>() != null;
                Assert.IsTrue(temOuvinte, $"A câmera de '{cena.name}' não tem FMOD Studio Listener.");
            });
    }

    private static void ComCena(string caminho, System.Action<Scene> acao)
    {
        Scene cena = SceneManager.GetSceneByPath(caminho);
        bool abriu = false;
        if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive); abriu = true; }
        try { acao(cena); }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
    }
}
