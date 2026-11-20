using UnityEngine;

/// <summary>
/// Mostra o SpriteRenderer só enquanto a etapa atual do tutorial for "Etapa Id".
/// Ao contrário do TutorialStepUI, volta a esconder quando o tutorial avança.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class IndicadorDeEtapaTutorial : MonoBehaviour
{
    [Tooltip("Deve ser exatamente igual ao 'Etapa Id' de uma das etapas configuradas no TutorialManager. " +
             "O indicador fica visível só enquanto essa for a etapa atual.")]
    public string etapaId;

    private SpriteRenderer sprite;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        sprite.enabled = false;
    }

    void OnEnable()
    {
        Inscrever();
    }

    void Start()
    {
        // O TutorialManager pode não ter rodado Awake quando este objeto foi habilitado.
        Inscrever();

        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[IndicadorDeEtapaTutorial] Nenhum TutorialManager encontrado na cena.", this);
        }
    }

    void OnDisable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
    }

    private void Inscrever()
    {
        if (TutorialManager.Instance == null) return;

        TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        TutorialManager.Instance.OnEtapaAlterada += HandleEtapaAlterada;
        HandleEtapaAlterada(TutorialManager.Instance.EtapaAtualId);
    }

    private void HandleEtapaAlterada(string etapaAtualId)
    {
        sprite.enabled = etapaAtualId == etapaId;
    }
}
