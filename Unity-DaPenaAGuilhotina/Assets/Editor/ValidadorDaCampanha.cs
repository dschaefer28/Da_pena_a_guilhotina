using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Confere se a campanha é jogável do jeito que está nos assets e cenas (sem alterar nada):
/// quantidade de casos por fase, exatamente um caso por rota na Fase 4, receitas/panfletos/alegações completos,
/// registro na Mesa de Casos e no CatalogoDeSave, e — por caso, na cena dele — as oportunidades de investigação e o
/// caminho mínimo até duas pistas Fato dentro do orçamento de horas com dívida.
/// Menu: Ferramentas > Campanha > 2 - Validar campanha. Também roda no teste EditMode CampanhaTests.
/// </summary>
public static class ValidadorDaCampanha
{
    public const int CasosEsperadosFase2 = 3;
    public const int CasosEsperadosFase3 = 4;
    public const int OportunidadesMinimas = 6;
    public const int OportunidadesMaximas = 7;

    public class Resultado
    {
        public readonly List<string> problemas = new List<string>();
        public readonly List<string> avisos = new List<string>();
        public readonly List<string> resumo = new List<string>();

        public string Texto()
        {
            var sb = new StringBuilder("[ValidadorDaCampanha]\n");
            foreach (string s in resumo) sb.AppendLine("  " + s);
            foreach (string s in avisos) sb.AppendLine("  AVISO: " + s);
            foreach (string s in problemas) sb.AppendLine("  PROBLEMA: " + s);
            sb.Append(problemas.Count == 0 ? "Campanha válida." : $"{problemas.Count} problema(s).");
            return sb.ToString();
        }
    }

    [MenuItem("Ferramentas/Campanha/2 - Validar campanha")]
    public static void ValidarPeloMenu()
    {
        Resultado r = Validar();
        if (r.problemas.Count == 0) Debug.Log(r.Texto()); else Debug.LogWarning(r.Texto());
    }

    private class Fonte
    {
        public string nome;
        public int custo;
        public readonly List<Item> itens = new List<Item>();
    }

