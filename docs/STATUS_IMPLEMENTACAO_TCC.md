# Status de implementação — Da Pena à Guilhotina

Atualizado em 26/09/2026. Referência: `docs/PROMPTS_CLAUDE_CODE_TCC.md`. Caminhos relativos a `Unity-DaPenaAGuilhotina/`.

Estados usados: **validado** (testado em runtime ou em teste automatizado), **existente não validado** (código/asset presente, sem teste de runtime), **parcial**, **ausente**.

Etapas: Prompts 0 a 3 concluídos; verificação dos Prompts 0–3 com os achados corrigidos (§7); **verificação completa contra os dois PDFs em 26/09, com correções que adiantaram partes dos Prompts 6 e 7 (§9)**; **Prompt 5 (linha editorial) concluído em 26/09 (§10)**. Próximo recomendado: **o que resta dos Prompts 6 e 7**, depois o Prompt 8. O **Prompt 4 é opcional** (decisão do grupo, ver §8).

Regras que mudaram nas verificações e prevalecem sobre o texto das seções 4–6:
- **Alegações:** qualquer impressão com alegação usa a versão Só Alegações.
- **Arquivamento:** sobras dos casos anteriores à Fase 4 são arquivadas ao concluir.
- **Inventário cheio:** interação sem espaço para o que entregaria não cobra horas.
- **Save:** passou para a versão 6 (linha editorial, Prompt 5).
- **Biblioteca:** tem estados por situação.
- **Pistas:** os nomes são neutros.
- **Revelações (26/09):** aplicadas no fim da fase por `GameManager.EncerrarFase`, na ordem revelações → despesas → avanço de fase/rota, e mostradas na cutscene do `FimDeFase`. `RevelacaoDeBoatos` foi removido.
- **Tribunal (26/09):** as provas são os itens do caso da Fase 4 obtidos (histórico, inclusive os gastos na prensa) mais o panfleto dele. O réu é absolvido só se esse panfleto saiu na versão Fatos; o destino do jogador continua vindo só da rota.
- **Final C (26/09):** "O Esquecido", definido pelo grupo: barras equilibradas, ninguém condena nem defende o jogador, que é apagado da história.
- **Tutorial (26/09):** a explicação do status aparece ao abrir a prensa, antes de imprimir.
- **Linha editorial (Prompt 5):** a partir da Fase 2, o Misturar pede Defesa do povo / Agradar a Coroa (o Comitê na Fase 4) / Sensacionalista antes de consumir as pistas. Cálculo: versão → apoio → linha. O exemplo do Prompt 3 (40/−5/50) é o valor antes da linha.

Documento de autor com a matriz dos casos: `docs/MATRIZ_DE_CASOS_TCC.md` (não exibir ao jogador).

---

## 1. Ambiente

| Item | Situação |
|---|---|
| Unity | 6000.3.9f1 aberto no Editor durante o trabalho (projeto `Unity-DaPenaAGuilhotina`) |
| Unity MCP | Operacional (`com.coplaydev.unity-mcp`): leitura de cenas, compilação, Play Mode, execução de código de Editor e Test Runner |
| Testes | `com.unity.test-framework` 1.6.0. Não havia testes. Os testes novos ficam em `Assets/Editor/Testes/` (assembly `Assembly-CSharp-Editor`, sem asmdef, porque os scripts do jogo não têm asmdef). Em 26/09: 46 testes EditMode; 57 depois do Prompt 5 |
| Compilação | Sem erros. Avisos antigos: `FindObjectOfType` obsoleto em `GameManager`; `Caso_Tutorial` sem `caseTitle`/`npcDialogueRoute` (OnValidate) |
| Save real do jogador | Até 25/09: não foi lido nem alterado. Em 26/09: ver §9.7 (um save de teste criado e apagado com autorização; duas PlayerPrefs de dica ficaram marcadas) |

## 2. Premissas adotadas (seção "Decisões adotadas" do documento de prompts)

1. Finais: A = Guilhotina, B = Tirano, C = Equilíbrio (código atual já segue).
2. Tribunal: investigar → panfleto → Dupaty → tribunal.
3. "Final da Fase 2" citado na Fase 3 = erro de numeração.
4. ~~Final C "O Exílio"~~ Substituída em 26/09: Final C "O Esquecido", definido pelo grupo (ver §9.3). Textos ainda provisórios.
5. Dedução ativa (C) nas Fases 3 e 4, por caso; sem confronto com NPC na 1ª versão.
6. Linha editorial (D) a partir da Fase 2; tutorial com receita simples; tom não muda a verdade.
7. Receita expandida mantém 2 slots; evidências complementares como qualificadores opcionais.
8. Campanha: Fase 2 = 1 de 3 casos; Fase 3 = 2 distintos de 4; Fase 4 = 1 caso da rota.
9. Sem bloqueio por falta de pistas (duas alegações não verificadas como saída de design, se necessário).
10. Valores e textos novos são protótipos editáveis, rotulados como provisórios.
11. (26/09, decisão do grupo) Pista melhor gera panfleto melhor; as barras decidem o final do jogador; o panfleto do caso da Fase 4 decide o destino do réu.

Dependências entre etapas: 1 (estabilidade/save) → 2 (conteúdo) → 3 (biblioteca) → 4 (dedução) → 5 (tom) → 6 (consequências/tribunal) → 7 (UI/tutorial) → 8 (validação). Ordem recomendada a partir de 26/09: 5 → restante de 6 e 7 → 8; o 4 só se o grupo decidir incluí-lo (as propostas C e D são "proposta, não implementada" no documento de dificuldade, e o 4 exige reescrever as pistas de 7 casos em pares contraditórios).

## 3. Diagnóstico (Prompt 0)

### 3.1 Mapa de requisitos

Estado na data do diagnóstico (25/09), com anotações dos Prompts 1–3. O estado depois de 26/09 está em §9.

| Requisito | Evidência (arquivo / campo) | Estado |
|---|---|---|
| Tutorial (Dupaty → Marie → Dupaty → alçapão → prensa → panfleto → escritório → mesa) | `TutorialManager` (15 etapas na cena Jogo), `GerenciadorCena1`, `Fase1Desfecho`, `IntroCutscene`, `TrapdoorInteractable.requisitosDeCaso` (Caso_Tutorial exige as 2 evidências) | existente não validado de ponta a ponta; receita da prensa **validada** (ver 4.4) |
| Mesa de casos por fase/rota | `CaseSelectionUI.availableCases` (UI.prefab) com 10 casos; filtro `CaseData.fase`/`rota`; estados disponível / em andamento / concluído | **validado** após Prompt 2 (antes: só Fase 2) |
| Progressão `{1,1,2,1}` e fim de fase | `GameManager.casosPorFase` (padrão do código; não serializado), `FimDeFase` no UI.prefab (cena Jogo) | existente não validado |
| Tempo de investigação (proposta A) | `RelogioDeInvestigacao`, `HudDoRelogio`, `NPCMovement.custoEmHoras`/`LootInteractable.custoEmHoras` (padrão 1h), `horasPorCaso` 4 | **validado** após Prompt 1 |
| Despesas e dívida (proposta B) | `despesasPorFase {0,25,50,0}`, `horasPerdidasPorDivida` 1, cobrança em `FimDeFase.Start` | parcial: perda de 1h validada em teste; ordem com revelações pendente (Prompt 6) |
| Fato x boato | `Item.confiabilidade`, `ReceitaDeCaso` (10 assets), `CraftingPress.TentarPanfletoDeCaso` | parcial: após o Prompt 2 cada caso tem 2 fatos, 1 boato, 1 calúnia e textos de revelação; nenhum `verificadaPor` (a verificação automática será substituída pela dedução no Prompt 4) |
| Revelação de boatos | `RevelacaoDeBoatos` | **ausente em cena**: o componente não está em nenhuma cena/prefab; `revelacoesPendentes` nunca é consumido |
| Biblioteca e qualidade | `BibliotecaUI`, `OfertaDaBiblioteca` (7), `Item.qualidade`/`documentoDeSuporte`, `ReceitaDeCaso.qualificadores`, `CalculadoraDePanfleto`, `SuportesDaPrensaUI` | **validado** após Prompt 3 (testes + Play Mode); visual não conferido em tela |
| Dedução ativa (C) | verificação automática em `InventoryManager.PistaVerificada`/`RegistrarVerificacoes` | ausente |
| Linha editorial (D) | — | ausente (implementada no Prompt 5, ver §10) |
| Tribunal e rota | `CheckpointDoTribunal` (filho do Dupaty, cena Jogo), `TribunalManager` (cena Tribunal), `GameManager.CalcularRota` (margem 20) | existente não validado |
| Save | `SistemaDeSave` (v2 no diagnóstico; v3 no Prompt 1; v4 no Prompt 3), `CatalogoDeSave` (no diagnóstico: 12 itens e 4 casos; hoje inclui o conteúdo das Fases 2–4) | **validado** (testes + Continuar com save real v3, ver §7) |
| Conteúdo | 11 `CaseData`: tutorial, 3 da Fase 2, 4 da Fase 3, 3 da Fase 4 (um por rota) | **validado** (validador + Play Mode); conteúdo novo **provisório** |
| Cenas | Build: menu principal, Jogo, Fase2, Porao, Fase3, Fase4, Tribunal (todas habilitadas) | Fase3/Fase4 agora com 4 NPCs e 3 objetos próprios cada; arte ainda é placeholder |
| Loot em cena | `LootInteractable` | 9 objetos (3 por cena de investigação) |

Não surgiram implementações novas de biblioteca, dedução ou tom depois da análise do documento de prompts.

