using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prompt 4: dedução ativa nas Fases 3 e 4; desde o Prompt 7 (decisão do grupo, 27/09), também na Fase 2.
/// - liga o quadro de dedução nos nove casos das Fases 2, 3 e 4, com dois pares contraditórios cada:
///   fato do cliente × boato e fato do objeto × calúnia (em cada par, exatamente uma afirmação é verdadeira);
/// - reescreve as pistas, as falas das reações e os documentos da biblioteca desses casos para que cada par se
///   contradiga de fato, e para que o cliente aponte quem espalha o boato, e este aponte os objetos. Na Fase 2 os fatos
///   (e as falas dos clientes, do roteiro do grupo) não mudam: mudam o boato, a calúnia, as falas de quem os espalha, a
///   revelação do boato de Réveillon e uma carta de cliente por caso, que passa a indicar quem espalha o boato;
/// - põe o QuadroDeDeducaoUI no prefab UI (objeto "QuadroDeDeducao"), a partir da Fase 2.
/// Os textos novos ficam nas ferramentas de conteúdo (CampanhaSetupTool e BibliotecaSetupTool); aqui só ficam os
/// textos anteriores. Um asset só é trocado se ainda tiver exatamente o texto anterior: o que o grupo editou
/// no Inspector é mantido e listado no relatório. CONTEÚDO PROVISÓRIO. Idempotente.
/// Menu: Ferramentas > Campanha > 5 - Aplicar dedução ativa
/// </summary>
public static class DeducaoSetupTool
{
    private const string PastaCasos = "Assets/ScriptableObjects/Cases";
    private const string PastaItens = "Assets/ScriptableObjects/Items/Campanha";
    private const string PastaDialogos = "Assets/ScriptableObjects/Dialogues/Campanha";
    private const string PastaFase2Antiga = "Assets/ScriptableObjects/Items"; // pistas F1/F2 e receitas da Fase 2 (anteriores ao Prompt 2)
    private const string PrefabUI = "Assets/Prefabs/UI.prefab";

    /// <summary>Fase a partir da qual o quadro de dedução aparece (decisão do grupo, 27/09).</summary>
    public const int FaseMinimaDoQuadro = 2;

    private class CasoDeDeducao
    {
        // Arquivos (sem extensão) em Scriptableobjects/Campanha/<fase>, ou caminho completo começando com "Assets/".
        public string caso, fase, f1, boato, f2, calunia;
        public CasoDeDeducao(string caso, string fase, string f1, string boato, string f2, string calunia)
        { this.caso = caso; this.fase = fase; this.f1 = f1; this.boato = boato; this.f2 = f2; this.calunia = calunia; }
    }

    private static readonly CasoDeDeducao[] Casos =
    {
        new CasoDeDeducao("Caso_Joalheiro", "Fase2", PastaFase2Antiga + "/Item_joalheiro1.asset", "Pista_Joias_Boato", PastaFase2Antiga + "/Item_joalheiro2.asset", "Pista_Joias_Calunia"),
        new CasoDeDeducao("Caso_Reveillon", "Fase2", PastaFase2Antiga + "/Item_Jean1.asset", "Pista_Jean_Boato", PastaFase2Antiga + "/Item_Jean2.asset", "Pista_Jean_Calunia"),
        new CasoDeDeducao("Caso_Operario", "Fase2", PastaFase2Antiga + "/Item_operario1.asset", "Pista_Operario_Boato", PastaFase2Antiga + "/Item_operario2.asset", "Pista_Operario_Calunia"),
        new CasoDeDeducao("Caso_Varennes", "Fase3", "Pista_Varennes_Contrato", "Pista_Varennes_Boato", "Pista_Varennes_Depoimento", "Pista_Varennes_Calunia"),
        new CasoDeDeducao("Caso_ChampDeMars", "Fase3", "Pista_Champ_Peticao", "Pista_Champ_Boato", "Pista_Champ_LeiMarcial", "Pista_Champ_Calunia"),
        new CasoDeDeducao("Caso_Assignats", "Fase3", "Pista_Assignats_Queixa", "Pista_Assignats_Boato", "Pista_Assignats_Tipos", "Pista_Assignats_Calunia"),
        new CasoDeDeducao("Caso_Jornalista", "Fase4", "Pista_Jornalista_Exemplares", "Pista_Jornalista_Boato", "Pista_Jornalista_Certificado", "Pista_Jornalista_Calunia"),
        new CasoDeDeducao("Caso_Negociante", "Fase4", "Pista_Negociante_Relatorio", "Pista_Negociante_Boato", "Pista_Negociante_Edital", "Pista_Negociante_Calunia"),
        new CasoDeDeducao("Caso_Girondina", "Fase4", "Pista_Girondina_Cartas", "Pista_Girondina_Boato", "Pista_Girondina_Retratacao", "Pista_Girondina_Calunia"),
    };

