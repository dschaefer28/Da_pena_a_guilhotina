# Status de implementação — Da Pena à Guilhotina

Atualizado em 27/09/2026 (Prompt 4, restante do Prompt 6 e Fase 3 com três casos, §13 a §16; **Prompt 7, 6h nas Fases 2–4 e dedução desde a Fase 2, §17**). Referência: `docs/PROMPTS_CLAUDE_CODE_TCC.md`. Caminhos relativos a `Unity-DaPenaAGuilhotina/`.

Estados usados: **validado** (testado em runtime ou em teste automatizado), **existente não validado** (código/asset presente, sem teste de runtime), **parcial**, **ausente**.

Etapas: Prompts 0 a 3 concluídos; verificação dos Prompts 0–3 com os achados corrigidos (§7); verificação completa contra os dois PDFs em 26/09, com correções que adiantaram partes dos Prompts 6 e 7 (§9); Prompt 5 (linha editorial) concluído em 26/09 (§10); **Prompt 4 (dedução ativa) incluído por decisão do grupo e concluído em 27/09 (§13)**; **restante do Prompt 6 concluído em 27/09 (§14)**; **Fase 3 reduzida a três casos, com um concluído (§15)**; **Prompt 7 concluído em 27/09 (§17), junto com dois ajustes pedidos pelo grupo: 6h de investigação nas Fases 2, 3 e 4 e dedução ativa desde a Fase 2**. Próximo: **Prompt 8 (não iniciado)**, depois dos testes manuais listados em §17.9.

Regras que mudaram nas verificações e prevalecem sobre o texto das seções 4–6:
- **Alegações:** qualquer impressão com alegação usa a versão Só Alegações.
- **Arquivamento:** sobras dos casos anteriores à Fase 4 são arquivadas ao concluir.
- **Inventário cheio:** interação sem espaço para o que entregaria não cobra horas.
- **Save:** versão 6 no Prompt 5 (linha editorial); versão 7 em 27/09 (dedução ativa, §13).
- **Biblioteca:** tem estados por situação.
- **Pistas:** os nomes são neutros.
- **Revelações (26/09):** aplicadas no fim da fase por `GameManager.EncerrarFase`, na ordem revelações → despesas → avanço de fase/rota, e mostradas na cutscene do `FimDeFase`. `RevelacaoDeBoatos` foi removido.
- **Tribunal (26/09):** as provas são os itens do caso da Fase 4 obtidos (histórico, inclusive os gastos na prensa) mais o panfleto dele. O réu é absolvido só se esse panfleto saiu na versão Fatos; o destino do jogador continua vindo só da rota.
- **Final C (26/09):** "O Esquecido", definido pelo grupo: barras equilibradas, ninguém condena nem defende o jogador, que é apagado da história.
- **Tutorial (26/09):** a explicação do status aparece ao abrir a prensa, antes de imprimir.
- **Linha editorial (Prompt 5):** a partir da Fase 2, o Misturar pede Defesa do povo / Agradar a Coroa (o Comitê na Fase 4) / Sensacionalista antes de consumir as pistas. Cálculo: versão → apoio → linha. O exemplo do Prompt 3 (40/−5/50) é o valor antes da linha.
- **Fala depois da entrega (27/09):** `CasoReacao.dialogoDepoisDaEntrega`. Quando a reação já entregou tudo, o NPC diz essa fala curta em vez de repetir a conversa inteira (Dupaty, Marie, Joalheiro). Vazio = comportamento anterior.
- **Caso das Joias (27/09):** o joalheiro entrega os dois fatos; a Mesa da Taverna ficou vazia nesse caso; caminho mínimo de 1h (§12.3).
- **Dedução ativa (27/09, §13; desde a Fase 2 pelo §17):** nos **nove** casos das Fases 2, 3 e 4, dois pares de afirmações contraditórias (fato do cliente × boato; fato do objeto × calúnia). O "Quadro de pistas" (botão na HUD, já na Fase 2) guarda as hipóteses do jogador; "Conferir dedução" só confirma o quadro inteiro, com uma resposta única para qualquer falha. Não bloqueia a publicação. Nesses casos a verdade não aparece mais sozinha (Verificada Por é ignorada).
- **Tempo (27/09, §17):** 6h por caso nas Fases 2, 3 e 4 (`GameManager.horasPorCaso`); a conversão continua 1h por NPC/objeto novo. Endividado: 5h. O quadro inteiro custa até 4h, então a dedução é alcançável mesmo com dívida.
- **Dicas de primeira vez (27/09, §17):** relógio, fato/boato (agora aponta o Quadro de pistas), dedução, despesas, Biblioteca e linha editorial. Ficam numa fila (não se perdem nem se sobrepõem), vão no save e zeram no Novo Jogo.
- **Marie e Dupaty depois do tutorial (27/09, §17):** pela regra de progresso (tutorial concluído), não mais pelo panfleto no inventário; vale também depois de carregar um save.
- **Mesa de Casos (27/09, §17):** é janela modal (trava movimento, interação, inventário e pause; Esc fecha), cabe na tela em qualquer proporção e fica acima dos controles de toque.
- **UI em telas estreitas (27/09, §17):** o canvas da UI passou a "Expand" (16:9 e celulares iguais; 16:10 e 4:3 sem cortar a prensa).
- **Fase 3 (27/09, §15):** três casos (saiu "O Padeiro de Notre-Dame"); o jogador escolhe e conclui um, os outros ficam bloqueados; concluído o caso, a volta ao escritório abre a Fase 4. `casosPorFase` = {1, 1, 1, 1}.
- **Mesa e porta do escritório (27/09, §14):** abrem pela conclusão do tutorial, não mais pelo panfleto do tutorial no inventário.
- **Fase 4 (27/09, §14):** a mesa mostra e aceita só o caso da rota travada; um caso sem rota nunca aparece nessa fase.
- **Dupaty (27/09, §14):** libera o tribunal só com o caso da rota concluído, a publicação registrada com panfleto existente, as pistas impressas no histórico e as evidências extras que forem configuradas.
- **Inventário cheio ao restaurar (27/09, §14):** o que não cabe fica guardado (`GameManager.itensForaDaGrade`), vai junto em cada troca de cena e no save, e volta para a grade quando houver espaço.
- **Tribunal (27/09, §14):** "Encerrar a defesa" exige duas provas apresentadas (configurável, limitado ao total); o veredito é sempre o completo.
- **Rota (27/09, §14):** margem mínima 1; com a margem inválida o empate continua na rota C.

Documento de autor com a matriz dos casos: `docs/MATRIZ_DE_CASOS_TCC.md` (não exibir ao jogador).

---

## 1. Ambiente

| Item | Situação |
|---|---|
| Unity | 6000.3.9f1 aberto no Editor durante o trabalho (projeto `Unity-DaPenaAGuilhotina`) |
| Unity MCP | Operacional (`com.coplaydev.unity-mcp`): leitura de cenas, compilação, Play Mode, execução de código de Editor e Test Runner |
| Testes | `com.unity.test-framework` 1.6.0. Não havia testes. Os testes novos ficam em `Assets/Editor/Testes/` (assembly `Assembly-CSharp-Editor`, sem asmdef, porque os scripts do jogo não têm asmdef). Em 26/09: 46 testes EditMode; 57 depois do Prompt 5; 58 em 27/09 (§12.2); 78 depois dos Prompts 4 e 6 (§16); 88 depois do Prompt 7 (§17) |
| Compilação | Sem erros. Avisos antigos: `FindObjectOfType` obsoleto em `GameManager`; `Caso_Tutorial` sem `caseTitle`/`npcDialogueRoute` (OnValidate) |
| Save real do jogador | Até 25/09: não foi lido nem alterado. Em 26/09: ver §9.7 (um save de teste criado e apagado com autorização; duas PlayerPrefs de dica ficaram marcadas) |

