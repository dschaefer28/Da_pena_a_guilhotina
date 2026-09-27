# Matriz de casos — documentação de autor

**Uso interno. Não exibir ao jogador.** A coluna "verdade" revela a confiabilidade real das pistas.

Gerada com o Prompt 2 em 25/09/2026 e revista nos Prompts 3, 4 e 7 (STATUS §5, §6, §13, §15 e §17). **Em 27/09/2026 o conteúdo narrativo das Fases 2, 3 e 4 foi alinhado ao GDD** ("Da Pena à Guilhotina", IFPR, 2026), com a planilha "30 Casos Reais" como referência histórica (STATUS §18). Fonte dos dados: `Assets/Editor/NarrativaGddSetupTool.cs` (textos, diálogos, NPCs, reações e objetos; **Ferramentas > Campanha > 8**), `Assets/Editor/CampanhaSetupTool.cs` (estrutura e valores das receitas), `Assets/Editor/BibliotecaSetupTool.cs` (preços e reforços), `Assets/Editor/DeducaoSetupTool.cs` (pares) e **Ferramentas > Campanha > 2 - Validar campanha**.

Do GDD vêm os casos, os clientes e as testemunhas, as falas-base de cada NPC, as duas pistas de cada caso (os dois fatos) e os títulos dos panfletos das Fases 3 e 4. **Foram escritos para a dedução e continuam provisórios:** boatos, calúnias, cartas do cliente, falas de quem espalha boatos, falas "pouco úteis", documentos da biblioteca, etapas complementares, textos de revelação e os ramos novos das conversas. As adaptações históricas estão na seção "Adaptações e divergências", no fim desta matriz.

## Regras comuns

- **Custo:** toda interação (NPC ou objeto) custa **1h** na primeira vez no caso; repetir é grátis. Orçamento **6h** nas Fases 2, 3 e 4 (decisão do grupo, 27/09); **5h** com dívida. Com o inventário cheio, uma interação que entregaria algo não cobra.
- **Oportunidades:** Fases 2 e 4, **7** por caso (4 NPCs + 3 objetos); Fase 3, **8** (5 NPCs do GDD + 3 objetos). Por caso: 2 fatos, 1 boato (de um NPC), 1 calúnia (de um objeto; na Fase 2, no caso de Réveillon, de um NPC) e as demais oportunidades sem pista. Todo NPC tem uma fala própria para cada caso da fase, mesmo quando não entrega nada: ela dá contexto ou outra perspectiva, nunca uma pista.
- **Quem entrega os fatos:** o GDD define. Na Fase 2 e nos casos de Kornmann, Desmoulins e do mercador, o próprio cliente entrega o fato 1. No caso de Danton, o fato 1 vem da Sobrevivente; no de Sirven, do Médico; no dos girondinos, de Brissot (os clientes Danton, Sirven e Vergniaud só conversam e apontam as testemunhas). O fato 2 vem de um objeto, exceto no colar da rainha, em que o joalheiro entrega os dois.
- **Caminho mínimo até dois fatos:** **2h** (colar da rainha: **1h**), dentro do orçamento com dívida.
- **Dedução ativa (Fases 2, 3 e 4):** dois pares contraditórios por caso, exatamente uma afirmação verdadeira em cada: **par 1** = fato 1 × boato; **par 2** = fato 2 × calúnia. As alegações ficam fora. O quadro inteiro custa **4h** (colar: 3h), dentro das 6h e das 5h com dívida. As pistas levam de uma fonte à outra: **o cliente aponta quem espalha o boato** (e a testemunha, quando o fato 1 não vem dele) e **quem espalha o boato cita os dois objetos**. No colar, a carta do duque aponta o gazeteiro. Conferir o quadro não custa nada e não é exigido para publicar.
- **Conversas:** a primeira fala de cada NPC traz as pistas de caminho; as escolhas (2 ou 3, quando fazem sentido) acrescentam contexto, outra perspectiva ou um indício de confiabilidade (ex.: quem espalha o boato admite que não viu nada). O item é entregue no fim da conversa, qualquer que seja o ramo. Depois da entrega, o NPC diz uma fala curta (`dialogoDepoisDaEntrega`); uma etapa complementar pendente tem prioridade sobre ela.
- **Pré-requisitos:** nenhum na fala normal. Etapas complementares (Prompt 3) exigem uma evidência já obtida.
- **Nomes neutros:** o nome da pista é só o assunto. O validador recusa prefixos que denunciem a verdade ("Conversa sobre", "Libelo", "Folha:", "Cartaz:", "Denúncia:", "Bilhete", "Boato", "Calúnia", "Rumor").
- **Saída sem bloqueio (proposta de design, não literal dos PDFs):** ao aceitar o caso, o jogador recebe 2 **alegações do cliente** (fonte "Carta do cliente"). Qualquer impressão com uma alegação (e sem calúnia) usa a versão **Só Alegações**.
- **Versões de receita:** Calúnia (qualquer calúnia ou dois boatos), Só Alegações (alguma alegação), Fatos (fato + fato), ComBoato (fato + boato). Valores no formato Povo / Estado / Ouro; as penalidades (Povo / Estado) são cobradas quando o boato é exposto, no fim da fase (`GameManager.EncerrarFase`). **Os valores não mudaram com o conteúdo do GDD.**
- **Arquivamento:** ao concluir um caso antes da Fase 4, alegações, sobras de pistas e documentos de apoio saem do inventário, mas ficam no histórico. No caso da Fase 4 só as alegações saem; o resto vai ao tribunal. Panfletos sempre ficam.

