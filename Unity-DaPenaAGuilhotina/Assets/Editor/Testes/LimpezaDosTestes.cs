using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Relatório de testes de 28/09, item 22. Ao fim de cada execução dos testes EditMode, destrói os ScriptableObjects do jogo
/// (itens, casos, receitas, ofertas...) criados na memória durante a execução: os que os testes criam e, principalmente,
/// os clones que o código testado faz (Item.Clone marca o clone como DontSave, e ele sobrevive até à recarga do domínio).
/// Antes, ~25 desses objetos continuavam vivos e repetiam avisos de validação no console ao dar Play.
/// Só apaga o que não existia antes da execução e nunca toca em assets salvos.
/// </summary>
[SetUpFixture]
public class LimpezaDosTestes
{
    private HashSet<int> jaExistiam;

    [OneTimeSetUp]
    public void AnotarObjetosExistentes()
    {
        jaExistiam = new HashSet<int>();
        foreach (ScriptableObject objeto in ObjetosDoJogoEmMemoria()) jaExistiam.Add(objeto.GetInstanceID());
    }

    [OneTimeTearDown]
    public void DestruirObjetosCriadosNosTestes()
    {
        foreach (ScriptableObject objeto in ObjetosDoJogoEmMemoria())
            if (jaExistiam == null || !jaExistiam.Contains(objeto.GetInstanceID())) Object.DestroyImmediate(objeto);
    }

    /// <summary>ScriptableObjects de tipos do jogo que estão só na memória (não são assets salvos).</summary>
    public static List<ScriptableObject> ObjetosDoJogoEmMemoria()
    {
        var lista = new List<ScriptableObject>();
        System.Reflection.Assembly doJogo = typeof(Item).Assembly;
        foreach (ScriptableObject objeto in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            if (objeto != null && objeto.GetType().Assembly == doJogo && !EditorUtility.IsPersistent(objeto)) lista.Add(objeto);
        return lista;
    }
}
