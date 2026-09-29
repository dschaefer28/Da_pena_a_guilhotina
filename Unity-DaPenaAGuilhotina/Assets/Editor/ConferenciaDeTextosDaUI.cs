using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conferência dos textos visíveis na tela (Play Mode), para validar janelas em várias resoluções sem depender só do olho:
/// - palavra partida no meio por falta de largura (ex.: "COM/PRAR");
/// - texto maior que a própria caixa (vaza da moldura);
/// - texto cortado por reticências ou truncado;
/// - texto fora da tela;
/// - texto cortado pela máscara de uma lista (informado à parte quando a lista rola: basta rolar para ver).
/// Considera só textos ativos e visíveis (alpha herdado > 0). Com nomes de raiz, confere só os textos dentro delas.
/// Menu: Ferramentas > UI > Conferir textos na tela (Play Mode)
/// </summary>
public static class ConferenciaDeTextosDaUI
{
    private const float Tolerancia = 2f;

    [MenuItem("Ferramentas/UI/Conferir textos na tela (Play Mode)")]
    public static void ConferirPeloMenu() => Debug.Log(Conferir());

    /// <summary>Relatório de uma linha por problema; a primeira linha resume. Raízes vazias = todos os textos da tela.</summary>
    public static string Conferir(params string[] raizes)
    {
        var problemas = new List<string>();
        var rolaveis = new List<string>();
        var reportados = new HashSet<int>();
        int conferidos = 0;

        foreach (TextMeshProUGUI tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            if (!tmp.isActiveAndEnabled || string.IsNullOrWhiteSpace(tmp.text) || tmp.canvas == null) continue;
            if (tmp.canvasRenderer.GetInheritedAlpha() < 0.01f || tmp.color.a < 0.01f) continue;
            if (raizes != null && raizes.Length > 0 && !DentroDeAlguma(tmp.transform, raizes)) continue;

            tmp.ForceMeshUpdate();
            TMP_TextInfo info = tmp.textInfo;
            if (info == null || info.characterCount == 0) continue;
            conferidos++;
            string nome = Caminho(tmp.transform);

            for (int l = 0; l < info.lineCount - 1; l++)
            {
                int ultimo = info.lineInfo[l].lastCharacterIndex;
                int seguinte = ultimo + 1;
                if (ultimo < 0 || seguinte >= info.characterCount) continue;
                char a = info.characterInfo[ultimo].character, b = info.characterInfo[seguinte].character;
                if (char.IsLetterOrDigit(a) && char.IsLetterOrDigit(b))
                    problemas.Add($"palavra partida em '{nome}': \"…{Trecho(info, ultimo)}|{Trecho(info, seguinte, false)}…\"");
            }

            // A caixa de cada linha usa o ascendente/descendente da fonte, maior que as letras: até 1/4 do corpo da
            // fonte para fora não aparece. Mais que isso é uma linha (ou parte dela) desenhada fora do lugar.
            float folga = Mathf.Max(Tolerancia, 0.25f * tmp.fontSize);
            Rect caixa = tmp.rectTransform.rect;
            Bounds texto = tmp.textBounds;
            Rect naTela = RetanguloNaTela(tmp, texto);
            float folgaNaTela = folga * tmp.canvas.rootCanvas.scaleFactor;

            // Dentro de uma lista com máscara: o que está rolado para fora não aparece (o jogador chega nele rolando); o
            // que está só em parte dentro é informado à parte se a lista rola.
            RectMask2D mascara = tmp.GetComponentInParent<RectMask2D>();
            if (mascara != null)
            {
                Rect area = RetanguloNaTela(mascara.rectTransform);
                if (!area.Overlaps(naTela)) continue;
                bool inteiro = naTela.xMin >= area.xMin - folgaNaTela && naTela.xMax <= area.xMax + folgaNaTela &&
                               naTela.yMin >= area.yMin - folgaNaTela && naTela.yMax <= area.yMax + folgaNaTela;
                if (!inteiro)
                {
                    ScrollRect lista = mascara.GetComponent<ScrollRect>();
                    bool rola = lista != null && lista.content != null && lista.content.rect.height > mascara.rectTransform.rect.height + 1f;
                    (rola ? rolaveis : problemas).Add($"cortado pela área '{mascara.name}' em '{nome}'" + (rola ? " (a lista rola)" : string.Empty));
                    if (rola) continue;
                }
            }

            if (texto.max.y > caixa.yMax + folga || texto.min.y < caixa.yMin - folga ||
                texto.max.x > caixa.xMax + folga || texto.min.x < caixa.xMin - folga)
                problemas.Add($"texto maior que a caixa em '{nome}' (texto {texto.size.x:0}x{texto.size.y:0}, caixa {caixa.width:0}x{caixa.height:0})");

            if (tmp.overflowMode != TextOverflowModes.Overflow && tmp.isTextOverflowing)
                problemas.Add($"texto cortado ({tmp.overflowMode}) em '{nome}'");

            if (naTela.xMin < -folgaNaTela || naTela.yMin < -folgaNaTela || naTela.xMax > Screen.width + folgaNaTela || naTela.yMax > Screen.height + folgaNaTela)
                problemas.Add($"fora da tela em '{nome}' ({naTela.xMin:0},{naTela.yMin:0})–({naTela.xMax:0},{naTela.yMax:0}) em {Screen.width}x{Screen.height}");

            // Vazar da moldura: o texto passa da borda do painel/botão visível mais próximo que o contém. Rótulos postos
            // de propósito fora da caixa do pai (ex.: "Entrada 1" embaixo do slot da prensa) não contam.
            Image moldura = MolduraDe(tmp);
            if (moldura != null && RetanguloNaTela(moldura.rectTransform).Contains(RetanguloNaTela(tmp.rectTransform).center))
            {
                Rect painel = RetanguloNaTela(moldura.rectTransform);
                if (naTela.xMin < painel.xMin - folgaNaTela || naTela.xMax > painel.xMax + folgaNaTela ||
                    naTela.yMin < painel.yMin - folgaNaTela || naTela.yMax > painel.yMax + folgaNaTela)
                    problemas.Add($"texto vaza de '{moldura.name}' em '{nome}'");
            }

            // Empurrado para fora pelo layout: um elemento posicionado por um layout group (o próprio texto ou o botão/linha
            // que o contém) inteiro ou em parte fora do grupo. Pega o pior caso, que a verificação da moldura não vê
            // (ex.: o "Cancelar" empurrado para baixo do painel da prensa). Listas roláveis ficam de fora (o grupo cresce).
            for (Transform filho = tmp.transform; filho.parent != null; filho = filho.parent)
            {
                var grupo = filho.parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if (grupo == null || !grupo.enabled || !(filho is RectTransform rtFilho) || !reportados.Add(filho.GetInstanceID())) continue;
                ScrollRect rolagem = filho.parent.parent != null ? filho.parent.parent.GetComponent<ScrollRect>() : null;
                if (rolagem != null && rolagem.content == filho.parent) continue;
                Rect elemento = RetanguloNaTela(rtFilho), area = RetanguloNaTela((RectTransform)filho.parent);
                if (elemento.xMin < area.xMin - folgaNaTela || elemento.xMax > area.xMax + folgaNaTela ||
                    elemento.yMin < area.yMin - folgaNaTela || elemento.yMax > area.yMax + folgaNaTela)
                    problemas.Add($"'{Caminho(filho)}' empurrado para fora de '{filho.parent.name}' pelo layout");
            }
        }

        var r = new StringBuilder($"[ConferenciaDeTextos] {Screen.width}x{Screen.height}: {conferidos} texto(s) conferido(s), {problemas.Count} problema(s)");
        r.Append(rolaveis.Count > 0 ? $", {rolaveis.Count} parcialmente fora de lista rolável.\n" : ".\n");
        foreach (string p in problemas) r.AppendLine("  PROBLEMA: " + p);
        foreach (string p in rolaveis) r.AppendLine("  rolável: " + p);
        return r.ToString();
    }

