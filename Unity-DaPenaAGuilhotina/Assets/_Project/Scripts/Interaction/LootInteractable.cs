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
    [Tooltip("ID estável e ÚNICO nesta cena. O que já foi vasculhado e pago é guardado por caso + este ID (sobrevive " +
             "a troca de cena e ao save). Vazio = caminho na hierarquia. Preencha com Ferramentas > Investigação > " +
             "Gerar IDs de interação. Não troque depois que houver saves.")]
    public string idDaInteracao;
    public bool destroyAfterLoot = false;
    [Tooltip("Horas de investigação gastas ao vasculhar este objeto (RelogioDeInvestigacao), mesmo que não haja " +
             "nada útil para o caso: procurar no lugar errado também custa tempo.")]
    [Min(0)] public int custoEmHoras = 1;

    /// <summary>"cena/id" desta interação (ver IdDeInteracao).</summary>
    public string ChaveDaInteracao => IdDeInteracao.ChaveDaCena(this, idDaInteracao);

    /// <summary>Pista deste objeto já guardada no caso atual (outro caso tem estado próprio).</summary>
    private bool ColetadoNoCasoAtual
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.registroDaInvestigacao.LootColetado(gm.casoEscolhido, ChaveDaInteracao);
        }
    }

    public bool PodeInteragir => !ColetadoNoCasoAtual;

    private void Start()
    {
        // Pista já recolhida neste caso (antes de trocar de cena ou carregar o save): o objeto não reaparece.
        if (destroyAfterLoot && ColetadoNoCasoAtual)
        {
            Destroy(gameObject);
            return;
        }

        // Se ativado, a estante (ou objeto) se teletransporta para um local aleatório da sala no início da fase
        if (aleatorizarPosicao && pontosDeSpawn != null)
        {
            // Ignora slots vazios arrastados por engano no Inspector
            var validos = pontosDeSpawn.FindAll(p => p != null);
            if (validos.Count > 0)
            {
                int index = Random.Range(0, validos.Count);
                transform.position = validos[index].position;
            }
        }

        amountToGive = Mathf.Max(1, amountToGive);
    }

    public void Interact()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LootInteractable] GameManager ausente.");
            return;
        }
        if (ColetadoNoCasoAtual) return;

        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual == null)
        {
            Debug.Log("Você não está investigando nenhum caso no momento.");
            return;
        }

        // Procura na lista qual é o item configurado para o caso atual. Caso já concluído não recolhe mais nada.
        Item itemCorreto = null;
        if (pistasPossiveis != null && GameManager.Instance.CasoAtualEmAndamento)
            foreach (var loot in pistasPossiveis)
                if (loot.caso == casoAtual) { itemCorreto = loot.itemParaDar; break; }

        InventoryManager inventory = GameManager.Instance.inventoryManager;
        if (itemCorreto != null && inventory == null)
        {
            Debug.LogWarning("[LootInteractable] InventoryManager ausente no GameManager.");
            return;
        }

        // Inventário cheio (não há descarte): se a pista não cabe, a busca não cobra horas.
        if (itemCorreto != null && !inventory.TemEspacoPara(itemCorreto))
        {
            AvisoNaTela.Mostrar("Inventário cheio: não há onde guardar o que está aqui. Libere espaço antes de vasculhar.");
            return;
        }

        // A primeira busca cobra, mesmo que o objeto esteja vazio; repetir no mesmo caso é de graça.
        if (!RelogioDeInvestigacao.TentarGastar(this, idDaInteracao, custoEmHoras)) return;

        // Se o caso atual não estiver na lista deste móvel, o móvel não entrega nada.
        if (itemCorreto == null)
        {
            AvisoNaTela.Mostrar("Nada de útil para o caso aqui.");
            return;
        }

        RegistroDaInvestigacao registro = GameManager.Instance.registroDaInvestigacao;

        // Save v2 (sem registro de loot): quem já tem a pista a recolheu pela regra antiga.
        if (registro.CasoComEstadoLegado(casoAtual) && inventory.PossuiEmQualquerLugar(itemCorreto.itemID))
        {
            registro.RegistrarLoot(casoAtual, ChaveDaInteracao);
            AvisoNaTela.Mostrar("Você já vasculhou aqui.");
            if (destroyAfterLoot) Destroy(gameObject);
            return;
        }

        Item itemClone = itemCorreto.Clone();
        itemClone.itemAmt = Mathf.Max(1, amountToGive);

        bool foiGuardado = inventory.AddItem(itemClone);

        if (foiGuardado)
        {
            Debug.Log($"Você encontrou: {itemCorreto.name}!");
            registro.RegistrarLoot(casoAtual, ChaveDaInteracao);
            if (destroyAfterLoot) Destroy(gameObject);
        }
        else
        {
            // Inventário cheio: nada é registrado (a busca já foi paga), e o jogador pode voltar sem pagar de novo
            AvisoNaTela.Mostrar("Inventário cheio. Libere espaço e volte para pegar esta pista.");
        }
    }
}