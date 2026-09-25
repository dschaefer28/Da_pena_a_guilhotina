using FMODUnity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações de Transição")]
    [Tooltip("O nome exato da cena de destino (Ex: Fase2)")]
    public string cenaDestino = "Fase2";

    [Tooltip("Marque na porta do escritório: ela leva à cena do caso em andamento (CaseData.nextSceneName) " +
             "e fica trancada enquanto não houver caso aceito. 'Cena Destino' vira só o valor reserva.")]
    public bool destinoPeloCaso = false;
    public string avisoSemCaso = "Preciso aceitar um caso na mesa antes de sair.";

    [Header("Áudio (FMOD)")]
    [Tooltip("Som de porta abrindo, tocado junto com o fade da troca de cena (documento: Transição de Cena).")]
    public EventReference somPorta;

    [Header("Trava de Progressão (Opcional)")]
    [Tooltip("Arraste o ScriptableObject do item necessário para passar. Deixe VAZIO para portas livres.")]
    public Item itemObrigatorio;
    
    [Tooltip("O que o personagem pensa se tentar passar sem o item?")]
    public DialogueData pensamentoBloqueado;

    /// <summary>Falso enquanto o item obrigatório não estiver no inventário: o aviso de interação
    /// (PromptDeInteracao) só aparece sobre a porta quando ela realmente responde.</summary>
    public bool PodeInteragir =>
        itemObrigatorio == null ||
        (GameManager.Instance != null && GameManager.Instance.inventoryManager != null &&
         GameManager.Instance.inventoryManager.HasItem(itemObrigatorio.itemID));

    public void Interact()
    {
        if (!PodeInteragir)
        {
            Debug.Log("A porta está trancada. Preciso terminar o meu trabalho primeiro.");

            if (pensamentoBloqueado != null && GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            {
                GameManager.Instance.dialogueSystem.dialogueData = pensamentoBloqueado;
                GameManager.Instance.dialogueSystem.Next();
            }
            return; // Corta a viagem
        }

        string destino = cenaDestino;
        if (destinoPeloCaso)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !gm.CasoAtualEmAndamento)
            {
                AvisoNaTela.Mostrar(avisoSemCaso);
                return;
            }
            if (!string.IsNullOrEmpty(gm.casoEscolhido.nextSceneName)) destino = gm.casoEscolhido.nextSceneName;
        }

        if (string.IsNullOrEmpty(destino) || !Application.CanStreamedLevelBeLoaded(destino))
        {
            Debug.LogError($"[DoorInteractable] Cena '{destino}' não está nas Build Settings.", this);
            return;
        }

        Debug.Log($"Salvando inventário e indo para a cena: {destino}...");
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.SalvarEstadoAtual();
        }

        AudioSeguro.TocarUmaVez(somPorta, transform.position);
        Time.timeScale = 1f; // garante que a próxima cena não abra congelada
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(destino);
        else
            SceneManager.LoadScene(destino);
    }
}