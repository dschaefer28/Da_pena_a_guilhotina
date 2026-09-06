using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Runs against the real Jogo/Porao scenes; play-mode changes are discarded on exit.</summary>
[InitializeOnLoad]
public static class ProgressionRegression
{
    private const string RunningKey = "DaPena.ProgressionRegression";
    private static int stage;
    private static double due;
    private static int assertions;

    static ProgressionRegression()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false))
            {
                stage = 0;
                assertions = 0;
                due = EditorApplication.timeSinceStartup + 2;
                EditorApplication.update += Tick;
            }
            if (state == PlayModeStateChange.ExitingPlayMode)
                EditorApplication.update -= Tick;
        };
    }

    [MenuItem("Tools/Da Pena/Validar tutorial e inventário")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Jogo" || SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("Abra a cena Jogo salva, fora do Play Mode, para executar a validação.");
            return;
        }
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        assertions++;
    }

    private static T Asset<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/" + path);

    private static void FinishConversation()
    {
        var system = GameManager.Instance.dialogueSystem;
        for (int step = 0; system.IsDialogueActive && step < 60; step++)
        {
            system.AdvanceDialogue();
            var lines = system.dialogueData.talkScript;
            var choices = lines[lines.Count - 1].choices;
            if (choices != null && choices.Count > 0) system.MakeChoice(choices[0].nextDialogue);
        }
        Require(!system.IsDialogueActive, "A conversa não chegou ao fim.");
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < due || SceneTransition.IsTransitioning) return;
        try
        {
            var manager = GameManager.Instance;
            var inventory = manager.inventoryManager;
            var report = Asset<Item>("Scriptableobjects/Evidencia_RelatoBradier.asset");
            var decree = Asset<Item>("Scriptableobjects/Evidencia_Decreto1780.asset");
            var pamphlet = Asset<Item>("Scriptableobjects/Panfleto_MemoireJustificatif.asset");
            if (stage == 0)
            {
                var npcs = UnityEngine.Object.FindObjectsByType<NPCMovement>(FindObjectsSortMode.None);
                var marie = npcs.Single(n => n.name == "Marie Bradier");
                var dupaty = npcs.Single(n => n.name == "Charles Dupaty");
                Require(!inventory.HasItem(report.itemID) && !inventory.HasItem(decree.itemID), "Inventário inicial sujo.");
                marie.Interact();
                Require(!manager.dialogueSystem.IsDialogueActive, "Marie liberada antes de Dupaty.");
                var trapdoor = UnityEngine.Object.FindFirstObjectByType<TrapdoorInteractable>();
                trapdoor.Interact();
                Require(!SceneTransition.IsTransitioning, "Porão liberado sem evidências.");
                manager.dialogueSystem.CancelDialogue();
                dupaty.Interact();
                Require(!marie.canInteract, "Marie liberada antes do fim da introdução.");
                FinishConversation();
                Require(marie.canInteract, "Dupaty não liberou Marie.");
                Require(!inventory.HasItem(decree.itemID), "Dupaty entregou o decreto antes do relato.");
                marie.Interact();
                manager.dialogueSystem.CancelDialogue();
                Require(!inventory.HasItem(report.itemID) && marie.canInteract, "Cancelamento concedeu recompensa ou bloqueou Marie.");

                var junk = ScriptableObject.CreateInstance<Item>();
                junk.itemID = "regression-full-inventory";
                junk.itemAmt = 1;
                foreach (Transform child in inventory.inventoryGrid.transform.Cast<Transform>().ToArray())
                {
                    var slot = child.GetComponent<UISlotHandler>();
                    if (slot != null) inventory.PlaceInInventory(slot, junk.Clone());
                }
                marie.Interact();
                FinishConversation();
                Require(!inventory.HasItem(report.itemID) && marie.canInteract, "Inventário cheio perdeu a recompensa pendente.");
                inventory.LimparInventarioVisual();
                marie.Interact();
                FinishConversation();
                Require(inventory.HasItem(report.itemID), "Marie não entregou o relato.");
                Require(!inventory.HasItem(decree.itemID), "Marie entregou o decreto indevidamente.");
                trapdoor.Interact();
                Require(!SceneTransition.IsTransitioning, "Porão liberado somente com o relato.");
                manager.dialogueSystem.CancelDialogue();
                dupaty.Interact();
                Require(manager.dialogueSystem.dialogueData == Asset<DialogueData>("Dialogos/Charles Dupaty/Dupatsequencia1.asset"), "Dupaty não mudou de conversa.");
                Require(!inventory.HasItem(decree.itemID), "Decreto entregue antes do fim da conversa.");
                FinishConversation();
                Require(inventory.HasItem(decree.itemID), "Dupaty não entregou o decreto.");
                dupaty.Interact();
                FinishConversation();
                Require(inventory.inventoryGrid.GetComponentsInChildren<UISlotHandler>(true).Single(s => s.item != null && s.item.itemID == decree.itemID).item.itemAmt == 1, "Recompensa duplicada.");
                trapdoor.Interact();
                Require(SceneTransition.IsTransitioning, "Porão continua bloqueado com as duas evidências.");
                stage = 1;
                due = EditorApplication.timeSinceStartup + 3;
            }
            else if (stage == 1)
            {
                Require(SceneManager.GetActiveScene().name == "Porao", "Transição não chegou ao porão.");
                Require(inventory.HasItem(report.itemID) && inventory.HasItem(decree.itemID), "Itens perdidos na descida.");
                var press = UnityEngine.Object.FindObjectsByType<CraftingPress>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
                var pressInteraction = UnityEngine.Object.FindFirstObjectByType<PressInteractable>();
                pressInteraction.Interact();
                foreach (Transform child in inventory.inventoryGrid.transform.Cast<Transform>().ToArray())
                {
                    var slot = child.GetComponent<UISlotHandler>();
                    if (slot == null || slot.item == null) continue;
                    if (slot.item.Matches(report)) inventory.PlaceInInventory(press.slotInput1, slot.item);
                    else if (slot.item.Matches(decree)) inventory.PlaceInInventory(press.slotInput2, slot.item);
                    else continue;
                    inventory.ClearItemSlot(slot);
                }
                press.CombineItems();
                Require(press.slotOutput.item != null && press.slotOutput.item.Matches(pamphlet), "Prensa não produziu o memorial.");
                Require(press.slotInput1.item == null && press.slotInput2.item == null, "Prensa não consumiu as evidências.");
                pressInteraction.ClosePressUI();
                Require(inventory.HasItem(pamphlet.itemID), "Fechar prensa perdeu o memorial.");
                Require(Time.timeScale == 1, "Fechar prensa deixou o jogo pausado.");
                var door = UnityEngine.Object.FindFirstObjectByType<DoorInteractable>();
                door.Interact();
                stage = 2;
                due = EditorApplication.timeSinceStartup + 3;
            }
            else
            {
                Require(SceneManager.GetActiveScene().name == "Jogo", "Retorno não chegou ao escritório.");
                Require(inventory.HasItem(pamphlet.itemID), "Memorial perdido no retorno.");
                var table = UnityEngine.Object.FindFirstObjectByType<TableInteractable>();
                table.Interact();
                Require(table.caseSelectionUI.activeSelf, "Mesa não liberada após imprimir memorial.");
                table.caseSelectionUI.SetActive(false);
                Debug.Log($"[REGRESSION PASS] {assertions} verificações: ordem, cancelamento, inventário cheio, recompensas, porão, prensa e retorno.");
                Stop();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[REGRESSION FAIL] " + exception);
            Stop();
        }
    }

    private static void Stop()
    {
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }
}
