using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Som de passo sincronizado com a animação de andar: cada pé que toca o chão no clipe walk dispara uma pisada.
///
/// O evento FMOD "fs madeira" é uma gravação de ~12,6 s em loop, com pisadas espaçadas de 0,6 a 0,8 s. Antes ele
/// tocava em loop enquanto o jogador andava, num ritmo que nunca batia com o dos pés (a animação dá um passo a cada
/// ~0,38 s). Os banks só podem ser refeitos no FMOD 2.02 de quem cuida do áudio, então o evento ficou como está:
/// cada Animation Event "Passo" cria uma instância, pula a timeline para logo antes do golpe de uma pisada e a
/// encerra no fim do trecho daquela pisada, antes de a gravação chegar na próxima.
///
/// Os eventos "Passo" ficam 14 quadros ANTES dos quadros de contato (0 e 26; os pés tocam o chão no 14 e no 40):
/// o evento é streaming e o FMOD leva ~200 ms para começar a tocá-lo (streamingscheduledelay padrão, medido de
/// 191 a 237 ms com os banks reais). A 1,2x, 14 quadros são 194 ms, então o golpe cai no quadro do contato. Se a
/// velocidade do estado Walk mudar muito, a antecipação em segundos muda junto (14/60 s ÷ velocidade).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PassosDoJogador : MonoBehaviour
{
    [SerializeField] private EventReference eventoDePassos;
    [Tooltip("Silêncio que entra antes do golpe (s). Pequeno, para o golpe cair no quadro em que o pé toca o chão.")]
    [SerializeField, Range(0f, 0.05f)] private float antesDoImpacto = 0.015f;
    [Tooltip("Volume geral dos passos, por cima do nivelamento de cada pisada e do volume de Efeitos das opções.")]
    [SerializeField, Range(0f, 2f)] private float volume = 1f;

    // Pisadas do evento "fs madeira" (audioJogofmod, Guilhotina_02.wav), em ms na timeline do evento: o início do golpe
    // principal (medido no .wav com posição exata por amostra; antes dele há silêncio) e o fim do trecho em que ele
    // está. Ficaram de fora uma pisada quase inaudível (3,8 s) e o resto de som no fim do loop. Se o evento for
    // reeditado no FMOD Studio, esta tabela precisa ser refeita: um golpe marcado alguns ms tarde faz o salto da
    // timeline passar por cima dele e só a cauda da pisada toca (soa fraca).
    // ganhoDb nivela as pisadas (na gravação elas variam 14 dB e as fracas sumiam no sorteio): leva o pico de cada uma
    // a ~-22 dBFS com Efeitos em 100%, limitado a -4/+9 dB.
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

    /// <summary>Chamado pelos Animation Events do clipe walk, ~200 ms antes de um pé tocar o chão (ver acima).</summary>
    public void Passo()
    {
        if (!AudioSeguro.TentarCriar(eventoDePassos, out FMOD.Studio.EventInstance instancia)) return;

        // Sorteia sem repetir a anterior, para não soar como o mesmo pé batendo duas vezes.
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

                // O fim do trecho é silêncio (o intervalo até a próxima pisada da gravação): cortar ali não estala.
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
