using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Padronização tipográfica com a EB Garamond (27/10/2026). Os três passos são idempotentes; rode na ordem,
/// fora do Play Mode, com as cenas abertas salvas.
///
/// 1 - Gerar Font Assets: cria (ou regenera no lugar, mantendo GUID e sub-assets) os TMP Font Assets SDF
///     estáticos a partir de Assets/Art/Fonts/EBGaramond/Sources (origem e licença em License/).
///     Regular e SemiBold em atlas 2048 e Italic em 1024 (só aparece em poucos textos pequenos); amostragem
///     automática (a maior que cabe no atlas), padding de 9% da amostragem, render SDF8 (distância calculada
///     com 8x de resolução: serifas e traços finos mais fiéis que no SDFAA), kerning e marcas do GPOS.
///     Caracteres: ASCII, Latin-1 (português completo), Œ œ Ÿ, pontuação tipográfica, ← → ▲ ▼, ﬁ ﬂ e tudo o que
///     o conteúdo do jogo usa (prefabs, cenas, ScriptableObjects e literais de Assets/Scripts). A lista final
///     fica em FontAssets/EBGaramond - caracteres.txt (formato do "Characters from File" do Font Asset Creator).
///     Pesos pela tabela de pesos do TMP: Negrito (estilo B ou tag b) desenha a SemiBold e Itálico desenha a
///     Italic, sem negrito nem inclinação sintéticos. Símbolos do conteúdo que a EB Garamond não tem (hoje só
///     o ●) vêm de um asset estático com apenas esses glifos, gerado da LiberationSans do TMP: letras nunca
///     caem nele.
/// 2 - Aplicar em todo o jogo: fonte padrão do TMP Settings, todos os textos TMP de prefabs e cenas, os
///     campos TMP_FontAsset dos scripts (fonte, fonteGlifos...), o kerning ('kern') de todo texto, o peso de
///     alguns textos (fala do diálogo sem negrito; títulos em SemiBold) e os ajustes de layout que a troca de
///     métricas exigiu (escala uniforme no diálogo, tamanhos nos cartões da Mesa e nos botões de toque). Em
///     instância de prefab só altera o valor que é da própria instância (override); o herdado é corrigido no
///     prefab de origem. Não toca em _Recovery.
/// 3 - Auditar: lista textos e campos fora da EB Garamond, materiais de outro atlas, caracteres do conteúdo
///     sem glifo, escalas não uniformes e Auto Size com mínimo baixo.
/// </summary>
public static class EBGaramondSetupTool
{
    private const string Pasta = "Assets/Art/Fonts/EBGaramond";
    private const string PastaFontAssets = Pasta + "/FontAssets";
    public const string CaminhoRegular = PastaFontAssets + "/EBGaramond-Regular SDF.asset";
    public const string CaminhoSemiBold = PastaFontAssets + "/EBGaramond-SemiBold SDF.asset";
    public const string CaminhoItalic = PastaFontAssets + "/EBGaramond-Italic SDF.asset";
    public const string CaminhoSimbolos = PastaFontAssets + "/LiberationSans SDF - Simbolos de apoio.asset";
    private const string CaminhoListaDeCaracteres = PastaFontAssets + "/EBGaramond - caracteres.txt";
    private const string FonteDosSimbolos = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

    private const GlyphRenderMode ModoDeRender = GlyphRenderMode.SDF8;
    private const float PaddingSobreAmostragem = 0.09f;
    private const int PaddingMinimo = 5;
    private const int TagKern = 0x6B65726E; // OTL_FeatureTag.kern ('kern')

    private struct Especificacao
    {
        public string ttf, asset;
        public int atlas, amostragemMaxima;
    }

    private static readonly Especificacao[] Pesos =
    {
        new Especificacao { ttf = Pasta + "/Sources/EBGaramond-Regular.ttf", asset = CaminhoRegular, atlas = 2048, amostragemMaxima = 200 },
        new Especificacao { ttf = Pasta + "/Sources/EBGaramond-SemiBold.ttf", asset = CaminhoSemiBold, atlas = 2048, amostragemMaxima = 200 },
        new Especificacao { ttf = Pasta + "/Sources/EBGaramond-Italic.ttf", asset = CaminhoItalic, atlas = 1024, amostragemMaxima = 200 },
    };

    private static readonly Especificacao Simbolos =
        new Especificacao { ttf = FonteDosSimbolos, asset = CaminhoSimbolos, atlas = 256, amostragemMaxima = 90 };

    /// <summary>Pesos decididos texto a texto. O caminho começa abaixo da raiz do prefab, ou na raiz da cena.</summary>
    private struct RegraDePeso
    {
        public string origem, caminho, motivo;
        public FontStyles tirar, por;
    }

