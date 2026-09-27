using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prompt 2: cadastra o conteúdo da campanha (Fases 2, 3 e 4) sobre os sistemas existentes — casos, pistas,
/// alegações iniciais, panfletos, receitas, diálogos, NPCs e objetos investigáveis nas cenas — e registra tudo na
/// Mesa de Casos (UI.prefab) e no CatalogoDeSave.
///
/// CONTEÚDO PROVISÓRIO: textos e valores novos foram escritos para tornar a campanha jogável na ausência do GDD.
/// Não são canônicos; edite-os à vontade nos assets gerados.
///
/// Idempotente e conservador: assets são achados pelo caminho e cenas pelo "Id Da Interacao". Rodar de novo não
/// duplica nada e NÃO sobrescreve textos/valores já preenchidos (edições feitas no Inspector são preservadas):
/// só preenche o que estiver vazio e aplica, uma vez, as migrações dos NPCs da Fase 2.
///
/// Menu: Ferramentas > Campanha > 1 - Aplicar conteúdo da campanha (Fases 2 a 4)
/// </summary>
public static class CampanhaSetupTool
{
    private const string PastaItens = "Assets/Scriptableobjects/Campanha";
    private const string PastaDialogos = "Assets/Dialogos/Campanha";
    private const string PastaCasos = "Assets/Casos";
    private const string PrefabNpc = "Assets/Prefab/NPCBasic.prefab";
    private const string PrefabUI = "Assets/Prefab/UI.prefab";

    private const string SpriteDocumento = "Assets/Sprites/paper-scroll-banner-3.png";
    private const string SpriteCarta = "Assets/Sprites/MenuDetails/vibrant-red-wax-seal-with-a-textured-surface-and-glossy-shine-cut-out-transparent-png.png";
    private const string SpritePanfleto = "Assets/Sprites/MenuDetails/pngtree-aged-parchment-with-faded-script-rests-on-wood-image_16688678.jpg";
    private const string SpriteMesa = "Assets/Sprites/objetos/mesa.png";

    // ===== Definições =====

    private class P // pista, alegação ou panfleto
    {
        public string caminho, id, nome, descricao, fonte;
        public Confiabilidade conf;
        public P(string caminho, string id, string nome, Confiabilidade conf, string fonte, string descricao)
        { this.caminho = caminho; this.id = id; this.nome = nome; this.conf = conf; this.fonte = fonte; this.descricao = descricao; }
    }

    private class V // versão da receita
    {
        public int povo, estado, ouro, penPovo, penEstado;
        public string revelacao;
        public V(int povo, int estado, int ouro, int penPovo = 0, int penEstado = 0, string revelacao = null)
        { this.povo = povo; this.estado = estado; this.ouro = ouro; this.penPovo = penPovo; this.penEstado = penEstado; this.revelacao = revelacao; }
    }

    private class C // caso
    {
        public string caminho, titulo, descricao, objetivo, cena, receita;
        public int fase, ouro, povo, estado;
        public RotaFinal rota;
        public P f1, f2, boato, calunia, panfleto;
        public P[] alegacoes;
        public V fatos, comBoato, comCalunia, soAlegacoes;
        public CaseData asset;
    }

    private class N // NPC
    {
        public string cena, id, nome, falante;
        public float x;
        public Color cor;
        public string[] padrao;
        public bool existente;
    }

    private class R // reação de NPC a um caso
    {
        public string npc, caso;
        public string[] falas;
        public string item; // caminho da pista entregue (vazio = só conversa)
    }

    private class L // objeto investigável
    {
        public string cena, id, nome, sprite;
        public float x, y, escala;
        public Dictionary<string, string> porCaso = new Dictionary<string, string>(); // caso -> pista
    }

    private static string Item(string fase, string arquivo) => $"{PastaItens}/{fase}/{arquivo}.asset";
    private static string Caso(string arquivo) => $"{PastaCasos}/{arquivo}.asset";

    private static readonly List<C> Casos = new List<C>();
    private static readonly List<N> Npcs = new List<N>();
    private static readonly List<R> Reacoes = new List<R>();
    private static readonly List<L> Loots = new List<L>();

    private static void Definir()
    {
        Casos.Clear(); Npcs.Clear(); Reacoes.Clear(); Loots.Clear();
        DefinirFase2();
        DefinirFase3();
        DefinirFase4();
    }

    // ----- Textos revisados na verificação (25/09) -----
    // Os nomes do Prompt 2 seguiam um padrão que revelava a verdade ("Conversa sobre…" = boato; "Libelo/Folha/Cartaz" =
    // calúnia; documentos = fato). Agora o nome é só o ASSUNTO; o que vem de pessoas é relato ("Segundo…" ou "X mostra…")
    // e o que vem de objetos é documento, qualquer que seja a verdade. A confiabilidade se deduz pelo conteúdo e pela fonte.
    // As definições abaixo guardam os textos ORIGINAIS: um asset que ainda tiver o nome original (nunca editado) recebe
    // estes; um asset editado no Inspector não é tocado. itemID -> { nome, fonte, descrição }.
    private static readonly Dictionary<string, string[]> TextosRevisados = new Dictionary<string, string[]>
    {
        // Fase 2 — dedução ativa desde o Prompt 7 (27/09): como nas Fases 3 e 4, fato do cliente × boato e fato do
        // objeto × calúnia, e em cada par só uma afirmação é verdadeira. Os fatos ficaram como estavam (os das Joias são do
        // roteiro do grupo); o boato e a calúnia de cada caso foram reescritos para contradizer o fato do mesmo assunto.
        // Os textos anteriores estão em DeducaoSetupTool, que migra os assets que ainda os tinham.
        // Fase 2 — Joias
        // Roteiro "O colar da rainha" (27/09): as duas pistas vêm do joalheiro; a 2ª deixou de ser achada na taverna.
        { "pista_joias_1", new[] { "Registro de compra", "Livro de vendas do joalheiro.", "Cópia da venda do colar em nome da rainha, negociada por um cardeal e uma intermediária. O joalheiro afirma que a rainha não compareceu." } },
        { "pista_joias_2", new[] { "Assinatura da encomenda", "Cópia da encomenda fornecida pelo joalheiro.", "A encomenda traz ‘Marie Antoinette de France’, enquanto os documentos oficiais citados na investigação usam ‘Marie Antoinette’. A diferença põe a autorização em dúvida." } },
        { "pista_joias_boato", new[] { "O segredo da Rainha", "Gazeteiro da esquina", "Segundo o gazeteiro, a própria Rainha compareceu à loja, disfarçada, para escolher o colar escondida do Rei, e depois negou tudo." } },
        { "pista_joias_calunia", new[] { "A Rainha e o Cardeal", "Cartaz sem assinatura", "Folha afixada no muro: a Rainha assinou a encomenda de próprio punho, como assina tudo, para dar o colar ao Cardeal, seu amante." } },
        // Fase 2 — Réveillon
        { "pista_jean_1", new[] { "O discurso na assembleia", "Ata da assembleia eleitoral", "Ata da assembleia do distrito: Réveillon propôs baixar os salários e o preço do pão ao mesmo tempo; não disse que o povo devia passar fome." } },
        { "pista_jean_2", new[] { "Os salários da manufatura", "Gazeta vendida na banca", "Cópia do livro de salários publicada por uma gazeta: Réveillon pagava acima da média do bairro e manteve os salários no inverno." } },
        { "pista_jean_boato", new[] { "Os quinze soldos", "Conversa de operários", "Segundo os operários, Réveillon disse na assembleia que um operário vive muito bem com quinze soldos por dia e pediu só o corte dos salários, sem uma palavra sobre o pão." } },
        { "pista_jean_calunia", new[] { "O dinheiro inglês", "Folha anônima vendida na rua", "Folha vendida pelo gazeteiro: no inverno, Réveillon cortou pela metade o salário da manufatura e mandou a diferença para a Inglaterra, que lhe paga para matar Paris de fome." } },
        // Fase 2 — Operário
        { "pista_operario_1", new[] { "O relato do sobrevivente", "O próprio operário", "Segundo o operário, a Guarda Francesa atirou contra a multidão desarmada que pedia pão e salário." } },
        { "pista_operario_2", new[] { "A ordem do comandante", "Cartaz arrancado de um muro", "Cartaz com a ordem do comandante: 'dispersar a multidão por todos os meios', afixado antes de qualquer violência." } },
        { "pista_operario_boato", new[] { "Os agitadores do duque", "Jean-Baptiste Réveillon", "Segundo Réveillon, ninguém ali pedia pão: a multidão veio armada, paga na véspera por agitadores do duque de Orléans, e a Guarda só atirou para se defender." } },
        { "pista_operario_calunia", new[] { "O fogo na fábrica", "Bilhete sem assinatura", "Bilhete deixado na taverna: a Guarda só recebeu ordem de agir depois que o operário ateou fogo à fábrica com as próprias mãos." } },
        // Fases 3 e 4 — dedução ativa (Prompt 4, 27/09): cada caso tem dois pares de afirmações contraditórias, e
        // em cada par só uma é verdadeira. Fato do cliente × boato; fato do objeto × calúnia. Os textos de antes
        // (25/09) estão em DeducaoSetupTool, que migra os assets que ainda os tinham.
        // Fase 3 — Varennes
        { "pista_varennes_1", new[] { "A viagem da baronesa", "Cocheiro Joubert", "O cocheiro mostra o contrato em nome de uma 'baronesa de Korff': sessenta libras em moeda francesa por uma viagem de aluguel, sem dizer quem iria na berlinda." } },
        { "pista_varennes_2", new[] { "A parada em Sainte-Menehould", "Ata afixada pelo clube dos Cordeliers", "Ata no mural: em Sainte-Menehould, o mestre de posta Drouet reconheceu o Rei sozinho e galopou até Varennes num cavalo descansado da própria posta." } },
        { "pista_varennes_boato", new[] { "O ouro austríaco", "Conversa no Champ de Mars", "Segundo a peticionária, o cocheiro recebeu ouro austríaco para levar o Rei e sabia desde Paris quem ia na berlinda." } },
        { "pista_varennes_calunia", new[] { "Os cavalos da posta", "Prova de impressão abandonada", "Prova de impressão na caixa de tipos: o cocheiro envenenou todos os cavalos da posta de Sainte-Menehould para que ninguém alcançasse a berlinda." } },
        // Fase 3 — Champ de Mars
        { "pista_champ_1", new[] { "A petição do altar da pátria", "Peticionária Lacombe", "Segundo a peticionária, quem assinava estava desarmado: a folha tem dezenas de assinaturas, e nenhum tiro partiu do altar da pátria." } },
        { "pista_champ_2", new[] { "A bandeira vermelha", "Proclamação pregada no balcão da padaria", "Proclamação da prefeitura: a bandeira vermelha da lei marcial foi hasteada por ordem do prefeito antes de a Guarda marchar ao Champ de Mars." } },
        { "pista_champ_boato", new[] { "O primeiro tiro", "Vizinhas do mercado", "Segundo as vizinhas do mercado, os peticionários vieram armados e atiraram primeiro; a Guarda só se defendeu." } },
        { "pista_champ_calunia", new[] { "A hora da bandeira", "Cartaz sem assinatura", "Cartaz no mural: a bandeira vermelha só foi hasteada depois dos tiros, para esconder que os peticionários, pagos pela Inglaterra, atacaram a Guarda." } },
        // Fase 3 — Assignats
        { "pista_assignats_1", new[] { "As chapas roubadas", "Gravador Morel", "O gravador mostra a cópia da queixa registrada na seção dias antes da prisão: ele denunciou o roubo de chapas de gravação." } },
        { "pista_assignats_2", new[] { "O defeito dos tipos", "Caixa de tipos da oficina", "Os tipos das notas falsas têm um defeito que não existe nos tipos de Morel: foram impressas em outra prensa." } },
        { "pista_assignats_boato", new[] { "A história do roubo", "Conversa de cocheiros", "Segundo os cocheiros, as chapas nunca saíram da oficina de Morel: ele inventou o roubo depois de preso, para enganar a seção." } },
        { "pista_assignats_calunia", new[] { "A prensa de Morel", "Denúncia anônima na padaria", "Papel deixado no balcão: as notas falsas saíram da prensa de Morel, que as imprimia à noite para os emigrados de Coblença." } },
        // Fase 4 — Jornalista
        { "pista_jornalista_1", new[] { "Os números do jornal", "Redator Marchand", "Marchand mostra os números do jornal: pedem clemência e justiça, mas nenhum convoca à revolta nem à volta do Rei." } },
        { "pista_jornalista_2", new[] { "O certificado da seção", "Arquivo da seção", "A seção deu a Marchand um certificado de civismo três meses antes da prisão." } },
        { "pista_jornalista_boato", new[] { "O número de outubro", "Carcereiro da Conciergerie", "Segundo o carcereiro, os presos contam que o jornal de Marchand pediu a volta do Rei num número de outubro." } },
        { "pista_jornalista_calunia", new[] { "O certificado recusado", "Caixa de denúncias", "Papel na caixa de denúncias: a seção recusou o certificado de civismo a Marchand, que esconde armas no porão da redação." } },
        // Fase 4 — Negociante
        { "pista_negociante_1", new[] { "Os armazéns de Garnier", "Comissário Vautrin", "O comissário mostra o relatório da inspeção: os armazéns de Garnier guardavam trigo em bom estado, o dobro do que ele declarou." } },
        { "pista_negociante_2", new[] { "Os preços do Máximo", "Parede de editais", "Pela lista de preços afixada, Garnier vendeu trigo à padaria da seção acima do preço máximo." } },
        { "pista_negociante_boato", new[] { "O trigo apodrecido", "Carcereiro da Conciergerie", "Segundo o carcereiro, os armazéns de Garnier estão cheios de trigo podre: ele deixa o grão estragar para o povo passar fome." } },
        { "pista_negociante_calunia", new[] { "As vendas de Garnier", "Arquivo da seção", "Papel no arquivo da seção: Garnier não vende grão a ninguém em Paris; guarda tudo para os ingleses, a mando de Pitt." } },
        // Fase 4 — Girondina
        { "pista_girondina_1", new[] { "As cartas do deputado", "Cidadã Delorme", "A viúva mostra as cartas do marido: falam de família e de dívidas; nenhuma cita fuga, plano ou nome de deputado." } },
        { "pista_girondina_2", new[] { "A denúncia retirada", "Caixa de denúncias", "O vizinho que a denunciou retirou a acusação por escrito: queria o apartamento dela." } },
        { "pista_girondina_boato", new[] { "O plano de fuga", "Redator Marchand", "Segundo o redator, as cartas do deputado trazem o plano de fuga dos girondinos e os nomes de quem os esconde." } },
        { "pista_girondina_calunia", new[] { "A acusação do vizinho", "Parede de editais", "Cartaz na parede de editais: o vizinho que denunciou a viúva mantém a acusação, e ela tentou envenená-lo para que se calasse." } },
    };

