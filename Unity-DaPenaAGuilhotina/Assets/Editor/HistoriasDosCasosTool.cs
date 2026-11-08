using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Preenche os textos do arquivo de casos (menu principal > Casos resolvidos) em cada CaseData: título e teor do panfleto, personagens, o que
/// aconteceu (a verdade das pistas, conforme docs/MATRIZ_DE_CASOS_TCC.md), o desfecho de cada versão do panfleto e a
/// nota histórica; e, em cada pista, o que o panfleto afirma quando ela é impressa. Só escreve campos VAZIOS: o que o grupo editar no Inspector nunca é sobrescrito. Para trocar um texto
/// por este padrão de novo, apague o campo no Inspector e rode o menu.
/// O que é invenção do GDD/jogo fica no "o que aconteceu" (é a verdade do mundo do jogo); a "nota histórica" só traz o
/// que é documentado (ver "Adaptações e divergências" na matriz).
/// Menu: Ferramentas > Campanha > 9 - Histórias do arquivo de casos
/// </summary>
public static class HistoriasDosCasosTool
{
    private sealed class Historia
    {
        public string arquivo, titulo, tituloDoPanfleto, teor, personagens, historia, fatos, comBoato, calunia, nota;
    }

    [MenuItem("Ferramentas/Campanha/9 - Histórias do arquivo de casos")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[HistoriasDosCasos] Textos do arquivo de casos\n");
        int campos = 0;
        foreach (Historia h in Historias())
        {
            string caminho = $"Assets/ScriptableObjects/Cases/{h.arquivo}.asset";
            CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>(caminho);
            if (caso == null) { r.AppendLine($"  {h.arquivo}: asset não encontrado em {caminho}."); continue; }

            Undo.RecordObject(caso, "Histórias do arquivo de casos");
            int antes = campos;
            Preencher(ref caso.tituloNoArquivo, h.titulo, ref campos);
            Preencher(ref caso.tituloDoPanfleto, h.tituloDoPanfleto, ref campos);
            Preencher(ref caso.teorDoPanfleto, h.teor, ref campos);
            Preencher(ref caso.personagens, h.personagens, ref campos);
            Preencher(ref caso.historia, h.historia, ref campos);
            Preencher(ref caso.desfechoFatos, h.fatos, ref campos);
            Preencher(ref caso.desfechoComBoato, h.comBoato, ref campos);
            Preencher(ref caso.desfechoCalunia, h.calunia, ref campos);
            Preencher(ref caso.notaHistorica, h.nota, ref campos);
            if (campos > antes) EditorUtility.SetDirty(caso);
            r.AppendLine($"  {h.arquivo}: {campos - antes} campo(s) preenchido(s).");
        }
        var pistasPorId = new Dictionary<string, Item>();
        foreach (string guid in AssetDatabase.FindAssets("t:Item", new[] { "Assets/ScriptableObjects" }))
        {
            Item item = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
            if (item != null && !string.IsNullOrEmpty(item.itemID) && !pistasPorId.ContainsKey(item.itemID)) pistasPorId.Add(item.itemID, item);
        }
        int afirmacoes = 0;
        foreach (KeyValuePair<string, string> a in Afirmacoes())
        {
            if (!pistasPorId.TryGetValue(a.Key, out Item pista)) { r.AppendLine($"  Pista '{a.Key}' não encontrada."); continue; }
            if (!string.IsNullOrWhiteSpace(pista.afirmacaoNoPanfleto)) continue;
            Undo.RecordObject(pista, "Afirmações no panfleto");
            pista.afirmacaoNoPanfleto = a.Value;
            EditorUtility.SetDirty(pista);
            afirmacoes++;
        }
        r.AppendLine($"  Afirmações no panfleto: {afirmacoes} pista(s) preenchida(s).");

        AssetDatabase.SaveAssets();
        return r.Append($"  Total: {campos} campo(s) de caso. Campos já escritos foram mantidos.").ToString();
    }

    private static void Preencher(ref string campo, string texto, ref int contador)
    {
        if (!string.IsNullOrWhiteSpace(campo) || string.IsNullOrWhiteSpace(texto)) return;
        campo = texto;
        contador++;
    }