    // Textos em vigor de 25/09 a 27/09 (itemID -> { nome, fonte, descrição }). As pistas que não mudaram não estão aqui.
    private static readonly Dictionary<string, string[]> TextosAnteriores = new Dictionary<string, string[]>
    {
        // Fase 2 (até o Prompt 7): boato e calúnia não contradiziam o fato do mesmo assunto.
        { "pista_joias_boato", new[] { "O segredo da Rainha", "Gazeteiro da esquina", "Segundo o gazeteiro, a Rainha encomendou o colar escondida do Rei e depois negou tudo." } },
        { "pista_joias_calunia", new[] { "A Rainha e o Cardeal", "Cartaz sem assinatura", "Folha afixada no muro: a Rainha e o Cardeal são amantes e dividiram o colar entre si." } },
        { "pista_jean_boato", new[] { "A ordem de atirar", "Conversa de operários", "Segundo os operários, foi o próprio Réveillon quem pediu à Guarda que atirasse na multidão." } },
        { "pista_jean_calunia", new[] { "O dinheiro inglês", "Folha anônima vendida na rua", "Folha vendida pelo gazeteiro: Réveillon recebe dinheiro da Inglaterra para matar Paris de fome." } },
        { "pista_operario_boato", new[] { "Os agitadores do duque", "Jean-Baptiste Réveillon", "Segundo Réveillon, agitadores pagos pelo duque de Orléans distribuíram dinheiro à multidão na véspera." } },
        { "pista_operario_calunia", new[] { "O fogo na fábrica", "Bilhete sem assinatura", "Bilhete deixado na taverna: foi o operário quem ateou fogo à fábrica com as próprias mãos." } },
        // Fases 3 e 4 (até o Prompt 4)
        { "pista_varennes_1", new[] { "A viagem da baronesa", "Cocheiro Joubert", "O cocheiro mostra o contrato em nome de uma 'baronesa de Korff': uma viagem de aluguel comum, sem menção a quem seria levado." } },
        { "pista_varennes_2", new[] { "A parada em Sainte-Menehould", "Ata afixada pelo clube dos Cordeliers", "Ata no mural: o mestre de posta reconheceu o Rei em Sainte-Menehould; o cocheiro não falou com ninguém na estrada." } },
        { "pista_varennes_boato", new[] { "O ouro austríaco", "Conversa no Champ de Mars", "Segundo a peticionária, o cocheiro era espião da Rainha, pago em ouro austríaco." } },
        { "pista_varennes_calunia", new[] { "Os cavalos da Guarda", "Prova de impressão abandonada", "Prova de impressão na caixa de tipos: o cocheiro envenenou os cavalos da Guarda para atrasar a perseguição." } },
        { "pista_champ_1", new[] { "A petição do altar da pátria", "Peticionária Lacombe", "Segundo a peticionária, a folha tem dezenas de assinaturas pacíficas e não havia armas junto ao altar da pátria." } },
        { "pista_champ_2", new[] { "A bandeira vermelha", "Proclamação pregada no balcão da padaria", "Proclamação da prefeitura: a bandeira vermelha da lei marcial foi hasteada antes do fogo." } },
        { "pista_champ_boato", new[] { "O primeiro tiro", "Vizinhas do mercado", "Segundo as vizinhas do mercado, os peticionários atiraram primeiro e a Guarda só se defendeu." } },
        { "pista_champ_calunia", new[] { "Os peticionários pagos", "Cartaz sem assinatura", "Cartaz no mural: os peticionários eram pagos pela Inglaterra para derrubar a Constituição." } },
        { "pista_assignats_boato", new[] { "As notas nas tavernas", "Conversa de cocheiros", "Segundo os cocheiros, o gravador vendia notas falsas nas tavernas, dez por uma moeda de ouro." } },
        { "pista_assignats_calunia", new[] { "Os emigrados de Coblença", "Denúncia anônima na padaria", "Papel deixado no balcão: o gravador trabalha para os emigrados de Coblença." } },
        { "pista_jornalista_boato", new[] { "As cartas com selo inglês", "Carcereiro da Conciergerie", "Segundo o carcereiro, o redator recebe cartas com selo inglês toda semana." } },
        { "pista_jornalista_calunia", new[] { "As armas da redação", "Caixa de denúncias", "Papel na caixa de denúncias: Marchand esconde armas no porão da redação." } },
        { "pista_negociante_1", new[] { "Os armazéns de Garnier", "Comissário Vautrin", "O comissário mostra o relatório oficial: os armazéns de Garnier guardavam mais grãos do que ele declarou." } },
        { "pista_negociante_2", new[] { "Os preços do Máximo", "Parede de editais", "Pela lista de preços afixada, Garnier vendeu acima do preço máximo." } },
        { "pista_negociante_boato", new[] { "O trigo apodrecido", "Carcereiro da Conciergerie", "Segundo o carcereiro, Garnier deixa o trigo apodrecer para matar o povo de fome." } },
        { "pista_negociante_calunia", new[] { "O dinheiro de Pitt", "Arquivo da seção", "Papel no arquivo da seção: Garnier é agente de Pitt e paga os inimigos da República." } },
        { "pista_girondina_1", new[] { "As cartas do deputado", "Cidadã Delorme", "A viúva mostra as cartas do marido: falam de família e de dívidas, sem nenhum plano contra a República." } },
        { "pista_girondina_boato", new[] { "Os deputados foragidos", "Redator Marchand", "Segundo o redator, a viúva esconde deputados foragidos em casa." } },
        { "pista_girondina_calunia", new[] { "O patriota envenenado", "Parede de editais", "Cartaz na parede de editais: a viúva envenenou um patriota da seção." } },
    };