## Correspondência de arquivos

O save guarda casos pelo nome do asset (`CaseData.name`), pistas pelo `itemID` e interações pelo `idDaInteracao`. Por isso os arquivos e IDs antigos foram mantidos, e só os textos mudaram:

| Arquivo do caso | Caso (GDD) | Rota | NPCs (ID de interação → personagem) | Objetos (ID → nome) |
|---|---|---|---|---|
| `Caso_Joalheiro` | O Colar da Rainha | — | `Joalheiro`, `Jean-Baptiste Réveillon`, `Operario`, `Gazeteiro` (sem mudança) | `BancaDePanfletos`, `MuroDeCartazes`, `MesaDaTaverna` (sem mudança) |
| `Caso_Reveillon` | Os Trabalhadores de Réveillon | — | idem | idem |
| `Caso_Operario` | O Operário Acusado | — | idem | idem |
| `Caso_ChampDeMars` | O Julgamento de Georges Danton | — | `GeorgesDanton` → Georges Danton (**novo**, x 46); `PeticionariaLacombe` → Sobrevivente; `ViuvaFrancois` → Médico; `CocheiroJoubert` → Guillaume Kornmann; `GravadorMorel` → Pierre-Paul Sirven | `BalcaoDaPadaria` → Mesa do Café; `MuralDosCordeliers`; `CaixaDeTipos` |
| `Caso_Varennes` | Adultério e Poder Ministerial | — | idem | idem |
| `Caso_Assignats` | Morte no Poço e Intolerância | — | idem | idem |
| `Caso_Jornalista` | O Manifesto da Clemência | A | `RedatorMarchand` → Camille Desmoulins; `ComissarioVautrin` → Mercador; `CidadaDelorme` → Pierre Vergniaud; `Carcereiro` → Jacques-Pierre Brissot | `ArquivoDaSecao` → Arquivo do Comitê; `ParedeDeEditais`; `CaixaDeDenuncias` |
| `Caso_Negociante` | O Caso do Pequeno Mercador de Grãos | B | idem | idem |
| `Caso_Girondina` | O Extermínio da Oposição | C | idem | idem |

Os arquivos de pista também guardam o nome antigo do assunto (ex.: `Pista_Varennes_Contrato` é "O diário da esposa"). Os diálogos seguem a convenção `<ID do NPC>_<arquivo do caso>` (entrada), com `_R1`/`_R2`/`_R3` para os ramos e `_Depois` para a fala depois da entrega; os do cliente da Fase 2 ficam nas pastas do grupo (`Dialogos/Jean-Baptiste Réveillon`, `Dialogos/Operário`, `Dialogos/Joalheiro`).

## Fase 2 — cena `Fase2` (1789, concluir 1 de 3)

NPCs: Joalheiro (x 12,9), Operário (29,2), Jean-Baptiste Réveillon (52,1), Gazeteiro (0). Objetos: Banca de Panfletos (−12), Muro de Cartazes (40), Mesa da Taverna (68). O caso de Réveillon e o do operário são as **duas perspectivas do motim de abril de 1789**: a do dono da manufatura, acusado de querer matar o povo de fome, e a do operário sobrevivente, acusado de participar do saque.

