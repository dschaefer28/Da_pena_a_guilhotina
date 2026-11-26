using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Janela da linha editorial sobre o painel da prensa: responde a OnLinhaEditorialPedida e, após a impressão, mostra as
/// parcelas do total. Fechar nunca consome nem guarda escolha. Só recebe a receita, então não pode revelar a verdade
/// das pistas (nem nomeia a versão ou a penalidade futura). Teclado: 1/2 escolhem, Esc cancela.
/// Vai no PainelPrensa (UI.prefab) e se monta por código na primeira abertura.
/// </summary>
[RequireComponent(typeof(CraftingPress))]
public class LinhaEditorialDaPrensaUI : MonoBehaviour
{
    public TMP_FontAsset fonte;

    [Header("Textos (provisórios, editáveis)")]
    public string titulo = "Linha editorial";
    [TextArea(1, 3)] public string pergunta = "Como o panfleto vai contar este caso?";
    [TextArea(1, 3)] public string descricaoNeutro = DescricaoNeutroPadrao;
    [TextArea(1, 3)] public string descricaoSensacionalista = DescricaoSensacionalistaPadrao;
    [TextArea(1, 3)] public string nota = "Valores somados ao efeito das pistas e do apoio. O tom não muda a verdade das pistas.";
    [Tooltip("Antes dos valores de cada opção: eles são somados ao efeito das pistas e do apoio, não são o total. Curto: " +
             "um rótulo que quebre a linha empurra o conteúdo para fora do painel.")]
    public string rotuloDoAjuste = RotuloDoAjustePadrao;
    [Tooltip("No lugar dos valores, na opção sem ajuste (Neutro).")]
    public string textoSemAjuste = "sem ajuste";

    // Também usados pela ferramenta Ferramentas > Campanha > 4b para atualizar o UI.prefab.
    public const string DescricaoNeutroPadrao = "Conta o caso sem exagero: o panfleto tem o efeito normal do caso.";
    public const string DescricaoSensacionalistaPadrao = "Manchete escandalosa: vende mais, mas convence menos o lado que o caso defende.";
    public const string RotuloDoAjustePadrao = "Ajuste:";

