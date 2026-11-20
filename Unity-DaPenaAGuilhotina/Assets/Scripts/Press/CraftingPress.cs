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

    public event Action OnPanfletoGerado;

    /// <summary>O tutorial usa para saber quando o jogador guardou o panfleto.</summary>
    public Item UltimoPanfleto { get; private set; }

    private void Start()
    {
        recipeDictionary = new Dictionary<string, Recipe>();

        if (recipes == null) return;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

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

    // Trava contra impressão aninhada (ouvinte de OnPanfletoGerado chamando CombineItems). Cliques repetidos são
    // barrados por GameManager.PodePublicarCaso.
    private bool imprimindo;

    /// <summary>
    /// Par validado, nada consumido, falta a linha editorial. A UI responde com ImprimirComLinhaEditorial; cancelar é
    /// não responder. Só recebe a receita: a escolha não pode depender da verdade das pistas.
    /// </summary>
    public event Action<ReceitaDeCaso> OnLinhaEditorialPedida;

    /// <summary>
    /// Disparado após aplicar, registrar e concluir o caso. Vector3Int = variação real nas barras (Povo, Estado, Ouro),
    /// já com o limite 0–100.
    /// </summary>
    public event Action<ReceitaDeCaso, ResultadoDoPanfleto, Vector3Int> OnPanfletoDeCasoPublicado;

    /// <summary>onClick do botão Misturar no UI_Inventory.prefab.</summary>
    public void CombineItems() => Tentar(LinhaEditorial.Nenhuma);

    /// <summary>Revalida tudo a partir do estado atual dos slots.</summary>
    public void ImprimirComLinhaEditorial(LinhaEditorial linha) => Tentar(linha);

    private void Tentar(LinhaEditorial linha)
    {
        if (imprimindo) return;
        imprimindo = true;
        try { Combinar(linha); }
        finally { imprimindo = false; }
    }

    private void Combinar(LinhaEditorial linha)
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

        if (TentarPanfletoDeCaso(linha)) return;

        string attemptKey = $"{slotInput1.item.itemID}_{slotInput2.item.itemID}";

        if (recipeDictionary.TryGetValue(attemptKey, out Recipe validRecipe) && validRecipe.resultItem != null)
        {
            GameManager gm = GameManager.Instance;
            CaseData caso = gm.casoEscolhido;
            if (PistaDeOutroCaso(slotInput1.item, caso) || PistaDeOutroCaso(slotInput2.item, caso))
            {
                AvisoNaTela.Mostrar("Essas pistas não são do caso que estou investigando.");
                return;
            }
            // Receitas exatas só concluem casos sem ReceitaDeCaso (o tutorial); os demais seguem a regra Fato x Boato.
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
    /// Regra Fato x Boato: duas pistas diferentes do caso atual sempre imprimem; a confiabilidade escolhe a versão.
    /// True se tratou a mistura (imprimindo ou avisando); false devolve o par para as receitas exatas.
    /// </summary>
    private bool TentarPanfletoDeCaso(LinhaEditorial linha)
    {
        Item a = slotInput1.item;
        Item b = slotInput2.item;
        if (a.EhSuporte || b.EhSuporte)
        {
            AvisoNaTela.Mostrar("Documentos de apoio não vão nas entradas: selecione-os no campo Suporte.");
            return true;
        }
        if (a.EhLeitura || b.EhLeitura)
        {
            AvisoNaTela.Mostrar("A carta do cliente é só para leitura: ela não vai para a prensa.");
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
        // Validado antes de consumir as pistas.
        if (!GameManager.Instance.PodePublicarCaso(a.caso, out string motivo))
        {
            AvisoNaTela.Mostrar(motivo);
            return true;
        }
        // Par contraditório precisa estar decidido no quadro; a recusa nunca depende da verdade das pistas.
        Item semEscolha = Deducao.AguardandoEscolha(GameManager.Instance, a) ? a : Deducao.AguardandoEscolha(GameManager.Instance, b) ? b : null;
        if (semEscolha != null)
        {
            AvisoNaTela.Mostrar($"\"{semEscolha.NomeExibicao}\" tem uma afirmação contrária. Escolha no Quadro de pistas em qual das duas você acredita antes de imprimir.");
            return true;
        }

        ReceitaDeCaso receita = a.caso.receitaDoPanfleto;
        if (receita == null)
        {
            Debug.LogWarning($"[CraftingPress] O caso '{a.caso.name}' não tem Receita Do Panfleto; tentando as receitas exatas.", a.caso);
            return false;
        }

        if (receita.ExigeLinhaEditorial && !LinhasEditoriais.Escolhivel(linha))
        {
            // Confere a saída antes de pedir o tom, sem olhar a versão: qualquer item lá recusa (o panfleto deste caso
            // nunca está lá, pois cada caso é publicado uma vez).
            if (slotOutput.item != null)
            {
                AvisoNaTela.Mostrar("Tire o panfleto pronto da prensa primeiro.");
                return true;
            }
            if (OnLinhaEditorialPedida != null) OnLinhaEditorialPedida(receita);
            else AvisoNaTela.Mostrar("Escolha a linha editorial do panfleto antes de imprimir.");
            return true;
        }

        ResultadoDoPanfleto resultado = CalculadoraDePanfleto.Calcular(receita, a, b, SuportesSelecionadosNoInventario(), linha);
        if (resultado == null) return true; // defensivo: a linha já foi conferida acima
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
            OnPanfletoDeCasoPublicado?.Invoke(receita, resultado, aplicado);
        }
        return true;
    }

    // ===== Documentos de apoio: selecionados à parte, nunca consumidos =====

    private readonly List<string> idsDeSuporte = new List<string>();
    private CaseData casoDaSelecao;

    public event Action OnSuportesAlterados;

    public bool SuporteSelecionado(Item suporte) => suporte != null && idsDeSuporte.Contains(suporte.itemID);

    /// <summary>Falso se o documento não for do caso em andamento.</summary>
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

    /// <summary>Só documentos que o jogador ainda tem, do caso em andamento.</summary>
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
        if (SaidaOcupadaPor(panfleto)) return false;

        ConsumeItem(slotInput1);
        ConsumeItem(slotInput2);
        bool foiParaOInventario = ProduceItem(panfleto);
        UltimoPanfleto = panfleto;

        aplicado = GameManager.Instance.AplicarImpactoPanfleto(povo, estado, ouro);

        OnPanfletoGerado?.Invoke();

        // AddItem já mostra o popup; só o que ficou na saída (inventário cheio) precisa do aviso manual.
        if (!foiParaOInventario) Inventario.NotificarItemRecebido(panfleto);
        return true;
    }

    // A saída guarda um panfleto diferente do que sairia: avisa e recusa (nada é consumido).
    private bool SaidaOcupadaPor(Item panfleto)
    {
        if (panfleto == null || slotOutput.item == null || slotOutput.item.itemID == panfleto.itemID) return false;
        AvisoNaTela.Mostrar("Tire o panfleto pronto da prensa primeiro.");
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

    /// <summary>Vai direto para o inventário (senão ficava esquecido na prensa); só com ele cheio fica na saída.
    /// Verdadeiro = foi para o inventário.</summary>
    private bool ProduceItem(Item resultPrefab)
    {
        if (resultPrefab == null) return false;

        if (slotOutput.item == null && Inventario != null)
        {
            Item novo = resultPrefab.Clone();
            novo.itemAmt = 1;
            if (Inventario.AddItem(novo)) return true;
        }

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
        return false;
    }
}