### 3.2 Pontos verificados

1. **Casos por fase/rota e catálogo.** Mesa e catálogo referenciam os 4 casos existentes; nenhum caso de fase 3/4 nem com `rota`. Consequência: ao concluir a Fase 2, `FimDeFase` avança para a Fase 3 e a mesa responde "Não há novos casos" — **a campanha para na Fase 3** até o Prompt 2. `Caso_Tutorial` está sem título/descrição.
2. **NPC/loot e duplicação de recompensas (antes do Prompt 1).** NPCs da Fase 2 davam as 2 pistas por diálogo, filtrando por "está no inventário agora"; `canInteract` não persistia. Revisitar a Fase 2 depois de imprimir devolvia as pistas e permitia **reimprimir o caso já concluído** (a prensa não tinha guarda). `NPCMovement` desligava o NPC ao entregar recompensa mesmo com `disableAfterDialogue` falso. Corrigido no Prompt 1.
3. **Ordem de despesas, revelações, fase e rota.** `FimDeFase.Start` (cena Jogo): cobra despesas → `AvancarFase` → ao entrar na Fase 4 trava a rota. Como `RevelacaoDeBoatos` não está em cena, penalidades de boato nunca são aplicadas e, se fosse colocado na cena seguinte, a rota já estaria travada antes delas. Tratar no Prompt 6.
4. **Inventário/prensa/tribunal.** A prensa consome as pistas; o panfleto na saída é salvo por `InventoryManager.SalvarEstadoAtual` (inclui entradas/saída da prensa e item na mão) e volta para a grade na cena seguinte. Se a grade estiver cheia, `RestaurarInventario` descarta o excedente **sem aviso** (Prompt 6). `TribunalManager.ColetarProvas` usa todo o `inventarioSalvo` (inclusive itens de outros casos) e sorteia variação da barra (`Random.Range`); "Encerrar defesa" é permitido após 1 prova.
5. **Tutorial, cenas e prefabs.** Todas as cenas jogáveis têm instância do `GameManager.prefab` (padrão `casoEscolhido = Caso_Tutorial`) e do `UI.prefab` (mesa, prensa, inventário, `FimDeFase`, `HudDoRelogio`). Marie some por panfleto no inventário ou caso ≠ tutorial (`GerenciadorCena1`; revisar no Prompt 7). Referências quebradas sem impacto aparente: `UI_Inventory.prefab` `recipes[0]` e itens de slots (sobrescritos no UI.prefab), `CameraConfiner` com prefab ausente na Porao, sprite ausente no `Marie Bradier.prefab` (sobrescrito na cena), `TypeTextAnimation.dialogueSystem` no UI.prefab. `TrapdoorInteractable` usa `Operario_Dialogo`/`resposta_operario` como "pensamentos" — provável referência errada.
6. **Execução.** Editor, MCP, compilação, Play Mode e Test Runner disponíveis e usados.

Outros achados: as 3 `Recipe` de caso repetem os valores `soFatos` das `ReceitaDeCaso` (sistema duplicado, só usado como fallback); os 9 itens de caso não têm `itemName`, `descricao`, `fonte` nem sprite; só há salvamento em pontos fixos (fim do tutorial, fim de fase, ida ao tribunal) — não há save no meio de um caso.

---

## 4. Prompt 1 — estabilidade da investigação, publicação e save

### 4.1 Requisitos atendidos

| # | Requisito | Implementação | Estado |
|---|---|---|---|
| 1 | ID estável e único por interação; estado por caso + interação | Campo serializado `idDaInteracao` em `NPCMovement` e `LootInteractable`. Chave = `caso|cena/id` (`RegistroDaInvestigacao`, `IdDeInteracao`). Vazio → caminho na hierarquia (mesmo valor que a ferramenta grava), com aviso. Validação de vazios/repetidos a cada cena carregada (`GameManager.OnSceneLoaded`) e na ferramenta de Editor | validado (teste + ferramenta executada) |
| 2 | 1ª interação cobra (inclusive busca vazia); repetição no mesmo caso não cobra; caso diferente tem estado próprio; sair/voltar e save não recarregam horas | `RelogioDeInvestigacao.TentarGastar(alvo, id, custo)` usa o registro persistente; o registro não é mais zerado ao aceitar caso | validado (Play Mode + testes) |
| 3 | NPC acessível para novos estágios; recompensa registrada por caso/interação/etapa/item; falha de `AddItem` não registra; inventário cheio permite receber depois sem nova cobrança | `NPCMovement`: só desliga se `disableAfterDialogue` e nada ficou pendente; entrega item a item; aviso de inventário cheio; não cobra nem assina evento se não houver fala; não abre conversa com diálogo ativo | validado (Play Mode) |
| 4 | Loot coletado não reaparece no mesmo caso; objeto vazio conserva a cobrança; outro caso usa o mesmo objeto | `LootInteractable` usa `lootsColetados` por caso; `destroyAfterLoot` some ao recarregar se já coletado | validado só por teste de regra (não há loot em nenhuma cena) |
| 5 | Confirmar de novo não reinicia relógio; recusar troca de caso ativo e caso concluído no domínio | `GameManager.PodeAceitarCaso` / `ConfirmarCaso` (retorna bool); também recusa caso de outra fase/rota ou já escolhido. `CaseSelectionUI` só fecha a mesa se aceito | validado (teste + Play Mode) |
| 6 | Publicação/conclusão/recompensa uma vez por caso; validar antes de consumir; preservar receita do tutorial | `GameManager.PodePublicarCaso`, `FoiPublicado`; `RegistrarPanfletoDeCaso` ignora repetição; `ConcluirCaso` retorna bool; `CraftingPress`: trava de reentrada, valida quantidade, caso em andamento, pista de outro caso e saída **antes** de consumir | validado (Play Mode: tutorial e Joalheiro) |
| 7 | Migrar save v2 sem perder progresso | `SistemaDeSave` versão 3 (ver 4.3) | validado (testes) |

### 4.2 Arquivos alterados

- `Assets/Scripts/RegistroDaInvestigacao.cs` (novo): `RegistroDaInvestigacao` e `IdDeInteracao`.
- `Assets/Scripts/GameManager.cs`: `registroDaInvestigacao` (substitui `interacoesPagas`, que era só de runtime e não estava serializado em cena/prefab), `PodeAceitarCaso`, `ConfirmarCaso` → bool, `PodePublicarCaso`, `FoiPublicado`, `ConcluirCaso` → bool, guarda em `RegistrarPanfletoDeCaso`, validação de IDs ao carregar cena.
- `Assets/Scripts/RelogioDeInvestigacao.cs`: cobrança por ID + registro; conversão das chaves do save v2; `JaPaga`.
- `Assets/Scripts/NPCMovement.cs`: `idDaInteracao`, entrega transacional, sem desligamento forçado.
- `Assets/Scripts/LootInteractable.cs`: `idDaInteracao`, estado por caso persistente.
- `Assets/Scripts/CraftingPress.cs`: validação antes de consumir, publicação única, trava de reentrada.
- `Assets/Scripts/CaseSelectionUI.cs`: fecha só quando o domínio aceita.
- `Assets/Scripts/SistemaDeSave.cs`: v3, `CapturarDados` / `DesserializarDados` / `AplicarDados` públicos (testáveis), migração, erro explícito sem catálogo.
- `Assets/Editor/IdsDeInteracaoTool.cs` (novo): menu **Ferramentas > Investigação > Gerar IDs de interação (cenas do build)** e **… > Validar IDs de interação (cenas do build)**. Idempotente; recusa rodar com cena suja ou em Play Mode; restaura as cenas abertas.
- `Assets/Editor/Testes/InvestigacaoESaveTests.cs` (novo): 11 testes EditMode.
- `Assets/Scenes/Jogo.unity`, `Assets/Scenes/Fase2.unity`: override `idDaInteracao` nos 5 NPCs. Ao salvar a cena Jogo, o Unity também gravou dois campos que já existiam no código e não estavam serializados (`TutorialManager.dicaTempo`, `TableInteractable.avisoSemCasos`) com o **mesmo valor padrão**, e reformatou a quebra de linha de uma mensagem do tutorial (texto idêntico). Sem mudança de comportamento.

Nenhum GUID, `.meta` existente, pacote, ProjectSettings ou UnityEvent foi alterado.

### 4.3 Save versão 3 e migração

- Novos campos: `interacoesPagasPorCaso`, `lootsColetados`, `recompensasEntregues`, `casosComEstadoLegado`, `interacoesPagasLegadas`. O campo antigo `interacoesPagas` continua sendo lido (só para migração).
- v1/v2 → v3 preserva capital (inclusive negativo), opiniões, horas do caso e restantes, caso atual, casos escolhidos/concluídos, fase, rota, inventário, panfletos, revelações pendentes, pistas verificadas e etapa do tutorial.
- Chaves antigas (`cena/nomeDoGameObject`) do caso em andamento viram `interacoesPagasLegadas`: a primeira interação com o objeto correspondente é convertida para a chave nova **sem cobrar**. Homônimos na mesma cena compartilham a chave antiga e todos contam como pagos — igual à cobrança do jogo antigo (ambiguidade registrada, sem reset).
- O caso em andamento de um save antigo fica em `casosComEstadoLegado`: como o v2 não registrava entregas/loot, nesse caso uma pista que o jogador **ainda tem** conta como já entregue (regra antiga), em vez de ser dada de novo. Casos novos não usam essa regra.
- Chaves antigas de um caso já encerrado são descartadas com log (o relógio só vale para o caso em andamento).
- Na prática, saves v2 quase nunca têm caso em andamento: os pontos de save existentes ficam fora de investigações.

