using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Passo sincronizado com a animação: o evento FMOD "fs madeira" é uma gravação em loop (~12,6 s) que não pode ser
/// refeita aqui, então cada Animation Event "Passo" cria uma instância, salta para logo antes de uma pisada e a
/// encerra no fim do trecho dela.
/// Os eventos ficam 14 quadros antes do contato (14 e 40) porque o streaming do FMOD leva ~200 ms para começar;
/// a 1,2x isso dá 194 ms. Mudando a velocidade do Walk, a antecipação muda junto (14/60 s ÷ velocidade).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PassosDoJogador : MonoBehaviour
{
    [SerializeField] private EventReference eventoDePassos;
    [Tooltip("Silêncio que entra antes do golpe (s). Pequeno, para o golpe cair no quadro em que o pé toca o chão.")]
    [SerializeField, Range(0f, 0.05f)] private float antesDoImpacto = 0.015f;
    [Tooltip("Volume geral dos passos, por cima do nivelamento de cada pisada e do volume de Efeitos das opções.")]
    [SerializeField, Range(0f, 2f)] private float volume = 1f;

    // Pisadas de "fs madeira" (Guilhotina_02.wav) em ms: início do golpe (medido por amostra) e fim do trecho.
    // Refazer se o evento for reeditado: golpe marcado tarde faz o salto pular o ataque e só a cauda toca.
    // ganhoDb nivela o pico de cada pisada a ~-22 dBFS (na gravação variam 14 dB), limitado a -4/+9 dB.
    private static readonly (int impacto, int fim, float ganhoDb)[] Pisadas =
    {
        (62, 400, 3.4f), (731, 1000, -3.5f), (1316, 1600, -1.0f), (1933, 2200, 8.5f), (2656, 3000, 4.5f),
        (3292, 3600, 9f), (4623, 4800, 7.6f), (5159, 5503, 7.7f), (6023, 6400, -3.9f), (6927, 7200, 6.8f),
        (7702, 8000, 4.8f), (8442, 8600, 3.7f), (8949, 9200, 4.0f), (9587, 9800, 0.5f), (10219, 10800, 6.5f),
        (11138, 11400, -3.7f), (11791, 12200, 1.2f),
    };

    // NonSerialized: num recompile durante o Play o Unity guardaria o índice mas não a lista (struct do FMOD).
    [NonSerialized] private readonly List<(FMOD.Studio.EventInstance instancia, int fim)> tocando =
        new List<(FMOD.Studio.EventInstance, int)>();
    [NonSerialized] private int ultimaPisada = -1;

    /// <summary>Animation Event do clipe walk.</summary>
    public void Passo()
    {
        if (!AudioSeguro.TentarCriar(eventoDePassos, out FMOD.Studio.EventInstance instancia)) return;

        // Sorteia sem repetir a anterior.
        int i = UnityEngine.Random.Range(0, ultimaPisada < 0 ? Pisadas.Length : Pisadas.Length - 1);
        if (ultimaPisada >= 0 && i >= ultimaPisada) i++;
        ultimaPisada = i;

        // Via VolumeDoAudio (e não setVolume), senão o volume de Efeitos sobrescreveria o ganho a cada frame.
        VolumeDoAudio.DefinirVolumeDaInstancia(instancia, volume * Mathf.Pow(10f, Pisadas[i].ganhoDb / 20f));
        instancia.setTimelinePosition(Mathf.Max(0, Pisadas[i].impacto - Mathf.RoundToInt(antesDoImpacto * 1000f)));
        instancia.start();
        tocando.Add((instancia, Pisadas[i].fim));
    }

    private void Update()
    {
        for (int k = tocando.Count - 1; k >= 0; k--)
        {
            FMOD.Studio.EventInstance instancia = tocando[k].instancia;
            if (instancia.isValid())
            {
                bool terminouOTrecho = instancia.getTimelinePosition(out int posicao) == FMOD.RESULT.OK && posicao >= tocando[k].fim;
                bool parou = instancia.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE estado) == FMOD.RESULT.OK &&
                             estado == FMOD.Studio.PLAYBACK_STATE.STOPPED;
                if (!terminouOTrecho && !parou) continue;

                // O fim do trecho é silêncio: cortar ali não estala.
                instancia.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instancia.release();
            }
            tocando.RemoveAt(k);
        }
    }

    private void OnDisable()
    {
        for (int k = 0; k < tocando.Count; k++)
        {
            FMOD.Studio.EventInstance instancia = tocando[k].instancia;
            AudioSeguro.PararELiberar(ref instancia);
        }
        tocando.Clear();
    }
}
