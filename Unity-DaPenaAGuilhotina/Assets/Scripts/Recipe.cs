using UnityEngine;

[CreateAssetMenu(fileName = "NovaReceita", menuName = "ScriptableObjects/Receita")]
public class Recipe : ScriptableObject
{
    [Header("Ingredientes que serão juntados")]
    public string itemID1; 
    public string itemID2; 

    [Header("Item Final Gerado")]
    public Item resultItem; 
}