    /// <summary>Nome, fonte e descrição de referência de uma pista (os textos em vigor), ou null. Usado pela ferramenta
    /// de dedução para migrar assets que ainda tinham os textos anteriores.</summary>
    internal static string[] TextoDeReferencia(string itemID) =>
        TextosRevisados.TryGetValue(itemID, out string[] texto) ? texto : null;

    /// <summary>Falas de referência da reação de um NPC a um caso (ex.: "CocheiroJoubert", "Caso_Varennes"), ou null.</summary>
    internal static string[] FalasDeReferencia(string npcId, string casoArquivo)
    {
        Definir();
        R reacao = Reacoes.Find(x => x.npc == npcId && Path.GetFileNameWithoutExtension(x.caso) == casoArquivo);
        return reacao != null ? reacao.falas : null;
    }

    // Revelações corrigidas (texto histórico e afirmações discutíveis): texto original -> texto novo.
    private static readonly Dictionary<string, string> RevelacoesRevisadas = new Dictionary<string, string>
    {
        { "O incêndio começou com a Guarda, não com o operário. A calúnia manchou o nome da tipografia.",
          "Ninguém provou que o operário ateou fogo à fábrica: a acusação era anônima e falsa, e manchou o nome da tipografia." },
        // Prompt 7: o boato de Réveillon passou a ser o dos "quinze soldos" (par com o discurso na assembleia).
        { "Descobriu-se que Réveillon nunca chamou a Guarda. O boato impresso desmoronou.", RevelacaoDoBoatoDeReveillon },
        { "Não se provou que Réveillon mandou a Guarda atirar: ele pediu proteção, não o fogo. O boato impresso desmoronou.", RevelacaoDoBoatoDeReveillon },
    };

    /// <summary>Revelação da versão Com Boato do Caso de Réveillon desde o Prompt 7 (a ferramenta de dedução migra o asset).</summary>
    internal const string RevelacaoDoBoatoDeReveillon =
        "A ata da assembleia desmentiu o boato: Réveillon nunca disse que um operário vive com quinze soldos, e pediu o pão mais " +
        "barato junto com o corte. O panfleto que o repetiu desmoronou.";

    // Prompt 7 (Fase 2 com dedução): nas Fases 3 e 4 o cliente cita, na fala, quem espalha o boato. Na Fase 2 as falas dos
    // clientes são do roteiro do grupo, então a pista vai na carta do cliente (alegação), que o jogador lê ao aceitar o caso.
    private static readonly Dictionary<string, string> AlegacoesComPista = new Dictionary<string, string>
    {
        { "alegacao_joias_1", "O joalheiro jura que acreditou estar negociando com a Rainha, e reclama que o gazeteiro da esquina conta outra história." },
        { "alegacao_jean_1", "Réveillon garante que suas palavras na assembleia foram distorcidas, e que os operários repetem a versão errada." },
        { "alegacao_operario_2", "Ele afirma que a Guarda abriu fogo antes de qualquer pedra ser atirada, e que o próprio Réveillon anda contando outra versão." },
    };

    internal static string AlegacaoComPista(string itemID) =>
        AlegacoesComPista.TryGetValue(itemID, out string texto) ? texto : null;

    // Estimativas da mesa (Fase 2, anteriores ao Prompt 2) alinhadas à versão Fatos da receita:
    // caso -> { ouro, povo, estado antigos } -> { novos }.
    private static readonly Dictionary<string, int[][]> EstimativasRevisadas = new Dictionary<string, int[][]>
    {
        { "Caso_Joalheiro", new[] { new[] { 50, -20, 0 }, new[] { 20, 50, -10 } } },
        { "Caso_Reveillon", new[] { new[] { 70, -50, 0 }, new[] { 50, -20, 50 } } },
        { "Caso_Operario",  new[] { new[] { 5, 50, 0 },   new[] { 10, 60, -20 } } },
    };

    // ----- Fase 2 (casos existentes; textos das pistas antigas só entram onde estiverem vazios) -----

