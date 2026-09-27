# Matriz de casos — documentação de autor

**Uso interno. Não exibir ao jogador.** A coluna "verdade" revela a confiabilidade real das pistas.

Gerada com o Prompt 2 em 25/09/2026; revisada no Prompt 3 e na verificação (25/09/2026); Caso das Joias atualizado em 27/09/2026 com o roteiro "O colar da rainha" (ver `STATUS_IMPLEMENTACAO_TCC.md` §12.3). Em 27/09/2026 a Fase 3 perdeu o caso do Padeiro (três casos, um concluído, STATUS §15) e as Fases 3 e 4 ganharam a dedução ativa, com as pistas reescritas em pares contraditórios (STATUS §13). Fonte dos dados: `Assets/Editor/CampanhaSetupTool.cs` (conteúdo; textos em vigor na tabela `TextosRevisados`), `Assets/Editor/BibliotecaSetupTool.cs`, `Assets/Editor/DeducaoSetupTool.cs` (pares) e `Ferramentas > Campanha > 2 - Validar campanha`. **Todo conteúdo das Fases 3 e 4, as pistas novas da Fase 2 e as alegações são PROVISÓRIOS**: escritos para a campanha ficar jogável na falta do GDD, sem valor canônico. Os casos das Fases 3/4 usam acontecimentos históricos como pano de fundo, com clientes e detalhes fictícios.

## Regras comuns

- **Custo:** toda interação (NPC ou objeto) custa **1h** na primeira vez no caso; repetir é grátis. Orçamento **4h**; **3h** com dívida. Com o inventário cheio, uma interação que entregaria algo não cobra.
- **Oportunidades:** cada caso tem **7** na sua cena: 4 NPCs + 3 objetos. Por caso: 2 fatos (F1 com o cliente, F2 num objeto), 1 boato (outro NPC), 1 calúnia (objeto) e 3 oportunidades sem nada útil (2 NPCs com fala "pouco útil" + 1 objeto vazio).
- **Caminho mínimo:** cliente + objeto de F2 = **2h** (cabe no orçamento com dívida). **Exceção (27/09):** no Caso das Joias o joalheiro entrega os dois fatos, então o caminho mínimo é **1h** e o caso tem 4 oportunidades vazias.
- **Dedução ativa (Fases 3 e 4, 27/09):** cada caso tem dois pares de afirmações contraditórias, e em cada par só uma é verdadeira: **par 1** = F1 do cliente × boato; **par 2** = F2 do objeto × calúnia. As alegações ficam fora. O quadro inteiro exige as quatro fontes: **4h** (endividado, 3h, não dá para completar). Para quem investiga com atenção, as falas levam de uma fonte à outra: o **cliente cita quem espalha o boato**, e **quem espalha o boato cita os dois objetos** (o do fato e o da calúnia). Conferir o quadro não custa nada e não é exigido para publicar.
- **Pré-requisitos:** nenhum na fala normal. Etapas complementares (Prompt 3) exigem uma evidência já obtida.
- **Nomes neutros:** o nome da pista é só o assunto. O que vem de pessoas é relato ("Segundo…" ou "X mostra…"), o que vem de objetos é documento, qualquer que seja a verdade. A confiabilidade se deduz pelo conteúdo, pela fonte e pela afirmação contrária. O validador recusa nomes com prefixos que denunciem a verdade ("Conversa sobre", "Libelo", "Folha:", "Cartaz:", "Denúncia:", "Bilhete", "Boato", "Calúnia", "Rumor").
- **Saída sem bloqueio (proposta de design, não literal dos PDFs):** ao aceitar o caso, o jogador recebe 2 **alegações do cliente** (fonte "Carta do cliente"). **Qualquer impressão com uma alegação** (e sem calúnia) usa a versão **Só Alegações** (ganho menor + consequência própria). Assim, "1 fato + 1 alegação" nunca rende mais que a investigação.
- **Versões de receita:**
  - Calúnia: qualquer calúnia, ou dois boatos.
  - Só Alegações: alguma alegação.
  - Fatos: fato + fato.
  - ComBoato: fato + boato.
  - Valores no formato Povo / Estado / Ouro. As penalidades (Povo / Estado) são cobradas quando o boato é exposto, no fim da fase (`GameManager.EncerrarFase`).
- **Arquivamento:** ao concluir um caso antes da Fase 4, alegações, sobras de pistas e documentos de apoio saem do inventário, mas ficam no histórico. No caso da Fase 4 só as alegações saem; o resto vai ao tribunal. Panfletos sempre ficam.

## Fase 2 — cena `Fase2` (existente, revisada; concluir 1 de 3)