### 4.4 Configuração realizada

- Ferramenta de IDs executada via MCP nas cenas do build: 5 IDs gravados (Jogo: `Marie Bradier`, `Charles Dupaty`; Fase2: `Joalheiro`, `Jean-Baptiste Réveillon`, `Operario`). Os IDs coincidem com a parte do nome das chaves antigas. Segunda execução: 0 alterações; validação: 0 problemas.

### 4.5 Testes executados

**EditMode — 11/11 aprovados** (`InvestigacaoESaveTests`):
registro por caso/interação; recompensa independente do inventário; loot por caso; homônimos com chaves distintas e detecção de ID repetido/vazio; reconfirmar não reinicia relógio; recusa de troca de caso, caso concluído e outra fase; dívida tira 1h e quitar não recarrega; publicação única (1 panfleto e 1 revelação); save v2 com caso em andamento (capital −30, 0h, inventário, chaves antigas); save v2 com caso encerrado; save v3 ida e volta.

**Play Mode (Editor, via MCP, estados montados por código):**

| Cenário | Resultado |
|---|---|
| Fase2: aceitar Joalheiro, tentar trocar para Operário, reconfirmar Joalheiro | aceito 4h; troca recusada; reconfirmação manteve 2h |
| Conversa com Joalheiro | −1h; 2 pistas entregues e registradas; NPC desligado (`disableAfterDialogue`) |
| Consumir as pistas, recarregar a cena, falar de novo | 0h cobradas; nenhuma pista devolvida |
| Prensa com pilhas de 2 pistas, `CombineItems` duas vezes | 1 publicação, capital +20 uma vez, caso concluído; 2º clique recusado sem consumir |
| Operário com inventário cheio (24 slots) | −1h; nada entregue nem registrado; NPC continua acessível e avisa |
| Liberar 2 slots e falar de novo | 0h cobradas; 2 pistas entregues; NPC desligado |
| Zerar horas, sair e voltar à cena | continua 0/4h; inventário restaurado (5 itens); NPC já pago conversa de graça; interação nova recusada |
| Porão: receita exata do tutorial, duas vezes | panfleto `Panfleto_MemoireJustificatif` na saída, capital +20, Caso_Tutorial concluído; 2ª tentativa recusada |

Observação: com o Editor sem foco o Play Mode não avança frames; para os testes de recarga foi usado `Application.runInBackground = true` só durante a sessão de Play (não altera ProjectSettings).

**Não testado em runtime:** percurso completo do tutorial com input real (mouse/teclado/toque), porta/fade, save/Continuar pelo menu com arquivo real, `LootInteractable` em cena (não existe nenhum).

### 4.6 Limitações e mudanças de comportamento

- Dupaty (`disableAfterDialogue` = falso) agora continua conversável depois de entregar o Decreto no tutorial, repetindo a fala sem entregar nada; antes era desligado à força. O contorno de interação continua aceso até o `GerenciadorCena1` silenciá-lo ao voltar do porão. Conferir visualmente no Prompt 7.
- NPCs com `disableAfterDialogue` = verdadeiro voltam a ficar interagíveis ao recarregar a cena (o `canInteract` não é persistido); repetir não cobra nem entrega de novo. A revisão de `disableAfterDialogue`/`casoObrigatorio` é escopo do Prompt 2.
- A etapa de recompensa é única por reação (`"reacao"`), porque `CasoReacao` tem uma lista de recompensas só; entregas em várias etapas ficam para os Prompts 2/3 (a chave já comporta a etapa).
- Um diálogo aberto por NPC sem falas não cobra horas (antes cobrava e deixava o evento de fim pendurado).
- Não há ponto de save durante um caso; "salvar/carregar" no meio da investigação só ocorre se um save for adicionado depois (sugestão para o Prompt 7).
- IDs vazios funcionam (caminho na hierarquia), mas mover/renomear o objeto na hierarquia mudaria a chave: rode a ferramenta de IDs ao adicionar NPCs/objetos.

### 4.7 Conteúdo provisório

Nenhum conteúdo narrativo novo. Textos novos de UI (avisos): "Este caso já foi concluído.", "O panfleto deste caso já foi publicado.", "Inventário cheio. Libere espaço e fale de novo para receber o que faltou.", "Inventário cheio. Libere espaço e volte para pegar esta pista.", "Você já vasculhou aqui.", "Este caso não pertence à fase atual/rota.".

### 4.8 Como revalidar

1. Unity 6000.3.9f1 → **Window > General > Test Runner > EditMode > Run All** (esperado 11/11).
2. **Ferramentas > Investigação > Validar IDs de interação (cenas do build)** → "0 problema(s)".
3. Manual: Novo Jogo → tutorial completo → aceitar um caso da Fase 2 → falar com o NPC → imprimir → voltar à Fase 2 e falar de novo (sem horas, sem pistas) → tentar imprimir de novo (recusado).

---

## 5. Prompt 2 — conteúdo e progressão das fases

### 5.1 Requisitos atendidos

| Requisito | Implementação | Estado |
|---|---|---|
| Preservar tutorial e os 3 casos da Fase 2 | Cena Jogo, receita do tutorial e valores Fatos/ComBoato/Calúnia da Fase 2 intactos. Só textos vazios foram preenchidos | validado (Play Mode Fase 2; tutorial não tocado) |
| Todos os NPCs da fase respondem ao caso ativo | NPCs da Fase 2 sem `casoObrigatorio`, com `disableAfterDialogue` falso e fala padrão "pouco útil"; a fala original do caso ficou na reação | validado (Play Mode: Réveillon responde ao Caso das Joias) |
| 6–7 oportunidades por caso com fatos, boatos e objetos vazios (4h) | 7 por caso: 4 NPCs + 3 objetos; 2 fatos, 1 boato, 1 calúnia, 3 sem nada. F2 dos casos antigos saiu do cliente para um objeto da rua | validado (validador: 7 em todos os 10 casos) |
| 4 casos da Fase 3 (concluir 2) e 3 da Fase 4 (um por rota) | 7 `CaseData` novos com cena, receita, panfleto, pistas e diálogos | validado (validador, testes, Play Mode) |
| Registrar na mesa e no `CatalogoDeSave`; percurso possível por caso | `UI.prefab` e catálogo atualizados; caminho mínimo de 2h por caso | validado |
| Fase3/Fase4 não podem ser ruas vazias | 4 NPCs + 3 objetos próprios em cada; tom de luz diferente no cenário | validado estruturalmente; arte placeholder |
| Mesa com disponível / em andamento / concluído; um por vez | `CaseSelectionUI.EstadoDe`; o botão usa a mesma regra do domínio | validado (teste + UI real em Play Mode) |
| Fase avança só com a cota; Fase 4 não vai sozinha ao tribunal | Cota já existente; nova guarda: com a cota atingida o domínio recusa outro caso da fase. Na Fase 4 `FimDeFase` não age, e o tribunal continua com o Dupaty | validado |
| Esgotamento de tempo com 0/1 pista e dívida (3h) sem campanha sem saída | Distribuição: caminho mínimo 2h ≤ 3h. Saída de design: **alegações iniciais** (ver 5.2) | validado (Play Mode: 3h endividado, 3 interações vazias, impresso com as alegações) |

### 5.2 Decisão de design aplicada: alegações iniciais (proposta 9 do documento)

A distribuição sozinha não garante saída: com 3 oportunidades vazias por caso, quem gastar 3h no lugar errado fica sem pistas. Por isso cada caso entrega, ao ser aceito, **2 alegações do cliente**. Elas são pistas do próprio caso com confiabilidade interna **Boato**, fonte "Carta do cliente", e aparecem como "não verificada" como qualquer pista. As duas juntas imprimem a nova versão **Só Alegações** da receita (novo valor `NivelDoPanfleto.Alegacoes`, acrescentado ao fim do enum — saves antigos continuam válidos). Essa versão rende sempre menos que Fatos e tem revelação própria (teste `Receitas_SoAlegacoesRendemMenosQueFatos`). Misturadas com pistas, seguem Fato × Boato. Não há horas extras. A entrega é registrada uma vez por caso; com inventário cheio, fica pendente e entra quando o inventário de uma cena é restaurado. Ao concluir o caso, as alegações que sobraram são arquivadas (saem do inventário, com aviso), para não lotar os 24 slots ao longo da campanha. **Isto é proposta de design, não requisito literal dos PDFs.**

### 5.3 Arquivos

Código:
- `Assets/Scripts/@CaseData.cs`: `alegacoesIniciais`, `EhAlegacao`.
- `Assets/Scripts/ReceitaDeCaso.cs`: `NivelDoPanfleto.Alegacoes`, versão `soAlegacoes`, classificação.
- `Assets/Scripts/GameManager.cs`: `EntregarAlegacoesPendentes`, arquivamento ao concluir, guarda de cota da fase em `PodeAceitarCaso`.
- `Assets/Scripts/InventoryManager.cs`: `RemoverItens`; entrega de alegações pendentes após restaurar.
- `Assets/Scripts/CaseSelectionUI.cs`: três estados; botão segue o domínio.
- `Assets/Editor/CampanhaSetupTool.cs` (novo): **Ferramentas > Campanha > 1 - Aplicar conteúdo da campanha (Fases 2 a 4)**.
- `Assets/Editor/ValidadorDaCampanha.cs` (novo): **Ferramentas > Campanha > 2 - Validar campanha**.
- `Assets/Editor/Testes/CampanhaTests.cs` (novo, 6 testes); `InvestigacaoESaveTests.cs` ajustado (ver 5.5).

