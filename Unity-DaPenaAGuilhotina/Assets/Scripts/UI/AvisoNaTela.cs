using System;
using UnityEngine;

/// <summary>
/// Canal único para mensagens curtas ao jogador (documento, Tarefas Globais: "Sistema de Pop-ups").
/// Quem quiser avisar algo chama AvisoNaTela.Mostrar(...); o ItemPickupNotificationUI da cena exibe
/// na mesma fila do popup de "item recebido". Estático para não depender de referência no Inspector.
/// </summary>
public static class AvisoNaTela
{
    public static event Action<string, Sprite> OnAviso;

    /// <summary>Segundos pedidos para o aviso que está sendo disparado agora (0 = duração padrão do popup). Só vale
    /// durante o OnAviso: quem exibe lê na hora.</summary>
    public static float DuracaoPedida { get; private set; }

    /// <param name="segundos">Tempo na tela para avisos que o jogador precisa ler com calma (0 = padrão do popup).</param>
    public static void Mostrar(string texto, Sprite icone = null, float segundos = 0f)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (OnAviso == null) { Debug.Log($"[Aviso] {texto}"); return; } // sem UI na cena (ex: testes)
        DuracaoPedida = Mathf.Max(0f, segundos);
        try { OnAviso.Invoke(texto, icone); }
        finally { DuracaoPedida = 0f; }
    }
}