| Caso | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **O Colar da Rainha** (`Caso_Joalheiro`; cliente: duque de Orléans, por carta) | Joalheiro → Registro de compra (Fato) + Assinatura da encomenda (Fato) · Gazeteiro → O segredo da Rainha (Boato) · Muro → A Rainha e o Cardeal (Calúnia) | Réveillon, Operário, Banca, Mesa da Taverna | 50/−10/20 · 70/−20/35 (pen −30/−5) · 90/−30/50 (pen −50/−15) · 20/−5/8 (pen −5/0) |
| **Os Trabalhadores de Réveillon** (`Caso_Reveillon`) | Réveillon → O preço do pão (Fato) · Banca → Os salários da manufatura (Fato) · Operário → Os quinze soldos (Boato) · Gazeteiro → O dinheiro inglês (Calúnia) | Joalheiro, Muro, Mesa | −20/50/50 · −25/60/70 (pen −10/−25) · −35/75/90 (pen −15/−45) · −8/20/20 (pen 0/−5) |
| **O Operário Acusado** (`Caso_Operario`) | Operário → Desejo de morte (Fato) · Muro → A ordem do comandante (Fato) · Réveillon → Os agitadores do duque (Boato) · Mesa → O fogo na fábrica (Calúnia) | Joalheiro, Gazeteiro, Banca | 60/−20/10 · 75/−25/25 (pen −30/−5) · 90/−35/40 (pen −50/−15) · 25/−8/5 (pen −5/0) |

| Caso | Par 1 | Par 2 | Pistas de caminho |
|---|---|---|---|
| Colar | a venda foi negociada por um cardeal e uma intermediária; a Rainha não compareceu × a Rainha compareceu disfarçada e negou tudo | a encomenda traz "Marie Antoinette de France", assinatura que ela não usava × a Rainha assinou de próprio punho para dar o colar ao Cardeal | carta do duque → gazeteiro; gazeteiro → muro de cartazes (o fato 2 vem do joalheiro) |
| Réveillon | propôs abolir as taxas das barreiras para baratear o pão e, com ele, os salários × pediu só o corte, sem falar do pão ("quinze soldos") | pagava acima da média e manteve os salários no inverno de 1788 × no inverno cortou os salários e mandou a diferença à Inglaterra | fala e carta de Réveillon → operários; Operário → banca de panfletos e gazeteiro |
| Operário | a Guarda atirou para matar numa multidão sem armas de fogo que pedia pão × a multidão veio armada, paga por agitadores do duque de Orléans | a ordem de dispersar "por todos os meios" foi afixada antes de a multidão chegar à manufatura × a Guarda só recebeu ordem depois que o operário ateou fogo à fábrica | fala e carta do operário → Réveillon; Réveillon → muro de cartazes e mesa da taverna |

Conversas: Réveillon (sequências 1 a 3 do GDD, com dois ramos em cada escolha: "nervoso" cita o refúgio na Bastilha; "revoltar" traz a versão mais dura dele); Operário (sequências 1 e 2 do GDD, com um ramo sobre a acusação); gazeteiro, operário e Réveillon como fontes de boato, com dois ramos cada (indício de que é boato ouvido e contexto: "Madame Déficit", voto censitário, preço do pão, Palais-Royal). O roteiro do joalheiro (grupo, 27/09) não mudou.

Estimativas da mesa (Ouro / Povo / Estado): Colar 20 / +50 / −10; Réveillon 50 / −20 / +50; Operário 10 / +60 / −20.

## Fase 3 — cena `Fase3` (1791–1792, concluir 1 de 3)

NPCs: Médico (−8), Guillaume Kornmann (14), Sobrevivente (34), **Georges Danton (46, novo, do `NPCBasic.prefab`)**, Pierre-Paul Sirven (58). Objetos: Mesa do Café (−16), Mural dos Cordeliers (24), Caixa de Tipos (70). O GDD situa a fase num "local público da cidade" e num "Café Popular": o antigo balcão da padaria virou a mesa do café. Escolhido um caso, os outros dois ficam bloqueados; concluído, a volta ao escritório abre a Fase 4.