    private class Reacao
    {
        public string fase, npc, caso;
        public string[] anteriores;
        public Reacao(string fase, string npc, string caso, params string[] anteriores) { this.fase = fase; this.npc = npc; this.caso = caso; this.anteriores = anteriores; }
    }

    // Falas anteriores das reações (Assets/ScriptableObjects/Dialogues/Campanha/<fase>/<npc>_<caso>.asset). As novas vêm do CampanhaSetupTool.
    private static readonly Reacao[] Reacoes =
    {
        new Reacao("Fase2", "Gazeteiro", "Caso_Joalheiro", "O colar? Nas tavernas só se fala disso!", "Dizem que a própria Rainha o encomendou escondida do Rei, e depois fingiu não saber de nada."),
        new Reacao("Fase2", "Gazeteiro", "Caso_Reveillon", "Réveillon? Tenho aqui uma folha que vende como pão quente.", "Diz que ele é pago pelos ingleses para matar Paris de fome. Leve, é sua."),
        new Reacao("Fase2", "Operario", "Caso_Reveillon", "Aquele homem? Todo mundo sabe que foi ele quem chamou a Guarda para atirar na gente!"),
        new Reacao("Fase2", "Jean-Baptiste Réveillon", "Caso_Operario", "Aqueles homens não estavam com fome, estavam pagos!", "Agitadores do duque de Orléans distribuíram dinheiro na véspera. Anote isso."),
        new Reacao("Fase3", "CocheiroJoubert", "Caso_Varennes", "Me contrataram em nome de uma tal baronesa de Korff. Eu só conduzia os cavalos, juro.", "Tenho o contrato aqui. Leia o senhor mesmo."),
        new Reacao("Fase3", "PeticionariaLacombe", "Caso_Varennes", "O cocheiro do Rei? Todo mundo sabe que era espião da Rainha, pago em ouro austríaco."),
        new Reacao("Fase3", "PeticionariaLacombe", "Caso_ChampDeMars", "Assinávamos uma petição. Só isso! E eles atiraram.", "Guardei a folha. O sangue ainda está nela."),
        new Reacao("Fase3", "ViuvaFrancois", "Caso_ChampDeMars", "As vizinhas do mercado dizem que os peticionários atiraram primeiro e a Guarda só se defendeu."),
        new Reacao("Fase3", "GravadorMorel", "Caso_Assignats", "Roubaram as minhas chapas, colega! Dei queixa na seção dias antes de me prenderem.", "Aqui está a cópia da queixa, com o carimbo da seção."),
        new Reacao("Fase3", "CocheiroJoubert", "Caso_Assignats", "O gravador? Dizem nas tavernas que ele vendia notas falsas, dez por uma de ouro."),
        new Reacao("Fase4", "RedatorMarchand", "Caso_Jornalista", "Pedi clemência, cidadão. Só isso. Agora querem a minha cabeça.", "Leve os exemplares. Mostre ao tribunal o que eu realmente escrevi."),
        new Reacao("Fase4", "Carcereiro", "Caso_Jornalista", "O redator? Aqui dentro dizem que ele recebe cartas com selo inglês toda semana."),
        new Reacao("Fase4", "ComissarioVautrin", "Caso_Negociante", "O Comitê conta com a sua prensa, cidadão. Garnier rouba o pão do povo.", "Eis o relatório oficial. Use-o bem, e será bem pago."),
        new Reacao("Fase4", "Carcereiro", "Caso_Negociante", "Garnier? Dizem que ele deixa o trigo apodrecer só para matar o povo de fome."),
        new Reacao("Fase4", "CidadaDelorme", "Caso_Girondina", "São só cartas de um marido para a esposa. Leia, por favor.", "Se houver conspiração nelas, eu mesma subo ao cadafalso."),
        new Reacao("Fase4", "RedatorMarchand", "Caso_Girondina", "A viúva Delorme? Dizem que ela esconde deputados foragidos em casa. Eu não afirmaria isso num jornal."),
    };