    private static void DefinirFase2()
    {
        const string F = "Fase2";
        const string SO = "Assets/Scriptableobjects/";

        Casos.Add(new C
        {
            caminho = Caso("Caso_Joalheiro"), fase = 2, cena = F, receita = SO + "ReceitaDeCaso_Joias.asset",
            f1 = new P(SO + "Item_joalheiro1.asset", "pista_joias_1", "Registro de venda do colar", Confiabilidade.Fato, "Livro de vendas do joalheiro",
                "O colar foi vendido 'em nome da Rainha': pago por um cardeal e retirado por uma moça. A Rainha nunca apareceu na loja."),
            f2 = new P(SO + "Item_joalheiro2.asset", "pista_joias_2", "Carta com a assinatura da Rainha", Confiabilidade.Fato, "Papéis esquecidos na taverna",
                "A carta de encomenda está assinada 'Marie Antoinette de France'. A Rainha nunca assinava assim."),
            boato = new P(Item(F, "Pista_Joias_Boato"), "pista_joias_boato", "Conversa sobre a Rainha", Confiabilidade.Boato, "Gazeteiro da esquina",
                "Dizem que a Rainha encomendou o colar escondida do Rei e depois negou tudo."),
            calunia = new P(Item(F, "Pista_Joias_Calunia"), "pista_joias_calunia", "Libelo contra a Rainha", Confiabilidade.Calunia, "Cartaz sem assinatura",
                "Folha anônima: 'A Austríaca e o Cardeal eram amantes e dividiram o colar'."),
            alegacoes = new[]
            {
                // Prompt 7: a carta do cliente aponta quem espalha o boato (as falas do joalheiro são do roteiro do grupo).
                new P(Item(F, "Alegacao_Joias_1"), "alegacao_joias_1", "Carta do joalheiro: fui enganado", Confiabilidade.Boato, "Carta do cliente",
                    AlegacaoComPista("alegacao_joias_1")),
                new P(Item(F, "Alegacao_Joias_2"), "alegacao_joias_2", "Carta do joalheiro: a Coroa me deve", Confiabilidade.Boato, "Carta do cliente",
                    "Ele afirma que a Coroa deveria pagar o colar, já que a fraude usou o nome da Rainha."),
            },
            comBoato = new V(0, 0, 0, revelacao: "Soube-se que a Rainha nunca encomendou o colar em segredo: o boato impresso pela tipografia caiu por terra."),
            comCalunia = new V(0, 0, 0, revelacao: "O libelo contra a Rainha e o Cardeal não tinha prova alguma. A tipografia é chamada de caluniadora."),
            soAlegacoes = new V(20, -5, 8, -5, 0, "O panfleto do Caso das Joias só repetia a versão do joalheiro. Sem provas, os leitores o esqueceram depressa."),
        });

        Casos.Add(new C
        {
            caminho = Caso("Caso_Reveillon"), fase = 2, cena = F, receita = SO + "ReceitaDeCaso_Jean.asset",
            f1 = new P(SO + "Item_Jean1.asset", "pista_jean_1", "Ata da assembleia do distrito", Confiabilidade.Fato, "Ata da assembleia eleitoral",
                "Réveillon propôs baixar salários JUNTO com o preço do pão. Não disse que o povo devia passar fome."),
            f2 = new P(SO + "Item_Jean2.asset", "pista_jean_2", "Livro de salários da manufatura", Confiabilidade.Fato, "Livro-caixa da manufatura",
                "Réveillon pagava mais que a média do bairro e manteve os salários durante o inverno."),
            boato = new P(Item(F, "Pista_Jean_Boato"), "pista_jean_boato", "Conversa sobre a Guarda", Confiabilidade.Boato, "Conversa de operários",
                "Dizem que foi o próprio Réveillon quem chamou a Guarda para atirar nos operários."),
            calunia = new P(Item(F, "Pista_Jean_Calunia"), "pista_jean_calunia", "Folha: agente inglês", Confiabilidade.Calunia, "Folha anônima vendida na rua",
                "Acusa Réveillon de ser pago pela Inglaterra para matar Paris de fome."),
            alegacoes = new[]
            {
                new P(Item(F, "Alegacao_Jean_1"), "alegacao_jean_1", "Carta de Réveillon: fui mal entendido", Confiabilidade.Boato, "Carta do cliente",
                    AlegacaoComPista("alegacao_jean_1")),
                new P(Item(F, "Alegacao_Jean_2"), "alegacao_jean_2", "Carta de Réveillon: nunca quis o mal deles", Confiabilidade.Boato, "Carta do cliente",
                    "Ele afirma que sempre tratou bem os seus operários."),
            },
            comBoato = new V(0, 0, 0, revelacao: "Descobriu-se que Réveillon nunca chamou a Guarda. O boato impresso desmoronou."),
            comCalunia = new V(0, 0, 0, revelacao: "Ninguém encontrou prova de que Réveillon servia aos ingleses: a calúnia voltou-se contra a tipografia."),
            soAlegacoes = new V(-8, 20, 20, 0, -5, "O panfleto sobre Réveillon só repetia a defesa dele. Nem a Corte lhe deu crédito."),
        });

        Casos.Add(new C
        {
            caminho = Caso("Caso_Operario"), fase = 2, cena = F, receita = SO + "ReceitaDeCaso_Operario.asset",
            f1 = new P(SO + "Item_operario1.asset", "pista_operario_1", "Relato do sobrevivente", Confiabilidade.Fato, "O próprio operário",
                "A Guarda Francesa atirou contra a multidão desarmada que pedia pão e salário."),
            f2 = new P(SO + "Item_operario2.asset", "pista_operario_2", "Ordem afixada da Guarda", Confiabilidade.Fato, "Cartaz arrancado de um muro",
                "A ordem do comandante manda 'dispersar a multidão por todos os meios', antes de qualquer violência."),
            boato = new P(Item(F, "Pista_Operario_Boato"), "pista_operario_boato", "Conversa sobre agitadores", Confiabilidade.Boato, "Jean-Baptiste Réveillon",
                "Dizem que agitadores pagos pelo duque de Orléans distribuíram dinheiro à multidão."),
            calunia = new P(Item(F, "Pista_Operario_Calunia"), "pista_operario_calunia", "Bilhete sobre o incêndio", Confiabilidade.Calunia, "Bilhete deixado na taverna",
                "Afirma que foi o operário quem ateou fogo à fábrica."),
            alegacoes = new[]
            {
                new P(Item(F, "Alegacao_Operario_1"), "alegacao_operario_1", "Carta do operário: só queríamos pão", Confiabilidade.Boato, "Carta do cliente",
                    "O operário jura que a multidão só pedia comida e salário."),
                new P(Item(F, "Alegacao_Operario_2"), "alegacao_operario_2", "Carta do operário: atiraram primeiro", Confiabilidade.Boato, "Carta do cliente",
                    AlegacaoComPista("alegacao_operario_2")),
            },
            comBoato = new V(0, 0, 0, revelacao: "Nenhum agitador pago foi encontrado. O boato publicado pela tipografia não se sustentou."),
            comCalunia = new V(0, 0, 0, revelacao: "O incêndio começou com a Guarda, não com o operário. A calúnia manchou o nome da tipografia."),
            soAlegacoes = new V(25, -8, 5, -5, 0, "O panfleto do operário só repetia o que ele contou. Faltou prova, e o caso esfriou."),
        });

        Npcs.Add(new N { cena = F, id = "Joalheiro", existente = true, falante = "Joalheiro",
            padrao = new[] { "Hoje não tenho tempo para conversa, rapaz. Se não vai comprar nada, deixe a vitrine para quem vai." } });
        Npcs.Add(new N { cena = F, id = "Jean-Baptiste Réveillon", existente = true, falante = "Jean Réveillon",
            padrao = new[] { "Já disse tudo aos magistrados. Não sei de nada que lhe interesse." } });
        Npcs.Add(new N { cena = F, id = "Operario", existente = true, falante = "Operário",
            padrao = new[] { "Desculpe, senhor. Não sei nada sobre isso. Só quero voltar ao trabalho, se ainda houver trabalho." } });
        Npcs.Add(new N { cena = F, id = "Gazeteiro", nome = "Gazeteiro", falante = "Gazeteiro", x = 0f, cor = new Color(0.85f, 0.75f, 0.45f),
            padrao = new[] { "Gazetas! Notícias de Versalhes! Sobre isso não ouvi nada que valha um soldo." } });

        // Dedução ativa (Prompt 7): quem espalha o boato aponta os objetos da cena (o do fato, quando há, e o da calúnia).
        Reacoes.Add(new R { npc = "Gazeteiro", caso = Caso("Caso_Joalheiro"), item = Item(F, "Pista_Joias_Boato"),
            falas = new[] { "O colar? Nas tavernas só se fala disso!",
                            "Dizem que a própria Rainha foi à loja, disfarçada, escolher o colar escondida do Rei. Depois negou tudo.",
                            "E no muro de cartazes colaram uma folha contando coisa pior, sobre ela e o Cardeal." } });
        Reacoes.Add(new R { npc = "Gazeteiro", caso = Caso("Caso_Reveillon"), item = Item(F, "Pista_Jean_Calunia"),
            falas = new[] { "Réveillon? Tenho aqui uma folha que vende como pão quente.",
                            "Diz que no inverno ele cortou pela metade o salário da manufatura e mandou a diferença para os ingleses. Leve, é sua." } });
        Reacoes.Add(new R { npc = "Operario", caso = Caso("Caso_Reveillon"), item = Item(F, "Pista_Jean_Boato"),
            falas = new[] { "Aquele homem? Disse na assembleia que um operário vive muito bem com quinze soldos por dia!",
                            "Só queria cortar o nosso salário. Do pão, nem uma palavra.",
                            "Na banca de panfletos vendem uma gazeta com as contas da manufatura, e o gazeteiro da esquina tem uma folha ainda pior sobre ele." } });
        Reacoes.Add(new R { npc = "Jean-Baptiste Réveillon", caso = Caso("Caso_Operario"), item = Item(F, "Pista_Operario_Boato"),
            falas = new[] { "Aqueles homens não pediam pão: vieram armados, pagos na véspera por agitadores do duque de Orléans!",
                            "A Guarda só atirou para se defender. Anote isso.",
                            "Colaram a ordem da Guarda no muro de cartazes, e na mesa da taverna deixaram um bilhete sobre o incêndio da minha fábrica. Cada um conta de um jeito." } });

        Loots.Add(new L { cena = F, id = "BancaDePanfletos", nome = "Banca de Panfletos", sprite = SpriteDocumento, x = -12f, y = 0f, escala = 0.2f,
            porCaso = { { Caso("Caso_Reveillon"), SO + "Item_Jean2.asset" } } });
        Loots.Add(new L { cena = F, id = "MuroDeCartazes", nome = "Muro de Cartazes", sprite = SpritePanfleto, x = 40f, y = 0f, escala = 0.5f,
            porCaso = { { Caso("Caso_Joalheiro"), Item(F, "Pista_Joias_Calunia") }, { Caso("Caso_Operario"), SO + "Item_operario2.asset" } } });
        Loots.Add(new L { cena = F, id = "MesaDaTaverna", nome = "Mesa da Taverna", sprite = SpriteMesa, x = 68f, y = -2.1f, escala = 1.2f,
            porCaso = { { Caso("Caso_Operario"), Item(F, "Pista_Operario_Calunia") } } }); // Joias: a assinatura agora vem do joalheiro
    }