## 2. Premissas adotadas (seção "Decisões adotadas" do documento de prompts)

1. Finais: A = Guilhotina, B = Tirano, C = Equilíbrio (código atual já segue).
2. Tribunal: investigar → panfleto → Dupaty → tribunal.
3. "Final da Fase 2" citado na Fase 3 = erro de numeração.
4. ~~Final C "O Exílio"~~ Substituída em 26/09: Final C "O Esquecido", definido pelo grupo (ver §9.3). Textos ainda provisórios.
5. Dedução ativa (C) nas Fases 3 e 4, por caso; sem confronto com NPC na 1ª versão. **Desde 27/09 (decisão do grupo, §17), também na Fase 2.**
6. Linha editorial (D) a partir da Fase 2; tutorial com receita simples; tom não muda a verdade.
7. Receita expandida mantém 2 slots; evidências complementares como qualificadores opcionais.
8. Campanha: Fase 2 = 1 de 3 casos; ~~Fase 3 = 2 distintos de 4~~ Fase 3 = 1 de 3 (decisão do grupo, 27/09, §15); Fase 4 = 1 caso da rota.
9. Sem bloqueio por falta de pistas (duas alegações não verificadas como saída de design, se necessário).
10. Valores e textos novos são protótipos editáveis, rotulados como provisórios.
11. (26/09, decisão do grupo) Pista melhor gera panfleto melhor; as barras decidem o final do jogador; o panfleto do caso da Fase 4 decide o destino do réu.
12. (27/09, decisão do grupo) Cada caso das Fases 2, 3 e 4 tem 6h no relógio do jogo (1h por NPC/objeto novo, como antes).

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

~~Prompt 5 — linha editorial (D)~~ Concluído em 26/09 (§10). ~~Prompt 4 — dedução ativa~~ Concluído em 27/09 (§13). ~~Restante do Prompt 6~~ Concluído em 27/09 (§14). ~~Prompt 7 — tutorial, transições e UI integrada~~ Concluído em 27/09 (§17).

**Próximo: Prompt 8 (validação final), não iniciado.** Antes dele, os testes manuais de §17.9 (teclado/toque físicos, aparelho Android, leitura dos textos novos). Pontos de atenção:
- **Prompt 8 (balanceamento):** com um caso só na Fase 3, a rota é decidida por três publicações (tutorial, Fase 2, Fase 3); a linha editorial desloca o desnível em até 15 pontos por publicação. Exemplos de percurso em `MATRIZ_DE_CASOS_TCC.md`.
- **Prompt 8 (tempo):** com 6h e caminho mínimo de 1–2h, sobra folga em todos os casos; vale medir, jogando, se a pressão do relógio ainda existe (antes: 4h).

Questões em aberto:
- ~~Revelações: onde colocar `RevelacaoDeBoatos` e a ordem em relação à trava de rota~~ Resolvido em 26/09 (§9).
- ~~Tribunal: provas restritas ao caso, incluindo documentos comprados~~ Resolvido em 26/09 (§9).
- ~~Inventário ao restaurar: perda silenciosa de itens quando a grade está cheia~~ Resolvido em 27/09 (§14.2).
- ~~Mesa depende do panfleto do tutorial~~ Resolvido em 27/09 (§14.3); a porta do escritório tinha a mesma trava e também passou para a regra de progresso.
- ~~Marie ausente após save/load~~ Resolvido em 27/09 (§17.2).
- ~~Referências quebradas: "pensamentos" do alçapão com falas do Operário~~ Resolvido em 27/09 (§17.2). As outras de 3.2, item 5 (`UI_Inventory.prefab` `recipes[0]`, `CameraConfiner` na Porao, `TypeTextAnimation.dialogueSystem`) continuam sem efeito visível e não foram mexidas.
- ~~Horas da dedução (§13.6)~~ Resolvido em 27/09 com 6h por caso (§17): o quadro inteiro (até 4h) cabe mesmo endividado (5h).

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

---

## 12. Diálogos narrativos e fala depois da entrega (26–27/09/2026)

Roteiros escritos pelo grupo (aprovados antes da implementação). Nenhum sistema de diálogo novo: `DialogueData`, `NPCMovement.reacoesDeCaso` e a entrega no fim da conversa (`OnDialogueEnded`) continuam os mesmos.

### 12.1 Caso 1 (tutorial): Marie Bradier e Charles Dupaty — commit `796fe1e`

- Três conversas: orientação de Dupaty (sem item), Marie com três abordagens (investigativa, empática, jurídica) que convergem, e Dupaty depois do relato, também com três abordagens e convergência. 26 `DialogueData` em `Dialogos/Marie Bradier` e `Dialogos/Charles Dupaty` (7 reaproveitados, 19 novos).
- `Jogo.unity`: a reação de Marie ao `Caso_Tutorial` começa em `MarieDialogueData` (antes pulava a primeira fala). `Evidencia_RelatoBradier` passou a se chamar **"Relato de Bradier"** (itemID e GUID iguais).
- Recompensas, eventos (Dupaty habilita Marie; Marie reabilita Dupaty), receita `ReceitaFase1` (+15/−10/+20) e ordem do tutorial inalterados.
- Validado em Play Mode (cena Jogo, botões reais do diálogo acionados por código): os 12 caminhos (6 de Marie, 6 de Dupaty), falas iguais ao roteiro na tela, relato e decreto entregues uma vez cada, tutorial na ordem certa, controle devolvido ao jogador, impressão no porão.

### 12.2 Fala depois da entrega (`CasoReacao.dialogoDepoisDaEntrega`)

- **Problema relatado pelo grupo:** depois de entregar o decreto, Dupaty repetia a conversa inteira, com as opções.
- **Correção:** campo opcional novo em `CasoReacao` (`NPCMovement.cs`). Se todas as recompensas da reação já foram entregues (pelo registro, que sobrevive à troca de cena), o NPC toca essa fala curta, sem opções e sem entregar nada. Uma etapa complementar pendente continua tendo prioridade. Campo vazio = comportamento anterior (nenhum NPC das Fases 2–4 mudou, exceto o joalheiro, §12.3).
- **Configurado:** Dupaty → `Dupat_Lembrete_Porao` ("Você já tem o relato de Marie e o decreto. Desça ao porão e escreva o memorial na prensa."); Marie → `Marie_Lembrete` ("Já lhe contei o que sei. Leve meu relato ao seu mestre, por favor."); Joalheiro → §12.3.
- **Marie:** `canInteract` e `BlockFutureEvents` não são persistidos. Voltando do porão sem imprimir, a conversa com Dupaty reativa Marie (evento que já existia). Antes ela repetia a conversa inteira (sem duplicar o relato); agora diz só o lembrete e volta a se desligar. Depois da impressão ela some (`GerenciadorCena1`), como antes.
- Teste: `ComponentesTests.Npc_DepoisDaEntrega_TocaFalaCurta_SemEntregarDeNovo_CampoVazioRepeteAConversa`.

### 12.3 O colar da rainha (Caso das Joias)

- **Fontes:** roteiro do grupo, com o GDD como referência principal (trechos da Fase 2 sobre o joalheiro conferidos nesta etapa: "Registro de compra", Duque de Orleans). O episódio histórico do colar (1785–1786) aparece em 1789 por adaptação do jogo; as falas são dramatizações. A planilha "30 Casos Reais" estava acessível, mas não foi lida.
- **Diálogo:** J0 (9 falas, joalheiro e Julien; as duas opções na última) → J1, pressão financeira, ou J2, limites da prova (3 falas cada, fim normal). Fala depois da entrega: "Já lhe dei as cópias. Tudo o que vi está no registro." Sem áudio, como antes.
  - Assets reaproveitados: J0 `Joalheiro_Dialogo`; J1 `respostaJoalheiro3`; J2 `respostaJoalheiro2`; fala depois da entrega `respostaJoalheiro1`. `repostaJoalheiro` ficou sem uso (não foi apagado). `npcDialogueRoute` continua em `Joalheiro_Dialogo`.
