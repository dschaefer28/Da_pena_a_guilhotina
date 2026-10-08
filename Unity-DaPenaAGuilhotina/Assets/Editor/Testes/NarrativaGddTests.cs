using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Conteúdo narrativo do GDD nas Fases 2 a 4 (Ferramentas > Campanha > 8). Usa os assets e as cenas reais: confere que as
/// conversas dos NPCs formam grafos completos (sem referência nula, sem ciclo, opções só na última fala) e que nenhum
/// texto que o jogador vê ainda cita os casos que o GDD substituiu.
/// </summary>
public class NarrativaGddTests
{
    private static readonly string[] CenasDaCampanha = { "Fase2", "Fase3", "Fase4" };

    // Personagens, lugares e assuntos dos casos provisórios de 25–27/09 que o conteúdo do GDD substituiu.
    private static readonly string[] TextosAntigos =
    {
        "Marchand", "Garnier", "Delorme", "Vautrin", "Joubert", "Lacombe", "Morel", "Viúva François", "Carcereiro",
        "Cocheiro", "Gravador", "Peticionária", "Varennes", "assignat", "Sans-culotte", "Korff", "Sainte-Menehould",
        "Caso das Joias",
    };

    private static List<CaseData> CasosDaCampanha()
    {
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI.prefab");
        return ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases.FindAll(c => c != null && c.fase >= 2);
    }