    // ----- Fase 3 (1789–1792) -----

    private static C NovoCaso(string arquivo, string cena, int fase, RotaFinal rota, string titulo, string objetivo, string descricao,
        int ouro, int povo, int estado)
    {
        string pasta = cena;
        var caso = new C
        {
            caminho = Caso(arquivo), cena = cena, fase = fase, rota = rota, titulo = titulo, objetivo = objetivo, descricao = descricao,
            ouro = ouro, povo = povo, estado = estado,
            receita = Item(pasta, "ReceitaDeCaso_" + arquivo.Replace("Caso_", "")),
        };
        Casos.Add(caso);
        return caso;
    }

    // "O Padeiro de Notre-Dame" (Caso_Padeiro) saiu da campanha em 27/09 (decisão do grupo: a Fase 3 tem três casos e o
    // jogador conclui um). Ferramentas > Campanha > 6 retira os assets e as reações dele nas cenas já configuradas.
    private static void DefinirFase3()
    {
        const string F = "Fase3";

        C varennes = NovoCaso("Caso_Varennes", F, 3, RotaFinal.Nenhuma, "A Carruagem de Varennes", "Descobrir o papel do cocheiro na fuga do Rei.",
            "Junho de 1791. A família real foi presa em Varennes tentando fugir. O cocheiro Joubert, que conduziu a berlinda, é acusado de ter ajudado a fuga. Ele pede que a tipografia prove que só cumpria um contrato.",
            45, -20, 30);
        varennes.f1 = new P(Item(F, "Pista_Varennes_Contrato"), "pista_varennes_1", "Contrato da berlinda", Confiabilidade.Fato, "Cocheiro Joubert",
            "Contrato em nome de uma 'baronesa de Korff': uma viagem comum, sem menção a quem seria levado.");
        varennes.f2 = new P(Item(F, "Pista_Varennes_Depoimento"), "pista_varennes_2", "Depoimento do mestre de posta", Confiabilidade.Fato, "Ata afixada pelo clube dos Cordeliers",
            "O mestre de posta reconheceu o Rei em Sainte-Menehould. O cocheiro não falou com ninguém na estrada.");
        varennes.boato = new P(Item(F, "Pista_Varennes_Boato"), "pista_varennes_boato", "Conversa sobre ouro austríaco", Confiabilidade.Boato, "Conversa no Champ de Mars",
            "Dizem que o cocheiro era espião da Rainha, pago em ouro austríaco.");
        varennes.calunia = new P(Item(F, "Pista_Varennes_Calunia"), "pista_varennes_calunia", "Folha: cavalos envenenados", Confiabilidade.Calunia, "Prova de impressão abandonada",
            "'O cocheiro envenenou os cavalos da Guarda para atrasar a perseguição.'");
        varennes.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Varennes_1"), "alegacao_varennes_1", "Carta do cocheiro: eu não sabia", Confiabilidade.Boato, "Carta do cliente", "O cocheiro jura que não sabia quem levava."),
            new P(Item(F, "Alegacao_Varennes_2"), "alegacao_varennes_2", "Carta do cocheiro: fui ameaçado", Confiabilidade.Boato, "Carta do cliente", "Ele diz que foi ameaçado para não parar a carruagem."),
        };
        varennes.panfleto = new P(Item(F, "Panfleto_Varennes"), "panfleto_varennes", "Panfleto: A Carruagem de Varennes", Confiabilidade.NaoEPista, "Tipografia", "Panfleto impresso sobre a fuga do Rei.");
        varennes.fatos = new V(-20, 30, 45);
        varennes.comBoato = new V(-30, 40, 60, -10, -10, "O 'ouro austríaco' do cocheiro nunca apareceu. O boato publicado foi desmascarado.");
        varennes.comCalunia = new V(-40, 50, 75, -20, -15, "Os cavalos da Guarda estavam sãos. A tipografia é acusada de inventar histórias.");
        varennes.soAlegacoes = new V(-8, 12, 18, 0, -5, "O panfleto só trazia a palavra do cocheiro. A Corte ignorou; o povo desconfiou.");

