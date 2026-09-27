using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Confere se a campanha é jogável do jeito que está nos assets e cenas (sem alterar nada):
/// quantidade de casos por fase, exatamente um caso por rota na Fase 4, receitas/panfletos/alegações completos,
/// registro na Mesa de Casos e no CatalogoDeSave, a dedução ativa das Fases 2, 3 e 4 (pares coerentes), o orçamento de
/// horas (6h nas Fases 2 a 4) e — por caso, na cena dele — as oportunidades de investigação, o caminho mínimo até duas
/// pistas Fato dentro do orçamento com dívida e quantas horas custa descobrir o quadro de dedução inteiro.
/// Menu: Ferramentas > Campanha > 2 - Validar campanha. Também roda no teste EditMode CampanhaTests.
/// </summary>
public static class ValidadorDaCampanha
{
    public const int CasosEsperadosFase2 = 3;
    public const int CasosEsperadosFase3 = 3; // decisão do grupo (27/09): um caso entre três, como na Fase 2
    public const int OportunidadesMinimas = 6;
    public const int OportunidadesMaximas = 7;
    public const int HorasEsperadasPorCaso = 6; // decisão do grupo (27/09): 6h no relógio nas Fases 2, 3 e 4
    public const int PrimeiraFaseComDeducao = 2; // decisão do grupo (27/09): o quadro de dedução vale desde a Fase 2

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
        ValidarOrcamento(casos, gm, r);
        ValidarItensDoProjeto(r);

