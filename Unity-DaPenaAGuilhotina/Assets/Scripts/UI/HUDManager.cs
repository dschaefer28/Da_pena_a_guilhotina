using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Barras de Opinião (Sliders)")]
    public Slider publicOpinionSlider;
    public Slider stateOpinionSlider;

    [Header("Recursos Financeiros")]
    public TextMeshProUGUI capitalText;
    [Tooltip("{0} = valor do capital.")]
    public string formatoCapital = "Ouro: {0}";

    [Header("Feedback de mudança (opcional)")]
    [Tooltip("Textos que mostram quanto cada status mudou (ex: +15). Ficam invisíveis até a primeira mudança.")]
    public TextMeshProUGUI deltaPovoText;
    public TextMeshProUGUI deltaEstadoText;
    public TextMeshProUGUI deltaCapitalText;
    [Min(0.05f)] public float duracaoAnimacao = 0.8f;
    [Min(0.1f)] public float duracaoDelta = 2.5f;
    public Color corGanho = new Color(0.45f, 0.85f, 0.35f);
    public Color corPerda = new Color(0.95f, 0.35f, 0.3f);

    // Valores já mostrados na tela: a diferença para o GameManager é o que acabou de mudar.
    private int povoExibido, estadoExibido, capitalExibido;
    private bool jaExibiu;
    private Coroutine animCapital;

    [Tooltip("Pausa entre duas mudanças seguidas da mesma barra (ex.: o ganho do panfleto e, logo depois, o desmentido).")]
    [Min(0f)] public float pausaEntreMudancas = 0.7f;

    // Mudanças seguidas da mesma barra tocam em fila (ex.: panfleto e depois desmentido) para o jogador ver as duas.
    private class Barra
    {
        public Slider slider;
        public TextMeshProUGUI delta;
        public readonly System.Collections.Generic.Queue<Vector2Int> fila = new System.Collections.Generic.Queue<Vector2Int>();
        public bool rodando;
    }
    private Barra barraPovo, barraEstado;
    // Guardada no início: uma animação interrompida no meio do "pisca" deixaria a cor meio-branca.
    private readonly System.Collections.Generic.Dictionary<Slider, Color> corDaBarra = new System.Collections.Generic.Dictionary<Slider, Color>();

    private void Start()
    {
        if (publicOpinionSlider != null) publicOpinionSlider.interactable = false;
        if (stateOpinionSlider != null) stateOpinionSlider.interactable = false;
        GuardarCor(publicOpinionSlider);
        GuardarCor(stateOpinionSlider);
        EsconderDelta(deltaPovoText);
        EsconderDelta(deltaEstadoText);
        EsconderDelta(deltaCapitalText);
        barraPovo = new Barra { slider = publicOpinionSlider, delta = deltaPovoText };
        barraEstado = new Barra { slider = stateOpinionSlider, delta = deltaEstadoText };

        // Em Start, não Awake: o Awake do GameManager já rodou.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatusChanged += AtualizarTela;

            AtualizarTela();
        }
        else
        {
            Debug.LogWarning("[HUD] Falha: GameManager não encontrado na cena durante o Start!");
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatusChanged -= AtualizarTela;
        }
    }

    private void AtualizarTela()
    {
        var gm = GameManager.Instance;
        // Desmentido do fim da fase: já está nos números, mas a barra só desce depois da cutscene (FimDeFase).
        int povo = gm.opiniaoPublicaAtual - gm.desmentidoNaoExibido.x, estado = gm.opiniaoEstadoAtual - gm.desmentidoNaoExibido.y;
        int capital = gm.capitalAtual;

        // Primeira leitura entra direto, sem animação nem "+X".
        if (!jaExibiu)
        {
            jaExibiu = true;
            if (publicOpinionSlider != null) publicOpinionSlider.value = povo;
            if (stateOpinionSlider != null) stateOpinionSlider.value = estado;
            if (capitalText != null) capitalText.text = string.Format(formatoCapital, capital);
        }
        else
        {
            if (povo != povoExibido) Enfileirar(barraPovo, povoExibido, povo);
            if (estado != estadoExibido) Enfileirar(barraEstado, estadoExibido, estado);
            if (capital != capitalExibido) Reiniciar(ref animCapital, AnimarCapital(capitalExibido, capital));
        }

        povoExibido = povo;
        estadoExibido = estado;
        capitalExibido = capital;
        Debug.Log("[HUD] Tela atualizada com os dados do GameManager.");
    }

    private void GuardarCor(Slider barra)
    {
        if (barra != null && barra.fillRect != null && barra.fillRect.TryGetComponent(out Graphic g)) corDaBarra[barra] = g.color;
    }

    private void Reiniciar(ref Coroutine rotina, IEnumerator nova)
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = StartCoroutine(nova);
    }

    private void Enfileirar(Barra barra, int de, int para)
    {
        barra.fila.Enqueue(new Vector2Int(de, para));
        if (barra.rodando) return;
        if (!isActiveAndEnabled) { MostrarSemAnimar(barra); return; } // corrotina não roda com a HUD desligada
        barra.rodando = true;
        StartCoroutine(RodarFila(barra));
    }

    // Desligar mata as corrotinas: as barras vão direto ao valor final.
    private void OnDisable()
    {
        if (barraPovo != null) MostrarSemAnimar(barraPovo);
        if (barraEstado != null) MostrarSemAnimar(barraEstado);
    }

    private void MostrarSemAnimar(Barra barra)
    {
        int? final = null;
        while (barra.fila.Count > 0) final = barra.fila.Dequeue().y;
        barra.rodando = false;
        if (barra.slider == null) return;
        if (final.HasValue) barra.slider.value = final.Value;
        else barra.slider.value = barra == barraPovo ? povoExibido : estadoExibido;
        if (barra.slider.fillRect != null && barra.slider.fillRect.TryGetComponent(out Graphic g) && corDaBarra.TryGetValue(barra.slider, out Color cor))
            g.color = cor;
        EsconderDelta(barra.delta);
    }

    // Tempo "unscaled": a prensa e o inventário deixam o jogo em timeScale 0 justamente quando o panfleto sai.
    private IEnumerator RodarFila(Barra barra)
    {
        while (barra.fila.Count > 0)
        {
            Vector2Int passo = barra.fila.Dequeue();
            yield return MoverBarra(barra.slider, passo.x, passo.y, barra.delta);

            if (barra.fila.Count > 0)
            {
                yield return new WaitForSecondsRealtime(pausaEntreMudancas);
                continue;
            }

            // Uma mudança nova durante a espera do "+X" entra na hora.
            float fim = Time.unscaledTime + Mathf.Max(0f, duracaoDelta - duracaoAnimacao);
            while (Time.unscaledTime < fim && barra.fila.Count == 0) yield return null;
            for (float t = 0f; t < 0.4f && barra.fila.Count == 0 && barra.delta != null; t += Time.unscaledDeltaTime)
            {
                barra.delta.alpha = 1f - t / 0.4f;
                yield return null;
            }
            if (barra.fila.Count == 0) EsconderDelta(barra.delta);
        }
        barra.rodando = false;
    }

    private IEnumerator MoverBarra(Slider barra, int de, int para, TextMeshProUGUI delta)
    {
        MostrarDelta(delta, para - de);
        if (barra == null) yield break;

        Graphic preenchimento = barra.fillRect != null ? barra.fillRect.GetComponent<Graphic>() : null;
        Color corOriginal = corDaBarra.TryGetValue(barra, out Color c) ? c : Color.white;

        for (float t = 0f; t < duracaoAnimacao; t += Time.unscaledDeltaTime)
        {
            float k = t / duracaoAnimacao;
            barra.value = Mathf.Lerp(de, para, 1f - (1f - k) * (1f - k));
            if (preenchimento != null) preenchimento.color = Color.Lerp(Color.white, corOriginal, k);
            yield return null;
        }
        barra.value = para;
        if (preenchimento != null) preenchimento.color = corOriginal;
    }

    private IEnumerator AnimarCapital(int de, int para)
    {
        MostrarDelta(deltaCapitalText, para - de);
        if (capitalText != null)
        {
            for (float t = 0f; t < duracaoAnimacao; t += Time.unscaledDeltaTime)
            {
                capitalText.text = string.Format(formatoCapital, Mathf.RoundToInt(Mathf.Lerp(de, para, t / duracaoAnimacao)));
                yield return null;
            }
            capitalText.text = string.Format(formatoCapital, para);
        }
        yield return EsperarEsconder(deltaCapitalText);
    }

    private IEnumerator EsperarEsconder(TextMeshProUGUI delta)
    {
        if (delta == null) yield break;
        float restante = Mathf.Max(0f, duracaoDelta - duracaoAnimacao);
        yield return new WaitForSecondsRealtime(restante);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            delta.alpha = 1f - t / 0.4f;
            yield return null;
        }
        EsconderDelta(delta);
    }

    private void MostrarDelta(TextMeshProUGUI delta, int valor)
    {
        if (delta == null) return;
        delta.text = valor > 0 ? $"+{valor}" : valor.ToString();
        delta.color = valor >= 0 ? corGanho : corPerda;
        delta.alpha = 1f;
        delta.gameObject.SetActive(true);
    }

    private static void EsconderDelta(TextMeshProUGUI delta)
    {
        if (delta != null) delta.gameObject.SetActive(false);
    }
}
