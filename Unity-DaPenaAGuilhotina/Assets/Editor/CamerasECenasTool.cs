using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Relatório de testes de 28/09, itens 12 e 20. Nas cenas, não em cópias:
/// - Porão: o limite da câmera (CinemachineConfiner2D) apontava para "CameraConfiner", instância de um prefab que não
///   existe mais (STATUS §3.2), com um polígono que passava das paredes (x de −12,5 a 14,4): a câmera mostrava o vazio
///   além delas. Troca por um objeto de cena "LimitesDaCamera" com o retângulo da sala: das faces externas das duas
///   paredes invisíveis (os colisores altos da cena) e do topo delas até a base do chão.
/// - Menu e Tribunal: a câmera ganha o FMOD Studio Listener (sem ele o FMOD avisava no console e o som 3D não tinha
///   ouvinte). Vale para toda cena do build cuja câmera principal ainda não tenha um.
/// Idempotente. Recusa rodar em Play Mode ou com cena suja; restaura a cena que estava aberta.
/// Menu: Ferramentas > Cenas > Aplicar limite da câmera do porão e FMOD Studio Listener
/// </summary>
public static class CamerasECenasTool
{
    private const string CenaPorao = "Assets/_Project/Scenes/Porao.unity";
    private const string NomeDoLimite = "LimitesDaCamera";
    private const float AlturaMinimaDeParede = 5f;

    [MenuItem("Ferramentas/Cenas/Aplicar limite da câmera do porão e FMOD Studio Listener")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[CamerasECenas]\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        string cenaOriginal = SceneManager.GetActiveScene().path;
        try
        {
            foreach (EditorBuildSettingsScene item in EditorBuildSettings.scenes)
            {
                if (!item.enabled) continue;
                Scene cena = EditorSceneManager.OpenScene(item.path, OpenSceneMode.Single);
                var feito = new List<string>();
                if (item.path == CenaPorao) feito.Add(LimitarCameraDoPorao(cena));
                string ouvinte = GarantirStudioListener(cena);
                if (ouvinte != null) feito.Add(ouvinte);
                feito.RemoveAll(string.IsNullOrEmpty);

                if (cena.isDirty) EditorSceneManager.SaveScene(cena);
                if (feito.Count > 0) r.AppendLine($"  {cena.name}: {string.Join("; ", feito)}.");
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(cenaOriginal)) EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
        if (r.ToString().TrimEnd().EndsWith("]")) r.AppendLine("  Nenhuma alteração.");
        return r.ToString();
    }

    /// <summary>Retângulo da sala do porão (paredes invisíveis e chão), em coordenadas do mundo.</summary>
    public static bool RetanguloDaSala(Scene cena, out Rect sala)
    {
        sala = default;
        float esquerda = float.PositiveInfinity, direita = float.NegativeInfinity, topo = float.NegativeInfinity, baixo = float.PositiveInfinity;
        int paredes = 0;
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            foreach (BoxCollider2D colisor in raiz.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (colisor.isTrigger) continue;
                Bounds b = colisor.bounds;
                if (b.size.y < AlturaMinimaDeParede || colisor.GetComponent<IInteractable>() != null) continue;
                esquerda = Mathf.Min(esquerda, b.min.x); direita = Mathf.Max(direita, b.max.x); topo = Mathf.Max(topo, b.max.y);
                paredes++;
            }
            foreach (SpriteRenderer sprite in raiz.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.name.StartsWith("chão")) baixo = Mathf.Min(baixo, sprite.bounds.min.y);
        }
        if (paredes < 2 || float.IsInfinity(baixo)) return false;
        sala = Rect.MinMaxRect(esquerda, baixo, direita, topo);
        return true;
    }

    private static string LimitarCameraDoPorao(Scene cena)
    {
        CinemachineConfiner2D confinador = Object.FindAnyObjectByType<CinemachineConfiner2D>(FindObjectsInactive.Include);
        if (confinador == null) return "sem CinemachineConfiner2D; nada alterado";
        if (!RetanguloDaSala(cena, out Rect sala)) return "não achei as paredes e o chão da sala; nada alterado";

        var feito = new List<string>();
        GameObject limite = null;
        foreach (GameObject raiz in cena.GetRootGameObjects()) if (raiz.name == NomeDoLimite) limite = raiz;
        if (limite == null)
        {
            limite = new GameObject(NomeDoLimite, typeof(PolygonCollider2D));
            Undo.RegisterCreatedObjectUndo(limite, "Limite da câmera do porão");
            SceneManager.MoveGameObjectToScene(limite, cena);
            feito.Add($"criado '{NomeDoLimite}'");
        }

        var poligono = limite.GetComponent<PolygonCollider2D>();
        limite.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        limite.transform.localScale = Vector3.one;
        var pontos = new[] { new Vector2(sala.xMin, sala.yMin), new Vector2(sala.xMax, sala.yMin), new Vector2(sala.xMax, sala.yMax), new Vector2(sala.xMin, sala.yMax) };
        if (!poligono.isTrigger || poligono.pathCount != 1 || !MesmosPontos(poligono.GetPath(0), pontos))
        {
            poligono.isTrigger = true; // só delimita a câmera: não bloqueia nada
            poligono.pathCount = 1;
            poligono.SetPath(0, pontos);
            EditorUtility.SetDirty(poligono);
            feito.Add($"sala x {sala.xMin:0.00}…{sala.xMax:0.00}, y {sala.yMin:0.00}…{sala.yMax:0.00}");
        }

        if (confinador.BoundingShape2D != poligono)
        {
            Undo.RecordObject(confinador, "Limite da câmera do porão");
            confinador.BoundingShape2D = poligono;
            confinador.InvalidateBoundingShapeCache();
            EditorUtility.SetDirty(confinador);
            feito.Add("confinador da câmera ligado a ele");
        }

        // A instância do prefab perdido ("CameraConfiner") não é mais usada por ninguém.
        foreach (GameObject raiz in cena.GetRootGameObjects())
            if (raiz.name == "CameraConfiner" && PrefabUtility.IsPrefabAssetMissing(raiz))
            {
                Undo.DestroyObjectImmediate(raiz);
                feito.Add("removida a instância do prefab perdido 'CameraConfiner'");
            }

        if (feito.Count > 0) EditorSceneManager.MarkSceneDirty(cena);
        return string.Join("; ", feito);
    }

    private static bool MesmosPontos(Vector2[] a, Vector2[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if ((a[i] - b[i]).sqrMagnitude > 1e-6f) return false;
        return true;
    }

    private static string GarantirStudioListener(Scene cena)
    {
        Camera camera = null;
        foreach (GameObject raiz in cena.GetRootGameObjects())
            foreach (Camera c in raiz.GetComponentsInChildren<Camera>(true))
                if (camera == null || c.CompareTag("MainCamera")) camera = c;
        if (camera == null || camera.GetComponent<StudioListener>() != null) return null;

        Undo.AddComponent<StudioListener>(camera.gameObject);
        EditorSceneManager.MarkSceneDirty(cena);
        return $"FMOD Studio Listener em '{camera.name}'";
    }
}
