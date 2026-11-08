using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mostra a FichaDaPista com um texto pronto enquanto o ponteiro está sobre este elemento de UI (as provas do tribunal,
/// que não são slots de inventário). Só trata entrar e sair: não intercepta o clique nem a rolagem do ScrollRect, como
/// um EventTrigger faria.
/// </summary>
public class FichaAoPassar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(3, 8)] public string texto;

    public void OnPointerEnter(PointerEventData eventData) => FichaDaPista.MostrarTexto(texto, transform as RectTransform);

    public void OnPointerExit(PointerEventData eventData) => FichaDaPista.Esconder(transform as RectTransform);

    private void OnDisable() => FichaDaPista.Esconder(transform as RectTransform);
}
