using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mostra as horas de investigação restantes no topo da tela, só enquanto o RelogioDeInvestigacao estiver ativo
/// (cena de investigação do caso em andamento). Fica no prefab UI e se monta por código no primeiro uso.
/// </summary>
public class HudDoRelogio : MonoBehaviour
{
    public TMP_FontAsset fonte;
    [Tooltip("{0} = horas restantes, {1} = horas do caso.")]
    public string formato = "Tempo de investigação: {0}h / {1}h";

    private GameObject painel;
    private TextMeshProUGUI texto;
    private static HudDoRelogio atual;

    /// <summary>Distância (unidades do canvas) do topo da tela até a base do relógio, ou 0 se ele não está na tela.
    /// O popup de item recebido (mesmo lugar, topo central) desce para não cobrir as horas.</summary>
    public static float BordaInferior
    {
        get
        {
            if (atual == null || atual.painel == null || !atual.painel.activeInHierarchy) return 0f;
            var rt = (RectTransform)atual.painel.transform;
            return -rt.anchoredPosition.y + rt.sizeDelta.y;
        }
    }

    private void OnEnable()
    {
        atual = this;
        RelogioDeInvestigacao.OnHorasMudaram += Atualizar;
    }

    private void OnDisable()
    {
        if (atual == this) atual = null;
        RelogioDeInvestigacao.OnHorasMudaram -= Atualizar;
    }

    private void Start() => Atualizar();

    private void Atualizar()
    {
        bool ativo = RelogioDeInvestigacao.Ativo;
        if (!ativo)
        {
            if (painel != null) painel.SetActive(false);
            return;
        }

        if (painel == null) Montar();
        GameManager gm = GameManager.Instance;
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_TEMPO, TutorialManager.Instance.dicaTempo);
        painel.SetActive(true);
        texto.text = string.Format(formato, gm.horasRestantes, gm.horasDoCaso);
        texto.color = gm.horasRestantes <= 1 ? new Color(0.95f, 0.45f, 0.35f) : new Color(0.98f, 0.85f, 0.55f);
    }

    private void Montar()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform pai = canvas != null ? canvas.rootCanvas.transform : transform;

        painel = new GameObject("HudDoRelogio", typeof(RectTransform), typeof(Image));
        painel.layer = gameObject.layer;
        painel.transform.SetParent(pai, false);
        var rt = (RectTransform)painel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(520f, 56f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        var fundo = painel.GetComponent<Image>();
        fundo.color = new Color(0.08f, 0.05f, 0.04f, 0.85f);
        fundo.raycastTarget = false;

        var t = new GameObject("Texto", typeof(RectTransform));
        t.transform.SetParent(painel.transform, false);
        var rtT = (RectTransform)t.transform;
        rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one; rtT.offsetMin = rtT.offsetMax = Vector2.zero;
        texto = t.AddComponent<TextMeshProUGUI>();
        if (fonte != null) texto.font = fonte;
        texto.fontSize = 28f;
        texto.enableAutoSizing = true; texto.fontSizeMin = 16f; texto.fontSizeMax = 28f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
    }
}