    private static readonly RegraDePeso[] Regras =
    {
        new RegraDePeso { origem = "Assets/Prefabs/UI.prefab", caminho = "DialogePanel/TalkTextScrollView/text", tirar = FontStyles.Bold, motivo = "fala do diálogo é texto corrido (Regular)" },
        new RegraDePeso { origem = "Assets/Prefabs/UI.prefab", caminho = "opções/opções", por = FontStyles.Bold, motivo = "título do painel (SemiBold)" },
        new RegraDePeso { origem = "Assets/Prefabs/CartaoPrefab.prefab", caminho = "TituloTexto", por = FontStyles.Bold, motivo = "título do cartão (SemiBold)" },
        new RegraDePeso { origem = "Assets/Scenes/menu principal.unity", caminho = "MenuCanvas/opções/texto opções", por = FontStyles.Bold, motivo = "título do painel (SemiBold)" },
        new RegraDePeso { origem = "Assets/Scenes/menu principal.unity", caminho = "MenuCanvas/Creditos/Image/Titulo", por = FontStyles.Bold, motivo = "título do painel (SemiBold)" },
        new RegraDePeso { origem = "Assets/Scenes/menu principal.unity", caminho = "MenuCanvas/painelDeConfirmação/Image/confirmação", por = FontStyles.Bold, motivo = "título do painel (SemiBold)" },
        new RegraDePeso { origem = "Assets/Scenes/menu principal.unity", caminho = "MenuCanvas/painelConfirmarNovoJogo/Image/titulo", por = FontStyles.Bold, motivo = "título do painel (SemiBold)" },
    };

    /// <summary>
    /// Textos com escala X diferente da Y (letras comprimidas na horizontal). Passam a ter X = Y; a largura do
    /// retângulo (âncoras no centro) é compensada para ocupar no Canvas o mesmo espaço de antes.
    /// </summary>
    private static readonly (string origem, string caminho)[] EscalasParaUniformizar =
    {
        ("Assets/Prefabs/UI.prefab", "DialogePanel/name"),
        ("Assets/Prefabs/UI.prefab", "DialogePanel/TalkTextScrollView"),
    };