    // Etapa complementar do Cocheiro (Varennes): a fala antiga citava o texto anterior do depoimento.
    private const string DialogoDaEtapaVarennes = PastaDialogos + "/Fase3/CocheiroJoubert_Caso_Varennes_recibo_da_estalagem.asset";
    private static readonly string[] FalasAnterioresDaEtapaVarennes =
    {
        "O depoimento do mestre de posta! Então o senhor já sabe que eu não falei com ninguém.",
        "Guardei o recibo da estalagem: paguei os cavalos do meu bolso, como qualquer cocheiro. Leve.",
    };

    // Descrições anteriores dos documentos da biblioteca (item de apoio e oferta), por caso.
    private static readonly Dictionary<string, string> DescricoesAnteriores = new Dictionary<string, string>
    {
        { "Caso_Varennes", "Cópia do relatório oficial da Assembleia Nacional sobre a fuga do Rei e as pessoas contratadas para a viagem." },
        { "Caso_ChampDeMars", "Ata oficial da sessão em que se decidiu hastear a bandeira vermelha da lei marcial no Champ de Mars." },
        { "Caso_Assignats", "Laudo dos gravadores da Casa da Moeda comparando as notas falsas com as verdadeiras." },
        { "Caso_Jornalista", "Todos os números do Velho Sans-culotte, encadernados pela Biblioteca Nacional." },
        { "Caso_Negociante", "Livros oficiais de entradas e saídas de grãos da seção, mês a mês." },
        { "Caso_Girondina", "Cartas do deputado Delorme arquivadas na Convenção, com o carimbo do arquivo." },
    };

    // Fase 2 (Prompt 7): descrição anterior das cartas de cliente que passam a indicar quem espalha o boato.
    private static readonly Dictionary<string, string> AlegacoesAnteriores = new Dictionary<string, string>
    {
        { "alegacao_joias_1", "O joalheiro jura que acreditou estar negociando com a Rainha." },
        { "alegacao_jean_1", "Réveillon garante que suas palavras foram distorcidas pelos inimigos." },
        { "alegacao_operario_2", "Ele afirma que a Guarda abriu fogo antes de qualquer pedra ser atirada." },
    };

    // Fase 2 (Prompt 7): a revelação da versão Com Boato de Réveillon falava da "ordem de atirar", o boato antigo.
    private const string ReceitaDeReveillon = PastaFase2Antiga + "/ReceitaDeCaso_Jean.asset";
    private static readonly string[] RevelacoesAnterioresDeReveillon =
    {
        "Descobriu-se que Réveillon nunca chamou a Guarda. O boato impresso desmoronou.",
        "Não se provou que Réveillon mandou a Guarda atirar: ele pediu proteção, não o fogo. O boato impresso desmoronou.",
    };

