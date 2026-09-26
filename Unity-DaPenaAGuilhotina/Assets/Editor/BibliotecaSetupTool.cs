using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prompt 3: biblioteca e documentos de apoio sobre o conteúdo do Prompt 2.
/// - um documento de qualidade superior à venda por caso das Fases 3 e 4 (OfertaDaBiblioteca + Item de suporte);
/// - um qualificador explícito na receita de cada um desses casos (não é multiplicador universal);
/// - exemplo verificável no Champ de Mars: Fatos (+20 Povo, −10 Estado, +30) → com apoio (+40, −5, +50);
/// - duas etapas complementares de NPC (Varennes e Girondina) que entregam um documento de apoio alternativo;
/// - BibliotecaUI no UI.prefab e SuportesDaPrensaUI no painel da prensa.
/// CONTEÚDO PROVISÓRIO (textos, preços e bônus). Idempotente: não sobrescreve o que já foi preenchido/editado.
/// Menu: Ferramentas > Campanha > 3 - Aplicar biblioteca e documentos de apoio
/// </summary>
public static class BibliotecaSetupTool
{
    private const string PastaItens = "Assets/Scriptableobjects/Campanha";
    private const string PastaOfertas = "Assets/Scriptableobjects/Campanha/Biblioteca";
    private const string PastaDialogos = "Assets/Dialogos/Campanha";
    private const string PrefabUI = "Assets/Prefab/UI.prefab";
    private const string SpriteDocumento = "Assets/Sprites/paper-scroll-banner-3.png";

    private class Oferta
    {
        public string caso, fase, id, titulo, descricao, itemNome, fonte;
        public int preco, povo, estado, ouro;
    }

    private class Etapa
    {
        public string cena, npc, caso, fase, id, requisito, itemArquivo, itemId, itemNome, itemDescricao, itemFonte, falante;
        public string[] falas;
    }

    private static readonly Oferta[] Ofertas =
    {
        new Oferta { caso = "Caso_Padeiro", fase = "Fase3", id = "biblioteca_padeiro", preco = 20, povo = 5, estado = 10, ouro = 15,
            titulo = "Registro da Halle aux Blés", itemNome = "Registro da Halle aux Blés", fonte = "Biblioteca (compra)",
            descricao = "Livro oficial do mercado de trigo: quanta farinha o padeiro François comprou no mês da fome." },
        new Oferta { caso = "Caso_Varennes", fase = "Fase3", id = "biblioteca_varennes", preco = 25, povo = 0, estado = 10, ouro = 20,
            titulo = "Relatório da Assembleia sobre a fuga", itemNome = "Relatório da Assembleia sobre a fuga", fonte = "Biblioteca (compra)",
            descricao = "Cópia do relatório oficial da Assembleia Nacional sobre a fuga do Rei e as pessoas contratadas para a viagem." },
        new Oferta { caso = "Caso_ChampDeMars", fase = "Fase3", id = "biblioteca_champ", preco = 20, povo = 20, estado = 5, ouro = 20,
            titulo = "Ata da prefeitura de Paris", itemNome = "Ata da prefeitura de Paris", fonte = "Biblioteca (compra)",
            descricao = "Ata oficial da sessão em que se decidiu hastear a bandeira vermelha da lei marcial no Champ de Mars." },
        new Oferta { caso = "Caso_Assignats", fase = "Fase3", id = "biblioteca_assignats", preco = 20, povo = 5, estado = 10, ouro = 15,
            titulo = "Laudo da Casa da Moeda", itemNome = "Laudo da Casa da Moeda", fonte = "Biblioteca (compra)",
            descricao = "Laudo dos gravadores da Casa da Moeda comparando as notas falsas com as verdadeiras." },
        new Oferta { caso = "Caso_Jornalista", fase = "Fase4", id = "biblioteca_jornalista", preco = 25, povo = 10, estado = 0, ouro = 10,
            titulo = "Coleção encadernada do jornal", itemNome = "Coleção encadernada do jornal", fonte = "Biblioteca (compra)",
            descricao = "Todos os números do Velho Sans-culotte, encadernados pela Biblioteca Nacional." },
        new Oferta { caso = "Caso_Negociante", fase = "Fase4", id = "biblioteca_negociante", preco = 30, povo = 0, estado = 10, ouro = 25,
            titulo = "Livros do Comitê de Subsistência", itemNome = "Livros do Comitê de Subsistência", fonte = "Biblioteca (compra)",
            descricao = "Livros oficiais de entradas e saídas de grãos da seção, mês a mês." },
        new Oferta { caso = "Caso_Girondina", fase = "Fase4", id = "biblioteca_girondina", preco = 25, povo = 5, estado = 5, ouro = 15,
            titulo = "Correspondência arquivada na Convenção", itemNome = "Correspondência arquivada na Convenção", fonte = "Biblioteca (compra)",
            descricao = "Cartas do deputado Delorme arquivadas na Convenção, com o carimbo do arquivo." },
    };

