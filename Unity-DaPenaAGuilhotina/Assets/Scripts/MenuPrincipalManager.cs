using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [Tooltip("Botão Continuar: só aparece quando existe um save.")]
    [SerializeField] private GameObject botaoContinuar;

    private void Start()
    {
        if (botaoContinuar != null) botaoContinuar.SetActive(SistemaDeSave.ExisteSave);
    }

    // Novo Jogo: o tutorial e as cutscenes começam do zero. O save antigo só é substituído no próximo ponto de save.
    public void Jogar()
    {
        ProgressoDoJogo.ComecarNovoJogo();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadScene(nomeDoLevelDeJogo);
        else
            SceneManager.LoadScene(nomeDoLevelDeJogo);
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
