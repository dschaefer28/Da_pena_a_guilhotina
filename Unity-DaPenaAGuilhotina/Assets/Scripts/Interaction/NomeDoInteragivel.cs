using UnityEngine;

/// <summary>Regra única do nome mostrado embaixo do aviso de interação (IInteractable.NomeExibido): o campo
/// "Nome Exibido" do Inspector e, vazio, o nome do GameObject.</summary>
public static class NomeDoInteragivel
{
    public static string Resolver(string nomeExibido, GameObject objeto) =>
        string.IsNullOrWhiteSpace(nomeExibido) ? objeto.name : nomeExibido.Trim();
}