- **Pistas (as duas Fato, do caso, mesmo sprite):** `pista_joias_1` **"Registro de compra"** (fonte "Livro de vendas do joalheiro.") e `pista_joias_2` **"Assinatura da encomenda"** (fonte "Cópia da encomenda fornecida pelo joalheiro."), com as descrições do roteiro. Apresentadas em J0, entregues juntas no fim de J1 ou J2.
- **Origem da 2ª pista:** saiu da Mesa da Taverna (só a entrada do Caso das Joias; a do Operário ficou). No joalheiro, a lista efetiva de recompensas passou de 1 para 2 itens (o override antigo do índice 1 já apontava para a assinatura, mas não valia porque a lista tinha tamanho 1).
- **Caso:** nova `caseDescription`; título "Caso das Joias", receitas, alegações, custos e valores inalterados.
- **`CampanhaSetupTool`:** a definição da Mesa não traz mais a pista das Joias (senão a ferramenta a devolveria ao ser reexecutada) e os textos de referência das duas pistas foram atualizados.
- **Consequência de design:** as Joias ficam com 3 fontes com pista, 4 oportunidades vazias e caminho mínimo de **1h** (os demais casos: 2h). Matriz atualizada.
- **Saves antigos:** o jogo só salva no fim do tutorial, no fim de fase e na ida ao tribunal, nunca no meio de um caso; nenhum save pode ter as Joias em andamento com a Mesa já vasculhada (o save do grupo em 27/09 está no início da Fase 2, sem as Joias). Se um ponto de save no meio de caso for criado, quem já tivesse pego a assinatura na Mesa receberia uma 2ª cópia do joalheiro (o registro é por interação). Nenhuma migração foi feita.

### 12.4 Arquivos

- Código: `Scripts/NPCMovement.cs` (campo e regra), `Editor/CampanhaSetupTool.cs` (Mesa e textos das Joias), `Editor/Testes/ComponentesTests.cs` (+1 teste).
- Assets: 26 diálogos do tutorial (§12.1), `Dupat_Lembrete_Porao`, `Marie_Lembrete` (novos); `Joalheiro_Dialogo`, `respostaJoalheiro1/2/3`, `Item_joalheiro1/2`, `Caso_Joalheiro`, `Evidencia_RelatoBradier`.
- Cenas (edições pontuais, sem ruído de layout do `UI.prefab`): `Jogo` (entrada da Marie, lembretes de Dupaty e Marie); `Fase2` (2 recompensas e lembrete no joalheiro; entrada das Joias removida da Mesa).
- Nenhum prefab, GUID ou `.meta` existente, pacote ou ProjectSettings alterado.

### 12.5 Verificação (27/09)

- **Estática:** validador "Campanha válida" (Joias: 7 oportunidades, 3 com pista, 1h); Ferramentas > Campanha > 1 reexecutada: **0 alterações** (a Mesa não recebe a pista de volta); EditMode **58/58**; grafos dos diálogos sem referência nula, sem ciclo, com opções só na última fala; valores efetivos da cena lidos pela Unity (só o joalheiro entrega `pista_joias_1/2`).
- **Play Mode** (Editor via MCP; Fase2 aberta direto, caso montado por código; botões reais do diálogo e da prensa acionados por `onClick`):

| Cenário | Resultado |
|---|---|
| J0 → J1 | 9 + 3 falas iguais ao roteiro (falantes e botões corretos); 1 de cada pista; −1h |
| Falar de novo | só a fala depois da entrega; sem hora nem pista a mais |
| Mesa da Taverna (Joias) | busca vazia (−1h); a 2ª busca não cobra; nenhuma pista |
| J0 → J2 (sessão nova) | 3 falas; 1 de cada pista |
| Inventário sem espaço | a conversa não abre e não cobra |
| 1 espaço livre | entra o Registro; a Assinatura fica pendente com aviso |
| Pendente sem espaço / com espaço | não abre / conversa completa de novo, entrega a Assinatura sem nova hora; depois só a fala curta |
| Prensa (Registro + Assinatura, Defesa do povo) | janela da linha editorial sem consumir; versão Fatos 60/−15/20; caso concluído pela impressão |
| Marie: relato → decreto → porão e volta sem imprimir → Dupaty → Marie | Dupaty diz o lembrete; Marie diz só o lembrete; relato x1 |

Console sem erros nem avisos (inclusive de caractere ausente para ‘ ’ e …).

**Não testado:** teclado/toque físicos, Android, áudio, aparência na tela (conferida pelo texto dos componentes TMP, não por captura).

### 12.6 Efeitos no ambiente

`save.json` e PlayerPrefs iguais aos de antes (hash conferido). `Application.runInBackground` só ligado durante as sessões de Play. A Ferramenta 1 regravou 19 assets de casos/receitas com conteúdo idêntico (só final de linha); foram restaurados pelo git. `LiberationSans SDF - Fallback.asset` é um atlas dinâmico que a Unity atualiza ao desenhar caracteres novos; já estava modificado antes e não faz parte desta mudança.

---

## 13. Prompt 4 — dedução ativa (27/09/2026)

Incluído por decisão do grupo (27/09). Vale para os seis casos das Fases 3 e 4; os casos da Fase 2 seguem a regra antiga.

### 13.1 Requisitos atendidos

| Requisito | Implementação | Estado |
|---|---|---|
| Conjunto de pistas e pares contraditórios por caso; exatamente uma afirmação verdadeira por par; validar duplicação, referência cruzada e par sem solução | `CaseData.deducao` (`ConfiguracaoDeDeducao`: `ativa` + lista de `ParContraditorio`). O conjunto é a união dos pares. `Deducao.Problemas` acusa par incompleto, pista repetida (no par ou entre pares), pista de outro caso, alegação ou documento de apoio no par e par sem exatamente um Fato. O validador exige dedução em todo caso das Fases 3 e 4 | validado (testes + validador) |
| Verdade interna separada da marcação do jogador (Não marcada / Confiável / Duvidosa); marcar não muda a pista nem a receita | `MarcacaoDaPista`, guardada em `GameManager.marcacoesDeDeducao` por caso + itemID. `Item.confiabilidade` e `ReceitaDeCaso.Classificar` não mudam | validado (teste) |
| Quadro com pistas conhecidas, fonte, descrição, contradições descobertas e marcações; sem revelar pista não descoberta nem o gabarito | `QuadroDeDeducaoUI` (botão "Quadro de pistas" na HUD, abaixo da Biblioteca, nas Fases 3 e 4 com o caso em andamento). Lista só as pistas do conjunto já obtidas (`evidenciasObtidas`, inclusive as gastas na prensa), agrupadas por par **na ordem em que o jogador as descobriu**. "Contradiz: X" só aparece quando as duas afirmações do par foram achadas; senão, "A afirmação contrária ainda não foi encontrada". Contador "Contradições encontradas: n de 2" | validado (testes + Play Mode) |
| "Conferir dedução" confirma só o conjunto inteiro, descoberto e bem marcado; parcial e errada recebem resposta genérica, sem dizer o que acertou nem quantas | `Deducao.Conferir`: percorre o conjunto todo e devolve `NaoSeSustenta` para pista faltando, marca faltando ou marca errada (a mesma mensagem na tela). Confirmado, as marcas travam | validado (testes + Play Mode com os botões reais) |
| Publicar sem dedução; sem custo por tentativa; sem recurso de confiança novo | A prensa não consulta o quadro; conferir não custa nada | validado (teste com a prensa real) |
| Remover a revelação automática (Verificada Por) nos casos aderentes; manter o legado nos outros | `InventoryManager.PistaVerificada`/`RotuloDaPista`/`RegistrarVerificacoes` e `FichaDaPista`: caso aderente → a situação vem do quadro (antes da confirmação, só "não verificada" + a hipótese do jogador; depois, "confirmada"/"desmentida"); nenhum aviso automático. Casos da Fase 2: regra antiga | validado (teste) |
| Consequências depois da publicação continuam | `revelacoesPendentes` e `EncerrarFase` não mudaram | validado (teste) |
| Biblioteca dá contexto, sem botão que entregue a resposta; conteúdo do Prompt 2 coerente | Descrições dos seis documentos da biblioteca reescritas para dar contexto a um dos pares (§13.2) | configurado |
| Histórico, marcações e confirmação no save; saves antigos sem hipóteses inventadas; verificação antiga ≠ dedução confirmada | Save **v7**: `marcacoesDeDeducao`, `deducoesConfirmadas`. Saves < 7 carregam sem nenhuma hipótese nem confirmação (mesmo que o JSON traga os campos); `pistasVerificadas` antigas continuam valendo só nos casos sem dedução | validado (testes) |