    [MenuItem("Ferramentas/Campanha/5 - Aplicar dedução ativa")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[DeducaoSetup] Aplicar dedução ativa\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        int casos = 0, textos = 0, falas = 0, documentos = 0;
        var mantidos = new List<string>();

        foreach (CasoDeDeducao def in Casos)
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>($"{PastaCasos}/{def.caso}.asset");
            if (caso == null) { r.AppendLine($"  AVISO: caso '{def.caso}' não existe (rode antes a ferramenta 1)."); continue; }

            Item f1 = Pista(def, def.f1), boato = Pista(def, def.boato), f2 = Pista(def, def.f2), calunia = Pista(def, def.calunia);
            foreach (Item pista in new[] { f1, boato, f2, calunia })
                if (pista != null && MigrarTexto(pista, mantidos)) textos++;

            if (caso.deducao == null) caso.deducao = new ConfiguracaoDeDeducao();
            if (caso.deducao.Vazia && f1 != null && boato != null && f2 != null && calunia != null)
            {
                caso.deducao.ativa = true;
                caso.deducao.pares = new List<ParContraditorio>
                {
                    new ParContraditorio { a = f1, b = boato },
                    new ParContraditorio { a = f2, b = calunia },
                };
                EditorUtility.SetDirty(caso);
                casos++;
            }

            string sufixo = def.caso.Replace("Caso_", "");
            string nova = BibliotecaSetupTool.DescricaoDaOferta(def.caso);
            if (DescricoesAnteriores.TryGetValue(def.caso, out string anterior) && nova != null)
            {
                Item apoio = AssetDatabase.LoadAssetAtPath<Item>($"{PastaItens}/{def.fase}/Apoio_{sufixo}_Biblioteca.asset");
                if (apoio != null) documentos += TrocarDescricao(apoio, anterior, nova, mantidos) ? 1 : 0;
                OfertaDaBiblioteca oferta = AssetDatabase.LoadAssetAtPath<OfertaDaBiblioteca>($"{PastaItens}/Biblioteca/Oferta_{sufixo}.asset");
                if (oferta != null)
                {
                    if (oferta.descricao == anterior) { oferta.descricao = nova; EditorUtility.SetDirty(oferta); documentos++; }
                    else if (oferta.descricao != nova) mantidos.Add(oferta.name + " (descrição)");
                }
            }
        }

        foreach (Reacao reacao in Reacoes)
        {
            string caminho = $"{PastaDialogos}/{reacao.fase}/{reacao.npc}_{reacao.caso}.asset";
            if (MigrarFalas(caminho, reacao.anteriores, CampanhaSetupTool.FalasDeReferencia(reacao.npc, reacao.caso), mantidos)) falas++;
        }
        if (MigrarFalas(DialogoDaEtapaVarennes, FalasAnterioresDaEtapaVarennes, BibliotecaSetupTool.FalasDaEtapa("recibo_da_estalagem"), mantidos)) falas++;

        // Fase 2: cartas de cliente com a pista de quem espalha o boato, e a revelação do boato de Réveillon.
        int cartas = 0, revelacoes = 0;
        foreach (var par in AlegacoesAnteriores)
        {
            Item alegacao = AlegacaoDaFase2(par.Key);
            string nova = CampanhaSetupTool.AlegacaoComPista(par.Key);
            if (alegacao != null && nova != null && TrocarDescricao(alegacao, par.Value, nova, mantidos)) cartas++;
        }
        ReceitaDeCaso receitaReveillon = AssetDatabase.LoadAssetAtPath<ReceitaDeCaso>(ReceitaDeReveillon);
        if (receitaReveillon != null && receitaReveillon.comBoato != null &&
            receitaReveillon.comBoato.textoRevelacao != CampanhaSetupTool.RevelacaoDoBoatoDeReveillon)
        {
            if (System.Array.IndexOf(RevelacoesAnterioresDeReveillon, receitaReveillon.comBoato.textoRevelacao) >= 0)
            {
                receitaReveillon.comBoato.textoRevelacao = CampanhaSetupTool.RevelacaoDoBoatoDeReveillon;
                EditorUtility.SetDirty(receitaReveillon);
                revelacoes++;
            }
            else mantidos.Add(receitaReveillon.name + " (revelação Com Boato)");
        }

        AssetDatabase.SaveAssets();
        int prefab = RegistrarNoPrefabUI();

