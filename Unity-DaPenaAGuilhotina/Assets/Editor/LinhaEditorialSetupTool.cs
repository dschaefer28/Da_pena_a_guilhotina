using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prompt 5: linha editorial na prensa a partir da Fase 2.
/// - liga a linha editorial nas receitas dos casos da mesa das Fases 2 a 4, com os modificadores PROVISÓRIOS abaixo;
/// - põe o LinhaEditorialDaPrensaUI no painel da prensa (UI.prefab), ao lado do SuportesDaPrensaUI.
/// O tutorial não é tocado (usa receita exata, sem linha editorial).
/// Idempotente: só preenche receitas cuja linha editorial nunca foi configurada; valores editados no Inspector ficam.
/// Menu: Ferramentas > Campanha > 4 - Aplicar linha editorial
/// </summary>
public static class LinhaEditorialSetupTool
{
    private const string PrefabUI = "Assets/Prefab/UI.prefab";

    // Valores provisórios (Povo / Estado / Ouro). Iguais em todas as versões da receita, para a escolha não depender
    // da verdade das pistas. Defesa e Agradar deslocam o desnível Povo × Estado em 15 pontos por publicação (a margem
    // da rota é 20); o sensacionalista paga um pouco mais de ouro e agrava em 50% a perda de uma revelação.
    private class Valores
    {
        public int defesaPovo = 10, defesaEstado = -5;
        public int agradarPovo = -5, agradarEstado = 10;
        public int sensacionalistaOuro;
        public int agravamento = 50;
    }

    private static Valores ValoresDaFase(int fase) => new Valores { sensacionalistaOuro = fase <= 2 ? 15 : 20 };

    [MenuItem("Ferramentas/Campanha/4 - Aplicar linha editorial")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[LinhaEditorialSetup] Aplicar linha editorial\n");
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
            if (!receita.linhaEditorial.Vazia) { mantidas++; continue; }

            Valores v = ValoresDaFase(caso.fase);
            ReceitaDeCaso.ConfiguracaoEditorial c = receita.linhaEditorial;
            c.ativa = true;
            c.defesaDoPovo = new ReceitaDeCaso.ModificadorEditorial { povo = v.defesaPovo, estado = v.defesaEstado };
            c.agradarOPoder = new ReceitaDeCaso.ModificadorEditorial { povo = v.agradarPovo, estado = v.agradarEstado };
            c.sensacionalista = new ReceitaDeCaso.ModificadorEditorial { ouro = v.sensacionalistaOuro };
            c.agravamentoPercentual = v.agravamento;
            EditorUtility.SetDirty(receita);
            configuradas++;
        }

        int prefab = RegistrarNoPrefabUI();
        AssetDatabase.SaveAssets();
        r.AppendLine($"  Receitas configuradas: {configuradas}; já configuradas (mantidas): {mantidas}.");
        r.Append($"  Alterações no UI.prefab: {prefab}.");
        return r.ToString();
    }

    private static int RegistrarNoPrefabUI()
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
            if (alterados > 0) PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return alterados;
    }
}
