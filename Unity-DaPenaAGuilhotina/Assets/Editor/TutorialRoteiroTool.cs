using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sincroniza as cenas da Fase 1 (Jogo e Porao) com o tutorial universal:
///  - copia o roteiro padrão do código (TutorialManager.etapas) para o TutorialManager da cena Jogo;
///  - faz as duas cenas usarem o popup/TutorialStepUI do prefab UI (o Porão tinha cópias próprias e
///    o Jogo tinha overrides de tamanho/posição e um Canvas adicionado só na cena);
///  - adiciona o PromptDeInteracao ao Player de cada cena.
/// Rode DEPOIS de "1 - Aplicar layout do popup". Play Mode desligado e cena atual salva.
/// Menu: Ferramentas > Tutorial > 2.
/// </summary>
public static class TutorialRoteiroTool
{
    private const string CaminhoPrefabUI = "Assets/Prefab/UI.prefab";
    private const string CaminhoFonte = "Assets/Fonts/Cinzel-Regular SDF.asset";
    private static readonly string[] Cenas = { "Assets/Scenes/Jogo.unity", "Assets/Scenes/Porao.unity" };

    [MenuItem("Ferramentas/Tutorial/2 - Aplicar roteiro padrão e sincronizar cenas (Jogo, Porao)")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[TutorialRoteiroTool] Saia do Play Mode antes.");
            return;
        }
        Scene atual = SceneManager.GetActiveScene();
        if (atual.isDirty)
        {
            Debug.LogError($"[TutorialRoteiroTool] A cena '{atual.name}' tem alterações não salvas. Salve (ou descarte) antes.");
            return;
        }
        string caminhoOriginal = atual.path;

        foreach (string caminho in Cenas)
        {
            Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
            var log = new System.Text.StringBuilder($"[TutorialRoteiroTool] {caminho}:");

            GameObject raizUI = null;
            foreach (GameObject raiz in cena.GetRootGameObjects())
            {
                if (raiz.name == "UI" && PrefabUtility.GetCorrespondingObjectFromSource(raiz) != null) raizUI = raiz;
                if (raiz.name == "TutorialStepUI" && !PrefabUtility.IsPartOfAnyPrefab(raiz))
                {
                    Object.DestroyImmediate(raiz);
                    log.Append(" removeu TutorialStepUI solto da cena;");
                }
            }

            if (raizUI != null)
            {
                log.Append(SincronizarInstanciaUI(raizUI));
            }
            else
            {
                log.Append(" (instância do prefab UI não encontrada);");
            }

            TutorialManager tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            if (tm != null)
            {
                AplicarRoteiroPadrao(tm);
                log.Append($" roteiro padrão aplicado ({tm.etapas.Count} etapas);");
            }

            if (raizUI != null) log.Append(AplicarRevelacoes(raizUI, cena));

            log.Append(GarantirPromptNoPlayer());
            log.Append(CorrigirReferenciasDoGerenciador(cena));

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log(log.ToString());
        }

