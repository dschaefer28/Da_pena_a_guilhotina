# Matriz de casos — documentação de autor

**Uso interno. Não exibir ao jogador.** A coluna "verdade" revela a confiabilidade real das pistas.

Gerada com o Prompt 2 em 25/09/2026; revisada no Prompt 3 e na verificação (25/09/2026). Fonte dos dados: `Assets/Editor/CampanhaSetupTool.cs` (conteúdo; textos em vigor na tabela `TextosRevisados`), `Assets/Editor/BibliotecaSetupTool.cs` e `Ferramentas > Campanha > 2 - Validar campanha`. **Todo conteúdo das Fases 3 e 4, as pistas novas da Fase 2 e as alegações são PROVISÓRIOS**: escritos para a campanha ficar jogável na falta do GDD, sem valor canônico. Os casos das Fases 3/4 usam acontecimentos históricos como pano de fundo, com clientes e detalhes fictícios.

## Regras comuns

- **Custo:** toda interação (NPC ou objeto) custa **1h** na primeira vez no caso; repetir é grátis. Orçamento **4h**; **3h** com dívida. Com o inventário cheio, uma interação que entregaria algo não cobra.
- **Oportunidades:** cada caso tem **7** na sua cena: 4 NPCs + 3 objetos. Por caso: 2 fatos (F1 com o cliente, F2 num objeto), 1 boato (outro NPC), 1 calúnia (objeto) e 3 oportunidades sem nada útil (2 NPCs com fala "pouco útil" + 1 objeto vazio).
- **Caminho mínimo:** cliente + objeto de F2 = **2h** (cabe no orçamento com dívida).
- **Pré-requisitos:** nenhum na fala normal. Etapas complementares (Prompt 3) exigem uma evidência já obtida.
- **Nomes neutros:** o nome da pista é só o assunto. O que vem de pessoas é relato ("Segundo…" ou "X mostra…"), o que vem de objetos é documento, qualquer que seja a verdade. A confiabilidade se deduz pelo conteúdo e pela fonte. O validador recusa nomes com prefixos que denunciem a verdade ("Conversa sobre", "Libelo", "Folha:", "Cartaz:", "Denúncia:", "Bilhete", "Boato", "Calúnia", "Rumor").
- **Saída sem bloqueio (proposta de design, não literal dos PDFs):** ao aceitar o caso, o jogador recebe 2 **alegações do cliente** (fonte "Carta do cliente"). **Qualquer impressão com uma alegação** (e sem calúnia) usa a versão **Só Alegações** (ganho menor + consequência própria). Assim, "1 fato + 1 alegação" nunca rende mais que a investigação.
- **Versões de receita:**
  - Calúnia: qualquer calúnia, ou dois boatos.
  - Só Alegações: alguma alegação.
  - Fatos: fato + fato.
  - ComBoato: fato + boato.
  - Valores no formato Povo / Estado / Ouro. As penalidades (Povo / Estado) só são cobradas quando a revelação for aplicada (Prompt 6).
- **Arquivamento:** ao concluir um caso antes da Fase 4, alegações, sobras de pistas e documentos de apoio saem do inventário, mas ficam no histórico. No caso da Fase 4 só as alegações saem; o resto vai ao tribunal. Panfletos sempre ficam.

## Fase 2 — cena `Fase2` (existente, revisada)

NPCs: Joalheiro (x 12,9), Operário (29,2), Jean-Baptiste Réveillon (52,1), **Gazeteiro** (0, novo). Objetos novos: **Banca de Panfletos** (−12), **Muro de Cartazes** (40), **Mesa da Taverna** (68). Mudança: cada cliente entregava as 2 pistas; agora entrega só F1 (F2 foi para um objeto), não exige mais `casoObrigatorio` e não se desliga após a conversa.