    private static readonly Etapa[] Etapas =
    {
        new Etapa { cena = "Fase3", npc = "CocheiroJoubert", caso = "Caso_Varennes", fase = "Fase3", id = "recibo_da_estalagem",
            requisito = "Pista_Varennes_Depoimento", itemArquivo = "Apoio_Varennes_Recibo", itemId = "apoio_varennes_recibo",
            itemNome = "Recibo da estalagem de Sainte-Menehould", itemFonte = "Cocheiro Joubert",
            itemDescricao = "Recibo da troca de cavalos pago pelo próprio cocheiro, como em qualquer viagem de aluguel.",
            falante = "Cocheiro Joubert",
            falas = new[] { "O depoimento do mestre de posta! Então o senhor já sabe que eu não falei com ninguém.", "Guardei o recibo da estalagem: paguei os cavalos do meu bolso, como qualquer cocheiro. Leve." } },
        new Etapa { cena = "Fase4", npc = "CidadaDelorme", caso = "Caso_Girondina", fase = "Fase4", id = "carta_da_secao",
            requisito = "Pista_Girondina_Retratacao", itemArquivo = "Apoio_Girondina_Recomendacao", itemId = "apoio_girondina_recomendacao",
            itemNome = "Carta de recomendação da seção", itemFonte = "Cidadã Delorme",
            itemDescricao = "A seção atesta que a cidadã Delorme doou roupas e pão aos voluntários de 1792.",
            falante = "Cidadã Delorme",
            falas = new[] { "Ele retirou a denúncia? Graças a Deus.", "Então tome também isto: a seção me deu esta carta quando doei roupas aos voluntários." } },
    };

    [MenuItem("Ferramentas/Campanha/3 - Aplicar biblioteca e documentos de apoio")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[BibliotecaSetup] Aplicar biblioteca e documentos de apoio\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        int criados = 0, alterados = 0;
        Sprite documento = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDocumento);

        MigrarExemploDoChampDeMars(ref alterados);