        if (!string.IsNullOrEmpty(caminhoOriginal)) EditorSceneManager.OpenScene(caminhoOriginal, OpenSceneMode.Single);
    }

    private static string SincronizarInstanciaUI(GameObject raizUI)
    {
        var log = new System.Text.StringBuilder();

        // 1) Objetos do prefab apagados só nesta cena (Porão): voltam.
        foreach (var removido in PrefabUtility.GetRemovedGameObjects(raizUI))
        {
            string nome = removido.assetGameObject != null ? removido.assetGameObject.name : "?";
            if (nome == "PopupTutorial" || nome == "PopupItemRecebido" || nome == "TutorialUI")
            {
                removido.Revert();
                log.Append($" reverteu remoção de {nome};");
            }
        }

        // 2) Cópias locais desses popups (adicionadas só na cena) somem: o prefab passa a mandar.
        Transform itens = raizUI.transform.Find("ItensPoupUp");
        if (itens != null)
        {
            for (int i = itens.childCount - 1; i >= 0; i--)
            {
                GameObject filho = itens.GetChild(i).gameObject;
                if (!PrefabUtility.IsAddedGameObjectOverride(filho)) continue;
                string nomeFilho = filho.name;
                if (nomeFilho == "PopupTutorial" || nomeFilho == "PopupItemRecebido" || nomeFilho == "TutorialUI")
                {
                    Object.DestroyImmediate(filho);
                    log.Append($" removeu cópia local de {nomeFilho};");
                }
            }
        }

        // 3) Canvas/GraphicRaycaster adicionados na cena por cima dos popups: o prefab já tem os seus.
        //    Ao ganhar o Canvas no prefab, a Unity marcou o do prefab como "removido" nesta instância e
        //    manteve o da cena como "adicionado". Primeiro o do prefab volta (senão o GraphicRaycaster,
        //    que exige um Canvas, impede tirar o duplicado); depois o duplicado sai.
        if (itens != null)
        {
            foreach (string nome in new[] { "PopupTutorial", "PopupItemRecebido" })
            {
                Transform popup = itens.Find(nome);
                if (popup == null) continue;
                GameObject go = popup.gameObject;
                bool tinhaDuplicado = false;
                foreach (Canvas c in go.GetComponents<Canvas>())
                    if (PrefabUtility.IsAddedComponentOverride(c)) tinhaDuplicado = true;
                if (!tinhaDuplicado) continue;

                // a) o GraphicRaycaster (do prefab) sai por um instante, senão a Unity não deixa tirar o Canvas;
                GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
                if (raycaster != null) Object.DestroyImmediate(raycaster);
                // b) o Canvas adicionado pela cena sai;
                foreach (Canvas c in go.GetComponents<Canvas>())
                    if (PrefabUtility.IsAddedComponentOverride(c)) PrefabUtility.RevertAddedComponent(c, InteractionMode.AutomatedAction);
                // c) Canvas e GraphicRaycaster do prefab voltam.
                foreach (var removido in PrefabUtility.GetRemovedComponents(raizUI))
                    if (removido.containingInstanceGameObject == go) removido.Revert();
                log.Append($" tirou Canvas duplicado de {nome};");
            }
        }

        // 4) Layout dos popups e ligações do TutorialStepUI vêm do prefab (mantendo Revelacoes, que são da cena).
        itens = raizUI.transform.Find("ItensPoupUp");
        if (itens != null)
        {
            int revertidos = 0;
            // O próprio ItensPoupUp tinha posição/tamanho sobrescritos na cena (era um 100x100 deslocado);
            // com o prefab em tela cheia, esse override vira um deslocamento de todos os popups.
            if (PrefabUtility.IsPartOfPrefabInstance(itens) && PrefabUtility.HasPrefabInstanceAnyOverrides(itens.gameObject, false))
            {
                PrefabUtility.RevertObjectOverride(itens.GetComponent<RectTransform>(), InteractionMode.AutomatedAction);
                revertidos++;
            }
            foreach (string nome in new[] { "PopupTutorial", "PopupItemRecebido" })
            {
                Transform popup = itens.Find(nome);
                if (popup == null) continue;
                foreach (Component c in popup.GetComponentsInChildren<Component>(true))
                {
                    if (c == null || !PrefabUtility.IsPartOfPrefabInstance(c)) continue;
                    if (!(c is RectTransform || c is Image || c is TextMeshProUGUI || c is LayoutElement || c is LayoutGroup || c is Canvas)) continue;
                    if (!PrefabUtility.HasPrefabInstanceAnyOverrides(c.gameObject, false)) continue;
                    PrefabUtility.RevertObjectOverride(c, InteractionMode.AutomatedAction);
                    revertidos++;
                }
            }
            log.Append($" layout dos popups revertido ({revertidos} componentes);");

            TutorialStepUI stepUI = itens.GetComponentInChildren<TutorialStepUI>(true);
            if (stepUI != null)
            {
                var so = new SerializedObject(stepUI);
                foreach (string campo in new[] { "painelPopup", "imagemIcone", "textoMensagem", "botaoContinuar", "containerGlifos", "fonteGlifos" })
                {
                    SerializedProperty p = so.FindProperty(campo);
                    if (p != null && p.prefabOverride) PrefabUtility.RevertPropertyOverride(p, InteractionMode.AutomatedAction);
                }
                log.Append(" ligações do TutorialStepUI vêm do prefab;");
            }

            ItemPickupNotificationUI popupItem = itens.GetComponentInChildren<ItemPickupNotificationUI>(true);
            if (popupItem != null && PrefabUtility.HasPrefabInstanceAnyOverrides(popupItem.gameObject, false))
            {
                PrefabUtility.RevertObjectOverride(popupItem, InteractionMode.AutomatedAction);
                log.Append(" ligações do popup de item vêm do prefab;");
            }
        }
        return log.ToString();
    }

    // Quais botões mobile cada etapa revela, por cena. Ficam como override da instância (são objetos de
    // cena, o prefab não pode apontar para eles). Reescrever a lista inteira evita o bug de a Unity perder
    // as referências quando a lista do prefab muda de tamanho.
    private static readonly (string etapaId, string caminho)[] RevelacoesJogo =
    {
        ("Joystick", "MobileControlsUI/TouchZone/ObjetosDesativar/JoystickBG"),
        ("InventoryButton", "MobileControlsUI/TouchZone/ObjetosDesativar/InventoryButton"),
        ("PauseButton", "MobileControlsUI/TouchZone/PauseButton"),
    };

    private static string AplicarRevelacoes(GameObject raizUI, Scene cena)
    {
        TutorialStepUI stepUI = raizUI.GetComponentInChildren<TutorialStepUI>(true);
        if (stepUI == null) return " (sem TutorialStepUI);";

        var lista = new List<RevelacaoDeEtapa>();
        if (cena.name == "Jogo")
        {
            foreach (var (etapaId, caminho) in RevelacoesJogo)
            {
                GameObject alvo = AcharNaCena(cena, caminho);
                if (alvo == null) { Debug.LogWarning($"[TutorialRoteiroTool] Não achei '{caminho}' na cena {cena.name} para a etapa {etapaId}."); continue; }
                lista.Add(new RevelacaoDeEtapa { etapaId = etapaId, objetosParaRevelar = new List<GameObject> { alvo } });
            }
        }
        Undo.RecordObject(stepUI, "Revelações do tutorial");
        stepUI.revelacoes = lista;
        EditorUtility.SetDirty(stepUI);
        return $" revelações da cena ({lista.Count});";
    }

    private static GameObject AcharNaCena(Scene cena, string caminho)
    {
        string[] partes = caminho.Split('/');
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            if (raiz.name != partes[0]) continue;
            Transform t = raiz.transform;
            for (int i = 1; i < partes.Length && t != null; i++) t = t.Find(partes[i]);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static void AplicarRoteiroPadrao(TutorialManager alvo)
    {
        // Um TutorialManager novo nasce com o roteiro do código (inicializador do campo).
        var temp = new GameObject("__roteiro_padrao");
        try
        {
            var padrao = temp.AddComponent<TutorialManager>();
            var copia = new List<TutorialStep>();
            foreach (var e in padrao.etapas)
            {
                copia.Add(new TutorialStep
                {
                    etapaId = e.etapaId,
                    mensagem = e.mensagem,
                    icone = e.icone,
                    controle = e.controle,
                    tipoDeAvanco = e.tipoDeAvanco,
                    nomeDoEvento = e.nomeDoEvento,
                    nomeDaCena = e.nomeDaCena
                });
            }
            Undo.RecordObject(alvo, "Aplicar roteiro padrão do tutorial");
            alvo.etapas = copia;
            EditorUtility.SetDirty(alvo);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    // GerenciadorCena1.npcsParaSumir apontava para o prefab da Marie (asset) em vez do objeto da cena.
    private static string CorrigirReferenciasDoGerenciador(Scene cena)
    {
        GerenciadorCena1 g = Object.FindAnyObjectByType<GerenciadorCena1>(FindObjectsInactive.Include);
        if (g == null || g.npcsParaSumir == null) return string.Empty;
        int corrigidos = 0;
        for (int i = 0; i < g.npcsParaSumir.Length; i++)
        {
            GameObject alvo = g.npcsParaSumir[i];
            if (alvo == null || alvo.scene == cena) continue;
            foreach (NPCMovement npc in Object.FindObjectsByType<NPCMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (npc.gameObject.scene != cena || npc.name != alvo.name) continue;
                Undo.RecordObject(g, "Corrigir referência de NPC");
                g.npcsParaSumir[i] = npc.gameObject;
                EditorUtility.SetDirty(g);
                corrigidos++;
                break;
            }
        }
        return corrigidos > 0 ? $" GerenciadorCena1: {corrigidos} referência(s) de NPC trocada(s) de prefab para objeto da cena;" : string.Empty;
    }

    private static string GarantirPromptNoPlayer()
    {
        PlayerInteraction jogador = Object.FindAnyObjectByType<PlayerInteraction>(FindObjectsInactive.Include);
        if (jogador == null) return " (sem Player);";
        PromptDeInteracao prompt = jogador.GetComponent<PromptDeInteracao>();
        if (prompt == null) prompt = jogador.gameObject.AddComponent<PromptDeInteracao>();
        if (prompt.fonte == null) prompt.fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoFonte);
        EditorUtility.SetDirty(prompt);
        return " PromptDeInteracao no Player;";
    }
}