Assets criados (105): 7 casos em `Assets/Casos/`; 7 receitas, 7 panfletos, 34 pistas e 20 alegações em `Assets/Scriptableobjects/Campanha/Fase2|Fase3|Fase4/`; 30 diálogos em `Assets/Dialogos/Campanha/Fase2|Fase3|Fase4/`.

Assets alterados (só campos vazios preenchidos): 3 casos da Fase 2 (alegações), 6 pistas da Fase 2 (nome, descrição, fonte, ícone), 3 receitas da Fase 2 (textos de revelação e versão Só Alegações), 3 panfletos da Fase 2 (ícone), `UI.prefab` (7 casos na mesa), `Resources/CatalogoDeSave.asset`.

Cenas: `Fase2` (Gazeteiro, 3 objetos, migração dos 3 NPCs), `Fase3` e `Fase4` (4 NPCs, 3 objetos, tom do cenário). `Jogo`, `Porao`, `Tribunal` e o menu não foram alterados.

### 5.4 Configuração realizada

Ferramenta de conteúdo executada via MCP. 1ª execução: 105 assets, 9 NPCs, 9 objetos, 37 reações/entradas, migrações dos NPCs da Fase 2, 7 casos na mesa. Defeito encontrado e corrigido durante a execução: ao abrir outra cena o Editor descarregava diálogos recém-criados, e 9 NPCs novos ficaram sem fala padrão. A ferramenta passou a guardar caminhos e recarregar os assets; a execução seguinte completou os 9. Execuções seguintes: **0 alterações** (idempotente). Verificação direta: 40 reações/entradas sem referência nula; todos os casos com rota de diálogo.

Resultado do validador: **Campanha válida** — Fase 2: 3 casos, Fase 3: 4, Fase 4: 3 (um por rota); os 10 casos com 7 oportunidades, 4 com pista (2 fatos, 2 boato/calúnia), 3 sem nada, caminho mínimo 2h (orçamento 4h; 3h endividado).

### 5.5 Testes executados

**EditMode — 17/17 aprovados** (11 do Prompt 1 + 6 novos): validador sem problemas; classificação das alegações; Só Alegações rende menos e tem consequência; 4 casos na Fase 3 e 1 por rota na Fase 4; três estados da mesa e cota de 2 casos na Fase 3; catálogo resolve casos, alegações e panfletos. O teste `ConfirmarCaso_RecusaTrocaDeCasoEmAndamento_ECasoConcluido` passou a declarar cota de 3 casos na fase de teste, porque a nova guarda recusa corretamente um 2º caso na Fase 2.

**Play Mode (Editor via MCP, estados montados por código):**

| Cenário | Resultado |
|---|---|
| Fase 3, endividado: aceitar Padeiro | 3/3h; 2 alegações entregues |
| 3 interações sem nada (Peticionária, Gravador, Caixa de Tipos) | 0h, nenhuma pista útil; Viúva recusada por falta de tempo |
| Prensa com as 2 alegações | versão Alegacoes, panfleto do caso, −5/+10/+12, revelação −5 agendada, 1/2 da fase |
| Reaceitar Padeiro; aceitar Varennes | recusado; aceito 4/4h (dívida quitada pelo ouro) |
| Cocheiro + Mural (caminho mínimo) → prensa | 2h gastas; versão Fatos; 2/2; alegações restantes arquivadas (0) |
| Tentar um 3º caso na Fase 3 | aceito — **defeito**, corrigido com a guarda de cota e coberto por teste |
| Fase 4, mesa real para rotas A, B e C | exatamente 1 cartão por rota, com o caso certo |
| Rota B: aceitar caso da rota A; aceitar Negociante; 4 interações; F1 + boato | A recusado; ComBoato, +80 ouro, revelação agendada; fase concluída sem avançar; mesa mostra "(concluído)" bloqueado |
| Fase 2: Réveillon no Caso das Joias; Joalheiro; Mesa da Taverna; Joalheiro de novo | fala "pouco útil"; só F1; F2 na mesa; NPC continua acessível e não cobra de novo |

**Não testado em runtime:** percurso com input real, porta/fade e retorno pelo alçapão/porão entre casos, save/Continuar com arquivo real (a resolução dos casos novos no catálogo foi testada), legibilidade visual dos objetos e NPCs novos.

### 5.6 Limitações e pendências

- **Arte:** NPCs novos são cápsulas coloridas (como Jean e o Operário); objetos usam `mesa.png`, o pergaminho e o banner de papel; Fase3/Fase4 reaproveitam a rua com outro tom de luz. Posições (x) precisam de conferência visual.
- **Revelações:** `RevelacaoDeBoatos` continua fora das cenas, então as penalidades agendadas (inclusive da versão Só Alegações) ainda não são aplicadas. Prompt 6.
- **Ícones:** todas as pistas usam o mesmo banner de papel e todas as alegações o mesmo selo; os ícones não diferenciam verdade.
- **Caminho barato:** o cliente sempre dá F1; um jogador atento conclui em 2h. É o requisito de orçamento mínimo; a dificuldade vem de identificar o objeto certo entre 3.
- `CaseData.moneyReward`/`publicOpinionReward`/`stateOpinionReward` dos casos novos são estimativas da mesa, alinhadas à versão Fatos.
- Rodar de novo a ferramenta não sobrescreve textos e valores dos assets. **Exceção (descrição corrigida na verificação; comportamento não alterado):** nos NPCs das cenas, a cada execução ela volta a forçar `casoObrigatorio` vazio, `disableAfterDialogue` falso e `canInteract` verdadeiro, e remove de novo a F2 da reação do cliente. Para regenerar um asset, apague-o antes.

### 5.7 Conteúdo provisório

Tudo o que é novo: 7 casos das Fases 3/4 (títulos, descrições, objetivos), 34 pistas, 20 alegações, 7 panfletos, 30 diálogos, falas "pouco úteis" dos NPCs da Fase 2, textos de revelação e valores das receitas novas, a versão Só Alegações de todos os casos. Os casos usam eventos históricos como pano de fundo (fome de 1789, Varennes, Champ de Mars, assignats, Terror de 1793) com personagens e detalhes inventados; não foram tirados do GDD, que não foi localizado. Lista completa: `docs/MATRIZ_DE_CASOS_TCC.md`.

### 5.8 Como revalidar

1. **Ferramentas > Campanha > 2 - Validar campanha** → "Campanha válida".
2. Test Runner > EditMode > Run All → 17/17.
3. **Ferramentas > Campanha > 1 - Aplicar conteúdo…** de novo → "Assets criados: 0 … casos novos na mesa: 0".
4. Manual: Novo Jogo → tutorial → aceitar um caso da Fase 2 → conferir as 2 alegações no inventário → investigar → imprimir → voltar ao escritório (fim da Fase 2) → dois casos da Fase 3 → Fase 4 com um só cartão → Dupaty → tribunal.

---

## 6. Prompt 3 — biblioteca, qualidade e pistas complementares

### 6.1 Requisitos atendidos

| Requisito | Implementação | Estado |
|---|---|---|
| Botão abre catálogo (nome, descrição, preço, disponibilidade por fase/caso, comprado) com ofertas em assets | Botão "Biblioteca" no canto superior direito a partir da Fase 3; janela com a lista das ofertas dos casos visíveis na mesa; estados Disponível / Ouro insuficiente / Aceite o caso / Comprado / Você já tem. Ofertas = assets `OfertaDaBiblioteca` | validado (Play Mode, UI real acionada por código) |
| Compra só com saldo; sem crédito; dívida não compra; entrega única; sem cobrança parcial; persistida por oferta+caso | `GameManager.ComprarNaBiblioteca`: valida, entrega e **só então** cobra; recusa capital negativo ou menor que o preço; trava de reentrada + botão desabilitado; `comprasDaBiblioteca` = "oferta\|caso" | validado (testes + Play Mode: saldo exato, 2º clique forçado, inventário cheio) |
| Qualidade independente da confiabilidade | `QualidadeDaEvidencia` separada; documento de apoio é `NaoEPista`; teste confirma que o apoio não muda versão nem penalidade | validado |
| Dois slots preservados; apoio opcional, sem consumo, sem empilhar bônus | Campo **Suporte** ao lado do inventário quando a prensa abre (Fase 3+); seleção por caso; documento de apoio nos slots é recusado com aviso; cada qualificador vale uma vez e cada documento serve a um só | validado (Play Mode: documento continua no inventário; 2 apoios do mesmo qualificador = 1 bônus) |
| NPCs com pistas complementares em etapa posterior, pré-requisitos editáveis, sem repetir | `CasoReacao.etapasComplementares` (id, requisitos, diálogo, recompensas). Requisito = evidência já obtida (`GameManager.evidenciasObtidas`, vale mesmo se gasta). Só entra depois que a fala normal entregou tudo. Configurado para Cocheiro (Varennes) e Cidadã Delorme (Girondina) | validado (Play Mode: Varennes) |
| Exemplo (+20, −10, +30) → (+40, −5, +50) | Champ de Mars: Fatos 20/−10/30 + qualificador `apoio_champdemars` (+20/+5/+20) com a "Ata da prefeitura de Paris" | validado (teste com assets reais + Play Mode: HUD 50→90 / 50→45 / 0→50) |
| Cálculo centralizado, sem alterar assets compartilhados | `CalculadoraDePanfleto.Calcular` → `ResultadoDoPanfleto` (cópia): versão → apoio → [linha editorial, Prompt 5]. A prensa só aplica | validado (teste confirma o asset intacto) |
| Prévia sem revelar a classificação | Campo Suporte mostra nomes nas entradas, tendência estimada do caso (a mesma da mesa), quantos apoios aceitos estão marcados e "o resultado real depende da verdade das pistas". Nenhum número, nenhuma versão | validado (texto conferido em Play Mode) |
| Salvar compras, evidências e dados para reconstruir resultados; preservar saves | Save **v4** (o snapshot guarda o valor calculado, antes do limite 0–100 aplicado ao Povo/Estado): `comprasDaBiblioteca`, `evidenciasObtidas` e, por publicação, pistas, apoios, qualificadores e valores aplicados (snapshot). v1–v3 carregam; publicações antigas ficam `legado` sem valores inventados; evidências antigas = só o que está no inventário | validado (testes v4 e v3) |

