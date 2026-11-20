using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuPrincipalManager : MonoBehaviour
{

    [SerializeField] private string nomeDoLevelDeJogo;
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject painelDeConfirmação;
    [Tooltip("Botão Continuar: só aparece quando o save tem uma partida em andamento (não depois do fim do jogo).")]
    [SerializeField] private GameObject botaoContinuar;
    [Tooltip("Pergunta \"começar um novo jogo?\" do botão Jogar, mostrada só quando existe um save.")]
    [SerializeField] private GameObject painelConfirmarNovoJogo;
    [Tooltip("Botão que recebe o foco do teclado quando a pergunta abre (o \"Não\", para o Enter não apagar o progresso).")]
    [SerializeField] private GameObject botaoCancelarNovoJogo;
    [Tooltip("Painel \"Casos resolvidos\" (ArquivoDeCasosUI): relê os casos e desfechos das partidas.")]
    [SerializeField] private GameObject painelCasos;
    [Tooltip("Tela de saves (SlotsDeSaveUI): o Jogar abre esta tela para escolher o slot (novo jogo, carregar ou excluir).")]
    [SerializeField] private GameObject painelSaves;

    [Header("Sliders de Opções (Áudio e Texto)")]
    [Tooltip("Slider de volume da música (OnValueChanged ligado a OnMusicaSliderChanged).")]
    [FormerlySerializedAs("sliderVolume")] [SerializeField] private Slider sliderMusica;
    [Tooltip("Slider de volume dos efeitos: sons, vozes e ambiente (OnValueChanged ligado a OnEfeitosSliderChanged).")]
    [SerializeField] private Slider sliderEfeitos;
    [Tooltip("Slider de velocidade do texto da tela de Opções (OnValueChanged ligado a OnTextSpeedSliderChanged).")]
    [SerializeField] private Slider sliderVelocidadeTexto;

    // Mesma chave do PauseMenu.
    private const string TextSpeedKey = "opt_textspeed";

    private GameObject focoAntesDaConfirmacao;

    private void Start()
    {
        AtualizarContinuar();
    }

    /// <summary>Só com partida em andamento: depois do tribunal o save registra fim de jogo.</summary>
    public void AtualizarContinuar()
    {
        if (botaoContinuar != null) botaoContinuar.SetActive(SistemaDeSave.ExistePartidaEmAndamento);
    }

    // Sem a tela de saves na cena: sem save começa direto; com save pergunta antes.
    public void Jogar()
    {
        if (painelSaves != null)
        {
            AbrirSaves();
            return;
        }
        if (SistemaDeSave.ExisteSave && painelConfirmarNovoJogo != null)
            AbrirConfirmacaoNovoJogo();
        else
            ComecarNovoJogo();
    }

    // O save antigo só é substituído no próximo ponto de save.
    public void ConfirmarNovoJogo()
    {
        if (painelConfirmarNovoJogo != null) painelConfirmarNovoJogo.SetActive(false);
        ComecarNovoJogo();
    }

    private void ComecarNovoJogo()
    {
        ProgressoDoJogo.ComecarNovoJogo();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(nomeDoLevelDeJogo);
        else
            SceneManager.LoadScene(nomeDoLevelDeJogo);
    }

    private void AbrirConfirmacaoNovoJogo()
    {
        EventSystem eventos = EventSystem.current;
        focoAntesDaConfirmacao = eventos != null ? eventos.currentSelectedGameObject : null;
        painelMenuInicial.SetActive(false);
        painelConfirmarNovoJogo.SetActive(true);
        if (eventos != null && botaoCancelarNovoJogo != null) eventos.SetSelectedGameObject(botaoCancelarNovoJogo);
    }

    public void FecharConfirmacaoNovoJogo()
    {
        painelConfirmarNovoJogo.SetActive(false);
        painelMenuInicial.SetActive(true);
        EventSystem eventos = EventSystem.current;
        if (eventos != null)
            eventos.SetSelectedGameObject(focoAntesDaConfirmacao != null && focoAntesDaConfirmacao.activeInHierarchy ? focoAntesDaConfirmacao : null);
    }

    // Carrega o slot jogado por último.
    public void Continuar()
    {
        if (!SistemaDeSave.Carregar() && botaoContinuar != null) botaoContinuar.SetActive(false);
    }

    public void AbrirSaves()
    {
        painelMenuInicial.SetActive(false);
        painelSaves.SetActive(true);
    }

    public void FecharSaves()
    {
        painelSaves.SetActive(false);
        painelMenuInicial.SetActive(true);
        AtualizarContinuar();
    }

    public void ComecarNovoJogoNoSlot(int slot)
    {
        SistemaDeSave.SlotAtivo = slot;
        ComecarNovoJogo();
    }

    /// <summary>Falso se o slot está vazio, ilegível ou com jogo já concluído.</summary>
    public bool CarregarSlot(int slot) => SistemaDeSave.Carregar(slot);

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);

        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(VolumeDoAudio.Musica);
        if (sliderEfeitos != null) sliderEfeitos.SetValueWithoutNotify(VolumeDoAudio.Efeitos);
        if (sliderVelocidadeTexto != null)
            sliderVelocidadeTexto.SetValueWithoutNotify(PlayerPrefs.GetFloat(TextSpeedKey, 0.5f));
    }

    public void OnMusicaSliderChanged(float valor01) => VolumeDoAudio.DefinirMusica(valor01);
    public void OnEfeitosSliderChanged(float valor01) => VolumeDoAudio.DefinirEfeitos(valor01);

    // Só grava: o TypeTextAnimation de cada cena lê o valor ao nascer.
    public void OnTextSpeedSliderChanged(float valor01)
    {
        PlayerPrefs.SetFloat(TextSpeedKey, valor01);
    }

    public void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void FecharOpcoesJogo()
    {
        painelOpcoes.SetActive(false);
    }

    public void AbrirCasos()
    {
        painelMenuInicial.SetActive(false);
        painelCasos.SetActive(true);
    }

    public void FecharCasos()
    {
        painelCasos.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    public void AbrirCreditos()
    {
        painelMenuInicial.SetActive(false);
        painelCreditos.SetActive(true);
    }

    public void FecharCreditos()
    {
        painelCreditos.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

     public void AbrirConfirmação()
    {
        painelMenuInicial.SetActive(false);
        painelDeConfirmação.SetActive(true);
    }

    public void FecharConfirmação()
    {
        painelDeConfirmação.SetActive(false);
        painelMenuInicial.SetActive(true);
    }
    
     public void SairJogo()
    {
        Debug.Log("Saiu do jogo");
        #if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
        #else
        Application.Quit();
        #endif
        
    }
}