### 13.2 Conteúdo reescrito (provisório)

Em cada caso: **par 1** = fato do cliente × boato de outro NPC; **par 2** = fato de um objeto × calúnia de outro objeto. As afirmações de cada par tratam do mesmo assunto e não podem ser verdadeiras juntas (ex.: Varennes — o contrato pago "em moeda francesa, sem dizer quem iria" × o boato do "ouro austríaco" e de que o cocheiro "sabia desde Paris"; o mestre de posta que "galopou até Varennes num cavalo da posta" × a calúnia de que o cocheiro "envenenou todos os cavalos da posta"). Textos completos na matriz.

- **Pistas que levam a outras:** o cliente cita quem espalha o boato, e quem espalha o boato cita os dois objetos (o do fato e o da calúnia). Seguindo essas pistas, as quatro afirmações saem em 4h; os três lugares restantes continuam sem nada.
- **Biblioteca:** cada documento dá contexto a um par (ex.: a Ata da prefeitura diz que a bandeira vermelha foi hasteada antes de a Guarda sair). O documento não aparece no quadro e não confirma nada sozinho.
- **Nomes:** continuam neutros (o assunto de cada afirmação). Renomeados: "Os cavalos da posta", "A hora da bandeira", "A história do roubo", "A prensa de Morel", "O número de outubro", "O certificado recusado", "As vendas de Garnier", "O plano de fuga", "A acusação do vizinho".
- **Migração:** Ferramentas > Campanha > 5 só troca um texto que ainda está igual ao de 25/09; um texto editado no Inspector é mantido e listado no relatório. Os textos novos ficam em `CampanhaSetupTool.TextosRevisados` e nas definições de reações, e em `BibliotecaSetupTool` (documentos e a fala da etapa do Cocheiro).

### 13.3 Arquivos

- Código novo: `Scripts/DeducaoAtiva.cs` (`MarcacaoDaPista`, `ParContraditorio`, `ConfiguracaoDeDeducao`, `MarcacaoDeDeducao`, regras em `Deducao`), `Scripts/QuadroDeDeducaoUI.cs` (janela montada por código, como a Biblioteca), `Scripts/JanelasModais.cs` (trava comum da Biblioteca e do quadro).
- Código alterado: `@CaseData.cs` (`deducao`), `GameManager.cs` (listas da dedução), `InventoryManager.cs` e `FichaDaPista.cs` (etiquetas e fim da revelação automática nos casos aderentes), `SistemaDeSave.cs` (v7), `BibliotecaUI.cs`, `PauseMenu.cs`, `PlayerInteraction.cs`, `PlayerMove.cs` (o quadro bloqueia pause, interação, inventário e movimento como a Biblioteca).
- Editor: `DeducaoSetupTool.cs` (**Ferramentas > Campanha > 5 - Aplicar dedução ativa**), `ValidadorDaCampanha.cs` (dedução e custo em horas do quadro inteiro), `CampanhaSetupTool.cs` e `BibliotecaSetupTool.cs` (textos de referência).
- Testes: `Testes/DeducaoTests.cs` (10), `ComponentesTests.cs` (+1: publicar sem conferir, com a prensa real).
- Assets: 6 casos das Fases 3 e 4 (pares), 19 pistas, 13 diálogos, 6 documentos de apoio e 6 ofertas; `UI.prefab` (objeto `QuadroDeDeducao`).

### 13.4 Configuração realizada

Ferramenta 5 executada via MCP: 6 casos configurados, 19 pistas, 13 falas e 12 descrições reescritas, 2 alterações no `UI.prefab`; nenhum texto mantido por edição manual. Segunda execução: 0 alterações. Ferramentas 1, 3 e 4 reexecutadas: 0 alterações.

### 13.5 Testes

- **EditMode (`DeducaoTests`):** pares coerentes e incoerências acusadas; aquisição não revela o gabarito, Verificada Por e verificações antigas não contam, legado da Fase 2 preservado; incompleta, parcial e errada dão a mesma resposta; conjunto certo confirma, mostra a verdade e trava as marcas; marcar não muda a verdade nem a versão; só o caso em andamento e só pistas descobertas do conjunto; contradição só com as duas pistas e ordem pela descoberta; novo caso não herda marcações; save v7 ida e volta e save v6 sem hipóteses inventadas; os seis casos reais com dois pares coerentes.
- **Play Mode (Fase3 aberta direto, Varennes, estado montado por código, botões reais via `onClick`):** caso aceito (4h, alegações entregues) → botão do quadro visível e escondido durante o diálogo → Cocheiro (fala nova com a pista para a peticionária) → quadro com 1 pista e "0 de 2" → Peticionária, Mural e Caixa de Tipos: 4 pistas em 4h, "2 de 2" → marcação errada e marcação parcial: mesma resposta genérica → marcação certa: "A dedução se sustenta", marcas e botão travados → inventário com "confirmada"/"desmentida" → com o quadro aberto: jogo pausado, sem interação, inventário e biblioteca não abrem, pause bloqueado; ao fechar tudo volta.
- **Visual:** capturas em 1920×1080 e 800×480. Corrigidos dois defeitos de layout achados nas capturas: botões de marca espremidos em tela estreita (largura mínima) e cabeçalho/rodapé esticados (não expandem mais na altura).

### 13.6 Limitações e decisões para o grupo

- **Horas:** descobrir o quadro inteiro custa 4h (as quatro fontes). Com o orçamento normal (4h) só dá seguindo as pistas sem errar nenhum lugar; **endividado (3h), a conferência fica impossível naquele caso** (publicar continua possível). Se quiserem a dedução sempre alcançável, a saída é dar 5h aos casos das Fases 3 e 4 (`CaseData.horasDeInvestigacao`); não mudei sem a decisão de vocês.
- **Busca exaustiva:** a conferência conjunta reduz a informação por tentativa, mas não impede tentar todas as combinações: com dois pares há só quatro coerentes, e conferir é grátis. Nenhuma proteção além disso foi implementada.
- **Ordem no quadro:** pela ordem de descoberta (escolha do jogador). A ordem alfabética foi descartada porque, por coincidência dos nomes, punha o fato primeiro nos dois pares de Varennes.
- **Depois de confirmar:** as pistas mostram "confirmada" (fato) e "desmentida" (boato ou calúnia, sem dizer qual).
- **O quadro só aparece com o caso em andamento;** depois de publicar, some.
- **Dica de fato/boato (Prompt 7):** o texto atual ainda fala em "procurar outra fonte que confirme".
- **Não testado:** teclado e toque físicos (o Esc usa o mesmo caminho da Biblioteca), aparelho Android.

### 13.7 Conteúdo provisório

