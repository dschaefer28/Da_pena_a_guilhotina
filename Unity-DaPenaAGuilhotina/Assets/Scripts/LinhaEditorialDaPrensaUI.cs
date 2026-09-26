using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Escolha da linha editorial na prensa (Prompt 5). Quando o jogador aperta Misturar com duas pistas de um caso que
/// exige linha editorial, a CraftingPress valida tudo, não consome nada e pede a escolha (OnLinhaEditorialPedida).
/// Esta janela cobre o painel da prensa — as barras de Povo/Estado continuam visíveis — com as três opções e
/// "Cancelar". Escolher chama CraftingPress.ImprimirComLinhaEditorial, que valida de novo antes de consumir.
///
/// Cancelar, Esc, fechar o inventário ou trocar de cena só fecham a janela: nada foi gasto e nenhuma escolha fica
/// guardada para outra impressão ou outro caso. Os textos mostram só a regra de cada linha (valores da receita, iguais
/// em qualquer versão): a janela recebe apenas a receita, então não pode revelar a verdade das pistas.
/// Mouse/toque nos botões; teclado: 1, 2, 3 escolhem, Esc cancela, setas + Enter navegam.
/// Vai no PainelPrensa (UI.prefab), ao lado do SuportesDaPrensaUI, e se monta por código na primeira vez.
/// </summary>
[RequireComponent(typeof(CraftingPress))]
public class LinhaEditorialDaPrensaUI : MonoBehaviour
{
    public TMP_FontAsset fonte;

    [Header("Textos (provisórios, editáveis)")]
    public string titulo = "Linha editorial";
    [TextArea(1, 3)] public string pergunta = "Como o panfleto vai contar este caso?";
    [TextArea(1, 3)] public string descricaoDefesaDoPovo = "Toma o partido das ruas contra os poderosos.";
    [TextArea(1, 3)] public string descricaoAgradarOPoder = "Escreve o que quem governa quer ler.";
    [TextArea(1, 3)] public string descricaoSensacionalista = "Manchete escandalosa: vende mais. Se algo for falso, o desmentido pesa mais.";
    [TextArea(1, 3)] public string nota = "O tom muda o panfleto, não a verdade das pistas.";

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);
    // Opaco: no espaço de cor Linear do projeto, um fundo escuro com alpha 0,97 ainda deixa ver os rótulos da prensa.
    private static readonly Color CorFundo = new Color(0.11f, 0.08f, 0.07f, 1f);
    private static readonly Color CorBotao = new Color(0.25f, 0.18f, 0.13f);

    private CraftingPress prensa;
    private GameObject janela;
    private TextMeshProUGUI textoTitulo;
    private readonly Button[] opcoes = new Button[3];
    private readonly TextMeshProUGUI[] rotulos = new TextMeshProUGUI[3];
    private Button botaoCancelar;

    /// <summary>A janela está aberta esperando a escolha.</summary>
    public bool Aberta => janela != null && janela.activeSelf;

    private void Awake() => prensa = GetComponent<CraftingPress>();

    private void OnEnable() => prensa.OnLinhaEditorialPedida += Abrir;

    private void OnDisable()
    {
        if (prensa != null) prensa.OnLinhaEditorialPedida -= Abrir;
        Fechar(); // inventário/prensa fechados com a janela aberta: nada foi consumido, nada fica escolhido
    }

    private void Update()
    {
        if (!Aberta || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        if (k.escapeKey.wasPressedThisFrame) Fechar();
        else if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[0]);
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[1]);
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[2]);
    }

    private void Abrir(ReceitaDeCaso receita)
    {
        if (receita == null) return;
        if (janela == null) Montar();

        textoTitulo.text = $"<b>{titulo}</b>\n<size=75%><color=#B0A48C>{pergunta}</color></size>";
        for (int i = 0; i < LinhasEditoriais.Opcoes.Length; i++)
            rotulos[i].text = TextoDaOpcao(i + 1, LinhasEditoriais.Opcoes[i], receita);

        janela.transform.SetAsLastSibling(); // por cima dos slots e do botão Misturar
        janela.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoCancelar.gameObject);
    }

    /// <summary>Opção escolhida (botão ou tecla). Fecha antes de imprimir: um segundo clique não chega à prensa, e se
    /// chegasse a regra de publicação única recusaria.</summary>
    public void Escolher(LinhaEditorial linha)
    {
        if (!Aberta) return;
        Fechar();
        prensa.ImprimirComLinhaEditorial(linha);
    }

    public void Fechar()
    {
        if (!Aberta) return;
        janela.SetActive(false);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(janela.transform))
            EventSystem.current.SetSelectedGameObject(null);
    }

    // Só regra conhecida: rótulo da fase, descrição e o modificador configurado na receita (o mesmo em qualquer versão).
    private string TextoDaOpcao(int numero, LinhaEditorial linha, ReceitaDeCaso receita)
    {
        ReceitaDeCaso.ConfiguracaoEditorial config = receita.linhaEditorial;
        ReceitaDeCaso.ModificadorEditorial mod = config != null ? config.De(linha) : null;
        var sb = new StringBuilder($"<b>{numero}. {LinhasEditoriais.Rotulo(linha, receita)}</b>\n<size=78%>");
        sb.Append(linha == LinhaEditorial.DefesaDoPovo ? descricaoDefesaDoPovo
                : linha == LinhaEditorial.AgradarOPoder ? descricaoAgradarOPoder : descricaoSensacionalista);
        sb.Append("</size>\n<size=78%>");
        if (mod != null)
        {
            Efeito(sb, mod.povo, "Povo");
            Efeito(sb, mod.estado, "Estado");
            Efeito(sb, mod.ouro, "Ouro");
        }
        if (linha == LinhaEditorial.Sensacionalista && config != null && config.agravamentoPercentual > 0)
            sb.Append($"<color=#E0A040>desmentido +{config.agravamentoPercentual}%</color>");
        sb.Append("</size>");
        return sb.ToString();
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
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; // cobre o painel da prensa
        janela.GetComponent<Image>().color = CorFundo; // e bloqueia o clique nos slots/Misturar por baixo
        var v = janela.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(22, 22, 22, 22); v.spacing = 12f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        textoTitulo = Texto("Titulo", janela.transform, 32f);
        for (int i = 0; i < opcoes.Length; i++)
        {
            LinhaEditorial linha = LinhasEditoriais.Opcoes[i];
            opcoes[i] = Botao(janela.transform, "Opcao_" + linha, 108f, out rotulos[i]);
            opcoes[i].onClick.AddListener(() => Escolher(linha));
        }
        Texto("Nota", janela.transform, 20f).text = $"<color=#B0A48C>{nota}</color>";
        botaoCancelar = Botao(janela.transform, "Cancelar", 72f, out TextMeshProUGUI rotuloCancelar);
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

    // Botão que cresce com o texto (altura mínima boa para toque).
    private Button Botao(Transform pai, string nome, float alturaMinima, out TextMeshProUGUI rotulo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        go.layer = gameObject.layer;
        go.transform.SetParent(pai, false);
        go.GetComponent<Image>().color = CorBotao;
        go.GetComponent<LayoutElement>().minHeight = alturaMinima;
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 10, 10);
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = true;
        v.childAlignment = TextAnchor.MiddleLeft;
        rotulo = Texto("Rotulo", go.transform, 24f);
        rotulo.alignment = TextAlignmentOptions.MidlineLeft;
        return go.GetComponent<Button>();
    }
}
