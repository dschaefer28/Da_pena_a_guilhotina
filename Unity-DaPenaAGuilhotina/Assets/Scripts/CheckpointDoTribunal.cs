using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Documento (Fase 4, "Gatilho de Progressão"): Dupaty é o checkpoint da fase. Na Fase 4, falar com ele confere se
/// o caso da rota já foi investigado e o panfleto impresso (GameManager.FaseConcluida); se sim, ele libera a ida
/// ao Tribunal. Antes da Fase 4 este objeto não responde, e o Dupaty segue com o comportamento normal.
///
/// Fica num filho do Dupaty com um Collider2D (trigger) próprio: o NPCMovement dele é silenciado depois do
/// tutorial (GerenciadorCena1), então o Player escolhe este interagível no lugar.
/// </summary>
public class CheckpointDoTribunal : MonoBehaviour, IInteractable
{
    public string cenaDoTribunal = "Tribunal";

    [Header("Falas (vazio = usa o aviso em texto)")]
    public DialogueData dialogoNaoPronto;
    public DialogueData dialogoPronto;
    [TextArea(2, 3)] public string avisoNaoPronto = "Dupaty: Ainda não. Aceite o caso na mesa, investigue e imprima o panfleto antes do julgamento.";
    [TextArea(2, 3)] public string avisoPronto = "Dupaty: As provas estão reunidas e o panfleto está pronto. Vamos ao Tribunal.";

    private Action aoTerminarDialogo;

    public bool PodeInteragir => GameManager.Instance != null && GameManager.Instance.faseAtual >= GameManager.UltimaFase;

    public void Interact()
    {
        if (!PodeInteragir) return;

        if (GameManager.Instance.FaseConcluida) Falar(dialogoPronto, avisoPronto, IrParaTribunal);
        else Falar(dialogoNaoPronto, avisoNaoPronto, null);
    }

    private void Falar(DialogueData dialogo, string aviso, Action depois)
    {
        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogo == null || dialogueSystem == null)
        {
            AvisoNaTela.Mostrar(aviso);
            depois?.Invoke();
            return;
        }

        aoTerminarDialogo = depois;
        if (depois != null) dialogueSystem.OnDialogueEnded += HandleDialogoTerminou;
        dialogueSystem.dialogueData = dialogo;
        dialogueSystem.Next();
    }

    private void HandleDialogoTerminou()
    {
        DialogueSystem dialogueSystem = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : null;
        if (dialogueSystem != null) dialogueSystem.OnDialogueEnded -= HandleDialogoTerminou;

        Action depois = aoTerminarDialogo;
        aoTerminarDialogo = null;
        depois?.Invoke();
    }

    private void IrParaTribunal()
    {
        if (!Application.CanStreamedLevelBeLoaded(cenaDoTribunal))
        {
            Debug.LogError($"[CheckpointDoTribunal] Cena '{cenaDoTribunal}' não está nas Build Settings.", this);
            return;
        }

        // Salva no escritório: quem continuar o jogo volta aqui e fala com o Dupaty de novo.
        SistemaDeSave.Salvar();

        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.LoadScene(cenaDoTribunal);
        else SceneManager.LoadScene(cenaDoTribunal);
    }

    private void OnDestroy()
    {
        if (aoTerminarDialogo != null && GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            GameManager.Instance.dialogueSystem.OnDialogueEnded -= HandleDialogoTerminou;
    }
}