        // Documentos das etapas complementares (criados antes para entrarem nos qualificadores).
        var docsDeNpc = new Dictionary<string, string>(); // caso -> caminho do documento
        foreach (Etapa e in Etapas)
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>($"Assets/Casos/{e.caso}.asset");
            string caminho = $"{PastaItens}/{e.fase}/{e.itemArquivo}.asset";
            GarantirSuporte(caminho, e.itemId, e.itemNome, e.itemDescricao, e.itemFonte, caso, documento, ref criados, ref alterados);
            docsDeNpc[e.caso] = caminho;
        }

        var caminhosDasOfertas = new List<string>();
        foreach (Oferta o in Ofertas)
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>($"Assets/Casos/{o.caso}.asset");
            if (caso == null) { r.AppendLine($"  AVISO: caso '{o.caso}' não existe (rode antes a ferramenta 1)."); continue; }
            string caminhoItem = $"{PastaItens}/{o.fase}/Apoio_{o.caso.Replace("Caso_", "")}_Biblioteca.asset";
            Item item = GarantirSuporte(caminhoItem, "apoio_" + o.id, o.itemNome, o.descricao, o.fonte, caso, documento, ref criados, ref alterados);

            string caminhoOferta = $"{PastaOfertas}/Oferta_{o.caso.Replace("Caso_", "")}.asset";
            OfertaDaBiblioteca oferta = CarregarOuCriar<OfertaDaBiblioteca>(caminhoOferta, ref criados, out bool nova);
            if (nova)
            {
                oferta.id = o.id; oferta.titulo = o.titulo; oferta.descricao = o.descricao; oferta.preco = o.preco;
                oferta.faseMinima = 3; oferta.caso = caso; oferta.item = item;
                EditorUtility.SetDirty(oferta);
            }
            caminhosDasOfertas.Add(caminhoOferta);

            // Qualificador explícito da receita do caso (aceita o documento comprado e, se houver, o do NPC).
            ReceitaDeCaso receita = caso.receitaDoPanfleto;
            if (receita.qualificadores == null) receita.qualificadores = new List<ReceitaDeCaso.Qualificador>();
            string idQ = "apoio_" + o.caso.Replace("Caso_", "").ToLowerInvariant();
            ReceitaDeCaso.Qualificador q = receita.qualificadores.Find(x => x != null && x.id == idQ);
            if (q == null)
            {
                q = new ReceitaDeCaso.Qualificador
                {
                    id = idQ, qualidadeMinima = QualidadeDaEvidencia.Superior, povo = o.povo, estado = o.estado, ouro = o.ouro,
                    descricao = "Documento oficial de qualidade superior reforça o panfleto (valor provisório).",
                    suportesAceitos = new List<Item> { item },
                };
                receita.qualificadores.Add(q);
                alterados++;
            }
            if (docsDeNpc.TryGetValue(o.caso, out string caminhoDoNpc))
            {
                Item doNpc = AssetDatabase.LoadAssetAtPath<Item>(caminhoDoNpc);
                if (!q.suportesAceitos.Contains(doNpc)) { q.suportesAceitos.Add(doNpc); alterados++; }
            }
            EditorUtility.SetDirty(receita);
        }
        AssetDatabase.SaveAssets();

        // Etapas complementares nos NPCs das cenas (guardando caminhos: abrir cena descarrega assets não usados).
        var falas = new Dictionary<string, string>();
        foreach (Etapa e in Etapas)
        {
            string caminho = $"{PastaDialogos}/{e.fase}/{e.npc}_{e.caso}_{e.id}.asset";
            DialogueData d = CarregarOuCriar<DialogueData>(caminho, ref criados, out bool novo);
            if (novo || d.talkScript == null || d.talkScript.Count == 0)
            {
                d.talkScript = new List<Dialogue>();
                foreach (string fala in e.falas) d.talkScript.Add(new Dialogue { name = e.falante, text = fala, choices = new List<Choice>() });
                EditorUtility.SetDirty(d);
            }
            falas[e.id] = caminho;
        }
        AssetDatabase.SaveAssets();

        SceneSetup[] configuracao = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string nomeDaCena in new[] { "Fase3", "Fase4" })
            {
                Scene cena = EditorSceneManager.OpenScene($"Assets/Scenes/{nomeDaCena}.unity", OpenSceneMode.Single);
                bool mudou = false;
                foreach (Etapa e in System.Array.FindAll(Etapas, x => x.cena == nomeDaCena))
                    mudou |= GarantirEtapa(cena, e, falas[e.id], docsDeNpc[e.caso], ref alterados, r);
                if (mudou) { EditorSceneManager.MarkSceneDirty(cena); EditorSceneManager.SaveScene(cena); r.AppendLine($"  {nomeDaCena}: etapas complementares salvas."); }
            }
        }
        finally
        {
            if (configuracao != null && configuracao.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(configuracao);
        }

        alterados += RegistrarNoPrefabUI(caminhosDasOfertas);
        CatalogoDeSaveEditor.Atualizar();
        AssetDatabase.SaveAssets();
        return r.Append($"Assets criados: {criados}; alterações: {alterados}.").ToString();
    }

    // Exemplo verificável pedido pelo Prompt 3. Só troca os valores provisórios do Prompt 2 se ninguém os editou.
    private static void MigrarExemploDoChampDeMars(ref int alterados)
    {
        CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_ChampDeMars.asset");
        if (caso == null || caso.receitaDoPanfleto == null) return;
        ReceitaDeCaso r = caso.receitaDoPanfleto;
        bool Trocar(ReceitaDeCaso.Versao v, int p0, int e0, int o0, int p1, int e1, int o1)
        {
            if (v.povo != p0 || v.estado != e0 || v.ouro != o0) return false;
            v.povo = p1; v.estado = e1; v.ouro = o1;
            return true;
        }
        int antes = alterados;
        if (Trocar(r.soFatos, 35, -25, 20, 20, -10, 30)) alterados++;
        if (Trocar(r.comBoato, 45, -35, 30, 30, -15, 40)) alterados++;
        if (Trocar(r.calunia, 55, -45, 40, 40, -20, 55)) alterados++;
        if (Trocar(r.soAlegacoes, 12, -8, 8, 8, -4, 10)) alterados++;
        if (caso.publicOpinionReward == 30 && caso.stateOpinionReward == -20 && caso.moneyReward == 20)
        {
            caso.publicOpinionReward = 20; caso.stateOpinionReward = -10; caso.moneyReward = 30;
            alterados++;
            EditorUtility.SetDirty(caso);
        }
        if (alterados != antes) EditorUtility.SetDirty(r);
    }

    private static bool GarantirEtapa(Scene cena, Etapa e, string caminhoFala, string caminhoDoc, ref int alterados, StringBuilder r)
    {
        NPCMovement npc = null;
        foreach (var par in IdDeInteracao.InteracoesDaCena(cena))
            if (par.Key is NPCMovement n && IdDeInteracao.IdEfetivo(n, n.idDaInteracao) == e.npc) npc = n;
        if (npc == null) { r.AppendLine($"  AVISO: NPC '{e.npc}' não encontrado em {cena.name}."); return false; }

        CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>($"Assets/Casos/{e.caso}.asset");
        int i = npc.reacoesDeCaso.FindIndex(x => x.caso == caso);
        if (i < 0) { r.AppendLine($"  AVISO: '{e.npc}' não tem reação ao caso '{e.caso}'."); return false; }

        CasoReacao reacao = npc.reacoesDeCaso[i];
        if (reacao.etapasComplementares == null) reacao.etapasComplementares = new List<EtapaComplementar>();
        if (reacao.etapasComplementares.Exists(x => x.id == e.id)) return false;

        reacao.etapasComplementares.Add(new EtapaComplementar
        {
            id = e.id,
            requisitos = new List<Item> { AssetDatabase.LoadAssetAtPath<Item>($"{PastaItens}/{e.fase}/{e.requisito}.asset") },
            dialogo = AssetDatabase.LoadAssetAtPath<DialogueData>(caminhoFala),
            recompensas = new List<Item> { AssetDatabase.LoadAssetAtPath<Item>(caminhoDoc) },
        });
        npc.reacoesDeCaso[i] = reacao;
        EditorUtility.SetDirty(npc);
        PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
        alterados++;
        return true;
    }

    private static int RegistrarNoPrefabUI(List<string> caminhosDasOfertas)
    {
        int alterados = 0;
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            BibliotecaUI biblioteca = raiz.GetComponentInChildren<BibliotecaUI>(true);
            if (biblioteca == null)
            {
                var go = new GameObject("Biblioteca", typeof(RectTransform));
                go.layer = raiz.layer;
                go.transform.SetParent(raiz.transform, false);
                biblioteca = go.AddComponent<BibliotecaUI>();
                alterados++;
            }
            if (biblioteca.fonte == null)
            {
                var modelo = raiz.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (modelo != null) { biblioteca.fonte = modelo.font; alterados++; }
            }
            foreach (string caminho in caminhosDasOfertas)
            {
                var oferta = AssetDatabase.LoadAssetAtPath<OfertaDaBiblioteca>(caminho);
                if (oferta != null && !biblioteca.ofertas.Contains(oferta)) { biblioteca.ofertas.Add(oferta); alterados++; }
            }

            CraftingPress prensa = raiz.GetComponentInChildren<CraftingPress>(true);
            if (prensa != null && prensa.GetComponent<SuportesDaPrensaUI>() == null)
            {
                prensa.gameObject.AddComponent<SuportesDaPrensaUI>();
                alterados++;
            }
            if (alterados > 0) PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        return alterados;
    }

    private static Item GarantirSuporte(string caminho, string id, string nome, string descricao, string fonte, CaseData caso,
        Sprite imagem, ref int criados, ref int alterados)
    {
        Item item = CarregarOuCriar<Item>(caminho, ref criados, out bool novo);
        if (novo)
        {
            item.itemID = id; item.itemName = nome; item.descricao = descricao; item.fonte = fonte; item.itemImg = imagem;
            item.itemAmt = 1; item.caso = caso; item.confiabilidade = Confiabilidade.NaoEPista;
            item.documentoDeSuporte = true; item.qualidade = QualidadeDaEvidencia.Superior;
            EditorUtility.SetDirty(item);
        }
        return item;
    }

    private static T CarregarOuCriar<T>(string caminho, ref int criados, out bool novo) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(caminho);
        novo = asset == null;
        if (!novo) return asset;
        GarantirPasta(Path.GetDirectoryName(caminho).Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, caminho);
        criados++;
        return asset;
    }

    private static void GarantirPasta(string pasta)
    {
        if (AssetDatabase.IsValidFolder(pasta)) return;
        string pai = Path.GetDirectoryName(pasta).Replace('\\', '/');
        GarantirPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(pasta));
    }
}
