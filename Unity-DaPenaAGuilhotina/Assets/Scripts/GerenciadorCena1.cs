using UnityEngine;

public class GerenciadorCena1 : MonoBehaviour
{
    [Header("Condição de Progresso")]
    [Tooltip("Arraste o ScriptableObject do panfleto que prova que o jogador já voltou do porão.")]
    public Item panfletoCraftado;

    [Header("NPCs para Desligar")]
    [Tooltip("Arraste a Mary e o Dupatch para cá.")]
    public NPCMovement[] npcsDaFase1;

    void Start()
    {
        // Agora podemos verificar imediatamente ao entrar na cena!
        VerificarEstadoDaCena();
    }

    private void VerificarEstadoDaCena()
    {
        if (GameManager.Instance == null || panfletoCraftado == null) return;

        bool temPanfleto = false;

        // 1. Procura o panfleto direto na memória imortal do jogo (100% seguro contra lags de UI)
        if (GameManager.Instance.inventarioSalvo != null)
        {
            foreach (Item item in GameManager.Instance.inventarioSalvo)
            {
                if (item != null && item.itemID == panfletoCraftado.itemID)
                {
                    temPanfleto = true;
                    break;
                }
            }
        }

        // 2. Se achou a prova de que o jogador já foi pro porão, silencia todo mundo
        if (temPanfleto)
        {
            Debug.Log("[CENA 1] Panfleto detectado na memória! Silenciando NPCs do prólogo...");
            
            foreach (NPCMovement npc in npcsDaFase1)
            {
                if (npc != null)
                {
                    npc.DisableInteraction();
                }
            }
        }
    }
}