using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    [Header("Configurações de Teste")]
    [Tooltip("Marque para ver os botões no Editor da Unity enquanto programa. Eles sumirão no Build final de PC de qualquer forma.")]
    public bool showInEditorForTesting = true;

    void Awake()
    {
        // 1. Se estiver rodando dentro do Editor da Unity
#if UNITY_EDITOR
        gameObject.SetActive(showInEditorForTesting);

        // 2. Se for o Build final para Android ou iOS
#elif UNITY_ANDROID || UNITY_IOS
        gameObject.SetActive(true);

        // 3. Se for o Build final para PC, Mac, WebGL ou Consoles
#else
        gameObject.SetActive(false);
#endif
    }
}