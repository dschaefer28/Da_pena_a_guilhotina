using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Conteúdo narrativo das Fases 2, 3 e 4 alinhado ao GDD ("Da Pena à Guilhotina", IFPR, 2026), com a planilha
/// "30 Casos Reais" como referência histórica. Troca textos, diálogos, NPCs e reações dos nove casos da campanha,
/// sem mexer em mecânicas: os arquivos de caso, os itemID, os IDs de interação, as rotas, as receitas (valores), os
/// preços e as regras de bloqueio continuam os mesmos. O save guarda casos pelo nome do asset e pistas pelo itemID,
/// por isso os arquivos mantêm os nomes antigos (ex.: Caso_Varennes é o caso de Kornmann). A correspondência está em
/// docs/MATRIZ_DE_CASOS_TCC.md.
///
///   Fase 2: O Colar da Rainha (Caso_Joalheiro), Os Trabalhadores de Réveillon (Caso_Reveillon) e O Operário Acusado
///           (Caso_Operario), as duas perspectivas do motim de abril de 1789.
///   Fase 3: O Julgamento de Georges Danton (Caso_ChampDeMars), Adultério e Poder Ministerial (Caso_Varennes) e
///           Morte no Poço e Intolerância (Caso_Assignats). NPCs: Danton (novo), Kornmann, Sirven, Sobrevivente, Médico.
///   Fase 4: O Manifesto da Clemência (Caso_Jornalista, rota A), O Caso do Pequeno Mercador de Grãos (Caso_Negociante,
///           rota B) e O Extermínio da Oposição (Caso_Girondina, rota C). NPCs: Desmoulins, Mercador, Vergniaud, Brissot.
///
/// Dedução (mesma regra das ferramentas 5 e 7): as duas pistas de cada caso no GDD são os dois fatos; o boato contradiz
/// o fato da testemunha/cliente e a calúnia contradiz o fato do objeto. O cliente aponta quem espalha o boato (e a
/// testemunha, quando não é ele quem entrega a pista), e quem espalha o boato aponta os dois objetos.
///
/// Idempotente e conservadora: um caso só é reescrito se ainda tiver o título anterior ao GDD; com o título do GDD,
/// nada é tocado (edições feitas depois no Inspector ficam); com outro título, o caso é listado como "mantido".
/// Menu: Ferramentas > Campanha > 8 - Aplicar conteúdo narrativo do GDD (Fases 2 a 4)
/// </summary>
public static class NarrativaGddSetupTool
{
    private const string PastaCasos = "Assets/Casos/";
    private const string SO = "Assets/Scriptableobjects/";
    private const string P2 = "Assets/Scriptableobjects/Campanha/Fase2/";
    private const string P3 = "Assets/Scriptableobjects/Campanha/Fase3/";
    private const string P4 = "Assets/Scriptableobjects/Campanha/Fase4/";
    private const string OF = "Assets/Scriptableobjects/Campanha/Biblioteca/";
    private const string D2 = "Assets/Dialogos/Campanha/Fase2/";
    private const string D3 = "Assets/Dialogos/Campanha/Fase3/";
    private const string D4 = "Assets/Dialogos/Campanha/Fase4/";
    private const string DJoa = "Assets/Dialogos/Joalheiro/";
    private const string DRev = "Assets/Dialogos/Jean-Baptiste Réveillon/";
    private const string DOpe = "Assets/Dialogos/Operário/";

    // Falantes (o nome aparece na caixa de diálogo; o do protagonista vira a caixa para o lado dele).
    private const string J = "Julien Valois";
    private const string JOA = "Joalheiro", REV = "Jean Réveillon", OPE = "Operário", GAZ = "Gazeteiro";
    private const string DAN = "Georges Danton", KOR = "Guillaume Kornmann", SIR = "Pierre-Paul Sirven", SOB = "Sobrevivente", MED = "Médico";
    private const string DES = "Camille Desmoulins", MER = "Mercador", VER = "Pierre Vergniaud", BRI = "Jacques-Pierre Brissot";

    // Casos (arquivo sem extensão)
    private const string Joias = "Caso_Joalheiro", Reveillon = "Caso_Reveillon", Operario = "Caso_Operario";
    private const string Danton = "Caso_ChampDeMars", Kornmann = "Caso_Varennes", Sirven = "Caso_Assignats";
    private const string Desmoulins = "Caso_Jornalista", Mercador = "Caso_Negociante", Girondinos = "Caso_Girondina";

    // ===== Modelo =====

    private sealed class CasoGdd
    {
        public string arquivo, cena, tituloAnterior, titulo, objetivo, descricao, rotaDeDialogo;
        public string revelacaoComBoato, revelacaoCalunia, revelacaoAlegacoes;
        public string oferta, ofertaTitulo, ofertaDescricao; // biblioteca (Fases 3 e 4)
    }

    private sealed class ItemGdd { public string caso, caminho, id, nome, fonte, descricao; }

    private sealed class NoGdd
    {
        public string caso, caminho;
        public readonly List<string[]> falas = new List<string[]>();   // { falante, texto }
        public readonly List<string[]> opcoes = new List<string[]>();  // { texto do botão, caminho do próximo nó }
        public NoGdd F(string falante, string texto) { falas.Add(new[] { falante, texto }); return this; }
        public NoGdd Op(string texto, string destino) { opcoes.Add(new[] { texto, destino }); return this; }
    }

    private sealed class PersonagemGdd
    {
        public string caso, cena, id, nomeAnterior, nome, padrao;
        public bool novo;
        public float x;
        public Color cor;
    }

    private sealed class ReacaoGdd { public string cena, npc, caso, entrada, depois; public string[] itens; }

    private sealed class ObjetoGdd
    {
        public string caso, cena, id, nomeAnterior, nome;
        public readonly List<string[]> porCaso = new List<string[]>(); // { caso, caminho do item }
    }

    private static readonly List<CasoGdd> Casos = new List<CasoGdd>();
    private static readonly List<ItemGdd> Itens = new List<ItemGdd>();
    private static readonly List<NoGdd> Nos = new List<NoGdd>();
    private static readonly List<PersonagemGdd> Personagens = new List<PersonagemGdd>();
    private static readonly List<ReacaoGdd> Reacoes = new List<ReacaoGdd>();
    private static readonly List<ObjetoGdd> Objetos = new List<ObjetoGdd>();

    private static NoGdd No(string caso, string caminho)
    {
        var no = new NoGdd { caso = caso, caminho = caminho };
        Nos.Add(no);
        return no;
    }

    private static void It(string caso, string caminho, string id, string nome, string fonte, string descricao) =>
        Itens.Add(new ItemGdd { caso = caso, caminho = caminho, id = id, nome = nome, fonte = fonte, descricao = descricao });

    private static void R(string cena, string npc, string caso, string entrada, string depois = null, params string[] itens) =>
        Reacoes.Add(new ReacaoGdd { cena = cena, npc = npc, caso = caso, entrada = entrada, depois = depois, itens = itens ?? new string[0] });

    private static void Garantir()
    {
        if (Casos.Count > 0) return;
        DefinirFase2();
        DefinirFase3();
        DefinirFase4();
    }

    // ===== Fase 2 (1789) =====

