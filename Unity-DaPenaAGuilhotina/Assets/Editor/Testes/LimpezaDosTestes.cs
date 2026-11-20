using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ao fim da execução EditMode, destrói os ScriptableObjects do jogo criados em memória — inclusive os clones de
/// Item.Clone (DontSave, sobrevivem até o domain reload e geram avisos de validação ao dar Play).
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

    public static List<ScriptableObject> ObjetosDoJogoEmMemoria()
    {
        var lista = new List<ScriptableObject>();
        System.Reflection.Assembly doJogo = typeof(Item).Assembly;
        foreach (ScriptableObject objeto in Resources.FindObjectsOfTypeAll<ScriptableObject>())
            if (objeto != null && objeto.GetType().Assembly == doJogo && !EditorUtility.IsPersistent(objeto)) lista.Add(objeto);
        return lista;
    }
}
