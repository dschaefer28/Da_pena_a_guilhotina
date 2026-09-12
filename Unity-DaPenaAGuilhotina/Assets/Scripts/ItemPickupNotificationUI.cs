using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mostra um popup temporário sempre que o jogador recebe um item novo no inventário
/// (estante, prensa, etc.). A imagem e o texto vêm do próprio ScriptableObject do Item
/// (campos "Item Img" e "Item Name"), então cada item pode ter sua própria mensagem
/// sem precisar mexer em código.
///
/// Setup no Editor:
/// 1. Crie um painel de UI (Image de fundo) dentro do Canvas da cena e deixe-o inativo por padrão.
/// 2. Dentro dele, coloque uma Image (para o ícone do item) e um texto TMP (para a mensagem).
/// 3. Coloque este script em qualquer GameObject do Canvas e arraste as referências abaixo.
/// </summary>
public class ItemPickupNotificationUI : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("GameObject raiz do popup (o que fica inativo/ativo). Deixe desmarcado no Editor por padrão.")]
    public GameObject painelPopup;
    [Tooltip("Image usada para mostrar o ícone do item recebido (Item.itemImg).")]
    public Image imagemItem;
    [Tooltip("Texto TMP usado para mostrar a mensagem de coleta.")]
    public TextMeshProUGUI textoMensagem;

    [Header("Configurações")]
    [Tooltip("Use {0} no lugar onde o nome do item deve aparecer.")]
    public string formatoMensagem = "Você recebeu: {0}";
    [Tooltip("Por quantos segundos o popup fica visível antes de sumir sozinho.")]
    [Min(0.1f)] public float duracaoExibicao = 2.5f;

    private InventoryManager inventoryManagerAtual;
    private readonly Queue<Item> filaDeItens = new Queue<Item>();
    private Coroutine exibicaoEmAndamento;

    void Start()
    {
        if (painelPopup != null) painelPopup.SetActive(false);
        VincularInventario();
    }

    void OnEnable()
    {
        VincularInventario();
    }

    void OnDisable()
    {
        if (inventoryManagerAtual != null)
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
    }

    // Segue o mesmo padrão do GameManager/HUDManager: o InventoryManager é recriado a cada
    // cena, então é preciso reconectar o evento sempre que a referência atual mudar.
    private void VincularInventario()
    {
        InventoryManager atual = GameManager.Instance != null
            ? GameManager.Instance.inventoryManager
            : FindAnyObjectByType<InventoryManager>();

        if (atual == inventoryManagerAtual) return;

        if (inventoryManagerAtual != null)
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;

        inventoryManagerAtual = atual;

        if (inventoryManagerAtual != null)
            inventoryManagerAtual.OnItemAdicionado += HandleItemAdicionado;
        else
            Debug.LogWarning("[ItemPickupNotificationUI] Nenhum InventoryManager encontrado na cena.");
    }

    private void HandleItemAdicionado(Item item)
    {
        if (item == null) return;

        filaDeItens.Enqueue(item);
        if (exibicaoEmAndamento == null)
            exibicaoEmAndamento = StartCoroutine(ExibirFila());
    }

    // Fila garante que, se dois itens forem recebidos quase juntos, o segundo popup só
    // aparece depois que o primeiro terminar (em vez de sobrescrever ou piscar).
    private IEnumerator ExibirFila()
    {
        while (filaDeItens.Count > 0)
        {
            Item item = filaDeItens.Dequeue();
            MostrarPopup(item);
            yield return new WaitForSecondsRealtime(duracaoExibicao);
            if (painelPopup != null) painelPopup.SetActive(false);
        }
        exibicaoEmAndamento = null;
    }

    private void MostrarPopup(Item item)
    {
        if (imagemItem != null)
        {
            imagemItem.sprite = item.itemImg;
            imagemItem.gameObject.SetActive(item.itemImg != null);
        }

        if (textoMensagem != null)
            textoMensagem.text = string.Format(formatoMensagem, item.NomeExibicao);

        if (painelPopup != null) painelPopup.SetActive(true);
    }
}
