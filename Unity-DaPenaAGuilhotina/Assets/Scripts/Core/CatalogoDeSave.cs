using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lista de todos os Items e Casos do projeto, para o SistemaDeSave transformar o ID gravado no arquivo de
/// volta no ScriptableObject. Fica em Resources/CatalogoDeSave e é preenchido sozinho (CatalogoDeSaveEditor)
/// ao dar Play e ao gerar o build — não precisa editar à mão.
/// </summary>
public class CatalogoDeSave : ScriptableObject
{
    public const string CaminhoNoResources = "CatalogoDeSave";

    public List<Item> itens = new List<Item>();
    public List<CaseData> casos = new List<CaseData>();

    private static CatalogoDeSave instancia;

    public static CatalogoDeSave Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = Resources.Load<CatalogoDeSave>(CaminhoNoResources);
                if (instancia == null)
                    Debug.LogError("[CatalogoDeSave] Resources/CatalogoDeSave não existe. Use Ferramentas > Save > Atualizar catálogo.");
            }
            return instancia;
        }
    }

    public Item BuscarItem(string itemID) => itens.Find(i => i != null && i.itemID == itemID);

    public CaseData BuscarCaso(string nome) => casos.Find(c => c != null && c.name == nome);
}
