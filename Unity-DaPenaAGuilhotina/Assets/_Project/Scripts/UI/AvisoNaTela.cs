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

    public static void Mostrar(string texto, Sprite icone = null)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (OnAviso == null) Debug.Log($"[Aviso] {texto}"); // sem UI na cena (ex: testes)
        else OnAviso.Invoke(texto, icone);
    }
}