NPCs: Joalheiro (x 12,9), Operário (29,2), Jean-Baptiste Réveillon (52,1), **Gazeteiro** (0, novo). Objetos novos: **Banca de Panfletos** (−12), **Muro de Cartazes** (40), **Mesa da Taverna** (68). Mudança: cada cliente entregava as 2 pistas; agora entrega só F1 (F2 foi para um objeto), não exige mais `casoObrigatorio` e não se desliga após a conversa. **Exceção (27/09, roteiro "O colar da rainha"):** o joalheiro voltou a entregar as duas pistas das Joias, ao fim da conversa; a Mesa da Taverna deixou de dar pista nesse caso. A Fase 2 não usa o quadro de dedução.

| Caso | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **Caso das Joias** (`Caso_Joalheiro`) | Joalheiro → Registro de compra (Fato) + Assinatura da encomenda (Fato) · Gazeteiro → O segredo da Rainha (Boato) · Muro → A Rainha e o Cardeal (Calúnia) | Réveillon, Operário, Banca, Mesa da Taverna | 50/−10/20 · 70/−20/35 (pen −30/−5) · 90/−30/50 (pen −50/−15) · 20/−5/8 (pen −5/0) |
| **Caso de Réveillon** (`Caso_Reveillon`) | Réveillon → O discurso na assembleia (Fato) · Banca → Os salários da manufatura (Fato) · Operário → A ordem de atirar (Boato) · Gazeteiro → O dinheiro inglês (Calúnia) | Joalheiro, Muro, Mesa | −20/50/50 · −25/60/70 (pen −10/−25) · −35/75/90 (pen −15/−45) · −8/20/20 (pen 0/−5) |
| **Caso do Operário** (`Caso_Operario`) | Operário → O relato do sobrevivente (Fato) · Muro → A ordem do comandante (Fato) · Réveillon → Os agitadores do duque (Boato) · Mesa → O fogo na fábrica (Calúnia) | Joalheiro, Gazeteiro, Banca | 60/−20/10 · 75/−25/25 (pen −30/−5) · 90/−35/40 (pen −50/−15) · 25/−8/5 (pen −5/0) |

Os valores Fatos/ComBoato/Calúnia da Fase 2 são os que já existiam. Foram acrescentados os textos de revelação, a versão Só Alegações e o nome de exibição dos 3 panfletos. As **estimativas da mesa** (anteriores ao Prompt 2) contradiziam as receitas e agora refletem a versão Fatos:

| Caso | Ouro / Povo / Estado |
|---|---|
| Joias | 20 / +50 / −10 |
| Réveillon | 50 / −20 / +50 |
| Operário | 10 / +60 / −20 |

## Fase 3 — cena `Fase3` (1789–1792, concluir 1 de 3)

NPCs: Viúva François (−8), Cocheiro Joubert (14), Peticionária Lacombe (34), Gravador Morel (58). Objetos: Balcão da Padaria (−16), Mural dos Cordeliers (24), Caixa de Tipos (70). Cenário com tom de entardecer. Escolhido um caso, os outros dois ficam bloqueados na mesa; concluído, a volta ao escritório abre a Fase 4. "O Padeiro de Notre-Dame" saiu em 27/09 (a Viúva François ficou como moradora da rua, sem caso próprio).

| Caso | Par 1: cliente × boato | Par 2: objeto × calúnia | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|---|
| **A Carruagem de Varennes** (`Caso_Varennes`) | Cocheiro → A viagem da baronesa (Fato) × Peticionária → O ouro austríaco (Boato) | Mural → A parada em Sainte-Menehould (Fato) × Caixa → Os cavalos da posta (Calúnia) | Viúva, Gravador, Balcão | −20/30/45 · −30/40/60 (pen −10/−10) · −40/50/75 (pen −20/−15) · −8/12/18 (pen 0/−5) |
| **O Fuzilamento do Champ de Mars** (`Caso_ChampDeMars`) | Peticionária → A petição do altar da pátria (Fato) × Viúva → O primeiro tiro (Boato) | Balcão → A bandeira vermelha (Fato) × Mural → A hora da bandeira (Calúnia) | Cocheiro, Gravador, Caixa | 20/−10/30 · 30/−15/40 (pen −10/−5) · 40/−20/55 (pen −20/−10) · 8/−4/10 (pen −5/0) (valores do exemplo do Prompt 3) |
| **Os Assignats Falsos** (`Caso_Assignats`) | Gravador → As chapas roubadas (Fato) × Cocheiro → A história do roubo (Boato) | Caixa → O defeito dos tipos (Fato) × Balcão → A prensa de Morel (Calúnia) | Viúva, Peticionária, Mural | 15/10/35 · 20/15/50 (pen −10/−10) · 25/20/65 (pen −15/−15) · 5/4/14 (pen −5/0) |

