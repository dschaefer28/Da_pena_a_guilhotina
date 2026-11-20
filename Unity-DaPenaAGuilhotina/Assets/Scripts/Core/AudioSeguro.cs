using System;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Fachada para o FMOD que engole as exceções do RuntimeManager (sistema não inicializado, banks ausentes,
/// evento renomeado): uma exceção no meio de Interact/DialogueSystem.Next travava o diálogo como "ativo".
/// </summary>
public static class AudioSeguro
{
    private static bool sistemaIndisponivel;
    private static bool jaAvisou;

    public static bool Disponivel => !sistemaIndisponivel;

    // Fora do Play Mode (ferramentas e testes do Editor) o RuntimeManager não existe.
    private static bool SemRuntime => sistemaIndisponivel || !Application.isPlaying;

    public static bool BanksCarregados
    {
        get
        {
            if (SemRuntime) return true;
            try { return RuntimeManager.HaveAllBanksLoaded; }
            catch (Exception e) { Registrar(e); return true; }
        }
    }

    public static void TocarUmaVez(EventReference evento)
    {
        if (SemRuntime || evento.IsNull) return;
        try { RuntimeManager.PlayOneShot(evento); }
        catch (Exception e) { Registrar(e); }
    }

    public static void TocarUmaVez(EventReference evento, Vector3 posicao)
    {
        if (SemRuntime || evento.IsNull) return;
        try { RuntimeManager.PlayOneShot(evento, posicao); }
        catch (Exception e) { Registrar(e); }
    }

    public static bool TentarCriar(EventReference evento, out FMOD.Studio.EventInstance instancia)
    {
        instancia = default;
        if (SemRuntime || evento.IsNull) return false;
        try
        {
            instancia = RuntimeManager.CreateInstance(evento);
            return instancia.isValid();
        }
        catch (Exception e) { Registrar(e); return false; }
    }

    public static void PararELiberar(ref FMOD.Studio.EventInstance instancia)
    {
        try
        {
            if (instancia.isValid())
            {
                instancia.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                instancia.release();
            }
        }
        catch (Exception e) { Registrar(e); }
        instancia = default;
    }

    private static void Registrar(Exception e)
    {
        // Sem sistema, desliga as próximas chamadas para evitar custo e spam de log.
        if (e is SystemNotInitializedException) sistemaIndisponivel = true;
        if (jaAvisou) return;
        jaAvisou = true;
        Debug.LogWarning($"[AudioSeguro] FMOD indisponível ou evento inválido; o jogo continua sem esse som. Detalhe: {e.Message}");
    }
}
