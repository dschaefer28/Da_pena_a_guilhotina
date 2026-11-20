using UnityEngine;

/// <summary>
/// Pause por contagem de solicitações: Time.timeScale fica em 0 enquanto houver qualquer UI modal aberta,
/// para que fechar uma delas não descongele o jogo com outra ainda aberta.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    private int pauseRequests = 0;

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

    public void PushPause()
    {
        pauseRequests++;
        ApplyTimeScale();
    }

    public void PopPause()
    {
        pauseRequests = Mathf.Max(0, pauseRequests - 1);
        ApplyTimeScale();
    }

    // Usado em troca de cena / volta ao menu.
    public void ResetPause()
    {
        pauseRequests = 0;
        ApplyTimeScale();
    }

    private void ApplyTimeScale()
    {
        Time.timeScale = pauseRequests > 0 ? 0f : 1f;
    }

    // Sem instância, recorre a Time.timeScale direto.
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

    public static void ForceReset()
    {
        if (Instance != null) Instance.ResetPause();
        else Time.timeScale = 1f;
    }
}
