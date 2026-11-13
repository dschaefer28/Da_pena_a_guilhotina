using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Linha editorial (teor) na prensa a partir da Fase 2. Desde 08/11/2026 são duas opções:
///   Neutro — sem modificador: o efeito normal do caso (o lado do caso sobe, o outro cai);
///   Sensacionalista — mais ouro, mas o lado que o caso favorece ganha menos que no Neutro e o outro cai o mesmo.
/// O modificador do sensacionalista sai do lado do caso (versão Fatos da receita), com os mesmos parâmetros para os
/// casos pró-povo e pró-Estado; nos mistos a perda é dividida entre as duas barras.
/// - "4": liga a linha nas receitas das Fases 2 a 4 que nunca foram configuradas (valores editados ficam);
/// - "4b": REDEFINE o sensacionalista de todas essas receitas com os valores abaixo, desliga o agravamento do desmentido
///   e atualiza os textos da janela no UI.prefab.
/// Ambas põem o LinhaEditorialDaPrensaUI no painel da prensa (UI.prefab), ao lado do SuportesDaPrensaUI.
/// O tutorial não é tocado (usa receita exata, sem linha editorial).
/// </summary>
public static class LinhaEditorialSetupTool
{
    private const string PrefabUI = "Assets/Prefabs/UI.prefab";

    /// <summary>Quanto o lado favorecido ganha a menos no sensacionalista (nos mistos, metade em cada barra).</summary>
    public const int PerdaDoLadoFavorecido = 5; // 13/11/2026: era 10, metade junto com as receitas
    /// <summary>Ouro a mais do sensacionalista.</summary>
    public const int OuroDoSensacionalista = 15;

    /// <summary>
    /// Modificador do sensacionalista para a receita, pelo lado do caso na versão Fatos: barra que sobe = lado favorecido.
    /// Pró-povo (+Povo/−Estado): Povo −5, Estado 0. Pró-Estado: Estado −5, Povo 0. Misto (as duas sobem) ou sem lado:
    /// −2 em cada. Sempre +15 de ouro. Igual em todas as versões, para não depender da verdade das pistas.
    /// </summary>
    public static ReceitaDeCaso.ModificadorEditorial ModificadorSensacionalista(ReceitaDeCaso receita)
    {
        bool povo = receita != null && receita.soFatos != null && receita.soFatos.povo > 0;
        bool estado = receita != null && receita.soFatos != null && receita.soFatos.estado > 0;
        var m = new ReceitaDeCaso.ModificadorEditorial { ouro = OuroDoSensacionalista };
        if (povo == estado) { m.povo = -PerdaDoLadoFavorecido / 2; m.estado = -PerdaDoLadoFavorecido / 2; }
        else if (povo) m.povo = -PerdaDoLadoFavorecido;
        else m.estado = -PerdaDoLadoFavorecido;
        return m;
    }

    [MenuItem("Ferramentas/Campanha/4 - Aplicar linha editorial")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar(false));

    [MenuItem("Ferramentas/Campanha/4b - Teor com duas opções (redefinir valores)")]
    public static void RedefinirPeloMenu() => Debug.Log(Aplicar(true));

    public static string Aplicar(bool redefinir)
    {
        var r = new StringBuilder($"[LinhaEditorialSetup] {(redefinir ? "Redefinir teor (Neutro / Sensacionalista)" : "Aplicar linha editorial")}\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUI);
        CaseSelectionUI mesa = ui != null ? ui.GetComponentInChildren<CaseSelectionUI>(true) : null;
        if (mesa == null || mesa.availableCases == null) return r.Append("Mesa de Casos não encontrada no UI.prefab. Nada foi alterado.").ToString();

        int configuradas = 0, mantidas = 0;
        var vistas = new HashSet<ReceitaDeCaso>();
        foreach (CaseData caso in mesa.availableCases)
        {
            if (caso == null || caso.fase < 2) continue;
            ReceitaDeCaso receita = caso.receitaDoPanfleto;
            if (receita == null) { r.AppendLine($"  AVISO: '{caso.name}' sem receita de caso."); continue; }
            if (!vistas.Add(receita)) continue;
            if (receita.linhaEditorial == null) receita.linhaEditorial = new ReceitaDeCaso.ConfiguracaoEditorial();
            if (!redefinir && !receita.linhaEditorial.Vazia) { mantidas++; continue; }

            ReceitaDeCaso.ConfiguracaoEditorial c = receita.linhaEditorial;
            c.ativa = true;
            c.sensacionalista = ModificadorSensacionalista(receita);
            c.agravamentoPercentual = 0;
            EditorUtility.SetDirty(receita);
            configuradas++;
            r.AppendLine($"  {receita.name}: Sensacionalista Povo {c.sensacionalista.povo:+0;-0;0}, Estado {c.sensacionalista.estado:+0;-0;0}, Ouro +{c.sensacionalista.ouro}.");
        }

        int prefab = RegistrarNoPrefabUI(redefinir);
        AssetDatabase.SaveAssets();
        r.AppendLine($"  Receitas configuradas: {configuradas}; já configuradas (mantidas): {mantidas}.");
        r.Append($"  Alterações no UI.prefab: {prefab}.");
        return r.ToString();
    }

    private static int RegistrarNoPrefabUI(bool redefinirTextos)
    {
        int alterados = 0;
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            CraftingPress prensa = raiz.GetComponentInChildren<CraftingPress>(true);
            if (prensa == null) { Debug.LogError("[LinhaEditorialSetup] CraftingPress não encontrada no UI.prefab."); return 0; }
            LinhaEditorialDaPrensaUI seletor = prensa.GetComponent<LinhaEditorialDaPrensaUI>();
            if (seletor == null)
            {
                seletor = prensa.gameObject.AddComponent<LinhaEditorialDaPrensaUI>();
                alterados++;
            }
            if (seletor.fonte == null)
            {
                SuportesDaPrensaUI suportes = prensa.GetComponent<SuportesDaPrensaUI>();
                TMPro.TMP_FontAsset fonte = suportes != null && suportes.fonte != null ? suportes.fonte
                    : prensa.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)?.font;
                if (fonte != null) { seletor.fonte = fonte; alterados++; }
            }
            if (redefinirTextos)
            {
                // O texto antigo do sensacionalista falava do desmentido agravado, que saiu.
                alterados += Trocar(ref seletor.descricaoNeutro, LinhaEditorialDaPrensaUI.DescricaoNeutroPadrao);
                alterados += Trocar(ref seletor.descricaoSensacionalista, LinhaEditorialDaPrensaUI.DescricaoSensacionalistaPadrao);
                alterados += Trocar(ref seletor.rotuloDoAjuste, LinhaEditorialDaPrensaUI.RotuloDoAjustePadrao);
            }
            if (alterados > 0) PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return alterados;
    }

    private static int Trocar(ref string campo, string valor)
    {
        if (campo == valor) return 0;
        campo = valor;
        return 1;
    }
}