### 6.2 Arquivos

Código novo: `CalculadoraDePanfleto.cs`, `OfertaDaBiblioteca.cs`, `BibliotecaUI.cs`, `SuportesDaPrensaUI.cs`. Alterados: `Item.cs` (qualidade, documento de apoio), `ReceitaDeCaso.cs` (qualificadores), `GameManager.cs` (compra, evidências, histórico da publicação), `CraftingPress.cs` (calculadora e seleção de apoio), `NPCMovement.cs` (etapas complementares), `InventoryManager.cs` (registro de evidência, rótulo de apoio, `ItemNaGrade`), `SistemaDeSave.cs` (v4), `FichaDaPista.cs`, `CaseSelectionUI.cs` (`CasoVisivel` público), `PauseMenu.cs`, `PlayerInteraction.cs`, `PlayerMove.cs` (biblioteca aberta bloqueia pause, interação e movimento).

Editor: `BibliotecaSetupTool.cs` (**Ferramentas > Campanha > 3 - Aplicar biblioteca e documentos de apoio**), `ValidadorDaCampanha.cs` (checagens da biblioteca), `Testes/BibliotecaTests.cs` (8 testes).

Assets (18 novos): 7 ofertas em `Scriptableobjects/Campanha/Biblioteca/`, 9 documentos de apoio, 2 diálogos de etapa complementar. Alterados: 7 receitas das Fases 3/4 (qualificador), receita e caso do Champ de Mars (valores do exemplo, só porque ainda estavam com os valores provisórios do Prompt 2), `UI.prefab` (objeto `Biblioteca` com as ofertas; `SuportesDaPrensaUI` no `PainelPrensa`), cenas `Fase3` e `Fase4` (etapas complementares), `CatalogoDeSave`.

### 6.3 Configuração e testes

- Ferramenta 3 executada: 18 assets, 26 alterações; 2ª execução: 0/0. Ferramentas 1 e 2 reexecutadas sem alterações. Validador: **Campanha válida**, 7 ofertas.
- **EditMode: 25/25** (17 anteriores + 8 novos: exemplo numérico, qualidade × verdade, saldo exato e repetição, saldo insuficiente/negativo/dívida com preço 0, inventário cheio, caso/fase, save v4, save v3).
- **Play Mode (Fase 3, estados montados por código; botões acionados pelo `onClick`):**

| Cenário | Resultado |
|---|---|
| Botão da biblioteca na Fase 3 | visível; abre com pausa (timeScale 0) e bloqueio de pause; lista só as 4 ofertas da Fase 3; só a do caso ativo comprável |
| Saldo 20 = preço 20; 2º clique forçado no listener | capital 0; 1 documento; 1 compra; mensagem "Você já comprou"; linha "Comprado" |
| Fechar | timeScale 1; `BloqueiaPausa` verdadeiro no mesmo frame (o Esc não abre o pause) |
| Champ de Mars: F1 + F2 + Ata como apoio | versão Fatos; Povo 50→90, Estado 50→45, Ouro 0→50; histórico com pistas, apoio, qualificador e snapshot 40/−5/50; documento continua no inventário |
| Varennes: comprar com inventário cheio; liberar espaço | InventarioCheio sem cobrança; depois Comprada (60→35) |
| Cocheiro antes / depois do Depoimento; 3ª conversa | contrato → recibo da estalagem (sem nova hora) → fala normal, sem repetir o recibo |
| Apoio da biblioteca + recibo + alegação selecionados | alegação recusada; bônus aplicado uma vez (−20/40/65) |
| Recarregar a cena | oferta continua "Comprada"; documentos no inventário |

**Layout:** a captura de tela do Editor não inclui a UI em overlay (Editor sem foco), então o layout foi conferido por coordenadas. O campo Suporte invadia a HUD de status em telas largas; foi corrigido (topo em y=260, HUD a partir de 289). **Não conferido visualmente:** aparência, legibilidade, toque real e navegação por teclado (o foco inicial vai para "Fechar").

### 6.4 Limitações

- **Tela 4:3:** o `PainelPrensa` original (x 420–860) já sai da tela quando o canvas tem 1440 de largura; o campo Suporte encolhe para 300 e cabe. Problema anterior a esta etapa (Prompt 7).
- **Inventário:** documentos de apoio não são arquivados ao concluir o caso (podem virar prova no tribunal, Prompt 6). Estimativa do pior caso ao entrar na Fase 4: ~22 dos 24 slots. Acompanhar no Prompt 6/7.
- **Tecla da biblioteca:** não há atalho de teclado para abrir (só o botão); Esc fecha.
- **Preços e bônus:** só vendem com ouro; após as despesas da Fase 2 o jogador pode não ter saldo. Balanceamento no Prompt 8.
- **Apoio × revelação:** o apoio não altera a penalidade de boatos; reforça o ganho imediato em qualquer versão (qualificadores sem restrição de versão, para a prévia não virar pista da verdade).

### 6.5 Conteúdo provisório

7 ofertas (títulos, descrições, preços), 9 documentos de apoio, 7 qualificadores (valores), 2 falas de etapa complementar, novos valores do Champ de Mars. Detalhes na matriz (`docs/MATRIZ_DE_CASOS_TCC.md`).

### 6.6 Como revalidar

1. Ferramentas > Campanha > 3 (e 1) de novo → 0 alterações; Ferramentas > Campanha > 2 → "Campanha válida".
2. Test Runner > EditMode → 25/25.
3. Manual (Fase 3): aceitar o Champ de Mars com 20 de ouro → botão Biblioteca → comprar a Ata → investigar Peticionária + Balcão → prensa → marcar a Ata no campo Suporte → Misturar → HUD +40 / −5 / +50.

---

## 7. Verificação dos Prompts 0–3 (25/09/2026)

Estado verificado: Prompt 1 no commit `541f1a9`; Prompts 2 e 3 no working tree, sem commit.

### 7.1 Checagens executadas

| Checagem | Resultado |
|---|---|
| Compilação | sem erros (só os 2 avisos antigos do `Caso_Tutorial`) |
| EditMode | 25/25 |
| Validador da campanha | "Campanha válida" |
| Ferramentas 1–3 e IDs reexecutadas | 0 alterações; 0 problemas de ID |
| Git | nada alterado em ProjectSettings, Packages, `.meta` existentes, `Jogo`/`Porao`/`Tribunal`/menu no working tree |
| **Continuar com o save real do jogador (v3)** | carregou na cena Jogo: Fase 3, capital, casos, registro, inventário; publicação antiga marcada `legado`; revelação pendente preservada |
| **Fluxo real a partir desse save** | mesa → aceitar Assignats (alegações entregues) → porta → Fase 3 → compra na biblioteca → Gravador + Caixa de Tipos → porta → alçapão → Porão → `PressInteractable` + apoio + botão Misturar → sair com o panfleto na saída → escritório: panfleto voltou à grade, apoio preservado, alegações arquivadas, mesa com "(concluído)" |
| **Tutorial completo (Novo Jogo)** | intro → continuar → andar → pause → Dupaty → Marie (escolhas) → inventário → Dupaty (Decreto uma vez; 3ª conversa não duplica) → alçapão → prensa → Misturar → guardar panfleto → porta → Fase 2, Marie sumiu, Dupaty silenciado, cutscene da Fase 1 → mesa → 1º caso da Fase 2 com alegações; tutorial concluído; botão da biblioteca oculto na Fase 2 |

O `save.json` e as chaves de progresso do PlayerPrefs foram copiados antes e **restaurados** depois (hash do save idêntico ao original). Ações de clique foram disparadas pelo `onClick`/`Interact` via MCP, não por input real.

### 7.2 Achados da verificação — todos corrigidos