| Caso | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **O Julgamento de Georges Danton** (`Caso_ChampDeMars`) | Sobrevivente → A petição manchada de sangue (Fato) · Mural → A Constituição de 1791: direito de reunião (Fato) · Médico → As armas do altar (Boato) · Mesa do Café → A lei das petições (Calúnia) | Danton (cliente, só conversa), Kornmann, Sirven, Caixa | 20/−10/30 · 30/−15/40 (pen −10/−5) · 40/−20/55 (pen −20/−10) · 8/−4/10 (pen −5/0) (exemplo do Prompt 3) |
| **Adultério e Poder Ministerial** (`Caso_Varennes`) | Kornmann → O diário da esposa (Fato) · Mesa do Café → A colaboração de Beaumarchais (Fato) · Danton → A letra do diário (Boato) · Caixa → O nome de Beaumarchais (Calúnia) | Sobrevivente, Médico, Sirven, Mural | −20/30/45 · −30/40/60 (pen −10/−10) · −40/50/75 (pen −20/−15) · −8/12/18 (pen 0/−5) |
| **Morte no Poço e Intolerância** (`Caso_Assignats`) | Médico → O testemunho do médico local (Fato) · Caixa → O Tratado sobre a Tolerância (Fato) · Kornmann → A conversão de Élisabeth (Boato) · Mural → A confissão de Calas (Calúnia) | Sirven (cliente, só conversa), Danton, Sobrevivente, Mesa | 15/10/35 · 20/15/50 (pen −10/−10) · 25/20/65 (pen −15/−15) · 5/4/14 (pen −5/0) |

| Caso | Par 1 | Par 2 | Pistas de caminho |
|---|---|---|---|
| Danton | pedaço da petição do altar, manchado na hora dos tiros, com assinaturas uma a uma; quem assinava estava desarmado × no altar não se assinava nada: Danton armou o povo e a folha foi escrita depois | a Constituição de 1791 garante a reunião pacífica e sem armas e a petição assinada individualmente × "a nova lei proíbe reunir o povo contra o Rei; Danton quis mártires" | Danton → Sobrevivente e Médico; Médico → mural e mesa do café |
| Kornmann | o diário, na letra da esposa, fala da proteção de gente do ministério e do tenente de polícia × Kornmann escreveu o diário imitando a letra dela | mémoire do próprio Beaumarchais: em 1781 ele pediu à polícia que tirasse a senhora Kornmann da casa de correção × "Beaumarchais nunca ouviu falar dela; o banqueiro inventou o nome" | Kornmann → Danton; Danton → mesa do café e caixa de tipos |
| Sirven | laudo do médico: depois do convento, Élisabeth falava com sombras e fugia de casa de madrugada (delírio) × ela saiu do convento sã, decidida a se converter, e o pai a trancava | o Tratado (1763) nasceu do caso Calas, reabilitado em 1765 × "Calas confessou no suplício; o Tratado é mentira paga por Genebra" | Sirven → Médico e Kornmann; Kornmann → caixa de tipos e mural |

Etapa complementar: **Kornmann**, etapa `recibo_da_estalagem` (ID mantido), depois de obter "A colaboração de Beaumarchais" → **A ordem de reclusão de 1781** (documento de apoio, mesmo reforço da biblioteca).

## Fase 4 — cena `Fase4` (outono de 1793, um caso por rota)

NPCs: Camille Desmoulins (−6), Mercador (18), Pierre Vergniaud (40), Jacques-Pierre Brissot (62). Objetos: Arquivo do Comitê (−15), Parede de Editais (30), Caixa de Denúncias (74). A mesa mostra só o caso da rota travada; depois de publicado, a ida ao tribunal é pelo Dupaty, no escritório. As rotas e os valores das receitas são os de antes (ver "Adaptações e divergências").

