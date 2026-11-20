using UnityEngine;

/// <summary>Regra única de IInteractable.NomeExibido: o campo do Inspector ou, vazio, o nome do GameObject.</summary>
public static class NomeDoInteragivel
{
    public static string Resolver(string nomeExibido, GameObject objeto) =>
        string.IsNullOrWhiteSpace(nomeExibido) ? objeto.name : nomeExibido.Trim();
}
