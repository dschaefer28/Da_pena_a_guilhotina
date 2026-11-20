using UnityEngine;

/// <summary>
/// Mantém a HUD de status (ancorada no canto superior esquerdo) dentro da área segura; a posição do prefab é a margem
/// base e o recuo da área segura é somado a ela.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HudAreaSegura : MonoBehaviour
{
    private RectTransform rect;
    private Canvas canvas;
    private Vector2 posicaoBase;
    private Rect ultimaArea;
    private Vector2Int ultimaTela;
    private float ultimaEscala;

    private void Awake()
    {
        rect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        posicaoBase = rect.anchoredPosition;
    }

    private void OnEnable() => Aplicar();

    // A área segura muda ao girar o aparelho ou redimensionar a janela.
    private void Update()
    {
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        if (Screen.safeArea != ultimaArea || Screen.width != ultimaTela.x || Screen.height != ultimaTela.y || !Mathf.Approximately(escala, ultimaEscala))
            Aplicar();
    }

    private void Aplicar()
    {
        if (rect == null) return;
        Rect area = Screen.safeArea;
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        ultimaArea = area;
        ultimaTela = new Vector2Int(Screen.width, Screen.height);
        ultimaEscala = escala;
        if (escala <= 0f) return;

        float esquerda = area.xMin / escala;
        float topo = (Screen.height - area.yMax) / escala;
        rect.anchoredPosition = posicaoBase + new Vector2(esquerda, -topo);
    }
}