    // Ancestral mais próximo com uma imagem visível (fundo de painel, cartão ou botão). Fundos que cobrem a tela inteira
    // (escurecimento de janela modal) não contam como moldura.
    private static Image MolduraDe(TextMeshProUGUI tmp)
    {
        for (Transform p = tmp.transform.parent; p != null; p = p.parent)
        {
            Image imagem = p.GetComponent<Image>();
            if (imagem == null || !imagem.enabled || imagem.color.a < 0.05f || imagem.canvasRenderer.GetInheritedAlpha() < 0.05f) continue;
            Rect r = RetanguloNaTela(imagem.rectTransform);
            if (r.width >= Screen.width - 1f && r.height >= Screen.height - 1f) continue;
            return imagem;
        }
        return null;
    }

    private static bool DentroDeAlguma(Transform t, string[] raizes)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (System.Array.IndexOf(raizes, p.name) >= 0) return true;
        return false;
    }

    private static string Caminho(Transform t)
    {
        string caminho = t.name;
        for (Transform p = t.parent; p != null && p.parent != null; p = p.parent) caminho = p.name + "/" + caminho;
        return caminho;
    }

    private static string Trecho(TMP_TextInfo info, int indice, bool antes = true)
    {
        var sb = new StringBuilder();
        if (antes) for (int i = Mathf.Max(0, indice - 6); i <= indice; i++) sb.Append(info.characterInfo[i].character);
        else for (int i = indice; i < Mathf.Min(info.characterCount, indice + 6); i++) sb.Append(info.characterInfo[i].character);
        return sb.ToString().Replace("\n", " ");
    }

    private static Rect RetanguloNaTela(TextMeshProUGUI tmp, Bounds local)
    {
        Vector3 a = tmp.transform.TransformPoint(local.min), b = tmp.transform.TransformPoint(local.max);
        return ParaTela(tmp.canvas, a, b);
    }

    private static Rect RetanguloNaTela(RectTransform rt)
    {
        var cantos = new Vector3[4];
        rt.GetWorldCorners(cantos);
        return ParaTela(rt.GetComponentInParent<Canvas>(), cantos[0], cantos[2]);
    }

    private static Rect ParaTela(Canvas canvas, Vector3 a, Vector3 b)
    {
        Camera cam = null;
        if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.rootCanvas.worldCamera != null ? canvas.rootCanvas.worldCamera : Camera.main; // aviso "E": canvas no mundo
        Vector2 sa = RectTransformUtility.WorldToScreenPoint(cam, a), sb = RectTransformUtility.WorldToScreenPoint(cam, b);
        return Rect.MinMaxRect(Mathf.Min(sa.x, sb.x), Mathf.Min(sa.y, sb.y), Mathf.Max(sa.x, sb.x), Mathf.Max(sa.y, sb.y));
    }
}
