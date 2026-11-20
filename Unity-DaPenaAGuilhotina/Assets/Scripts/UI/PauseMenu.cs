using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public GameObject container;
    public GameObject opções;

    [Header("Sliders de Opções (Áudio e Texto)")]
    [FormerlySerializedAs("sliderVolume")] public Slider sliderMusica;
    public Slider sliderEfeitos;
    public Slider sliderVelocidadeTexto;

    private const string TextSpeedKey = "opt_textspeed";
    // Precisam bater com TypeTextAnimation.Awake().
    private const float TypeDelayLento = 0.12f;
    private const float TypeDelayRapido = 0.01f;

    public bool IsOpen => container != null && container.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (container.activeSelf)
            {
                ResumirButton();
            }
            else
            {
                Pausar();
            }
        }
    }


    private void Pausar()
    {
        // Janela modal aberta (ou fechada pelo mesmo Esc neste frame): o Esc era dela.
        if (JanelasModais.BloqueiaPausa) return;

        // A cutscene devolve o timeScale ao terminar: um pause por baixo ficaria aberto com o jogo andando.
        if (CutsceneLegendas.EmExibicao) return;

        // Dois modais brigariam pela tela e pelo raycast.
        var inventoryManager = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf)
        {
            Debug.Log("Não é possível pausar com o inventário aberto.");
            return;
        }

        container.SetActive(true);
        Time.timeScale = 0;

        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    public void ResumirButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;

        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);
    }

    public void ConfigButton()
    {
        opções.SetActive(true);

        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(VolumeDoAudio.Musica);
        if (sliderEfeitos != null) sliderEfeitos.SetValueWithoutNotify(VolumeDoAudio.Efeitos);
        if (sliderVelocidadeTexto != null)
            sliderVelocidadeTexto.SetValueWithoutNotify(PlayerPrefs.GetFloat(TextSpeedKey, 0.5f));
    }

    public void OnMusicaSliderChanged(float valor01) => VolumeDoAudio.DefinirMusica(valor01);
    public void OnEfeitosSliderChanged(float valor01) => VolumeDoAudio.DefinirEfeitos(valor01);

    public void OnTextSpeedSliderChanged(float valor01)
    {
        PlayerPrefs.SetFloat(TextSpeedKey, valor01);

        // Cenas futuras leem o valor salvo no Awake; aqui aplica na cena atual.
        var typeText = FindAnyObjectByType<TypeTextAnimation>();
        if (typeText != null)
            typeText.typeDelay = Mathf.Lerp(TypeDelayLento, TypeDelayRapido, valor01);
    }

    // Não mexe no timeScale: o jogo continua pausado até o Resumir.
    public void VoltarOpcoesButton()
    {
        if (opções != null) opções.SetActive(false);
    }

    /// <summary>Nome exato da cena em Build Settings (Assets/Scenes/menu principal.unity).</summary>
    public const string CenaDoMenu = "menu principal";

    public void MainMenuButton()
    {

        Time.timeScale = 1;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(CenaDoMenu);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(CenaDoMenu);
    }

    public void TogglePauseMobile()
    {
        if (container.activeSelf)
        {
            ResumirButton();
        }
        else
        {
            Pausar();
        }
    }
}