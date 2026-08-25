using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Padrão Singleton: permite que qualquer script acesse o GameManager de forma fácil
    public static GameManager Instance;

    [Header("Dados da Investigação")]
    public CaseData casoEscolhido; // Guarda o ScriptableObject que o player escolher

    void Awake()
    {
        // Garante que só exista um GameManager no jogo inteiro
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Isso o torna "imortal" entre as cenas
        }
        else
        {
            Destroy(gameObject); // Se já existir outro, destrói o clone
        }
    }

    // Função que a interface da Mesa vai chamar quando o player clicar em "Confirmar"
    public void ConfirmarCaso(CaseData caso)
    {
        casoEscolhido = caso;
        Debug.Log($"Caso escolhido e salvo no sistema: {caso.caseTitle}");
    }
}