    [Header("Resultado da impressão")]
    public string tituloDoResultado = "Panfleto impresso";
    [TextArea(1, 3)] public string subtituloDoResultado = "O efeito total é a soma destas partes.";
    public string rotuloPistas = "Pistas";
    public string rotuloApoio = "Apoio";
    [Tooltip("Usado quando um apoio só vale em algumas versões: separado, ele mostraria se as pistas eram verdadeiras.")]
    public string rotuloPistasEApoio = "Pistas e apoio";
    public string rotuloTotal = "Total";
    [Tooltip("Quando o limite 0–100 cortou parte do total. {0} = barra, {1} = o que mudou de fato, {2} = limite.")]
    public string textoLimite = "{0} na barra: {1} (limite {2})";

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);
    // Opaco: em espaço Linear, alpha 0,97 ainda deixa ver os rótulos da prensa.
    private static readonly Color CorFundo = new Color(0.11f, 0.08f, 0.07f, 1f);
    private static readonly Color CorBotao = new Color(0.25f, 0.18f, 0.13f);

    private CraftingPress prensa;
    private GameObject janela;
    private TextMeshProUGUI textoTitulo;
    private readonly Button[] opcoes = new Button[LinhasEditoriais.Opcoes.Length];
    private readonly TextMeshProUGUI[] rotulos = new TextMeshProUGUI[LinhasEditoriais.Opcoes.Length];
    private Button botaoCancelar;
    private TextMeshProUGUI rotuloCancelar;
    private TextMeshProUGUI textoNota;
    private TextMeshProUGUI textoResultado;
    private bool mostrandoResultado;

    public bool Aberta => janela != null && janela.activeSelf;

    public bool MostrandoResultado => Aberta && mostrandoResultado;

    private void Awake() => prensa = GetComponent<CraftingPress>();

    private void OnEnable()
    {
        prensa.OnLinhaEditorialPedida += Abrir;
        prensa.OnPanfletoDeCasoPublicado += MostrarResultado;
        PedirDicaSeOCasoUsaLinha();
    }

    // Dica pedida ao abrir a prensa, não a janela: lá o "Cancelar" tem o foco e o Enter que fecha a dica cancelaria a escolha.
    private static void PedirDicaSeOCasoUsaLinha()
    {
        GameManager gm = GameManager.Instance;
        TutorialManager tm = TutorialManager.Instance;
        if (gm == null || tm == null || !gm.CasoAtualEmAndamento) return;
        ReceitaDeCaso receita = gm.casoEscolhido.receitaDoPanfleto;
        if (receita != null && receita.ExigeLinhaEditorial)
            tm.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_LINHA_EDITORIAL, tm.dicaLinhaEditorial);
    }

    private void OnDisable()
    {
        if (prensa != null)
        {
            prensa.OnLinhaEditorialPedida -= Abrir;
            prensa.OnPanfletoDeCasoPublicado -= MostrarResultado;
        }
        Fechar();
    }

    private void Update()
    {
        if (!Aberta || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        if (k.escapeKey.wasPressedThisFrame) Fechar();
        else if (mostrandoResultado) return; // o resultado só fecha (Esc, Enter ou Continuar)
        else if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[0]);
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[1]);
    }

    private void Abrir(ReceitaDeCaso receita)
    {
        if (receita == null) return;
        if (janela == null) Montar();

        textoTitulo.text = $"<b>{titulo}</b>\n<size=80%><color=#B0A48C>{pergunta}</color></size>";
        for (int i = 0; i < LinhasEditoriais.Opcoes.Length; i++)
            rotulos[i].text = TextoDaOpcao(i + 1, LinhasEditoriais.Opcoes[i], receita);
        MudarModo(false);
        Exibir();
    }

    /// <summary>Fecha antes de imprimir para um segundo clique não chegar à prensa.</summary>
    public void Escolher(LinhaEditorial linha)
    {
        if (!Aberta || mostrandoResultado) return;
        Fechar();
        prensa.ImprimirComLinhaEditorial(linha);
    }

    private void MostrarResultado(ReceitaDeCaso receita, ResultadoDoPanfleto resultado, Vector3Int aplicado)
    {
        if (resultado == null) return;
        if (janela == null) Montar();

        textoTitulo.text = $"<b>{tituloDoResultado}</b>\n<size=80%><color=#B0A48C>{subtituloDoResultado}</color></size>";
        textoResultado.text = TextoDoResultado(receita, resultado, aplicado);
        MudarModo(true);
        Exibir();
    }

    private void MudarModo(bool resultado)
    {
        mostrandoResultado = resultado;
        foreach (Button opcao in opcoes) opcao.gameObject.SetActive(!resultado);
        textoNota.gameObject.SetActive(!resultado);
        textoResultado.gameObject.SetActive(resultado);
        rotuloCancelar.text = resultado ? "<b>Continuar</b>" : "<b>Cancelar</b>";
    }

    private void Exibir()
    {
        janela.transform.SetAsLastSibling(); // por cima dos slots e do botão Misturar
        janela.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoCancelar.gameObject);
    }

    public void Fechar()
    {
        if (!Aberta) return;
        janela.SetActive(false);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(janela.transform))
            EventSystem.current.SetSelectedGameObject(null);
    }

    // Só regra conhecida: o modificador da receita é o mesmo em qualquer versão.
    private string TextoDaOpcao(int numero, LinhaEditorial linha, ReceitaDeCaso receita)
    {
        ReceitaDeCaso.ConfiguracaoEditorial config = receita.linhaEditorial;
        ReceitaDeCaso.ModificadorEditorial mod = config != null ? config.De(linha) : null;
        var sb = new StringBuilder($"<b>{numero}. {LinhasEditoriais.Rotulo(linha, receita)}</b>\n<size=85%>");
        sb.Append(linha == LinhaEditorial.Sensacionalista ? descricaoSensacionalista : descricaoNeutro);
        sb.Append("</size>\n<size=85%>");
        if (mod == null || mod.Zerado)
        {
            if (!string.IsNullOrWhiteSpace(textoSemAjuste)) sb.Append($"<color=#9E9E9E>{textoSemAjuste.Trim()}</color>   ");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(rotuloDoAjuste))
                sb.Append($"<color=#B0A48C>{rotuloDoAjuste.Trim()}</color> ");
            Efeito(sb, mod.povo, "Povo");
            Efeito(sb, mod.estado, "Estado");
            Efeito(sb, mod.ouro, "Ouro");
        }
        if (linha == LinhaEditorial.Sensacionalista && config != null && config.agravamentoPercentual > 0)
            sb.Append($"<color=#E0A040>desmentido +{config.agravamentoPercentual}%</color>");
        sb.Append("</size>");
        return sb.ToString();
    }

    // Ordem do cálculo: pistas → apoio → linha. O limite 0–100 só é citado quando cortou parte do total.
    private string TextoDoResultado(ReceitaDeCaso receita, ResultadoDoPanfleto r, Vector3Int aplicado)
    {
        var sb = new StringBuilder();
        // Um apoio que só vale em algumas versões, mostrado à parte, diria se as pistas eram verdadeiras.
        bool apoioDependeDaVersao = receita != null && receita.qualificadores != null &&
                                    receita.qualificadores.Exists(q => q != null && q.niveis != null && q.niveis.Count > 0);
        if (apoioDependeDaVersao)
            Parcela(sb, rotuloPistasEApoio, r.VersaoPovo + r.apoioPovo, r.VersaoEstado + r.apoioEstado, r.VersaoOuro + r.apoioOuro);
        else
        {
            Parcela(sb, rotuloPistas, r.VersaoPovo, r.VersaoEstado, r.VersaoOuro);
            if (r.qualificadores.Count > 0) Parcela(sb, rotuloApoio, r.apoioPovo, r.apoioEstado, r.apoioOuro);
        }
        if (LinhasEditoriais.Escolhivel(r.linha))
            Parcela(sb, LinhasEditoriais.Rotulo(r.linha, receita), r.editorialPovo, r.editorialEstado, r.editorialOuro);
        sb.Append("\n<size=40%> </size>");
        Parcela(sb, rotuloTotal, r.povo, r.estado, r.ouro);
        Limite(sb, "Povo", r.povo, aplicado.x);
        Limite(sb, "Estado", r.estado, aplicado.y);
        return sb.ToString();
    }

    // Valores na linha de baixo: na mesma linha do rótulo, a quebra caía no meio de um valor.
    private static void Parcela(StringBuilder sb, string rotulo, int povo, int estado, int ouro)
    {
        if (sb.Length > 0) sb.Append('\n');
        sb.Append($"<b>{rotulo}</b>\n<size=85%>");
        if (povo == 0 && estado == 0 && ouro == 0) sb.Append("<color=#9E9E9E>sem efeito</color>");
        Efeito(sb, povo, "Povo");
        Efeito(sb, estado, "Estado");
        Efeito(sb, ouro, "Ouro");
        sb.Append("</size>");
    }

    private void Limite(StringBuilder sb, string barra, int calculado, int aplicado)
    {
        if (calculado == aplicado || string.IsNullOrWhiteSpace(textoLimite)) return;
        string mudou = aplicado > 0 ? $"+{aplicado}" : aplicado.ToString();
        sb.Append("\n<size=85%><color=#B0A48C>").Append(string.Format(textoLimite, barra, mudou, calculado > 0 ? 100 : 0)).Append("</color></size>");
    }

    private static void Efeito(StringBuilder sb, int valor, string nome)
    {
        if (valor > 0) sb.Append($"<color=#4CAF50>▲ +{valor} {nome}</color>   ");
        else if (valor < 0) sb.Append($"<color=#E53935>▼ {valor} {nome}</color>   ");
    }

    // ===== Montagem =====

    private void Montar()
    {
        if (fonte == null)
        {
            var modelo = GetComponentInChildren<TextMeshProUGUI>(true);
            if (modelo != null) fonte = modelo.font;
        }

        janela = new GameObject("SeletorLinhaEditorial", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        janela.layer = gameObject.layer;
        janela.transform.SetParent(transform, false);
        var rt = (RectTransform)janela.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        janela.GetComponent<Image>().color = CorFundo; // bloqueia o clique nos slots por baixo
        var v = janela.GetComponent<VerticalLayoutGroup>();
        // Margens verticais curtas: o conteúdo mal cabe no painel de 720.
        v.padding = new RectOffset(22, 22, 16, 16); v.spacing = 8f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // Mínimo de ~22 na referência de 1080 (EB Garamond).
        textoTitulo = Texto("Titulo", janela.transform, 34f);
        textoResultado = Texto("Resultado", janela.transform, 27f);
        textoResultado.GetComponent<LayoutElement>().flexibleHeight = 1f; // empurra o botão para baixo
        textoResultado.gameObject.SetActive(false);
        for (int i = 0; i < opcoes.Length; i++)
        {
            LinhaEditorial linha = LinhasEditoriais.Opcoes[i];
            opcoes[i] = Botao(janela.transform, "Opcao_" + linha, 108f, out rotulos[i]);
            opcoes[i].onClick.AddListener(() => Escolher(linha));
        }
        textoNota = Texto("Nota", janela.transform, 23f);
        textoNota.text = $"<color=#B0A48C>{nota}</color>";
        botaoCancelar = Botao(janela.transform, "Cancelar", 72f, out rotuloCancelar);
        botaoCancelar.GetComponent<LayoutElement>().flexibleHeight = 0f;
        rotuloCancelar.text = "<b>Cancelar</b>";
        rotuloCancelar.alignment = TextAlignmentOptions.Center;
        botaoCancelar.onClick.AddListener(Fechar);
        janela.SetActive(false);
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, float tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(LayoutElement));
        go.layer = gameObject.layer;
        go.transform.SetParent(pai, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (fonte != null) tmp.font = fonte;
        tmp.fontSize = tamanho; tmp.color = CorTexto; tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    private Button Botao(Transform pai, string nome, float alturaMinima, out TextMeshProUGUI rotulo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        go.layer = gameObject.layer;
        go.transform.SetParent(pai, false);
        bool comSprite = EstiloDeBotao.Aplicar(go.GetComponent<Image>(), go.GetComponent<Button>());
        if (!comSprite) go.GetComponent<Image>().color = CorBotao;
        go.GetComponent<LayoutElement>().minHeight = alturaMinima;
        var v = go.GetComponent<VerticalLayoutGroup>();
        // Com o sprite, o texto fica na face (fora da moldura e da sombra).
        v.padding = comSprite ? EstiloDeBotao.RecuosDoRotulo() : new RectOffset(16, 16, 10, 10);
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = true;
        v.childAlignment = TextAnchor.MiddleLeft;
        rotulo = Texto("Rotulo", go.transform, 27f);
        rotulo.alignment = TextAlignmentOptions.MidlineLeft;
        var botao = go.GetComponent<Button>();
        SomDeCliqueDoBotao.Garantir(botao);
        return botao;
    }
}
