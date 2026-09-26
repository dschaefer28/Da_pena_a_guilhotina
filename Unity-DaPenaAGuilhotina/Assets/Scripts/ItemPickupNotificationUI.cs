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
    [Tooltip("Use {0} no lugar onde o nome do item deve aparecer. Aceita rich text do TMP.")]
    [TextArea(2, 3)]
    public string formatoMensagem = "<size=75%><color=#FAD98C>Você recebeu</color></size>\n{0}";
    [Tooltip("Por quantos segundos o popup fica visível antes de sumir sozinho.")]
    [Min(0.1f)] public float duracaoExibicao = 2.5f;
    [Tooltip("Largura máxima do texto: mensagens curtas deixam o popup estreito, as longas quebram linha aqui.")]
    [Min(100f)] public float larguraMaximaTexto = 520f;
    [Tooltip("Duração do fade de entrada/saída (0 = aparece e some seco).")]
    [Min(0f)] public float duracaoFade = 0.2f;

    [Tooltip("Espaço entre o relógio de investigação (HudDoRelogio) e o popup, quando os dois estão no topo.")]
    [Min(0f)] public float margemAbaixoDoRelogio = 12f;

    private InventoryManager inventoryManagerAtual;
    private float? alturaBasePopup;
    // Itens recebidos e avisos de texto (AvisoNaTela) dividem a mesma fila, para nunca se sobreporem.
    private readonly Queue<(string texto, Sprite icone)> filaDeItens = new Queue<(string, Sprite)>();
    private Coroutine exibicaoEmAndamento;

    void Start()
    {
        if (painelPopup != null) painelPopup.SetActive(false);
        VincularInventario();
    }

    void OnEnable()
    {
        VincularInventario();
        AvisoNaTela.OnAviso += HandleAviso;
    }

    void OnDisable()
    {
        if (inventoryManagerAtual != null)
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
        // Esquece a referência: senão, ao reativar com o mesmo inventário, VincularInventario achava que já
        // estava inscrito e o popup parava de aparecer.
        inventoryManagerAtual = null;
        AvisoNaTela.OnAviso -= HandleAviso;
        // Desativar mata a corrotina; sem zerar isso a fila nunca mais andaria.
        exibicaoEmAndamento = null;
        filaDeItens.Clear();
        if (painelPopup != null) painelPopup.SetActive(false);
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
        Enfileirar(string.Format(formatoMensagem, item.NomeExibicao), item.itemImg);
    }

    private void HandleAviso(string texto, Sprite icone) => Enfileirar(texto, icone);

    private void Enfileirar(string texto, Sprite icone)
    {
        // Objeto inativo não roda corrotina (ex: UI da cena antiga durante a troca de cena).
        if (!isActiveAndEnabled) return;

        filaDeItens.Enqueue((texto, icone));
        if (exibicaoEmAndamento == null)
            exibicaoEmAndamento = StartCoroutine(ExibirFila());
    }

    // Fila garante que, se dois itens forem recebidos quase juntos, o segundo popup só
    // aparece depois que o primeiro terminar (em vez de sobrescrever ou piscar).
    private IEnumerator ExibirFila()
    {
        while (filaDeItens.Count > 0)
        {
            var (texto, icone) = filaDeItens.Dequeue();
            MostrarPopup(texto, icone);
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(duracaoExibicao);
            yield return Fade(1f, 0f);
            if (painelPopup != null) painelPopup.SetActive(false);
        }
        exibicaoEmAndamento = null;
    }

    private IEnumerator Fade(float de, float para)
    {
        CanvasGroup grupo = painelPopup != null ? painelPopup.GetComponent<CanvasGroup>() : null;
        if (grupo == null) yield break;
        for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(de, para, t / duracaoFade);
            yield return null;
        }
        grupo.alpha = para;
    }

    private void MostrarPopup(string texto, Sprite icone)
    {
        if (imagemItem != null)
        {
            imagemItem.sprite = icone;
            imagemItem.gameObject.SetActive(icone != null);
        }

        if (textoMensagem != null)
        {
            textoMensagem.text = texto;
            // O popup se ajusta ao texto (ContentSizeFitter), mas sem limite uma frase longa viraria uma
            // linha só atravessando a tela: a largura do texto é a da frase, até larguraMaximaTexto.
            LayoutElement le = textoMensagem.GetComponent<LayoutElement>();
            if (le != null) le.preferredWidth = Mathf.Min(textoMensagem.GetPreferredValues(texto).x + 2f, larguraMaximaTexto);
        }

        if (painelPopup != null)
        {
            painelPopup.SetActive(true);
            // Recalcula já, para o primeiro frame não aparecer com o tamanho da mensagem anterior.
            var rect = painelPopup.transform as RectTransform;
            if (rect != null)
            {
                // Relógio de investigação no topo (mesmo lugar): o popup desce para baixo dele.
                if (!alturaBasePopup.HasValue) alturaBasePopup = rect.anchoredPosition.y;
                float borda = HudDoRelogio.BordaInferior;
                float y = borda > 0f ? Mathf.Min(alturaBasePopup.Value, -(borda + margemAbaixoDoRelogio)) : alturaBasePopup.Value;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
        }
    }
}