| # | Achado | Correção | Evidência |
|---|---|---|---|
| 1 | "1 fato + 1 alegação" classificava como ComBoato e rendia mais que Fato+Fato | `ReceitaDeCaso.Classificar`: qualquer alegação (sem calúnia) → **Só Alegações**; calúnia continua prevalecendo | teste `AtalhoComAlegacao_NuncaRendeMaisQueAInvestigacao_EmNenhumCaso` (10 casos); Play Mode Varennes: −8/22/38 contra −20/40/65 da investigação |
| 2 | Nomes denunciavam a verdade | 40 pistas renomeadas para o assunto; relatos ("Segundo…"/"X mostra…") para o que vem de pessoas e documentos para o que vem de objetos, em todos os níveis (`CampanhaSetupTool.TextosRevisados`, migra só assets não editados). O validador passou a recusar prefixos reveladores | validador "Campanha válida"; nomes conferidos no inventário em Play Mode |
| 3 | Compra sem efeito (apoio equivalente) e prévia contando documentos | Estado `ApoioEquivalente` (não vende nem cobra); prévia conta **reforços** (`CalculadoraDePanfleto.ReforcosPossiveis`), avisando que equivalentes não somam | teste `Oferta_ApoioEquivalenteJaObtido_NaoEhVendida`; Play Mode: "Você já tem um apoio equivalente", compra recusada, capital intacto |
| 4 | Inventário sem descarte podia travar | Ao concluir caso antes da Fase 4, sobras (pistas, apoios, alegações) saem da grade **e das entradas da prensa** (histórico preservado); NPC/objeto não cobra horas se o que entregaria não cabe | testes `ConcluirCaso_ArquivaSobras…`, `Npc_InventarioCheio…`, `Loot_…InventarioCheioNaoCobra`; Play Mode: inventário com 4 itens ao entrar na Fase 4 |
| 5 | Componentes sem teste automatizado | `ComponentesTests` (13 testes) sobre relógio, `NPCMovement`, `LootInteractable`, `CraftingPress` e compra com `AddItem` real | 41/41 EditMode |
| 6 | Histórico sem o delta aplicado | Save **v5**: `povoAplicado/estadoAplicado/ouroAplicado` (+ `aplicadoConhecido`); saves < 5 ficam "desconhecido", nada recalculado | teste `Historico_GuardaODeltaEfetivamenteAplicado` (95 +20 → +5) |
| 7 | Texto "Aceite o caso" genérico | Estados `CasoNaoAceito`, `OutroCasoEmAndamento`, `CasoConcluido`, cada um com texto próprio | Play Mode: "Termine o caso atual primeiro" |
| 8 | Botão da biblioteca sobre o pause | Oculto com pause, inventário/prensa, diálogo ou cutscene | Play Mode: some com o inventário aberto e volta ao fechar |
| 9 | Estimativas da Fase 2 contradiziam as receitas | Alinhadas à versão Fatos (só se ainda com os valores antigos) | conferido nos assets |
| 10 | Textos históricos | Revelações do Operário e de Réveillon reescritas; livro de salários passou a ser "cópia publicada por uma gazeta" (fonte "Gazeta vendida na banca") | conferido nos assets |
| 11 | `#2` do 2º homônimo diferia do runtime | `IdDeInteracao.IdPadrao` (caminho + `#n` por ordem entre irmãos) usado pelo jogo e pela ferramenta | 0 problemas de ID |
| 12 | Ambiguidade da migração v2 só contada | Log lista as chaves antigas; na 1ª interação, homônimos que dividem uma chave são listados com os IDs novos | código (`RelogioDeInvestigacao.AvisarHomonimos`) |

Suspeitas também tratadas:
- **Receita exata:** a receita exata não conclui casos que têm receita de caso (itens do tutorial não publicam outro caso).
- **Posse no save legado:** o modo v2 e as evidências consultam grade, prensa e mão (`InventoryManager.PossuiEmQualquerLugar`).
- **Save v1:** um save v1 com caso em andamento recebe o orçamento do caso.
- **Qualificadores:** a atribuição é um emparelhamento máximo, independente da ordem de seleção (teste `Qualificadores_NaoDependemDaOrdemDeSelecao…`).
- **Caso concluído:** NPC e objeto não entregam pistas (teste `Npc_CasoConcluido_SoConversa`).
- **Etapas complementares:** id vazio, repetido ou reservado é ignorado com aviso e acusado pelo validador.
- **Evidências:** qualquer item obtido passa a valer como pré-requisito, mesmo sem caso.

Outras correções encontradas durante a reverificação:
- **FMOD fora de Play Mode:** `AudioSeguro` não toca o `RuntimeManager` fora de Play Mode. Em Edit Mode ele gerava erro de console; o jogo não muda.
- **Panfletos da Fase 2:** os 3 ganharam nome de exibição (apareciam como `panfleto_joalheiro`).
- **Ferramenta de conteúdo:** a migração dos NPCs agora roda uma única vez, então edições no Inspector (`casoObrigatorio`, `disableAfterDialogue`, recompensas) são preservadas.

### 7.3 Reverificação após as correções

| Checagem | Resultado |
|---|---|
| Compilação / console | sem erros |
| EditMode | **41/41** (25 anteriores ajustados às regras novas + 16 novos) |
| Validador / ferramentas / IDs | "Campanha válida"; 2ª execução das ferramentas sem alterações; 0 problemas de ID |
| Continuar com o save real (v3) → Varennes (Mural + Cocheiro → recibo; biblioteca "apoio equivalente"; impressão fato + alegação + apoio = Só Alegações; sobras arquivadas) → Padeiro (Fatos) → escritório | Fase 4, **rota C** (77 × 67), despesa de 50 cobrada, inventário com 4 itens, mesa com 1 cartão (A Viúva Girondina) |
| Tutorial completo (Novo Jogo) | sem regressões; Novo Jogo zerou compras, evidências e publicações da partida anterior |
| Git | nada alterado em ProjectSettings, Packages, `.meta` existentes, `Jogo`/`Porao`/`Tribunal`/menu |

Save e PlayerPrefs do jogador restaurados de novo depois dos testes (hash idêntico).

**Ainda não coberto:** input real (mouse/teclado/toque), conferência visual da UI e o tribunal ponta a ponta (Prompt 6).

---

## 8. Próximo prompt

~~Prompt 5 — linha editorial (D)~~ Concluído em 26/09 (§10).

**Recomendado agora: o que resta dos Prompts 6 e 7** (o que já foi feito está em §9 e §10), depois o Prompt 8. Pontos de atenção vindos do Prompt 5:
- **Prompt 6/8 (balanceamento):** a linha editorial desloca o desnível Povo × Estado em até 15 pontos por publicação (3 publicações antes da rota). A rota B ficou alcançável com "Agradar"; conferir as três rotas com decisões reais.
- **Prompt 7 (dicas):** a janela de linha editorial já explica cada opção; falta só a dica de primeira vez prevista no Prompt 7, se o grupo quiser.

**Prompt 4 — dedução ativa (C), opcional.** Se o grupo decidir incluir, os pontos de partida continuam valendo:
- **Histórico:** `GameManager.evidenciasObtidas` já guarda as pistas obtidas, inclusive as gastas.
- **Verificação atual:** a verificação automática está em `InventoryManager.PistaVerificada`/`RegistrarVerificacoes` e na `FichaDaPista`; hoje nenhuma pista tem `verificadaPor`.
- **Fora dos conjuntos de dedução:** alegações e documentos de apoio.
- **Pistas:** os nomes já são neutros. A **fonte** continua sendo a dica de confiabilidade prevista no desenho original.

Questões em aberto:
- ~~Revelações: onde colocar `RevelacaoDeBoatos` e a ordem em relação à trava de rota~~ Resolvido em 26/09 (§9).
- ~~Tribunal: provas restritas ao caso, incluindo documentos comprados~~ Resolvido em 26/09 (§9).
- **Inventário ao restaurar:** perda silenciosa de itens quando a grade está cheia (Prompt 6).
- **Mesa depende do panfleto do tutorial:** `TableInteractable.itemObrigatorio` exige `Panfleto_MemoireJustificatif` na grade. Em jogo normal ele nunca sai, mas um save sem ele trava a mesa (Prompt 6).
- **Marie ausente após save/load:** depende do panfleto do tutorial no inventário ou de um caso diferente do tutorial (Prompt 7).
- **Referências quebradas:** as listadas em 3.2, item 5.

---

## 9. Verificação completa e correções (26/09/2026)

Verificação feita contra os dois PDFs ("Tarefas de Programação TCC" e "Mecânica de dificuldade"), seguida das correções aprovadas pelo grupo. Nada foi commitado pela sessão; o commit fica com o grupo.

### 9.1 O que foi verificado e como

Compilação sem erros, EditMode 41/41 antes das correções, validador "Campanha válida" e Play Mode no Editor via MCP. As ações foram disparadas pelas mesmas funções dos botões (`Interact()`, `onClick`), não por teclado ou toque físicos.

| Percurso | Resultado |
|---|---|
| Tutorial completo (15 etapas), cutscene de abertura, Marie/Dupaty, alçapão, prensa, cutscene da Fase 1, mesa | sem travar; Marie some e Dupaty silencia ao voltar |
| Fase 2 (Caso das Joias) | os 4 NPCs respondem; NPC errado gasta 1h sem pista; Gazeteiro entrega boato |
| Fase 3 (Champ de Mars e Varennes) | biblioteca (30 → 10 de ouro), relógio (repetir grátis, 0h recusa), prensa +40/−5/+50 com a Ata, pista complementar do Cocheiro |
| Fim da Fase 3 | despesa de 50, avanço para a Fase 4, rota C |
| Fase 4 (Viúva Girondina) | mesa com 1 cartão; Dupaty recusa antes do panfleto e libera depois |
| Tribunal rotas A, B e C | barra explode em A, fica ≤ 32 em B; tela de fim e volta ao menu |
| Controles no celular (Ferramentas > Controles > Simular celular no Editor) | as 15 etapas do tutorial com o mesmo texto e só os controles trocados; botões na tela; ícone "Interagir" |

### 9.2 Defeitos encontrados e correções