Textos do quadro (`QuadroDeDeducaoUI`, editáveis no Inspector do `UI.prefab`), as 19 pistas, as 13 falas e as 6 descrições reescritas.

---

## 14. Prompt 6 — o que restava (27/09/2026)

### 14.1 Requisitos atendidos

| Requisito | Implementação | Estado |
|---|---|---|
| Dupaty só libera o tribunal com o caso correto concluído, panfleto recuperável e evidências exigidas; histórico comprova as pistas gastas | `CheckpointDoTribunal.Avaliar`: caso da Fase 4 **da rota travada** concluído → publicação registrada e panfleto da versão existente na receita → as duas pistas impressas no histórico de evidências (publicações `legado`, de saves antigos, não são barradas) → evidências extras configuráveis por caso (`evidenciasExigidas`, vazio por padrão). Falas próprias para "sem panfleto" e "falta prova" | validado (teste + Play Mode) |
| Nunca perder item ao trocar de cena | Ao restaurar com a grade cheia, o que não cabe fica em `GameManager.itensForaDaGrade` (aviso na tela), entra em `SalvarEstadoAtual`, no save e na próxima restauração, e volta para a grade quando o arquivamento libera espaço. A saída da prensa já era salva na troca de cena | validado (teste) |
| Mesa sem depender do panfleto do tutorial | `TableInteractable.exigeTutorialConcluido` (padrão ligado) usa `GameManager.TutorialConcluido` (caso da Fase 1 concluído ou fase > 1). A **porta do escritório** tinha a mesma dependência (herdada do `Porta.prefab`): ganhou `DoorInteractable.exigeTutorialConcluido`, ligado só nela; o prefab perdeu a trava por item | validado (teste + Play Mode) |
| Limites da rota −21/−20/−19/0/19/20/21; margem inválida sem viés | `GameManager.MargemValida` (mínimo 1, aviso no console); `[Min(1)]` no Inspector. Com margem 0 ou negativa, o empate fica na rota C | validado (testes) |
| Rota travada na Fase 4, inclusive depois de salvar/carregar; barras da Fase 4 não recalculam o final | Já era assim; agora coberto por teste | validado (teste) |
| Fase 4 mostra exatamente o caso da rota; caso sem rota nunca aparece junto | `GameManager.CasoDaRotaAtual` (usado pela mesa e pela regra de aceitar) | validado (teste + Play Mode) |
| Tribunal: item de outro caso não vira argumento | O uso do inventário como prova ficou só para a cena aberta sem caso (teste no Editor); com caso, sem provas registradas, entram os argumentos de reserva | inspecionado |
| Encerrar a defesa cedo | `TribunalManager.minimoParaEncerrar` (padrão 2, limitado ao total): o botão só habilita depois disso. Encerrar sempre dá o veredito completo (barra final, explosão na rota A, fala e cutscene); um segundo clique no mesmo instante é ignorado | validado (teste + Play Mode) |
| Consequências sem reaplicar ao reabrir a cena | Coberto por teste: depois de `EncerrarFase` e de salvar/carregar, nada fica pendente e a fase nova não está concluída | validado (teste) |

### 14.2 Arquivos

`CheckpointDoTribunal.cs`, `InventoryManager.cs`, `GameManager.cs`, `TableInteractable.cs`, `DoorInteractable.cs`, `TribunalManager.cs`, `CaseSelectionUI.cs`; `Prefab/Porta.prefab` (sem trava por item); `Scenes/Jogo.unity` (mesa sem item, porta do escritório com a regra do tutorial, campos novos do checkpoint). Testes: `Testes/ProgressaoETribunalTests.cs` (9). Três testes antigos do `ComponentesTests` passaram a dar rota ao caso da Fase 4, que a regra nova exige.

### 14.3 Limitações

- **Evidências exigidas:** depois de publicado, o caso não entrega mais pistas. Exigir uma pista que o jogador pode não ter pego trava a ida ao tribunal; o campo fica vazio por padrão.
- **Itens guardados fora da grade** só voltam na próxima cena ou quando um arquivamento libera espaço (voltar ao preencher qualquer espaço atrapalharia arrastar itens para a prensa).

---

## 15. Fase 3 com três casos (27/09/2026)

Pedido do grupo: a Fase 3 oferece três casos, o jogador escolhe e conclui um; os outros ficam bloqueados, inclusive depois da conclusão; terminado o caso, a Fase 4 começa.

- **Caso retirado: "O Padeiro de Notre-Dame".** Ficam Champ de Mars (puxa para o Povo; guarda o exemplo numérico do Prompt 3), Varennes (puxa para o Estado; tem a etapa complementar do Cocheiro) e Assignats (sobe os dois). Assim a única publicação da Fase 3 ainda pode empurrar a rota para qualquer lado. Para trocar a escolha, recupere os arquivos pelo git e ajuste a ferramenta 6.
- **Ferramenta 6** (**Ferramentas > Campanha > 6 - Retirar o caso do Padeiro**): tirou o caso da mesa e a oferta da biblioteca (`UI.prefab`), removeu as 4 reações/entradas dele na cena Fase3 e apagou os 13 assets do caso; o catálogo foi atualizado. Idempotente. As definições das ferramentas 1 e 3 não o recriam mais.
- **Regra:** `GameManager.casosPorFase` = {1, 1, 1, 1}. Na mesa, os outros casos da fase aparecem "(bloqueado)", esmaecidos e sem aceitar, assim que um é escolhido (novo estado `Bloqueado` em `CaseSelectionUI`) e continuam assim depois da conclusão. Voltando ao escritório, `FimDeFase` encerra a fase (revelações → despesa de 50 → Fase 4 e rota).
- **Saves antigos:** não há save no meio de um caso. Um save na Fase 3 com um caso concluído (regra antiga: 1 de 2) passa direto para a Fase 4 ao voltar ao escritório. Um save que citasse o Padeiro perde esse caso ao carregar (aviso no console); o do grupo em 27/09 está no início da Fase 2.
- **Rotas:** com três publicações antes da rota, os exemplos da matriz foram refeitos (A: Joias + Champ de Mars; B: Réveillon + Varennes; C: Réveillon + Champ de Mars).
- **Testes:** `CampanhaTests` (três casos na Fase 3; um aceito bloqueia os outros, antes e depois da conclusão; `EncerrarFase` abre a Fase 4). Play Mode na cena Jogo: três cartas → aceitar Champ de Mars → os outros "(bloqueado)" → concluir → continuam bloqueados → fim da fase → Fase 4 com um cartão só.

---

## 16. Verificação de 27/09 e o que o grupo precisa conferir

### 16.1 Resultados

| Checagem | Resultado |
|---|---|
| Compilação / console | sem erros |
| EditMode | **78/78** (58 anteriores + 20 novos) |
| Validador | "Campanha válida", 0 avisos: Fase 2: 3, Fase 3: 3, Fase 4: 3 (um por rota); dedução em 6 casos; quadro inteiro = 4h em cada um; caminho mínimo até 2 fatos = 2h (Joias: 1h) |
| Ferramentas 1, 3, 4, 5 e 6 reexecutadas | 0 alterações |
| Play Mode — Fase3 (Varennes) | §13.5 |
| Play Mode — Jogo | mesa e porta trancadas no tutorial, liberadas pela conclusão sem o panfleto no inventário; Fase 3 com bloqueio (§15); Fase 4 com o cartão da rota A; Dupaty recusa antes da publicação (fala "não pronto", nenhum save) e fica "Pronto" depois (versão Fatos pela prensa real) |
| Play Mode — Tribunal (carregado direto, sem o save do Dupaty) | 7 provas do caso; "Encerrar" desabilitado com 0 e 1 prova, habilitado com 2; encerrar dá o veredito completo da rota A (barra em 100, réu absolvido); segundo clique ignorado |
| Git | só as mudanças descritas; ruído de layout do `UI.prefab` que o Unity escreveu no `Jogo.unity` foi revertido; assets regravados sem mudança foram restaurados |