        var cenasDoBuild = new HashSet<string>();
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled) cenasDoBuild.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));

        foreach (CaseData caso in casos) ValidarDadosDoCaso(caso, catalogo, cenasDoBuild, r);
        ValidarBiblioteca(ui, casos, catalogo, r);
        ValidarLinhaEditorial(ui, casos, r);
        ValidarDeducao(ui, casos, r);

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
                foreach (CaseData caso in par.Value)
                {
                    int orcamento = HorasDoCaso(caso, gm);
                    ValidarInvestigacao(caso, interacoes, orcamento, Mathf.Max(1, orcamento - gm.horasPerdidasPorDivida), r);
                }
            }
            finally
            {
                if (abriu) EditorSceneManager.CloseScene(cena, true);
            }
        }
        return r;
    }

    /// <summary>Horas do relógio para o caso: o valor do caso, ou o padrão do GameManager (mesma regra de GameManager.IniciarRelogio).</summary>
    public static int HorasDoCaso(CaseData caso, GameManager gm) =>
        caso != null && caso.horasDeInvestigacao > 0 ? caso.horasDeInvestigacao : (gm != null ? gm.horasPorCaso : 0);

    // Prompt 7: 6h no relógio para todo caso das Fases 2, 3 e 4 (a conversão continua 1h por interação nova).
    private static void ValidarOrcamento(List<CaseData> casos, GameManager gm, Resultado r)
    {
        foreach (CaseData caso in casos)
        {
            if (caso.fase < 2) continue;
            int horas = HorasDoCaso(caso, gm);
            if (horas != HorasEsperadasPorCaso)
                r.problemas.Add($"'{caso.name}' (Fase {caso.fase}) tem {horas}h de investigação; o combinado é {HorasEsperadasPorCaso}h nas Fases 2 a 4.");
        }
        r.resumo.Add($"Orçamento: {gm.horasPorCaso}h por caso ({Mathf.Max(1, gm.horasPorCaso - gm.horasPerdidasPorDivida)}h endividado); 1h por interação nova.");
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

        // Fase 4: com a rota travada, a mesa mostra só o caso daquela rota (GameManager.CasoDaRotaAtual). Exatamente 1.
        foreach (RotaFinal rota in new[] { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio })
        {
            int visiveis = 0;
            foreach (CaseData caso in casos)
                if (caso.fase == GameManager.UltimaFase && caso.rota == rota) visiveis++;
            if (visiveis != 1) r.problemas.Add($"Rota {rota}: a Fase 4 mostraria {visiveis} caso(s); esperado exatamente 1.");
        }
        foreach (CaseData caso in casos)
        {
            if (caso.fase == GameManager.UltimaFase && caso.rota == RotaFinal.Nenhuma)
                r.problemas.Add($"'{caso.name}' é da Fase 4 com rota Nenhuma: não apareceria em rota nenhuma.");
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

    // Prompt 4 (Prompt 7: desde a Fase 2): dedução ativa em todo caso das Fases 2, 3 e 4, com pares coerentes (exatamente
    // um Fato por par, pistas do próprio caso, sem alegações nem documentos de apoio, sem repetir pista), e o botão do
    // quadro aparecendo já na Fase 2. O tutorial (Fase 1) segue a regra antiga.
    private static void ValidarDeducao(GameObject ui, List<CaseData> casos, Resultado r)
    {
        QuadroDeDeducaoUI quadro = ui.GetComponentInChildren<QuadroDeDeducaoUI>(true);
        if (quadro == null)
            r.problemas.Add("QuadroDeDeducaoUI não está no UI.prefab (rode Ferramentas > Campanha > 5).");
        else if (quadro.faseMinima > PrimeiraFaseComDeducao)
            r.problemas.Add($"QuadroDeDeducaoUI (UI.prefab) só aparece a partir da Fase {quadro.faseMinima}: a dedução vale desde a Fase {PrimeiraFaseComDeducao} (rode Ferramentas > Campanha > 5).");

        int aderentes = 0;
        foreach (CaseData caso in casos)
        {
            foreach (string p in Deducao.Problemas(caso)) r.problemas.Add($"'{caso.name}' (dedução): {p}");
            if (Deducao.Aderente(caso))
            {
                aderentes++;
                if (caso.fase < PrimeiraFaseComDeducao) r.avisos.Add($"'{caso.name}' (Fase {caso.fase}) tem dedução ativa: o planejado é a partir da Fase {PrimeiraFaseComDeducao}.");
            }
            else if (caso.fase >= PrimeiraFaseComDeducao)
                r.problemas.Add($"'{caso.name}' (Fase {caso.fase}) sem dedução ativa: o quadro vale para todo caso das Fases 2, 3 e 4.");
        }
        r.resumo.Add($"Dedução ativa: {aderentes} caso(s).");
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
        string deducao = string.Empty;
        if (Deducao.Aderente(caso))
        {
            int horas = CustoDoQuadroInteiro(fontes, caso, r);
            deducao = "; quadro de dedução inteiro: " + (horas == int.MaxValue ? "impossível" : horas + "h");
            if (horas == int.MaxValue) r.problemas.Add($"'{caso.name}': alguma pista do quadro de dedução não é entregue por ninguém na cena.");
            else if (horas > orcamento) r.problemas.Add($"'{caso.name}': descobrir o quadro de dedução inteiro custa {horas}h, acima do orçamento de {orcamento}h (a conferência seria impossível).");
            else if (horas > orcamentoComDivida) r.avisos.Add($"'{caso.name}': descobrir o quadro de dedução inteiro custa {horas}h, acima do orçamento com dívida ({orcamentoComDivida}h).");
        }
        r.resumo.Add($"{caso.name} ({caso.nextSceneName}): {oportunidades} oportunidades, {fontes.Count} com pista ({fatos} fato(s), " +
                     $"{duvidosas} boato/calúnia), {vazias} sem nada; caminho mínimo até 2 fatos: " +
                     (caminho == int.MaxValue ? "impossível" : caminho + "h") + $" (orçamento {orcamento}h, {orcamentoComDivida}h endividado)" + deducao + ".");

        if (oportunidades < OportunidadesMinimas) r.problemas.Add($"'{caso.name}': só {oportunidades} oportunidades (mínimo {OportunidadesMinimas}).");
        if (oportunidades > OportunidadesMaximas) r.avisos.Add($"'{caso.name}': {oportunidades} oportunidades (planejado até {OportunidadesMaximas}).");
        if (caminho > orcamentoComDivida) r.problemas.Add($"'{caso.name}': duas pistas Fato custam no mínimo " +
                                                          (caminho == int.MaxValue ? "∞" : caminho + "h") + $", acima de {orcamentoComDivida}h (orçamento com dívida).");
        if (duvidosas == 0) r.avisos.Add($"'{caso.name}': nenhum boato ou calúnia na investigação.");
        if (vazias == 0) r.avisos.Add($"'{caso.name}': nenhuma oportunidade vazia (sem risco de investigar no lugar errado).");
    }

    private static bool TemFala(DialogueData dialogo) => dialogo != null && dialogo.talkScript != null && dialogo.talkScript.Count > 0;

    // Menor soma de custos das fontes que, juntas, entregam todas as pistas do quadro de dedução (poucas fontes: busca
    // por subconjuntos). Também avisa de pistas do caso fora do quadro — uma pista sem contradição seria um indício.
    private static int CustoDoQuadroInteiro(List<Fonte> fontes, CaseData caso, Resultado r)
    {
        var conjunto = new HashSet<string>();
        foreach (Item pista in caso.deducao.Conjunto()) conjunto.Add(pista.itemID);
        foreach (Fonte f in fontes)
            foreach (Item item in f.itens)
                if (item.caso == caso && item.EhPista && !caso.EhAlegacao(item) && !conjunto.Contains(item.itemID))
                    r.avisos.Add($"'{caso.name}': '{item.name}' ({f.nome}) é pista do caso mas não está em nenhum par do quadro.");

        int melhor = int.MaxValue;
        int n = Mathf.Min(fontes.Count, 16);
        for (int mascara = 1; mascara < (1 << n); mascara++)
        {
            int custo = 0;
            var cobertas = new HashSet<string>();
            for (int i = 0; i < n; i++)
            {
                if ((mascara & (1 << i)) == 0) continue;
                custo += fontes[i].custo;
                foreach (Item item in fontes[i].itens) if (conjunto.Contains(item.itemID)) cobertas.Add(item.itemID);
            }
            if (cobertas.Count == conjunto.Count && custo < melhor) melhor = custo;
        }
        return melhor;
    }

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
