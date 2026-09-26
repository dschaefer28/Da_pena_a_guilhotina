using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Documento (Fase 4, "Sistema de Julgamento"): o jogador apresenta as provas do caso da Fase 4 (pistas obtidas,
/// documentos comprados e o panfleto publicado) diante da bancada, e a barra de irritação dos juízes reage a cada
/// uma. A reação muda conforme a prova (fatos acalmam, boatos irritam), mas o destino do JOGADOR é travado pela
/// rota, isto é, pelas barras ao fim da Fase 3 (ilusão de agência):
///   A (Guilhotina): a última prova faz a barra explodir e o jogador é condenado;
///   B (Tirano):     a barra nunca chega perto do limite e o jogador sobrevive;
///   C (Equilíbrio): a bancada perde o interesse, a barra termina no meio e o jogador é esquecido pela história.
/// O destino do RÉU vem do panfleto do caso da Fase 4: só com fatos, ele é absolvido; com boato, calúnia ou só
/// alegações, condenado ("pista melhor, panfleto melhor"). Isso muda a fala do veredito e a 1ª linha da cutscene.
/// Depois do veredito toca a cutscene do final da rota e a tela de fim, que volta ao menu.
///
/// A HUD é montada por código (placeholder até as artes): basta este componente num objeto da cena Tribunal.
/// Aberta direto no Editor, sem rota definida, a cena usa a "Rota De Teste".
/// </summary>
public class TribunalManager : MonoBehaviour
{
    [Serializable]
    public class Juiz
    {
        public string nome;
        [Tooltip("Opcional. Sem retrato, o cartão mostra a inicial do nome.")]
        public Sprite retrato;
    }

    [Serializable]
    public class DesfechoDaRota
    {
        public RotaFinal rota;
        public string titulo;
        [TextArea(2, 4)]
        [Tooltip("Fala da bancada depois da última prova quando o panfleto do caso só tinha fatos. {juiz} = primeiro juiz da lista.")]
        public string vereditoReuAbsolvido;
        [TextArea(2, 4)]
        [FormerlySerializedAs("veredito")]
        [Tooltip("Fala da bancada quando o panfleto do caso tinha boato, calúnia ou só alegações. {juiz} = primeiro juiz da lista.")]
        public string vereditoReuCondenado;
        [TextArea(2, 4)]
        [Tooltip("1ª linha da cutscene do final quando o réu é absolvido.")]
        public string legendaReuAbsolvido;
        [TextArea(2, 4)]
        [Tooltip("1ª linha da cutscene do final quando o réu é condenado.")]
        public string legendaReuCondenado;
        [Tooltip("Linhas seguintes da cutscene, iguais para os dois destinos do réu.")]
        public List<CutsceneLegendas.Linha> legendas = new List<CutsceneLegendas.Linha>();
    }

    private class Prova
    {
        public string nome;
        public Sprite icone;
        public Confiabilidade confiabilidade;
        public Button botao;
    }

    [Header("Teste (cena aberta direto no Editor)")]
    [Tooltip("Usada quando o GameManager ainda não travou a rota (a Fase 4 não começou).")]
    public RotaFinal rotaDeTeste = RotaFinal.A_Guilhotina;
    [Tooltip("Usado quando não há panfleto publicado do caso (cena aberta direto): marcado = réu absolvido.")]
    public bool reuAbsolvidoDeTeste = true;

    [Header("Bancada")]
    public List<Juiz> juizes = new List<Juiz>
    {
        new Juiz { nome = "Robespierre" },
        new Juiz { nome = "Saint-Just" },
        new Juiz { nome = "Couthon" },
    };
    [Tooltip("Quantas provas o jogador apresenta antes do veredito (menos, se tiver menos provas).")]
    [Min(1)] public int maxArgumentos = 5;
    [Tooltip("Segundos que a barra leva para chegar ao novo valor.")]
    [Min(0.05f)] public float duracaoDaBarra = 0.8f;

    [Header("Reações da bancada ({juiz} e {prova})")]
    public List<string> reacoesFavoraveis = new List<string>
    {
        "{juiz}: Hm. Isso pesa a favor do acusado.",
        "{juiz}: A prova é sólida. Prossiga, cidadão.",
        "{juiz} anota algo e acena devagar.",
    };
    public List<string> reacoesNeutras = new List<string>
    {
        "{juiz}: Já ouvimos isso antes. Adiante.",
        "{juiz} troca um olhar com os outros juízes, sem dizer nada.",
    };
    public List<string> reacoesHostis = new List<string>
    {
        "{juiz}: Palavras bonitas não salvam ninguém neste tribunal.",
        "{juiz}: Cuidado, impressor. Defender traidores também é trair.",
        "Murmúrios na galeria. {juiz} bate o martelo, impaciente.",
    };
    [Tooltip("Argumentos usados quando o jogador chega sem nenhuma prova no inventário.")]
    public List<string> argumentosReserva = new List<string> { "Um apelo em nome da Revolução" };

    [Header("Cutscenes")]
    public List<CutsceneLegendas.Linha> abertura = new List<CutsceneLegendas.Linha>
    {
        new CutsceneLegendas.Linha { texto = "Paris, 1793. Salão do Tribunal Revolucionário.", duracao = 4f },
        new CutsceneLegendas.Linha { texto = "Na bancada, os homens do Comitê. Na galeria, o povo, que veio assistir.", duracao = 5f },
        new CutsceneLegendas.Linha { texto = "Cada prova que você apresentar será pesada. E cada palavra, lembrada.", duracao = 4f },
    };

    public List<DesfechoDaRota> desfechos = DesfechosPadrao();

    /// <summary>Textos padrão dos três finais (provisórios, para revisão do grupo). Também usados pela ferramenta
    /// de Editor que atualiza a cena Tribunal.</summary>
    public static List<DesfechoDaRota> DesfechosPadrao() => new List<DesfechoDaRota>
    {
        new DesfechoDaRota
        {
            rota = RotaFinal.A_Guilhotina,
            titulo = "Final A: A Guilhotina",
            vereditoReuAbsolvido = "{juiz}: As provas inocentam o acusado, e ele está livre. Mas quem o defendeu com tanto fervor serve a outros senhores. Prendam o impressor!",
            vereditoReuCondenado = "{juiz}: Basta! Quem defende os inimigos da Revolução é um deles. Prendam também o impressor!",
            legendaReuAbsolvido = "O réu deixa o tribunal livre, graças ao seu panfleto. Você não sai.",
            legendaReuCondenado = "O réu sobe à carroça antes de você. Suas palavras não bastaram para salvá-lo.",
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "Na mesma noite, a tipografia é lacrada e seus panfletos queimam na praça.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "Ao amanhecer, uma carroça o leva até a Place de la Révolution.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "A lâmina cai. Suas palavras continuam nas ruas; você, não.", duracao = 5f },
            }
        },
        new DesfechoDaRota
        {
            rota = RotaFinal.B_Tirano,
            titulo = "Final B: O Tirano",
            vereditoReuAbsolvido = "{juiz}: As provas são claras, e o Comitê reconhece seus serviços, cidadão. O acusado está livre. Por ora.",
            vereditoReuCondenado = "{juiz}: O Comitê reconhece seus serviços, cidadão. Quanto ao acusado, o tribunal decidirá conforme o interesse da República.",
            legendaReuAbsolvido = "O réu é absolvido. O Comitê tolera a sua defesa, porque você ainda lhe é útil.",
            legendaReuCondenado = "O réu é condenado para agradar o Estado. Ninguém pergunta o que você achava justo.",
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "Nas semanas seguintes, o Comitê encomenda à sua prensa editais, listas e denúncias.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "Você sobrevive e enriquece. Cada cabeça que rola paga um pouco do seu conforto.", duracao = 5f },
            }
        },
        new DesfechoDaRota
        {
            rota = RotaFinal.C_Equilibrio,
            titulo = "Final C: O Esquecido",
            vereditoReuAbsolvido = "{juiz}: O acusado está livre. Quanto ao impressor: nem o povo o defende, nem o Comitê o teme. Próximo caso.",
            vereditoReuCondenado = "{juiz}: O acusado está condenado. Quanto ao impressor: nem o povo o defende, nem o Comitê o teme. Próximo caso.",
            legendaReuAbsolvido = "O réu é solto. Ninguém lembra quem escreveu a defesa.",
            legendaReuCondenado = "O réu é condenado, e o seu panfleto é esquecido como você.",
            legendas = new List<CutsceneLegendas.Linha>
            {
                new CutsceneLegendas.Linha { texto = "Ninguém o condena. Ninguém o defende. O processo é arquivado sem assinatura.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "Os panfletos da tipografia deixam de circular, e os leitores passam a outros nomes.", duracao = 5f },
                new CutsceneLegendas.Linha { texto = "Você imprimiu milhares de páginas. Nenhuma delas guardou o seu nome.", duracao = 5f },
            }
        },
    };

    public string cenaDoMenu = "menu principal";

    [Header("Visual")]
    public TMP_FontAsset fonte;
    [Tooltip("Opcional: arte do salão. Vazio = fundo escuro liso.")]
    public Sprite fundo;

    private static readonly Color CorTexto = new Color(0.91f, 0.85f, 0.71f);
    private static readonly Color CorPainel = new Color(0.10f, 0.07f, 0.06f, 0.92f);
    private static readonly Color CorBotao = new Color(0.25f, 0.18f, 0.13f);
    private static readonly Color CorCalma = new Color(0.42f, 0.56f, 0.31f);
    private static readonly Color CorFuria = new Color(0.70f, 0.15f, 0.12f);
    private const float Limite = 100f;

    private RotaFinal rota;
    private CaseData caso;
    private bool reuAbsolvido;
    private readonly List<Prova> provas = new List<Prova>();
    private int totalDeArgumentos;
    private int apresentados;
    private float irritacao;
    private bool ocupado = true;

    private RectTransform raiz;
    private RectTransform barraFill;
    private Image barraImagem;
    private RectTransform barraMoldura;
    private TextMeshProUGUI textoReacao;
    private TextMeshProUGUI textoContador;
    private Button botaoEncerrar;
    private CutsceneLegendas cutscene;

    private void Start()
    {
        GameManager gm = GameManager.Instance;
        rota = gm != null && gm.rotaFinal != RotaFinal.Nenhuma ? gm.rotaFinal : rotaDeTeste;
        caso = gm != null ? gm.casoEscolhido : null;
        bool? pelaPublicacao = ReuAbsolvido(gm, caso);
        reuAbsolvido = pelaPublicacao ?? reuAbsolvidoDeTeste;
        Debug.Log($"[TRIBUNAL] Rota {rota}, caso '{(caso != null ? caso.name : "-")}', réu {(reuAbsolvido ? "absolvido" : "condenado")}" +
                  (pelaPublicacao.HasValue ? " (pelo panfleto publicado)." : " (valor de teste: nenhum panfleto do caso publicado)."));

        ColetarProvas(gm);
        totalDeArgumentos = Mathf.Clamp(provas.Count, 1, maxArgumentos);
        irritacao = IrritacaoInicial();

        Montar();
        AtualizarBarra(irritacao);
        AtualizarContador();
        textoReacao.text = "A bancada aguarda a sua defesa. Escolha uma prova para apresentar.";

        if (abertura != null && abertura.Count > 0) cutscene.Tocar(abertura, () => ocupado = false, fadeDeEntradaInstantaneo: true);
        else ocupado = false;
    }

    // ===== Provas =====

    private void ColetarProvas(GameManager gm)
    {
        List<Item> itens = ProvasDoCaso(gm, caso, CatalogoDeSave.Instancia);
        // Sem nada registrado para o caso (ex.: cena aberta direto no Editor): usa o que está no inventário.
        if (itens.Count == 0 && gm != null && gm.inventarioSalvo != null)
            foreach (Item item in gm.inventarioSalvo)
                if (item != null && item.itemAmt > 0) itens.Add(item);

        var vistos = new HashSet<string>();
        foreach (Item item in itens)
        {
            string id = string.IsNullOrEmpty(item.itemID) ? item.name : item.itemID;
            if (!vistos.Add(id)) continue;
            provas.Add(new Prova { nome = item.NomeExibicao, icone = item.itemImg, confiabilidade = item.confiabilidade });
        }

        if (provas.Count == 0)
            foreach (string argumento in argumentosReserva)
                if (!string.IsNullOrWhiteSpace(argumento))
                    provas.Add(new Prova { nome = argumento, confiabilidade = Confiabilidade.NaoEPista });
    }

    /// <summary>
    /// Documento (Fase 4, "Fase de Argumentação"): as provas coletadas, os documentos comprados e o panfleto gerado
    /// DO CASO julgado. Vem do histórico (GameManager.evidenciasObtidas), porque a prensa consome as duas pistas
    /// impressas; panfletos de casos anteriores não entram. Vazio se não houver caso ou catálogo.
    /// </summary>
    public static List<Item> ProvasDoCaso(GameManager gm, CaseData caso, CatalogoDeSave catalogo)
    {
        var itens = new List<Item>();
        if (gm == null || caso == null || catalogo == null) return itens;

        foreach (string id in gm.evidenciasObtidas)
        {
            Item item = catalogo.BuscarItem(id);
            if (item != null && item.caso == caso && !itens.Contains(item)) itens.Add(item);
        }

        GameManager.PanfletoPublicado publicado = gm.panfletosPublicados.Find(p => p != null && p.caso == caso);
        ReceitaDeCaso receita = caso.receitaDoPanfleto;
        if (publicado != null && receita != null)
        {
            Item panfleto = receita.PanfletoDe(receita.VersaoDe(publicado.nivel));
            if (panfleto != null && !itens.Contains(panfleto)) itens.Add(panfleto);
        }
        return itens;
    }

    /// <summary>Destino do réu: absolvido se o panfleto do caso foi publicado só com fatos; condenado com boato,
    /// calúnia ou só alegações. Nulo se o caso não tem panfleto publicado.</summary>
    public static bool? ReuAbsolvido(GameManager gm, CaseData caso)
    {
        if (gm == null || caso == null) return null;
        GameManager.PanfletoPublicado publicado = gm.panfletosPublicados.Find(p => p != null && p.caso == caso);
        if (publicado == null) return null;
        return publicado.nivel == NivelDoPanfleto.Fatos;
    }

    private void Apresentar(Prova prova)
    {
        if (ocupado || prova.botao == null || !prova.botao.interactable) return;
        prova.botao.interactable = false;
        apresentados++;
        AtualizarContador();

        bool ultima = apresentados >= totalDeArgumentos || !RestaProva();
        string cabecalho = $"<i>Você apresenta: {prova.nome}</i>\n";

        if (ultima)
        {
            StartCoroutine(Veredito(cabecalho));
            return;
        }

        float alvo = Mathf.Clamp(Curva(apresentados / (float)totalDeArgumentos) + Ajuste(prova.confiabilidade), 0f, TetoAntesDoVeredito());
        float delta = alvo - irritacao;
        string juiz = NomeDoJuiz(apresentados);
        List<string> falas = delta < -3f ? reacoesFavoraveis : delta > 3f ? reacoesHostis : reacoesNeutras;
        textoReacao.text = cabecalho + Formatar(Sortear(falas), juiz, prova.nome);
        StartCoroutine(AnimarBarra(alvo));
    }

    private void EncerrarDefesa()
    {
        if (ocupado || apresentados == 0) return;
        StartCoroutine(Veredito("<i>Você encerra a sua defesa.</i>\n"));
    }

    private bool RestaProva()
    {
        foreach (Prova p in provas) if (p.botao != null && p.botao.interactable) return true;
        return false;
    }

    // ===== Barra scriptada pela rota =====

    private float IrritacaoInicial()
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return 30f;
            case RotaFinal.B_Tirano: return 10f;
            default: return 30f;
        }
    }

    /// <summary>Tendência da barra pelo andamento do julgamento (0 = início, 1 = última prova).</summary>
    private float Curva(float progresso)
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return Mathf.Lerp(30f, 80f, progresso);
            case RotaFinal.B_Tirano: return Mathf.Lerp(15f, 25f, progresso);
            default: return Mathf.Lerp(30f, 55f, progresso); // C: a bancada nunca se inflama nem se acalma
        }
    }

    /// <summary>A "reação" à qualidade da prova: só muda o caminho da barra, nunca o desfecho.</summary>
    private static float Ajuste(Confiabilidade confiabilidade)
    {
        float ajuste;
        switch (confiabilidade)
        {
            case Confiabilidade.Fato: ajuste = -10f; break;
            case Confiabilidade.Boato: ajuste = 6f; break;
            case Confiabilidade.Calunia: ajuste = 12f; break;
            default: ajuste = -4f; break;
        }
        return ajuste + UnityEngine.Random.Range(-4f, 4f);
    }

    private float TetoAntesDoVeredito() => rota == RotaFinal.B_Tirano ? 45f : 88f;

    private float IrritacaoFinal()
    {
        switch (rota)
        {
            case RotaFinal.A_Guilhotina: return Limite;
            case RotaFinal.B_Tirano: return Mathf.Min(irritacao, 25f);
            default: return 50f; // C: indiferença, os juízes perdem o interesse no impressor
        }
    }

    private IEnumerator Veredito(string cabecalho)
    {
        ocupado = true;
        foreach (Prova p in provas) if (p.botao != null) p.botao.interactable = false;
        botaoEncerrar.interactable = false;

        textoReacao.text = cabecalho + "A bancada se reúne em silêncio.";
        yield return new WaitForSecondsRealtime(1.2f);

        yield return AnimarBarra(IrritacaoFinal());
        if (rota == RotaFinal.A_Guilhotina) yield return Explodir();

        DesfechoDaRota desfecho = DesfechoDe(rota);
        string veredito = desfecho == null ? null : (reuAbsolvido ? desfecho.vereditoReuAbsolvido : desfecho.vereditoReuCondenado);
        textoReacao.text = Formatar(veredito, NomeDoJuiz(0), string.Empty);
        yield return new WaitForSecondsRealtime(4f);

        Debug.Log($"[TRIBUNAL] Veredito da rota {rota}, réu {(reuAbsolvido ? "absolvido" : "condenado")}.");
        var legendas = new List<CutsceneLegendas.Linha>();
        if (desfecho != null)
        {
            string doReu = reuAbsolvido ? desfecho.legendaReuAbsolvido : desfecho.legendaReuCondenado;
            if (!string.IsNullOrWhiteSpace(doReu)) legendas.Add(new CutsceneLegendas.Linha { texto = doReu, duracao = 5f });
            if (desfecho.legendas != null) legendas.AddRange(desfecho.legendas);
        }
        if (legendas.Count > 0) cutscene.Tocar(legendas, () => MostrarFim(desfecho));
        else MostrarFim(desfecho);
    }

    private IEnumerator AnimarBarra(float alvo)
    {
        bool estavaOcupado = ocupado;
        ocupado = true;
        float inicio = irritacao;
        float t = 0f;
        while (t < duracaoDaBarra)
        {
            t += Time.unscaledDeltaTime;
            irritacao = Mathf.Lerp(inicio, alvo, Mathf.SmoothStep(0f, 1f, t / duracaoDaBarra));
            AtualizarBarra(irritacao);
            yield return null;
        }
        irritacao = alvo;
        AtualizarBarra(irritacao);
        ocupado = estavaOcupado;
    }

    // Final A: a barra "explode" (documento): treme e pisca no limite.
    private IEnumerator Explodir()
    {
        Vector2 posicao = barraMoldura.anchoredPosition;
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            barraMoldura.anchoredPosition = posicao + UnityEngine.Random.insideUnitCircle * 12f;
            barraImagem.color = Mathf.PingPong(t * 8f, 1f) > 0.5f ? Color.white : CorFuria;
            yield return null;
        }
        barraMoldura.anchoredPosition = posicao;
        barraImagem.color = CorFuria;
    }

    private void AtualizarBarra(float valor)
    {
        float p = Mathf.Clamp01(valor / Limite);
        barraFill.anchorMax = new Vector2(p, 1f);
        barraImagem.color = Color.Lerp(CorCalma, CorFuria, p);
    }

    private void AtualizarContador() =>
        textoContador.text = $"Argumentos: {apresentados}/{totalDeArgumentos}";

    // ===== Fim =====

    private void MostrarFim(DesfechoDaRota desfecho)
    {
        RectTransform tela = Painel("TelaFinal", raiz, Vector2.zero, Vector2.one, Color.black);
        Texto("Fim", tela, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.75f), 72f, "FIM");
        Texto("Titulo", tela, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.6f), 48f, desfecho != null ? desfecho.titulo : string.Empty);
        Texto("Reu", tela, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.44f), 30f, reuAbsolvido ? "O réu foi absolvido." : "O réu foi condenado.");
        Button voltar = Botao("Voltar", tela, new Vector2(0.35f, 0.2f), new Vector2(0.65f, 0.3f), "Voltar ao menu", null);
        voltar.onClick.AddListener(VoltarAoMenu);
    }

    private void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.LoadScene(cenaDoMenu);
        else SceneManager.LoadScene(cenaDoMenu);
    }

    // ===== Auxiliares =====

    private DesfechoDaRota DesfechoDe(RotaFinal r)
    {
        foreach (DesfechoDaRota d in desfechos) if (d != null && d.rota == r) return d;
        return null;
    }

    private string NomeDoJuiz(int indice) =>
        juizes != null && juizes.Count > 0 ? juizes[indice % juizes.Count].nome : "O juiz";

    private static string Sortear(List<string> lista) =>
        lista == null || lista.Count == 0 ? string.Empty : lista[UnityEngine.Random.Range(0, lista.Count)];

    private static string Formatar(string texto, string juiz, string prova) =>
        string.IsNullOrEmpty(texto) ? string.Empty : texto.Replace("{juiz}", juiz).Replace("{prova}", prova);

    // ===== Montagem da HUD =====

    private void Montar()
    {
        var canvasGo = new GameObject("TribunalCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        raiz = (RectTransform)canvasGo.transform;

        RectTransform fundoRt = Painel("Fundo", raiz, Vector2.zero, Vector2.one, fundo != null ? Color.white : new Color(0.07f, 0.05f, 0.04f));
        if (fundo != null) fundoRt.GetComponent<Image>().sprite = fundo;

        string titulo = caso != null && !string.IsNullOrWhiteSpace(caso.caseTitle)
            ? $"Tribunal Revolucionário: {caso.caseTitle.Trim()}"
            : "Tribunal Revolucionário";
        Texto("Titulo", raiz, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f), 44f, titulo);

        // Bancada
        RectTransform bancada = Vazio("Bancada", raiz, new Vector2(0.15f, 0.64f), new Vector2(0.85f, 0.88f));
        var layout = bancada.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f; layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        foreach (Juiz juiz in juizes)
        {
            RectTransform cartao = Painel("Juiz", bancada, Vector2.zero, Vector2.one, CorPainel);
            RectTransform retrato = Painel("Retrato", cartao, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.95f), CorBotao);
            if (juiz.retrato != null)
            {
                var img = retrato.GetComponent<Image>();
                img.sprite = juiz.retrato; img.color = Color.white; img.preserveAspect = true;
            }
            else if (!string.IsNullOrEmpty(juiz.nome))
                Texto("Inicial", retrato, Vector2.zero, Vector2.one, 64f, juiz.nome.Substring(0, 1));
            Texto("Nome", cartao, new Vector2(0f, 0.02f), new Vector2(1f, 0.28f), 30f, juiz.nome);
        }

        // Barra de irritação
        Texto("RotuloBarra", raiz, new Vector2(0.2f, 0.585f), new Vector2(0.8f, 0.625f), 26f, "Irritação da bancada");
        barraMoldura = Painel("Barra", raiz, new Vector2(0.2f, 0.54f), new Vector2(0.8f, 0.58f), new Color(0.03f, 0.02f, 0.02f));
        barraFill = Painel("Preenchimento", barraMoldura, Vector2.zero, new Vector2(0f, 1f), CorCalma);
        barraImagem = barraFill.GetComponent<Image>();

        // Reação da bancada
        RectTransform caixa = Painel("Reacao", raiz, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.52f), CorPainel);
        textoReacao = Texto("Texto", caixa, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f), 30f, string.Empty);
        textoReacao.alignment = TextAlignmentOptions.MidlineLeft;

        // Provas (rolagem vertical para inventários grandes)
        Texto("RotuloProvas", raiz, new Vector2(0.05f, 0.3f), new Vector2(0.75f, 0.34f), 26f, "Suas provas").alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform area = Painel("Provas", raiz, new Vector2(0.05f, 0.04f), new Vector2(0.75f, 0.3f), CorPainel);
        area.gameObject.AddComponent<RectMask2D>();
        RectTransform conteudo = Vazio("Conteudo", area, new Vector2(0f, 1f), new Vector2(1f, 1f));
        conteudo.pivot = new Vector2(0.5f, 1f);
        var grade = conteudo.gameObject.AddComponent<GridLayoutGroup>();
        grade.cellSize = new Vector2(420f, 90f); grade.spacing = new Vector2(16f, 16f);
        grade.padding = new RectOffset(16, 16, 16, 16); grade.childAlignment = TextAnchor.UpperCenter;
        conteudo.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var rolagem = area.gameObject.AddComponent<ScrollRect>();
        rolagem.content = conteudo; rolagem.horizontal = false; rolagem.movementType = ScrollRect.MovementType.Clamped;

        foreach (Prova prova in provas)
        {
            Prova p = prova;
            p.botao = Botao("Prova", conteudo, Vector2.zero, Vector2.one, p.nome, p.icone);
            p.botao.onClick.AddListener(() => Apresentar(p));
        }

        textoContador = Texto("Contador", raiz, new Vector2(0.78f, 0.22f), new Vector2(0.95f, 0.28f), 26f, string.Empty);
        botaoEncerrar = Botao("Encerrar", raiz, new Vector2(0.78f, 0.1f), new Vector2(0.95f, 0.2f), "Encerrar a defesa", null);
        botaoEncerrar.onClick.AddListener(EncerrarDefesa);

        // Cutscene própria: montada inativa para receber a fonte antes do Awake.
        var cutsceneGo = new GameObject("Cutscenes", typeof(RectTransform));
        cutsceneGo.SetActive(false);
        cutsceneGo.transform.SetParent(raiz, false);
        cutscene = cutsceneGo.AddComponent<CutsceneLegendas>();
        cutscene.fonte = fonte;
        cutsceneGo.SetActive(true);
    }

    private static RectTransform Vazio(string nome, Transform pai, Vector2 min, Vector2 max)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform Painel(string nome, Transform pai, Vector2 min, Vector2 max, Color cor)
    {
        RectTransform rt = Vazio(nome, pai, min, max);
        rt.gameObject.AddComponent<Image>().color = cor;
        return rt;
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, Vector2 min, Vector2 max, float tamanho, string conteudo)
    {
        RectTransform rt = Vazio(nome, pai, min, max);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) tmp.font = fonte;
        tmp.text = conteudo;
        tmp.color = CorTexto;
        tmp.fontSize = tamanho;
        tmp.enableAutoSizing = true; tmp.fontSizeMin = tamanho * 0.5f; tmp.fontSizeMax = tamanho;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button Botao(string nome, Transform pai, Vector2 min, Vector2 max, string rotulo, Sprite icone)
    {
        RectTransform rt = Painel(nome, pai, min, max, CorBotao);
        var botao = rt.gameObject.AddComponent<Button>();
        ColorBlock cores = botao.colors;
        cores.highlightedColor = new Color(1.25f, 1.2f, 1.1f);
        cores.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
        botao.colors = cores;

        float inicioTexto = 0.05f;
        if (icone != null)
        {
            RectTransform img = Vazio("Icone", rt, new Vector2(0.02f, 0.1f), new Vector2(0.2f, 0.9f));
            var imagem = img.gameObject.AddComponent<Image>();
            imagem.sprite = icone; imagem.preserveAspect = true; imagem.raycastTarget = false;
            inicioTexto = 0.22f;
        }
        Texto("Rotulo", rt, new Vector2(inicioTexto, 0.05f), new Vector2(0.97f, 0.95f), 28f, rotulo);
        return botao;
    }
}