    // itemID da pista → o que o panfleto afirma quando ela é impressa.
    private static Dictionary<string, string> Afirmacoes() => new Dictionary<string, string>
    {
        // O Colar da Rainha
        { "pista_joias_1", "A compra do colar foi negociada por um cardeal e uma intermediária, e a Rainha nunca foi à loja." },
        { "pista_joias_2", "A encomenda traz a assinatura \"Marie Antoinette de France\", que não é a que a Rainha usava: a autorização é duvidosa." },
        { "pista_joias_boato", "A Rainha foi à loja disfarçada, escolheu o colar escondida do Rei e depois negou tudo." },
        { "pista_joias_calunia", "A Rainha assinou a encomenda de próprio punho para dar o colar ao Cardeal, seu amante." },
        // Os Trabalhadores de Réveillon
        { "pista_jean_1", "Réveillon propôs abolir as taxas das barreiras para baratear o pão e, com ele, os salários e o preço das manufaturas." },
        { "pista_jean_2", "Réveillon pagava acima da média do bairro e manteve os salários no inverno de 1788, com o trabalho parado." },
        { "pista_jean_boato", "Réveillon disse que um operário vive bem com quinze soldos por dia e pediu só o corte dos salários." },
        { "pista_jean_calunia", "Réveillon cortou os salários pela metade no inverno e mandou a diferença à Inglaterra, que lhe paga para matar Paris de fome." },
        // O Operário Acusado
        { "pista_operario_1", "A Guarda Francesa chegou atirando para matar numa multidão sem armas de fogo, que pedia pão e salário." },
        { "pista_operario_2", "A ordem de dispersar a multidão \"por todos os meios\" foi afixada antes de ela chegar à manufatura." },
        { "pista_operario_boato", "A multidão veio armada, paga por agitadores do duque de Orléans, e a Guarda só atirou para se defender." },
        { "pista_operario_calunia", "A Guarda só agiu depois que o operário ateou fogo à fábrica com as próprias mãos." },
        // O Julgamento de Georges Danton
        { "pista_champ_1", "No altar da pátria se assinava uma petição, nome por nome, e quem assinava estava desarmado quando a Guarda atirou." },
        { "pista_champ_2", "A Constituição de 1791 garante o direito de se reunir em paz, sem armas, e de assinar petições às autoridades." },
        { "pista_champ_boato", "No altar não se assinava nada: Danton armou o povo, e a petição foi escrita depois para disfarçar a insurreição." },
        { "pista_champ_calunia", "A nova lei proibia reunir o povo contra o Rei, e Danton mandou os peticionários ao altar para ter mártires." },
        // Adultério e Poder Ministerial
        { "pista_varennes_1", "O diário da esposa, na letra dela, conta que o amante lhe prometeu a proteção do ministério e do tenente de polícia." },
        { "pista_varennes_2", "O próprio Beaumarchais admitiu ter pedido à polícia, em 1781, que tirasse a senhora Kornmann da casa de correção." },
        { "pista_varennes_boato", "O diário foi escrito pelo próprio Kornmann, imitando a letra da esposa, para ganhar o divórcio e ficar com o dote." },
        { "pista_varennes_calunia", "Beaumarchais nunca ouviu falar da senhora Kornmann: o banqueiro inventou o nome dele para vender escândalo." },
        // Morte no Poço e Intolerância
        { "pista_assignats_1", "O médico que tratou Élisabeth atesta que ela voltou do convento delirando e fugia de casa de madrugada, sem sinal de maus-tratos." },
        { "pista_assignats_2", "O caso repete o de Calas, supliciado pela mesma acusação e reabilitado em 1765, que levou Voltaire a escrever o Tratado sobre a Tolerância." },
        { "pista_assignats_boato", "Élisabeth saiu do convento sã, decidida a se converter, e o pai a trancava em casa para impedi-la." },
        { "pista_assignats_calunia", "Calas confessou no suplício, o Tratado de Voltaire é mentira paga por Genebra, e Sirven fez o mesmo que Calas." },
        // O Manifesto da Clemência
        { "pista_jornalista_1", "As prisões estão cheias de gente comum, um alfaiate, uma viúva, padeiros, presos por \"indiferença\" ou por uma palavra, sem crime nenhum." },
        { "pista_jornalista_2", "Robespierre leu o pedido de clemência, riscou as frases mais duras e escreveu \"publicar\" na margem." },
        { "pista_jornalista_boato", "A lista de Camille só tem aristocratas e banqueiros amigos dele, e metade dos nomes foi inventada." },
        { "pista_jornalista_calunia", "As anotações de Robespierre no rascunho são falsificação do próprio Camille, que imita a letra do amigo." },
        // O Caso do Pequeno Mercador de Grãos
        { "pista_negociante_1", "A farinha do porão é de um fornecedor dos exércitos, que aluga o porão; o mercador só recebe o aluguel." },
        { "pista_negociante_2", "A farinha some das padarias pequenas porque o preço do Máximo, sem o frete, não paga o transporte, como a própria seção admite." },
        { "pista_negociante_boato", "O mercador compra toda a farinha do bairro e a revende pelo triplo, às escondidas." },
        { "pista_negociante_calunia", "Desde o Máximo as padarias estão cheias, e só falta farinha porque o mercador esconde tudo no porão." },
        // O Extermínio da Oposição
        { "pista_girondina_1", "As cartas de Brissot pedem que as províncias defendam a Convenção da pressão da Comuna de Paris; nenhuma fala em separá-las da República." },
        { "pista_girondina_2", "Uma nota de Robespierre pede ao tribunal que abrevie os debates e condene depressa, antes que a defesa termine." },
        { "pista_girondina_boato", "As cartas de Brissot mandam as províncias se separarem de Paris e abrirem os portos aos ingleses." },
        { "pista_girondina_calunia", "Os próprios girondinos pediram o fim dos debates, porque já tinham confessado a traição." },
    };