    /// <summary>
    /// Tamanhos revistos para a EB Garamond, cuja altura-x (~0,40 em) é bem menor que a das versaletes da Cinzel
    /// (~0,60 em). Cartões da Mesa e popup do tutorial: a pedido do grupo (27/10/2026), nada abaixo de ~22 na
    /// referência de 1080 (15 px em 720), com a hierarquia mantida (título > cabeçalho > linhas > aviso). Os rótulos dos
    /// botões de toque estavam em VT323, que desenha bem menor no mesmo tamanho: em EB Garamond passavam das bordas.
    /// Só troca se o tamanho atual for um dos antigos; com auto size, ajusta também o mínimo e o máximo.
    /// </summary>
    private static readonly (string origem, string caminho, float para, float[] antigos, float autoMin, float autoMax)[] Tamanhos =
    {
        ("Assets/Prefabs/CartaoPrefab.prefab", "TituloTexto", 32f, new[] { 30f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "ObjetivoTexto", 24f, new[] { 20f, 22f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "DescricaoTexto", 26f, new[] { 24f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "ConsequenciasHeader", 26f, new[] { 22f, 24f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "RecompensaTexto", 24f, new[] { 20f, 22f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "OpiniaoPopularTexto", 24f, new[] { 20f, 22f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "OpiniaoEstadoTexto", 24f, new[] { 20f, 22f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "AvisoTexto", 22f, new[] { 16f, 19f }, 0f, 0f),
        ("Assets/Prefabs/CartaoPrefab.prefab", "ButtonRow/BotaoAceitar/Text (TMP)", 24f, new[] { 20f, 22f }, 0f, 0f),
        ("Assets/Prefabs/UI.prefab", "ItensPoupUp/PopupTutorial/Titulo", 30f, new[] { 28f }, 0f, 0f),
        ("Assets/Prefabs/UI.prefab", "ItensPoupUp/PopupTutorial/Corpo/TextoEtapa", 27f, new[] { 24f }, 22f, 28f),
        ("Assets/Prefabs/UI.prefab", "ItensPoupUp/PopupTutorial/BotaoContinuar/Text (TMP)", 26f, new[] { 24f }, 0f, 0f),
        ("Assets/Prefabs/UI.prefab", "ItensPoupUp/PopupTutorial/BotaoPular/Texto", 22f, new[] { 20f }, 0f, 0f),
        ("Assets/Prefabs/MobileControlsUI.prefab", "TouchZone/PauseButton/Text (TMP)", 36f, new[] { 54f }, 0f, 0f),
        ("Assets/Prefabs/MobileControlsUI.prefab", "TouchZone/ObjetosDesativar/InteractButton/Text (TMP)", 36f, new[] { 50f }, 0f, 0f),
        ("Assets/Prefabs/MobileControlsUI.prefab", "TouchZone/ObjetosDesativar/InventoryButton/Text (TMP)", 36f, new[] { 44.98f }, 0f, 0f),
    };

    // ------------------------------------------------------------------ 1. Font Assets

    [MenuItem("Ferramentas/Fontes/EB Garamond/1 - Gerar ou atualizar Font Assets")]
    public static void GerarFontAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[EBGaramond] Saia do Play Mode antes de gerar os Font Assets.");
            return;
        }

        var log = new StringBuilder("[EBGaramond] Font Assets\n");
        SortedDictionary<int, string> doConteudo = CaracteresDoConteudo();
        string desejados = Distintos(ConjuntoBase() + Texto(doConteudo.Keys));

        var gerados = new List<TMP_FontAsset>();
        foreach (Especificacao e in Pesos)
            gerados.Add(GerarOuAtualizar(e, desejados, log));
        if (gerados.Any(f => f == null))
        {
            Debug.LogError(log.ToString());
            return;
        }
        TMP_FontAsset regular = gerados[0], semiBold = gerados[1], italic = gerados[2];

        // Do conteúdo, o que a EB Garamond não desenha: símbolos vão para o asset de apoio; letras são erro.
        var semGlifo = doConteudo.Keys.Where(c => !regular.characterLookupTable.ContainsKey((uint)c)).ToList();
        var letras = semGlifo.Where(c => char.IsLetterOrDigit(char.ConvertFromUtf32(c), 0)).ToList();
        foreach (int c in letras)
            log.AppendLine($"  ERRO: letra/algarismo do conteúdo sem glifo na EB Garamond: {Descrever(c)} (ex.: {doConteudo[c]})");
        string simbolos = Texto(semGlifo.Except(letras));

        TMP_FontAsset apoio = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoSimbolos);
        if (simbolos.Length > 0)
        {
            apoio = GerarOuAtualizar(Simbolos, simbolos, log);
            foreach (int c in semGlifo.Except(letras))
                log.AppendLine($"  símbolo {Descrever(c)} (ex.: {doConteudo[c]}) vem de '{Path.GetFileNameWithoutExtension(CaminhoSimbolos)}'.");
        }

        // Tabela de pesos: Negrito -> SemiBold (sem negrito sintético) e Itálico -> Italic.
        regular.fontWeightTable[4].italicTypeface = italic;
        regular.fontWeightTable[6].regularTypeface = semiBold;
        regular.fontWeightTable[7].regularTypeface = semiBold;
        semiBold.fontWeightTable[7].regularTypeface = semiBold; // B num texto já SemiBold não engrossa de novo

        foreach (TMP_FontAsset fa in gerados)
        {
            fa.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (apoio != null && simbolos.Length > 0) fa.fallbackFontAssetTable.Add(apoio);
            EditorUtility.SetDirty(fa);
        }

        File.WriteAllText(CaminhoListaDeCaracteres, desejados, new UTF8Encoding(false));
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(CaminhoListaDeCaracteres);
        log.AppendLine($"  {desejados.Length} caracteres pedidos ({doConteudo.Count} distintos no conteúdo); lista em {CaminhoListaDeCaracteres}.");
        Debug.Log(log.ToString());
    }

    private static TMP_FontAsset GerarOuAtualizar(Especificacao e, string caracteres, StringBuilder log)
    {
        Font fonte = AssetDatabase.LoadAssetAtPath<Font>(e.ttf);
        if (fonte == null)
        {
            log.AppendLine($"  ERRO: {e.ttf} não encontrado.");
            return null;
        }

        // Só pede o que a fonte desenha; o resto vai para o relatório.
        FontEngine.LoadFontFace(fonte, 90);
        string comGlifo = Texto(Codigos(caracteres).Where(c => FontEngine.TryGetGlyphIndex((uint)c, out uint g) && g != 0));
        string semGlifo = Texto(Codigos(caracteres).Where(c => !(FontEngine.TryGetGlyphIndex((uint)c, out uint g) && g != 0)));

        TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(e.asset);
        int amostragem = MaiorAmostragemQueCabe(fonte, comGlifo, e.atlas, e.amostragemMaxima);
        for (; amostragem >= 12; amostragem -= 2)
        {
            int padding = Padding(amostragem);
            if (fa == null)
            {
                fa = TMP_FontAsset.CreateFontAsset(fonte, amostragem, padding, ModoDeRender, e.atlas, e.atlas, AtlasPopulationMode.Dynamic, false);
                fa.name = Path.GetFileNameWithoutExtension(e.asset);
                AssetDatabase.CreateAsset(fa, e.asset);
                fa.atlasTexture.name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
                fa.material.name = fa.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            Reconfigurar(fa, fonte, amostragem, padding, e.atlas);
            if (fa.TryAddCharacters(comGlifo, out string naoCouberam, true) && naoCouberam.Length == 0) break;
            log.AppendLine($"  {fa.name}: {naoCouberam.Length} glifo(s) não couberam com amostragem {amostragem}; tentando menor.");
        }

        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        Texture2D atlas = fa.atlasTexture;
        var soAtlas = new SerializedObject(atlas);
        soAtlas.FindProperty("m_IsReadable").boolValue = false; // estático: sem cópia na CPU no build
        soAtlas.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(atlas);
        EditorUtility.SetDirty(fa.material);
        EditorUtility.SetDirty(fa);

        log.AppendLine($"  {fa.name}: {fa.characterTable.Count} caracteres, amostragem {fa.faceInfo.pointSize} pt, padding {fa.atlasPadding}, " +
                       $"atlas {fa.atlasWidth}x{fa.atlasHeight} {atlas.format}, {fa.atlasRenderMode}, " +
                       $"{fa.fontFeatureTable.glyphPairAdjustmentRecords.Count} pares de kerning" +
                       (semGlifo.Length > 0 ? $"; sem glifo na fonte: {string.Join(" ", Codigos(semGlifo).Select(Descrever))}" : string.Empty));
        return fa;
    }

    /// <summary>Volta a dinâmico só para regenerar: amostragem, padding e atlas novos, tabelas limpas.</summary>
    private static void Reconfigurar(TMP_FontAsset fa, Font fonte, int amostragem, int padding, int atlas)
    {
        typeof(TMP_FontAsset).GetProperty("SourceFont_EditorRef", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(fa, fonte);
        fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        var so = new SerializedObject(fa);
        so.FindProperty("m_AtlasWidth").intValue = atlas;
        so.FindProperty("m_AtlasHeight").intValue = atlas;
        so.FindProperty("m_AtlasPadding").intValue = padding;
        so.FindProperty("m_AtlasRenderMode").intValue = (int)ModoDeRender;
        so.FindProperty("m_IsMultiAtlasTexturesEnabled").boolValue = false;
        so.FindProperty("m_GetFontFeatures").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        FontEngine.LoadFontFace(fonte, amostragem);
        fa.faceInfo = FontEngine.GetFaceInfo();
        fa.ClearFontAssetData(true);

        Material m = fa.material;
        m.SetTexture(ShaderUtilities.ID_MainTex, fa.atlasTexture);
        m.SetFloat(ShaderUtilities.ID_GradientScale, padding + 1);
        m.SetFloat(ShaderUtilities.ID_TextureWidth, atlas);
        m.SetFloat(ShaderUtilities.ID_TextureHeight, atlas);
        m.SetFloat(ShaderUtilities.ID_WeightNormal, fa.normalStyle);
        m.SetFloat(ShaderUtilities.ID_WeightBold, fa.boldStyle);
    }

    private static int Padding(int amostragem) => Mathf.Max(PaddingMinimo, Mathf.RoundToInt(amostragem * PaddingSobreAmostragem));

    /// <summary>Busca binária da maior amostragem cujo atlas comporta todos os glifos.</summary>
    private static int MaiorAmostragemQueCabe(Font fonte, string caracteres, int atlas, int maxima)
    {
        int menor = 12, maior = maxima;
        while (menor < maior)
        {
            int meio = (menor + maior + 1) / 2;
            if (Cabe(fonte, caracteres, meio, atlas)) menor = meio;
            else maior = meio - 1;
        }
        return menor;
    }

    private static bool Cabe(Font fonte, string caracteres, int amostragem, int atlas)
    {
        // Ensaio em memória com SDFAA: o empacotamento só depende das métricas dos glifos, e o SDFAA é rápido.
        TMP_FontAsset ensaio = TMP_FontAsset.CreateFontAsset(fonte, amostragem, Padding(amostragem), GlyphRenderMode.SDFAA,
            atlas, atlas, AtlasPopulationMode.Dynamic, false);
        try
        {
            return ensaio.TryAddCharacters(caracteres, out string faltam) && faltam.Length == 0;
        }
        finally
        {
            Object.DestroyImmediate(ensaio.atlasTexture);
            Object.DestroyImmediate(ensaio.material);
            Object.DestroyImmediate(ensaio);
        }
    }

    /// <summary>ASCII, Latin-1, Œ œ Ÿ, pontuação tipográfica, setas e triângulos da interface, ligaduras ﬁ ﬂ.</summary>
    private static string ConjuntoBase()
    {
        var sb = new StringBuilder();
        for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);
        for (int c = 0xA0; c <= 0xFF; c++) sb.Append((char)c);
        sb.Append("ŒœŸ");                                     // Œ œ Ÿ
        sb.Append("–—‘’‚“”„");       // – — ‘ ’ ‚ “ ” „
        sb.Append("†‡•…‰‹›€™−"); // † ‡ • … ‰ ‹ › € ™ −
        sb.Append("←→▲▼");                               // ← → ▲ ▼
        sb.Append("ﬁﬂ");                                           // ﬁ ﬂ
        return sb.ToString();
    }

    // ------------------------------------------------------------------ 2. Aplicar

    [MenuItem("Ferramentas/Fontes/EB Garamond/2 - Aplicar em todo o jogo")]
    public static void AplicarMenu() => Debug.Log(AplicarEmTodoOJogo());

    public static string AplicarEmTodoOJogo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "[EBGaramond] Saia do Play Mode antes de aplicar a fonte.";
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoRegular);
        if (regular == null)
            return "[EBGaramond] Rode antes 'Ferramentas/Fontes/EB Garamond/1 - Gerar ou atualizar Font Assets'.";

        var log = new StringBuilder("[EBGaramond] Aplicação\n");

        var settings = new SerializedObject(TMP_Settings.instance);
        SerializedProperty padrao = settings.FindProperty("m_defaultFontAsset");
        if (padrao.objectReferenceValue != regular)
        {
            padrao.objectReferenceValue = regular;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(TMP_Settings.instance);
            log.AppendLine($"  TMP Settings: fonte padrão = {regular.name}.");
        }
        // O valor antigo [0] só vira 'kern' quando o TMP Settings é lido em runtime; textos criados por ferramentas
        // de Editor ficavam sem kerning.
        if (GarantirKerning(settings.FindProperty("m_ActiveFontFeatures"), settings.FindProperty("m_enableKerning")))
        {
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(TMP_Settings.instance);
            log.AppendLine("  TMP Settings: kerning ('kern') nos recursos padrão de textos novos.");
        }

        foreach (string caminho in PrefabsEmOrdem())
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(caminho);
            try
            {
                int n = Ajustar(raiz, caminho, raiz.transform, regular, log);
                if (n == 0) continue;
                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                log.AppendLine($"  {caminho}: {n} alteração(ões).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        foreach (string caminho in CenasDoJogo())
        {
            Scene cena = SceneManager.GetSceneByPath(caminho);
            bool jaAberta = cena.IsValid() && cena.isLoaded;
            if (jaAberta && cena.isDirty)
            {
                log.AppendLine($"  PULADA: {caminho} está aberta com alterações não salvas. Salve e rode de novo.");
                continue;
            }
            if (!jaAberta) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
            try
            {
                int n = 0;
                foreach (GameObject raiz in cena.GetRootGameObjects())
                    n += Ajustar(raiz, caminho, null, regular, log);
                if (n == 0) continue;
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
                log.AppendLine($"  {caminho}: {n} alteração(ões).");
            }
            finally
            {
                if (!jaAberta) EditorSceneManager.CloseScene(cena, true);
            }
        }

        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    /// <summary>Ajusta textos e campos de fonte abaixo de <paramref name="raiz"/>. Devolve quantas alterações fez.</summary>
    private static int Ajustar(GameObject raiz, string origem, Transform raizDoPrefab, TMP_FontAsset regular, StringBuilder log)
    {
        int alteracoes = 0;

        foreach (TMP_Text texto in raiz.GetComponentsInChildren<TMP_Text>(true))
        {
            var so = new SerializedObject(texto);
            bool instancia = PrefabUtility.IsPartOfPrefabInstance(texto);
            SerializedProperty pFonte = so.FindProperty("m_fontAsset");
            SerializedProperty pMaterial = so.FindProperty("m_sharedMaterial");
            SerializedProperty pMateriais = so.FindProperty("m_fontSharedMaterials");

            if ((!instancia || pFonte.prefabOverride) && !DaFamilia(pFonte.objectReferenceValue as TMP_FontAsset))
            {
                pFonte.objectReferenceValue = regular;
                alteracoes++;
            }

            // O material precisa ser do atlas da fonte efetiva (o TMP desenharia a Cinzel com o atlas novo).
            var fonte = pFonte.objectReferenceValue as TMP_FontAsset;
            if (fonte != null && (!instancia || pMaterial.prefabOverride) && !DoAtlas(pMaterial.objectReferenceValue as Material, fonte))
            {
                pMaterial.objectReferenceValue = fonte.material;
                alteracoes++;
            }
            if (fonte != null && pMateriais != null && (!instancia || pMateriais.prefabOverride))
            {
                for (int i = 0; i < pMateriais.arraySize; i++)
                {
                    SerializedProperty m = pMateriais.GetArrayElementAtIndex(i);
                    if (m.objectReferenceValue == null || DaFamilia(m.objectReferenceValue as Material)) continue;
                    m.objectReferenceValue = fonte.material;
                    alteracoes++;
                }
            }

            foreach ((string o, string c, float para, float[] antigos, float autoMin, float autoMax) in Tamanhos)
            {
                if (o != origem || Caminho(texto.transform, raizDoPrefab) != c) continue;
                // Com auto size, m_fontSize guarda o último tamanho calculado; o que se escolhe no Inspector é a base.
                SerializedProperty pBase = so.FindProperty("m_fontSizeBase");
                float atual = pBase.floatValue;
                bool antigo = antigos.Any(a => Mathf.Abs(atual - a) < 0.05f);
                if (antigo)
                {
                    pBase.floatValue = para;
                    so.FindProperty("m_fontSize").floatValue = para;
                    alteracoes++;
                    log.AppendLine($"    {origem} > {c}: tamanho {atual:0.##} -> {para:0.##}.");
                }
                if (autoMax <= 0f || !(antigo || Mathf.Abs(atual - para) < 0.05f)) continue;
                SerializedProperty pMin = so.FindProperty("m_fontSizeMin"), pMax = so.FindProperty("m_fontSizeMax");
                if (Mathf.Approximately(pMin.floatValue, autoMin) && Mathf.Approximately(pMax.floatValue, autoMax)) continue;
                log.AppendLine($"    {origem} > {c}: auto size {pMin.floatValue:0.##}-{pMax.floatValue:0.##} -> {autoMin:0.##}-{autoMax:0.##}.");
                pMin.floatValue = autoMin;
                pMax.floatValue = autoMax;
                alteracoes++;
            }

            SerializedProperty pRecursos = so.FindProperty("m_ActiveFontFeatures");
            if ((!instancia || pRecursos.prefabOverride) && GarantirKerning(pRecursos, so.FindProperty("m_enableKerning")))
                alteracoes++;

            foreach (RegraDePeso regra in Regras)
            {
                if (regra.origem != origem || Caminho(texto.transform, raizDoPrefab) != regra.caminho) continue;
                SerializedProperty pEstilo = so.FindProperty("m_fontStyle");
                var estilo = (FontStyles)pEstilo.intValue;
                var novo = (estilo & ~regra.tirar) | regra.por;
                if (novo == estilo) continue;
                pEstilo.intValue = (int)novo;
                alteracoes++;
                log.AppendLine($"    {origem} > {regra.caminho}: {estilo} -> {novo} ({regra.motivo}).");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Campos TMP_FontAsset dos scripts do jogo (BibliotecaUI.fonte, TutorialStepUI.fonteGlifos...).
        foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb is TMP_Text || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;
            var so = new SerializedObject(mb);
            bool instancia = PrefabUtility.IsPartOfPrefabInstance(mb);
            bool mudou = false;
            SerializedProperty p = so.GetIterator();
            while (p.Next(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference || p.type != "PPtr<$TMP_FontAsset>") continue;
                if (instancia && !p.prefabOverride) continue;
                if (DaFamilia(p.objectReferenceValue as TMP_FontAsset)) continue;
                log.AppendLine($"    {origem} > {mb.gameObject.name}: {mb.GetType().Name}.{p.propertyPath} " +
                               $"{(p.objectReferenceValue != null ? p.objectReferenceValue.name : "vazio")} -> {regular.name}.");
                p.objectReferenceValue = regular;
                mudou = true;
                alteracoes++;
            }
            if (mudou) so.ApplyModifiedPropertiesWithoutUndo();
        }

        alteracoes += CorrigirOverridesGravados(raiz, regular, origem, log);

        foreach ((string o, string c) in EscalasParaUniformizar)
        {
            if (o != origem || raizDoPrefab == null) continue;
            var rt = raizDoPrefab.Find(c) as RectTransform;
            if (rt == null) continue;
            Vector3 e = rt.localScale;
            if (Mathf.Abs(e.x) < 1e-5f || Mathf.Abs(Mathf.Abs(e.x) - Mathf.Abs(e.y)) < 1e-5f) continue;
            if (!Mathf.Approximately(rt.anchorMin.x, rt.anchorMax.x))
            {
                log.AppendLine($"    {origem} > {c}: escala não uniforme, mas a largura depende das âncoras; ajuste à mão.");
                continue;
            }
            float fator = Mathf.Abs(e.x / e.y);
            Vector2 tamanhoAntes = rt.sizeDelta;
            rt.sizeDelta = new Vector2(tamanhoAntes.x * fator, tamanhoAntes.y);
            rt.localScale = new Vector3(Mathf.Sign(e.x) * Mathf.Abs(e.y), e.y, e.z);
            alteracoes++;
            log.AppendLine($"    {origem} > {c}: escala {e.x:0.####} x {e.y:0.####} -> uniforme {e.y:0.####}; largura {tamanhoAntes.x:0.##} -> {rt.sizeDelta.x:0.##} (mesmo espaço no Canvas).");
        }

        return alteracoes;
    }

    /// <summary>
    /// Overrides de instância gravados com fonte ou material de outra família. Ao carregar, o TMP troca em memória o
    /// material que não é do atlas da fonte, então o SerializedObject já mostra o novo; mas o arquivo continuaria
    /// apontando para a fonte antiga (e o atlas dela iria para o build).
    /// </summary>
    private static int CorrigirOverridesGravados(GameObject raiz, TMP_FontAsset regular, string origem, StringBuilder log)
    {
        int corrigidos = 0;
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
            PropertyModification[] mods = PrefabUtility.GetPropertyModifications(t.gameObject);
            if (mods == null) continue;
            bool mudou = false;
            foreach (PropertyModification m in mods)
            {
                TMP_FontAsset dona = FonteDona(m.objectReference);
                if (dona == null || DaFamilia(dona) || AssetDatabase.GetAssetPath(dona) == CaminhoSimbolos) continue;
                Object novo = m.objectReference is TMP_FontAsset ? regular : (Object)regular.material;
                log.AppendLine($"    {origem} > override {m.propertyPath} em {(m.target != null ? m.target.name : "?")}: {m.objectReference.name} -> {novo.name}.");
                m.objectReference = novo;
                mudou = true;
                corrigidos++;
            }
            if (mudou) PrefabUtility.SetPropertyModifications(t.gameObject, mods);
        }
        return corrigidos;
    }

    /// <summary>O Font Asset a que um objeto pertence: ele mesmo, o material ou o atlas de um Font Asset.</summary>
    private static TMP_FontAsset FonteDona(Object o)
    {
        if (o is TMP_FontAsset fa) return fa;
        Texture atlas = o is Material m && m.HasProperty(ShaderUtilities.ID_MainTex) ? m.GetTexture(ShaderUtilities.ID_MainTex) : o as Texture2D;
        return atlas != null ? AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(atlas)) as TMP_FontAsset : null;
    }

    // ------------------------------------------------------------------ 3. Auditar

    [MenuItem("Ferramentas/Fontes/EB Garamond/3 - Auditar textos e caracteres")]
    public static void AuditarMenu() => Debug.Log(Auditar());

    public static string Auditar()
    {
        var log = new StringBuilder("[EBGaramond] Auditoria\n");
        TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoRegular);
        TMP_FontAsset semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoSemiBold);
        TMP_FontAsset italic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoItalic);
        TMP_FontAsset apoio = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoSimbolos);
        if (regular == null || semiBold == null || italic == null) return log.Append("  Font Assets ainda não gerados.").ToString();

        log.AppendLine($"  TMP Settings: fonte padrão = {(TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset.name : "nenhuma")}.");

        int textos = 0, problemas = 0;
        void Inspecionar(GameObject raiz, string origem)
        {
            foreach (TMP_Text t in raiz.GetComponentsInChildren<TMP_Text>(true))
            {
                textos++;
                string onde = $"{origem} > {Caminho(t.transform, null)}";
                if (!DaFamilia(t.font)) { problemas++; log.AppendLine($"  FONTE: {onde} usa {(t.font != null ? t.font.name : "nenhuma")}."); }
                else if (!DoAtlas(t.fontSharedMaterial, t.font)) { problemas++; log.AppendLine($"  MATERIAL: {onde} usa '{(t.fontSharedMaterial != null ? t.fontSharedMaterial.name : "nenhum")}'."); }
                Vector2 e = EscalaAcumulada(t.transform);
                if (Mathf.Abs(e.x) > 1e-5f && Mathf.Abs(Mathf.Abs(e.y / e.x) - 1f) > 0.01f)
                    log.AppendLine($"  ESCALA não uniforme: {onde} ({e.x:0.###} x {e.y:0.###}).");
                if (!TemKerning(t)) log.AppendLine($"  KERNING desligado: {onde}.");
                if (t.enableAutoSizing && t.fontSizeMin < 16f)
                    log.AppendLine($"  AUTO SIZE: {onde} pode descer a {t.fontSizeMin:0.#} (máx. {t.fontSizeMax:0.#}).");
            }
            foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb is TMP_Text || mb.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;
                SerializedProperty p = new SerializedObject(mb).GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && p.type == "PPtr<$TMP_FontAsset>" && !DaFamilia(p.objectReferenceValue as TMP_FontAsset))
                    {
                        problemas++;
                        log.AppendLine($"  CAMPO: {origem} > {mb.gameObject.name} {mb.GetType().Name}.{p.propertyPath} = {(p.objectReferenceValue != null ? p.objectReferenceValue.name : "vazio")}.");
                    }
            }
        }

