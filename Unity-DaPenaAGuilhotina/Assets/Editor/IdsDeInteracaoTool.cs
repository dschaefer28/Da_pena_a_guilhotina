using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Preenche o "Id Da Interacao" de NPCMovement e LootInteractable nas cenas do build. Idempotente: IDs já
/// preenchidos e únicos não mudam; só recebem ID os vazios e as cópias repetidas (a primeira na ordem da hierarquia
/// fica com o ID). O ID gerado é IdDeInteracao.IdPadrao (caminho na hierarquia, com "#2", "#3"... para irmãos de
/// mesmo nome) — o mesmo valor que o jogo usa quando o campo está vazio, então saves feitos antes de rodar a
/// ferramenta continuam apontando para o mesmo NPC/objeto (salvo se o ID padrão já estiver ocupado por outro).
/// </summary>
public static class IdsDeInteracaoTool
{
    private const string Campo = "idDaInteracao";

    [MenuItem("Ferramentas/Investigação/Gerar IDs de interação (cenas do build)")]
    public static void GerarPeloMenu() => Debug.Log(Executar(aplicar: true));

    [MenuItem("Ferramentas/Investigação/Validar IDs de interação (cenas do build)")]
    public static void ValidarPeloMenu() => Debug.Log(Executar(aplicar: false));

    /// <summary>Roda nas cenas habilitadas do build e devolve um relatório. aplicar=false só lista problemas.</summary>
    public static string Executar(bool aplicar)
    {
        var relatorio = new StringBuilder("[IdsDeInteracao] ");
        relatorio.AppendLine(aplicar ? "Gerar IDs de interação" : "Validar IDs de interação");

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return relatorio.Append("Saia do Play Mode antes de rodar a ferramenta.").ToString();

        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return relatorio.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas: salve ou descarte antes. Nada foi alterado.").ToString();

        SceneSetup[] configuracaoOriginal = EditorSceneManager.GetSceneManagerSetup();
        int alterados = 0, problemas = 0;
        try
        {
            foreach (EditorBuildSettingsScene cenaDoBuild in EditorBuildSettings.scenes)
            {
                if (!cenaDoBuild.enabled) continue;
                Scene cena = EditorSceneManager.OpenScene(cenaDoBuild.path, OpenSceneMode.Single);
                List<KeyValuePair<Component, string>> interacoes = IdDeInteracao.InteracoesDaCena(cena);
                if (interacoes.Count == 0) continue;

                if (!aplicar)
                {
                    foreach (string problema in IdDeInteracao.Problemas(interacoes))
                    {
                        relatorio.AppendLine($"  {cena.name}: {problema}");
                        problemas++;
                    }
                    continue;
                }

                int alteradosNaCena = Preencher(interacoes, cena.name, relatorio);
                if (alteradosNaCena > 0)
                {
                    EditorSceneManager.MarkSceneDirty(cena);
                    EditorSceneManager.SaveScene(cena);
                    alterados += alteradosNaCena;
                }
            }
        }
        finally
        {
            if (configuracaoOriginal != null && configuracaoOriginal.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(configuracaoOriginal);
        }

        relatorio.Append(aplicar ? $"Concluído: {alterados} ID(s) preenchido(s)." : $"Concluído: {problemas} problema(s).");
        return relatorio.ToString();
    }

    private static int Preencher(List<KeyValuePair<Component, string>> interacoes, string nomeDaCena, StringBuilder relatorio)
    {
        // IDs já preenchidos ficam reservados: um ID novo nunca colide com eles.
        var usados = new HashSet<string>();
        foreach (var par in interacoes)
            if (!string.IsNullOrWhiteSpace(par.Value)) usados.Add(par.Value.Trim());

        var jaMantidos = new HashSet<string>();
        int alterados = 0;
        foreach (var par in interacoes)
        {
            string atual = string.IsNullOrWhiteSpace(par.Value) ? null : par.Value.Trim();
            // Primeiro dono de um ID preenchido fica com ele (repetidos seguintes recebem um novo).
            if (atual != null && jaMantidos.Add(atual))
            {
                if (atual != par.Value) { Gravar(par.Key, atual); alterados++; } // só tira espaços das pontas
                continue;
            }

            // Mesmo valor que o jogo usa com o campo vazio (IdPadrao); só muda se já estiver ocupado por outro ID.
            string baseId = IdDeInteracao.CaminhoNaHierarquia(par.Key.transform);
            string novo = IdDeInteracao.IdPadrao(par.Key.transform);
            for (int n = 2; usados.Contains(novo); n++) novo = baseId + "#" + n;
            usados.Add(novo);

            Gravar(par.Key, novo);
            alterados++;
            relatorio.AppendLine($"  {nomeDaCena}: {par.Key.GetType().Name} '{baseId}' -> '{novo}'" +
                                 (atual != null ? $" (antes repetido: '{atual}')" : string.Empty));
        }
        return alterados;
    }

    // SerializedObject registra override de prefab e Undo corretamente, sem mexer no asset do prefab.
    private static void Gravar(Component componente, string id)
    {
        var serializado = new SerializedObject(componente);
        SerializedProperty propriedade = serializado.FindProperty(Campo);
        if (propriedade == null)
        {
            Debug.LogError($"[IdsDeInteracao] {componente.GetType().Name} não tem o campo '{Campo}'.", componente);
            return;
        }
        propriedade.stringValue = id;
        serializado.ApplyModifiedProperties();
    }
}
