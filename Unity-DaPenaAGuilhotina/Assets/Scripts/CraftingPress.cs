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

    // Trava de reentrada: protege contra uma impressão aninhada (ex.: um ouvinte do OnPanfletoGerado que chamasse
    // CombineItems). Cliques repetidos em sequência são barrados pela regra de domínio (GameManager.PodePublicarCaso).
    private bool imprimindo;

    public void CombineItems()
    {
        if (imprimindo) return;
        imprimindo = true;
        try { Combinar(); }
        finally { imprimindo = false; }
    }

    private void Combinar()
    {
        if (!SlotsConfigurados()) return;

        if (recipeDictionary == null) Start(); // caso CombineItems seja chamado antes do Start (ordem de execução)

        if (slotInput1.item == null || slotInput2.item == null || slotInput1.item.itemAmt <= 0 || slotInput2.item.itemAmt <= 0)
        {
            Debug.Log("Faltam ingredientes nos slots!");
            return;
        }

        if (Inventario == null || GameManager.Instance == null)
        {
            Debug.LogError("[CraftingPress] GameManager/InventoryManager indisponível.", this);
            return;
        }

        if (TentarPanfletoDeCaso()) return;

        string attemptKey = $"{slotInput1.item.itemID}_{slotInput2.item.itemID}";

        if (recipeDictionary.TryGetValue(attemptKey, out Recipe validRecipe) && validRecipe.resultItem != null)
        {
            // Receita exata (ex: o panfleto do tutorial): conclui o caso em andamento, uma única vez.
            GameManager gm = GameManager.Instance;
            CaseData caso = gm.casoEscolhido;
            if (PistaDeOutroCaso(slotInput1.item, caso) || PistaDeOutroCaso(slotInput2.item, caso))
            {
                AvisoNaTela.Mostrar("Essas pistas não são do caso que estou investigando.");
                return;
            }
            // Receitas exatas só concluem casos sem receita de caso (o tutorial). Um caso com ReceitaDeCaso é
            // publicado pela regra Fato x Boato: itens sem caso (ex.: os do tutorial) não podem concluí-lo por aqui.
            if (caso != null && caso.receitaDoPanfleto != null)
            {
                AvisoNaTela.Mostrar("Essa combinação não forma um panfleto.");
                return;
            }
            if (!gm.PodePublicarCaso(caso, out string motivo))
            {
                AvisoNaTela.Mostrar(motivo);
                return;
            }
            if (Imprimir(validRecipe.resultItem, validRecipe.publicOpinionImpact, validRecipe.stateOpinionImpact, validRecipe.moneyReward, out _))
                gm.ConcluirCaso(caso);
        }
        else
        {
            AvisoNaTela.Mostrar("Essa combinação não forma um panfleto.");
        }
    }

    private static bool PistaDeOutroCaso(Item item, CaseData caso) => item.caso != null && item.caso != caso;

    /// <summary>
    /// Regra Fato x Boato (ReceitaDeCaso): duas pistas diferentes do caso atual sempre imprimem, e a
    /// confiabilidade delas escolhe a versão. Retorna true se tratou a mistura (imprimindo ou explicando o
    /// porquê de não imprimir); false devolve o par para as receitas exatas (Recipe).
    /// </summary>
    private bool TentarPanfletoDeCaso()
    {
        Item a = slotInput1.item;
        Item b = slotInput2.item;
        if (a.EhSuporte || b.EhSuporte)
        {
            AvisoNaTela.Mostrar("Documentos de apoio não vão nas entradas: selecione-os no campo Suporte.");
            return true;
        }
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
        // Só o caso em andamento, e uma única vez: validado ANTES de consumir as pistas.
        if (!GameManager.Instance.PodePublicarCaso(a.caso, out string motivo))
        {
            AvisoNaTela.Mostrar(motivo);
            return true;
        }

        ReceitaDeCaso receita = a.caso.receitaDoPanfleto;
        if (receita == null)
        {
            Debug.LogWarning($"[CraftingPress] O caso '{a.caso.name}' não tem Receita Do Panfleto; tentando as receitas exatas.", a.caso);
            return false;
        }

        // Cálculo único (versão → apoio → [linha editorial no Prompt 5]); nada nos assets é alterado.
        ResultadoDoPanfleto resultado = CalculadoraDePanfleto.Calcular(receita, a, b, SuportesSelecionadosNoInventario());
        if (resultado.panfleto == null)
        {
            Debug.LogError($"[CraftingPress] '{receita.name}' não tem panfleto para a versão {resultado.nivel}.", receita);
            return true;
        }

        if (Imprimir(resultado.panfleto, resultado.povo, resultado.estado, resultado.ouro, out Vector3Int aplicado))
        {
            GameManager.Instance.RegistrarPanfletoDeCaso(a.caso, resultado, aplicado);
            GameManager.Instance.ConcluirCaso(a.caso);
            LimparSuportes(); // os documentos continuam no inventário: só a seleção é desfeita
        }
        return true;
    }

    // ===== Documentos de apoio (Prompt 3): selecionados à parte, nunca consumidos =====

    private readonly List<string> idsDeSuporte = new List<string>();
    private CaseData casoDaSelecao;

    /// <summary>Disparado quando a seleção de apoio muda (a UI de suporte e a prévia escutam).</summary>
    public event Action OnSuportesAlterados;

    public bool SuporteSelecionado(Item suporte) => suporte != null && idsDeSuporte.Contains(suporte.itemID);

    /// <summary>Marca/desmarca um documento de apoio do caso em andamento. Falso se não for elegível.</summary>
    public bool AlternarSuporte(Item suporte)
    {
        GameManager gm = GameManager.Instance;
        DescartarSelecaoDeOutroCaso();
        if (gm == null || suporte == null || !suporte.EhSuporte || suporte.caso != gm.casoEscolhido) return false;

        if (!idsDeSuporte.Remove(suporte.itemID)) idsDeSuporte.Add(suporte.itemID);
        casoDaSelecao = gm.casoEscolhido;
        OnSuportesAlterados?.Invoke();
        return true;
    }

    public void LimparSuportes()
    {
        if (idsDeSuporte.Count == 0) return;
        idsDeSuporte.Clear();
        OnSuportesAlterados?.Invoke();
    }

    /// <summary>Seleção resolvida contra o inventário atual: só documentos que o jogador ainda tem, do caso em andamento.</summary>
    public List<Item> SuportesSelecionadosNoInventario()
    {
        DescartarSelecaoDeOutroCaso();
        var itens = new List<Item>();
        InventoryManager inventario = Inventario;
        if (inventario == null) return itens;
        foreach (string id in idsDeSuporte)
        {
            Item item = inventario.ItemNaGrade(id);
            if (item != null) itens.Add(item);
        }
        return CalculadoraDePanfleto.SuportesValidos(GameManager.Instance != null ? GameManager.Instance.casoEscolhido : null, itens);
    }

    // Uma seleção feita em outro caso nunca atravessa para o caso atual.
    private void DescartarSelecaoDeOutroCaso()
    {
        GameManager gm = GameManager.Instance;
        if (idsDeSuporte.Count > 0 && (gm == null || casoDaSelecao != gm.casoEscolhido)) LimparSuportes();
    }

    private bool Imprimir(Item panfleto, int povo, int estado, int ouro, out Vector3Int aplicado)
    {
        aplicado = Vector3Int.zero;
        if (panfleto == null) return false;
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
        aplicado = GameManager.Instance.AplicarImpactoPanfleto(povo, estado, ouro);

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