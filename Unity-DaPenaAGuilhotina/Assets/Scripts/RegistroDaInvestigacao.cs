using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memória da investigação que sobrevive a troca de cena e ao save: o que já foi pago no relógio, o que já foi
/// vasculhado e quais recompensas os NPCs já entregaram — sempre por CASO + INTERAÇÃO. Assim, voltar a uma cena
/// não cobra horas de novo, nem devolve pistas já recebidas (mesmo que tenham sido gastas na prensa), e o mesmo
/// NPC/objeto continua com estado próprio em outro caso.
///
/// Chave de interação = "cena/id" (IdDeInteracao). Chave completa = "caso|cena/id", onde caso = CaseData.name
/// (o mesmo identificador que o SistemaDeSave já grava).
/// </summary>
[Serializable]
public class RegistroDaInvestigacao
{
    /// <summary>Etapa única de recompensa de uma reação de caso (CasoReacao tem uma lista de recompensas só).</summary>
    public const string EtapaReacao = "reacao";

    [Tooltip("\"caso|cena/id\" das interações que já cobraram horas no caso.")]
    public List<string> interacoesPagas = new List<string>();
    [Tooltip("\"caso|cena/id\" dos objetos cuja pista já foi guardada no inventário.")]
    public List<string> lootsColetados = new List<string>();
    [Tooltip("\"caso|cena/id|etapa|itemID\" das recompensas de diálogo já entregues.")]
    public List<string> recompensasEntregues = new List<string>();

    [Header("Migração de save antigo (versão 2)")]
    [Tooltip("Casos que estavam em andamento num save v2: não há registro de loot/recompensas deles, então uma pista " +
             "que o jogador ainda tem conta como já entregue (regra antiga) em vez de ser dada de novo.")]
    public List<string> casosComEstadoLegado = new List<string>();
    [Tooltip("\"caso|cena/nomeDoObjeto\" pagos num save v2 (a chave antiga usava o nome do GameObject).")]
    public List<string> interacoesPagasLegadas = new List<string>();

    public static string NomeDoCaso(CaseData caso) => caso != null ? caso.name : string.Empty;

    public static string Chave(CaseData caso, string interacao) => NomeDoCaso(caso) + "|" + interacao;

    public static string ChaveDeRecompensa(CaseData caso, string interacao, string etapa, string itemID) =>
        Chave(caso, interacao) + "|" + etapa + "|" + itemID;

    // ===== Relógio =====

    public bool InteracaoPaga(CaseData caso, string interacao) =>
        caso != null && interacoesPagas.Contains(Chave(caso, interacao));

    public void RegistrarPagamento(CaseData caso, string interacao)
    {
        if (caso == null) return;
        string chave = Chave(caso, interacao);
        if (!interacoesPagas.Contains(chave)) interacoesPagas.Add(chave);
    }

    /// <summary>Save v2: a mesma interação foi paga com a chave antiga (cena/nome do GameObject)?
    /// Homônimos na mesma cena compartilham a chave antiga — exatamente como o jogo antigo cobrava.</summary>
    public bool PagaNoSaveAntigo(CaseData caso, string chaveAntiga) =>
        caso != null && !string.IsNullOrEmpty(chaveAntiga) && interacoesPagasLegadas.Contains(Chave(caso, chaveAntiga));

    // ===== Loot =====

    public bool LootColetado(CaseData caso, string interacao) =>
        caso != null && lootsColetados.Contains(Chave(caso, interacao));

    public void RegistrarLoot(CaseData caso, string interacao)
    {
        if (caso == null) return;
        string chave = Chave(caso, interacao);
        if (!lootsColetados.Contains(chave)) lootsColetados.Add(chave);
    }

    // ===== Recompensas de NPC =====

    public bool RecompensaEntregue(CaseData caso, string interacao, string etapa, string itemID) =>
        caso != null && recompensasEntregues.Contains(ChaveDeRecompensa(caso, interacao, etapa, itemID));

    public void RegistrarRecompensa(CaseData caso, string interacao, string etapa, string itemID)
    {
        if (caso == null || string.IsNullOrEmpty(itemID)) return;
        string chave = ChaveDeRecompensa(caso, interacao, etapa, itemID);
        if (!recompensasEntregues.Contains(chave)) recompensasEntregues.Add(chave);
    }