    public static Resultado Validar()
    {
        var r = new Resultado();

        GameManager gm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/GameManager.prefab")?.GetComponent<GameManager>();
        if (gm == null) { r.problemas.Add("GameManager.prefab não encontrado."); return r; }
        int orcamento = gm.horasPorCaso;
        int orcamentoComDivida = Mathf.Max(1, orcamento - gm.horasPerdidasPorDivida);

        GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI.prefab");
        CaseSelectionUI mesa = ui != null ? ui.GetComponentInChildren<CaseSelectionUI>(true) : null;
        if (mesa == null || mesa.availableCases == null) { r.problemas.Add("Mesa de Casos (CaseSelectionUI no UI.prefab) não encontrada."); return r; }
        CatalogoDeSave catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        if (catalogo == null) r.problemas.Add("Resources/CatalogoDeSave.asset não existe.");

        var casos = new List<CaseData>();
        foreach (CaseData caso in mesa.availableCases)
        {
            if (caso == null) { r.problemas.Add("A mesa tem uma referência de caso vazia."); continue; }
            if (casos.Contains(caso)) { r.problemas.Add($"'{caso.name}' aparece duas vezes na mesa."); continue; }
            casos.Add(caso);
        }

        ValidarQuantidades(casos, gm, r);
        ValidarItensDoProjeto(r);

        var cenasDoBuild = new HashSet<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled) cenasDoBuild.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));

        foreach (CaseData caso in casos) ValidarDadosDoCaso(caso, catalogo, cenasDoBuild, r);
        ValidarBiblioteca(ui, casos, catalogo, r);
        ValidarLinhaEditorial(ui, casos, r);

        // Oportunidades: abre cada cena de investigação uma vez (aditiva, sem salvar) e avalia os casos dela.
        var porCena = new Dictionary<string, List<CaseData>>();
        foreach (CaseData caso in casos)
        {
            if (string.IsNullOrEmpty(caso.nextSceneName) || !cenasDoBuild.Contains(caso.nextSceneName)) continue;
            if (!porCena.ContainsKey(caso.nextSceneName)) porCena[caso.nextSceneName] = new List<CaseData>();
            porCena[caso.nextSceneName].Add(caso);
        }
        foreach (var par in porCena)
        {
            string caminho = $"Assets/Scenes/{par.Key}.unity";
            Scene cena = SceneManager.GetSceneByPath(caminho);
            bool abriu = false;
            if (!cena.isLoaded)
            {
                cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
                abriu = true;
            }
            try
            {
                List<KeyValuePair<Component, string>> interacoes = IdDeInteracao.InteracoesDaCena(cena);
                foreach (string p in IdDeInteracao.Problemas(interacoes)) r.problemas.Add($"{par.Key}: {p}");
                ValidarEtapasComplementares(par.Key, interacoes, r);
                foreach (CaseData caso in par.Value) ValidarInvestigacao(caso, interacoes, orcamento, orcamentoComDivida, r);
            }
            finally
            {
                if (abriu) EditorSceneManager.CloseScene(cena, true);
            }
        }
        return r;
    }

    private static void ValidarQuantidades(List<CaseData> casos, GameManager gm, Resultado r)
    {
        int[] porFase = new int[GameManager.UltimaFase + 1];
        foreach (CaseData caso in casos) if (caso.fase >= 1 && caso.fase <= GameManager.UltimaFase) porFase[caso.fase]++;

        if (porFase[2] != CasosEsperadosFase2) r.problemas.Add($"Fase 2 tem {porFase[2]} caso(s) na mesa; esperado {CasosEsperadosFase2}.");
        if (porFase[3] != CasosEsperadosFase3) r.problemas.Add($"Fase 3 tem {porFase[3]} caso(s) na mesa; esperado {CasosEsperadosFase3}.");
        for (int fase = 2; fase <= GameManager.UltimaFase; fase++)
        {
            int exigidos = fase <= gm.casosPorFase.Length ? gm.casosPorFase[fase - 1] : 1;
            int disponiveis = fase == GameManager.UltimaFase ? 1 : porFase[fase];
            if (disponiveis < exigidos) r.problemas.Add($"Fase {fase} exige {exigidos} caso(s) concluídos, mas só há {disponiveis}.");
        }

        // Fase 4: com a rota travada, a mesa mostra os casos da rota + os de rota Nenhuma. Tem de ser exatamente 1.
        foreach (RotaFinal rota in new[] { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio })
        {
            int visiveis = 0;
            foreach (CaseData caso in casos)
                if (caso.fase == GameManager.UltimaFase && (caso.rota == RotaFinal.Nenhuma || caso.rota == rota)) visiveis++;
            if (visiveis != 1) r.problemas.Add($"Rota {rota}: a Fase 4 mostraria {visiveis} caso(s); esperado exatamente 1.");
        }
        foreach (CaseData caso in casos)
        {
            if (caso.fase == GameManager.UltimaFase && caso.rota == RotaFinal.Nenhuma)
                r.problemas.Add($"'{caso.name}' é da Fase 4 com rota Nenhuma: apareceria em todas as rotas.");
            if (caso.fase < GameManager.UltimaFase && caso.rota != RotaFinal.Nenhuma)
                r.avisos.Add($"'{caso.name}' (Fase {caso.fase}) tem rota {caso.rota}: a rota só é travada na Fase 4 e o caso ficaria oculto.");
        }
        r.resumo.Add($"Casos na mesa — Fase 2: {porFase[2]}, Fase 3: {porFase[3]}, Fase 4: {porFase[4]} (um por rota).");
    }

    // Prompt 3: ofertas coerentes, documentos de apoio do caso certo e aceitos por um qualificador da receita.
    private static void ValidarBiblioteca(GameObject ui, List<CaseData> casos, CatalogoDeSave catalogo, Resultado r)
    {
        BibliotecaUI biblioteca = ui.GetComponentInChildren<BibliotecaUI>(true);
        if (biblioteca == null) { r.problemas.Add("BibliotecaUI não está no UI.prefab."); return; }
        if (ui.GetComponentInChildren<SuportesDaPrensaUI>(true) == null) r.problemas.Add("SuportesDaPrensaUI não está no painel da prensa.");

        var ids = new HashSet<string>();
        int porCaso = 0;
        foreach (OfertaDaBiblioteca o in biblioteca.ofertas)
        {
            if (o == null) { r.problemas.Add("Oferta vazia na biblioteca."); continue; }
            if (string.IsNullOrWhiteSpace(o.id) || !ids.Add(o.id)) r.problemas.Add($"Oferta '{o.name}' sem id ou com id repetido.");
            if (o.caso == null || !casos.Contains(o.caso)) { r.problemas.Add($"Oferta '{o.name}' aponta para um caso fora da mesa."); continue; }
            if (o.item == null || !o.item.EhSuporte || o.item.caso != o.caso) r.problemas.Add($"Oferta '{o.name}': o item precisa ser documento de apoio do mesmo caso.");
            else
            {
                if (o.item.confiabilidade != Confiabilidade.NaoEPista) r.problemas.Add($"'{o.item.name}' é documento de apoio e não pode ter confiabilidade.");
                if (catalogo != null && catalogo.BuscarItem(o.item.itemID) == null) r.problemas.Add($"'{o.item.name}' fora do CatalogoDeSave.");
                if (o.caso.receitaDoPanfleto == null || !o.caso.receitaDoPanfleto.AceitaComoSuporte(o.item))
                    r.avisos.Add($"Oferta '{o.name}': nenhum qualificador da receita aceita o documento (compra sem efeito no panfleto).");
            }
            if (o.preco < 0) r.problemas.Add($"Oferta '{o.name}' com preço negativo.");
            if (o.faseMinima > o.caso.fase) r.problemas.Add($"Oferta '{o.name}' só abre na fase {o.faseMinima}, depois do caso (fase {o.caso.fase}).");
            porCaso++;
        }

        foreach (CaseData caso in casos)
        {
            ReceitaDeCaso receita = caso.receitaDoPanfleto;
            if (receita == null || receita.qualificadores == null) continue;
            var idsQ = new HashSet<string>();
            foreach (ReceitaDeCaso.Qualificador q in receita.qualificadores)
            {
                if (q == null || string.IsNullOrWhiteSpace(q.id) || !idsQ.Add(q.id)) r.problemas.Add($"'{receita.name}': qualificador sem id ou repetido.");
                else if (q.suportesAceitos == null || q.suportesAceitos.Count == 0 || q.suportesAceitos.Exists(s => s == null || s.caso != caso || !s.EhSuporte))
                    r.problemas.Add($"'{receita.name}': qualificador '{q.id}' aceita documento inválido ou de outro caso.");
            }
        }
        r.resumo.Add($"Biblioteca: {porCaso} oferta(s).");
    }

    // Prompt 5: todo caso da Fase 2 em diante exige a escolha explícita entre as três linhas, com a direção de cada uma
    // (Defesa: +Povo/−Estado; Agradar: +Estado/−Povo; Sensacionalista: +ouro e agravamento). O tutorial fica simples.
    private static void ValidarLinhaEditorial(GameObject ui, List<CaseData> casos, Resultado r)
    {
        if (ui.GetComponentInChildren<LinhaEditorialDaPrensaUI>(true) == null)
            r.problemas.Add("LinhaEditorialDaPrensaUI não está no painel da prensa (rode Ferramentas > Campanha > 4).");

        int ativas = 0;
        foreach (CaseData caso in casos)
        {
            ReceitaDeCaso receita = caso.receitaDoPanfleto;
            if (receita == null) continue;
            if (caso.fase < 2)
            {
                if (receita.ExigeLinhaEditorial) r.avisos.Add($"'{receita.name}' (tutorial) exige linha editorial: o tutorial deveria continuar simples.");
                continue;
            }
            if (!receita.ExigeLinhaEditorial)
            {
                r.problemas.Add($"'{receita.name}': caso da Fase {caso.fase} sem linha editorial (publicaria neutro, sem a escolha).");
                continue;
            }
            ativas++;
            ReceitaDeCaso.ConfiguracaoEditorial c = receita.linhaEditorial;
            if (c.defesaDoPovo == null || c.defesaDoPovo.povo <= 0 || c.defesaDoPovo.estado >= 0)
                r.problemas.Add($"'{receita.name}': Defesa do povo precisa de Povo positivo e Estado negativo.");
            if (c.agradarOPoder == null || c.agradarOPoder.estado <= 0 || c.agradarOPoder.povo >= 0)
                r.problemas.Add($"'{receita.name}': {LinhasEditoriais.Rotulo(LinhaEditorial.AgradarOPoder, receita)} precisa de Estado positivo e Povo negativo.");
            if (c.sensacionalista == null || c.sensacionalista.ouro <= 0)
                r.problemas.Add($"'{receita.name}': Sensacionalista precisa de ouro adicional.");
            if (c.agravamentoPercentual <= 0)
                r.problemas.Add($"'{receita.name}': Sensacionalista sem agravamento da penalidade de boato.");
        }
        r.resumo.Add($"Linha editorial: {ativas} receita(s) com as três opções.");
    }

    // Etapas complementares: id estável, único por reação, diferente do id reservado da etapa normal; requisitos e
    // recompensas preenchidos; recompensas do próprio caso.
    private static void ValidarEtapasComplementares(string cena, List<KeyValuePair<Component, string>> interacoes, Resultado r)
    {
        foreach (var par in interacoes)
        {
            if (!(par.Key is NPCMovement npc) || npc.reacoesDeCaso == null) continue;
            foreach (CasoReacao reacao in npc.reacoesDeCaso)
            {
                if (reacao.etapasComplementares == null) continue;
                var ids = new HashSet<string>();
                foreach (EtapaComplementar e in reacao.etapasComplementares)
                {
                    string onde = $"{cena}/{npc.name} ({(reacao.caso != null ? reacao.caso.name : "-")})";
                    if (string.IsNullOrWhiteSpace(e.id) || e.id == RegistroDaInvestigacao.EtapaReacao || !ids.Add(e.id))
                        r.problemas.Add($"{onde}: etapa complementar com id vazio, repetido ou reservado ('{e.id}').");
                    if (e.dialogo == null || e.recompensas == null || e.recompensas.Count == 0 || e.recompensas.Exists(i => i == null))
                        r.problemas.Add($"{onde}: etapa '{e.id}' sem fala ou com recompensa vazia.");
                    else if (e.recompensas.Exists(i => i.caso != reacao.caso))
                        r.problemas.Add($"{onde}: etapa '{e.id}' entrega item de outro caso.");
                    if (e.requisitos != null && e.requisitos.Exists(i => i == null))
                        r.problemas.Add($"{onde}: etapa '{e.id}' com pré-requisito vazio.");
                }
            }
        }
    }

    // Prefixos que marcavam a verdade no nome (Prompt 2, revisado): um boato/calúnia não pode ser reconhecido pelo nome.
    private static readonly string[] PrefixosQueRevelam = { "Conversa sobre", "Libelo", "Folha:", "Cartaz:", "Denúncia:", "Bilhete", "Boato", "Calúnia", "Rumor" };

    private static void ValidarItensDoProjeto(Resultado r)
    {
        var ids = new Dictionary<string, Item>();
        foreach (string guid in AssetDatabase.FindAssets("t:Item", new[] { "Assets" }))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemID)) { r.problemas.Add($"Item '{item.name}' sem itemID."); continue; }
            if (ids.TryGetValue(item.itemID, out Item outro)) r.problemas.Add($"itemID '{item.itemID}' repetido em '{item.name}' e '{outro.name}'.");
            else ids.Add(item.itemID, item);
            if (item.EhPista && (string.IsNullOrWhiteSpace(item.descricao) || string.IsNullOrWhiteSpace(item.fonte)))
                r.avisos.Add($"Pista '{item.name}' sem descrição ou fonte (a ficha do inventário fica vazia).");
            if (item.EhPista && !item.caso.EhAlegacao(item))
                foreach (string prefixo in PrefixosQueRevelam)
                    if (item.NomeExibicao.StartsWith(prefixo, System.StringComparison.OrdinalIgnoreCase))
                        r.problemas.Add($"Pista '{item.name}': o nome '{item.NomeExibicao}' denuncia a confiabilidade (prefixo '{prefixo}').");
        }
    }

    private static void ValidarDadosDoCaso(CaseData caso, CatalogoDeSave catalogo, HashSet<string> cenasDoBuild, Resultado r)
    {
        string n = caso.name;
        if (string.IsNullOrWhiteSpace(caso.caseTitle)) r.problemas.Add($"'{n}' sem título.");
        if (!cenasDoBuild.Contains(caso.nextSceneName)) r.problemas.Add($"'{n}': cena '{caso.nextSceneName}' não está no build.");
        if (catalogo != null && !catalogo.casos.Contains(caso)) r.problemas.Add($"'{n}' não está no CatalogoDeSave (o save perderia o caso).");

        ReceitaDeCaso receita = caso.receitaDoPanfleto;
        if (receita == null) { r.problemas.Add($"'{n}' sem Receita Do Panfleto: nada pode ser impresso."); return; }
        if (receita.caso != caso) r.problemas.Add($"A receita '{receita.name}' aponta para outro caso ('{(receita.caso != null ? receita.caso.name : "-")}').");
        foreach (NivelDoPanfleto nivel in new[] { NivelDoPanfleto.Fatos, NivelDoPanfleto.ComBoato, NivelDoPanfleto.Calunia, NivelDoPanfleto.Alegacoes })
        {
            Item panfleto = receita.PanfletoDe(receita.VersaoDe(nivel));
            if (panfleto == null) r.problemas.Add($"'{receita.name}': versão {nivel} sem panfleto.");
            else if (catalogo != null && catalogo.BuscarItem(panfleto.itemID) == null) r.problemas.Add($"Panfleto '{panfleto.name}' fora do CatalogoDeSave.");
        }

        // Saída sem bloqueio: duas alegações do próprio caso, distintas, que a prensa aceita juntas.
        if (caso.alegacoesIniciais == null || caso.alegacoesIniciais.Count < 2)
        {
            r.problemas.Add($"'{n}' sem as duas alegações iniciais: sem pistas, o caso não teria saída.");
            return;
        }
        var ids = new HashSet<string>();
        foreach (Item alegacao in caso.alegacoesIniciais)
        {
            if (alegacao == null) { r.problemas.Add($"'{n}': alegação vazia."); continue; }
            if (alegacao.caso != caso || !alegacao.EhPista) r.problemas.Add($"'{n}': a alegação '{alegacao.name}' precisa ser pista do próprio caso.");
            if (!ids.Add(alegacao.itemID)) r.problemas.Add($"'{n}': alegações com o mesmo itemID.");
            if (catalogo != null && catalogo.BuscarItem(alegacao.itemID) == null) r.problemas.Add($"Alegação '{alegacao.name}' fora do CatalogoDeSave.");
        }
        if (caso.alegacoesIniciais.Count >= 2 && caso.alegacoesIniciais[0] != null && caso.alegacoesIniciais[1] != null &&
            ReceitaDeCaso.Classificar(caso.alegacoesIniciais[0], caso.alegacoesIniciais[1]) != NivelDoPanfleto.Alegacoes)
            r.problemas.Add($"'{n}': as duas alegações não são reconhecidas pela prensa como 'Só Alegações'.");
    }

    private static void ValidarInvestigacao(CaseData caso, List<KeyValuePair<Component, string>> interacoes, int orcamento, int orcamentoComDivida, Resultado r)
    {
        var fontes = new List<Fonte>();
        int oportunidades = 0;

        foreach (var par in interacoes)
        {
            if (par.Key is NPCMovement npc)
            {
                if (!npc.canInteract || (npc.casoObrigatorio != null && npc.casoObrigatorio != caso)) continue;
                bool temReacao = false;
                var fonte = new Fonte { nome = npc.name, custo = npc.custoEmHoras };
                if (npc.reacoesDeCaso != null)
                    foreach (CasoReacao reacao in npc.reacoesDeCaso)
                    {
                        if (reacao.caso != caso) continue;
                        temReacao = reacao.dialogoInicialDoCaso != null || reacao.dialogoComPista != null;
                        if (reacao.recompensasDoDialogo != null) fonte.itens.AddRange(reacao.recompensasDoDialogo.FindAll(i => i != null));
                        break;
                    }
                if (!temReacao && !TemFala(npc.dialogoPadrao)) continue; // não responde ao caso
                oportunidades++;
                if (fonte.itens.Count > 0) fontes.Add(fonte);
            }
            else if (par.Key is LootInteractable loot)
            {
                oportunidades++;
                var fonte = new Fonte { nome = loot.name, custo = loot.custoEmHoras };
                if (loot.pistasPossiveis != null)
                    foreach (LootDeCaso entrada in loot.pistasPossiveis)
                        if (entrada.caso == caso && entrada.itemParaDar != null) { fonte.itens.Add(entrada.itemParaDar); break; }
                if (fonte.itens.Count > 0) fontes.Add(fonte);
            }
        }

        int fatos = 0, duvidosas = 0;
        foreach (Fonte f in fontes)
            foreach (Item item in f.itens)
            {
                if (item.caso != caso) r.problemas.Add($"'{caso.name}': '{f.nome}' entrega '{item.name}', que pertence a outro caso.");
                else if (item.confiabilidade == Confiabilidade.Fato) fatos++;
                else if (item.confiabilidade == Confiabilidade.Boato || item.confiabilidade == Confiabilidade.Calunia) duvidosas++;
            }

        int caminho = CaminhoMinimoAteDoisFatos(fontes, caso);
        int vazias = oportunidades - fontes.Count;
        r.resumo.Add($"{caso.name} ({caso.nextSceneName}): {oportunidades} oportunidades, {fontes.Count} com pista ({fatos} fato(s), " +
                     $"{duvidosas} boato/calúnia), {vazias} sem nada; caminho mínimo até 2 fatos: " +
                     (caminho == int.MaxValue ? "impossível" : caminho + "h") + $" (orçamento {orcamento}h, {orcamentoComDivida}h endividado).");

        if (oportunidades < OportunidadesMinimas) r.problemas.Add($"'{caso.name}': só {oportunidades} oportunidades (mínimo {OportunidadesMinimas}).");
        if (oportunidades > OportunidadesMaximas) r.avisos.Add($"'{caso.name}': {oportunidades} oportunidades (planejado até {OportunidadesMaximas}).");
        if (caminho > orcamentoComDivida) r.problemas.Add($"'{caso.name}': duas pistas Fato custam no mínimo " +
                                                          (caminho == int.MaxValue ? "∞" : caminho + "h") + $", acima de {orcamentoComDivida}h (orçamento com dívida).");
        if (duvidosas == 0) r.avisos.Add($"'{caso.name}': nenhum boato ou calúnia na investigação.");
        if (vazias == 0) r.avisos.Add($"'{caso.name}': nenhuma oportunidade vazia (sem risco de investigar no lugar errado).");
    }

    private static bool TemFala(DialogueData dialogo) => dialogo != null && dialogo.talkScript != null && dialogo.talkScript.Count > 0;

    // Menor soma de custos para juntar duas pistas Fato DIFERENTES do caso (uma fonte pode dar as duas).
    private static int CaminhoMinimoAteDoisFatos(List<Fonte> fontes, CaseData caso)
    {
        var fatosPorFonte = new List<HashSet<string>>();
        foreach (Fonte f in fontes)
        {
            var ids = new HashSet<string>();
            foreach (Item i in f.itens) if (i.caso == caso && i.confiabilidade == Confiabilidade.Fato) ids.Add(i.itemID);
            fatosPorFonte.Add(ids);
        }

        int melhor = int.MaxValue;
        for (int a = 0; a < fontes.Count; a++)
        {
            if (fatosPorFonte[a].Count >= 2) melhor = Mathf.Min(melhor, fontes[a].custo);
            for (int b = a + 1; b < fontes.Count; b++)
            {
                var uniao = new HashSet<string>(fatosPorFonte[a]);
                uniao.UnionWith(fatosPorFonte[b]);
                if (uniao.Count >= 2) melhor = Mathf.Min(melhor, fontes[a].custo + fontes[b].custo);
            }
        }
        return melhor;
    }
}
