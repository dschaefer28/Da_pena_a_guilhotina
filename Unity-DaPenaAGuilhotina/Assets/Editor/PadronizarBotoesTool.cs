using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Aplica o EstiloDeBotao (sprite laranja com contorno claro, texto centralizado com respiro) a todos os botões das
/// cenas abertas. Ignora botões transparentes (áreas de clique, como o "avançar" do diálogo e o "Pular tutorial").
/// </summary>
public static class PadronizarBotoesTool
{
    [MenuItem("Ferramentas/UI/Padronizar botões das cenas abertas")]
    public static void PadronizarCenasAbertas()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[PadronizarBotoes] Saia do Play Mode antes de padronizar as cenas.");
            return;
        }
        int total = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene cena = SceneManager.GetSceneAt(i);
            if (!cena.isLoaded) continue;
            int naCena = 0;
            foreach (GameObject raiz in cena.GetRootGameObjects())
                foreach (Button botao in raiz.GetComponentsInChildren<Button>(true))
                    if (Padronizar(botao)) naCena++;
            if (naCena > 0) EditorSceneManager.MarkSceneDirty(cena);
            total += naCena;
        }
        Debug.Log($"[PadronizarBotoes] {total} botão(ões) padronizado(s). Salve as cenas para manter.");
    }

    /// <summary>Retorna false para botões sem fundo visível (não são botões "desenhados").</summary>
    public static bool Padronizar(Button botao)
    {
        Image fundo = botao.targetGraphic as Image;
        if (fundo == null) fundo = botao.GetComponent<Image>();
        if (fundo == null || fundo.color.a < 0.01f) return false;

        Undo.RecordObject(fundo, "Padronizar botão");
        Undo.RecordObject(botao, "Padronizar botão");
        if (!EstiloDeBotao.Aplicar(fundo, botao)) return false;

        var layout = botao.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layout != null)
        {
            Undo.RecordObject(layout, "Padronizar botão");
            layout.padding = EstiloDeBotao.RecuosDoRotulo();
        }
        foreach (TMP_Text rotulo in botao.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(rotulo, "Padronizar botão");
            Undo.RecordObject(rotulo.rectTransform, "Padronizar botão");
            if (layout != null) { rotulo.color = Color.white; EstiloDeBotao.ConfigurarTexto(rotulo); }
            else EstiloDeBotao.AjustarRotulo(rotulo);
        }
        return true;
    }
}
