using FMODUnity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toca o som de clique (event:/clique1 por padrão) quando o botão é acionado (mouse, toque ou confirmação).
/// Configurável por botão na Hierarchy: desmarque o componente para silenciar ou troque o Evento Clique no Inspector.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class SomDeCliqueDoBotao : MonoBehaviour
{
    public const string CaminhoPadrao = "event:/clique1";
    public const string GuidPadrao = "{f77e4a6d-f92d-4445-9803-14d1394cbcc6}";

    [Tooltip("Evento FMOD tocado ao clicar no botão. Vazio usa event:/clique1.")]
    [SerializeField] private EventReference eventoClique;

    private Button botao;

    public EventReference EventoClique
    {
        get => eventoClique.IsNull ? EventoPadrao() : eventoClique;
        set => eventoClique = value;
    }

    public static EventReference EventoPadrao()
    {
#if UNITY_EDITOR
        if (EventReference.GuidLookupDelegate != null)
        {
            EventReference encontrado = EventReference.Find(CaminhoPadrao);
            if (!encontrado.Guid.IsNull) return encontrado;
        }
        return new EventReference { Guid = FMOD.GUID.Parse(GuidPadrao), Path = CaminhoPadrao };
#else
        return new EventReference { Guid = FMOD.GUID.Parse(GuidPadrao) };
#endif
    }

    /// <summary>Garante o componente no botão com o evento padrão preenchido.</summary>
    public static SomDeCliqueDoBotao Garantir(Button alvo)
    {
        if (alvo == null) return null;
        var som = alvo.GetComponent<SomDeCliqueDoBotao>();
        if (som == null) som = alvo.gameObject.AddComponent<SomDeCliqueDoBotao>();
        som.PreencherPadraoSeVazio();
        return som;
    }

    public void PreencherPadraoSeVazio()
    {
        if (eventoClique.IsNull) eventoClique = EventoPadrao();
#if UNITY_EDITOR
        else if (string.IsNullOrEmpty(eventoClique.Path) && eventoClique.Guid == FMOD.GUID.Parse(GuidPadrao))
            eventoClique.Path = CaminhoPadrao;
#endif
    }

    private void Reset() => PreencherPadraoSeVazio();

#if UNITY_EDITOR
    private void OnValidate() => PreencherPadraoSeVazio();
#endif

    private void Awake()
    {
        PreencherPadraoSeVazio();
        botao = GetComponent<Button>();
        if (botao != null)
        {
            botao.onClick.RemoveListener(TocarClique);
            botao.onClick.AddListener(TocarClique);
        }
    }

    private void OnDestroy()
    {
        if (botao != null) botao.onClick.RemoveListener(TocarClique);
    }

    // Não remove em OnDisable: botões de fechar painel desativam o próprio pai no 1º listener de onClick,
    // antes deste listener de runtime rodar na mesma invocação.
    public void TocarClique()
    {
        if (!enabled) return;
        AudioSeguro.TocarUmaVez(EventoClique);
    }
}