    private static List<Historia> Historias() => new List<Historia>
    {
        // ===== Fase 1 =====
        new Historia
        {
            arquivo = "Caso_Tutorial",
            tituloDoPanfleto = "Memorial justificativo para três homens condenados à roda",
            teor =
                "O memorial de defesa dos três condenados. Com o relato de Marie Bradier, apontava as dúvidas sobre o reconhecimento " +
                "dos acusados; com o decreto de 1780, mostrava que a confissão fora arrancada por uma tortura que a própria lei da " +
                "Coroa já tinha abolido. Pedia que a sentença fosse revista antes que três inocentes morressem na roda.",
            titulo = "Os Três Condenados à Roda",
            personagens =
                "Charles Dupaty — magistrado, seu mestre, que assume a defesa\n" +
                "Marie Bradier — da família de Bradier, um dos condenados\n" +
                "Bradier, Simare e Lardoise — os três homens condenados à roda\n" +
                "Os magistrados — os juízes que os condenaram",
            historia =
                "Três homens pobres do interior, Bradier, Simare e Lardoise, foram condenados à morte na roda por um roubo " +
                "com violência. O processo foi conduzido às pressas: ninguém investigou de verdade, os acusados não foram " +
                "ouvidos e a identificação feita pelas vítimas veio tarde e cheia de dúvidas.\n\n" +
                "Marie Bradier contou o que viu em casa e o que não fazia sentido no reconhecimento. Dupaty mostrou o resto: " +
                "os três foram torturados para confessar, embora o decreto de 1780 de Luís XVI já tivesse abolido a tortura " +
                "na instrução dos processos. Os juízes agiram contra a própria lei da Coroa.\n\n" +
                "Com o relato de Marie e o decreto, você escreveu e imprimiu o memorial da defesa: o primeiro panfleto da tipografia.",
            fatos =
                "O memorial levou o caso a leitores que nunca tinham ouvido falar daqueles homens. Uma sentença dada no " +
                "silêncio virou escândalo público, e os magistrados tiveram de responder às perguntas que tinham ignorado.",
            nota =
                "O caso é real. Em 1786, Charles Dupaty publicou o \"Mémoire justificatif pour trois hommes condamnés à la " +
                "roue\". O Parlamento de Paris mandou queimar o texto, mas o escândalo forçou a revisão do processo, e os " +
                "três homens foram absolvidos em 1787. A tortura preparatória tinha sido abolida por Luís XVI em 1780."
        },

        // ===== Fase 2 =====
        new Historia
        {
            arquivo = "Caso_Joalheiro",
            teor =
                "Um panfleto sobre o colar vendido em nome da Rainha, encomendado pelo duque de Orléans para pôr Maria Antonieta no " +
                "centro do escândalo. O texto contava quem negociou a compra e o que a encomenda provava.",
            personagens =
                "Duque de Orléans — primo do Rei e rival da Rainha; encomendou o panfleto\n" +
                "Maria Antonieta — a Rainha, em nome de quem o colar foi vendido\n" +
                "O cardeal de Rohan — negociou a compra do colar\n" +
                "Jeanne de La Motte — a intermediária da negociação\n" +
                "O joalheiro — vendeu o colar e guardou o registro da compra\n" +
                "O gazeteiro — espalhava o \"segredo da Rainha\"",
            historia =
                "Em 1784, o cardeal de Rohan, que tentava recuperar o favor da Rainha, foi convencido por Jeanne de La Motte " +
                "de que Maria Antonieta queria, em segredo, um colar de diamantes de 1,6 milhão de libras. O registro do " +
                "joalheiro mostra que a compra foi negociada pelo cardeal e pela intermediária: a Rainha nunca compareceu. O " +
                "encontro noturno em Versalhes que o cardeal contava foi com uma moça disfarçada de Rainha.\n\n" +
                "A encomenda trazia a assinatura \"Marie Antoinette de France\", que a Rainha nunca usou: era falsa. Ela não " +
                "assinou nada para dar o colar ao cardeal. O colar foi desmontado e vendido em pedaços.\n\n" +
                "Em 1789, o duque de Orléans quis reacender o escândalo para manchar a Rainha. Os documentos contam outra " +
                "história: Maria Antonieta foi o nome usado numa fraude, não a autora dela.",
            fatos =
                "Seu panfleto contou o que os documentos provam: uma fraude feita em nome da Rainha. O duque não ganhou o " +
                "escândalo que encomendou, mas as ruas leram a verdade sobre o colar, e a tipografia ganhou fama de séria.",
            comBoato =
                "O panfleto juntou os documentos ao boato do encontro secreto da Rainha. Vendeu bem e agradou o duque, mas a " +
                "parte inventada caiu, e com ela o crédito do resto do texto.",
            calunia =
                "O panfleto acusou a Rainha de ter assinado e escondido o colar, sem prova nenhuma. A calúnia correu Paris, " +
                "encheu o caixa e alimentou o ódio contra Maria Antonieta, até ser desmentida e cobrar seu preço.",
            nota =
                "O escândalo é real (1785–1786). O cardeal de Rohan foi absolvido em 1786; Jeanne de La Motte foi condenada, " +
                "fugiu da prisão e publicou memórias contra a Rainha. A assinatura \"Marie Antoinette de France\" era falsa. " +
                "A encomenda do panfleto pelo duque em 1789 é invenção do jogo."
        },
        new Historia
        {
            arquivo = "Caso_Reveillon",
            teor =
                "Uma defesa de Jean-Baptiste Réveillon: o panfleto queria limpar o nome do patrão depois do saque, contando o que ele " +
                "propôs na assembleia e como tratava os seus operários.",
            personagens =
                "Jean-Baptiste Réveillon — dono da manufatura de papéis de parede; seu cliente\n" +
                "Os operários do faubourg Saint-Antoine — trabalhadores e vizinhos da fábrica\n" +
                "O operário sobrevivente — contava a história dos \"quinze soldos\"\n" +
                "O gazeteiro — espalhava que Réveillon mandava dinheiro à Inglaterra\n" +
                "A Guarda Francesa — atirou contra a multidão",
            historia =
                "Em 23 de abril de 1789, numa assembleia eleitoral, Réveillon propôs abolir as taxas cobradas nas barreiras de " +
                "Paris para baratear o pão e, com ele, os salários. No bairro, a frase chegou cortada: que ele só queria " +
                "baixar os salários, a \"quinze soldos\" por dia.\n\n" +
                "Os registros da manufatura mostram outro patrão: Réveillon pagava acima da média e manteve os salários no " +
                "inverno duríssimo de 1788. A história do dinheiro mandado à Inglaterra era calúnia.\n\n" +
                "Nos dias 27 e 28 de abril, uma multidão faminta saqueou sua casa e sua fábrica. Réveillon se refugiou na " +
                "Bastilha, e a Guarda Francesa atirou contra o povo.",
            fatos =
                "O panfleto mostrou o que Réveillon propôs e como pagava seus operários. Limpou o nome do patrão e agradou a " +
                "quem queria ordem nas ruas, mas, no faubourg, muitos o leram como a defesa de um rico enquanto enterravam seus mortos.",
            comBoato =
                "O panfleto defendeu Réveillon com um fato e um boato. A versão agradou aos poderosos e rendeu bem, até a parte " +
                "falsa ser desmentida e virar munição contra o próprio Réveillon.",
            calunia =
                "O panfleto apostou em acusações sem prova para defender Réveillon. O ganho veio rápido; quando a mentira caiu, " +
                "o Estado que você quis agradar foi o primeiro a se afastar.",
            nota =
                "A assembleia de 23/04/1789 e o motim de 27 e 28/04/1789 são reais; Réveillon se refugiou na Bastilha e a " +
                "Guarda Francesa atirou. O número de mortos varia muito entre as fontes, e elas divergem sobre o que ele " +
                "disse exatamente sobre os salários."
        },
        new Historia
        {
            arquivo = "Caso_Operario",
            teor =
                "Uma defesa do operário sobrevivente: o panfleto pedia que ele não fosse condenado como saqueador e cobrava da Guarda " +
                "Francesa a responsabilidade pelos mortos do faubourg.",
            personagens =
                "O operário do faubourg Saint-Antoine — sobrevivente do massacre; seu cliente\n" +
                "Jean-Baptiste Réveillon — dono da manufatura atacada\n" +
                "O comandante da Guarda Francesa — assinou a ordem de dispersar a multidão\n" +
                "A multidão do faubourg — operários que pediam pão e salário",
            historia =
                "No fim de abril de 1789, operários do faubourg Saint-Antoine marcharam até a manufatura de Réveillon pedindo " +
                "pão e salário. A multidão não tinha armas de fogo, e a Guarda Francesa atirou para matar.\n\n" +
                "O cartaz com a ordem do comandante prova que a ordem de dispersar \"por todos os meios\" foi afixada antes de " +
                "a multidão chegar à fábrica: os tiros não foram resposta a um incêndio. As versões que circularam depois, " +
                "de que o povo fora pago por agitadores do duque de Orléans e de que o operário ateara fogo à fábrica, " +
                "serviram para justificar o massacre.\n\n" +
                "O sobrevivente, procurado como saqueador, só tinha a própria palavra contra a da Guarda.",
            fatos =
                "O panfleto mostrou que a multidão estava desarmada e que a ordem de atirar veio antes. O operário ganhou uma " +
                "defesa pública, e o povo do faubourg viu a tipografia do seu lado.",
            comBoato =
                "O panfleto defendeu o operário com um fato e um boato. Comoveu as ruas, mas a parte falsa foi desmentida e deu " +
                "aos acusadores um motivo para duvidar de tudo.",
            calunia =
                "O panfleto acusou sem provas para defender o operário. Inflamou o bairro e vendeu muito, até a calúnia cair e " +
                "o povo que você defendia pagar pela sua pressa.",
            nota =
                "O motim de 27 e 28/04/1789 e os tiros da Guarda Francesa são reais. O boato de que o duque de Orléans pagou " +
                "agitadores nunca foi provado. O operário e o cartaz com a ordem do comandante são do jogo."
        },

        // ===== Fase 3 =====
        new Historia
        {
            arquivo = "Caso_ChampDeMars",
            teor =
                "Uma defesa de Georges Danton e dos peticionários do Champ de Mars: o panfleto sustentava que eles exerciam um direito " +
                "e denunciava os tiros da Guarda Nacional de Lafayette.",
            personagens =
                "Georges Danton — líder do clube dos Cordeliers; seu cliente\n" +
                "A sobrevivente — estava no altar da pátria e guardou um pedaço da petição\n" +
                "O médico — repetia que havia armas no altar\n" +
                "Bailly — prefeito de Paris, que decretou a lei marcial\n" +
                "Lafayette — comandante da Guarda Nacional, que abriu fogo",
            historia =
                "Em 17 de julho de 1791, depois da fuga do Rei para Varennes, milhares de parisienses foram ao Champ de Mars " +
                "assinar, no altar da pátria, uma petição contra a volta de Luís XVI ao trono. O pedaço da petição que a " +
                "sobrevivente guardou, manchado de sangue, mostra as assinaturas feitas uma a uma: quem assinava estava desarmado.\n\n" +
                "O prefeito Bailly decretou a lei marcial, e a Guarda Nacional de Lafayette atirou na multidão. Depois vieram " +
                "os mandados contra os líderes dos Cordeliers, e Danton foi acusado de armar uma insurreição.\n\n" +
                "A Constituição de 1791 garantia o direito de se reunir em paz e sem armas e de assinar petições. Danton " +
                "exerceu um direito; a história das armas no altar e da lei que proibia a petição era falsa.",
            fatos =
                "O panfleto pôs a petição ensanguentada e a Constituição lado a lado. A acusação contra Danton perdeu força nas " +
                "ruas, e o povo de Paris viu quem tinha atirado primeiro.",
            comBoato =
                "O panfleto defendeu Danton misturando um fato a um boato. Inflamou os Cordeliers, mas a parte inventada foi " +
                "desmentida e deu argumentos a quem queria vê-lo preso.",
            calunia =
                "O panfleto apostou numa calúnia para defender Danton. As ruas leram com fúria, até a mentira cair e o povo " +
                "pagar o preço da sua defesa apressada.",
            nota =
                "O massacre do Champ de Mars (17/07/1791) é real. Houve mandado contra Danton, mas não julgamento: ele fugiu " +
                "para a Inglaterra e foi coberto pela anistia de setembro de 1791. Danton foi guilhotinado em abril de 1794."
        },
        new Historia
        {
            arquivo = "Caso_Varennes",
            teor =
                "O processo de Kornmann contado como prova da corrupção dos antigos ministros: o panfleto acusava gente do poder de " +
                "proteger quem tinha amigos influentes, enquanto a lei valia só para os outros.",
            personagens =
                "Guillaume Kornmann — banqueiro; seu cliente\n" +
                "A senhora Kornmann — a esposa, acusada de adultério\n" +
                "Beaumarchais — escritor que interveio a favor dela\n" +
                "Lenoir — tenente-general de polícia\n" +
                "Nicolas Bergasse — advogado que escreveu os mémoires de Kornmann\n" +
                "Georges Danton — dizia que o diário fora forjado",
            historia =
                "Desde 1787, Guillaume Kornmann acusava a esposa de adultério e, pelos mémoires do advogado Bergasse, acusava " +
                "também o amante dela, o escritor Beaumarchais e o tenente de polícia Lenoir de a protegerem.\n\n" +
                "O diário, escrito na letra da própria esposa, fala da proteção de gente do ministério e da polícia. E o mémoire " +
                "do próprio Beaumarchais confirma: em 1781 ele pediu à polícia que tirasse a senhora Kornmann da casa de " +
                "correção onde o marido a mandara trancar. A esposa teve, sim, protetores poderosos; o diário não era falso, e " +
                "Beaumarchais sabia muito bem quem ela era.\n\n" +
                "Em 1792, com a nova lei do divórcio, Kornmann quis transformar o processo num panfleto contra os antigos ministros.",
            fatos =
                "O panfleto mostrou, com o diário e o mémoire de Beaumarchais, como o poder dos antigos ministros protegia " +
                "quem tinha amigos. Agradou à nova ordem, que gostava de ver o Antigo Regime exposto, e Kornmann pagou bem.",
            comBoato =
                "O panfleto de Kornmann misturou um fato a um boato. Rendeu bem e agradou aos poderosos, mas a parte falsa " +
                "foi desmentida e virou o processo contra o próprio banqueiro.",
            calunia =
                "O panfleto usou uma calúnia contra os protetores da esposa. O escândalo vendeu, até a mentira ser exposta e " +
                "deixar o Estado que você quis agradar de mãos sujas.",
            nota =
                "Guillaume Kornmann, os mémoires de Bergasse (desde 1787) e a intervenção de Beaumarchais em 1781 são reais; " +
                "na história, Beaumarchais foi adversário de Kornmann. O divórcio foi legalizado em 20/09/1792. O diário da " +
                "esposa é invenção do jogo."
        },
        new Historia
        {
            arquivo = "Caso_Assignats",
            teor =
                "Uma defesa de Pierre-Paul Sirven e da família: o panfleto denunciava um julgamento feito sem provas, movido pelo ódio " +
                "aos protestantes, e pedia que o pai não fosse condenado pela morte da filha.",
            personagens =
                "Pierre-Paul Sirven — protestante, pai de Élisabeth; seu cliente\n" +
                "Élisabeth Sirven — a filha, encontrada morta num poço\n" +
                "O médico local — tratou Élisabeth depois do convento\n" +
                "O bispo de Castres — mandou interná-la num convento\n" +
                "Guillaume Kornmann — repetia que ela queria se converter\n" +
                "Voltaire — escreveu o Tratado sobre a Tolerância",
            historia =
                "Élisabeth, filha do protestante Pierre-Paul Sirven, foi internada num convento por ordem do bispo de Castres, " +
                "para ser convertida ao catolicismo. Voltou para casa transtornada: o médico que a tratou conta que ela falava " +
                "com sombras e fugia de casa de madrugada. Pouco depois, foi encontrada morta num poço.\n\n" +
                "Os juízes acusaram o pai de matá-la para impedir a conversão, a mesma acusação que levara o protestante Jean " +
                "Calas ao suplício em 1762, antes de ser reabilitado em 1765. A história de que Élisabeth saiu do convento sã e " +
                "decidida a se converter era boato, e a \"confissão de Calas\" era calúnia.\n\n" +
                "O Tratado sobre a Tolerância de Voltaire nasceu do caso Calas e deu força à defesa dos Sirven.",
            fatos =
                "O panfleto contou como Élisabeth vivia e lembrou o caso Calas. A acusação contra o pai perdeu o chão, e o " +
                "caso virou um exemplo de justiça feita com preconceito.",
            comBoato =
                "O panfleto defendeu os Sirven com um fato e um boato. Comoveu muitos leitores, mas a parte falsa foi " +
                "desmentida e deu munição aos juízes.",
            calunia =
                "O panfleto usou uma calúnia na defesa. Vendeu bem, até a mentira cair e manchar tanto a família quanto a tipografia.",
            nota =
                "O caso é real (Castres, 1760–1771): Élisabeth foi internada num convento, voltou perturbada e foi achada num " +
                "poço; a família fugiu, foi condenada à revelia e reabilitada com a campanha de Voltaire. O médico e o laudo " +
                "são do jogo."
        },

        // ===== Fase 4 =====
        new Historia
        {
            arquivo = "Caso_Jornalista",
            teor =
                "O pedido de clemência de Camille Desmoulins: o panfleto dizia ao povo que a Lei dos Suspeitos prendia inocentes e " +
                "pedia que a Revolução parasse de devorar os seus próprios filhos.",
            personagens =
                "Camille Desmoulins — jornalista; seu cliente\n" +
                "Maximilien Robespierre — colega de escola de Camille e membro do Comitê de Salvação Pública\n" +
                "Jacques-Pierre Brissot — dizia que a lista só tinha aristocratas\n" +
                "Os presos da Lei dos Suspeitos — gente comum presa por \"indiferença\"",
            historia =
                "Com a Lei dos Suspeitos, de setembro de 1793, as prisões de Paris se encheram. A lista que Camille copiou dos " +
                "registros de prisão mostra alfaiates, viúvas, padeiros e escrivães presos por \"indiferença\" ou por uma palavra " +
                "mal dita, não só aristocratas e banqueiros.\n\n" +
                "O rascunho do pedido de clemência traz anotações de Robespierre, na letra dos relatórios dele: ele risca frases, " +
                "mas escreve \"publicar\". Não era falsificação de Camille.\n\n" +
                "Defender a clemência era dizer ao Comitê que a Revolução prendia inocentes. Camille levou essa defesa ao Tribunal Revolucionário.",
            fatos =
                "O panfleto mostrou a lista e as anotações de Robespierre. Diante das provas, o tribunal não pôde condenar Camille.",
            comBoato =
                "O panfleto misturou as provas a um boato. A parte falsa deu ao tribunal o pretexto de que precisava.",
            calunia =
                "O panfleto apostou numa calúnia. A mentira foi usada contra Camille, e a defesa virou prova de acusação.",
            nota =
                "Camille Desmoulins é real: colega de escola de Robespierre, fez a campanha de clemência no Vieux Cordelier " +
                "entre dezembro de 1793 e janeiro de 1794. Foi preso em março e guilhotinado em 5 de abril de 1794. A lista e " +
                "o manuscrito anotado são documentos do jogo, inspirados em fatos reais."
        },
        new Historia
        {
            arquivo = "Caso_Negociante",
            teor =
                "Uma defesa do pequeno mercador acusado de açambarcamento: o panfleto apontava o verdadeiro dono da farinha do porão e " +
                "as falhas das leis de abastecimento que esvaziavam as padarias.",
            personagens =
                "O pequeno mercador — acusado de esconder farinha; seu cliente\n" +
                "O atravessador — fornecedor dos exércitos, dono da farinha do porão\n" +
                "Camille Desmoulins — repetia que a farinha era do mercador\n" +
                "Os comissários da seção — fiscalizavam estoques e preços",
            historia =
                "No outono de 1793, a fome apertava Paris, e a lei contra o açambarcamento punia com a morte quem escondesse " +
                "gêneros. As faturas mostram que a farinha do porão era de um fornecedor dos exércitos, que alugava o porão; o " +
                "mercador só recebia o aluguel.\n\n" +
                "E a farinha sumia das padarias pequenas por causa da própria lei do Máximo: o preço fixado, o de 1790 mais um " +
                "terço e sem contar o frete, não pagava o transporte, e a seção avisava da falta. A história de que as padarias " +
                "estavam cheias era falsa.\n\n" +
                "O pequeno mercador era o culpado mais fácil de achar para uma multidão com fome.",
            fatos =
                "O panfleto mostrou as faturas e a falha das leis de abastecimento. Diante das provas, o tribunal absolveu o mercador.",
            comBoato =
                "O panfleto misturou as faturas a um boato. A parte falsa caiu, e o tribunal ficou com a desculpa de que precisava.",
            calunia =
                "O panfleto apostou numa calúnia. A mentira foi usada contra o próprio mercador, e a multidão teve o culpado que queria.",
            nota =
                "A lei contra o açambarcamento (26/07/1793, com pena de morte) e o Máximo geral (29/09/1793, preços de 1790 " +
                "mais um terço) são reais. O mercador, o atravessador e as faturas são do jogo."
        },
        new Historia
        {
            arquivo = "Caso_Girondina",
            teor =
                "Uma defesa dos deputados girondinos: o panfleto mostrava ao povo o que as provas da acusação diziam de fato e o quanto " +
                "o julgamento estava sendo apressado.",
            personagens =
                "Pierre Vergniaud — deputado girondino e grande orador; seu cliente\n" +
                "Jacques-Pierre Brissot — deputado girondino, autor das cartas aos departamentos\n" +
                "Maximilien Robespierre — membro do Comitê de Salvação Pública\n" +
                "Camille Desmoulins — falava de um plano para separar as províncias\n" +
                "O Tribunal Revolucionário — julgou os vinte e um deputados",
            historia =
                "Em outubro de 1793, vinte e um deputados girondinos foram julgados pelo Tribunal Revolucionário, acusados de " +
                "federalismo. As cartas de Brissot aos departamentos pediam que defendessem a Convenção da pressão da Comuna de " +
                "Paris, não que se separassem da República. O \"plano das províncias\" era boato.\n\n" +
                "Enquanto a eloquência de Vergniaud comovia as galerias, uma nota de Robespierre, arquivada no Comitê, pedia ao " +
                "tribunal que abreviasse os debates e condenasse depressa. Os girondinos não pediram o fim dos debates nem confessaram.\n\n" +
                "O processo não buscava provas: buscava terminar antes que o público mudasse de lado.",
            fatos =
                "O panfleto mostrou as cartas e a nota de Robespierre. Diante das provas, o tribunal absolveu os acusados.",
            comBoato =
                "O panfleto misturou as provas a um boato. A parte falsa foi desmentida e serviu ao Comitê para acelerar a condenação.",
            calunia =
                "O panfleto apostou numa calúnia. A mentira caiu, e o tribunal teve o pretexto para encerrar os debates e condenar.",
            nota =
                "O julgamento dos girondinos (24 a 30/10/1793) é real. Em 29/10/1793, um decreto permitiu encerrar os debates " +
                "depois de três dias; os condenados foram guilhotinados em 31/10/1793. A nota de Robespierre é invenção do GDD."
        },
    };
}