| Caso | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **Caso das Joias** (`Caso_Joalheiro`) | Joalheiro → A venda do colar (Fato) · Mesa da Taverna → A assinatura da encomenda (Fato) · Gazeteiro → O segredo da Rainha (Boato) · Muro → A Rainha e o Cardeal (Calúnia) | Réveillon, Operário, Banca | 50/−10/20 · 70/−20/35 (pen −30/−5) · 90/−30/50 (pen −50/−15) · 20/−5/8 (pen −5/0) |
| **Caso de Réveillon** (`Caso_Reveillon`) | Réveillon → O discurso na assembleia (Fato) · Banca → Os salários da manufatura (Fato) · Operário → A ordem de atirar (Boato) · Gazeteiro → O dinheiro inglês (Calúnia) | Joalheiro, Muro, Mesa | −20/50/50 · −25/60/70 (pen −10/−25) · −35/75/90 (pen −15/−45) · −8/20/20 (pen 0/−5) |
| **Caso do Operário** (`Caso_Operario`) | Operário → O relato do sobrevivente (Fato) · Muro → A ordem do comandante (Fato) · Réveillon → Os agitadores do duque (Boato) · Mesa → O fogo na fábrica (Calúnia) | Joalheiro, Gazeteiro, Banca | 60/−20/10 · 75/−25/25 (pen −30/−5) · 90/−35/40 (pen −50/−15) · 25/−8/5 (pen −5/0) |

Os valores Fatos/ComBoato/Calúnia da Fase 2 são os que já existiam. Foram acrescentados os textos de revelação, a versão Só Alegações e o nome de exibição dos 3 panfletos. As **estimativas da mesa** (anteriores ao Prompt 2) contradiziam as receitas e agora refletem a versão Fatos:

| Caso | Ouro / Povo / Estado |
|---|---|
| Joias | 20 / +50 / −10 |
| Réveillon | 50 / −20 / +50 |
| Operário | 10 / +60 / −20 |

## Fase 3 — cena `Fase3` (1789–1792, concluir 2 de 4)

NPCs novos: Viúva François (−8), Cocheiro Joubert (14), Peticionária Lacombe (34), Gravador Morel (58). Objetos: Balcão da Padaria (−16), Mural dos Cordeliers (24), Caixa de Tipos (70). Cenário com tom de entardecer.

| Caso | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **O Padeiro de Notre-Dame** (`Caso_Padeiro`) | Viúva → As fornadas do dia (Fato) · Balcão → O pão dos deputados (Fato) · Cocheiro → A farinha no porão (Boato) · Mural → O pão envenenado (Calúnia) | Peticionária, Gravador, Caixa | −15/25/30 · −25/35/45 (pen −10/−5) · −35/45/60 (pen −15/−15) · −5/10/12 (pen −5/0) |
| **A Carruagem de Varennes** (`Caso_Varennes`) | Cocheiro → A viagem da baronesa (Fato) · Mural → A parada em Sainte-Menehould (Fato) · Peticionária → O ouro austríaco (Boato) · Caixa → Os cavalos da Guarda (Calúnia) | Viúva, Gravador, Balcão | −20/30/45 · −30/40/60 (pen −10/−10) · −40/50/75 (pen −20/−15) · −8/12/18 (pen 0/−5) |
| **O Fuzilamento do Champ de Mars** (`Caso_ChampDeMars`) | Peticionária → A petição do altar da pátria (Fato) · Balcão → A bandeira vermelha (Fato) · Viúva → O primeiro tiro (Boato) · Mural → Os peticionários pagos (Calúnia) | Cocheiro, Gravador, Caixa | 20/−10/30 · 30/−15/40 (pen −10/−5) · 40/−20/55 (pen −20/−10) · 8/−4/10 (pen −5/0) (valores do exemplo do Prompt 3) |
| **Os Assignats Falsos** (`Caso_Assignats`) | Gravador → As chapas roubadas (Fato) · Caixa → O defeito dos tipos (Fato) · Cocheiro → As notas nas tavernas (Boato) · Balcão → Os emigrados de Coblença (Calúnia) | Viúva, Peticionária, Mural | 15/10/35 · 20/15/50 (pen −10/−10) · 25/20/65 (pen −15/−15) · 5/4/14 (pen −5/0) |

## Fase 4 — cena `Fase4` (1793, um caso por rota)

NPCs novos: Redator Marchand (−6), Comissário Vautrin (18), Cidadã Delorme (40), Carcereiro da Conciergerie (62). Objetos: Arquivo da Seção (−15), Parede de Editais (30), Caixa de Denúncias (74). Cenário com tom frio. A mesa mostra só o caso da rota travada; depois de publicado, a ida ao tribunal é pelo Dupaty, no escritório.