### 16.2 Efeitos no ambiente

- `save.json`: não foi criado, alterado nem apagado (md5 conferido antes e depois).
- PlayerPrefs: a dica de fato/boato apareceu no Play Mode e marcou `dica_fato_boato_vista`; a chave foi apagada no fim, e as cinco chaves voltaram ao estado anterior.
- Game View: trocada para 1920×1080 e 800×480 só nas capturas; voltou para "Free Aspect". `Application.runInBackground` só ligado nas sessões de Play. O Editor voltou para a cena Fase2.
- **`Scenes/Fase2.unity` tem mudanças salvas por vocês durante a sessão** (overrides de fonte/cor/tamanho de textos do UI e uma posição): não são desta etapa e não foram tocadas. Confiram antes do commit.
- Nada foi commitado.

### 16.3 O que conferir na validação

1. **Jogar um caso da Fase 3 inteiro com teclado/mouse:** mesa com três cartas; depois de aceitar, as outras bloqueadas; pistas levando de uma fonte a outra; Quadro de pistas (marcar, conferir errado, conferir certo); publicar com e sem conferir; voltar ao escritório → Fase 4.
2. **Textos novos** (provisórios) das pistas, falas e documentos das Fases 3 e 4 (matriz) e do quadro. Se algum for editado no Inspector, a ferramenta 5 não o sobrescreve.
3. ~~**Decisão de horas** da dedução (§13.6).~~ Decidido em 27/09: 6h nas Fases 2 a 4 (§17).
4. **Tribunal:** o mínimo de 2 provas para encerrar está bom?
5. **Porta e mesa** no tutorial (Novo Jogo): continuam trancadas até imprimir o panfleto e abrem depois.
6. **Celular/toque:** botão "Quadro de pistas" e janela no aparelho.

---

## 17. Prompt 7 — tutorial, transições e UI integrada (27/09/2026)

Pedido do grupo: conferir no projeto o que já existia, classificar cada requisito, completar o Prompt 7 e, junto com ele, **(A)** dar **6h** no relógio do jogo aos casos das Fases 2, 3 e 4, mantendo a conversão do projeto (1h por NPC ou objeto novo), e **(B)** deixar a **dedução de pistas** disponível e integrada **desde a Fase 2**. O Prompt 8 não foi iniciado.

### 17.1 Diagnóstico (conferido no projeto antes de mexer)

Conferidos scripts, cenas, prefabs e ScriptableObjects pelo Unity (MCP), não só pelos documentos.

| # | Requisito | O que havia no projeto | Estado antes |
|---|---|---|---|
| 1 | Tutorial: intro → controles → Dupaty e Marie → alçapão → status na prensa → panfleto → cutscene → volta com Marie ausente | `TutorialManager` (15 etapas na cena Jogo), `IntroCutscene`, `Fase1Desfecho`, `GerenciadorCena1`; percurso validado em 26/09 (§9.1) | **concluído**, com dois defeitos achados agora: o fim de *qualquer* diálogo concluía "fale com Dupaty" (o pensamento do alçapão pulava a etapa, e Marie ainda não respondia); os pensamentos do alçapão eram falas do Operário da Fase 2 (`Operario_Dialogo`, `resposta_operario`) |
| 2 | Marie ausente depois de salvar/carregar, sem depender do panfleto | `GerenciadorCena1` escondia Marie só com o panfleto em `inventarioSalvo` ou com um caso diferente do tutorial | **parcial** |
| 3 | Mesa com selecionados/concluídos bloqueados e recompensa como estimativa | `CaseSelectionUI` (estados, "Recompensa estimada", tendências) | **concluído** no conteúdo; na tela, **parcial**: o painel foi desenhado para 2376 de largura, então em 16:9 o **Fechar ficava fora da tela** (x −1077) e em 4:3 os cartões eram cortados |
| 4 | Porta com fade e evento FMOD existente, sem gerenciador duplicado | `event:/portaabrir` na porta e `event:/bauabrir` no alçapão (caminho e GUID na cena); fade corrigido em §11 | **concluído** |
| 5 | Pop-ups quando o item entra de fato e quando o caso é aceito; nada ao restaurar | `InventoryManager.AddItem`/`OnItemAdicionado` (silencioso na restauração), `GameManager.ConfirmarCaso` ("Caso aceito") | **concluído** |
| 6 | Dicas de primeira vez (relógio, despesas, biblioteca, dedução, tom), salvas e zeradas no Novo Jogo; texto da dica de fato/boato | Só relógio e fato/boato; a de fato/boato mandava "procurar outra fonte que confirme"; um só lugar para dica pendente (a segunda apagava a primeira) | **parcial** |
| 7 | Biblioteca, quadro, inventário, prensa, pause e cutscene em sequência, com input e `timeScale` restaurados | `JanelasModais` (Biblioteca e Quadro) | **parcial**: o pause abria por baixo de uma cutscene (ao terminar, ela devolvia `timeScale` 1 com o pause aberto); o E avançava a conversa escondida pelo pause; a Mesa não travava movimento, inventário nem pause; o popup do tutorial aceitava Enter por baixo do pause |
| 8 | Resoluções desktop e celular, área segura, legibilidade, rolagem, toque, foco | Área segura na HUD, Biblioteca e Quadro; capturas de 26–27/09 | **parcial**: prensa fora da tela em 4:3 (§6.4); Mesa (item 3); controles de toque desenhados por cima da Mesa |
| A | 6h nas Fases 2, 3 e 4 | `GameManager.horasPorCaso` = 4 (padrão do código; nem o prefab nem as cenas gravam o campo); nenhum caso define horas próprias | **não implementado** |
| B | Dedução desde a Fase 2 | Quadro com `faseMinima` 3; os casos da Fase 2 sem pares; boato e calúnia da Fase 2 sem contradizer o fato do mesmo assunto; validador e testes exigiam só Fases 3 e 4 | **não implementado** |

### 17.2 O que foi feito

