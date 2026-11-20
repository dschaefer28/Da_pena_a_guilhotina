using System;
using UnityEngine;

/// <summary>
/// Canal único de mensagens curtas ao jogador; o ItemPickupNotificationUI exibe na fila do popup de item.
/// Estático para não depender de referência no Inspector.
/// </summary>
public static class AvisoNaTela
{
    public static event Action<string, Sprite> OnAviso;

    /// <summary>Duração pedida (0 = padrão). Só é válida durante o OnAviso.</summary>
    public static float DuracaoPedida { get; private set; }

    public static void Mostrar(string texto, Sprite icone = null, float segundos = 0f)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (OnAviso == null) { Debug.Log($"[Aviso] {texto}"); return; } // sem UI na cena (ex: testes)
        DuracaoPedida = Mathf.Max(0f, segundos);
        try { OnAviso.Invoke(texto, icone); }
        finally { DuracaoPedida = 0f; }
    }
}
