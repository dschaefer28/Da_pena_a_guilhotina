using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Volumes separados de Música e de Efeitos (tudo o que não é música: sons, vozes, ambiente).
///
/// O projeto FMOD (audioJogofmod) não tem buses de Música/Efeitos: todo evento sai direto no Master Bus, e os banks
/// são gerados no FMOD Studio 2.02 de quem cuida do áudio (o 2.03 instalado aqui só abre o projeto migrando, e banks
/// 2.03 não carregam no plugin 2.02 do Unity). Por isso a separação é feita aqui, pelo bank de cada evento: o que
/// está SÓ no bank "Musicas" é música; o resto é efeito (o crowdAmbience também está em Sons, então é efeito).
///
/// A cada frame, logo antes do RuntimeManager do FMOD (ordem 100) mandar os comandos da frame, toda instância viva
/// (PlayOneShot, StudioEventEmitter, instância criada à mão) recebe o volume da sua categoria. Como o ajuste vai no
/// mesmo envio que o start, um som novo já começa no volume certo. Se um dia o projeto FMOD ganhar buses de Música e
/// Efeitos, basta trocar este ajuste por Bus.setVolume.
/// </summary>
[DefaultExecutionOrder(99)]
public class VolumeDoAudio : MonoBehaviour
{
    public const string ChaveMusica = "opt_volume_musica";
    public const string ChaveEfeitos = "opt_volume_efeitos";
    /// <summary>Slider único de antes (mexia no Master Bus): vira o valor inicial dos dois para quem já tinha ajustado.</summary>
    public const string ChaveAntiga = "opt_volume";
    public const string BankDeMusicas = "bank:/Musicas";

    public static float Musica => Ler(ChaveMusica);
    public static float Efeitos => Ler(ChaveEfeitos);

    public static void DefinirMusica(float volume01) => PlayerPrefs.SetFloat(ChaveMusica, Mathf.Clamp01(volume01));
    public static void DefinirEfeitos(float volume01) => PlayerPrefs.SetFloat(ChaveEfeitos, Mathf.Clamp01(volume01));

    private static float Ler(string chave) => Mathf.Clamp01(PlayerPrefs.GetFloat(chave, PlayerPrefs.GetFloat(ChaveAntiga, 1f)));

    /// <summary>
    /// Regra da categoria: música é o evento que está no bank Musicas e em nenhum outro. Recebe, por evento, os
    /// caminhos dos banks onde ele aparece.
    /// </summary>
    public static bool EhMusica(ICollection<string> banksDoEvento) =>
        banksDoEvento.Count == 1 && banksDoEvento.Contains(BankDeMusicas);

    // Instâncias que tocam abaixo do normal (o hover do inventário toca a 50%): o volume final é base x categoria.
    private static readonly Dictionary<IntPtr, float> volumeBase = new Dictionary<IntPtr, float>();

    /// <summary>Use no lugar de EventInstance.setVolume, que seria sobrescrito pelo volume da categoria.</summary>
    public static void DefinirVolumeDaInstancia(FMOD.Studio.EventInstance instancia, float volume01) =>
        volumeBase[instancia.handle] = volume01;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Criar()
    {
        var go = new GameObject(nameof(VolumeDoAudio));
        DontDestroyOnLoad(go);
        go.AddComponent<VolumeDoAudio>();
    }

    // NonSerialized: num recompile durante o Play o Unity preservaria o contador mas não as listas (o struct do FMOD
    // não é serializável), e as listas vazias nunca seriam refeitas. Assim tudo volta junto ao estado inicial.
    [NonSerialized] private readonly List<FMOD.Studio.EventDescription> musicas = new List<FMOD.Studio.EventDescription>();
    [NonSerialized] private readonly List<FMOD.Studio.EventDescription> efeitos = new List<FMOD.Studio.EventDescription>();
    [NonSerialized] private readonly List<IntPtr> paraRemover = new List<IntPtr>();
    [NonSerialized] private int banksClassificados = -1;
    [NonSerialized] private bool listaVencida;