| Requisito | Implementação | Estado |
|---|---|---|
| 1. Tutorial | A etapa "fale com … até o fim" passou a contar só conversa com NPC: o `NPCMovement` avisa o `TutorialManager` (`NotificarConversaComNpcTerminada`) no fim da conversa, depois dos eventos que liberam o próximo NPC; o `TutorialManager` não escuta mais o fim de todo diálogo. Pensamentos novos do protagonista no alçapão (`Dialogos/Julien Valois/Pensamento_Alcapao_SemCaso` e `…_FaltamPistas`), ligados na cena Jogo | validado (teste + Play Mode) |
| 2. Marie | `GerenciadorCena1.TutorialEncerrado`: tutorial concluído (`GameManager.TutorialConcluido`, caso do tutorial concluído ou caso da mesa aceito) esconde Marie e silencia Dupaty; o panfleto no inventário ficou só como reserva de saves antigos. O save é aplicado no `Awake` do `GameManager`, antes de qualquer `Start`, e a regra lê só o `GameManager` | validado (teste + Play Mode com Continuar simulado) |
| 3/8. Mesa na tela | `CaseSelectionUI` ajusta o painel à tela visível: cobre a tela, a área dos cartões encolhe até caber (nunca passa de 1885 de largura) e usa a altura livre, o **Fechar** vai para o canto superior direito dentro da área segura. Canvas próprio (ordem 5) acima dos controles de toque (2): o toque vai para os cartões | validado (capturas 16:9, 4:3 e 18,5:9, raycast do toque) |
| 6. Dicas | Fila no `TutorialManager` (`DicaPendente`, `ConcluirDica`): nenhuma dica some nem sobrepõe outra, e uma dica não fechada volta na cena seguinte. Quatro dicas novas: **despesas** (depois da cutscene da primeira cobrança, `FimDeFase`), **Biblioteca** (quando o botão aparece pela primeira vez), **dedução** (primeira pista de um par do quadro), **linha editorial** (quando a prensa abre com um caso que pede o tom, antes da janela, para o Enter da dica não cair no "Cancelar" dela). Dica de fato/boato reescrita (aponta o Quadro de pistas; código e cena Jogo). Chaves novas em `ProgressoDoJogo.ChavesDaPartida`: vão no save e zeram no Novo Jogo; volume e velocidade do texto continuam. Nenhuma dica diz qual pista é verdadeira | validado (testes + Play Mode) |
| 6. Popup | `TutorialStepUI`: some com conversa, cutscene, pause, Biblioteca, Quadro ou Mesa abertos e volta sozinho; Enter/Espaço não agem no frame em que ele aparece nem quando outro controle da UI tem o foco do teclado; corrigida a disputa de `Start` que deixava a dica do relógio presa (ela era pedida antes de o popup se montar) | validado (Play Mode) |
| 7. Modais | `PauseMenu` não abre durante cutscene; `PlayerInteraction` ignora o Interagir com o pause aberto; `JanelasModais` inclui a Mesa (sem movimento, interação, inventário nem pause por cima); Esc fecha a Mesa sem abrir o pause no mesmo frame; foco do teclado começa no Fechar; controles de toque desligados enquanto a Mesa está aberta | validado (testes + Play Mode, Esc real pelo Input System) |
| 8. Telas estreitas | `CanvasScaler` do `UI.prefab` em **Expand** (continua 1920×1080 de referência): 16:9 e celulares ficam iguais; em 16:10 e 4:3 a UI inteira cabe na largura (antes a prensa saía da tela) | validado (capturas 4:3 com prensa + Suporte) |
| A. 6h | `GameManager.horasPorCaso` = **6** (os nove casos das Fases 2–4 usam o padrão). A conversão continua: 1h por NPC/objeto novo, repetir é grátis, HUD "Tempo de investigação: Xh / 6h"; endividado, 5h. O validador exige 6h em todo caso das Fases 2 a 4 | validado (teste + validador + Play Mode 6h/6h) |
| B. Dedução na Fase 2 | Pares nos três casos (fato do cliente × boato; fato do objeto × calúnia), quadro desde a Fase 2 (`faseMinima` 2; botão no lugar da Biblioteca enquanto ela não existe), boato e calúnia reescritos para contradizer o fato do mesmo assunto, falas de quem espalha o boato apontando os objetos, uma carta de cliente por caso apontando quem espalha o boato, revelação do boato de Réveillon ajustada. Validador exige dedução em todo caso das Fases 2 a 4 e o quadro inteiro dentro do orçamento | validado (validador + testes + Play Mode completo no Caso de Réveillon) |

### 17.3 Decisões desta etapa

- **Onde ficam as 6h:** no padrão `GameManager.horasPorCaso`, que já era o único valor usado pelos casos (todos com `horasDeInvestigacao` 0); um caso ainda pode definir horas próprias no Inspector. O prefab e as cenas não gravam o campo, então não houve mudança de asset. O tutorial continua sem relógio.
- **Conteúdo da Fase 2:** os fatos das Joias e as falas dos clientes (roteiros do grupo) não mudaram. Mudaram o boato e a calúnia de cada caso (conteúdo provisório do Prompt 2). Como as falas dos clientes são do grupo, a pista "quem espalha o boato" foi para a carta do cliente (alegação), que o jogador lê ao aceitar o caso. O boato de Réveillon trocou de assunto: de "a ordem de atirar" para **"Os quinze soldos"** (o boato histórico sobre o discurso), para contradizer "O discurso na assembleia"; a fala do grupo "Eu não mandei atirar em ninguém!" continua fazendo sentido.
- **Quando cada dica aparece:** no primeiro contato com o recurso, e nunca por cima de uma janela; a da Biblioteca quando o botão aparece; a da dedução quando o quadro ganha a primeira pista; a do tom ao abrir a prensa; a das despesas depois da cutscene que as cobra.
- **Mesa de Casos como janela modal**, com o Fechar à direita como o inventário, a Biblioteca e o Quadro (à esquerda ficava sobre a HUD).
- **"Expand" em vez de redesenhar painéis:** uma propriedade do `CanvasScaler` resolve a prensa e o Suporte em 16:10/4:3 sem mexer no layout de 16:9.

### 17.4 Arquivos

- Scripts: `GameManager`, `GerenciadorCena1`, `TutorialManager`, `TutorialStepUI`, `ProgressoDoJogo`, `FimDeFase`, `BibliotecaUI`, `QuadroDeDeducaoUI`, `LinhaEditorialDaPrensaUI`, `PauseMenu`, `PlayerInteraction`, `JanelasModais`, `CaseSelectionUI`, `NPCMovement`; comentários em `DeducaoAtiva`, `@CaseData`, `Item`.
- Editor: `DeducaoSetupTool` (Fase 2), `CampanhaSetupTool` (textos de referência da Fase 2), `ValidadorDaCampanha` (dedução desde a Fase 2, 6h, quadro dentro do orçamento), `TutorialRoteiroTool` (copia todas as dicas), `Prompt7SetupTool` (novo: **Ferramentas > Campanha > 7 - Aplicar ajustes do Prompt 7**).
- Testes: `Testes/TutorialEUiTests.cs` (novo, 9), `DeducaoTests` (+1; o teste dos casos reais passou a exigir as Fases 2 a 4), `ComponentesTests` (fixa 4h nos testes de cobrança, que medem 1h por interação, não o orçamento).
- Assets: `Casos/Caso_Joalheiro|Reveillon|Operario` (pares), 6 pistas e 3 alegações em `Scriptableobjects/Campanha/Fase2`, 4 diálogos em `Dialogos/Campanha/Fase2`, `ReceitaDeCaso_Jean` (revelação Com Boato), 2 diálogos novos em `Dialogos/Julien Valois`, `Prefab/UI.prefab` (CanvasScaler, `faseMinima` do quadro, 2 campos novos da Mesa), `Scenes/Jogo.unity` (só as dicas do `TutorialManager` e os pensamentos do alçapão; o ruído de layout do `UI.prefab` que o Unity gravou junto foi revertido).
- Não mudaram: `GameManager.prefab`, as outras cenas, ProjectSettings, Packages, GUIDs e `.meta` existentes.

### 17.5 Configuração realizada

- Ferramenta 5: 3 casos com dedução ligada, 6 pistas, 4 falas, 3 cartas de cliente, 1 revelação e 1 alteração no `UI.prefab` (`faseMinima` 2). Segunda execução: 0.
- Ferramenta 7: `CanvasScaler` em Expand, 2 pensamentos criados, 3 alterações na cena Jogo (dica de fato/boato e os dois pensamentos). Segunda execução: 0.
- Ferramentas 1, 3, 4 e 6 reexecutadas: 0 alterações (a 1 regravou 8 receitas com conteúdo idêntico, restauradas pelo git).
- Validador: **"Campanha válida", 0 avisos**. Orçamento 6h (5h endividado); dedução em 9 casos; quadro inteiro: Joias 3h, os outros oito 4h; caminho mínimo até 2 fatos: Joias 1h, os outros 2h.

### 17.6 Testes

**EditMode: 88/88** (78 anteriores + 10 novos): fila de dicas (duas pedidas juntas aparecem uma depois da outra, dica vista não volta, marcada no pedido); dica da dedução só na primeira pista de um par (alegação não conta) e texto de fato/boato apontando o quadro; Novo Jogo zera as quatro chaves novas e preserva o volume; etapa "fale com Dupaty" não avança com o pensamento do alçapão e avança com conversa de NPC; Marie some pelo progresso sem o panfleto, também depois de salvar/carregar; 6h no prefab e no código, nos 9 casos, 5h endividado; pause não abre com cutscene; Mesa aberta trava pause e inventário; quadro disponível na Fase 2 com caso real; pares da Fase 2 do mesmo assunto e sem nome que denuncie a verdade.