| Caso (rota) | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **O Manifesto da Clemência** (`Caso_Jornalista`, A) | Desmoulins → A lista de 'suspeitos' inocentes (Fato) · Arquivo → O manuscrito de Robespierre (Fato) · Brissot → Os nomes da lista (Boato) · Caixa → As anotações do rascunho (Calúnia) | Mercador, Vergniaud, Parede | 20/−20/15 · 30/−30/25 (pen −10/−5) · 40/−40/35 (pen −20/−10) · 8/−8/6 (pen −5/0) |
| **O Caso do Pequeno Mercador de Grãos** (`Caso_Negociante`, B) | Mercador → As faturas do atravessador (Fato) · Parede → A insuficiência do Máximo (Fato) · Desmoulins → A farinha do porão (Boato) · Caixa → As prateleiras vazias (Calúnia) | Vergniaud, Brissot, Arquivo | −15/30/60 · −25/40/80 (pen −5/−10) · −35/50/100 (pen −10/−20) · −6/12/24 (pen 0/−5) |
| **O Extermínio da Oposição** (`Caso_Girondina`, C) | Brissot → Provas de federalismo (Fato) · Arquivo → O testemunho de Robespierre (Fato) · Desmoulins → O plano das províncias (Boato) · Parede → O fim dos debates (Calúnia) | Vergniaud (cliente, só conversa), Mercador, Caixa | 10/10/30 · 15/15/45 (pen −10/−10) · 20/20/60 (pen −15/−15) · 4/4/12 (pen −5/−5) |

| Caso | Par 1 | Par 2 | Pistas de caminho |
|---|---|---|---|
| Desmoulins | lista copiada dos registros de prisão: alfaiate, viúva, padeiros, escrivão, presos por "indiferença" ou palavras × só aristocratas e banqueiros, e metade dos nomes inventada | rascunho de Camille com anotações de Robespierre, na letra dos relatórios dele: risca frases, mas escreve "publicar" × "as anotações são falsificação do próprio Camille" | Desmoulins → Brissot; Brissot → arquivo do Comitê e caixa de denúncias |
| Mercador | faturas: a farinha é de um fornecedor dos exércitos, que aluga o porão; o mercador só recebe o aluguel × os sacos são dele, que compra a farinha do bairro para revender pelo triplo | tabela do Máximo (preço de 1790 mais um terço, sem frete) e aviso da seção sobre a falta de farinha nas padarias pequenas × "desde o Máximo as padarias estão cheias; só o mercador esconde" | Mercador → Desmoulins; Desmoulins → parede de editais e caixa de denúncias |
| Girondinos | cartas de Brissot aos departamentos: defender a Convenção da pressão da Comuna, sem separar as províncias × as cartas mandam separar as províncias e abrir os portos aos ingleses | nota de Robespierre, arquivada no Comitê, pedindo que o tribunal abrevie os debates e condene depressa × "os próprios girondinos pediram o fim dos debates porque confessaram" | Vergniaud → Brissot e Desmoulins; Desmoulins → arquivo do Comitê e parede de editais |

Etapa complementar: **Vergniaud**, etapa `carta_da_secao` (ID mantido), depois de obter "O testemunho de Robespierre" → **As notas da defesa de Vergniaud** (documento de apoio, mesmo reforço da biblioteca).

## Biblioteca e documentos de apoio (Prompt 3; textos revistos com o conteúdo do GDD)

Documentos de apoio: confiabilidade `NaoEPista`, qualidade **Superior**, não vão nos dois slots, não são gastos e não entram no quadro de dedução. Cada receita das Fases 3/4 tem **um** qualificador (`apoio_<caso>`), que soma os valores abaixo à versão impressa, uma vez por publicação. A biblioteca não vende um documento se o jogador já tiver outro que ativa o mesmo reforço. Cada documento dá **contexto** para julgar um dos pares, sem dizer qual pista é verdadeira. Os documentos da biblioteca são reais (o que eles afirmam é histórico); preços e reforços não mudaram.

| Caso | Oferta (preço) | Contexto | Outra fonte do apoio | Reforço Povo / Estado / Ouro |
|---|---|---|---|---|
| Danton | Folhas da petição do Champ de Mars (20) | par 1: milhares de assinaturas no altar em 17/07/1791, muitas com uma cruz | — | **+20 / +5 / +20** → Fatos passa de 20/−10/30 para **40/−5/50** (exemplo do Prompt 3) |
| Kornmann | O mémoire de Bergasse (1787) (25) | par 2: Bergasse acusou a esposa, o amante, Beaumarchais e Lenoir de livrá-la do marido | **Kornmann**, etapa `recibo_da_estalagem`, depois de "A colaboração de Beaumarchais" → A ordem de reclusão de 1781 | 0 / +10 / +20 |
| Sirven | Registro do convento de Castres (20) | par 1: Élisabeth internada por ordem do bispo e devolvida porque já não estava em seu juízo | — | +5 / +10 / +15 |
| Desmoulins (A) | A Lei dos Suspeitos (17 de setembro de 1793) (25) | par 1: critérios vagos de suspeição e listas dos comitês de vigilância | — | +10 / 0 / +10 |
| Mercador (B) | Decreto contra o açambarcamento (26 de julho de 1793) (30) | par 1: o crime é guardar gêneros sem vendê-los; estoques declarados e inspecionados | — | 0 / +10 / +25 |
| Girondinos (C) | Decreto de 8 de brumário do ano II (25) | par 2: em 29/10/1793 a Convenção permitiu encerrar o julgamento depois de três dias | **Vergniaud**, etapa `carta_da_secao`, depois de "O testemunho de Robespierre" → As notas da defesa de Vergniaud | +5 / +5 / +15 |