| Caso (rota) | Fonte → item (verdade) | Vazias | Receita Fatos / ComBoato / Calúnia / Só Alegações |
|---|---|---|---|
| **O Redator do Velho Sans-culotte** (`Caso_Jornalista`, A) | Marchand → Os números do jornal (Fato) · Arquivo → O certificado da seção (Fato) · Carcereiro → As cartas com selo inglês (Boato) · Caixa → As armas da redação (Calúnia) | Vautrin, Delorme, Parede | 20/−20/15 · 30/−30/25 (pen −10/−5) · 40/−40/35 (pen −20/−10) · 8/−8/6 (pen −5/0) |
| **O Negociante de Grãos** (`Caso_Negociante`, B) | Vautrin → Os armazéns de Garnier (Fato) · Parede → Os preços do Máximo (Fato) · Carcereiro → O trigo apodrecido (Boato) · Arquivo → O dinheiro de Pitt (Calúnia) | Marchand, Delorme, Caixa | −15/30/60 · −25/40/80 (pen −5/−10) · −35/50/100 (pen −10/−20) · −6/12/24 (pen 0/−5) |
| **A Viúva Girondina** (`Caso_Girondina`, C) | Delorme → As cartas do deputado (Fato) · Caixa → A denúncia retirada (Fato) · Marchand → Os deputados foragidos (Boato) · Parede → O patriota envenenado (Calúnia) | Vautrin, Carcereiro, Arquivo | 10/10/30 · 15/15/45 (pen −10/−10) · 20/20/60 (pen −15/−15) · 4/4/12 (pen −5/−5) |

## Biblioteca e documentos de apoio (Prompt 3, provisório)

Documentos de apoio: confiabilidade `NaoEPista`, qualidade **Superior**, não vão nos dois slots e não são gastos. Cada receita das Fases 3/4 tem **um** qualificador (`apoio_<caso>`), que soma os valores abaixo à versão impressa (qualquer versão), uma vez por publicação. A biblioteca não vende um documento se o jogador já tiver outro que ativa o mesmo reforço ("apoio equivalente").

| Caso | Oferta (preço) | Outra fonte do apoio | Reforço Povo / Estado / Ouro |
|---|---|---|---|
| Padeiro | Registro da Halle aux Blés (20) | — | +5 / +10 / +15 |
| Varennes | Relatório da Assembleia sobre a fuga (25) | **Cocheiro Joubert**, etapa `recibo_da_estalagem`, depois de obter "A parada em Sainte-Menehould" → Recibo da estalagem | 0 / +10 / +20 |
| Champ de Mars | Ata da prefeitura de Paris (20) | — | **+20 / +5 / +20** → Fatos passa de 20/−10/30 para **40/−5/50** (exemplo do Prompt 3) |
| Assignats | Laudo da Casa da Moeda (20) | — | +5 / +10 / +15 |
| Jornalista (A) | Coleção encadernada do jornal (25) | — | +10 / 0 / +10 |
| Negociante (B) | Livros do Comitê de Subsistência (30) | — | 0 / +10 / +25 |
| Girondina (C) | Correspondência arquivada na Convenção (25) | **Cidadã Delorme**, etapa `carta_da_secao`, depois de obter "A denúncia retirada" → Carta de recomendação da seção | +5 / +5 / +15 |

## Exemplos de percurso até cada rota (só versões Fatos, sem revelações nem apoio)

Início 50/50; tutorial (+15/−10) → 65/40. Margem da rota: 20. Valores limitados a 0..100.

| Rota | Fase 2 | Fase 3 | Resultado |
|---|---|---|---|
| A | Joias → 100/30 | Champ de Mars → 100/20; Assignats → 100/30 | +70 → **A** |
| B | Réveillon → 45/90 | Varennes → 25/100; Padeiro → 10/100 | −90 → **B** |
| C | Operário → 100/20 | Varennes → 80/50; Padeiro → 65/75 | −10 → **C** |

A validação de balanceamento com decisões reais (boatos, revelações, despesas) fica para os Prompts 6 e 8. Percurso real observado na verificação (save do jogador, Joias com boato): Varennes só com alegação e apoio, depois Padeiro com fatos → Povo 77 × Estado 67 → **C**.