    /// <summary>Abre as cenas das Fases 2 a 4 (aditivas, sem salvar), entrega os NPCs e objetos de cada uma e as fecha.</summary>
    private static void ParaCadaCena(System.Action<string, List<NPCMovement>, List<LootInteractable>> acao)
    {
        foreach (string nome in CenasDaCampanha)
        {
            string caminho = $"Assets/_Project/Scenes/{nome}.unity";
            Scene cena = SceneManager.GetSceneByPath(caminho);
            bool abriu = false;
            if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive); abriu = true; }
            try
            {
                var npcs = new List<NPCMovement>();
                var objetos = new List<LootInteractable>();
                foreach (GameObject raiz in cena.GetRootGameObjects())
                {
                    npcs.AddRange(raiz.GetComponentsInChildren<NPCMovement>(true));
                    objetos.AddRange(raiz.GetComponentsInChildren<LootInteractable>(true));
                }
                acao(nome, npcs, objetos);
            }
            finally
            {
                if (abriu) EditorSceneManager.CloseScene(cena, true);
            }
        }
    }

    /// <summary>Conversas que um NPC pode tocar: fala padrão, entrada e fala depois da entrega de cada reação e etapas.</summary>
    private static IEnumerable<DialogueData> RaizesDoNpc(NPCMovement npc)
    {
        if (npc.dialogoPadrao != null) yield return npc.dialogoPadrao;
        foreach (CasoReacao reacao in npc.reacoesDeCaso)
        {
            yield return reacao.dialogoInicialDoCaso; // obrigatória: nula é defeito
            if (reacao.dialogoDepoisDaEntrega != null) yield return reacao.dialogoDepoisDaEntrega;
            if (reacao.etapasComplementares != null)
                foreach (EtapaComplementar etapa in reacao.etapasComplementares) yield return etapa.dialogo;
        }
    }

    // Percorre o grafo a partir de um nó: todo nó alcançável tem falas completas, opções só na última fala e nenhum
    // caminho volta a um nó já aberto (a conversa sempre termina).
    private static void ConferirGrafo(DialogueData no, string onde, HashSet<DialogueData> naPilha, HashSet<DialogueData> vistos, List<string> problemas, List<DialogueData> alcancaveis)
    {
        if (no == null) { problemas.Add($"{onde}: referência de diálogo nula."); return; }
        if (naPilha.Contains(no)) { problemas.Add($"{onde}: ciclo em '{no.name}'."); return; }
        if (!vistos.Add(no)) return;
        alcancaveis.Add(no);
        if (no.talkScript == null || no.talkScript.Count == 0) { problemas.Add($"{onde}: '{no.name}' sem falas."); return; }

        naPilha.Add(no);
        for (int i = 0; i < no.talkScript.Count; i++)
        {
            Dialogue fala = no.talkScript[i];
            if (string.IsNullOrWhiteSpace(fala.name) || string.IsNullOrWhiteSpace(fala.text))
                problemas.Add($"{onde}: '{no.name}' fala {i} sem falante ou sem texto.");
            if (fala.choices == null || fala.choices.Count == 0) continue;
            if (i != no.talkScript.Count - 1) problemas.Add($"{onde}: '{no.name}' tem opções na fala {i}, antes da última.");
            foreach (Choice opcao in fala.choices)
            {
                if (string.IsNullOrWhiteSpace(opcao.choiceText)) problemas.Add($"{onde}: '{no.name}' tem opção sem texto.");
                ConferirGrafo(opcao.nextDialogue, $"{onde} > '{opcao.choiceText}'", naPilha, vistos, problemas, alcancaveis);
            }
        }
        naPilha.Remove(no);
    }

    [Test]
    public void Conversas_DasFasesDoisAQuatro_SaoGrafosCompletos_ETodoNpcRespondeATodoCaso()
    {
        List<CaseData> casos = CasosDaCampanha();
        var problemas = new List<string>();
        int conversas = 0;
        ParaCadaCena((cena, npcs, objetos) =>
        {
            List<CaseData> daCena = casos.FindAll(c => c.nextSceneName == cena);
            Assert.AreEqual(3, daCena.Count, $"{cena}: três casos na mesa");
            foreach (NPCMovement npc in npcs)
            {
                foreach (CaseData caso in daCena)
                    if (!npc.reacoesDeCaso.Exists(r => r.caso == caso))
                        problemas.Add($"{cena}/{npc.name}: sem reação ao caso '{caso.name}' (responderia só com a fala padrão).");
                foreach (DialogueData raiz in RaizesDoNpc(npc))
                {
                    conversas++;
                    ConferirGrafo(raiz, $"{cena}/{npc.name}", new HashSet<DialogueData>(), new HashSet<DialogueData>(), problemas, new List<DialogueData>());
                }
            }
        });
        Assert.Greater(conversas, 0);
        Assert.IsEmpty(problemas, string.Join("\n", problemas));
    }

    [Test]
    public void TextosDoJogador_NasFasesDoisAQuatro_NaoCitamOsCasosSubstituidos()
    {
        var textos = new List<KeyValuePair<string, string>>(); // onde, texto
        void Anotar(string onde, string texto) { if (!string.IsNullOrEmpty(texto)) textos.Add(new KeyValuePair<string, string>(onde, texto)); }
        void AnotarItem(Item item) { if (item == null) return; Anotar(item.name, item.itemName); Anotar(item.name, item.fonte); Anotar(item.name, item.descricao); }

        foreach (CaseData caso in CasosDaCampanha())
        {
            Anotar(caso.name, caso.caseTitle); Anotar(caso.name, caso.caseDescription); Anotar(caso.name, caso.objectiveText);
            foreach (Item alegacao in caso.alegacoesIniciais) AnotarItem(alegacao);
            foreach (Item pista in caso.deducao.Conjunto()) AnotarItem(pista);
            ReceitaDeCaso receita = caso.receitaDoPanfleto;
            AnotarItem(receita.panfletoPadrao);
            foreach (ReceitaDeCaso.Versao versao in new[] { receita.soFatos, receita.comBoato, receita.calunia, receita.soAlegacoes })
            {
                Anotar(receita.name, versao.textoRevelacao);
                AnotarItem(versao.panfleto);
            }
        }
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI.prefab");
        foreach (OfertaDaBiblioteca oferta in ui.GetComponentInChildren<BibliotecaUI>(true).ofertas)
        {
            Anotar(oferta.name, oferta.titulo); Anotar(oferta.name, oferta.descricao);
            AnotarItem(oferta.item);
        }
        ParaCadaCena((cena, npcs, objetos) =>
        {
            foreach (NPCMovement npc in npcs)
            {
                var vistos = new HashSet<DialogueData>();
                var alcancaveis = new List<DialogueData>();
                foreach (DialogueData raiz in RaizesDoNpc(npc))
                    ConferirGrafo(raiz, cena, new HashSet<DialogueData>(), vistos, new List<string>(), alcancaveis);
                foreach (DialogueData no in alcancaveis)
                    foreach (Dialogue fala in no.talkScript)
                    {
                        Anotar($"{cena}/{no.name}", fala.name); Anotar($"{cena}/{no.name}", fala.text);
                        if (fala.choices != null) foreach (Choice opcao in fala.choices) Anotar($"{cena}/{no.name}", opcao.choiceText);
                    }
                foreach (CasoReacao reacao in npc.reacoesDeCaso)
                {
                    foreach (Item item in reacao.recompensasDoDialogo) AnotarItem(item);
                    if (reacao.etapasComplementares != null)
                        foreach (EtapaComplementar etapa in reacao.etapasComplementares) foreach (Item item in etapa.recompensas) AnotarItem(item);
                }
            }
            foreach (LootInteractable objeto in objetos)
                foreach (LootDeCaso entrada in objeto.pistasPossiveis) AnotarItem(entrada.itemParaDar);
        });

        var achados = new List<string>();
        foreach (var par in textos)
            foreach (string antigo in TextosAntigos)
                if (par.Value.IndexOf(antigo, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    achados.Add($"{par.Key}: '{antigo}' em \"{par.Value}\"");
        Assert.Greater(textos.Count, 300, "o levantamento deveria cobrir casos, pistas, biblioteca e conversas");
        Assert.IsEmpty(achados, string.Join("\n", achados));
    }
}