## Adaptações e divergências (conteúdo do GDD)

O que é histórico e o que é adaptação do jogo. Nenhum texto do jogo dá como documentado o que está na coluna da direita.

| Caso | Base histórica | Adaptação do GDD / do jogo |
|---|---|---|
| Colar da Rainha | Escândalo de 1785–1786; o cardeal de Rohan foi absolvido em 1786; a assinatura "Marie Antoinette de France" era falsa (a Rainha não assinava assim); o colar valia 1,6 milhão de libras; Jeanne de La Motte fugiu da prisão e publicou memórias; o retrato "en chemise" de Vigée Le Brun foi retirado do Salão de 1783 | O caso é retomado em 1789; o duque de Orléans encomenda o panfleto (GDD); as falas do joalheiro são dramatização do grupo |
| Réveillon / Operário | Assembleia eleitoral de 23/04/1789; motim de 27 e 28/04/1789; Réveillon se refugiou na Bastilha; pão de 4 libras a 14,5 soldos; a Guarda Francesa atirou; o número de mortos varia muito entre as fontes; o boato de que o duque de Orléans pagou agitadores nunca foi provado | As falas são dramatizações (as sequências do GDD, corrigidas para a proposta real: abolir as taxas das barreiras para baratear o pão). O cartaz com a ordem do comandante é um documento do jogo. **Não se afirma** que Réveillon nunca falou em quinze soldos (as fontes divergem); o boato é o de que ele pediu só o corte |
| Danton | Massacre de 17/07/1791; lei marcial de Bailly, Guarda de Lafayette; a petição do altar; mandado contra Danton, que fugiu para a Inglaterra e foi coberto pela anistia de setembro de 1791; a Constituição de 1791 (Título I) garante reunião pacífica e petição assinada individualmente | Não houve julgamento de Danton em 1791: o título é do GDD, e o texto da mesa fala em mandado e acusação. No jogo, Danton está em Paris e fala com Julien |
| Kornmann | Guillaume Kornmann, banqueiro; mémoires de Bergasse (a partir de 1787) contra a esposa, o amante, Beaumarchais e Lenoir; em 1781 Beaumarchais interveio pela soltura da senhora Kornmann; divórcio legalizado em 20/09/1792 | O caso é levado a 1792, com Julien no papel do advogado; o "diário da esposa" é invenção do GDD. **Divergência:** o GDD fala em "colaboração com Beaumarchais" (ele "oferece ajuda"); na história ele foi adversário de Kornmann. O jogo usa a intervenção real dele (a pista se chama "A colaboração de Beaumarchais") |
| Sirven | Castres, 1760–1771: Élisabeth internada num convento por ordem do bispo, devolvida perturbada, achada num poço; a família fugiu, foi condenada à revelia e reabilitada com a campanha de Voltaire; Calas foi supliciado em 1762 e reabilitado em 1765 | O GDD traz o caso para a Fase 3 (1792) e cita as "leis de 1791": os textos do caso não dão ano. O médico e o laudo são personagens/documentos do jogo; "temia a água" (GDD) virou "fugia de casa de madrugada" |
| Desmoulins | Colega de escola de Robespierre; Lei dos Suspeitos (17/09/1793); campanha de clemência (dezembro de 1793 a janeiro de 1794); Robespierre leu as primeiras provas do jornal dele; preso em março e guilhotinado em abril de 1794 | A Fase 4 condensa o outono de 1793: a campanha aparece junto do julgamento dos girondinos. A lista de inocentes e o manuscrito anotado são documentos do GDD, inspirados em fatos reais. "A Revolução devora seus próprios filhos" é o título do panfleto no GDD; a frase é atribuída a Vergniaud (março de 1793), e o jogo a põe na boca dele |
| Mercador | Máximo geral (29/09/1793, preços de 1790 mais um terço); lei contra o açambarcamento (26/07/1793, pena de morte); comissários de seção | O mercador, o atravessador e as faturas são do jogo. O GDD situa o caso em 1794; a Fase 4 o põe no outono de 1793 |
| Girondinos | Julgamento de 24 a 30/10/1793, 21 acusados; decreto de 8 de brumário (29/10/1793) que permitiu encerrar os debates depois de três dias; execução em 31/10/1793; Toulon entregue aos ingleses por monarquistas (agosto de 1793) | A nota de Robespierre ("O testemunho de Robespierre") é invenção do GDD; as cartas de Brissot são resumidas pela posição girondina. Vergniaud e Brissot aparecem na rua em todas as rotas porque a cena é uma só |