        C champ = NovoCaso("Caso_ChampDeMars", F, 3, RotaFinal.Nenhuma, "O Fuzilamento do Champ de Mars", "Descobrir quem ordenou o fogo.",
            "Julho de 1791. A Guarda Nacional abriu fogo contra quem assinava uma petição pela República no Champ de Mars. A peticionária Lacombe, ferida, quer que a tipografia revele quem deu a ordem.",
            20, 30, -20);
        champ.f1 = new P(Item(F, "Pista_Champ_Peticao"), "pista_champ_1", "Petição manchada de sangue", Confiabilidade.Fato, "Peticionária Lacombe",
            "Dezenas de assinaturas pacíficas. Não havia armas junto ao altar da pátria.");
        champ.f2 = new P(Item(F, "Pista_Champ_LeiMarcial"), "pista_champ_2", "Proclamação da lei marcial", Confiabilidade.Fato, "Proclamação pregada no balcão da padaria",
            "A bandeira vermelha da lei marcial foi hasteada por ordem da prefeitura antes do fogo.");
        champ.boato = new P(Item(F, "Pista_Champ_Boato"), "pista_champ_boato", "Conversa sobre o primeiro tiro", Confiabilidade.Boato, "Vizinhas do mercado",
            "Dizem que os peticionários atiraram primeiro e a Guarda só se defendeu.");
        champ.calunia = new P(Item(F, "Pista_Champ_Calunia"), "pista_champ_calunia", "Libelo: peticionários pagos", Confiabilidade.Calunia, "Cartaz sem assinatura",
            "'Os peticionários eram pagos pela Inglaterra para derrubar a Constituição.'");
        champ.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Champ_1"), "alegacao_champ_1", "Carta da peticionária: éramos pacíficos", Confiabilidade.Boato, "Carta do cliente", "Ela jura que ninguém ali estava armado."),
            new P(Item(F, "Alegacao_Champ_2"), "alegacao_champ_2", "Carta da peticionária: mandaram atirar", Confiabilidade.Boato, "Carta do cliente", "Ela afirma que os comandantes mandaram atirar para matar."),
        };
        champ.panfleto = new P(Item(F, "Panfleto_Champ"), "panfleto_champ", "Panfleto: O Champ de Mars", Confiabilidade.NaoEPista, "Tipografia", "Panfleto impresso sobre o fuzilamento.");
        champ.fatos = new V(35, -25, 20);
        champ.comBoato = new V(45, -35, 30, -10, -5, "Nenhum peticionário armado foi encontrado: o boato publicado ruiu.");
        champ.comCalunia = new V(55, -45, 40, -20, -10, "Não havia dinheiro inglês algum. A tipografia é acusada de caluniar os mortos.");
        champ.soAlegacoes = new V(12, -8, 8, -5, 0, "O panfleto só repetia o relato da peticionária. Comoveu poucos e não provou nada.");

        C assignats = NovoCaso("Caso_Assignats", F, 3, RotaFinal.Nenhuma, "Os Assignats Falsos", "Descobrir quem imprimiu os assignats falsos.",
            "1792. Notas falsas de assignat inundam Paris. O gravador Morel, colega de ofício da tipografia, foi preso acusado de fabricá-las. Ele jura que as chapas foram roubadas da sua oficina.",
            35, 10, 10);
        assignats.f1 = new P(Item(F, "Pista_Assignats_Queixa"), "pista_assignats_1", "Queixa de roubo das chapas", Confiabilidade.Fato, "Gravador Morel",
            "Queixa registrada na seção dias antes da prisão: Morel denunciou o roubo de chapas de gravação.");
        assignats.f2 = new P(Item(F, "Pista_Assignats_Tipos"), "pista_assignats_2", "Tipos com defeito", Confiabilidade.Fato, "Caixa de tipos da oficina",
            "As notas falsas têm um defeito de tipo que não existe nos tipos de Morel: foram impressas em outra prensa.");
        assignats.boato = new P(Item(F, "Pista_Assignats_Boato"), "pista_assignats_boato", "Conversa sobre notas nas tavernas", Confiabilidade.Boato, "Conversa de cocheiros",
            "Dizem que o gravador vendia notas falsas nas tavernas, dez por uma moeda de ouro.");
        assignats.calunia = new P(Item(F, "Pista_Assignats_Calunia"), "pista_assignats_calunia", "Denúncia: agente dos emigrados", Confiabilidade.Calunia, "Denúncia anônima na padaria",
            "'O gravador trabalha para os emigrados de Coblença.'");
        assignats.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Assignats_1"), "alegacao_assignats_1", "Carta do gravador: roubaram minhas chapas", Confiabilidade.Boato, "Carta do cliente", "Morel jura que as chapas sumiram da oficina."),
            new P(Item(F, "Alegacao_Assignats_2"), "alegacao_assignats_2", "Carta do gravador: sou patriota", Confiabilidade.Boato, "Carta do cliente", "Ele lembra que gravou de graça os emblemas da seção."),
        };
        assignats.panfleto = new P(Item(F, "Panfleto_Assignats"), "panfleto_assignats", "Panfleto: Os Assignats Falsos", Confiabilidade.NaoEPista, "Tipografia", "Panfleto impresso sobre as notas falsas.");
        assignats.fatos = new V(15, 10, 35);
        assignats.comBoato = new V(20, 15, 50, -10, -10, "Ninguém viu o gravador vender nota alguma. O boato impresso foi desmentido.");
        assignats.comCalunia = new V(25, 20, 65, -15, -15, "Morel nunca teve contato com emigrados. A calúnia custou caro à tipografia.");
        assignats.soAlegacoes = new V(5, 4, 14, -5, 0, "O panfleto só repetia a defesa do gravador. Os leitores esperavam provas.");

        Npcs.Add(new N { cena = F, id = "ViuvaFrancois", nome = "Viúva François", falante = "Viúva François", x = -8f, cor = new Color(0.55f, 0.45f, 0.6f),
            padrao = new[] { "Desculpe, não sei nada sobre isso. Desde que levaram o meu Denis, mal saio de casa." } });
        Npcs.Add(new N { cena = F, id = "CocheiroJoubert", nome = "Cocheiro Joubert", falante = "Cocheiro Joubert", x = 14f, cor = new Color(0.45f, 0.35f, 0.25f),
            padrao = new[] { "Eu só conheço estradas e cavalos, patrão. Disso aí não sei nada." } });
        Npcs.Add(new N { cena = F, id = "PeticionariaLacombe", nome = "Peticionária Lacombe", falante = "Peticionária Lacombe", x = 34f, cor = new Color(0.7f, 0.3f, 0.3f),
            padrao = new[] { "Não tenho nada a dizer sobre isso. A minha luta é outra." } });
        Npcs.Add(new N { cena = F, id = "GravadorMorel", nome = "Gravador Morel", falante = "Gravador Morel", x = 58f, cor = new Color(0.3f, 0.45f, 0.6f),
            padrao = new[] { "Não posso ajudar com isso, colega. Já tenho problemas demais com a justiça." } });

        // Dedução ativa: o cliente aponta quem espalha o boato, e quem espalha o boato aponta os dois objetos (o do fato e
        // o da calúnia). Seguir essas pistas leva às quatro afirmações do quadro em 4h; os outros três lugares não têm nada.
        Reacoes.Add(new R { npc = "CocheiroJoubert", caso = varennes.caminho, item = varennes.f1.caminho,
            falas = new[] { "Me contrataram em nome de uma tal baronesa de Korff. Sessenta libras, em moeda francesa, como qualquer frete.",
                            "Eu só conduzia os cavalos, juro. Tenho o contrato aqui: leia o senhor mesmo.",
                            "E a peticionária da praça ainda anda contando que me pagaram em ouro da Áustria!" } });
        Reacoes.Add(new R { npc = "PeticionariaLacombe", caso = varennes.caminho, item = varennes.boato.caminho,
            falas = new[] { "O cocheiro do Rei? Recebeu ouro austríaco, isso sim, e sabia desde Paris quem levava na berlinda.",
                            "Os Cordeliers pregaram uma ata no mural contando outra história. E na caixa de tipos alguém largou a prova de um panfleto sobre os cavalos." } });
        Reacoes.Add(new R { npc = "PeticionariaLacombe", caso = champ.caminho, item = champ.f1.caminho,
            falas = new[] { "Assinávamos uma petição, sem uma arma sequer. E eles atiraram.", "Guardei a folha. O sangue ainda está nela.",
                            "E no mercado a viúva do padeiro repete que fomos nós que atiramos primeiro!" } });
        Reacoes.Add(new R { npc = "ViuvaFrancois", caso = champ.caminho, item = champ.boato.caminho,
            falas = new[] { "As vizinhas do mercado juram que os peticionários vieram armados e atiraram primeiro. A Guarda só se defendeu.",
                            "Pregaram uma proclamação no balcão da padaria, e no mural dos Cordeliers colaram um cartaz sobre a bandeira. Cada um conta de um jeito." } });
        Reacoes.Add(new R { npc = "GravadorMorel", caso = assignats.caminho, item = assignats.f1.caminho,
            falas = new[] { "Roubaram as minhas chapas, colega! Dei queixa na seção dias antes de me prenderem.", "Aqui está a cópia da queixa, com o carimbo da seção.",
                            "Os cocheiros espalham que eu inventei o roubo. Pergunte ao Joubert o que andam dizendo." } });
        Reacoes.Add(new R { npc = "CocheiroJoubert", caso = assignats.caminho, item = assignats.boato.caminho,
            falas = new[] { "O gravador? Nas tavernas se diz que as chapas nunca saíram da oficina dele. Inventou o roubo depois de preso.",
                            "Deixaram um papel sobre isso no balcão da padaria. E a caixa de tipos da oficina dele ainda está na rua, se o senhor quiser conferir." } });

        Loots.Add(new L { cena = F, id = "BalcaoDaPadaria", nome = "Balcão da Padaria", sprite = SpriteMesa, x = -16f, y = -2.1f, escala = 1.2f,
            porCaso = { { champ.caminho, champ.f2.caminho }, { assignats.caminho, assignats.calunia.caminho } } });
        Loots.Add(new L { cena = F, id = "MuralDosCordeliers", nome = "Mural dos Cordeliers", sprite = SpritePanfleto, x = 24f, y = 0f, escala = 0.5f,
            porCaso = { { varennes.caminho, varennes.f2.caminho }, { champ.caminho, champ.calunia.caminho } } });
        Loots.Add(new L { cena = F, id = "CaixaDeTipos", nome = "Caixa de Tipos", sprite = SpriteDocumento, x = 70f, y = 0f, escala = 0.2f,
            porCaso = { { varennes.caminho, varennes.calunia.caminho }, { assignats.caminho, assignats.f2.caminho } } });
    }

    // ----- Fase 4 (1793, um caso por rota) -----

    private static void DefinirFase4()
    {
        const string F = "Fase4";

        C jornalista = NovoCaso("Caso_Jornalista", F, 4, RotaFinal.A_Guilhotina, "O Redator do Velho Sans-culotte", "Reunir provas para a defesa de Marchand.",
            "1793. Lucien Marchand, redator de um jornal que pediu clemência e o fim do Terror, será julgado pelo Tribunal Revolucionário por 'conspiração contra a República'. O povo lê o seu jornal; o Comitê quer silenciá-lo.",
            15, 20, -20);
        jornalista.f1 = new P(Item(F, "Pista_Jornalista_Exemplares"), "pista_jornalista_1", "Exemplares do jornal", Confiabilidade.Fato, "Redator Marchand",
            "Os números pedem clemência e justiça, mas nenhum convoca à revolta nem à volta do Rei.");
        jornalista.f2 = new P(Item(F, "Pista_Jornalista_Certificado"), "pista_jornalista_2", "Certificado de civismo", Confiabilidade.Fato, "Arquivo da seção",
            "A seção deu a Marchand um certificado de civismo três meses antes da prisão.");
        jornalista.boato = new P(Item(F, "Pista_Jornalista_Boato"), "pista_jornalista_boato", "Conversa sobre cartas inglesas", Confiabilidade.Boato, "Carcereiro da Conciergerie",
            "Dizem que o redator recebe cartas com selo inglês toda semana.");
        jornalista.calunia = new P(Item(F, "Pista_Jornalista_Calunia"), "pista_jornalista_calunia", "Denúncia: armas escondidas", Confiabilidade.Calunia, "Caixa de denúncias",
            "'Marchand esconde armas no porão da redação.'");
        jornalista.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Jornalista_1"), "alegacao_jornalista_1", "Carta de Marchand: escrevi pela República", Confiabilidade.Boato, "Carta do cliente", "Marchand jura que cada linha foi escrita em defesa da República."),
            new P(Item(F, "Alegacao_Jornalista_2"), "alegacao_jornalista_2", "Carta de Marchand: temem a verdade", Confiabilidade.Boato, "Carta do cliente", "Ele afirma que o Comitê teme o que ele publica."),
        };
        jornalista.panfleto = new P(Item(F, "Panfleto_Jornalista"), "panfleto_jornalista", "Panfleto: Em defesa de Marchand", Confiabilidade.NaoEPista, "Tipografia", "Panfleto impresso em defesa do redator.");
        jornalista.fatos = new V(20, -20, 15);
        jornalista.comBoato = new V(30, -30, 25, -10, -5, "As 'cartas inglesas' nunca existiram. O boato impresso enfraqueceu a defesa.");
        jornalista.comCalunia = new V(40, -40, 35, -20, -10, "Não havia arma alguma na redação. A calúnia foi usada contra a própria tipografia.");
        jornalista.soAlegacoes = new V(8, -8, 6, -5, 0, "O panfleto só repetia as palavras de Marchand. Soou como desespero.");

        C negociante = NovoCaso("Caso_Negociante", F, 4, RotaFinal.B_Tirano, "O Negociante de Grãos", "Provar as acusações contra Garnier.",
            "1793. O Comitê encomenda à tipografia um panfleto contra o negociante Garnier, acusado de açambarcar grãos e burlar a Lei do Máximo. Um trabalho bem pago, com o Estado ao seu lado.",
            60, -15, 25);
        negociante.f1 = new P(Item(F, "Pista_Negociante_Relatorio"), "pista_negociante_1", "Relatório de subsistência", Confiabilidade.Fato, "Comissário Vautrin",
            "Relatório oficial: os armazéns de Garnier guardavam mais grãos do que ele declarou.");
        negociante.f2 = new P(Item(F, "Pista_Negociante_Edital"), "pista_negociante_2", "Lista de preços do Máximo", Confiabilidade.Fato, "Parede de editais",
            "Pela lista de preços afixada, Garnier vendeu acima do preço máximo.");
        negociante.boato = new P(Item(F, "Pista_Negociante_Boato"), "pista_negociante_boato", "Conversa sobre trigo podre", Confiabilidade.Boato, "Carcereiro da Conciergerie",
            "Dizem que Garnier deixa o trigo apodrecer para matar o povo de fome.");
        negociante.calunia = new P(Item(F, "Pista_Negociante_Calunia"), "pista_negociante_calunia", "Denúncia: agente de Pitt", Confiabilidade.Calunia, "Arquivo da seção",
            "'Garnier é agente de Pitt e paga os inimigos da República.'");
        negociante.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Negociante_1"), "alegacao_negociante_1", "Ofício do Comitê: inimigo do povo", Confiabilidade.Boato, "Carta do cliente", "O Comitê afirma que Garnier é inimigo do povo."),
            new P(Item(F, "Alegacao_Negociante_2"), "alegacao_negociante_2", "Ofício do Comitê: culpa notória", Confiabilidade.Boato, "Carta do cliente", "O ofício diz que a culpa dele 'é notória'."),
        };
        negociante.panfleto = new P(Item(F, "Panfleto_Negociante"), "panfleto_negociante", "Panfleto: O Açambarcador", Confiabilidade.NaoEPista, "Tipografia", "Panfleto encomendado pelo Comitê.");
        negociante.fatos = new V(-15, 30, 60);
        negociante.comBoato = new V(-25, 40, 80, -5, -10, "O trigo de Garnier estava são. O boato impresso comprometeu o Comitê.");
        negociante.comCalunia = new V(-35, 50, 100, -10, -20, "Ninguém provou elo algum com Pitt. Até o Comitê se afastou da tipografia.");
        negociante.soAlegacoes = new V(-6, 12, 24, 0, -5, "O panfleto só repetia o ofício do Comitê. Soou como propaganda.");

        C girondina = NovoCaso("Caso_Girondina", F, 4, RotaFinal.C_Equilibrio, "A Viúva Girondina", "Descobrir o que dizem as cartas.",
            "1793. Depois da queda dos girondinos, a cidadã Delorme, viúva de um deputado, é acusada de guardar cartas de conspiração. Nem o povo nem o Comitê confiam nela, nem em você.",
            30, 5, 5);
        girondina.f1 = new P(Item(F, "Pista_Girondina_Cartas"), "pista_girondina_1", "Cartas do deputado", Confiabilidade.Fato, "Cidadã Delorme",
            "As cartas falam de família e de dívidas, sem nenhum plano contra a República.");
        girondina.f2 = new P(Item(F, "Pista_Girondina_Retratacao"), "pista_girondina_2", "Denúncia retirada", Confiabilidade.Fato, "Caixa de denúncias",
            "O vizinho que a denunciou retirou a acusação por escrito: queria o apartamento dela.");
        girondina.boato = new P(Item(F, "Pista_Girondina_Boato"), "pista_girondina_boato", "Conversa sobre foragidos", Confiabilidade.Boato, "Redator Marchand",
            "Dizem que a viúva esconde deputados foragidos em casa.");
        girondina.calunia = new P(Item(F, "Pista_Girondina_Calunia"), "pista_girondina_calunia", "Cartaz: envenenadora", Confiabilidade.Calunia, "Parede de editais",
            "'A viúva girondina envenenou um patriota da seção.'");
        girondina.alegacoes = new[]
        {
            new P(Item(F, "Alegacao_Girondina_1"), "alegacao_girondina_1", "Carta de Delorme: sou só uma viúva", Confiabilidade.Boato, "Carta do cliente", "Ela diz que nunca se meteu em política."),
            new P(Item(F, "Alegacao_Girondina_2"), "alegacao_girondina_2", "Carta de Delorme: ele amava a República", Confiabilidade.Boato, "Carta do cliente", "Ela jura que o marido morreu fiel à República."),
        };
        girondina.panfleto = new P(Item(F, "Panfleto_Girondina"), "panfleto_girondina", "Panfleto: A Viúva Girondina", Confiabilidade.NaoEPista, "Tipografia", "Panfleto impresso sobre a viúva Delorme.");
        girondina.fatos = new V(10, 10, 30);
        girondina.comBoato = new V(15, 15, 45, -10, -10, "Nenhum foragido foi achado na casa da viúva. O boato impresso foi desmentido.");
        girondina.comCalunia = new V(20, 20, 60, -15, -15, "O 'patriota envenenado' morreu de febre. A calúnia voltou-se contra a tipografia.");
        girondina.soAlegacoes = new V(4, 4, 12, -5, -5, "O panfleto só repetia a palavra da viúva. Não convenceu ninguém.");

        Npcs.Add(new N { cena = F, id = "RedatorMarchand", nome = "Redator Marchand", falante = "Redator Marchand", x = -6f, cor = new Color(0.6f, 0.2f, 0.2f),
            padrao = new[] { "Não posso falar disso agora, cidadão. Cada palavra minha vira prova no tribunal." } });
        Npcs.Add(new N { cena = F, id = "ComissarioVautrin", nome = "Comissário Vautrin", falante = "Comissário Vautrin", x = 18f, cor = new Color(0.2f, 0.2f, 0.5f),
            padrao = new[] { "O Comitê não tem nada a lhe dizer sobre isso, cidadão. Circule." } });
        Npcs.Add(new N { cena = F, id = "CidadaDelorme", nome = "Cidadã Delorme", falante = "Cidadã Delorme", x = 40f, cor = new Color(0.5f, 0.5f, 0.55f),
            padrao = new[] { "Não sei de nada. Por favor, me deixe em paz." } });
        Npcs.Add(new N { cena = F, id = "Carcereiro", nome = "Carcereiro da Conciergerie", falante = "Carcereiro", x = 62f, cor = new Color(0.35f, 0.3f, 0.25f),
            padrao = new[] { "Na Conciergerie a gente ouve de tudo, cidadão. Mas disso aí, nada." } });

        Reacoes.Add(new R { npc = "RedatorMarchand", caso = jornalista.caminho, item = jornalista.f1.caminho,
            falas = new[] { "Pedi clemência, cidadão. Só isso. Agora querem a minha cabeça.", "Leve os exemplares. Mostre ao tribunal o que eu realmente escrevi.",
                            "Na Conciergerie, o carcereiro conta aos presos que eu pedi a volta do Rei. Nunca escrevi isso!" } });
        Reacoes.Add(new R { npc = "Carcereiro", caso = jornalista.caminho, item = jornalista.boato.caminho,
            falas = new[] { "O redator? Aqui dentro os presos contam que o jornal dele pediu a volta do Rei, num número de outubro.",
                            "Se quer saber do civismo dele, o arquivo da seção guarda os papéis. E a caixa de denúncias vive cheia de bilhetes sobre ele." } });
        Reacoes.Add(new R { npc = "ComissarioVautrin", caso = negociante.caminho, item = negociante.f1.caminho,
            falas = new[] { "O Comitê conta com a sua prensa, cidadão. Garnier rouba o pão do povo.",
                            "Eis o relatório da inspeção: o trigo estava lá, em bom estado, o dobro do que ele declarou.",
                            "O carcereiro da Conciergerie jura que o trigo é podre. Não é: eu mesmo o vi." } });
        Reacoes.Add(new R { npc = "Carcereiro", caso = negociante.caminho, item = negociante.boato.caminho,
            falas = new[] { "Garnier? Dizem que os armazéns dele estão cheios de trigo podre. Deixa o grão estragar só para o povo passar fome.",
                            "Os preços que ele cobrou estão na parede de editais. E no arquivo da seção enfiaram um papel sobre ele e os ingleses." } });
        Reacoes.Add(new R { npc = "CidadaDelorme", caso = girondina.caminho, item = girondina.f1.caminho,
            falas = new[] { "São só cartas de um marido para a esposa. Leia, por favor.", "Se houver conspiração nelas, eu mesma subo ao cadafalso.",
                            "O redator Marchand anda dizendo que as cartas trazem um plano de fuga. Ele nunca as leu." } });
        Reacoes.Add(new R { npc = "RedatorMarchand", caso = girondina.caminho, item = girondina.boato.caminho,
            falas = new[] { "A viúva Delorme? Dizem que as cartas do marido trazem o plano de fuga dos girondinos, e os nomes de quem os esconde.",
                            "Eu não afirmaria isso num jornal. Veja a caixa de denúncias e a parede de editais: o vizinho dela não para de escrever." } });

        Loots.Add(new L { cena = F, id = "ArquivoDaSecao", nome = "Arquivo da Seção", sprite = SpriteMesa, x = -15f, y = -2.1f, escala = 1.2f,
            porCaso = { { jornalista.caminho, jornalista.f2.caminho }, { negociante.caminho, negociante.calunia.caminho } } });
        Loots.Add(new L { cena = F, id = "ParedeDeEditais", nome = "Parede de Editais", sprite = SpritePanfleto, x = 30f, y = 0f, escala = 0.5f,
            porCaso = { { negociante.caminho, negociante.f2.caminho }, { girondina.caminho, girondina.calunia.caminho } } });
        Loots.Add(new L { cena = F, id = "CaixaDeDenuncias", nome = "Caixa de Denúncias", sprite = SpriteDocumento, x = 74f, y = 0f, escala = 0.2f,
            porCaso = { { jornalista.caminho, jornalista.calunia.caminho }, { girondina.caminho, girondina.f2.caminho } } });
    }

    // ===== Execução =====

    [MenuItem("Ferramentas/Campanha/1 - Aplicar conteúdo da campanha (Fases 2 a 4)")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var relatorio = new StringBuilder("[CampanhaSetup] Aplicar conteúdo da campanha\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return relatorio.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return relatorio.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        Definir();
        var contagem = new Contagem();

        // 1. Assets
        foreach (C caso in Casos) GarantirCaso(caso, contagem);
        foreach (C caso in Casos) GarantirConteudoDoCaso(caso, contagem);
        // Guarda CAMINHOS, não referências: abrir outra cena descarrega assets não usados e a referência antiga vira nula.
        var dialogos = new Dictionary<string, string>();
        foreach (N npc in Npcs)
        {
            string caminho = $"{PastaDialogos}/{npc.cena}/{npc.id}_Padrao.asset";
            GarantirDialogo(caminho, npc.falante, npc.padrao, contagem);
            dialogos[npc.id + "|padrao"] = caminho;
        }
        foreach (R r in Reacoes)
        {
            N npc = Npcs.Find(n => n.id == r.npc);
            string caminho = $"{PastaDialogos}/{npc.cena}/{npc.id}_{Path.GetFileNameWithoutExtension(r.caso)}.asset";
            GarantirDialogo(caminho, npc.falante, r.falas, contagem);
            dialogos[r.npc + "|" + r.caso] = caminho;
        }
        foreach (C caso in Casos)
        {
            // Rota de diálogo do cartão: a fala do "dono" do caso (a reação que entrega a pista F1).
            R dono = Reacoes.Find(r => r.caso == caso.caminho && r.item == caso.f1.caminho);
            if (caso.asset.npcDialogueRoute == null && dono != null)
            {
                caso.asset.npcDialogueRoute = AssetDatabase.LoadAssetAtPath<DialogueData>(dialogos[dono.npc + "|" + dono.caso]);
                EditorUtility.SetDirty(caso.asset);
            }
        }
        AssetDatabase.SaveAssets();

        // 2. Cenas
        SceneSetup[] configuracaoOriginal = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string cena in new[] { "Fase2", "Fase3", "Fase4" })
                ConfigurarCena(cena, dialogos, contagem, relatorio);
        }
        finally
        {
            if (configuracaoOriginal != null && configuracaoOriginal.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(configuracaoOriginal);
        }

        // 3. Mesa de Casos e catálogo
        RegistrarNaMesa(contagem);
        CatalogoDeSaveEditor.Atualizar();
        AssetDatabase.SaveAssets();

        relatorio.AppendLine($"Assets criados: {contagem.assetsCriados}; campos preenchidos: {contagem.camposPreenchidos}; " +
                             $"NPCs criados: {contagem.npcsCriados}; objetos criados: {contagem.lootsCriados}; reações adicionadas: {contagem.reacoesAdicionadas}; " +
                             $"migrações de NPC: {contagem.migracoes}; casos novos na mesa: {contagem.casosNaMesa}.");
        return relatorio.ToString();
    }

    private class Contagem
    {
        public int assetsCriados, camposPreenchidos, npcsCriados, lootsCriados, reacoesAdicionadas, migracoes, casosNaMesa;
    }

    // ----- Assets -----

    private static T CarregarOuCriar<T>(string caminho, Contagem contagem, out bool novo) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(caminho);
        novo = asset == null;
        if (!novo) return asset;
        GarantirPasta(Path.GetDirectoryName(caminho).Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, caminho);
        contagem.assetsCriados++;
        return asset;
    }

    private static void GarantirPasta(string pasta)
    {
        if (AssetDatabase.IsValidFolder(pasta)) return;
        string pai = Path.GetDirectoryName(pasta).Replace('\\', '/');
        GarantirPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(pasta));
    }

    private static bool Vazio(string texto) => string.IsNullOrWhiteSpace(texto);

    private static void GarantirCaso(C def, Contagem contagem)
    {
        CaseData caso = CarregarOuCriar<CaseData>(def.caminho, contagem, out bool novo);
        def.asset = caso;
        if (novo)
        {
            caso.caseTitle = def.titulo;
            caso.caseDescription = def.descricao;
            caso.objectiveText = def.objetivo;
            caso.fase = def.fase;
            caso.rota = def.rota;
            caso.nextSceneName = def.cena;
            caso.moneyReward = def.ouro;
            caso.publicOpinionReward = def.povo;
            caso.stateOpinionReward = def.estado;
        }
        else if (EstimativasRevisadas.TryGetValue(caso.name, out int[][] estimativa) &&
                 caso.moneyReward == estimativa[0][0] && caso.publicOpinionReward == estimativa[0][1] && caso.stateOpinionReward == estimativa[0][2])
        {
            // Estimativa da mesa que contradizia a receita (e nunca foi editada): passa a refletir a versão Fatos.
            caso.moneyReward = estimativa[1][0];
            caso.publicOpinionReward = estimativa[1][1];
            caso.stateOpinionReward = estimativa[1][2];
        }
        EditorUtility.SetDirty(caso);
    }

    private static void GarantirConteudoDoCaso(C def, Contagem contagem)
    {
        CaseData caso = def.asset;
        Sprite documento = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDocumento);
        Sprite carta = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteCarta);
        Sprite panfletoImg = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePanfleto);

        foreach (P p in new[] { def.f1, def.f2, def.boato, def.calunia }) GarantirItem(p, caso, documento, contagem);

        var alegacoes = new List<Item>();
        foreach (P p in def.alegacoes) alegacoes.Add(GarantirItem(p, caso, carta, contagem));
        if (caso.alegacoesIniciais == null || caso.alegacoesIniciais.Count == 0)
        {
            caso.alegacoesIniciais = alegacoes;
            contagem.camposPreenchidos++;
        }

        ReceitaDeCaso receita = caso.receitaDoPanfleto != null ? caso.receitaDoPanfleto : CarregarOuCriar<ReceitaDeCaso>(def.receita, contagem, out _);
        if (caso.receitaDoPanfleto == null) { caso.receitaDoPanfleto = receita; contagem.camposPreenchidos++; }
        if (receita.caso == null) { receita.caso = caso; contagem.camposPreenchidos++; }
        if (receita.panfletoPadrao == null && def.panfleto != null)
        {
            receita.panfletoPadrao = GarantirItem(def.panfleto, null, panfletoImg, contagem);
            contagem.camposPreenchidos++;
        }
        else if (receita.panfletoPadrao != null && receita.panfletoPadrao.itemImg == null)
        {
            receita.panfletoPadrao.itemImg = panfletoImg; // panfletos antigos sem arte
            EditorUtility.SetDirty(receita.panfletoPadrao);
            contagem.camposPreenchidos++;
        }
        // Panfletos antigos sem nome de exibição apareciam com o nome do arquivo (ex.: "panfleto_joalheiro").
        if (receita.panfletoPadrao != null && Vazio(receita.panfletoPadrao.itemName) && !Vazio(caso.caseTitle))
        {
            receita.panfletoPadrao.itemName = "Panfleto: " + caso.caseTitle.Trim();
            EditorUtility.SetDirty(receita.panfletoPadrao);
            contagem.camposPreenchidos++;
        }
        PreencherVersao(receita.soFatos, def.fatos, contagem);
        PreencherVersao(receita.comBoato, def.comBoato, contagem);
        PreencherVersao(receita.calunia, def.comCalunia, contagem);
        PreencherVersao(receita.soAlegacoes, def.soAlegacoes, contagem);
        EditorUtility.SetDirty(receita);
        EditorUtility.SetDirty(caso);
    }

    private static bool VersaoVazia(ReceitaDeCaso.Versao v) =>
        v.povo == 0 && v.estado == 0 && v.ouro == 0 && v.penalidadePovo == 0 && v.penalidadeEstado == 0 && Vazio(v.textoRevelacao);

    private static void PreencherVersao(ReceitaDeCaso.Versao versao, V def, Contagem contagem)
    {
        if (versao == null) return;
        if (!Vazio(versao.textoRevelacao) && RevelacoesRevisadas.TryGetValue(versao.textoRevelacao, out string corrigida))
        {
            versao.textoRevelacao = corrigida;
            contagem.camposPreenchidos++;
        }
        if (def == null) return;
        if (!Vazio(def.revelacao) && RevelacoesRevisadas.TryGetValue(def.revelacao, out string corrigidaDef))
            def = new V(def.povo, def.estado, def.ouro, def.penPovo, def.penEstado, corrigidaDef);
        if (VersaoVazia(versao))
        {
            versao.povo = def.povo; versao.estado = def.estado; versao.ouro = def.ouro;
            versao.penalidadePovo = def.penPovo; versao.penalidadeEstado = def.penEstado;
            versao.textoRevelacao = def.revelacao;
            contagem.camposPreenchidos++;
        }
        else if (Vazio(versao.textoRevelacao) && !Vazio(def.revelacao) && (versao.penalidadePovo != 0 || versao.penalidadeEstado != 0))
        {
            versao.textoRevelacao = def.revelacao; // receitas antigas: tinham penalidade, mas nenhuma legenda
            contagem.camposPreenchidos++;
        }
    }

    private static Item GarantirItem(P def, CaseData caso, Sprite imagem, Contagem contagem)
    {
        Item item = CarregarOuCriar<Item>(def.caminho, contagem, out bool novo);
        int antes = contagem.camposPreenchidos;

        // Texto revisado: vale para assets novos e para os que ainda têm o nome original do Prompt 2 (nunca editados).
        if (TextosRevisados.TryGetValue(def.id, out string[] revisado))
        {
            if (!Vazio(item.itemName) && item.itemName == def.nome)
            {
                item.itemName = revisado[0]; item.fonte = revisado[1]; item.descricao = revisado[2];
                contagem.camposPreenchidos++;
            }
            def = new P(def.caminho, def.id, revisado[0], def.conf, revisado[1], revisado[2]);
        }

        if (Vazio(item.itemID)) { item.itemID = def.id; contagem.camposPreenchidos++; }
        if (Vazio(item.itemName)) { item.itemName = def.nome; contagem.camposPreenchidos++; }
        if (Vazio(item.descricao)) { item.descricao = def.descricao; contagem.camposPreenchidos++; }
        if (Vazio(item.fonte)) { item.fonte = def.fonte; contagem.camposPreenchidos++; }
        if (item.itemImg == null && imagem != null) { item.itemImg = imagem; contagem.camposPreenchidos++; }
        if (caso != null && item.caso == null) { item.caso = caso; contagem.camposPreenchidos++; }
        if (caso != null && item.confiabilidade == Confiabilidade.NaoEPista) { item.confiabilidade = def.conf; contagem.camposPreenchidos++; }
        if (novo) item.itemAmt = 1;
        if (novo || contagem.camposPreenchidos != antes) EditorUtility.SetDirty(item);
        return item;
    }

    private static DialogueData GarantirDialogo(string caminho, string falante, string[] falas, Contagem contagem)
    {
        DialogueData dialogo = CarregarOuCriar<DialogueData>(caminho, contagem, out bool novo);
        if (novo || dialogo.talkScript == null || dialogo.talkScript.Count == 0)
        {
            dialogo.talkScript = new List<Dialogue>();
            foreach (string fala in falas)
                dialogo.talkScript.Add(new Dialogue { name = falante, text = fala, choices = new List<Choice>() });
            EditorUtility.SetDirty(dialogo);
        }
        return dialogo;
    }

    // ----- Cenas -----

    private static void ConfigurarCena(string nomeDaCena, Dictionary<string, string> dialogos, Contagem contagem, StringBuilder relatorio)
    {
        string caminho = $"Assets/Scenes/{nomeDaCena}.unity";
        Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
        int alteracoesAntes = contagem.npcsCriados + contagem.lootsCriados + contagem.reacoesAdicionadas + contagem.migracoes;
        bool mudou = false;

        var npcsDaCena = new Dictionary<string, NPCMovement>();
        var lootsDaCena = new Dictionary<string, LootInteractable>();
        foreach (var par in IdDeInteracao.InteracoesDaCena(cena))
        {
            string id = IdDeInteracao.IdEfetivo(par.Key, par.Value);
            if (par.Key is NPCMovement npc) npcsDaCena[id] = npc;
            if (par.Key is LootInteractable loot) lootsDaCena[id] = loot;
        }

        foreach (N def in Npcs.FindAll(n => n.cena == nomeDaCena))
        {
            if (!npcsDaCena.TryGetValue(def.id, out NPCMovement npc))
            {
                if (def.existente)
                {
                    relatorio.AppendLine($"  AVISO {nomeDaCena}: NPC existente '{def.id}' não encontrado (rode Ferramentas > Investigação > Gerar IDs).");
                    continue;
                }
                npc = CriarNpc(def, cena);
                npcsDaCena[def.id] = npc;
                contagem.npcsCriados++;
            }
            ConfigurarNpc(npc, def, dialogos, contagem);
        }

        foreach (L def in Loots.FindAll(l => l.cena == nomeDaCena))
        {
            if (!lootsDaCena.TryGetValue(def.id, out LootInteractable loot))
            {
                loot = CriarLoot(def);
                contagem.lootsCriados++;
            }
            ConfigurarLoot(loot, def, contagem);
        }

        mudou |= TingirCenario(cena, nomeDaCena);
        mudou |= contagem.npcsCriados + contagem.lootsCriados + contagem.reacoesAdicionadas + contagem.migracoes != alteracoesAntes;
        if (mudou)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            relatorio.AppendLine($"  {nomeDaCena}: cena atualizada e salva.");
        }
    }

    private static NPCMovement CriarNpc(N def, Scene cena)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabNpc);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
        go.name = def.nome;
        go.transform.position = new Vector3(def.x, -1.1f, 0f);
        go.transform.localScale = new Vector3(2f, 3f, 2f);
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = def.cor; // placeholder: cápsula colorida, como Jean e o Operário (arte pendente)
            PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        NPCMovement npc = go.GetComponent<NPCMovement>();
        npc.idDaInteracao = def.id;
        npc.dialogoPadrao = null;
        npc.casoObrigatorio = null;
        npc.reacoesDeCaso = new List<CasoReacao>();
        npc.OnDialogueComplete = new UnityEngine.Events.UnityEvent(); // o NPCBasic traz uma chamada vazia herdada
        PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
        return npc;
    }

    private static void ConfigurarNpc(NPCMovement npc, N def, Dictionary<string, string> dialogos, Contagem contagem)
    {
        bool mudou = false;

        // Migração ÚNICA dos NPCs: respondem a qualquer caso (sem casoObrigatorio), continuam acessíveis
        // (disableAfterDialogue falso) e usam uma fala "pouco útil" como padrão — a fala do próprio caso já está na reação.
        // "Única": só enquanto o NPC ainda não recebeu a fala padrão desta ferramenta. Depois disso, o que for
        // mudado no Inspector (casoObrigatorio, disableAfterDialogue, recompensas) é preservado.
        DialogueData padrao = AssetDatabase.LoadAssetAtPath<DialogueData>(dialogos[def.id + "|padrao"]);
        bool primeiraVez = padrao != null && npc.dialogoPadrao != padrao;
        if (primeiraVez)
        {
            if (npc.casoObrigatorio != null) { npc.casoObrigatorio = null; mudou = true; contagem.migracoes++; }
            if (npc.disableAfterDialogue) { npc.disableAfterDialogue = false; mudou = true; contagem.migracoes++; }
            if (!npc.canInteract) { npc.canInteract = true; mudou = true; }
        }
        bool deveTrocar = npc.dialogoPadrao == null || (def.existente && DialogoEhDeReacao(npc, npc.dialogoPadrao));
        if (padrao == null) Debug.LogError($"[CampanhaSetup] Fala padrão de '{def.id}' não carregou.");
        else if (deveTrocar && npc.dialogoPadrao != padrao)
        {
            npc.dialogoPadrao = padrao; mudou = true; contagem.migracoes++;
        }
        if (Vazio(npc.idDaInteracao)) { npc.idDaInteracao = def.id; mudou = true; }
        if (npc.reacoesDeCaso == null) npc.reacoesDeCaso = new List<CasoReacao>();

        // A 2ª pista (F2) dos casos antigos saiu do "dono" e foi para um objeto da rua (mais oportunidades). Também só
        // na primeira vez: se alguém devolver a F2 ao cliente depois, a ferramenta não desfaz.
        for (int i = 0; primeiraVez && i < npc.reacoesDeCaso.Count; i++)
        {
            CasoReacao reacao = npc.reacoesDeCaso[i];
            string caminhoDoCaso = reacao.caso != null ? AssetDatabase.GetAssetPath(reacao.caso) : null;
            C caso = Casos.Find(c => c.caminho == caminhoDoCaso);
            if (caso == null || reacao.recompensasDoDialogo == null) continue;
            Item f1 = AssetDatabase.LoadAssetAtPath<Item>(caso.f1.caminho);
            Item f2 = AssetDatabase.LoadAssetAtPath<Item>(caso.f2.caminho);
            if (reacao.recompensasDoDialogo.Contains(f1) && reacao.recompensasDoDialogo.Contains(f2))
            {
                reacao.recompensasDoDialogo.Remove(f2);
                npc.reacoesDeCaso[i] = reacao;
                mudou = true; contagem.migracoes++;
            }
        }

        foreach (R r in Reacoes.FindAll(x => x.npc == def.id))
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(r.caso);
            if (npc.reacoesDeCaso.Exists(x => x.caso == caso)) continue;
            var reacao = new CasoReacao
            {
                caso = caso,
                dialogoInicialDoCaso = AssetDatabase.LoadAssetAtPath<DialogueData>(dialogos[r.npc + "|" + r.caso]),
                recompensasDoDialogo = new List<Item>(),
            };
            if (!Vazio(r.item)) reacao.recompensasDoDialogo.Add(AssetDatabase.LoadAssetAtPath<Item>(r.item));
            npc.reacoesDeCaso.Add(reacao);
            mudou = true; contagem.reacoesAdicionadas++;
        }

        if (mudou)
        {
            EditorUtility.SetDirty(npc);
            PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
        }
    }

    private static bool DialogoEhDeReacao(NPCMovement npc, DialogueData dialogo)
    {
        if (dialogo == null) return false;
        foreach (CasoReacao r in npc.reacoesDeCaso)
            if (r.dialogoInicialDoCaso == dialogo || r.dialogoComPista == dialogo) return true;
        return false;
    }

    private static LootInteractable CriarLoot(L def)
    {
        var go = new GameObject(def.nome);
        go.transform.position = new Vector3(def.x, def.y, 0f);
        go.transform.localScale = Vector3.one * def.escala;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.sprite);
        sr.sortingLayerName = "personagem";
        sr.sortingOrder = -1;
        var colisor = go.AddComponent<BoxCollider2D>();
        colisor.isTrigger = true;
        if (sr.sprite != null) colisor.size = sr.sprite.bounds.size;
        LootInteractable loot = go.AddComponent<LootInteractable>();
        loot.idDaInteracao = def.id;
        loot.custoEmHoras = 1;
        loot.pistasPossiveis = new List<LootDeCaso>();
        return loot;
    }

    private static void ConfigurarLoot(LootInteractable loot, L def, Contagem contagem)
    {
        bool mudou = false;
        if (loot.pistasPossiveis == null) loot.pistasPossiveis = new List<LootDeCaso>();
        foreach (var par in def.porCaso)
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(par.Key);
            if (loot.pistasPossiveis.Exists(x => x.caso == caso)) continue;
            loot.pistasPossiveis.Add(new LootDeCaso { caso = caso, itemParaDar = AssetDatabase.LoadAssetAtPath<Item>(par.Value) });
            mudou = true; contagem.reacoesAdicionadas++;
        }
        if (mudou) EditorUtility.SetDirty(loot);
    }

    // Fase3 e Fase4 são a mesma rua da Fase2: um tom diferente de luz marca a passagem do tempo (arte pendente).
    private static bool TingirCenario(Scene cena, string nomeDaCena)
    {
        Color tom;
        if (nomeDaCena == "Fase3") tom = new Color(0.9f, 0.78f, 0.66f);      // entardecer de 1789–1792
        else if (nomeDaCena == "Fase4") tom = new Color(0.66f, 0.7f, 0.82f); // inverno do Terror, 1793
        else return false;

        bool mudou = false;
        foreach (GameObject raiz in cena.GetRootGameObjects())
        {
            if (raiz.name != "rua_0" && raiz.name != "chao-rua") continue;
            var sr = raiz.GetComponent<SpriteRenderer>();
            if (sr == null || sr.color != Color.white) continue;
            sr.color = tom;
            EditorUtility.SetDirty(sr);
            mudou = true;
        }
        return mudou;
    }

    // ----- Mesa de Casos -----

    private static void RegistrarNaMesa(Contagem contagem)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            CaseSelectionUI mesa = raiz.GetComponentInChildren<CaseSelectionUI>(true);
            if (mesa == null) { Debug.LogError("[CampanhaSetup] CaseSelectionUI não encontrado no UI.prefab."); return; }
            if (mesa.availableCases == null) mesa.availableCases = new List<CaseData>();
            bool mudou = false;
            foreach (C def in Casos)
            {
                CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(def.caminho);
                if (caso == null || mesa.availableCases.Contains(caso)) continue;
                mesa.availableCases.Add(caso);
                contagem.casosNaMesa++;
                mudou = true;
            }
            if (mudou) PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }
}
