using UnityEngine;

/// <summary>
/// Mostra um SpriteRenderer (ex: uma seta) só enquanto a etapa atual do TutorialManager for "Etapa Id".
/// Diferente do TutorialStepUI (que revela botões e nunca mais os esconde), este indicador some de novo
/// assim que o tutorial avança para a próxima etapa — ideal para setas/marcadores que só fazem sentido
/// enquanto o jogador ainda não chegou no lugar indicado.
///
/// Setup no Editor: coloque este script no mesmo GameObject do SpriteRenderer da seta e preencha
/// "Etapa Id" com o mesmo texto usado no TutorialManager (ex: "ir_para_porao").
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
        // Reforça a inscrição aqui (igual o TutorialStepUI faz), caso o TutorialManager ainda não
        // tivesse rodado seu Awake (e definido Instance) no momento em que este objeto foi habilitado.
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
