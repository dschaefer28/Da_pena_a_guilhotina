using UnityEngine;

/// <summary>
/// Gerencia o congelamento do jogo de forma centralizada, por CONTAGEM de solicitações.
/// Enquanto houver pelo menos 1 solicitação ativa (inventário, prensa, menu de pause, diálogo...),
/// Time.timeScale fica em 0. O jogo só volta a rodar quando TODAS as solicitações forem liberadas.
///
/// Isso elimina a "guerra do Time.timeScale": abrir o pause e o inventário juntos e fechar
/// um deles não descongela mais o jogo por engano.
///
/// Coloque este script em um GameObject único na primeira cena (ele sobrevive à troca de cenas).
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    private int pauseRequests = 0;

    /// <summary>True enquanto houver qualquer solicitação de pause ativa.</summary>
    public bool IsPaused => pauseRequests > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyTimeScale();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Registra uma solicitação de pause. Chame ao ABRIR uma UI modal.</summary>
    public void PushPause()
    {
        pauseRequests++;
        ApplyTimeScale();
    }

    /// <summary>Libera uma solicitação de pause. Chame ao FECHAR uma UI modal.</summary>
    public void PopPause()
    {
        pauseRequests = Mathf.Max(0, pauseRequests - 1);
        ApplyTimeScale();
    }

    /// <summary>Zera todas as solicitações e descongela o jogo (use em troca de cena / carregar menu).</summary>
    public void ResetPause()
    {
        pauseRequests = 0;
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = pauseRequests > 0 ? 0f : 1f;
    }

    /// <summary>
    /// Atalho estático seguro: se não existir PauseManager na cena, cai no comportamento antigo
    /// (mexe direto no Time.timeScale), então nada quebra se o objeto não tiver sido adicionado ainda.
    /// </summary>
    public static void RequestPause(bool pause)
    {
        if (Instance != null)
        {
            if (pause) Instance.PushPause();
            else Instance.PopPause();
        }
        else
        {
            Time.timeScale = pause ? 0f : 1f;
        }
    }

    /// <summary>Atalho estático para zerar o pause mesmo sem instância.</summary>
    public static void ForceReset()
    {
        if (Instance != null) Instance.ResetPause();
        else Time.timeScale = 1f;
    }
}