    private void Update()
    {
        if (!AudioSeguro.Disponivel || !RuntimeManager.IsInitialized) return;

        // Refeito quando entra bank novo ou algum evento da lista deixou de valer (bank recarregado, FMOD reiniciado).
        // No celular os banks carregam aos poucos: enquanto algum não terminou, classifica o que já existe e tenta de
        // novo na próxima frame.
        FMOD.Studio.System sistema = RuntimeManager.StudioSystem;
        if (sistema.getBankCount(out int quantos) == FMOD.RESULT.OK && (quantos != banksClassificados || listaVencida))
            banksClassificados = Classificar(sistema, musicas, efeitos) ? quantos : -1;

        listaVencida = !Aplicar(musicas, Musica) | !Aplicar(efeitos, Efeitos);
        LimparVolumesBase();
    }

    /// <summary>Separa os eventos dos banks carregados em música e efeitos. Falso se algum bank ainda está carregando.</summary>
    public static bool Classificar(FMOD.Studio.System sistema, List<FMOD.Studio.EventDescription> musicas,
        List<FMOD.Studio.EventDescription> efeitos)
    {
        musicas.Clear();
        efeitos.Clear();
        if (sistema.getBankList(out FMOD.Studio.Bank[] banks) != FMOD.RESULT.OK) return false;

        bool todosCarregados = true;
        var eventos = new Dictionary<FMOD.GUID, FMOD.Studio.EventDescription>();
        var banksPorEvento = new Dictionary<FMOD.GUID, List<string>>();
        foreach (FMOD.Studio.Bank bank in banks)
        {
            bank.getLoadingState(out FMOD.Studio.LOADING_STATE estado);
            if (estado != FMOD.Studio.LOADING_STATE.LOADED) { todosCarregados = false; continue; }
            bank.getPath(out string caminho);
            if (bank.getEventList(out FMOD.Studio.EventDescription[] lista) != FMOD.RESULT.OK) { todosCarregados = false; continue; }
            foreach (FMOD.Studio.EventDescription ev in lista)
            {
                ev.getID(out FMOD.GUID id);
                eventos[id] = ev;
                if (!banksPorEvento.TryGetValue(id, out List<string> banksDoEvento))
                    banksPorEvento[id] = banksDoEvento = new List<string>();
                banksDoEvento.Add(caminho);
            }
        }

        foreach (var par in eventos)
            (EhMusica(banksPorEvento[par.Key]) ? musicas : efeitos).Add(par.Value);
        return todosCarregados;
    }

    /// <summary>Falso se algum evento da lista não é mais válido.</summary>
    private static bool Aplicar(List<FMOD.Studio.EventDescription> eventos, float volumeDaCategoria)
    {
        bool todosValidos = true;
        foreach (FMOD.Studio.EventDescription ev in eventos)
        {
            if (ev.getInstanceCount(out int vivas) != FMOD.RESULT.OK) { todosValidos = false; continue; }
            if (vivas == 0) continue;
            if (ev.getInstanceList(out FMOD.Studio.EventInstance[] instancias) != FMOD.RESULT.OK) continue;
            foreach (FMOD.Studio.EventInstance instancia in instancias)
            {
                float alvo = volumeDaCategoria * (volumeBase.TryGetValue(instancia.handle, out float b) ? b : 1f);
                if (instancia.getVolume(out float atual) == FMOD.RESULT.OK && Mathf.Abs(atual - alvo) > 0.0001f)
                    instancia.setVolume(alvo);
            }
        }
        return todosValidos;
    }

    private void LimparVolumesBase()
    {
        if (volumeBase.Count == 0) return;
        paraRemover.Clear();
        foreach (IntPtr handle in volumeBase.Keys)
            if (!new FMOD.Studio.EventInstance(handle).isValid()) paraRemover.Add(handle);
        foreach (IntPtr handle in paraRemover) volumeBase.Remove(handle);
    }
}
