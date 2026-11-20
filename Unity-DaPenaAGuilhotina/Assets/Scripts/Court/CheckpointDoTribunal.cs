using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dupaty como checkpoint da Fase 4: só libera o tribunal se, pelo histórico (a prensa consome as pistas):
///  1. um caso da Fase 4 da rota travada foi concluído;
///  2. a publicação dele está registrada e o panfleto da versão impressa existe;
///  3. as pistas impressas constam no histórico de evidências (publicações legadas não são barradas);
///  4. as evidências extras configuradas para o caso foram obtidas (mesmo se gastas).
/// Fica num filho do Dupaty com trigger próprio porque o NPCMovement dele é silenciado após o tutorial.
/// </summary>
public class CheckpointDoTribunal : MonoBehaviour, IInteractable
{
    [Serializable]
    public class EvidenciasDoCaso
    {
        public CaseData caso;
        [Tooltip("Evidências que o jogador precisa ter obtido neste caso (valem mesmo se gastas na prensa). Cuidado: depois " +
                 "de o panfleto sair, o caso não entrega mais pistas — exija só o que o jogador sempre obtém antes.")]
        public List<Item> evidencias = new List<Item>();
    }

    public enum Situacao { ForaDaFase4, CasoNaoConcluido, SemPanfleto, PistasSemRegistro, FaltaEvidencia, Pronto }

    public string cenaDoTribunal = "Tribunal";

    [Header("Evidências exigidas (opcional, além do panfleto do caso)")]
    public List<EvidenciasDoCaso> evidenciasExigidas = new List<EvidenciasDoCaso>();

    [Header("Falas (vazio = usa o aviso em texto)")]
    public DialogueData dialogoNaoPronto;
    public DialogueData dialogoPronto;
    [TextArea(2, 3)] public string avisoNaoPronto = "Dupaty: Ainda não. Aceite o caso na mesa, investigue e imprima o panfleto antes do julgamento.";
    [TextArea(2, 3)] public string avisoPronto = "Dupaty: As provas estão reunidas e o panfleto está pronto. Vamos ao Tribunal.";
    [TextArea(2, 3)] public string avisoSemPanfleto = "Dupaty: Não encontro o panfleto deste caso. Sem ele não há defesa para levar ao tribunal.";
    [TextArea(2, 3)] public string avisoFaltaEvidencia = "Dupaty: Ainda falta uma prova que o tribunal vai exigir deste caso.";

    private Action aoTerminarDialogo;

    public bool PodeInteragir => GameManager.Instance != null && GameManager.Instance.faseAtual >= GameManager.UltimaFase;

    // Fica num filho do Dupaty: o aviso de interação mostra o nome dele, não o deste objeto.
    public string NomeExibido
    {
        get
        {
            NPCMovement npc = GetComponentInParent<NPCMovement>(true);
            return npc != null ? npc.NomeExibido : null;
        }
    }

    public void Interact()
    {
        if (!PodeInteragir) return;

        switch (Avaliar(GameManager.Instance, out _))
        {
            case Situacao.Pronto: Falar(dialogoPronto, avisoPronto, IrParaTribunal); break;
            case Situacao.SemPanfleto:
            case Situacao.PistasSemRegistro: Falar(null, avisoSemPanfleto, null); break;
            case Situacao.FaltaEvidencia: Falar(null, avisoFaltaEvidencia, null); break;
            default: Falar(dialogoNaoPronto, avisoNaoPronto, null); break;
        }
    }

    /// <summary>Devolve também o caso da Fase 4 que será julgado (ou null).</summary>
    public Situacao Avaliar(GameManager gm, out CaseData caso)
    {
        caso = null;
        if (gm == null || gm.faseAtual < GameManager.UltimaFase) return Situacao.ForaDaFase4;

        caso = gm.casosConcluidos.Find(c => c != null && c.fase == GameManager.UltimaFase && gm.CasoDaRotaAtual(c));
        if (caso == null) return Situacao.CasoNaoConcluido;
        CaseData julgado = caso;

        GameManager.PanfletoPublicado publicado = gm.panfletosPublicados.Find(p => p != null && p.caso == julgado);
        ReceitaDeCaso receita = caso.receitaDoPanfleto;
        if (publicado == null || receita == null || receita.PanfletoDe(receita.VersaoDe(publicado.nivel)) == null)
            return Situacao.SemPanfleto;

        if (!publicado.legado && publicado.pistas != null)
            foreach (string pista in publicado.pistas)
                if (!string.IsNullOrEmpty(pista) && !gm.evidenciasObtidas.Contains(pista)) return Situacao.PistasSemRegistro;

        EvidenciasDoCaso exigidas = evidenciasExigidas != null ? evidenciasExigidas.Find(e => e != null && e.caso == julgado) : null;
        if (exigidas != null && exigidas.evidencias != null)
            foreach (Item evidencia in exigidas.evidencias)
                if (evidencia != null && !gm.EvidenciaObtida(evidencia)) return Situacao.FaltaEvidencia;

        return Situacao.Pronto;
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

        // Salva no escritório: ao continuar, o jogador volta aqui e fala com o Dupaty de novo.
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
