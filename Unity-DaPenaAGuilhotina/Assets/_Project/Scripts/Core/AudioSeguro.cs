using System;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Fachada para o FMOD que nunca deixa um problema de áudio derrubar o gameplay.
/// O RuntimeManager lança exceção quando o sistema não inicializa (ERR_MEMORY, banks ausentes no
/// build, evento renomeado...). Antes, essa exceção estourava no meio de NPCMovement.Interact /
/// DialogueSystem.Next e deixava o diálogo "ativo" para sempre: toda interação seguinte caía em
/// AdvanceDialogue e nada mais respondia. Aqui o som simplesmente não toca e o jogo segue.
/// </summary>
public static class AudioSeguro
{
    private static bool sistemaIndisponivel;
    private static bool jaAvisou;

    /// <summary>Verdadeiro enquanto o FMOD respondeu normalmente nesta sessão.</summary>
    public static bool Disponivel => !sistemaIndisponivel;

    // Fora do Play Mode (ferramentas e testes do Editor) o RuntimeManager não existe: acessá-lo só gera erro no
    // console. Num build o jogo está sempre em Play, então nada muda para o jogador.
    private static bool SemRuntime => sistemaIndisponivel || !Application.isPlaying;

    public static bool BanksCarregados
    {
        get
        {
            if (SemRuntime) return true; // não há o que esperar
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

    /// <summary>Cria a instância do evento. Retorna falso (sem exceção) se o FMOD ou o evento não existirem.</summary>
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

    public static void DefinirVolumeDoBus(string caminhoDoBus, float volume01)
    {
        if (SemRuntime) return;
        try { RuntimeManager.GetBus(caminhoDoBus).setVolume(volume01); }
        catch (Exception e) { Registrar(e); }
    }

    private static void Registrar(Exception e)
    {
        // Sem sistema, não adianta tentar de novo a cada chamada (só geraria custo e spam de log).
        if (e is SystemNotInitializedException) sistemaIndisponivel = true;
        if (jaAvisou) return;
        jaAvisou = true;
        Debug.LogWarning($"[AudioSeguro] FMOD indisponível ou evento inválido; o jogo continua sem esse som. Detalhe: {e.Message}");
    }
}