    public bool CasoComEstadoLegado(CaseData caso) => caso != null && casosComEstadoLegado.Contains(caso.name);

    public void Limpar()
    {
        interacoesPagas.Clear();
        lootsColetados.Clear();
        recompensasEntregues.Clear();
        casosComEstadoLegado.Clear();
        interacoesPagasLegadas.Clear();
    }
}

/// <summary>
/// Identificador estável de NPCs e objetos investigáveis: o "Id Da Interacao" serializado no componente, único na
/// cena. Vazio = caminho do objeto na hierarquia (o mesmo valor que Ferramentas > Investigação > Gerar IDs grava),
/// com aviso. Nunca usa GetInstanceID (muda a cada execução) nem só o nome do GameObject (homônimos colidem).
/// </summary>
public static class IdDeInteracao
{
    /// <summary>"cena/id" — a parte da chave que identifica a interação dentro do jogo inteiro.</summary>
    public static string ChaveDaCena(Component alvo, string idSerializado)
    {
        if (alvo == null) return string.Empty;
        return alvo.gameObject.scene.name + "/" + IdEfetivo(alvo, idSerializado);
    }

    public static string IdEfetivo(Component alvo, string idSerializado) =>
        string.IsNullOrWhiteSpace(idSerializado) ? CaminhoNaHierarquia(alvo.transform) : idSerializado.Trim();

    /// <summary>Chave do save v2 (RelogioDeInvestigacao antigo): "cena/nomeDoGameObject".</summary>
    public static string ChaveAntiga(Component alvo) =>
        alvo != null ? alvo.gameObject.scene.name + "/" + alvo.gameObject.name : string.Empty;

    public static string CaminhoNaHierarquia(Transform t)
    {
        if (t == null) return string.Empty;
        string caminho = t.name;
        for (Transform pai = t.parent; pai != null; pai = pai.parent) caminho = pai.name + "/" + caminho;
        return caminho;
    }

    /// <summary>Lista problemas de IDs (vazios ou repetidos) entre os componentes dados, todos da mesma cena.</summary>
    public static List<string> Problemas(IEnumerable<KeyValuePair<Component, string>> interacoes)
    {
        var problemas = new List<string>();
        var vistos = new Dictionary<string, Component>();
        foreach (var par in interacoes)
        {
            if (par.Key == null) continue;
            if (string.IsNullOrWhiteSpace(par.Value))
                problemas.Add($"'{CaminhoNaHierarquia(par.Key.transform)}' ({par.Key.GetType().Name}) está sem Id Da Interacao.");
            string id = IdEfetivo(par.Key, par.Value);
            if (vistos.TryGetValue(id, out Component outro))
                problemas.Add($"Id Da Interacao '{id}' repetido em '{CaminhoNaHierarquia(par.Key.transform)}' e " +
                              $"'{CaminhoNaHierarquia(outro.transform)}': as duas interações dividiriam cobrança e recompensas.");
            else
                vistos.Add(id, par.Key);
        }
        return problemas;
    }

    /// <summary>Coleta NPCs e objetos investigáveis de uma cena já carregada.</summary>
    public static List<KeyValuePair<Component, string>> InteracoesDaCena(UnityEngine.SceneManagement.Scene cena)
    {
        var lista = new List<KeyValuePair<Component, string>>();
        if (!cena.IsValid() || !cena.isLoaded) return lista;
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            foreach (NPCMovement npc in raiz.GetComponentsInChildren<NPCMovement>(true))
                lista.Add(new KeyValuePair<Component, string>(npc, npc.idDaInteracao));
            foreach (LootInteractable loot in raiz.GetComponentsInChildren<LootInteractable>(true))
                lista.Add(new KeyValuePair<Component, string>(loot, loot.idDaInteracao));
        }
        return lista;
    }

    /// <summary>Avisa no Console sobre IDs vazios/repetidos da cena (chamado a cada cena carregada).</summary>
    public static void ValidarCena(UnityEngine.SceneManagement.Scene cena)
    {
        foreach (string problema in Problemas(InteracoesDaCena(cena)))
            Debug.LogWarning($"[IdDeInteracao] Cena '{cena.name}': {problema} Use Ferramentas > Investigação > Gerar IDs de interação.");
    }
}