| # | Defeito | Correção | Evidência |
|---|---|---|---|
| 1 | A punição por boato nunca era aplicada (`RevelacaoDeBoatos` não estava em cena): a calúnia sempre rendia mais | `GameManager.EncerrarFase` (revelações → despesas → avanço/rota) usado pelo `FimDeFase`; revelação na cutscene; `RevelacaoDeBoatos` removido | testes `EncerrarFase_*`; Play Mode: Povo 70→60, Estado 45→35 antes da rota |
| 2 | Tribunal usava o inventário inteiro como prova (pistas gastas sumiam, panfletos de outros casos entravam) | `TribunalManager.ProvasDoCaso` pelo histórico | teste + Play Mode |
| 3 | Tutorial do status só depois de imprimir e sem citar fato/boato | etapa `explicar_efeito_barras` logo após abrir a prensa, novo texto, `DestaqueDeEtapaTutorial` pisca o HUD, `TutorialManager.AcaoJaFeita` pula etapas já cumpridas | Play Mode: 4 ordens diferentes de ação, nenhuma trava |
| 4 | Botão Biblioteca por cima da mesa de casos | `CaseSelectionUI.Aberta` esconde o botão | Play Mode |
| 5 | Pop-up de item cobria o relógio | pop-up desce abaixo do relógio (`HudDoRelogio.BordaInferior`) | Play Mode |
| 6 | Sem ícone de interação nas Fases 2–4 | `PromptDeInteracao` no Player das três cenas | Play Mode |
| 7 | Alçapão sem som | `event:/bauabrir` | cena Jogo |
| 8 | Fade de troca de cena na ordem 9, abaixo de pop-ups (10), biblioteca (30) e cutscenes (50) | ordem 1000 no prefab + garantia no `SceneTransitionManager.Awake` | Play Mode |
| 9 | Cenário piscava antes da cutscene de início de cena (fade da cutscene cruzando com o da troca) | `CutsceneLegendas` nasce preta com a tela coberta ou no início da cena | medido: 0% do cenário visível em 3.872 frames |
| 10 | `Ferramentas > Tutorial > 2` não copiava "Salvar Ao Concluir" (apagava o save do fim do tutorial) | campo copiado | compilação |

### 9.3 Finais (decisão do grupo)

A rota decide o destino do jogador; o panfleto do caso da Fase 4 decide o do réu (Fatos = absolvido; boato, calúnia ou só alegações = condenado). Isso muda a fala do veredito e a 1ª linha da cutscene. Final C = "O Esquecido": a barra dos juízes termina no meio (50). Textos em `TribunalManager.DesfechosPadrao()` e na cena Tribunal, todos provisórios.

| Final | Réu absolvido | Réu condenado |
|---|---|---|
| A: Guilhotina | o réu sai livre, o jogador é preso | réu e jogador condenados |
| B: Tirano | o réu é absolvido e o Comitê tolera | o réu é condenado para agradar o Estado |
| C: O Esquecido | o réu é solto; ninguém lembra quem o defendeu | o réu é condenado; o panfleto é esquecido |

### 9.4 Arquivos

- Scripts: `GameManager`, `FimDeFase`, `TribunalManager`, `TutorialManager`, `CutsceneLegendas`, `SceneTransitionManager`, `CaseSelectionUI`, `BibliotecaUI`, `HudDoRelogio`, `ItemPickupNotificationUI`, `ReceitaDeCaso` e `RotaFinal` (comentários), `DestaqueDeEtapaTutorial` (novo), `RevelacaoDeBoatos` (removido).
- Editor: `TutorialRoteiroTool`, `Testes/FinaisEBoatosTests` (novo, 5 testes).
- Assets: `Prefab/UI.prefab`, `Prefab/SceneTransitonManager.prefab`, cenas `Jogo`, `Fase2`, `Fase3`, `Fase4` e `Tribunal` (diffs só com o necessário, sem ruído de layout).

### 9.5 Testes após as correções

EditMode **46/46**; validador "Campanha válida"; console sem erros; Play Mode dos itens de §9.2.

### 9.6 Ainda não coberto

Teclado e toque físicos, aparelho Android real, Continuar com save real depois das mudanças (nos testes a gravação do fim de fase foi interceptada), balanceamento das rotas com decisões reais de jogo (Prompt 8) e revisão dos textos provisórios.

### 9.7 Efeitos no ambiente de quem testou

- Um `save.json` de teste foi criado pelo fim da Fase 1 e apagado com autorização.
- As PlayerPrefs `dica_fato_boato_vista` e `dica_tempo_vista` ficaram marcadas no Editor; Novo Jogo ou Ferramentas > Tutorial > 3 zeram.
- A EditorPref `DaPena_SimularCelularNoEditor` ficou em falso (o padrão).

---

## 10. Prompt 5 — linha editorial e composição de resultados (26/09/2026)

### 10.1 Requisitos atendidos

| Requisito | Implementação | Estado |
|---|---|---|
| Escolha da linha editorial na prensa, a partir da Fase 2, depois das duas pistas; tutorial sem etapa extra | `CraftingPress.CombineItems` (Misturar, mesmo `onClick` do prefab) valida tudo e, se a receita exige linha, **não consome nada** e dispara `OnLinhaEditorialPedida(receita)`. A janela `LinhaEditorialDaPrensaUI` cobre o painel da prensa (as barras de Povo/Estado continuam visíveis) com as três opções e Cancelar; a escolha chama `ImprimirComLinhaEditorial(linha)`, que valida de novo antes de consumir. O tutorial usa a receita exata (`Recipe`) e nunca pede a escolha | validado (testes + Play Mode) |
| Três opções explícitas | `LinhaEditorial`: **Defesa do povo** (+Povo −Estado), **Agradar a Coroa / o Comitê** (+Estado −Povo; rótulo "Coroa" nas Fases 2–3, 1789–1792, e "Comitê" na Fase 4, 1793; a receita pode trocar o rótulo), **Sensacionalista** (+ouro, com a perda da revelação agravada em % configurável, sem sorteio) | validado |
| Modificadores por receita, valores provisórios, fallback neutro | `ReceitaDeCaso.linhaEditorial` (`ConfiguracaoEditorial`: `ativa`, três `ModificadorEditorial`, `rotuloAgradarOPoder`, `agravamentoPercentual`). Desligada = publicação **Neutra**, sem modificador (assets antigos). `LinhaEditorial.Neutra` é só de compatibilidade: nunca aparece na janela | validado |
| Casos novos exigem escolha explícita | Com a linha ligada, `CalculadoraDePanfleto.Calcular(..., Neutra)` devolve nulo e a prensa não imprime sem a escolha; o validador exige a linha ligada em todo caso da Fase 2 em diante, com a direção certa de cada modificador | validado |
| Cálculo único e testável | `CalculadoraDePanfleto.Calcular(receita, a, b, suportes, linha)`: versão pela confiabilidade real → qualificadores de apoio → linha editorial → resultado. O clamp 0–100 continua só em `GameManager.AplicarImpactoPanfleto`. `CalcularAntesDaLinha` expõe as etapas 1–2 para teste | validado |
| Tom/dedução/preço não mudam a verdade; prévia não detecta boato | A versão vem só de `ReceitaDeCaso.Classificar`. O modificador da linha é igual em todas as versões; a janela recebe só a receita e mostra a regra de cada opção (valores do modificador e "desmentido +50%"), nunca o resultado combinado | validado (teste da versão invariável; texto conferido em Play Mode) |
| Histórico da publicação com snapshot | `PanfletoPublicado` guarda pistas, apoios, qualificadores, nível, **linha**, valores calculados, aplicados (após o limite), **parte editorial** (`editorialPovo/Estado/Ouro`) e **penalidade agravada** (`agravamentoPovo/Estado`, já somado em `penalidadePovo/Estado`). Mudar a receita depois não altera o que foi publicado | validado |
| Penalidades no fluxo de revelações existente | A penalidade agravada entra em `GameManager.revelacoesPendentes` (com a linha) e é aplicada uma vez por `EncerrarFase`, antes da rota. O asset `Versao` não é alterado. A cutscene do `FimDeFase` acrescenta "O exagero da manchete fez o desmentido correr ainda mais depressa." às revelações de panfleto sensacionalista | validado (teste + Play Mode) |
| Guardas de transação | Cancelar, Esc, fechar o inventário ou trocar de cena só fecham a janela: nada consumido e nenhuma escolha guardada (a linha é parâmetro de cada impressão, não estado). Escolher fecha a janela antes de imprimir. Duplo clique: a publicação única do Prompt 1 recusa. Saída ocupada: recusada **antes** de pedir a linha, sem olhar a versão (a recusa não varia com a verdade das pistas). A seleção de apoio de outro caso continua descartada (`DescartarSelecaoDeOutroCaso`) | validado |
| Save antigo sem tom | Save **v6**: `PanfletoSalvo.linha/editorial*/agravamento*` e `RevelacaoSalva.linha`. Saves < 6 carregam como publicação **Neutra**, sem modificador nem agravamento inventados (mesmo se o JSON trouxer um campo `linha`) | validado (testes) |

### 10.2 Decisões tomadas nesta etapa

- **O que o sensacionalista agrava:** a perda da revelação que a versão já tem, em Com Boato, Calúnia e Só Alegações (a alegação do cliente é um boato não verificado). Fatos nunca recebe penalidade, com qualquer tom. Só perdas (valores negativos) crescem; arredondamento para longe de zero (−5 × 150% = −8).
- **Modificador igual em todas as versões:** se variasse com a versão, a janela viraria detector de boatos.
- **A janela mostra números do modificador**, que são informação conhecida e dão controle consciente das barras. Ela não mostra o resultado previsto.
- **Exemplo do Prompt 3:** 20/−10/30 → 40/−5/50 continua valendo como etapa "versão → apoio". O panfleto final soma a linha escolhida (ex.: Champ de Mars com a Ata e Defesa do povo = 50/−10/50). Em §6.6 e §9.1, os valores "+40/−5/+50" na HUD são de antes da linha editorial.
- **Mesa de casos:** a estimativa (`CaseData`) não inclui a linha, que só é escolhida na impressão.
- **Tribunal:** o destino do réu continua pela versão (Fatos = absolvido); o tom não interfere (conferido em Play Mode: Jornalista, Fatos + Sensacionalista → réu absolvido).

### 10.3 Arquivos