        r.AppendLine($"  Casos com dedução ligada agora: {casos}; pistas reescritas: {textos}; falas reescritas: {falas}; " +
                     $"cartas de cliente: {cartas}; revelações: {revelacoes}; documentos da biblioteca: {documentos}; " +
                     $"alterações no UI.prefab: {prefab}.");
        if (mantidos.Count > 0) r.AppendLine("  Mantidos (editados no Inspector, sem o texto anterior): " + string.Join(", ", mantidos) + ".");
        return r.ToString();
    }

    private static Item Pista(CasoDeDeducao def, string arquivo) =>
        AssetDatabase.LoadAssetAtPath<Item>(arquivo.StartsWith("Assets/") ? arquivo : $"{PastaItens}/{def.fase}/{arquivo}.asset");

    private static Item AlegacaoDaFase2(string itemID)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Item", new[] { $"{PastaItens}/Fase2" }))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && item.itemID == itemID) return item;
        }
        return null;
    }

    // Troca nome, fonte e descrição pelos de referência só se o asset ainda tem exatamente os três textos anteriores.
    private static bool MigrarTexto(Item pista, List<string> mantidos)
    {
        string[] novo = CampanhaSetupTool.TextoDeReferencia(pista.itemID);
        if (novo == null || !TextosAnteriores.TryGetValue(pista.itemID, out string[] antes)) return false;
        if (pista.itemName == novo[0] && pista.fonte == novo[1] && pista.descricao == novo[2]) return false;
        if (pista.itemName != antes[0] || pista.fonte != antes[1] || pista.descricao != antes[2]) { mantidos.Add(pista.name); return false; }
        pista.itemName = novo[0]; pista.fonte = novo[1]; pista.descricao = novo[2];
        EditorUtility.SetDirty(pista);
        return true;
    }

    private static bool TrocarDescricao(Item item, string anterior, string nova, List<string> mantidos)
    {
        if (item.descricao == nova) return false;
        if (item.descricao != anterior) { mantidos.Add(item.name + " (descrição)"); return false; }
        item.descricao = nova;
        EditorUtility.SetDirty(item);
        return true;
    }

    // Troca as falas de um diálogo só se ele ainda tem exatamente as falas anteriores (o falante de cada linha é mantido).
    private static bool MigrarFalas(string caminho, string[] anteriores, string[] novas, List<string> mantidos)
    {
        DialogueData dialogo = AssetDatabase.LoadAssetAtPath<DialogueData>(caminho);
        if (dialogo == null || dialogo.talkScript == null || novas == null) return false;
        if (MesmasFalas(dialogo, novas)) return false;
        if (!MesmasFalas(dialogo, anteriores)) { mantidos.Add(dialogo.name); return false; }

        string falante = dialogo.talkScript.Count > 0 ? dialogo.talkScript[0].name : string.Empty;
        dialogo.talkScript = new List<Dialogue>();
        foreach (string fala in novas) dialogo.talkScript.Add(new Dialogue { name = falante, text = fala, choices = new List<Choice>() });
        EditorUtility.SetDirty(dialogo);
        return true;
    }

    private static bool MesmasFalas(DialogueData dialogo, string[] falas)
    {
        if (dialogo.talkScript.Count != falas.Length) return false;
        for (int i = 0; i < falas.Length; i++)
            if (dialogo.talkScript[i].text != falas[i] || (dialogo.talkScript[i].choices != null && dialogo.talkScript[i].choices.Count > 0)) return false;
        return true;
    }

    private static int RegistrarNoPrefabUI()
    {
        int alterados = 0;
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            QuadroDeDeducaoUI quadro = raiz.GetComponentInChildren<QuadroDeDeducaoUI>(true);
            if (quadro == null)
            {
                var go = new GameObject("QuadroDeDeducao", typeof(RectTransform));
                go.layer = raiz.layer;
                go.transform.SetParent(raiz.transform, false);
                quadro = go.AddComponent<QuadroDeDeducaoUI>();
                alterados++;
            }
            if (quadro.fonte == null)
            {
                BibliotecaUI biblioteca = raiz.GetComponentInChildren<BibliotecaUI>(true);
                TMPro.TMP_FontAsset fonte = biblioteca != null && biblioteca.fonte != null ? biblioteca.fonte
                    : raiz.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)?.font;
                if (fonte != null) { quadro.fonte = fonte; alterados++; }
            }
            // Prompt 7: o quadro vale desde a Fase 2. Só troca o valor antigo (3); outro valor escolhido no Inspector fica.
            if (quadro.faseMinima == 3)
            {
                quadro.faseMinima = FaseMinimaDoQuadro;
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
}
