using System.Collections.Generic;
using UnityEngine;

// Criamos uma estrutura que liga a Carta da Mesa (Caso) à Pista que será encontrada
[System.Serializable]
public struct LootDeCaso
{
    public CaseData caso;
    public Item itemParaDar;
}

public class LootInteractable : MonoBehaviour, IInteractable
{
    [Header("Pistas da Estante (1 por Caso)")]
    [Tooltip("Adicione os casos e a pista correspondente a cada um.")]
    public List<LootDeCaso> pistasPossiveis;
    public int amountToGive = 1;

    [Header("Aleatoriedade de Posição (Opcional)")]
    public bool aleatorizarPosicao = false;
    [Tooltip("Crie GameObjects vazios na cena e arraste-os aqui para sortear o local de nascimento deste objeto.")]
    public List<Transform> pontosDeSpawn;

    [Header("Configurações")]
    public bool destroyAfterLoot = false;
    private bool alreadyLooted = false;

    private void Start()
    {
        // Se ativado, a estante (ou objeto) se teletransporta para um local aleatório da sala no início da fase
        if (aleatorizarPosicao && pontosDeSpawn != null && pontosDeSpawn.Count > 0)
        {
            int index = Random.Range(0, pontosDeSpawn.Count);
            transform.position = pontosDeSpawn[index].position;
        }
    }

    public void Interact()
    {
        if (alreadyLooted) return;

        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual == null)
        {
            Debug.Log("Você não está investigando nenhum caso no momento.");
            return;
        }

        // Procura na lista qual é o item correto configurado para o caso atual
        Item itemCorreto = null;
        foreach (var loot in pistasPossiveis)
        {
            if (loot.caso == casoAtual)
            {
                itemCorreto = loot.itemParaDar;
                break;
            }
        }

        // Se o caso atual não estiver na lista deste móvel, o móvel não entrega nada.
        if (itemCorreto == null)
        {
            Debug.Log("Não há pistas úteis para o seu caso atual aqui.");
            return;
        }

        InventoryManager inventory = GameManager.Instance.inventoryManager;
        if (inventory != null)
        {
            Item itemClone = itemCorreto.Clone();
            itemClone.itemAmt = amountToGive;

            bool foiGuardado = inventory.AddItem(itemClone);

            if (foiGuardado)
            {
                Debug.Log($"Você encontrou: {itemCorreto.name}!");
                
                if (destroyAfterLoot) Destroy(gameObject);
                else alreadyLooted = true;
            }
        }
    }
}