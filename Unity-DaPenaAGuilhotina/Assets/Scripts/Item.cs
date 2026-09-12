using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Items")]
public class Item : ScriptableObject
{
    public string itemID;
    public Sprite itemImg;
    [Min(0)] public int itemAmt;

    [Header("Popup de Coleta (opcional)")]
    [Tooltip("Nome mostrado ao jogador no popup de 'item recebido'. Se deixar vazio, usa o nome do arquivo do ScriptableObject.")]
    public string itemName;

    /// <summary>Nome pronto para exibição em UI (usa itemName se configurado, senão cai no nome do asset).</summary>
    public string NomeExibicao => string.IsNullOrWhiteSpace(itemName) ? name : itemName;

    /// <summary>Verdadeiro se os dois itens representam o mesmo tipo (mesmo itemID não-vazio).</summary>
    public bool Matches(Item other)
    {
        return other != null
            && !string.IsNullOrEmpty(itemID)
            && itemID == other.itemID;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemID))
            Debug.LogWarning($"[Item] '{name}' está sem itemID. O empilhamento e as buscas de pista podem falhar.", this);

        if (itemAmt < 0) itemAmt = 0;
    }
#endif
}

public static class ScriptableObjectExtension
{
    /// <summary>
    /// Creates and returns a clone of any given scriptable object.
    /// </summary>
    public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
    {
        if (scriptableObject == null)
        {
            Debug.LogError($"ScriptableObject was null. Returning default {typeof(T)} object.");
            return (T)ScriptableObject.CreateInstance(typeof(T));
        }

        T instance = UnityEngine.Object.Instantiate(scriptableObject);
        instance.name = scriptableObject.name; // remove (Clone) from name
        instance.hideFlags = HideFlags.DontSave; // clone é só de runtime, nunca deve ser serializado
        return instance;
    }
}