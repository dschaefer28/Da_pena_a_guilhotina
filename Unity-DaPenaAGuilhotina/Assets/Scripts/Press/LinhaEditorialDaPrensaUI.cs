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
/// em qualquer versão), marcados como ajuste somado ao panfleto: a janela recebe apenas a receita, então não pode
/// revelar a verdade das pistas.
/// Depois da impressão (CraftingPress.OnPanfletoDeCasoPublicado), a mesma janela mostra as parcelas do total — pistas,
/// apoio e linha — e o que entrou nas barras. Não nomeia a versão nem mostra a penalidade de um desmentido futuro.
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
    [TextArea(1, 3)] public string nota = "Valores somados ao efeito das pistas e do apoio. O tom não muda a verdade das pistas.";
    [Tooltip("Antes dos valores de cada opção: eles são somados ao efeito das pistas e do apoio, não são o total. Curto: " +
             "a janela está no limite de altura, e um rótulo que quebre a linha empurra o conteúdo para fora do painel.")]
    public string rotuloDoAjuste = "Extra:";

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
    // Opaco: no espaço de cor Linear do projeto, um fundo escuro com alpha 0,97 ainda deixa ver os rótulos da prensa.
    private static readonly Color CorFundo = new Color(0.11f, 0.08f, 0.07f, 1f);
    private static readonly Color CorBotao = new Color(0.25f, 0.18f, 0.13f);

    private CraftingPress prensa;
    private GameObject janela;
    private TextMeshProUGUI textoTitulo;
    private readonly Button[] opcoes = new Button[3];
    private readonly TextMeshProUGUI[] rotulos = new TextMeshProUGUI[3];
    private Button botaoCancelar;
    private TextMeshProUGUI rotuloCancelar;
    private TextMeshProUGUI textoNota;
    private TextMeshProUGUI textoResultado;
    private bool mostrandoResultado;

    /// <summary>A janela está aberta (esperando a escolha ou mostrando o resultado).</summary>
    public bool Aberta => janela != null && janela.activeSelf;

    /// <summary>A janela aberta mostra o resultado de uma impressão, não as opções.</summary>
    public bool MostrandoResultado => Aberta && mostrandoResultado;

    private void Awake() => prensa = GetComponent<CraftingPress>();

    private void OnEnable()
    {
        prensa.OnLinhaEditorialPedida += Abrir;
        prensa.OnPanfletoDeCasoPublicado += MostrarResultado;
        PedirDicaSeOCasoUsaLinha();
    }

    // Prompt 7: na primeira vez que a prensa abre com um caso que pede linha editorial, a dica explica a escolha que vem
    // ao apertar Misturar. Pedida aqui, e não quando a janela abre: com a janela aberta o "Cancelar" tem o foco do
    // teclado, e o Enter que fecha a dica também cancelaria a escolha.
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
        Fechar(); // inventário/prensa fechados com a janela aberta: nada foi consumido, nada fica escolhido
    }

    private void Update()
    {
        if (!Aberta || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        if (k.escapeKey.wasPressedThisFrame) Fechar();
        else if (mostrandoResultado) return; // o resultado só fecha (Esc, Enter ou Continuar)
        else if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[0]);
        else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[1]);
        else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Escolher(LinhasEditoriais.Opcoes[2]);
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

    /// <summary>Opção escolhida (botão ou tecla). Fecha antes de imprimir: um segundo clique não chega à prensa, e se
    /// chegasse a regra de publicação única recusaria.</summary>
    public void Escolher(LinhaEditorial linha)
    {
        if (!Aberta || mostrandoResultado) return;
        Fechar();
        prensa.ImprimirComLinhaEditorial(linha);
    }

    // Panfleto de caso publicado: a mesma janela volta com as parcelas do total.
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
        textoNota.gameObject.SetActive(!resultado); // a nota explica as opções; o resultado já diz que o total é a soma
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

    // Só regra conhecida: rótulo da fase, descrição e o modificador configurado na receita (o mesmo em qualquer versão).
    private string TextoDaOpcao(int numero, LinhaEditorial linha, ReceitaDeCaso receita)
    {
        ReceitaDeCaso.ConfiguracaoEditorial config = receita.linhaEditorial;
        ReceitaDeCaso.ModificadorEditorial mod = config != null ? config.De(linha) : null;
        var sb = new StringBuilder($"<b>{numero}. {LinhasEditoriais.Rotulo(linha, receita)}</b>\n<size=85%>");
        sb.Append(linha == LinhaEditorial.DefesaDoPovo ? descricaoDefesaDoPovo
                : linha == LinhaEditorial.AgradarOPoder ? descricaoAgradarOPoder : descricaoSensacionalista);
        sb.Append("</size>\n<size=85%>");
        if (mod != null)
        {
            // Os valores da linha não são o efeito do panfleto: são somados ao das pistas e do apoio.
            if (!mod.Zerado && !string.IsNullOrWhiteSpace(rotuloDoAjuste))
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

    // Parcelas do total, na ordem do cálculo (CalculadoraDePanfleto): pistas → apoio → linha. A versão nunca é nomeada
    // e a penalidade de um desmentido futuro não aparece; o limite 0–100 só é citado quando cortou parte do total.
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
        sb.Append("\n<size=40%> </size>"); // respiro antes do total
        Parcela(sb, rotuloTotal, r.povo, r.estado, r.ouro);
        Limite(sb, "Povo", r.povo, aplicado.x);
        Limite(sb, "Estado", r.estado, aplicado.y);
        return sb.ToString();
    }

    // Rótulo numa linha e valores na seguinte: com os três valores na mesma linha do rótulo, a largura da janela
    // quebrava a linha no meio de um valor.
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
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; // cobre o painel da prensa
        janela.GetComponent<Image>().color = CorFundo; // e bloqueia o clique nos slots/Misturar por baixo
        var v = janela.GetComponent<VerticalLayoutGroup>();
        // Margens verticais menores: com 22/12 o conteúdo pedia 747 de altura num painel de 720, e o título e a nota
        // eram espremidos até passar da própria caixa.
        v.padding = new RectOffset(22, 22, 16, 16); v.spacing = 8f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // Tamanhos revistos com a EB Garamond (27/10/2026): nada abaixo de ~22 na referência de 1080 (15 px em 720).
        textoTitulo = Texto("Titulo", janela.transform, 34f);
        textoResultado = Texto("Resultado", janela.transform, 27f); // no lugar das opções, depois da impressão
        textoResultado.GetComponent<LayoutElement>().flexibleHeight = 1f; // ocupa a sobra: o botão fica embaixo
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
        botaoCancelar.GetComponent<LayoutElement>().flexibleHeight = 0f; // sem as opções sobra espaço: não esticar
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
        return go.GetComponent<Button>();
    }
}