O que cada par contradiz e as pistas que levam de uma fonte à outra:

| Caso | Par 1 | Par 2 | Pistas nas falas |
|---|---|---|---|
| Varennes | contrato de 60 libras em moeda francesa, sem dizer quem iria × ouro austríaco e "sabia desde Paris" quem levava | Drouet reconheceu o Rei sozinho e galopou num cavalo descansado da posta × o cocheiro envenenou todos os cavalos da posta | Cocheiro → "a peticionária da praça"; Peticionária → ata no mural e prova de impressão na caixa de tipos |
| Champ de Mars | quem assinava estava desarmado, nenhum tiro partiu do altar × vieram armados e atiraram primeiro | bandeira vermelha hasteada por ordem do prefeito antes de a Guarda marchar × a bandeira só subiu depois dos tiros | Peticionária → "a viúva do padeiro", no mercado; Viúva → proclamação no balcão e cartaz no mural |
| Assignats | queixa do roubo registrada dias antes da prisão × o roubo foi inventado depois de preso | tipos das falsas com defeito que não existe nos de Morel (outra prensa) × as falsas saíram da prensa de Morel | Gravador → "o Joubert"; Cocheiro → papel no balcão e caixa de tipos |

## Fase 4 — cena `Fase4` (1793, um caso por rota)

NPCs: Redator Marchand (−6), Comissário Vautrin (18), Cidadã Delorme (40), Carcereiro da Conciergerie (62). Objetos: Arquivo da Seção (−15), Parede de Editais (30), Caixa de Denúncias (74). Cenário com tom frio. A mesa mostra só o caso da rota travada (um caso sem rota nunca aparece nessa fase); depois de publicado, a ida ao tribunal é pelo Dupaty, no escritório.

| Caso (rota) | Par 1: cliente × boato | Par 2: objeto × calúnia | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|---|
| **O Redator do Velho Sans-culotte** (`Caso_Jornalista`, A) | Marchand → Os números do jornal (Fato) × Carcereiro → O número de outubro (Boato) | Arquivo → O certificado da seção (Fato) × Caixa → O certificado recusado (Calúnia) | Vautrin, Delorme, Parede | 20/−20/15 · 30/−30/25 (pen −10/−5) · 40/−40/35 (pen −20/−10) · 8/−8/6 (pen −5/0) |
| **O Negociante de Grãos** (`Caso_Negociante`, B) | Vautrin → Os armazéns de Garnier (Fato) × Carcereiro → O trigo apodrecido (Boato) | Parede → Os preços do Máximo (Fato) × Arquivo → As vendas de Garnier (Calúnia) | Marchand, Delorme, Caixa | −15/30/60 · −25/40/80 (pen −5/−10) · −35/50/100 (pen −10/−20) · −6/12/24 (pen 0/−5) |
| **A Viúva Girondina** (`Caso_Girondina`, C) | Delorme → As cartas do deputado (Fato) × Marchand → O plano de fuga (Boato) | Caixa → A denúncia retirada (Fato) × Parede → A acusação do vizinho (Calúnia) | Vautrin, Carcereiro, Arquivo | 10/10/30 · 15/15/45 (pen −10/−10) · 20/20/60 (pen −15/−15) · 4/4/12 (pen −5/−5) |

| Caso | Par 1 | Par 2 | Pistas nas falas |
|---|---|---|---|
| Jornalista | nenhum número convoca à revolta nem à volta do Rei × pediu a volta do Rei num número de outubro | certificado de civismo três meses antes da prisão × a seção recusou o certificado (armas no porão) | Marchand → o carcereiro; Carcereiro → arquivo da seção e caixa de denúncias |
| Negociante | trigo em bom estado, o dobro do declarado × armazéns cheios de trigo podre | vendeu à padaria da seção acima do Máximo × não vende a ninguém, guarda tudo para os ingleses | Vautrin → o carcereiro; Carcereiro → parede de editais e arquivo da seção |
| Girondina | cartas sem fuga, plano ou nome de deputado × cartas com o plano de fuga e os nomes de quem os esconde | o vizinho retirou a acusação por escrito × o vizinho mantém a acusação e ela tentou envenená-lo | Delorme → o redator Marchand; Marchand → caixa de denúncias e parede de editais |

## Biblioteca e documentos de apoio (Prompt 3, provisório)

