using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// FichaDaPista com texto pronto para elementos que não são slots (provas do tribunal). Usa só enter/exit em vez de
/// EventTrigger para não interceptar clique nem rolagem do ScrollRect.
/// </summary>
public class FichaAoPassar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea(3, 8)] public string texto;

    public void OnPointerEnter(PointerEventData eventData) => FichaDaPista.MostrarTexto(texto, transform as RectTransform);

    public void OnPointerExit(PointerEventData eventData) => FichaDaPista.Esconder(transform as RectTransform);

    private void OnDisable() => FichaDaPista.Esconder(transform as RectTransform);
}
