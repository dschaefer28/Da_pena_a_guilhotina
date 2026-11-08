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

    // Mesmas chaves e padrões do PauseMenu: o que o jogador ajusta aqui vale no jogo, e o contrário.
    private const string TextSpeedKey = "opt_textspeed";

    private GameObject focoAntesDaConfirmacao;

    // O Continuar aparece com uma partida em andamento no save; depois do tribunal o save registra o fim do jogo e não há
    // o que continuar (o Jogar ainda pergunta antes de substituir esse registro).
    private void Start()
    {
        AtualizarContinuar();
    }

    /// <summary>Mostra o Continuar só se algum slot tem partida em andamento (chamado de novo ao excluir um save).</summary>
    public void AtualizarContinuar()
    {
        if (botaoContinuar != null) botaoContinuar.SetActive(SistemaDeSave.ExistePartidaEmAndamento);
    }

    // Jogar: abre a tela de saves (cada slot vazio começa um jogo novo nele; os ocupados carregam ou são excluídos).
    // Sem a tela na cena, volta ao comportamento antigo: sem save começa direto; com save pergunta antes.
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

    // Novo Jogo: o tutorial e as cutscenes começam do zero. O save antigo só é substituído no próximo ponto de save.
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

    // Continuar: volta ao último ponto de save (do slot jogado por último), com o tutorial na etapa em que estava.
    public void Continuar()
    {
        if (!SistemaDeSave.Carregar() && botaoContinuar != null) botaoContinuar.SetActive(false);
    }

    // ===== Tela de saves =====

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

    /// <summary>Novo jogo num slot vazio: a partida passa a gravar nele.</summary>
    public void ComecarNovoJogoNoSlot(int slot)
    {
        SistemaDeSave.SlotAtivo = slot;
        ComecarNovoJogo();
    }

    /// <summary>Carrega o save do slot. Falso se ele não puder ser continuado (vazio, ilegível ou jogo já concluído).</summary>
    public bool CarregarSlot(int slot) => SistemaDeSave.Carregar(slot);

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);

        // Mostra os sliders já na posição salva (mesmos padrões do menu de pause).
        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(VolumeDoAudio.Musica);
        if (sliderEfeitos != null) sliderEfeitos.SetValueWithoutNotify(VolumeDoAudio.Efeitos);
        if (sliderVelocidadeTexto != null)
            sliderVelocidadeTexto.SetValueWithoutNotify(PlayerPrefs.GetFloat(TextSpeedKey, 0.5f));
    }

    // Ligados no OnValueChanged dos sliders de Música e de Efeitos da tela de Opções (o VolumeDoAudio aplica na hora).
    public void OnMusicaSliderChanged(float valor01) => VolumeDoAudio.DefinirMusica(valor01);
    public void OnEfeitosSliderChanged(float valor01) => VolumeDoAudio.DefinirEfeitos(valor01);

    // Ligado no OnValueChanged do slider de Velocidade de Texto da tela de Opções. No menu não há diálogo na tela: o
    // valor fica salvo e o TypeTextAnimation de cada cena do jogo o lê ao nascer.
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