Documentos de apoio: confiabilidade `NaoEPista`, qualidade **Superior**, não vão nos dois slots, não são gastos e não entram no quadro de dedução. Cada receita das Fases 3/4 tem **um** qualificador (`apoio_<caso>`), que soma os valores abaixo à versão impressa (qualquer versão), uma vez por publicação. A biblioteca não vende um documento se o jogador já tiver outro que ativa o mesmo reforço ("apoio equivalente"). Desde 27/09 cada documento também dá **contexto** para julgar um dos pares, sem dizer qual pista é verdadeira.

| Caso | Oferta (preço) | Contexto | Outra fonte do apoio | Reforço Povo / Estado / Ouro |
|---|---|---|---|---|
| Varennes | Relatório da Assembleia sobre a fuga (25) | par 2: Drouet, montado num cavalo da posta, deu o alarme em Varennes | **Cocheiro Joubert**, etapa `recibo_da_estalagem`, depois de obter "A parada em Sainte-Menehould" → Recibo da estalagem | 0 / +10 / +20 |
| Champ de Mars | Ata da prefeitura de Paris (20) | par 2: lei marcial votada e bandeira hasteada à tarde, antes de a Guarda sair | — | **+20 / +5 / +20** → Fatos passa de 20/−10/30 para **40/−5/50** (exemplo do Prompt 3) |
| Assignats | Laudo da Casa da Moeda (20) | par 2: as falsas foram impressas numa prensa diferente das dos gravadores da seção | — | +5 / +10 / +15 |
| Jornalista (A) | Coleção encadernada do jornal (25) | par 1: números de março a outubro; o de outubro trata da carestia do pão | — | +10 / 0 / +10 |
| Negociante (B) | Livros do Comitê de Subsistência (30) | par 2: registram as vendas de Garnier à padaria da seção | — | 0 / +10 / +25 |
| Girondina (C) | Correspondência arquivada na Convenção (25) | par 1: cópias das cartas, nenhuma palavra sobre fuga | **Cidadã Delorme**, etapa `carta_da_secao`, depois de obter "A denúncia retirada" → Carta de recomendação da seção | +5 / +5 / +15 |

## Linha editorial (Prompt 5, provisório)

A partir da Fase 2, depois de escolher as duas pistas, o jogador escolhe a linha editorial na prensa (o tutorial imprime direto). O modificador é somado **depois** da versão e do apoio e é o mesmo em todas as versões da receita, então não revela a verdade das pistas. Configurado em cada `ReceitaDeCaso` (bloco `linhaEditorial`) por **Ferramentas > Campanha > 4**.

| Fase (casos) | Defesa do povo | Agradar a Coroa / o Comitê | Sensacionalista |
|---|---|---|---|
| 2 (Joias, Réveillon, Operário) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +15; perda da revelação +50% |
| 3 (Varennes, Champ de Mars, Assignats) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +20; perda +50% |
| 4 (Jornalista, Negociante, Girondina) | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar o Comitê") | 0 / 0 / +20; perda +50% |

- **Agravamento:** só no sensacionalista e só se a versão impressa tiver revelação (Com Boato, Calúnia ou Só Alegações). Só as perdas crescem, arredondadas para longe de zero. Ex.: Joias Com Boato (pen −30/−5) sensacionalista → −45/−8. A versão Fatos nunca recebe penalidade.
- **Exemplo do Prompt 3:** Champ de Mars Fatos + Ata = 40/−5/50 **antes** da linha; com Defesa do povo = 50/−10/50.
- **Rota:** Defesa e Agradar deslocam o desnível Povo × Estado em 15 pontos por publicação (margem da rota: 20).

## Exemplos de percurso até cada rota (só versões Fatos, sem revelações, sem apoio e sem linha editorial)

Início 50/50; tutorial (+15/−10) → 65/40. Com um caso só na Fase 3 (27/09), a rota é decidida por três publicações. Margem da rota: 20. Valores limitados a 0..100.

| Rota | Fase 2 | Fase 3 | Resultado |
|---|---|---|---|
| A | Joias → 100/30 | Champ de Mars → 100/20 | +80 → **A** |
| B | Réveillon → 45/90 | Varennes → 25/100 | −75 → **B** |
| C | Réveillon → 45/90 | Champ de Mars → 65/80 | −15 → **C** |

Com a linha editorial também dá para chegar à C por outros caminhos (ex.: Joias com "Agradar a Coroa" → 100/40; Varennes com "Agradar a Coroa" → 75/80 → −5 → **C**). A validação de balanceamento com decisões reais (boatos, revelações, despesas) fica para o Prompt 8.
