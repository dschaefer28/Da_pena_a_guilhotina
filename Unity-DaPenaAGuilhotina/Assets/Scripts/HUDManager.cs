using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Barras de Opinião (Sliders)")]
    public Slider publicOpinionSlider;
    public Slider stateOpinionSlider;

    [Header("Recursos Financeiros")]
    public TextMeshProUGUI capitalText;

    private void Start()
    {
        // 1. Desativa a interação do mouse por segurança
        foreach (var slider in new[] { publicOpinionSlider, stateOpinionSlider })
        {
            if (slider == null) continue;
            slider.interactable = false;
            slider.minValue = 0;
            slider.maxValue = 100;
        }

        // ARQUITETURA BLINDADA: 
        // O Start() garante que o Awake() do GameManager já terminou de rodar na cena.
        if (GameManager.Instance != null)
        {
            // Assina o evento para escutar atualizações futuras (quando usar a Prensa)
            GameManager.Instance.OnStatusChanged += AtualizarTela;
            
            // Puxa os dados imediatamente para sumir com o "New Text" e ajustar as barras
            AtualizarTela(); 
        }
        else
        {
            Debug.LogWarning("[HUD] Falha: GameManager não encontrado na cena durante o Start!");
        }
    }

    private void OnDestroy()
    {
        // Boa Prática: Evita Memory Leaks quando a HUD for destruída na troca de cenas
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatusChanged -= AtualizarTela;
        }
    }

    private void AtualizarTela()
    {
        if (publicOpinionSlider != null) 
            publicOpinionSlider.value = GameManager.Instance.opiniaoPublicaAtual;

        if (stateOpinionSlider != null) 
            stateOpinionSlider.value = GameManager.Instance.opiniaoEstadoAtual;

        if (capitalText != null) 
            capitalText.text = GameManager.Instance.capitalAtual.ToString();
            
        Debug.Log("[HUD] Tela atualizada com os dados do GameManager.");
    }
}
