using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FMODUnity;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    public GameObject container;
    public GameObject opções;

    [Header("Sliders de Opções (Áudio e Texto)")]
    public Slider sliderVolume;
    public Slider sliderVelocidadeTexto;

    private const string VolumeKey = "opt_volume";
    private const string TextSpeedKey = "opt_textspeed";
    // Mesmos valores usados em TypeTextAnimation.Awake() pra converter o slider (0-1) em delay (segundos).
    private const float TypeDelayLento = 0.12f;
    private const float TypeDelayRapido = 0.01f;

    /// <summary>True enquanto o menu de pause estiver aberto na tela.</summary>
    public bool IsOpen => container != null && container.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Aplica o volume salvo assim que a cena carrega (o FMOD mantém o volume do bus
        // entre cenas, então isso garante que uma cena nova comece com o valor certo).
        RuntimeManager.GetBus("bus:/").setVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // Lê a tecla Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Lógica de "Toggle": Se já estiver aberto, fecha. Se estiver fechado, abre.
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
        // Não deixa pausar com o inventário aberto (evita os dois modais brigando pela tela/raycast).
        var inventoryManager = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf)
        {
            Debug.Log("Não é possível pausar com o inventário aberto.");
            return;
        }

        container.SetActive(true);
        Time.timeScale = 0;

        // Desliga o raycast dos controles mobile enquanto o menu de pause está aberto.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    public void ResumirButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;

        // Devolve o raycast pros controles mobile assim que o menu fecha.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);
    }

    public void ConfigButton()
    {
        opções.SetActive(true);
        // Removi o Time.timeScale = 1 daqui.

        // Mostra os sliders já na posição salva (senão sempre reabriam no valor padrão 0.429).
        if (sliderVolume != null)
            sliderVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (sliderVelocidadeTexto != null)
            sliderVelocidadeTexto.SetValueWithoutNotify(PlayerPrefs.GetFloat(TextSpeedKey, 0.5f));
    }

    // Ligado no OnValueChanged do slider de Volume da tela de Opções.
    public void OnVolumeSliderChanged(float valor01)
    {
        PlayerPrefs.SetFloat(VolumeKey, valor01);
        RuntimeManager.GetBus("bus:/").setVolume(valor01);
    }

    // Ligado no OnValueChanged do slider de Velocidade de Texto da tela de Opções.
    public void OnTextSpeedSliderChanged(float valor01)
    {
        PlayerPrefs.SetFloat(TextSpeedKey, valor01);

        // Aplica na hora em qualquer TypeTextAnimation já existente na cena atual
        // (o valor salvo também é lido por ela mesma no Awake, pra cenas futuras).
        var typeText = FindAnyObjectByType<TypeTextAnimation>();
        if (typeText != null)
            typeText.typeDelay = Mathf.Lerp(TypeDelayLento, TypeDelayRapido, valor01);
    }

    /// <summary>
    /// Botão "Voltar" da tela de opções do menu de pause: fecha as opções e volta
    /// pro menu de pause (sem alterar Time.timeScale, que continua 0 até o jogador
    /// dar Resumir). Antes esse botão não tinha nenhum método pra chamar.
    /// </summary>
    public void VoltarOpcoesButton()
    {
        if (opções != null) opções.SetActive(false);
    }

    public void MainMenuButton()
    {

        Time.timeScale = 1;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene("Menu principal");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu principal");
    }

    public void TogglePauseMobile()
    {
        // Se o menu já estiver aberto, fecha. Se estiver fechado, pausa.
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