**Play Mode** (Editor via MCP; botões pelo `onClick`/`Interact`, Esc por evento do Input System; capturas por `ScreenCapture`):

| Sessão | Resultado |
|---|---|
| Tutorial desde a intro (16:9) | pause recusado durante a cutscene de abertura; popup some com o pause e volta ao fechar; o pensamento do alçapão é do protagonista (e, depois da correção, não conclui "fale com Dupaty"; a conversa com Dupaty conclui e libera Marie); Marie (6 escolhas), inventário, decreto, porão, prensa, panfleto, volta: Fase 2, Marie fora, Dupaty silenciado, cutscene da Fase 1 |
| Mesa (16:9) | cabe na tela, Fechar visível no canto superior direito, foco no Fechar, popup do tutorial escondido enquanto ela está aberta; aceitar Réveillon → cartas do cliente → dica de fato/boato depois que a mesa fecha; botão do quadro no lugar da Biblioteca |
| Fase 2 com dedução (Réveillon) | relógio 6h/6h; dica do tempo; Operário → dica da dedução (5h); Réveillon, Gazeteiro e Banca → 4 pistas em 4h, "2 de 2"; com o quadro aberto, pause e inventário não abrem; tudo "Confiável" e uma errada → mesma resposta genérica; certo → "A dedução se sustenta", inventário com "confirmada/desmentida" |
| Prensa | dica da linha editorial ao abrir; janela com foco em Cancelar; Fatos + Defesa do povo: Povo 65→55, Estado 40→85, ouro 20→70 |
| Fim da Fase 2 | Fase 3, despesa de 25 (70→45), cutscene → dica das despesas → dica da Biblioteca (em fila) |
| Continuar simulado (dados do save em memória, sem ler nem gravar o arquivo) | Fase 3 sem o panfleto: Marie fora, Dupaty silenciado; save logo após o tutorial (caso ainda do tutorial, inventário vazio): Marie fora — o código antigo a mostraria |
| Esc na Mesa | fecha sem abrir o pause |
| Troca de cena com dica aberta | a dica não fechada reaparece na Fase 2, seguida da do tempo |
| 4:3 (1440×1080) | HUD, relógio, quadro e dica cabem; a prensa cabe inteira (antes saía da tela); prensa + Suporte (Fase 3) cabem; Mesa ocupa a tela toda |
| Celular simulado (4:3 e 2960×1440) | Mesa acima dos controles de toque; o toque nos "Aceitar Caso" cai nos botões da Mesa; no escritório, Pause, Biblioteca e Quadro empilhados sem se cobrir; os controles voltam a responder quando a Mesa fecha |

### 17.7 Limitações e pendências

- **Não testado com dispositivos reais:** teclado e toque físicos, aparelho Android, áudio (as referências FMOD foram conferidas, o som não).
- **4:3 e telas menores:** com "Expand" a UI inteira fica menor (escala 0,75 em 1440×1080; 0,53 em 1024×768, com texto pequeno).
- **Menu principal:** os botões Novo Jogo e Continuar não foram usados (apagam/regravam as chaves de quem testa); o Continuar foi simulado em memória e o Novo Jogo coberto por teste.
- **Enter no popup:** ignorado quando outro controle da UI está com o foco do teclado (ex.: depois de clicar em Misturar); aí é preciso clicar em Continuar.
- **Dicas:** marcadas quando pedidas (como antes); fechar o jogo antes de ler uma dica a perde.
- **Pista do boato na Fase 2:** vem na carta do cliente, não na fala dele (roteiro do grupo). Se quiserem, uma linha na fala do cliente substitui a carta.
- **Tempo:** com 6h e caminho mínimo de 1–2h, o relógio pesa menos; o balanceamento fica para o Prompt 8.
- **Cosmético, anterior a esta etapa:** o "E" da porta aparece sobre o relógio no ponto de chegada das cenas de investigação; o quadro também aparece no escritório com um caso em andamento (como antes).
- **"Pensamento sem caso" do alçapão** quase nunca aparece: o jogo começa com o caso do tutorial escolhido.

### 17.8 Conteúdo provisório

Textos das quatro dicas novas e a dica de fato/boato reescrita (`TutorialManager`, editáveis na cena Jogo); os dois pensamentos do alçapão; boato e calúnia dos três casos da Fase 2, as quatro falas de quem os espalha, três cartas de cliente e a revelação Com Boato de Réveillon (lista na matriz). Edições no Inspector são mantidas pelas ferramentas 5 e 7 (só trocam texto que ainda é o anterior).

### 17.9 O que ainda exige teste manual na Unity

1. **Tutorial inteiro com teclado e mouse** (Novo Jogo): em "fale com Dupaty", tentar o alçapão antes (deve aparecer o pensamento do Julien e a etapa não avançar); ao voltar do porão, Marie some.
2. **Salvar e continuar de verdade:** jogar até o fim da Fase 1 (ou de uma fase), sair para o menu, **Continuar**: Marie continua fora, as dicas já vistas não repetem, a partida segue da mesma fase.
3. **Novo Jogo depois de Continuar:** dicas e cutscenes voltam; volume e velocidade do texto ficam.
4. **Caso da Fase 2 com dedução:** conferir, lendo, se cada par (Joias, Réveillon, Operário) se contradiz de forma justa e se a carta do cliente e as falas levam de uma fonte à outra.
5. **Relógio de 6h:** sentir se a pressão do tempo ainda existe; endividado deve começar com 5h.
6. **Dicas em sequência:** na virada da Fase 3, despesas e depois Biblioteca; nenhuma por cima de janela aberta.
7. **Mesa de Casos:** Esc, Fechar e mouse/teclado (Tab/setas) em 16:9; no celular, tocar em "Aceitar Caso" e no Fechar.
8. **Celular Android real:** Mesa acima dos controles, prensa com Suporte, Quadro de pistas, popup de dica sem cobrir os botões de toque.
9. **Tela 16:10 ou 4:3 (tablet ou monitor antigo):** legibilidade com a UI reduzida.

### 17.10 Efeitos no ambiente de quem testou

- **`save.json`** (Fase 4, rota A, gravado às 15:54): copiado para o scratchpad da sessão e deixado **somente leitura** durante o Play Mode. As duas gravações automáticas das sessões (fim do tutorial e fim de fase) falharam contra ele, como planejado, e deixaram um `save.json.tmp`, que foi apagado. Atributo restaurado; **md5 idêntico ao de antes** (`5AE7EA19…`).
- **PlayerPrefs:** as cinco chaves de progresso voltaram a 1; as quatro chaves novas de dica foram apagadas (não existiam), então as dicas novas aparecem na próxima partida de vocês.
- **Editor:** simulação de celular de volta a desligada; o tamanho 4:3 temporário da Game View foi removido e a Game View voltou para "Free Aspect"; `Application.runInBackground` desligado; cena Jogo aberta e limpa.
- `TestResults.xml` (em `persistentDataPath`, já existia) foi regravado pelo Test Runner.
- Capturas em `Unity-DaPenaAGuilhotina/Temp/CapturasP7` (pasta Temp, fora do git).
- Nada foi commitado.

### 17.11 Como revalidar

1. **Ferramentas > Campanha > 5** e **> 7** duas vezes → a segunda com 0 alterações; **Ferramentas > Campanha > 2** → "Campanha válida" com "Orçamento: 6h por caso (5h endividado)" e "Dedução ativa: 9 caso(s)".
2. Test Runner > EditMode > Run All → **88/88**.
3. Roteiro manual de §17.9.