**Divergências de mecânica deixadas para o grupo decidir** (nada foi mudado):
- **Rotas e valores da Fase 4:** o GDD pede "impacto máximo" na Opinião Popular para o mercador e na do Estado para os girondinos. Mantidos a rota e os valores de cada arquivo (mercador na rota B, pró-Estado, −15/30/60; girondinos na rota C, 10/10/30). Os textos do caso do mercador foram escritos de modo coerente com esses valores. Trocar exigiria mudar a rota ou os valores das receitas.
- **Kornmann e o ouro:** o GDD diz que o caso de Sirven é o que "gera mais dinheiro"; nas receitas atuais, Kornmann (45) rende mais ouro que Sirven (35), embora o ouro seja o maior componente do caso Sirven.
- **Fase 2 com três casos:** o GDD fala em "dois casos" na Fase 2; o jogo mantém os três (o operário é a segunda perspectiva do motim), como já estava implementado e como pede esta tarefa.

## Linha editorial (Prompt 5, provisório)

A partir da Fase 2, depois de escolher as duas pistas, o jogador escolhe a linha editorial na prensa (o tutorial imprime direto). O modificador é somado **depois** da versão e do apoio e é o mesmo em todas as versões da receita, então não revela a verdade das pistas. Configurado em cada `ReceitaDeCaso` (bloco `linhaEditorial`) por **Ferramentas > Campanha > 4**.

| Fase (casos) | Defesa do povo | Agradar a Coroa / o Comitê | Sensacionalista |
|---|---|---|---|
| 2 (Colar da Rainha, Réveillon, Operário) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +15; perda da revelação +50% |
| 3 (Kornmann, Danton, Sirven) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +20; perda +50% |
| 4 (Desmoulins, Mercador, Girondinos) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar o Comitê") | 0 / 0 / +20; perda +50% |

- **Agravamento:** só no sensacionalista e só se a versão impressa tiver revelação (Com Boato, Calúnia ou Só Alegações). Só as perdas crescem, arredondadas para longe de zero. Ex.: Colar da Rainha Com Boato (pen −30/−5) sensacionalista → −45/−8. A versão Fatos nunca recebe penalidade.
- **Exemplo do Prompt 3:** Danton (`Caso_ChampDeMars`) Fatos + Folhas da petição = 40/−5/50 **antes** da linha; com Defesa do povo = 50/−10/50.
- **Rota:** Defesa e Agradar deslocam o desnível Povo × Estado em 15 pontos por publicação (margem da rota: 20).

## Exemplos de percurso até cada rota (só versões Fatos, sem revelações, sem apoio e sem linha editorial)

Início 50/50; tutorial (+15/−10) → 65/40. Com um caso só na Fase 3 (27/09), a rota é decidida por três publicações. Margem da rota: 20. Valores limitados a 0..100.

| Rota | Fase 2 | Fase 3 | Resultado |
|---|---|---|---|
| A | Colar da Rainha → 100/30 | Danton → 100/20 | +80 → **A** |
| B | Réveillon → 45/90 | Kornmann → 25/100 | −75 → **B** |
| C | Réveillon → 45/90 | Danton → 65/80 | −15 → **C** |

Com a linha editorial também dá para chegar à C por outros caminhos (ex.: Colar da Rainha com "Agradar a Coroa" → 100/40; Kornmann com "Agradar a Coroa" → 75/80 → −5 → **C**). A validação de balanceamento com decisões reais (boatos, revelações, despesas) fica para o Prompt 8.