- Código novo: `Scripts/LinhaEditorial.cs` (enum, opções, rótulo por fase), `Scripts/LinhaEditorialDaPrensaUI.cs` (janela, montada por código na primeira vez).
- Código alterado: `ReceitaDeCaso.cs` (configuração editorial), `CalculadoraDePanfleto.cs` (etapa 3, `CalcularAntesDaLinha`, `Agravamento`), `CraftingPress.cs` (pedido da linha, `ImprimirComLinhaEditorial`, saída ocupada conferida antes), `GameManager.cs` (histórico e revelação com a linha), `SistemaDeSave.cs` (v6), `FimDeFase.cs` (texto do agravamento).
- Editor: `LinhaEditorialSetupTool.cs` (novo, **Ferramentas > Campanha > 4 - Aplicar linha editorial**), `ValidadorDaCampanha.cs` (checagens da linha editorial).
- Testes: `Testes/LinhaEditorialTests.cs` (novo, 8 testes), `ComponentesTests.cs` (+3 testes da prensa real). `BibliotecaTests` e `CampanhaTests` passaram a usar `CalcularAntesDaLinha` para os valores de antes da linha, e `CampanhaTests` confere o atalho de alegação nos três tons.
- Assets: as 10 `ReceitaDeCaso` das Fases 2–4 (só o bloco `linhaEditorial` acrescentado); `Prefab/UI.prefab` (`LinhaEditorialDaPrensaUI` no `PainelPrensa`, com os textos; o `FimDeFase` ganhou o campo novo serializado com o valor padrão). **Nenhuma cena alterada.** Nenhum GUID, `.meta` existente, pacote ou ProjectSettings alterado.
- Observação de git: os 10 `Assets/Casos/*.asset` aparecem como "M" no `git status` porque o Unity regravou os arquivos, mas o hash do blob é idêntico ao do HEAD e `git diff` sai vazio: não há mudança a commitar neles.

### 10.4 Configuração realizada

Ferramenta 4 executada via MCP: 10 receitas configuradas, 2 alterações no UI.prefab. Segunda execução: 0 receitas e 0 alterações (idempotente; só preenche receitas cuja linha nunca foi configurada, então valores editados no Inspector ficam). Ferramentas 1 e 3 reexecutadas: 0 alterações. Validador: **Campanha válida**, "Linha editorial: 10 receita(s) com as três opções", 0 avisos.

Valores provisórios (Povo / Estado / Ouro), iguais em todas as versões de cada receita:

| Fase | Defesa do povo | Agradar (rótulo) | Sensacionalista |
|---|---|---|---|
| 2 | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +15; perda da revelação +50% |
| 3 | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar a Coroa") | 0 / 0 / +20; perda +50% |
| 4 | +10 / −5 / 0 | −5 / +10 / 0 ("Agradar o Comitê") | 0 / 0 / +20; perda +50% |

### 10.5 Testes executados

**EditMode: 57/57** (46 anteriores + 11 novos). Console sem erros.
- `LinhaEditorialTests`: três tons × quatro versões (soma do modificador; versão e panfleto inalterados); sensacionalista agrava só a perda existente (Fatos sem penalidade, Com Boato −10/−5 → −15/−8, Calúnia −20/−10 → −30/−15, Só Alegações −5 → −8, asset intacto); caso novo sem escolha → nulo, receita antiga → neutra; rótulo Coroa/Comitê/personalizado sem mudar a regra; Champ de Mars real com apoio superior (40/−5/50 + linha, apoio uma vez, boato continua boato); histórico + revelação agravada aplicada uma vez no `EncerrarFase`; save v6 ida e volta; save v5 → neutra.
- `ComponentesTests` (prensa real): Misturar pede a linha sem consumir, repetir pede de novo, duplo clique na opção publica uma vez (ouro 40 + 15), revelação −15/−8; sem janela não imprime; saída ocupada recusa antes de pedir e depois imprime com Defesa (50→80 / 50→35); receita exata do tutorial imprime sem pedir linha.

**Play Mode (cena Fase2 aberta direto no Editor via MCP; estado montado por código; botões pelo `onClick`; teclado por eventos do Input System):**

| Cenário | Resultado |
|---|---|
| Caso das Joias, fato + boato, Misturar | janela aberta; entradas intactas, saída vazia, 0 publicações; foco em Cancelar; layout dentro dos 720 px do painel |
| Captura 1920×1080 | janela legível, barras visíveis. **Defeito encontrado e corrigido:** com alpha 0,97 os rótulos da prensa apareciam por baixo (espaço de cor Linear); o fundo passou a ser opaco |
| Cancelar → Misturar → Sensacionalista (duplo clique) | nada consumido ao cancelar; 1 publicação ComBoato 70/−20/50 (35 + 15 de ouro), aplicado +35 (limite 100), penalidade −45/−8 (agravamento −15/−3), caso concluído |
| Fim da fase | cutscene com o texto da revelação + frase do agravamento; Povo 100→55, Estado 20→12, aplicada uma vez |
| Fase 4, rota A, Jornalista | rótulo "Agradar o Comitê"; campo Suporte visível ao lado; fechar o inventário com a janela aberta fecha a janela sem consumir; Fatos + Sensacionalista = 20/−20/35, sem penalidade nem revelação; réu absolvido |
| Teclado (Negociante, rota B montada por código) | Esc fecha sem consumir e sem abrir o pause; tecla 2 publica "Agradar o Comitê" (−15/30/60 → −20/40/60) |

**Não testado em runtime:** toque e teclado físicos, aparelho Android, a prensa aberta pelo alçapão/`PressInteractable` no Porão (o painel foi aberto por código na Fase2; é o mesmo prefab), tela 4:3 (o painel da prensa já saía da tela antes, §6.4).

### 10.6 Limitações e pendências

- **Balanceamento (Prompt 8):** Defesa/Agradar deslocam o desnível Povo × Estado em 15 por publicação; com 3 publicações antes da rota, o tom sozinho move até 45 pontos. Isso dá controle consciente da rota (a B ficou alcançável com "Agradar"), mas os valores são provisórios.
- **Moldura:** a janela cobre o painel da prensa inteiro, inclusive a borda dourada.
- **Texto do agravamento:** a frase entra em toda revelação de panfleto sensacionalista. O validador exige agravamento > 0, então a frase nunca aparece sem efeito.
- **Dica de primeira vez para o tom:** fica para o Prompt 7. A janela já descreve cada opção.

### 10.7 Conteúdo provisório

Valores da tabela de §10.4, rótulos "Agradar a Coroa"/"Agradar o Comitê", textos da janela (título, pergunta, três descrições, nota "O tom muda o panfleto, não a verdade das pistas.") e a frase do agravamento no `FimDeFase`. Todos editáveis no Inspector (receitas, `LinhaEditorialDaPrensaUI` e `FimDeFase` no UI.prefab).

### 10.8 Efeitos no ambiente de quem testou

Não existia `save.json` e nenhum foi criado. As PlayerPrefs de progresso ficaram iguais às de antes (as cinco chaves já estavam marcadas). O tamanho da Game View foi trocado para 1920×1080 só durante a captura e voltou para "16:9 Landscape". `Application.runInBackground` só foi ligado durante a sessão de Play. O Editor voltou para a cena Jogo, sem alterações.

### 10.9 Como revalidar

1. **Ferramentas > Campanha > 4 - Aplicar linha editorial** duas vezes → "Receitas configuradas: 0; … Alterações no UI.prefab: 0". **Ferramentas > Campanha > 2** → "Campanha válida".
2. Test Runner > EditMode > Run All → 57/57.
3. Manual: Novo Jogo → tutorial (a prensa imprime direto, sem janela) → caso da Fase 2 → duas pistas na prensa → Misturar → conferir as três opções e Cancelar (nada gasto) → Misturar → Sensacionalista → HUD com o ouro extra → voltar ao escritório: se havia boato, a cutscene cita o exagero e a perda é maior. Na Fase 4 a opção 2 aparece como "Agradar o Comitê".

---

## 11. Correção dos fades de transição (26/09/2026)

Relato do grupo: "os fades de transição entre cenas não funcionam". Duas causas, as duas corrigidas e medidas em Play Mode começando pelo menu (como o jogador).

| # | Causa | Correção |
|---|---|---|
| 1 | Na cena `menu principal`, o objeto "Canvas" do `SceneTransitonManager` estava **desligado** (override salvo na cena). Como a cópia da primeira cena é a que sobrevive entre cenas, nenhum fade aparecia no jogo inteiro. Nos testes que começavam pela cena Jogo o fade funcionava, por isso passou na verificação de §9. O motivo provável do desligamento: o prefab tinha o CanvasGroup em Alpha 1, cobrindo a Game View no Editor | override removido da cena do menu; prefab com Alpha 0 e Blocks Raycasts desligado (invisível no Editor); `SceneTransitionManager.Awake` liga o objeto do fade sempre |
| 2 | O primeiro frame depois de carregar uma cena é pesado. O fade contava o tempo real, então um frame lento consumia o meio segundo inteiro e a tela ia de clara a escura de uma vez | cada frame avança no máximo 1/30 s do fade; o clareamento começa um frame depois da cena carregar; uma transição que interrompe outra parte do alpha atual |

Medição (frames com o fade entre 1% e 99%): menu → Jogo, 146 escurecendo e 151 clareando; Jogo → Porão (alçapão) e Porão → Jogo (porta), cerca de 150 por fade. Antes: 0 a 1 frame. Arquivos: `Scripts/SceneTransitionManager.cs`, `Prefab/SceneTransitonManager.prefab`, `Scenes/menu principal.unity` (só o override removido). EditMode 57/57. O save do jogador não foi alterado.