        foreach (string caminho in PrefabsEmOrdem())
            Inspecionar(AssetDatabase.LoadAssetAtPath<GameObject>(caminho), caminho);
        foreach (string caminho in CenasDoJogo())
        {
            Scene cena = EditorSceneManager.OpenPreviewScene(caminho);
            try { foreach (GameObject raiz in cena.GetRootGameObjects()) Inspecionar(raiz, caminho); }
            finally { EditorSceneManager.ClosePreviewScene(cena); }
        }
        // Referências gravadas em arquivo, inclusive overrides de instância que o TMP mascara em memória.
        foreach (string caminho in PrefabsEmOrdem().Concat(CenasDoJogo()))
            foreach (string dep in AssetDatabase.GetDependencies(caminho, false))
                if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(TMP_FontAsset) && !dep.StartsWith(PastaFontAssets + "/"))
                {
                    problemas++;
                    log.AppendLine($"  REFERÊNCIA GRAVADA: {caminho} -> {dep}.");
                }
        log.AppendLine($"  {textos} textos TMP em prefabs e cenas; {problemas} fora do padrão.");

        // Caracteres do conteúdo.
        SortedDictionary<int, string> doConteudo = CaracteresDoConteudo();
        foreach (KeyValuePair<int, string> c in doConteudo)
        {
            uint u = (uint)c.Key;
            bool r = regular.characterLookupTable.ContainsKey(u), s = semiBold.characterLookupTable.ContainsKey(u), i = italic.characterLookupTable.ContainsKey(u);
            bool a = apoio != null && apoio.characterLookupTable.ContainsKey(u);
            if (r && s && i) continue;
            string pesos = $"{(r ? "" : " Regular")}{(s ? "" : " SemiBold")}{(i ? "" : " Italic")}";
            log.AppendLine($"  CARACTERE {Descrever(c.Key)} sem glifo em:{pesos}{(a ? " (desenhado pelo asset de símbolos de apoio)" : "")}; ex.: {c.Value}.");
        }
        log.AppendLine($"  {doConteudo.Count} caracteres distintos no conteúdo.");
        return log.ToString();
    }

    // ------------------------------------------------------------------ Conteúdo

    private static readonly Regex LiteralOuComentario = new Regex(
        @"//[^\n]*|/\*[\s\S]*?\*/|@""(?:[^""]|"""")*""|""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)+'", RegexOptions.Compiled);

    private static readonly Regex EscapeUnicode = new Regex(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

    /// <summary>Caracteres (exceto controles) do conteúdo do jogo, com uma origem de exemplo para cada um.</summary>
    internal static SortedDictionary<int, string> CaracteresDoConteudo()
    {
        var achados = new SortedDictionary<int, string>();
        void Anotar(string s, string origem)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (int c in Codigos(s))
                if (c >= 0x20 && !(c >= 0x7F && c < 0xA0) && !achados.ContainsKey(c)) achados[c] = origem;
        }
        void AnotarObjeto(Object o, string origem)
        {
            SerializedProperty p = new SerializedObject(o).GetIterator();
            while (p.Next(true))
                if (p.propertyType == SerializedPropertyType.String) Anotar(p.stringValue, origem);
        }
        void AnotarHierarquia(GameObject raiz, string origem)
        {
            foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null) AnotarObjeto(mb, $"{origem} > {mb.gameObject.name}");
        }

        foreach (string caminho in PrefabsEmOrdem())
            AnotarHierarquia(AssetDatabase.LoadAssetAtPath<GameObject>(caminho), caminho);
        foreach (string caminho in CenasDoJogo())
        {
            Scene cena = EditorSceneManager.OpenPreviewScene(caminho);
            try { foreach (GameObject raiz in cena.GetRootGameObjects()) AnotarHierarquia(raiz, caminho); }
            finally { EditorSceneManager.ClosePreviewScene(cena); }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/ScriptableObjects", "Assets/Resources" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
                if (o != null) AnotarObjeto(o, caminho);
        }
        foreach (string arquivo in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            string fonte = File.ReadAllText(arquivo);
            foreach (Match m in LiteralOuComentario.Matches(fonte))
            {
                if (m.Value.StartsWith("/")) continue;
                string literal = EscapeUnicode.Replace(m.Value, u => ((char)System.Convert.ToInt32(u.Groups[1].Value, 16)).ToString());
                Anotar(literal, arquivo.Replace('\\', '/'));
            }
        }
        return achados;
    }

    // ------------------------------------------------------------------ Utilitários

    /// <summary>Liga o kerning (pares do GPOS) numa lista de recursos do TMP. Devolve se alterou.</summary>
    private static bool GarantirKerning(SerializedProperty recursos, SerializedProperty kerningLegado)
    {
        if (recursos == null) return false;
        // Formato antigo: [0] quer dizer "use o booleano m_enableKerning".
        bool legado = recursos.arraySize == 1 && recursos.GetArrayElementAtIndex(0).intValue == 0;
        if (legado)
        {
            if (kerningLegado != null && kerningLegado.boolValue) return false;
            recursos.GetArrayElementAtIndex(0).intValue = TagKern;
            return true;
        }
        for (int i = 0; i < recursos.arraySize; i++)
            if (recursos.GetArrayElementAtIndex(i).intValue == TagKern) return false;
        recursos.InsertArrayElementAtIndex(recursos.arraySize);
        recursos.GetArrayElementAtIndex(recursos.arraySize - 1).intValue = TagKern;
        return true;
    }

    /// <summary>Escala do texto em relação ao Canvas (a raiz de um Canvas guardado em prefab tem escala 0).</summary>
    private static Vector2 EscalaAcumulada(Transform t)
    {
        Vector2 e = Vector2.one;
        for (Transform atual = t; atual != null; atual = atual.parent)
        {
            Vector3 local = atual.localScale;
            if (Mathf.Abs(local.x) < 1e-5f || Mathf.Abs(local.y) < 1e-5f) continue;
            e = new Vector2(e.x * local.x, e.y * local.y);
        }
        return e;
    }

    private static bool TemKerning(TMP_Text texto)
    {
        var so = new SerializedObject(texto);
        SerializedProperty recursos = so.FindProperty("m_ActiveFontFeatures");
        if (recursos.arraySize == 1 && recursos.GetArrayElementAtIndex(0).intValue == 0)
            return so.FindProperty("m_enableKerning").boolValue;
        for (int i = 0; i < recursos.arraySize; i++)
            if (recursos.GetArrayElementAtIndex(i).intValue == TagKern) return true;
        return false;
    }

    private static bool DaFamilia(TMP_FontAsset fa) =>
        fa != null && AssetDatabase.GetAssetPath(fa).StartsWith(PastaFontAssets + "/EBGaramond-");

    private static bool DaFamilia(Material m) =>
        m != null && AssetDatabase.GetAssetPath(m).StartsWith(PastaFontAssets + "/EBGaramond-");

    private static bool DoAtlas(Material m, TMP_FontAsset fa) =>
        m != null && fa != null && m.HasProperty(ShaderUtilities.ID_MainTex) && m.GetTexture(ShaderUtilities.ID_MainTex) == fa.atlasTexture;

    /// <summary>Prefabs do jogo, os que não contêm outros antes (o aninhado é corrigido antes de quem o usa).</summary>
    private static List<string> PrefabsEmOrdem() =>
        AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => !p.StartsWith("Assets/TextMesh Pro/") && !p.StartsWith("Assets/Plugins/") && !p.StartsWith("Assets/_Recovery/"))
            .OrderBy(p => AssetDatabase.GetDependencies(p, true).Count(d => d.EndsWith(".prefab")))
            .ThenBy(p => p, System.StringComparer.Ordinal)
            .ToList();

    private static List<string> CenasDoJogo() =>
        AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .ToList();

    private static string Caminho(Transform t, Transform raizDoPrefab)
    {
        var partes = new List<string>();
        for (Transform atual = t; atual != null && atual != raizDoPrefab; atual = atual.parent)
            partes.Insert(0, atual.name);
        return string.Join("/", partes);
    }

    private static IEnumerable<int> Codigos(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                yield return char.ConvertToUtf32(s[i], s[i + 1]);
                i++;
            }
            else yield return s[i];
        }
    }

    private static string Texto(IEnumerable<int> codigos) => string.Concat(codigos.Select(char.ConvertFromUtf32));

    private static string Distintos(string s) => Texto(Codigos(s).Distinct().OrderBy(c => c));

    private static string Descrever(int c) => $"'{char.ConvertFromUtf32(c)}' (U+{c:X4})";
}
