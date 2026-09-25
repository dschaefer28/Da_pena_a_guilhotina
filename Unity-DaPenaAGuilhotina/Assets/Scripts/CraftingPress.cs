using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingPress : MonoBehaviour
{
    [Header("Slots da Prensa")]
    public UISlotHandler slotInput1;
    public UISlotHandler slotInput2;
    public UISlotHandler slotOutput;

    [Header("Configurações")]
    public List<Recipe> recipes;

    private Dictionary<string, Recipe> recipeDictionary;

    // NOVO: Evento Observer disparado toda vez que a prensa gera um panfleto novo (usado pelo tutorial).
    public event Action OnPanfletoGerado;

    /// <summary>Último panfleto impresso nesta prensa (o tutorial usa para saber quando o jogador o guardou).</summary>
    public Item UltimoPanfleto { get; private set; }

    private void Start()
    {
        recipeDictionary = new Dictionary<string, Recipe>();

        if (recipes == null) return;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            // Ignora receitas mal configuradas em vez de estourar NullReference ao montar o dicionário
            if (!recipe.IsValid())
            {
                Debug.LogWarning($"[CraftingPress] Receita '{recipe.name}' ignorada: ingredientes ou resultado ausentes.", this);
                continue;
            }

            string key1 = $"{recipe.ingrediente1.itemID}_{recipe.ingrediente2.itemID}";
            string key2 = $"{recipe.ingrediente2.itemID}_{recipe.ingrediente1.itemID}";

            recipeDictionary.TryAdd(key1, recipe);
            recipeDictionary.TryAdd(key2, recipe);
        }
    }

    private bool SlotsConfigurados()
    {
        if (slotInput1 == null || slotInput2 == null || slotOutput == null)
        {
            Debug.LogError("[CraftingPress] Slots de entrada/saída não atribuídos no Inspector.", this);
            return false;
        }
        return true;
    }

    private InventoryManager Inventario =>
        GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;

    public void CombineItems()
    {
        if (!SlotsConfigurados()) return;

        if (recipeDictionary == null) Start(); // caso CombineItems seja chamado antes do Start (ordem de execução)

        if (slotInput1.item == null || slotInput2.item == null)
        {
            Debug.Log("Faltam ingredientes nos slots!");
            return;
        }

        if (Inventario == null)
        {
            Debug.LogError("[CraftingPress] InventoryManager indisponível no GameManager.", this);
            return;
        }

        if (TentarPanfletoDeCaso()) return;

        string attemptKey = $"{slotInput1.item.itemID}_{slotInput2.item.itemID}";

        if (recipeDictionary.TryGetValue(attemptKey, out Recipe validRecipe) && validRecipe.resultItem != null)
        {
            // Receita exata (ex: o panfleto do tutorial): conclui o caso em andamento.
            if (Imprimir(validRecipe.resultItem, validRecipe.publicOpinionImpact, validRecipe.stateOpinionImpact, validRecipe.moneyReward))
                GameManager.Instance.ConcluirCaso(GameManager.Instance.casoEscolhido);
        }
        else
        {
            AvisoNaTela.Mostrar("Essa combinação não forma um panfleto.");
        }
    }

    /// <summary>
    /// Regra Fato x Boato (ReceitaDeCaso): duas pistas diferentes do caso atual sempre imprimem, e a
    /// confiabilidade delas escolhe a versão. Retorna true se tratou a mistura (imprimindo ou explicando o
    /// porquê de não imprimir); false devolve o par para as receitas exatas (Recipe).
    /// </summary>
    private bool TentarPanfletoDeCaso()
    {
        Item a = slotInput1.item;
        Item b = slotInput2.item;
        if (!a.EhPista || !b.EhPista) return false;

        if (a.itemID == b.itemID)
        {
            AvisoNaTela.Mostrar("Preciso de duas pistas diferentes.");
            return true;
        }
        if (a.caso != b.caso)
        {
            AvisoNaTela.Mostrar("Essas pistas são de casos diferentes.");
            return true;
        }
        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual != null && a.caso != casoAtual)
        {
            AvisoNaTela.Mostrar("Essas pistas não são do caso que estou investigando.");
            return true;
        }

        ReceitaDeCaso receita = a.caso.receitaDoPanfleto;
        if (receita == null)
        {
            Debug.LogWarning($"[CraftingPress] O caso '{a.caso.name}' não tem Receita Do Panfleto; tentando as receitas exatas.", a.caso);
            return false;
        }

        NivelDoPanfleto nivel = ReceitaDeCaso.Classificar(a, b);
        ReceitaDeCaso.Versao versao = receita.VersaoDe(nivel);
        Item panfleto = receita.PanfletoDe(versao);
        if (panfleto == null)
        {
            Debug.LogError($"[CraftingPress] '{receita.name}' não tem panfleto para a versão {nivel}.", receita);
            return true;
        }

        if (Imprimir(panfleto, versao.povo, versao.estado, versao.ouro))
        {
            GameManager.Instance.RegistrarPanfletoDeCaso(a.caso, nivel, versao);
            GameManager.Instance.ConcluirCaso(a.caso);
        }
        return true;
    }

    private bool Imprimir(Item panfleto, int povo, int estado, int ouro)
    {
        if (slotOutput.item != null && slotOutput.item.itemID != panfleto.itemID)
        {
            AvisoNaTela.Mostrar("Tire o panfleto pronto da prensa primeiro.");
            return false;
        }

        ConsumeItem(slotInput1);
        ConsumeItem(slotInput2);
        ProduceItem(panfleto);
        UltimoPanfleto = panfleto;

        // Dispara a consequência matemática da receita (GDD)
        GameManager.Instance.AplicarImpactoPanfleto(povo, estado, ouro);

        OnPanfletoGerado?.Invoke();

        // Avisa o jogador (mesmo popup de "item recebido" já usado no resto do jogo), já que
        // ProduceItem coloca o panfleto direto no slot de saída da prensa, sem passar por AddItem.
        Inventario.NotificarItemRecebido(panfleto);
        return true;
    }

    private void ConsumeItem(UISlotHandler slot)
    {
        if (slot == null || slot.item == null) return;

        slot.item.itemAmt--;
        if (slot.item.itemAmt <= 0)
        {
            Inventario.ClearItemSlot(slot);
        }
        else if (slot.itemCount != null)
        {
            slot.itemCount.text = slot.item.itemAmt.ToString();
        }
    }

    private void ProduceItem(Item resultPrefab)
    {
        if (resultPrefab == null) return;

        if (slotOutput.item == null)
        {
            Item newItem = resultPrefab.Clone();
            newItem.itemAmt = 1;
            Inventario.PlaceInInventory(slotOutput, newItem);
        }
        else
        {
            slotOutput.item.itemAmt++;
            if (slotOutput.itemCount != null)
                slotOutput.itemCount.text = slotOutput.item.itemAmt.ToString();
        }
    }
}