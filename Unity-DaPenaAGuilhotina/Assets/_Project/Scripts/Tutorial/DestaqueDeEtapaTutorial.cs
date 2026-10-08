using UnityEngine;

/// <summary>
/// Versão de UI do IndicadorDeEtapaTutorial: enquanto a etapa atual do TutorialManager for "Etapa Id", este
/// RectTransform pulsa (escala), chamando a atenção para ele. Usado no HUD_Status do prefab UI na etapa que
/// explica as barras ao abrir a prensa (documento, Fase 1: "HUD de tutorial focada em status").
/// Usa tempo real: a prensa aberta pausa o jogo (timeScale 0).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DestaqueDeEtapaTutorial : MonoBehaviour
{
    [Tooltip("Deve ser exatamente igual ao 'Etapa Id' de uma das etapas do TutorialManager.")]
    public string etapaId;
    [Tooltip("Quanto a escala cresce no pico do pulso (0.06 = 6%).")]
    [Min(0f)] public float amplitude = 0.06f;
    [Tooltip("Pulsos por segundo.")]
    [Min(0.1f)] public float frequencia = 1.2f;

    private Vector3 escalaBase;
    private bool ativo;
    private float inicio;

    private void Awake() => escalaBase = transform.localScale;

    private void OnEnable() => Inscrever();

    private void Start() => Inscrever(); // o TutorialManager pode nascer depois deste objeto

    private void OnDisable()
    {
        if (TutorialManager.Instance != null) TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        Definir(false);
    }

    private void Inscrever()
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        TutorialManager.Instance.OnEtapaAlterada += HandleEtapaAlterada;
        HandleEtapaAlterada(TutorialManager.Instance.EtapaAtualId);
    }

    private void HandleEtapaAlterada(string etapaAtualId) => Definir(!string.IsNullOrEmpty(etapaId) && etapaAtualId == etapaId);

    private void Definir(bool valor)
    {
        if (valor && !ativo) inicio = Time.unscaledTime;
        ativo = valor;
        if (!ativo) transform.localScale = escalaBase;
    }

    private void Update()
    {
        if (!ativo) return;
        float onda = 0.5f - 0.5f * Mathf.Cos((Time.unscaledTime - inicio) * frequencia * 2f * Mathf.PI);
        transform.localScale = escalaBase * (1f + amplitude * onda);
    }
}
