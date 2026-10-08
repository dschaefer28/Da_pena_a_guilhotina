using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

    private GameObject focoAntesDaConfirmacao;

    // O Continuar aparece com uma partida em andamento no save; depois do tribunal o save registra o fim do jogo e não há
    // o que continuar (o Jogar ainda pergunta antes de substituir esse registro).
    private void Start()
    {
        if (botaoContinuar != null) botaoContinuar.SetActive(SistemaDeSave.ExistePartidaEmAndamento);
    }

    // Jogar: sem save, começa direto; com save, pergunta antes, porque o Novo Jogo zera o progresso.
    public void Jogar()
    {
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

    // Continuar: volta ao último ponto de save, com o tutorial na etapa em que estava.
    public void Continuar()
    {
        if (!SistemaDeSave.Carregar() && botaoContinuar != null) botaoContinuar.SetActive(false);
    }

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);
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