    private static void DefinirFase2()
    {
        const string F = "Fase2";

        // ----- O Colar da Rainha -----
        // GDD: o duque de Orléans procura a tipografia para manchar a reputação da Rainha e manda na carta o retrato
        // "Marie Antoinette in a Chemise Dress". O joalheiro é testemunha; as falas dele são o roteiro do grupo (27/09),
        // que já segue o GDD e não muda aqui.
        Casos.Add(new CasoGdd
        {
            arquivo = Joias, cena = F, tituloAnterior = "Caso das Joias", titulo = "O Colar da Rainha",
            objetivo = "Descobrir quem negociou o colar em nome da Rainha e o que a encomenda realmente prova.",
            descricao = "1789. O escândalo do colar de diamantes, julgado em 1786, volta às ruas. O duque de Orléans, primo do Rei e rival de " +
                        "Maria Antonieta, encomenda um panfleto para manchar a reputação dela e anexa um retrato da Rainha. O colar foi vendido " +
                        "em nome dela: descubra quem negociou a compra e o que a encomenda prova.",
            rotaDeDialogo = DJoa + "Joalheiro_Dialogo.asset",
            revelacaoComBoato = "O registro de venda mostrou que a Rainha nunca foi à loja: o boato de que ela escolheu o colar disfarçada caiu por terra, e o panfleto que o repetiu também.",
            revelacaoCalunia = "A encomenda trazia uma assinatura que a Rainha nunca usou: a folha que a acusava de assinar para o Cardeal era calúnia, e a tipografia ganhou fama de caluniadora.",
            revelacaoAlegacoes = "O panfleto só repetia as acusações do duque de Orléans, sem prova alguma. Os leitores viram nele mais um libelo encomendado e o esqueceram depressa.",
        });
        It(Joias, P2 + "Alegacao_Joias_1.asset", "alegacao_joias_1", "Carta do duque: a Rainha quis o colar", "Carta do cliente",
            "O duque de Orléans afirma que a Rainha cobiçou o colar e depois fingiu não saber de nada. Diz que o gazeteiro da esquina conhece a história que Paris comenta.");
        It(Joias, P2 + "Alegacao_Joias_2.asset", "alegacao_joias_2", "Carta do duque: o retrato da Rainha", "Carta do cliente",
            "O duque anexa uma gravura do retrato de Maria Antonieta em vestido de musselina, pintado por Vigée Le Brun e retirado do Salão de 1783 depois das críticas. Para ele, o retrato prova que a Rainha é leviana.");
        It(Joias, SO + "panfleto_joalheiro.asset", "panfleto_joias", "Panfleto: O Colar da Rainha", "Tipografia",
            "Panfleto da tipografia sobre o colar vendido em nome da Rainha.");

        No(Joias, D2 + "Gazeteiro_Caso_Joalheiro.asset")
            .F(GAZ, "Gazetas! O colar da Rainha outra vez! Desde que aquela condessa fugiu da prisão e escreveu suas memórias, Paris não fala de outra coisa.")
            .F(J, "E o que dizem nas tavernas?")
            .F(GAZ, "Que a própria Rainha foi à loja do joalheiro, disfarçada, escolher o colar escondida do Rei. Depois negou tudo.")
            .F(GAZ, "E no muro de cartazes colaram uma folha contando coisa pior, sobre ela e o Cardeal.")
            .Op("Quem viu a Rainha na loja?", D2 + "Gazeteiro_Caso_Joalheiro_R1.asset")
            .Op("Por que o povo acredita tão fácil?", D2 + "Gazeteiro_Caso_Joalheiro_R2.asset");
        No(Joias, D2 + "Gazeteiro_Caso_Joalheiro_R1.asset")
            .F(GAZ, "Ninguém que eu conheça. Mas todo mundo jura que alguém viu, e numa taverna isso vale como prova.");
        No(Joias, D2 + "Gazeteiro_Caso_Joalheiro_R2.asset")
            .F(GAZ, "Porque o pão está pela hora da morte e ela gasta como se o Tesouro fosse dela. 'Madame Déficit', é como a chamam. Qualquer história contra ela vende.");
        No(Joias, D2 + "Jean-Baptiste Réveillon_Caso_Joalheiro.asset")
            .F(REV, "O colar? Um milhão e seiscentas mil libras em diamantes, rapaz. Eu forro de papel pintado os salões de Paris e sei quanto vale o luxo.")
            .F(REV, "Mas quem vendeu e quem comprou, pergunte ao joalheiro. Eu já tenho problemas demais.");
        No(Joias, D2 + "Operario_Caso_Joalheiro.asset")
            .F(OPE, "Um colar de diamantes? Com o preço de uma pedra só, o bairro inteiro comia um ano.")
            .F(OPE, "Disso eu não sei nada, senhor. Só sei que o pão de quatro libras já custa quase metade do que ganho num dia.");

        // ----- Os Trabalhadores de Réveillon (a perspectiva do dono) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Reveillon, cena = F, tituloAnterior = "Caso de Jean-Baptiste Réveillon", titulo = "Os Trabalhadores de Réveillon",
            objetivo = "Descobrir o que Réveillon realmente propôs na assembleia e como ele tratava seus operários.",
            descricao = "Abril de 1789. Jean-Baptiste Réveillon, dono de uma próspera manufatura de papéis de parede no faubourg Saint-Antoine, " +
                        "falou de salários e do preço do pão numa assembleia eleitoral. Dias depois, uma multidão faminta saqueou sua casa e sua " +
                        "fábrica, e a Guarda Francesa atirou contra ela. Réveillon quer que a tipografia limpe seu nome.",
            rotaDeDialogo = DRev + "Jean_Dialogo.asset",
            revelacaoComBoato = CampanhaSetupTool.RevelacaoDoBoatoDeReveillon,
            revelacaoCalunia = "Ninguém encontrou prova de que Réveillon servia aos ingleses: a calúnia voltou-se contra a tipografia.",
            revelacaoAlegacoes = "O panfleto sobre Réveillon só repetia a defesa dele. Nem a Corte lhe deu crédito.",
        });
        It(Reveillon, SO + "Item_Jean1.asset", "pista_jean_1", "O preço do pão", "Ata da assembleia eleitoral",
            "Cópia da ata da assembleia do distrito de Sainte-Marguerite: Réveillon propôs abolir as taxas de entrada nas barreiras de Paris para baratear o pão e, com o pão mais barato, baixar os salários e o preço das manufaturas.");
        It(Reveillon, P2 + "Pista_Jean_Boato.asset", "pista_jean_boato", "Os quinze soldos", "Conversa de operários",
            "Segundo os operários, Réveillon disse na assembleia que um operário vive muito bem com quinze soldos por dia e pediu só o corte dos salários, sem uma palavra sobre o pão.");
        It(Reveillon, SO + "Item_Jean2.asset", "pista_jean_2", "Os salários da manufatura", "Gazeta vendida na banca",
            "Cópia do livro de salários publicada por uma gazeta: Réveillon pagava acima da média do bairro e manteve os salários durante o inverno rigoroso de 1788, com o trabalho parado.");
        It(Reveillon, P2 + "Pista_Jean_Calunia.asset", "pista_jean_calunia", "O dinheiro inglês", "Folha anônima vendida na rua",
            "Folha vendida pelo gazeteiro: no inverno, Réveillon cortou pela metade o salário da manufatura e mandou a diferença para a Inglaterra, que lhe paga para matar Paris de fome.");
        It(Reveillon, P2 + "Alegacao_Jean_1.asset", "alegacao_jean_1", "Carta de Réveillon: fui mal entendido", "Carta do cliente",
            "Réveillon garante que suas palavras na assembleia foram distorcidas, e que os operários repetem a versão errada.");
        It(Reveillon, P2 + "Alegacao_Jean_2.asset", "alegacao_jean_2", "Carta de Réveillon: nunca quis o mal deles", "Carta do cliente",
            "Ele afirma que sempre tratou bem os seus operários.");
        It(Reveillon, SO + "panfleto_jean.asset", "panfleto_jean", "Panfleto: Os Trabalhadores de Réveillon", "Tipografia",
            "Panfleto da tipografia sobre Réveillon e o motim do faubourg Saint-Antoine.");

        // Diálogo do cliente: sequências 1 a 3 do GDD, com as duas escolhas de cada uma levando a respostas diferentes.
        No(Reveillon, DRev + "Jean_Dialogo.asset")
            .F(REV, "Ah, outro não! Eu não mandei atirar em ninguém!")
            .Op("Senhor, apenas vim em busca de resposta e da verdade.", DRev + "resposta jean.asset")
            .Op("Por que está tão nervoso, se afirma não ter feito nada?", DRev + "respostaJeanNervoso.asset");
        No(Reveillon, DRev + "resposta jean.asset")
            .F(REV, "A verdade? Em Paris cada um tem a sua, e a minha não vende gazetas.")
            .F(REV, "Tsk. O que quer?")
            .Op("O que aconteceu com sua fábrica?", DRev + "respostaJean1.asset")
            .Op("O que o senhor fez para revoltar os trabalhadores?", DRev + "respostaJeanRevolta.asset");
        No(Reveillon, DRev + "respostaJeanNervoso.asset")
            .F(REV, "Nervoso? Queimaram meus móveis na rua, saquearam minha casa, e tive de me esconder na Bastilha para não ser linchado!")
            .F(REV, "E ainda me chamam de assassino. Tsk. O que quer?")
            .Op("O que aconteceu com sua fábrica?", DRev + "respostaJean1.asset")
            .Op("O que o senhor fez para revoltar os trabalhadores?", DRev + "respostaJeanRevolta.asset");
        const string contraArgumento = "Então o senhor acha que, se diminuir os salários, os preços caem? Os salários estarão menores na mesma proporção. Será o mesmo.";
        No(Reveillon, DRev + "respostaJean1.asset")
            .F(REV, "Na assembleia do distrito, eu disse que, sem as taxas cobradas nas barreiras da cidade, o pão ficaria mais barato. E, com o pão barato, os salários podiam baixar, e tudo o que se fabrica em Paris sairia mais em conta.")
            .F(REV, "Mas eles pensaram que eu só queria diminuir por diminuir. Falta clareza a esse povo.")
            .Op(contraArgumento, DRev + "respostaJean2.asset");
        No(Reveillon, DRev + "respostaJeanRevolta.asset")
            .F(REV, "Revoltar? Eu dou trabalho a trezentas pessoas!")
            .F(REV, "Falei em baixar os salários para baratear tudo, principalmente o pão. E esses animais não conseguiram entender: vieram, quebraram a fábrica, invadiram minha casa. Tudo por causa da mente limitada.")
            .Op(contraArgumento, DRev + "respostaJean2.asset");
        No(Reveillon, DRev + "respostaJean2.asset")
            .F(REV, "Eu… não tinha pensado nisso.")
            .F(J, "Se o pão não baixar, quem ganha menos come menos. Foi isso que o bairro ouviu.")
            .F(REV, "Então leve a cópia da ata da assembleia. Está escrito ali o que eu propus: o pão e os salários, juntos.")
            .F(REV, "E pergunte aos operários o que andam repetindo. Garanto que nenhum deles leu a ata.");
        No(Reveillon, DRev + "respostaJeanDepois.asset")
            .F(REV, "Já lhe dei a ata. Leia com atenção: o pão está lá, ao lado dos salários.");

        No(Reveillon, D2 + "Operario_Caso_Reveillon.asset")
            .F(OPE, "Aquele homem? Disse na assembleia que um operário vive muito bem com quinze soldos por dia!")
            .F(OPE, "Só queria cortar o nosso salário. Do pão, nem uma palavra.")
            .F(OPE, "Na banca de panfletos vendem uma gazeta com as contas da manufatura, e o gazeteiro da esquina tem uma folha ainda pior sobre ele.")
            .Op("Você ouviu isso na assembleia?", D2 + "Operario_Caso_Reveillon_R1.asset")
            .Op("Por que isso revoltou tanto o bairro?", D2 + "Operario_Caso_Reveillon_R2.asset");
        No(Reveillon, D2 + "Operario_Caso_Reveillon_R1.asset")
            .F(OPE, "Eu? Operário não vota, senhor. Só entra na assembleia quem paga imposto. Mas o faubourg inteiro repete a mesma coisa.");
        No(Reveillon, D2 + "Operario_Caso_Reveillon_R2.asset")
            .F(OPE, "Porque o pão de quatro libras já custa quatorze soldos e meio. Com quinze soldos por dia, quem tem filhos passa fome.");
        No(Reveillon, D2 + "Gazeteiro_Caso_Reveillon.asset")
            .F(GAZ, "Réveillon? Tenho aqui uma folha que vende como pão quente.")
            .F(GAZ, "Diz que no inverno ele cortou pela metade o salário da manufatura e mandou a diferença para os ingleses. Leve, é sua.")
            .Op("Quem escreveu isso?", D2 + "Gazeteiro_Caso_Reveillon_R1.asset")
            .Op("E por que os ingleses pagariam Réveillon?", D2 + "Gazeteiro_Caso_Reveillon_R2.asset");
        No(Reveillon, D2 + "Gazeteiro_Caso_Reveillon_R1.asset")
            .F(GAZ, "Não tem assinatura. Quem assina uma folha dessas acaba na Bastilha.");
        No(Reveillon, D2 + "Gazeteiro_Caso_Reveillon_R2.asset")
            .F(GAZ, "Porque o papel de parede dele concorre com o inglês... ou porque a história vende. Eu não pergunto, eu vendo.");
        No(Reveillon, D2 + "Joalheiro_Caso_Reveillon.asset")
            .F(JOA, "Réveillon? Bom cliente. Forra de papel pintado os mesmos salões que eu enfeito de joias.")
            .F(JOA, "O que ele disse na assembleia, não sei. Só sei que ninguém compra luxo com o povo gritando por pão na rua.");

        // ----- O Operário Acusado (a perspectiva do sobrevivente) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Operario, cena = F, tituloAnterior = "Caso do Operário", titulo = "O Operário Acusado",
            objetivo = "Descobrir como a Guarda agiu contra a multidão e o que sustenta a acusação contra o operário.",
            descricao = "Abril de 1789. Um operário do faubourg Saint-Antoine sobreviveu ao massacre na manufatura de Réveillon e agora é procurado " +
                        "por participar do levante. Ele diz que a multidão só pedia pão e salário, e que a Guarda Francesa atirou para matar. " +
                        "Ele pede ajuda para escapar da condenação.",
            rotaDeDialogo = DOpe + "Operario_Dialogo.asset",
            revelacaoComBoato = "Nenhum agitador pago foi encontrado. O boato publicado pela tipografia não se sustentou.",
            revelacaoCalunia = "Ninguém provou que o operário ateou fogo à fábrica: a acusação era anônima e falsa, e manchou o nome da tipografia.",
            revelacaoAlegacoes = "O panfleto do operário só repetia o que ele contou. Faltou prova, e o caso esfriou.",
        });
        It(Operario, SO + "Item_operario1.asset", "pista_operario_1", "Desejo de morte", "O próprio operário",
            "Segundo o operário, a Guarda Francesa chegou atirando para matar, e não para dispersar: a multidão, sem armas de fogo, pedia pão e salário, e ele viu os companheiros caírem.");
        It(Operario, P2 + "Pista_Operario_Boato.asset", "pista_operario_boato", "Os agitadores do duque", "Jean-Baptiste Réveillon",
            "Segundo Réveillon, ninguém ali pedia pão: a multidão veio armada, paga na véspera por agitadores do duque de Orléans, e a Guarda só atirou para se defender.");
        It(Operario, SO + "Item_operario2.asset", "pista_operario_2", "A ordem do comandante", "Cartaz arrancado de um muro",
            "Cartaz com a ordem do comandante da Guarda, 'dispersar a multidão por todos os meios', afixado antes de a multidão chegar à manufatura.");
        It(Operario, P2 + "Pista_Operario_Calunia.asset", "pista_operario_calunia", "O fogo na fábrica", "Bilhete sem assinatura",
            "Bilhete deixado na taverna: a Guarda só recebeu ordem de agir depois que o operário ateou fogo à fábrica com as próprias mãos.");
        It(Operario, P2 + "Alegacao_Operario_1.asset", "alegacao_operario_1", "Carta do operário: só queríamos pão", "Carta do cliente",
            "O operário jura que a multidão só pedia comida e salário.");
        It(Operario, P2 + "Alegacao_Operario_2.asset", "alegacao_operario_2", "Carta do operário: atiraram primeiro", "Carta do cliente",
            "Ele afirma que a Guarda abriu fogo antes de qualquer pedra ser atirada, e que o próprio Réveillon anda contando outra versão.");
        It(Operario, SO + "panfleto_operario.asset", "panfleto_operario", "Panfleto: O Operário Acusado", "Tipografia",
            "Panfleto da tipografia em defesa do operário do faubourg Saint-Antoine.");

        No(Operario, DOpe + "Operario_Dialogo.asset")
            .F(OPE, "Obrigado por me ajudar, senhor. Serei eternamente grato.")
            .Op("Mas me diga, como tudo aconteceu?", DOpe + "resposta_operario.asset")
            .Op("Do que exatamente o acusam?", DOpe + "respostaOperarioAcusacao.asset");
        No(Operario, DOpe + "resposta_operario.asset")
            .F(OPE, "O senhor da fábrica queria diminuir mais ainda os salários. Eles não cansam de ter dinheiro!")
            .F(OPE, "Na segunda-feira, queimamos um boneco dele na praça. Na terça, voltamos.")
            .Op("E o que aconteceu na terça?", DOpe + "repostaOperario1.asset");
        No(Operario, DOpe + "respostaOperarioAcusacao.asset")
            .F(OPE, "Dizem que eu estava entre os que entraram na casa do Réveillon e quebraram tudo.")
            .F(OPE, "Eu estava na rua, gritando por pão como todo mundo. Já enforcaram companheiros para servir de exemplo.")
            .Op("Conte como tudo aconteceu.", DOpe + "repostaOperario1.asset");
        No(Operario, DOpe + "repostaOperario1.asset")
            .F(OPE, "Nós nos reunimos. Precisávamos de comida. Pouco tempo depois, os guardas chegaram... e, Deus, como havia sangue.")
            .Op("Como assim?", DOpe + "respostaOperario2.asset");
        No(Operario, DOpe + "respostaOperario2.asset")
            .F(OPE, "Os guardas não queriam afastar a gente, queriam nos matar. Eu não sei como sobrevivi. Todos os meus companheiros, meus amigos, morreram como ratos.")
            .F(OPE, "E agora o Réveillon anda dizendo que éramos pagos pelo duque de Orléans. Que diga isso na minha cara!");
        No(Operario, DOpe + "respostaOperarioDepois.asset")
            .F(OPE, "Já lhe contei tudo o que vi. Quem conta outra versão que prove o que diz.");

        No(Operario, D2 + "Jean-Baptiste Réveillon_Caso_Operario.asset")
            .F(REV, "Aqueles homens não pediam pão: vieram armados, pagos na véspera por agitadores do duque de Orléans!")
            .F(REV, "A Guarda só atirou para se defender. Anote isso.")
            .F(REV, "Colaram a ordem da Guarda no muro de cartazes, e na mesa da taverna deixaram um bilhete sobre o incêndio da minha fábrica. Cada um conta de um jeito.")
            .Op("O senhor viu os agitadores?", D2 + "Jean-Baptiste Réveillon_Caso_Operario_R1.asset")
            .Op("Por que o duque de Orléans pagaria uma revolta?", D2 + "Jean-Baptiste Réveillon_Caso_Operario_R2.asset");
        No(Operario, D2 + "Jean-Baptiste Réveillon_Caso_Operario_R1.asset")
            .F(REV, "Ver? Eu estava escondido, rapaz. Mas meu criado jura que distribuíam moedas na praça.");
        No(Operario, D2 + "Jean-Baptiste Réveillon_Caso_Operario_R2.asset")
            .F(REV, "Porque o Palais-Royal dele é um ninho de panfletários, e todo mundo sabe que ele gostaria de mandar mais que o primo.");
        No(Operario, D2 + "Joalheiro_Caso_Operario.asset")
            .F(JOA, "O motim do faubourg? Fechei a loja por três dias. Quando a fome sai à rua, joalheiro é o primeiro a baixar as portas.")
            .F(JOA, "Do operário não sei nada, rapaz.");
        No(Operario, D2 + "Gazeteiro_Caso_Operario.asset")
            .F(GAZ, "O massacre do faubourg? Cada folha conta um número de mortos: vinte e cinco, cem, trezentos... Eu vendo todas.")
            .F(GAZ, "Mas sobre o seu operário não ouvi nada que valha um soldo.");

        // Reações (todo NPC responde a todo caso; a fala padrão fica para quando não há caso).
        R(F, "Joalheiro", Joias, DJoa + "Joalheiro_Dialogo.asset", DJoa + "respostaJoalheiro1.asset", SO + "Item_joalheiro1.asset", SO + "Item_joalheiro2.asset");
        R(F, "Joalheiro", Reveillon, D2 + "Joalheiro_Caso_Reveillon.asset");
        R(F, "Joalheiro", Operario, D2 + "Joalheiro_Caso_Operario.asset");
        R(F, "Jean-Baptiste Réveillon", Reveillon, DRev + "Jean_Dialogo.asset", DRev + "respostaJeanDepois.asset", SO + "Item_Jean1.asset");
        R(F, "Jean-Baptiste Réveillon", Operario, D2 + "Jean-Baptiste Réveillon_Caso_Operario.asset", null, P2 + "Pista_Operario_Boato.asset");
        R(F, "Jean-Baptiste Réveillon", Joias, D2 + "Jean-Baptiste Réveillon_Caso_Joalheiro.asset");
        R(F, "Operario", Operario, DOpe + "Operario_Dialogo.asset", DOpe + "respostaOperarioDepois.asset", SO + "Item_operario1.asset");
        R(F, "Operario", Reveillon, D2 + "Operario_Caso_Reveillon.asset", null, P2 + "Pista_Jean_Boato.asset");
        R(F, "Operario", Joias, D2 + "Operario_Caso_Joalheiro.asset");
        R(F, "Gazeteiro", Joias, D2 + "Gazeteiro_Caso_Joalheiro.asset", null, P2 + "Pista_Joias_Boato.asset");
        R(F, "Gazeteiro", Reveillon, D2 + "Gazeteiro_Caso_Reveillon.asset", null, P2 + "Pista_Jean_Calunia.asset");
        R(F, "Gazeteiro", Operario, D2 + "Gazeteiro_Caso_Operario.asset");

        // Objetos da rua (sem mudança de estrutura na Fase 2).
        var banca = new ObjetoGdd { cena = F, id = "BancaDePanfletos" };
        banca.porCaso.Add(new[] { Reveillon, SO + "Item_Jean2.asset" });
        var muro = new ObjetoGdd { cena = F, id = "MuroDeCartazes" };
        muro.porCaso.Add(new[] { Joias, P2 + "Pista_Joias_Calunia.asset" });
        muro.porCaso.Add(new[] { Operario, SO + "Item_operario2.asset" });
        var taverna = new ObjetoGdd { cena = F, id = "MesaDaTaverna" };
        taverna.porCaso.Add(new[] { Operario, P2 + "Pista_Operario_Calunia.asset" });
        Objetos.AddRange(new[] { banca, muro, taverna });
    }

    // ===== Fase 3 (1791–1792) =====

    private static void DefinirFase3()
    {
        const string F = "Fase3";

        // IDs de interação e arquivos de diálogo seguem os NPCs antigos (o save aponta para eles):
        // ViuvaFrancois = Médico, CocheiroJoubert = Kornmann, PeticionariaLacombe = Sobrevivente, GravadorMorel = Sirven.
        Personagens.Add(new PersonagemGdd { caso = Sirven, cena = F, id = "ViuvaFrancois", nomeAnterior = "Viúva François", nome = "Médico", padrao = D3 + "ViuvaFrancois_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Kornmann, cena = F, id = "CocheiroJoubert", nomeAnterior = "Cocheiro Joubert", nome = "Guillaume Kornmann", padrao = D3 + "CocheiroJoubert_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Danton, cena = F, id = "PeticionariaLacombe", nomeAnterior = "Peticionária Lacombe", nome = "Sobrevivente", padrao = D3 + "PeticionariaLacombe_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Sirven, cena = F, id = "GravadorMorel", nomeAnterior = "Gravador Morel", nome = "Pierre-Paul Sirven", padrao = D3 + "GravadorMorel_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Danton, cena = F, id = "GeorgesDanton", nome = "Georges Danton", padrao = D3 + "GeorgesDanton_Padrao.asset",
                                            novo = true, x = 46f, cor = new Color(0.55f, 0.25f, 0.2f) });

        No(Sirven, D3 + "ViuvaFrancois_Padrao.asset").F(MED, "Sou médico, não advogado. Se é sobre doenças, estou às ordens; se é sobre processos, não posso ajudar.");
        No(Kornmann, D3 + "CocheiroJoubert_Padrao.asset").F(KOR, "Tenho negócios a tratar, meu caro. Se não é sobre o meu processo, não tenho tempo.");
        No(Danton, D3 + "PeticionariaLacombe_Padrao.asset").F(SOB, "Desde o Champ de Mars eu não falo com estranhos. Me desculpe.");
        No(Sirven, D3 + "GravadorMorel_Padrao.asset").F(SIR, "Perdão, senhor. Um homem na minha situação não pode se meter em mais nada.");
        No(Danton, D3 + "GeorgesDanton_Padrao.asset").F(DAN, "Hoje não, cidadão. Tenho mais inimigos do que horas no dia.");

        // ----- O Julgamento de Georges Danton (Caso_ChampDeMars) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Danton, cena = F, tituloAnterior = "O Fuzilamento do Champ de Mars", titulo = "O Julgamento de Georges Danton",
            objetivo = "Provar se Danton incitou uma insurreição armada ou exerceu o direito de petição.",
            descricao = "1791. Depois do massacre do Champ de Mars, a justiça expede mandados de prisão contra líderes dos Cordeliers. " +
                        "Georges Danton é acusado de incitar uma insurreição armada. Ele diz que só defendeu uma petição à Assembleia e quer " +
                        "que a tipografia mostre que a lei protege quem assina e se reúne em paz.",
            rotaDeDialogo = D3 + "GeorgesDanton_Caso_ChampDeMars.asset",
            revelacaoComBoato = "As folhas originais da petição, com milhares de assinaturas, desmentiram o boato: no altar da pátria se assinava uma petição quando a Guarda chegou. O panfleto que falava em insurreição ruiu.",
            revelacaoCalunia = "A Constituição garantia a reunião pacífica e a petição assinada: o bilhete que chamava Danton de criminoso era calúnia, e a tipografia foi acusada de caluniar os mortos.",
            revelacaoAlegacoes = "O panfleto só repetia a palavra de Danton. Sem provas, pareceu discurso de clube, e ninguém mudou de ideia.",
            oferta = OF + "Oferta_ChampDeMars.asset", ofertaTitulo = "Folhas da petição do Champ de Mars",
            ofertaDescricao = "Cópia das folhas recolhidas no altar da pátria em 17 de julho de 1791: milhares de assinaturas, muitas feitas com uma cruz por quem não sabia escrever. O texto pede à Assembleia que consulte a nação sobre o destino do Rei.",
        });
        It(Danton, P3 + "Pista_Champ_Peticao.asset", "pista_champ_1", "A petição manchada de sangue", "Sobrevivente do Champ de Mars",
            "Pedaço da petição do altar da pátria, manchado de sangue na hora dos tiros: pede à Assembleia que consulte a nação antes de decidir o destino do Rei. As assinaturas, uma a uma, muitas com uma cruz, param na borda rasgada. Segundo o sobrevivente, quem assinava estava desarmado.");
        It(Danton, P3 + "Pista_Champ_Boato.asset", "pista_champ_boato", "As armas do altar", "Guardas tratados pelo médico",
            "Segundo os guardas que o médico tratou, no altar da pátria não se assinava petição nenhuma: Danton mandou armar o povo, e a folha de assinaturas foi escrita depois, para disfarçar a insurreição.");
        It(Danton, P3 + "Pista_Champ_LeiMarcial.asset", "pista_champ_2", "A Constituição de 1791: direito de reunião", "Artigos afixados no mural dos Cordeliers",
            "Artigos da Constituição votada em setembro de 1791, afixados no mural: ela garante aos cidadãos a liberdade de se reunir pacificamente e sem armas e de dirigir às autoridades petições assinadas individualmente.");
        It(Danton, P3 + "Pista_Champ_Calunia.asset", "pista_champ_calunia", "A lei das petições", "Bilhete sem assinatura",
            "Bilhete deixado na mesa do café: a nova lei proíbe reunir o povo para pedir qualquer coisa contra o Rei; Danton sabia que era crime e mandou os peticionários ao altar para ter mártires.");
        It(Danton, P3 + "Alegacao_Champ_1.asset", "alegacao_champ_1", "Carta de Danton: só usei a palavra", "Carta do cliente",
            "Danton jura que nunca pegou em armas nem mandou ninguém pegar: só defendeu uma petição à Assembleia.");
        It(Danton, P3 + "Alegacao_Champ_2.asset", "alegacao_champ_2", "Carta de Danton: querem calar os Cordeliers", "Carta do cliente",
            "Ele afirma que os mandados servem para calar os clubes populares, e não para fazer justiça aos mortos.");
        It(Danton, P3 + "Panfleto_Champ.asset", "panfleto_champ", "O Grito do Povo Contra os Fuzis de Lafayette", "Tipografia",
            "Panfleto da tipografia em defesa de Danton e dos peticionários do Champ de Mars.");
        It(Danton, P3 + "Apoio_ChampDeMars_Biblioteca.asset", "apoio_biblioteca_champ", "Folhas da petição do Champ de Mars", "Biblioteca (compra)",
            "Cópia das folhas recolhidas no altar da pátria em 17 de julho de 1791: milhares de assinaturas, muitas feitas com uma cruz por quem não sabia escrever. O texto pede à Assembleia que consulte a nação sobre o destino do Rei.");

        No(Danton, D3 + "GeorgesDanton_Caso_ChampDeMars.asset")
            .F(DAN, "Dizem que incitei a insurreição armada. Tolice! Eu apenas usei a voz que a Constituição me deu.")
            .F(DAN, "Quem assinava no altar da pátria não carregava fuzil. Procure os sobreviventes: um deles guardou a petição.")
            .F(DAN, "E desconfie do médico da esquina: anda repetindo o que os guardas feridos lhe contam.")
            .F(DAN, "Valois, você consegue convencer o júri de que a lei protege a voz das ruas?")
            .Op("A Constituição de 1791 garante o direito de petição. Vou focar na legalidade técnica do seu ato.", D3 + "GeorgesDanton_Caso_ChampDeMars_R1.asset")
            .Op("Se atacarmos Lafayette e o prefeito Bailly, o senhor terá ainda mais inimigos.", D3 + "GeorgesDanton_Caso_ChampDeMars_R2.asset")
            .Op("Por que o senhor não se apresenta e se defende no tribunal?", D3 + "GeorgesDanton_Caso_ChampDeMars_R3.asset");
        No(Danton, D3 + "GeorgesDanton_Caso_ChampDeMars_R1.asset")
            .F(DAN, "Escolha bem as palavras. Um advogado medroso usa a lei como escudo; um mestre usa a lei como espada para derrubar tiranos.")
            .F(DAN, "Os artigos estão pregados no mural dos Cordeliers. Leia antes que alguém os arranque.");
        No(Danton, D3 + "GeorgesDanton_Caso_ChampDeMars_R2.asset")
            .F(DAN, "Inimigos eu tenho de sobra. Bailly ergueu a bandeira vermelha, Lafayette marchou com a Guarda, e os fuzis falaram.")
            .F(DAN, "Que o povo saiba quem estava do outro lado do altar.");
        No(Danton, D3 + "GeorgesDanton_Caso_ChampDeMars_R3.asset")
            .F(DAN, "Porque esse mandado não busca justiça, busca silêncio. Enquanto o seu panfleto não sair, não durmo duas noites na mesma casa.");

        No(Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars.asset")
            .F(SOB, "Eles atiraram sem aviso... O papel que carregávamos não era uma arma, era um pedido de socorro à Nação!")
            .F(SOB, "Guardei isso comigo. Está manchado, mas é a prova da nossa intenção.")
            .Op("Entregue-me a petição. Vou transformá-la em um grito que a Assembleia não poderá ignorar.", D3 + "PeticionariaLacombe_Caso_ChampDeMars_R1.asset")
            .Op("Este papel é perigoso. Se eu for pego com ele, seremos ambos considerados traidores.", D3 + "PeticionariaLacombe_Caso_ChampDeMars_R2.asset")
            .Op("O que acontecia no altar antes dos tiros?", D3 + "PeticionariaLacombe_Caso_ChampDeMars_R3.asset");
        No(Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars_R1.asset")
            .F(SOB, "Tome. Que o sangue nestas letras manche a consciência daqueles que se dizem representantes do povo.");
        No(Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars_R2.asset")
            .F(SOB, "A neutralidade agora é apenas covardia. Se você não a levar, a verdade morrerá neste beco comigo.")
            .F(SOB, "Tome.");
        No(Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars_R3.asset")
            .F(SOB, "Assinávamos desde a manhã, um de cada vez. Havia mulheres, crianças, gente que só sabia fazer uma cruz.")
            .F(SOB, "Quando a Guarda chegou, uns rapazes jogaram pedras do barranco. Nós, no altar, só tínhamos a pena e o papel. Tome, leve a petição.");
        No(Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars_Depois.asset")
            .F(SOB, "A petição já está com você. Faça com que ela seja lida.");

        No(Danton, D3 + "ViuvaFrancois_Caso_ChampDeMars.asset")
            .F(MED, "Depois do Champ de Mars, tratei dois guardas com a cabeça aberta por pedradas.")
            .F(MED, "Eles juram que no altar não se assinava petição nenhuma: Danton mandou armar o povo, e a folha de assinaturas foi escrita depois, para disfarçar a insurreição.")
            .F(MED, "Os Cordeliers pregaram artigos da Constituição no mural, e alguém deixou um bilhete sobre a lei na mesa do café. Cada um conta de um jeito.")
            .Op("O senhor viu algum peticionário armado?", D3 + "ViuvaFrancois_Caso_ChampDeMars_R1.asset")
            .Op("Por que o senhor acredita nos guardas?", D3 + "ViuvaFrancois_Caso_ChampDeMars_R2.asset");
        No(Danton, D3 + "ViuvaFrancois_Caso_ChampDeMars_R1.asset")
            .F(MED, "Eu vi feridas, cidadão, não o que as causou. Os guardas tinham pedradas. Os mortos que levaram ao hospital tinham balas.");
        No(Danton, D3 + "ViuvaFrancois_Caso_ChampDeMars_R2.asset")
            .F(MED, "Porque estavam assustados, e homens assustados raramente mentem de propósito. Mentem por medo. Tire suas conclusões.");
        No(Danton, D3 + "CocheiroJoubert_Caso_ChampDeMars.asset")
            .F(KOR, "Danton? Um advogado barulhento que vive de discursos. Os Cordeliers fazem barulho; eu faço negócios.")
            .F(KOR, "Do Champ de Mars só sei o que os jornais dizem, meu caro. E cada jornal diz uma coisa.");
        No(Danton, D3 + "GravadorMorel_Caso_ChampDeMars.asset")
            .F(SIR, "Fuzis contra quem assinava uma petição... Conheço bem a justiça que chega antes das provas.")
            .F(SIR, "Não sei nada do que houve lá, senhor. Mas desejo sorte a esse Danton.");

        // ----- Adultério e Poder Ministerial (Caso_Varennes) -----
        // Na história, Beaumarchais foi ADVERSÁRIO de Kornmann: em 1781 ajudou a tirar a senhora Kornmann da reclusão, e o
        // mémoire de Bergasse (1787) o acusou por isso. A "colaboração com Beaumarchais" do GDD vira essa intervenção real,
        // que Julien usa para "associar o caso a Beaumarchais" e fazer Paris falar (fala do GDD).
        Casos.Add(new CasoGdd
        {
            arquivo = Kornmann, cena = F, tituloAnterior = "A Carruagem de Varennes", titulo = "Adultério e Poder Ministerial",
            objetivo = "Descobrir quem protegeu a esposa de Kornmann e o que as provas mostram.",
            descricao = "1792. A nova lei permite o divórcio, e o banqueiro Guillaume Kornmann, que acusa a esposa de adultério desde 1787, quer " +
                        "usá-la. Ele afirma que ela escapou da justiça porque tinha protetores no ministério do Rei. Kornmann paga bem para " +
                        "transformar o processo num panfleto contra a corrupção dos antigos ministros.",
            rotaDeDialogo = D3 + "CocheiroJoubert_Caso_Varennes.asset",
            revelacaoComBoato = "Peritos compararam o diário com as cartas da senhora Kornmann: a letra era dela. O boato de que o banqueiro o forjou caiu por terra, e o panfleto que o repetiu também.",
            revelacaoCalunia = "O próprio Beaumarchais tinha admitido, por escrito, que ajudou a tirar a senhora Kornmann da reclusão. O libelo que negava tudo era calúnia, e a tipografia foi acusada de inventar histórias.",
            revelacaoAlegacoes = "O panfleto só repetia as queixas de Kornmann. Pareceu vingança de marido rico, e ninguém o levou a sério.",
            oferta = OF + "Oferta_Varennes.asset", ofertaTitulo = "O mémoire de Bergasse (1787)",
            ofertaDescricao = "Cópia do mémoire que o advogado Nicolas Bergasse escreveu por Kornmann em 1787: acusa a esposa, o amante, Beaumarchais e o antigo tenente de polícia Lenoir de se unirem para livrá-la do marido. O texto correu Paris e fez do caso um escândalo político.",
        });
        It(Kornmann, P3 + "Pista_Varennes_Contrato.asset", "pista_varennes_1", "O diário da esposa", "Guillaume Kornmann",
            "Kornmann mostra o diário da esposa, com a mesma letra das cartas que ela assinou no processo: ela escreve que o amante lhe prometeu a proteção de gente do ministério e do tenente de polícia contra qualquer queixa do marido.");
        It(Kornmann, P3 + "Pista_Varennes_Boato.asset", "pista_varennes_boato", "A letra do diário", "Georges Danton",
            "Segundo Danton, todo o café comenta que o diário foi escrito pelo próprio Kornmann, imitando a letra da esposa, para ganhar o divórcio e ficar com o dote.");
        It(Kornmann, P3 + "Pista_Varennes_Depoimento.asset", "pista_varennes_2", "A colaboração de Beaumarchais", "Mémoire esquecido na mesa do café",
            "Exemplar de um mémoire do próprio Beaumarchais, o autor de 'As Bodas de Fígaro': ele conta que, em 1781, pediu às autoridades de polícia que tirassem a senhora Kornmann, então grávida, da casa de correção onde o marido a mandara recolher.");
        It(Kornmann, P3 + "Pista_Varennes_Calunia.asset", "pista_varennes_calunia", "O nome de Beaumarchais", "Prova de impressão abandonada",
            "Prova de um libelo na caixa de tipos: Beaumarchais nunca ouviu falar da senhora Kornmann; o banqueiro inventou o nome do dramaturgo para vender escândalo e viver do dote da mulher.");
        It(Kornmann, P3 + "Alegacao_Varennes_1.asset", "alegacao_varennes_1", "Carta de Kornmann: fui desonrado", "Carta do cliente",
            "Kornmann jura que a esposa o traiu com um homem protegido por ministros, e que a justiça do antigo regime fechou os olhos.");
        It(Kornmann, P3 + "Alegacao_Varennes_2.asset", "alegacao_varennes_2", "Carta de Kornmann: os ministros a acobertaram", "Carta do cliente",
            "Ele afirma que ministros do Rei e o tenente de polícia a tiraram da reclusão para agradar amigos poderosos.");
        It(Kornmann, P3 + "Panfleto_Varennes.asset", "panfleto_varennes", "Memórias de Kornmann: Onde a Lei Termina e a Libertinagem Começa", "Tipografia",
            "Panfleto da tipografia sobre o processo de Kornmann e a proteção dos antigos ministros.");
        It(Kornmann, P3 + "Apoio_Varennes_Biblioteca.asset", "apoio_biblioteca_varennes", "O mémoire de Bergasse (1787)", "Biblioteca (compra)",
            "Cópia do mémoire que o advogado Nicolas Bergasse escreveu por Kornmann em 1787: acusa a esposa, o amante, Beaumarchais e o antigo tenente de polícia Lenoir de se unirem para livrá-la do marido. O texto correu Paris e fez do caso um escândalo político.");
        It(Kornmann, P3 + "Apoio_Varennes_Recibo.asset", "apoio_varennes_recibo", "A ordem de reclusão de 1781", "Guillaume Kornmann",
            "Cópia da ordem do Rei, obtida pelo próprio Kornmann, que mandou recolher a esposa numa casa de correção em 1781, sem juiz nem processo. Ela saiu de lá pouco depois, por intervenção de amigos poderosos.");

        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes.asset")
            .F(KOR, "Minha esposa me desonrou, mas o verdadeiro crime é o Estado protegê-la porque ela serve ao prazer dos ministros do Rei.")
            .F(KOR, "Tenho o diário dela, escrito com a letra dela. Está tudo ali: o amante, as promessas, os protetores.")
            .F(KOR, "E aquele advogado dos Cordeliers, o Danton, ri de mim no café dizendo que eu mesmo escrevi o diário!")
            .F(KOR, "Eu tenho o ouro, você tem a pena. Vamos destruir a reputação desses aristocratas?")
            .Op("Podemos associar o seu caso ao dramaturgo Beaumarchais para garantir que toda Paris fale sobre essa corrupção.", D3 + "CocheiroJoubert_Caso_Varennes_R1.asset")
            .Op("Se atacarmos os antigos ministros, os amigos deles ainda têm poder. O senhor está pronto para as consequências?", D3 + "CocheiroJoubert_Caso_Varennes_R2.asset")
            .Op("Por que o senhor quer o divórcio agora?", D3 + "CocheiroJoubert_Caso_Varennes_R3.asset");
        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes_R1.asset")
            .F(KOR, "Beaumarchais! Aquele saltimbanco meteu o nariz no meu casamento e ainda escreveu sobre isso.")
            .F(KOR, "A autoridade deles já está podre. Um panfleto bem escrito será o empurrão que falta para o povo odiá-los tanto quanto eu os odeio.");
        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes_R2.asset")
            .F(KOR, "Os ministros caíram junto com o Rei, meu caro, mas os amigos deles continuam nos salões.")
            .F(KOR, "Estou pronto. E o senhor?");
        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes_R3.asset")
            .F(KOR, "Porque até setembro a lei não permitia. A nova lei me devolve o direito que o antigo regime me negou.")
            .F(KOR, "E porque quero que Paris saiba por que precisei dele.");
        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes_Depois.asset")
            .F(KOR, "Já tem o diário. Agora mostre a Paris quem a protegia.");
        No(Kornmann, D3 + "CocheiroJoubert_Caso_Varennes_recibo_da_estalagem.asset")
            .F(KOR, "Então achou o mémoire daquele saltimbanco! Ele mesmo confessa que tirou minha mulher da reclusão.")
            .F(KOR, "Leve também isto: a ordem do Rei que a mandou recolher, em 1781. Ela estava presa por ordem régia, e mesmo assim os amigos dela a soltaram.")
            .F(J, "Uma ordem do Rei, sem juiz nem processo.")
            .F(KOR, "Era a lei do meu tempo, meu caro. Use-a como quiser.");

        No(Kornmann, D3 + "GeorgesDanton_Caso_Varennes.asset")
            .F(DAN, "O banqueiro Kornmann? Todo o café comenta que ele mesmo escreveu o diário da mulher, imitando a letra dela, para ganhar o divórcio e ficar com o dote.")
            .F(DAN, "Alguém esqueceu na mesa do café um mémoire do Beaumarchais sobre o caso, e na caixa de tipos estão compondo um libelo contra o banqueiro. Leia os dois e ria comigo.")
            .Op("O senhor leu o diário?", D3 + "GeorgesDanton_Caso_Varennes_R1.asset")
            .Op("Por que Paris se importa com o casamento de um banqueiro?", D3 + "GeorgesDanton_Caso_Varennes_R2.asset");
        No(Kornmann, D3 + "GeorgesDanton_Caso_Varennes_R1.asset")
            .F(DAN, "Eu? Tenho mais o que fazer do que ler as queixas de um marido rico. Repito o que se diz; julgar é com você.");
        No(Kornmann, D3 + "GeorgesDanton_Caso_Varennes_R2.asset")
            .F(DAN, "Porque, por trás da alcova, estão os ministros do Rei. Um escândalo de alcova derruba mais reputações do que cem discursos.");
        No(Kornmann, D3 + "PeticionariaLacombe_Caso_Varennes.asset")
            .F(SOB, "Um banqueiro que briga com a mulher... Enquanto isso, as viúvas do Champ de Mars não têm o que comer.")
            .F(SOB, "Não sei nada disso, senhor. Pergunte a quem frequenta os salões.");
        No(Kornmann, D3 + "ViuvaFrancois_Caso_Varennes.asset")
            .F(MED, "A senhora Kornmann? Nunca foi minha paciente. Médico de subúrbio não trata de esposas de banqueiro.")
            .F(MED, "Mas dizem que os mémoires desse processo venderam mais que qualquer remédio.");
        No(Kornmann, D3 + "GravadorMorel_Caso_Varennes.asset")
            .F(SIR, "Um banqueiro rico que usa a lei contra a própria esposa... e eu, que só queria enterrar minha filha em paz.")
            .F(SIR, "Não conheço esse senhor Kornmann. Não posso ajudar.");

        // ----- Morte no Poço e Intolerância (Caso_Assignats) -----
        // O caso Sirven é de 1762–1771 (Castres); o GDD o traz para a Fase 3. Os textos não dão ano ao caso.
        Casos.Add(new CasoGdd
        {
            arquivo = Sirven, cena = F, tituloAnterior = "Os Assignats Falsos", titulo = "Morte no Poço e Intolerância",
            objetivo = "Descobrir como Élisabeth vivia antes de morrer e o que sustenta a acusação contra o pai.",
            descricao = "Élisabeth Sirven, jovem de família protestante que passou meses internada num convento, foi encontrada morta num poço, " +
                        "em Castres. Juízes conservadores acusam o pai, Pierre-Paul Sirven, de matá-la para impedir sua conversão ao catolicismo, " +
                        "e querem confiscar os bens da família, que fugiu para o exílio. Leitores de Voltaire pagam a defesa.",
            rotaDeDialogo = D3 + "GravadorMorel_Caso_Assignats.asset",
            revelacaoComBoato = "O laudo do médico e o registro do convento mostraram que Élisabeth voltou doente, e não decidida a se converter. O boato dos juízes ruiu, e o panfleto que o repetiu também.",
            revelacaoCalunia = "Calas nunca confessou: morreu jurando inocência e foi reabilitado em 1765. O cartaz era calúnia, e a tipografia pagou caro por espalhá-lo.",
            revelacaoAlegacoes = "O panfleto só repetia a palavra de Sirven. Os leitores de Voltaire esperavam provas, e o caso esfriou.",
            oferta = OF + "Oferta_Assignats.asset", ofertaTitulo = "Registro do convento de Castres",
            ofertaDescricao = "Cópia do registro do convento: Élisabeth Sirven foi internada por ordem do bispo, para ser instruída na fé católica, e devolvida à família meses depois porque já não estava em seu juízo perfeito.",
        });
        It(Sirven, P3 + "Pista_Assignats_Queixa.asset", "pista_assignats_1", "O testemunho do médico local", "Médico",
            "Laudo assinado pelo médico que atendeu Élisabeth depois do convento: ela falava com sombras, passava noites sem dormir e fugia de casa de madrugada. O médico conclui que ela sofria de delírio, e não de maus-tratos.");
        It(Sirven, P3 + "Pista_Assignats_Boato.asset", "pista_assignats_boato", "A conversão de Élisabeth", "Guillaume Kornmann",
            "Segundo Kornmann, os juízes contam que Élisabeth saiu do convento sã e feliz, decidida a se converter, e que o pai a trancava em casa para impedi-la.");
        It(Sirven, P3 + "Pista_Assignats_Tipos.asset", "pista_assignats_2", "O Tratado sobre a Tolerância", "Provas de impressão na caixa de tipos",
            "Provas de uma nova edição do Tratado sobre a Tolerância, que Voltaire escreveu em 1763 pelo caso Calas: um pai protestante de Toulouse supliciado sob a acusação de matar o filho para impedir sua conversão. Calas foi reabilitado em 1765. Voltaire defende que a razão, e não o dogma, guie o juiz.");
        It(Sirven, P3 + "Pista_Assignats_Calunia.asset", "pista_assignats_calunia", "A confissão de Calas", "Cartaz sem assinatura",
            "Cartaz no mural: Calas confessou no suplício que matou o filho, o Tratado de Voltaire é mentira paga pelos protestantes de Genebra, e Sirven fez o mesmo que Calas.");
        It(Sirven, P3 + "Alegacao_Assignats_1.asset", "alegacao_assignats_1", "Carta de Sirven: minha filha estava doente", "Carta do cliente",
            "Sirven jura que Élisabeth nunca mais foi a mesma depois do convento e que ninguém em casa lhe fez mal.");
        It(Sirven, P3 + "Alegacao_Assignats_2.asset", "alegacao_assignats_2", "Carta de Sirven: querem os nossos bens", "Carta do cliente",
            "Ele afirma que os juízes querem a acusação para confiscar os bens da família, e não para fazer justiça.");
        It(Sirven, P3 + "Panfleto_Assignats.asset", "panfleto_assignats", "A Inocência no Poço: O Crime de Julgar Sem Provas", "Tipografia",
            "Panfleto da tipografia em defesa da família Sirven.");
        It(Sirven, P3 + "Apoio_Assignats_Biblioteca.asset", "apoio_biblioteca_assignats", "Registro do convento de Castres", "Biblioteca (compra)",
            "Cópia do registro do convento: Élisabeth Sirven foi internada por ordem do bispo, para ser instruída na fé católica, e devolvida à família meses depois porque já não estava em seu juízo perfeito.");

        No(Sirven, D3 + "GravadorMorel_Caso_Assignats.asset")
            .F(SIR, "Eles não querem justiça pela minha filha. Eles querem meu ouro e meu silêncio.")
            .F(SIR, "O médico que atendeu Élisabeth depois do convento sabe o que ela sofria, mas tem medo de falar. Ele está aqui mesmo, nesta rua.")
            .F(SIR, "E até um banqueiro, esse Kornmann, repete nos cafés o que os juízes dizem de mim.")
            .F(SIR, "Valois, você acredita que a 'Tolerância' de Voltaire pode vencer o peso desta toga conservadora?")
            .Op("Vou usar o Tratado sobre a Tolerância para provar que este tribunal é uma relíquia do passado.", D3 + "GravadorMorel_Caso_Assignats_R1.asset")
            .Op("O povo prefere histórias de crimes religiosos a laudos médicos. Será uma luta difícil para a Opinião Pública.", D3 + "GravadorMorel_Caso_Assignats_R2.asset")
            .Op("Como era Élisabeth antes do convento?", D3 + "GravadorMorel_Caso_Assignats_R3.asset");
        No(Sirven, D3 + "GravadorMorel_Caso_Assignats_R1.asset")
            .F(SIR, "Minha família fugiu para o exílio. Tudo o que temos agora é a sua habilidade de transformar nossa tragédia em um manifesto contra a barbárie.")
            .F(SIR, "Dizem que uma tipografia daqui está reimprimindo o Tratado.");
        No(Sirven, D3 + "GravadorMorel_Caso_Assignats_R2.asset")
            .F(SIR, "Eu sei. Em Toulouse, um pai protestante, Jean Calas, foi supliciado na roda por uma acusação igual à minha. Não quero ser o próximo.");
        No(Sirven, D3 + "GravadorMorel_Caso_Assignats_R3.asset")
            .F(SIR, "Alegre, curiosa, um pouco frágil. Levaram-na por ordem do bispo, para ser instruída na fé católica.")
            .F(SIR, "Quando voltou, falava sozinha e não dormia. Eu nunca mais reconheci minha filha.");

        No(Sirven, D3 + "ViuvaFrancois_Caso_Assignats.asset")
            .F(MED, "Vi a jovem Élisabeth depois que saiu do convento. Ela falava com sombras e fugia de casa de madrugada... Foi um delírio, não um crime.")
            .F(MED, "Mas, se eu assinar esse laudo, os juízes dirão que sou cúmplice de protestantes.")
            .Op("A ciência forense deve guiar a justiça. Sua prova pode salvar a vida de Pierre-Paul e o patrimônio da família.", D3 + "ViuvaFrancois_Caso_Assignats_R1.asset")
            .Op("Entendo seu medo. Mas sem o seu testemunho, o dogma da Igreja vencerá a razão.", D3 + "ViuvaFrancois_Caso_Assignats_R2.asset");
        No(Sirven, D3 + "ViuvaFrancois_Caso_Assignats_R1.asset")
            .F(MED, "Tome este laudo de insanidade. Mas use-o com cuidado: os fanáticos não aceitam que a medicina explique o que eles chamam de 'pecado'.");
        No(Sirven, D3 + "ViuvaFrancois_Caso_Assignats_R2.asset")
            .F(MED, "Razão... Voltaire escreveu sobre ela a vida inteira, e ainda assim queimam livros.")
            .F(MED, "Tome o laudo. E que Deus, o de qualquer igreja, proteja nós dois.");
        No(Sirven, D3 + "ViuvaFrancois_Caso_Assignats_Depois.asset")
            .F(MED, "O laudo já está com o senhor. Não me faça repeti-lo em voz alta nesta rua.");

        No(Sirven, D3 + "CocheiroJoubert_Caso_Assignats.asset")
            .F(KOR, "Conheço os juízes, meu caro, janto com metade deles. Contam que a moça saiu do convento sã e feliz, decidida a se converter, e que o pai a trancava em casa para impedi-la.")
            .F(KOR, "A tipografia da esquina reimprime o Tratado de Voltaire na caixa de tipos, e colaram um cartaz sobre Calas no mural. Paris adora uma briga de religião.")
            .Op("Os juízes viram a moça depois do convento?", D3 + "CocheiroJoubert_Caso_Assignats_R1.asset")
            .Op("Por que o senhor repete o que os juízes dizem?", D3 + "CocheiroJoubert_Caso_Assignats_R2.asset");
        No(Sirven, D3 + "CocheiroJoubert_Caso_Assignats_R1.asset")
            .F(KOR, "Viram? Duvido. Juiz não visita casa de protestante. Mas falam com muita certeza, e certeza, num jantar, vale mais que prova.");
        No(Sirven, D3 + "CocheiroJoubert_Caso_Assignats_R2.asset")
            .F(KOR, "Porque convém concordar com os juízes à mesa, meu caro. Tenho processos demais para fazer inimigos de toga.");
        No(Sirven, D3 + "GeorgesDanton_Caso_Assignats.asset")
            .F(DAN, "Um pai perseguido por ser protestante? A Constituição garante a liberdade de culto, cidadão. Quem julga assim vive no século passado.")
            .F(DAN, "Mas desse caso não sei mais nada. Tenho os meus próprios juízes atrás de mim.");
        No(Sirven, D3 + "PeticionariaLacombe_Caso_Assignats.asset")
            .F(SOB, "Uma moça morta num poço e o pai acusado... É sempre o mais fraco que paga.")
            .F(SOB, "Não conheço essa família, senhor. Não posso ajudar.");

        // Reações
        R(F, "GeorgesDanton", Danton, D3 + "GeorgesDanton_Caso_ChampDeMars.asset");
        R(F, "GeorgesDanton", Kornmann, D3 + "GeorgesDanton_Caso_Varennes.asset", null, P3 + "Pista_Varennes_Boato.asset");
        R(F, "GeorgesDanton", Sirven, D3 + "GeorgesDanton_Caso_Assignats.asset");
        R(F, "PeticionariaLacombe", Danton, D3 + "PeticionariaLacombe_Caso_ChampDeMars.asset", D3 + "PeticionariaLacombe_Caso_ChampDeMars_Depois.asset", P3 + "Pista_Champ_Peticao.asset");
        R(F, "PeticionariaLacombe", Kornmann, D3 + "PeticionariaLacombe_Caso_Varennes.asset");
        R(F, "PeticionariaLacombe", Sirven, D3 + "PeticionariaLacombe_Caso_Assignats.asset");
        R(F, "ViuvaFrancois", Danton, D3 + "ViuvaFrancois_Caso_ChampDeMars.asset", null, P3 + "Pista_Champ_Boato.asset");
        R(F, "ViuvaFrancois", Kornmann, D3 + "ViuvaFrancois_Caso_Varennes.asset");
        R(F, "ViuvaFrancois", Sirven, D3 + "ViuvaFrancois_Caso_Assignats.asset", D3 + "ViuvaFrancois_Caso_Assignats_Depois.asset", P3 + "Pista_Assignats_Queixa.asset");
        R(F, "CocheiroJoubert", Kornmann, D3 + "CocheiroJoubert_Caso_Varennes.asset", D3 + "CocheiroJoubert_Caso_Varennes_Depois.asset", P3 + "Pista_Varennes_Contrato.asset");
        R(F, "CocheiroJoubert", Sirven, D3 + "CocheiroJoubert_Caso_Assignats.asset", null, P3 + "Pista_Assignats_Boato.asset");
        R(F, "CocheiroJoubert", Danton, D3 + "CocheiroJoubert_Caso_ChampDeMars.asset");
        R(F, "GravadorMorel", Sirven, D3 + "GravadorMorel_Caso_Assignats.asset");
        R(F, "GravadorMorel", Danton, D3 + "GravadorMorel_Caso_ChampDeMars.asset");
        R(F, "GravadorMorel", Kornmann, D3 + "GravadorMorel_Caso_Varennes.asset");

        // Objetos: cada um guarda o fato de um caso e a calúnia de outro. O balcão vira a mesa do "Café Popular" do GDD.
        var mesa = new ObjetoGdd { caso = Kornmann, cena = F, id = "BalcaoDaPadaria", nomeAnterior = "Balcão da Padaria", nome = "Mesa do Café" };
        mesa.porCaso.Add(new[] { Kornmann, P3 + "Pista_Varennes_Depoimento.asset" });
        mesa.porCaso.Add(new[] { Danton, P3 + "Pista_Champ_Calunia.asset" });
        var mural = new ObjetoGdd { cena = F, id = "MuralDosCordeliers" };
        mural.porCaso.Add(new[] { Danton, P3 + "Pista_Champ_LeiMarcial.asset" });
        mural.porCaso.Add(new[] { Sirven, P3 + "Pista_Assignats_Calunia.asset" });
        var caixa = new ObjetoGdd { cena = F, id = "CaixaDeTipos" };
        caixa.porCaso.Add(new[] { Sirven, P3 + "Pista_Assignats_Tipos.asset" });
        caixa.porCaso.Add(new[] { Kornmann, P3 + "Pista_Varennes_Calunia.asset" });
        Objetos.AddRange(new[] { mesa, mural, caixa });
    }

    // ===== Fase 4 (outono de 1793) =====
    // A fase condensa num só momento o julgamento dos girondinos (24 a 30 de outubro de 1793), o Máximo geral (setembro)
    // e a campanha de clemência de Desmoulins (que, na história, começa em dezembro de 1793).

    private static void DefinirFase4()
    {
        const string F = "Fase4";

        // RedatorMarchand = Desmoulins, ComissarioVautrin = Mercador, CidadaDelorme = Vergniaud, Carcereiro = Brissot.
        Personagens.Add(new PersonagemGdd { caso = Desmoulins, cena = F, id = "RedatorMarchand", nomeAnterior = "Redator Marchand", nome = "Camille Desmoulins", padrao = D4 + "RedatorMarchand_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Mercador, cena = F, id = "ComissarioVautrin", nomeAnterior = "Comissário Vautrin", nome = "Mercador", padrao = D4 + "ComissarioVautrin_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Girondinos, cena = F, id = "CidadaDelorme", nomeAnterior = "Cidadã Delorme", nome = "Pierre Vergniaud", padrao = D4 + "CidadaDelorme_Padrao.asset" });
        Personagens.Add(new PersonagemGdd { caso = Girondinos, cena = F, id = "Carcereiro", nomeAnterior = "Carcereiro da Conciergerie", nome = "Jacques-Pierre Brissot", padrao = D4 + "Carcereiro_Padrao.asset" });

        No(Desmoulins, D4 + "RedatorMarchand_Padrao.asset").F(DES, "Agora não, cidadão. Cada palavra minha vira prova num tribunal.");
        No(Mercador, D4 + "ComissarioVautrin_Padrao.asset").F(MER, "Não tenho nada para vender, cidadão. Nem farinha, nem conversa.");
        No(Girondinos, D4 + "CidadaDelorme_Padrao.asset").F(VER, "Guardo a voz para o tribunal, cidadão. Não posso gastá-la com o que não conheço.");
        No(Girondinos, D4 + "Carcereiro_Padrao.asset").F(BRI, "Não sei nada disso, cidadão. Um homem acusado de tudo aprende a não falar do que não sabe.");

        // ----- O Manifesto da Clemência (Caso_Jornalista, rota A) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Desmoulins, cena = F, tituloAnterior = "O Redator do Velho Sans-culotte", titulo = "O Manifesto da Clemência",
            objetivo = "Reunir provas para o pedido de clemência de Camille Desmoulins.",
            descricao = "Outono de 1793. A Lei dos Suspeitos enche as prisões de Paris. O jornalista Camille Desmoulins, colega de escola de " +
                        "Robespierre, quer publicar um pedido de clemência e já é olhado com desconfiança pelo Comitê de Salvação Pública. " +
                        "Ele pede à tipografia um panfleto que mostre ao povo o que se faz no escuro.",
            rotaDeDialogo = D4 + "RedatorMarchand_Caso_Jornalista.asset",
            revelacaoComBoato = "Os registros de prisão confirmaram os nomes da lista: artesãos, viúvas e lojistas, não aristocratas. O boato de que Camille inventou tudo ruiu, e enfraqueceu o panfleto que o repetiu.",
            revelacaoCalunia = "As anotações eram mesmo de Robespierre: a letra conferia com a dos papéis do Comitê. A denúncia de falsificação era calúnia, usada depois contra a própria tipografia.",
            revelacaoAlegacoes = "O panfleto só repetia as palavras de Camille. Soou como o apelo de um homem assustado.",
            oferta = OF + "Oferta_Jornalista.asset", ofertaTitulo = "A Lei dos Suspeitos (17 de setembro de 1793)",
            ofertaDescricao = "Texto do decreto: são suspeitos os que, por sua conduta, suas relações, suas palavras ou seus escritos, se mostraram partidários da tirania ou do federalismo, além de ex-nobres e parentes de emigrados que não demonstraram apego à Revolução. Os comitês de vigilância fazem as listas e mandam prender.",
        });
        It(Desmoulins, P4 + "Pista_Jornalista_Exemplares.asset", "pista_jornalista_1", "A lista de 'suspeitos' inocentes", "Camille Desmoulins",
            "Lista que Camille copiou dos registros de prisão da sua seção: um alfaiate, uma viúva, dois padeiros, um velho escrivão. Ao lado de cada nome, o motivo anotado pelo comitê de vigilância: 'indiferente', 'falou mal do preço do pão', 'parente de emigrado'. Nenhum é acusado de crime algum.");
        It(Desmoulins, P4 + "Pista_Jornalista_Boato.asset", "pista_jornalista_boato", "Os nomes da lista", "Jacques-Pierre Brissot",
            "Segundo Brissot, a lista de Camille só tem aristocratas e banqueiros amigos da mulher dele, e metade dos nomes foi inventada para tirar da prisão quem ele quer proteger.");
        It(Desmoulins, P4 + "Pista_Jornalista_Certificado.asset", "pista_jornalista_2", "O manuscrito de Robespierre", "Arquivo do Comitê",
            "Rascunho de um artigo de Camille, arquivado com os papéis do Comitê, com anotações de Robespierre na margem, na mesma letra dos relatórios que ele assina: ele risca as frases mais duras, mas escreve 'publicar' ao lado do pedido de clemência.");
        It(Desmoulins, P4 + "Pista_Jornalista_Calunia.asset", "pista_jornalista_calunia", "As anotações do rascunho", "Denúncia na caixa de denúncias",
            "Denúncia anônima: Robespierre nunca leu uma linha de Camille; as anotações na margem do rascunho são falsificação do próprio Camille, que imita a letra do amigo para se proteger.");
        It(Desmoulins, P4 + "Alegacao_Jornalista_1.asset", "alegacao_jornalista_1", "Carta de Camille: quero salvar a República", "Carta do cliente",
            "Camille jura que não quer derrubar a República, e sim salvá-la de si mesma: clemência não é traição.");
        It(Desmoulins, P4 + "Alegacao_Jornalista_2.asset", "alegacao_jornalista_2", "Carta de Camille: prendem inocentes", "Carta do cliente",
            "Ele afirma que a Lei dos Suspeitos prende gente sem crime algum, só por palavras ou parentesco.");
        It(Desmoulins, P4 + "Panfleto_Jornalista.asset", "panfleto_jornalista", "A Revolução Devora Seus Próprios Filhos", "Tipografia",
            "Panfleto da tipografia com o pedido de clemência de Camille Desmoulins.");
        It(Desmoulins, P4 + "Apoio_Jornalista_Biblioteca.asset", "apoio_biblioteca_jornalista", "A Lei dos Suspeitos (17 de setembro de 1793)", "Biblioteca (compra)",
            "Texto do decreto: são suspeitos os que, por sua conduta, suas relações, suas palavras ou seus escritos, se mostraram partidários da tirania ou do federalismo, além de ex-nobres e parentes de emigrados que não demonstraram apego à Revolução. Os comitês de vigilância fazem as listas e mandam prender.");

        No(Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista.asset")
            .F(DES, "Maximilien foi meu colega de escola... Ele hesitou antes de deixar que me chamassem de suspeito.")
            .F(DES, "Eu não quero derrubar a República, Valois, eu quero salvá-la de si mesma! Esta lista prova que estamos prendendo inocentes.")
            .F(DES, "E Brissot, lá da prisão, espalha que eu inventei os nomes. Logo ele, que a minha pena ajudou a derrubar.")
            .Op("Se Robespierre anotou o seu rascunho, isso prova que há ranhuras no Comitê. Onde está o manuscrito?", D4 + "RedatorMarchand_Caso_Jornalista_R1.asset")
            .Op("Publicar esta lista de inocentes agora é declarar guerra aberta ao Terror. Você está pronto para ser o próximo?", D4 + "RedatorMarchand_Caso_Jornalista_R2.asset")
            .Op("Quem são as pessoas da lista?", D4 + "RedatorMarchand_Caso_Jornalista_R3.asset");
        No(Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista_R1.asset")
            .F(DES, "Ele me devolveu as folhas com a letra dele na margem, e depois o Comitê as recolheu. Devem estar no arquivo, com os outros papéis.")
            .F(DES, "A pena é a única coisa que me sobrou. Imprima 'A Revolução devora seus próprios filhos'. Que o povo saiba o que estamos fazendo no escuro.");
        No(Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista_R2.asset")
            .F(DES, "Ninguém está pronto. Mas vi a acusação usar os meus panfletos contra os girondinos, e não quero mais sangue com a minha assinatura.")
            .F(DES, "A pena é a única coisa que me sobrou. Que o povo saiba o que estamos fazendo no escuro.");
        No(Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista_R3.asset")
            .F(DES, "Um alfaiate que chamou a lei de injusta numa taverna. Uma viúva cujo filho emigrou. Um padeiro 'indiferente'.")
            .F(DES, "Indiferente, Valois! Hoje isso dá prisão.");
        No(Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista_Depois.asset")
            .F(DES, "Você tem a lista. Cada nome nela é uma pessoa esperando numa cela.");

        No(Desmoulins, D4 + "Carcereiro_Caso_Jornalista.asset")
            .F(BRI, "Camille pedindo clemência? Que ironia. Foi a pena dele que ajudou a nos trazer até aqui.")
            .F(BRI, "Aqui dentro se diz que a lista dele só tem aristocratas e banqueiros amigos da mulher, e que metade dos nomes foi inventada.")
            .F(BRI, "Se quer saber o que Robespierre pensa do amigo, o arquivo do Comitê guarda o rascunho anotado. E a caixa de denúncias vive cheia de papéis sobre Camille.")
            .Op("O senhor viu a lista?", D4 + "Carcereiro_Caso_Jornalista_R1.asset")
            .Op("O senhor guarda rancor de Camille?", D4 + "Carcereiro_Caso_Jornalista_R2.asset");
        No(Desmoulins, D4 + "Carcereiro_Caso_Jornalista_R1.asset")
            .F(BRI, "Vi? Estou preso, cidadão. Ouço o que os guardas contam e o que os presos inventam para passar o tempo.");
        No(Desmoulins, D4 + "Carcereiro_Caso_Jornalista_R2.asset")
            .F(BRI, "Ele me chamou de traidor em cada folha que escreveu. Rancor... talvez. Mas ainda sei distinguir um panfleto de uma prova.");
        No(Desmoulins, D4 + "ComissarioVautrin_Caso_Jornalista.asset")
            .F(MER, "Desmoulins? Os fregueses leem os escritos dele em voz alta na fila do pão. Dizem que escreve bem.")
            .F(MER, "Mas eu mal sei ler, cidadão, e tenho problemas demais para me meter com o Comitê.");
        No(Desmoulins, D4 + "CidadaDelorme_Caso_Jornalista.asset")
            .F(VER, "Camille quer clemência agora? Quando os acusados éramos nós, a pena dele pedia pressa.")
            .F(VER, "Ainda assim, que salve quem puder. A Revolução, como Saturno, está devorando os próprios filhos.");

        // ----- O Caso do Pequeno Mercador de Grãos (Caso_Negociante, rota B) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Mercador, cena = F, tituloAnterior = "O Negociante de Grãos", titulo = "O Caso do Pequeno Mercador de Grãos",
            objetivo = "Descobrir a quem pertence a farinha do porão e por que ela some das lojas.",
            descricao = "Outono de 1793. A fome aperta Paris, e a lei contra o açambarcamento pune com a morte quem esconde gêneros de primeira " +
                        "necessidade. Um pequeno mercador é acusado de guardar sacos de farinha no porão enquanto crianças passam fome. Ele jura " +
                        "que a farinha é de um atravessador rico. Defendê-lo é enfrentar uma multidão que precisa de um culpado.",
            rotaDeDialogo = D4 + "ComissarioVautrin_Caso_Negociante.asset",
            revelacaoComBoato = "As faturas e o contrato de aluguel mostraram que a farinha era do atravessador, e não do mercador. O boato da seção ruiu, e o panfleto que o repetiu perdeu o crédito.",
            revelacaoCalunia = "Os próprios avisos da seção admitiam a falta de farinha nas padarias desde o Máximo. A denúncia que culpava só o mercador era calúnia, e voltou-se contra a tipografia.",
            revelacaoAlegacoes = "O panfleto só repetia a palavra do mercador. Para uma cidade com fome, soou como desculpa de açambarcador.",
            oferta = OF + "Oferta_Negociante.asset", ofertaTitulo = "Decreto contra o açambarcamento (26 de julho de 1793)",
            ofertaDescricao = "Texto do decreto: é crime capital retirar de circulação gêneros de primeira necessidade e guardá-los sem pô-los à venda. Quem guarda esses gêneros deve declarar os estoques, e comissários de cada seção inspecionam os depósitos.",
        });
        It(Mercador, P4 + "Pista_Negociante_Relatorio.asset", "pista_negociante_1", "As faturas do atravessador", "Mercador",
            "Faturas em nome de um fornecedor dos exércitos, homem rico do bairro: foi ele quem comprou e pagou a farinha, e ele aluga o porão do mercador como depósito. O mercador recebe só o aluguel.");
        It(Mercador, P4 + "Pista_Negociante_Boato.asset", "pista_negociante_boato", "A farinha do porão", "Camille Desmoulins",
            "Segundo Desmoulins, a seção inteira jura que os sacos são do próprio mercador: ele compra toda a farinha do bairro para revendê-la pelo triplo, às escondidas.");
        It(Mercador, P4 + "Pista_Negociante_Edital.asset", "pista_negociante_2", "A insuficiência do Máximo", "Tabela afixada na parede de editais",
            "Tabela do Máximo geral, afixada desde setembro: o preço da farinha foi fixado no de 1790 mais um terço, sem contar o frete. Ao lado, um aviso da seção admite que as padarias pequenas recebem cada vez menos farinha desde que a lei entrou em vigor.");
        It(Mercador, P4 + "Pista_Negociante_Calunia.asset", "pista_negociante_calunia", "As prateleiras vazias", "Bilhete na caixa de denúncias",
            "Denúncia anônima: desde o Máximo as padarias estão cheias, e se falta farinha no bairro é só porque o mercador esconde tudo no porão.");
        It(Mercador, P4 + "Alegacao_Negociante_1.asset", "alegacao_negociante_1", "Carta do mercador: a farinha não é minha", "Carta do cliente",
            "O mercador jura que os sacos pertencem a um homem rico que usa o porão dele como depósito.");
        It(Mercador, P4 + "Alegacao_Negociante_2.asset", "alegacao_negociante_2", "Carta do mercador: são sementes", "Carta do cliente",
            "Ele afirma que parte dos sacos é semente para o próximo plantio, e que vendê-la agora deixaria o campo sem colheita.");
        It(Mercador, P4 + "Panfleto_Negociante.asset", "panfleto_negociante", "A Falha das Leis de Abastecimento", "Tipografia",
            "Panfleto da tipografia sobre o mercador acusado e as leis de abastecimento.");
        It(Mercador, P4 + "Apoio_Negociante_Biblioteca.asset", "apoio_biblioteca_negociante", "Decreto contra o açambarcamento (26 de julho de 1793)", "Biblioteca (compra)",
            "Texto do decreto: é crime capital retirar de circulação gêneros de primeira necessidade e guardá-los sem pô-los à venda. Quem guarda esses gêneros deve declarar os estoques, e comissários de cada seção inspecionam os depósitos.");

        No(Mercador, D4 + "ComissarioVautrin_Caso_Negociante.asset")
            .F(MER, "Cidadão Valois, eu não sou um traidor! Se eu vender o que sobrou, não haverá sementes para o amanhã.")
            .F(MER, "O povo me chama de açambarcador, mas os verdadeiros lucros estão nos bolsos da elite que me usa como depósito!")
            .F(MER, "E o jornalista Desmoulins já escreveu que a farinha é minha. Na seção, ninguém lê outra coisa.")
            .Op("Entregue as faturas. Se provarmos que você é apenas um empregado da elite, o ódio da massa terá um novo alvo.", D4 + "ComissarioVautrin_Caso_Negociante_R1.asset")
            .Op("Vou atacar as Leis de Abastecimento. O governo prefere prender mercadores a admitir que o tabelamento de preços falhou.", D4 + "ComissarioVautrin_Caso_Negociante_R2.asset")
            .Op("Afinal, os sacos são sementes ou farinha?", D4 + "ComissarioVautrin_Caso_Negociante_R3.asset");
        No(Mercador, D4 + "ComissarioVautrin_Caso_Negociante_R1.asset")
            .F(MER, "Tome as faturas. Mas cuidado: expor quem realmente controla o trigo é assinar sua própria sentença de morte na guilhotina.");
        No(Mercador, D4 + "ComissarioVautrin_Caso_Negociante_R2.asset")
            .F(MER, "O Máximo? Olhe a tabela na parede de editais: pagam pela farinha menos do que custa trazê-la do campo. Quem pode, esconde; quem não pode, fecha a loja.")
            .F(MER, "Tome as faturas. E cuidado com quem você acusa.");
        No(Mercador, D4 + "ComissarioVautrin_Caso_Negociante_R3.asset")
            .F(MER, "Alguns são sementes, juro. O resto é farinha do fornecedor, que me paga o aluguel do porão. Eu só guardo a chave.")
            .F(MER, "Tome as faturas e veja com seus próprios olhos.");
        No(Mercador, D4 + "ComissarioVautrin_Caso_Negociante_Depois.asset")
            .F(MER, "Já lhe dei as faturas. Rezo para que ninguém descubra que fui eu.");

        No(Mercador, D4 + "RedatorMarchand_Caso_Negociante.asset")
            .F(DES, "O mercador do porão? A seção inteira jura que os sacos são dele: compra toda a farinha do bairro para revendê-la pelo triplo, às escondidas.")
            .F(DES, "Eu mesmo escrevi isso, que Deus me perdoe. Com fome, ninguém quer ouvir outra versão.")
            .F(DES, "A tabela do Máximo está na parede de editais, e a caixa de denúncias recebe um bilhete sobre ele por dia.")
            .Op("O senhor viu os sacos?", D4 + "RedatorMarchand_Caso_Negociante_R1.asset")
            .Op("Por que culpar um homem tão pequeno?", D4 + "RedatorMarchand_Caso_Negociante_R2.asset");
        No(Mercador, D4 + "RedatorMarchand_Caso_Negociante_R1.asset")
            .F(DES, "Não. Copiei o que me contaram na seção. Um jornalista com pressa escreve primeiro e confere depois... quando confere.");
        No(Mercador, D4 + "RedatorMarchand_Caso_Negociante_R2.asset")
            .F(DES, "Porque um culpado pequeno cabe numa carroça, cidadão. Um grande exige coragem.");
        No(Mercador, D4 + "Carcereiro_Caso_Negociante.asset")
            .F(BRI, "O Máximo! Nós, girondinos, avisamos que tabelar os preços faria o trigo sumir dos mercados. Por isso também nos chamam de inimigos do povo.")
            .F(BRI, "Sobre esse mercador, não sei nada. Daqui de dentro, só vejo o pão encolher.");
        No(Mercador, D4 + "CidadaDelorme_Caso_Negociante.asset")
            .F(VER, "Um homem pequeno acusado por uma cidade faminta... Já vi esse espetáculo, cidadão. Termina sempre na mesma praça.")
            .F(VER, "Não conheço o caso. Não posso ajudar.");

        // ----- O Extermínio da Oposição (Caso_Girondina, rota C) -----
        Casos.Add(new CasoGdd
        {
            arquivo = Girondinos, cena = F, tituloAnterior = "A Viúva Girondina", titulo = "O Extermínio da Oposição",
            objetivo = "Descobrir o que as provas contra os girondinos realmente mostram.",
            descricao = "Outubro de 1793. Vinte e um deputados girondinos, entre eles Vergniaud e Brissot, estão diante do Tribunal Revolucionário, " +
                        "acusados de federalismo e de conspirar contra a unidade da República. A eloquência de Vergniaud começa a comover as " +
                        "galerias, e o Comitê tem pressa. Vergniaud pede à tipografia que mostre ao povo o que as provas dizem.",
            rotaDeDialogo = D4 + "CidadaDelorme_Caso_Girondina.asset",
            revelacaoComBoato = "As cartas foram lidas em público: pediam a defesa da Convenção, e não a separação das províncias nem a entrega dos portos. O boato ruiu, e o panfleto que o repetiu também.",
            revelacaoCalunia = "A pressa vinha da acusação, e não dos réus: a nota de Robespierre pedia que os debates fossem abreviados. O cartaz era calúnia, e a tipografia foi acusada de mentir.",
            revelacaoAlegacoes = "O panfleto só repetia a palavra de Vergniaud. Belo como um discurso, fraco como prova.",
            oferta = OF + "Oferta_Girondina.asset", ofertaTitulo = "Decreto de 8 de brumário do ano II",
            ofertaDescricao = "Decreto votado pela Convenção em 29 de outubro de 1793: depois de três dias de debates, o presidente do tribunal pode perguntar aos jurados se a consciência deles já está suficientemente esclarecida e, se estiver, encerrar o julgamento.",
        });
        It(Girondinos, P4 + "Pista_Girondina_Cartas.asset", "pista_girondina_1", "Provas de federalismo", "Jacques-Pierre Brissot",
            "Cópias das cartas de Brissot aos departamentos, que a acusação chama de provas de federalismo: pedem que as províncias defendam a liberdade da Convenção contra a pressão armada da Comuna de Paris. Nenhuma fala em separar as províncias da República.");
        It(Girondinos, P4 + "Pista_Girondina_Boato.asset", "pista_girondina_boato", "O plano das províncias", "Camille Desmoulins",
            "Segundo Desmoulins, as cartas de Brissot mandam as províncias se separarem de Paris e abrir os portos aos ingleses, como fizeram os traidores de Toulon.");
        It(Girondinos, P4 + "Pista_Girondina_Retratacao.asset", "pista_girondina_2", "O testemunho de Robespierre", "Arquivo do Comitê",
            "Nota com a letra de Robespierre, arquivada entre os papéis do Comitê: pede que o tribunal abrevie os debates e condene os acusados depressa, 'para salvar a pátria', antes que a defesa termine.");
        It(Girondinos, P4 + "Pista_Girondina_Calunia.asset", "pista_girondina_calunia", "O fim dos debates", "Cartaz na parede de editais",
            "Cartaz: os próprios girondinos pediram que os debates fossem encerrados, porque já tinham confessado a traição.");
        It(Girondinos, P4 + "Alegacao_Girondina_1.asset", "alegacao_girondina_1", "Carta de Vergniaud: somos republicanos", "Carta do cliente",
            "Vergniaud jura que nenhum dos acusados quis dividir a República: queriam protegê-la da violência da Comuna.");
        It(Girondinos, P4 + "Alegacao_Girondina_2.asset", "alegacao_girondina_2", "Carta de Vergniaud: o julgamento é uma farsa", "Carta do cliente",
            "Ele afirma que o tribunal decidiu a sentença antes de ouvir a defesa.");
        It(Girondinos, P4 + "Panfleto_Girondina.asset", "panfleto_girondina", "A Liberdade da Escrita", "Tipografia",
            "Panfleto da tipografia sobre o julgamento dos girondinos.");
        It(Girondinos, P4 + "Apoio_Girondina_Biblioteca.asset", "apoio_biblioteca_girondina", "Decreto de 8 de brumário do ano II", "Biblioteca (compra)",
            "Decreto votado pela Convenção em 29 de outubro de 1793: depois de três dias de debates, o presidente do tribunal pode perguntar aos jurados se a consciência deles já está suficientemente esclarecida e, se estiver, encerrar o julgamento.");
        It(Girondinos, P4 + "Apoio_Girondina_Recomendacao.asset", "apoio_girondina_recomendacao", "As notas da defesa de Vergniaud", "Pierre Vergniaud",
            "Rascunho da defesa que Vergniaud preparava para o tribunal: responde a cada acusação com datas, cartas e votos na Convenção, e lembra que os acusados votaram pela República una e indivisível.");

        No(Girondinos, D4 + "CidadaDelorme_Caso_Girondina.asset")
            .F(VER, "Minha retórica é minha única arma. As galerias estão começando a ouvir a razão. Robespierre está com medo de que o povo nos absolva!")
            .F(VER, "Brissot guarda as cartas que a acusação chama de provas de federalismo. Leia-as antes de acreditar no que dizem delas.")
            .F(VER, "E desconfie de Camille Desmoulins: a pena dele ajudou a nos trazer até aqui, e ainda corre pelas ruas.")
            .Op("Sua voz é poderosa, mas o Comitê tem o poder de calá-la por decreto. Precisamos agir rápido.", D4 + "CidadaDelorme_Caso_Girondina_R1.asset")
            .Op("Vou coletar o testemunho de Robespierre. Se provarmos a manipulação dele, a República terá que recuar.", D4 + "CidadaDelorme_Caso_Girondina_R2.asset")
            .Op("O senhor teme a guilhotina?", D4 + "CidadaDelorme_Caso_Girondina_R3.asset");
        No(Girondinos, D4 + "CidadaDelorme_Caso_Girondina_R1.asset")
            .F(VER, "Vá! Mostre a eles que a justiça não se curva diante de tiranos, mesmo que eles usem o barrete frígio.");
        No(Girondinos, D4 + "CidadaDelorme_Caso_Girondina_R2.asset")
            .F(VER, "Se existe uma ordem dele, está no arquivo do Comitê, onde tudo se escreve e nada se mostra.")
            .F(VER, "Vá! A justiça não se curva diante de tiranos.");
        No(Girondinos, D4 + "CidadaDelorme_Caso_Girondina_R3.asset")
            .F(VER, "Temo que a Revolução, como Saturno, devore os próprios filhos. Eu disse isso na tribuna em março, e ninguém quis ouvir.");
        No(Girondinos, D4 + "CidadaDelorme_Caso_Girondina_carta_da_secao.asset")
            .F(VER, "Então você viu a nota de Robespierre. Eles têm pressa porque a nossa defesa assusta.")
            .F(VER, "Leve também as minhas notas. Se eu não puder terminar de falar, que o seu panfleto termine por mim.");

        No(Girondinos, D4 + "Carcereiro_Caso_Girondina.asset")
            .F(BRI, "Valois, eles nos acusam de federalismo para justificar o Terror. Eles não querem justiça, querem silêncio.")
            .F(BRI, "Você ainda acredita que a lei pode vencer o 'Decreto dos Três Dias' de Robespierre?")
            .Op("O Estado exige uma condenação rápida. Talvez usar suas cartas de província ajude a acalmar os radicais e salvar sua vida.", D4 + "Carcereiro_Caso_Girondina_R1.asset")
            .Op("Se eu imprimir o panfleto 'A Liberdade da Escrita' agora, o povo verá que este julgamento é uma farsa.", D4 + "Carcereiro_Caso_Girondina_R2.asset");
        No(Girondinos, D4 + "Carcereiro_Caso_Girondina_R1.asset")
            .F(BRI, "Minhas cartas? Leia-as, e depois diga se há nelas uma palavra contra a República. Tome as cópias.")
            .F(BRI, "A Revolução tornou-se um monstro que exige sangue. Se você nos defender com a verdade, será o vigésimo segundo a subir no cadafalso.");
        No(Girondinos, D4 + "Carcereiro_Caso_Girondina_R2.asset")
            .F(BRI, "Então imprima com as nossas próprias palavras. Tome as cópias das cartas que chamam de provas.")
            .F(BRI, "Mas saiba: se você nos defender com a verdade, será o vigésimo segundo a subir no cadafalso.");
        No(Girondinos, D4 + "Carcereiro_Caso_Girondina_Depois.asset")
            .F(BRI, "Você já tem as cartas. Leia-as em voz alta, se tiver coragem.");

        No(Girondinos, D4 + "RedatorMarchand_Caso_Girondina.asset")
            .F(DES, "As cartas de Brissot? Dizem que mandam as províncias se separarem de Paris e abrir os portos aos ingleses, como fizeram em Toulon.")
            .F(DES, "Robespierre deixou uma nota sobre o julgamento no arquivo do Comitê, e colaram um cartaz sobre os debates na parede de editais. Leia você mesmo; eu já escrevi demais sobre esses homens.")
            .Op("O senhor leu as cartas?", D4 + "RedatorMarchand_Caso_Girondina_R1.asset")
            .Op("O senhor acredita que eles merecem a morte?", D4 + "RedatorMarchand_Caso_Girondina_R2.asset");
        No(Girondinos, D4 + "RedatorMarchand_Caso_Girondina_R1.asset")
            .F(DES, "Li os trechos que a acusação escolheu, e escrevi um panfleto inteiro com eles. Hoje não sei se li as cartas ou o que eu queria encontrar nelas.");
        No(Girondinos, D4 + "RedatorMarchand_Caso_Girondina_R2.asset")
            .F(DES, "Brissot já foi meu amigo. Se forem condenados, terei ajudado a matá-los. É o que me tira o sono.");
        No(Girondinos, D4 + "ComissarioVautrin_Caso_Girondina.asset")
            .F(MER, "Os deputados girondinos? Na fila do pão ninguém fala de outra coisa, e ninguém entende nada.")
            .F(MER, "Eu só queria que dividissem a farinha, cidadão.");

        // Reações
        R(F, "RedatorMarchand", Desmoulins, D4 + "RedatorMarchand_Caso_Jornalista.asset", D4 + "RedatorMarchand_Caso_Jornalista_Depois.asset", P4 + "Pista_Jornalista_Exemplares.asset");
        R(F, "RedatorMarchand", Mercador, D4 + "RedatorMarchand_Caso_Negociante.asset", null, P4 + "Pista_Negociante_Boato.asset");
        R(F, "RedatorMarchand", Girondinos, D4 + "RedatorMarchand_Caso_Girondina.asset", null, P4 + "Pista_Girondina_Boato.asset");
        R(F, "ComissarioVautrin", Mercador, D4 + "ComissarioVautrin_Caso_Negociante.asset", D4 + "ComissarioVautrin_Caso_Negociante_Depois.asset", P4 + "Pista_Negociante_Relatorio.asset");
        R(F, "ComissarioVautrin", Desmoulins, D4 + "ComissarioVautrin_Caso_Jornalista.asset");
        R(F, "ComissarioVautrin", Girondinos, D4 + "ComissarioVautrin_Caso_Girondina.asset");
        R(F, "CidadaDelorme", Girondinos, D4 + "CidadaDelorme_Caso_Girondina.asset"); // cliente: só conversa (+ etapa complementar)
        R(F, "CidadaDelorme", Desmoulins, D4 + "CidadaDelorme_Caso_Jornalista.asset");
        R(F, "CidadaDelorme", Mercador, D4 + "CidadaDelorme_Caso_Negociante.asset");
        R(F, "Carcereiro", Girondinos, D4 + "Carcereiro_Caso_Girondina.asset", D4 + "Carcereiro_Caso_Girondina_Depois.asset", P4 + "Pista_Girondina_Cartas.asset");
        R(F, "Carcereiro", Desmoulins, D4 + "Carcereiro_Caso_Jornalista.asset", null, P4 + "Pista_Jornalista_Boato.asset");
        R(F, "Carcereiro", Mercador, D4 + "Carcereiro_Caso_Negociante.asset");

        // Objetos: o arquivo da seção vira o arquivo do Comitê de Salvação Pública (ambiente da Fase 4 no GDD).
        var arquivo = new ObjetoGdd { caso = Desmoulins, cena = F, id = "ArquivoDaSecao", nomeAnterior = "Arquivo da Seção", nome = "Arquivo do Comitê" };
        arquivo.porCaso.Add(new[] { Desmoulins, P4 + "Pista_Jornalista_Certificado.asset" });
        arquivo.porCaso.Add(new[] { Girondinos, P4 + "Pista_Girondina_Retratacao.asset" });
        var parede = new ObjetoGdd { cena = F, id = "ParedeDeEditais" };
        parede.porCaso.Add(new[] { Mercador, P4 + "Pista_Negociante_Edital.asset" });
        parede.porCaso.Add(new[] { Girondinos, P4 + "Pista_Girondina_Calunia.asset" });
        var caixa = new ObjetoGdd { cena = F, id = "CaixaDeDenuncias" };
        caixa.porCaso.Add(new[] { Desmoulins, P4 + "Pista_Jornalista_Calunia.asset" });
        caixa.porCaso.Add(new[] { Mercador, P4 + "Pista_Negociante_Calunia.asset" });
        Objetos.AddRange(new[] { arquivo, parede, caixa });
    }

    // ===== Consultas usadas pelas outras ferramentas (textos de referência em vigor) =====

    /// <summary>Nome, fonte e descrição de uma pista, alegação, panfleto ou documento no conteúdo do GDD, ou null.</summary>
    internal static string[] TextoDeItem(string itemID)
    {
        Garantir();
        ItemGdd i = Itens.Find(x => x.id == itemID);
        return i != null ? new[] { i.nome, i.fonte, i.descricao } : null;
    }

    /// <summary>Verdadeiro se a reação (id do NPC na cena + arquivo do caso) é definida por esta ferramenta.</summary>
    internal static bool GerenciaReacao(string npcId, string casoArquivo)
    {
        Garantir();
        return Reacoes.Exists(r => r.npc == npcId && r.caso == casoArquivo);
    }

    /// <summary>Textos da primeira fala (nó de entrada) da reação, ou null.</summary>
    internal static string[] FalasDaEntrada(string npcId, string casoArquivo)
    {
        Garantir();
        ReacaoGdd r = Reacoes.Find(x => x.npc == npcId && x.caso == casoArquivo);
        return r != null ? FalasDoNo(r.entrada) : null;
    }

    /// <summary>Textos das falas de um nó de diálogo definido aqui (pelo caminho), ou null.</summary>
    internal static string[] FalasDoNo(string caminho)
    {
        Garantir();
        NoGdd no = Nos.Find(n => n.caminho == caminho);
        return no != null ? no.falas.ConvertAll(f => f[1]).ToArray() : null;
    }

    /// <summary>Falas de uma etapa complementar ("recibo_da_estalagem", "carta_da_secao"), ou null.</summary>
    internal static string[] FalasDaEtapa(string etapaId)
    {
        if (etapaId == "recibo_da_estalagem") return FalasDoNo(D3 + "CocheiroJoubert_Caso_Varennes_recibo_da_estalagem.asset");
        if (etapaId == "carta_da_secao") return FalasDoNo(D4 + "CidadaDelorme_Caso_Girondina_carta_da_secao.asset");
        return null;
    }

    /// <summary>Descrição do documento da biblioteca do caso (ex.: "Caso_Varennes"), ou null.</summary>
    internal static string DescricaoDaOferta(string casoArquivo)
    {
        Garantir();
        return Casos.Find(c => c.arquivo == casoArquivo)?.ofertaDescricao;
    }

    // ===== Execução =====

    private class Contagem
    {
        public int casos, itens, dialogosCriados, dialogosAlterados, revelacoes, ofertas, reacoes, objetos, npcsCriados, renomeados;
    }

    [MenuItem("Ferramentas/Campanha/8 - Aplicar conteúdo narrativo do GDD (Fases 2 a 4)")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[NarrativaGdd] Aplicar conteúdo narrativo do GDD (Fases 2 a 4)\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        Garantir();
        var migrar = new HashSet<string>();
        var jaAplicados = new List<string>();
        var mantidos = new List<string>();
        foreach (CasoGdd c in Casos)
        {
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(PastaCasos + c.arquivo + ".asset");
            if (caso == null) { r.AppendLine($"  AVISO: o caso '{c.arquivo}' não existe (rode antes as ferramentas 1 a 7)."); continue; }
            string titulo = (caso.caseTitle ?? string.Empty).Trim();
            if (titulo == c.titulo) jaAplicados.Add(c.arquivo);
            else if (titulo == c.tituloAnterior) migrar.Add(c.arquivo);
            else mantidos.Add($"{c.arquivo} ('{titulo}')");
        }

        var cont = new Contagem();
        if (migrar.Count > 0)
        {
            // 1. Diálogos: primeiro garante que todos os nós existem (as opções apontam de um para outro), depois escreve.
            foreach (NoGdd no in Nos)
                if (migrar.Contains(no.caso) && AssetDatabase.LoadAssetAtPath<DialogueData>(no.caminho) == null)
                {
                    GarantirPasta(Path.GetDirectoryName(no.caminho).Replace('\\', '/'));
                    var novo = ScriptableObject.CreateInstance<DialogueData>();
                    novo.talkScript = new List<Dialogue>();
                    AssetDatabase.CreateAsset(novo, no.caminho);
                    cont.dialogosCriados++;
                }
            AssetDatabase.SaveAssets();
            foreach (NoGdd no in Nos) if (migrar.Contains(no.caso)) EscreverNo(no, cont, r);

            // 2. Itens (pistas, alegações, panfletos, documentos de apoio).
            foreach (ItemGdd item in Itens) if (migrar.Contains(item.caso)) EscreverItem(item, cont, r);

            // 3. Casos, revelações das receitas e ofertas da biblioteca.
            foreach (CasoGdd c in Casos) if (migrar.Contains(c.arquivo)) EscreverCaso(c, cont, r);
            AssetDatabase.SaveAssets();

            // 4. Cenas: NPCs, reações e objetos. Guarda caminhos: abrir outra cena descarrega assets não usados.
            SceneSetup[] configuracao = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string cena in new[] { "Fase2", "Fase3", "Fase4" })
                    ConfigurarCena(cena, migrar, cont, r);
            }
            finally
            {
                if (configuracao != null && configuracao.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(configuracao);
            }
            AssetDatabase.SaveAssets();
        }

        r.AppendLine($"  Casos reescritos: {cont.casos}; itens: {cont.itens}; diálogos criados: {cont.dialogosCriados}, alterados: {cont.dialogosAlterados}; " +
                     $"revelações: {cont.revelacoes}; ofertas: {cont.ofertas}; reações: {cont.reacoes}; entradas de objetos: {cont.objetos}; " +
                     $"NPCs criados: {cont.npcsCriados}; objetos/NPCs renomeados: {cont.renomeados}.");
        if (jaAplicados.Count > 0) r.AppendLine("  Já com o conteúdo do GDD (não tocados): " + string.Join(", ", jaAplicados) + ".");
        if (mantidos.Count > 0) r.AppendLine("  Mantidos (título diferente do anterior e do GDD, editado no Inspector): " + string.Join(", ", mantidos) + ".");
        return r.ToString();
    }

    private static void GarantirPasta(string pasta)
    {
        if (AssetDatabase.IsValidFolder(pasta)) return;
        string pai = Path.GetDirectoryName(pasta).Replace('\\', '/');
        GarantirPasta(pai);
        AssetDatabase.CreateFolder(pai, Path.GetFileName(pasta));
    }

    private static void EscreverNo(NoGdd no, Contagem cont, StringBuilder r)
    {
        DialogueData dialogo = AssetDatabase.LoadAssetAtPath<DialogueData>(no.caminho);
        if (dialogo == null) { r.AppendLine($"  PROBLEMA: diálogo '{no.caminho}' não pôde ser criado."); return; }

        var roteiro = new List<Dialogue>();
        for (int i = 0; i < no.falas.Count; i++)
        {
            var linha = new Dialogue { name = no.falas[i][0], text = no.falas[i][1], choices = new List<Choice>() };
            if (dialogo.talkScript != null && i < dialogo.talkScript.Count) linha.dialogueAudio = dialogo.talkScript[i].dialogueAudio;
            if (i == no.falas.Count - 1) // opções só na última fala (é quando a resposta do protagonista aparece)
                foreach (string[] opcao in no.opcoes)
                {
                    DialogueData destino = AssetDatabase.LoadAssetAtPath<DialogueData>(opcao[1]);
                    if (destino == null) r.AppendLine($"  PROBLEMA: a opção '{opcao[0]}' de '{no.caminho}' aponta para '{opcao[1]}', que não existe.");
                    linha.choices.Add(new Choice { choiceText = opcao[0], nextDialogue = destino });
                }
            roteiro.Add(linha);
        }
        if (MesmoRoteiro(dialogo.talkScript, roteiro)) return;
        dialogo.talkScript = roteiro;
        EditorUtility.SetDirty(dialogo);
        cont.dialogosAlterados++;
    }

    private static bool MesmoRoteiro(List<Dialogue> atual, List<Dialogue> novo)
    {
        if (atual == null || atual.Count != novo.Count) return false;
        for (int i = 0; i < novo.Count; i++)
        {
            if (atual[i].name != novo[i].name || atual[i].text != novo[i].text) return false;
            int a = atual[i].choices?.Count ?? 0, b = novo[i].choices?.Count ?? 0;
            if (a != b) return false;
            for (int k = 0; k < b; k++)
                if (atual[i].choices[k].choiceText != novo[i].choices[k].choiceText || atual[i].choices[k].nextDialogue != novo[i].choices[k].nextDialogue)
                    return false;
        }
        return true;
    }

    private static void EscreverItem(ItemGdd def, Contagem cont, StringBuilder r)
    {
        Item item = AssetDatabase.LoadAssetAtPath<Item>(def.caminho);
        if (item == null) { r.AppendLine($"  PROBLEMA: item '{def.caminho}' não existe."); return; }
        if (item.itemID != def.id) { r.AppendLine($"  PROBLEMA: '{def.caminho}' tem itemID '{item.itemID}', esperado '{def.id}'. Nada foi trocado nele."); return; }
        if (item.itemName == def.nome && item.fonte == def.fonte && item.descricao == def.descricao) return;
        item.itemName = def.nome;
        item.fonte = def.fonte;
        item.descricao = def.descricao;
        EditorUtility.SetDirty(item);
        cont.itens++;
    }

    private static void EscreverCaso(CasoGdd def, Contagem cont, StringBuilder r)
    {
        CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(PastaCasos + def.arquivo + ".asset");
        caso.caseTitle = def.titulo;
        caso.caseDescription = def.descricao;
        caso.objectiveText = def.objetivo;
        DialogueData rota = AssetDatabase.LoadAssetAtPath<DialogueData>(def.rotaDeDialogo);
        if (rota != null) caso.npcDialogueRoute = rota;
        EditorUtility.SetDirty(caso);
        cont.casos++;

        ReceitaDeCaso receita = caso.receitaDoPanfleto;
        if (receita == null) r.AppendLine($"  PROBLEMA: '{def.arquivo}' sem receita.");
        else
        {
            if (TrocarRevelacao(receita.comBoato, def.revelacaoComBoato)) cont.revelacoes++;
            if (TrocarRevelacao(receita.calunia, def.revelacaoCalunia)) cont.revelacoes++;
            if (TrocarRevelacao(receita.soAlegacoes, def.revelacaoAlegacoes)) cont.revelacoes++;
            EditorUtility.SetDirty(receita);
        }

        if (def.oferta == null) return;
        OfertaDaBiblioteca oferta = AssetDatabase.LoadAssetAtPath<OfertaDaBiblioteca>(def.oferta);
        if (oferta == null) { r.AppendLine($"  PROBLEMA: oferta '{def.oferta}' não existe."); return; }
        if (oferta.titulo == def.ofertaTitulo && oferta.descricao == def.ofertaDescricao) return;
        oferta.titulo = def.ofertaTitulo;
        oferta.descricao = def.ofertaDescricao;
        EditorUtility.SetDirty(oferta);
        cont.ofertas++;
    }

    private static bool TrocarRevelacao(ReceitaDeCaso.Versao versao, string texto)
    {
        if (versao == null || string.IsNullOrEmpty(texto) || versao.textoRevelacao == texto) return false;
        versao.textoRevelacao = texto;
        return true;
    }

    // ----- Cenas -----

    private static void ConfigurarCena(string nomeDaCena, HashSet<string> migrar, Contagem cont, StringBuilder r)
    {
        if (!Casos.Exists(c => c.cena == nomeDaCena && migrar.Contains(c.arquivo))) return;
        Scene cena = EditorSceneManager.OpenScene($"Assets/Scenes/{nomeDaCena}.unity", OpenSceneMode.Single);

        var npcs = new Dictionary<string, NPCMovement>();
        var objetos = new Dictionary<string, LootInteractable>();
        foreach (var par in IdDeInteracao.InteracoesDaCena(cena))
        {
            string id = IdDeInteracao.IdEfetivo(par.Key, par.Value);
            if (par.Key is NPCMovement npc) npcs[id] = npc;
            if (par.Key is LootInteractable loot) objetos[id] = loot;
        }
        bool mudou = false;

        // NPCs: o novo (Danton) nasce do NPCBasic.prefab, como os outros NPCs da campanha; os antigos só mudam de nome.
        foreach (PersonagemGdd p in Personagens.FindAll(x => x.cena == nomeDaCena && migrar.Contains(x.caso)))
        {
            if (!npcs.TryGetValue(p.id, out NPCMovement npc))
            {
                if (!p.novo) { r.AppendLine($"  AVISO {nomeDaCena}: NPC '{p.id}' não encontrado."); continue; }
                npc = CampanhaSetupTool.CriarNpcNaCena(cena, p.id, p.nome, p.x, p.cor);
                npc.canInteract = true;
                npc.disableAfterDialogue = false; // responde a todo caso e continua acessível, como os demais
                npc.custoEmHoras = 1;
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                npcs[p.id] = npc;
                cont.npcsCriados++;
                mudou = true;
            }
            if (!string.IsNullOrEmpty(p.nomeAnterior) && npc.gameObject.name == p.nomeAnterior)
            {
                npc.gameObject.name = p.nome;
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc.gameObject);
                cont.renomeados++;
                mudou = true;
            }
            DialogueData padrao = AssetDatabase.LoadAssetAtPath<DialogueData>(p.padrao);
            if (padrao != null && npc.dialogoPadrao != padrao)
            {
                npc.dialogoPadrao = padrao;
                EditorUtility.SetDirty(npc);
                PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
                mudou = true;
            }
        }

        // Reações: entrada, fala depois da entrega e recompensas; pré-requisitos e etapas complementares são preservados.
        foreach (ReacaoGdd def in Reacoes.FindAll(x => x.cena == nomeDaCena && migrar.Contains(x.caso)))
        {
            if (!npcs.TryGetValue(def.npc, out NPCMovement npc)) { r.AppendLine($"  AVISO {nomeDaCena}: NPC '{def.npc}' não encontrado para '{def.caso}'."); continue; }
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(PastaCasos + def.caso + ".asset");
            if (npc.reacoesDeCaso == null) npc.reacoesDeCaso = new List<CasoReacao>();
            int indice = npc.reacoesDeCaso.FindIndex(x => x.caso == caso);
            CasoReacao atual = indice >= 0 ? npc.reacoesDeCaso[indice] : new CasoReacao { caso = caso };
            CasoReacao desejada = atual;
            desejada.dialogoInicialDoCaso = AssetDatabase.LoadAssetAtPath<DialogueData>(def.entrada);
            desejada.dialogoDepoisDaEntrega = def.depois != null ? AssetDatabase.LoadAssetAtPath<DialogueData>(def.depois) : null;
            desejada.recompensasDoDialogo = new List<Item>();
            foreach (string caminho in def.itens)
            {
                Item item = AssetDatabase.LoadAssetAtPath<Item>(caminho);
                if (item == null) r.AppendLine($"  PROBLEMA: recompensa '{caminho}' não existe.");
                else desejada.recompensasDoDialogo.Add(item);
            }
            if (desejada.dialogoInicialDoCaso == null) r.AppendLine($"  PROBLEMA: fala '{def.entrada}' não existe.");
            if (indice >= 0 && MesmaReacao(atual, desejada)) continue;

            if (indice >= 0) npc.reacoesDeCaso[indice] = desejada;
            else npc.reacoesDeCaso.Add(desejada);
            EditorUtility.SetDirty(npc);
            PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
            cont.reacoes++;
            mudou = true;
        }

        // Objetos: nome e, para cada caso reescrito, exatamente a entrada definida (ou nenhuma, se o objeto é vazio nele).
        foreach (ObjetoGdd def in Objetos.FindAll(x => x.cena == nomeDaCena))
        {
            if (!objetos.TryGetValue(def.id, out LootInteractable loot)) { r.AppendLine($"  AVISO {nomeDaCena}: objeto '{def.id}' não encontrado."); continue; }
            if (!string.IsNullOrEmpty(def.nomeAnterior) && migrar.Contains(def.caso) && loot.gameObject.name == def.nomeAnterior)
            {
                loot.gameObject.name = def.nome;
                EditorUtility.SetDirty(loot.gameObject);
                cont.renomeados++;
                mudou = true;
            }
            if (loot.pistasPossiveis == null) loot.pistasPossiveis = new List<LootDeCaso>();
            foreach (CasoGdd c in Casos.FindAll(x => x.cena == nomeDaCena && migrar.Contains(x.arquivo)))
            {
                CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(PastaCasos + c.arquivo + ".asset");
                string[] entrada = def.porCaso.Find(x => x[0] == c.arquivo);
                Item desejado = entrada != null ? AssetDatabase.LoadAssetAtPath<Item>(entrada[1]) : null;
                if (entrada != null && desejado == null) { r.AppendLine($"  PROBLEMA: item '{entrada[1]}' não existe."); continue; }
                List<LootDeCaso> atuais = loot.pistasPossiveis.FindAll(x => x.caso == caso);
                bool certo = desejado == null ? atuais.Count == 0 : atuais.Count == 1 && atuais[0].itemParaDar == desejado;
                if (certo) continue;
                loot.pistasPossiveis.RemoveAll(x => x.caso == caso);
                if (desejado != null) loot.pistasPossiveis.Add(new LootDeCaso { caso = caso, itemParaDar = desejado });
                EditorUtility.SetDirty(loot);
                cont.objetos++;
                mudou = true;
            }
        }

        if (mudou)
        {
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            r.AppendLine($"  {nomeDaCena}: cena atualizada e salva.");
        }
    }

    private static bool MesmaReacao(CasoReacao a, CasoReacao b)
    {
        if (a.dialogoInicialDoCaso != b.dialogoInicialDoCaso || a.dialogoDepoisDaEntrega != b.dialogoDepoisDaEntrega) return false;
        int na = a.recompensasDoDialogo?.Count ?? 0, nb = b.recompensasDoDialogo?.Count ?? 0;
        if (na != nb) return false;
        for (int i = 0; i < nb; i++) if (a.recompensasDoDialogo[i] != b.recompensasDoDialogo[i]) return false;
        return true;
    }
}
