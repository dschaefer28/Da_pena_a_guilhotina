# Status de implementação — Da Pena à Guilhotina

Atualizado em 25/09/2026. Referência: `docs/PROMPTS_CLAUDE_CODE_TCC.md`. Caminhos relativos a `Unity-DaPenaAGuilhotina/`.

Estados usados: **validado** (testado em runtime ou em teste automatizado), **existente não validado** (código/asset presente, sem teste de runtime), **parcial**, **ausente**.

Etapas: Prompt 0 (auditoria) concluído; **Prompt 1 concluído**; próximo liberado: **Prompt 2**.

---

## 1. Ambiente

| Item | Situação |
|---|---|
| Unity | 6000.3.9f1 aberto no Editor durante o trabalho (projeto `Unity-DaPenaAGuilhotina`) |
| Unity MCP | Operacional (`com.coplaydev.unity-mcp`): leitura de cenas, compilação, Play Mode, execução de código de Editor e Test Runner |
| Testes | `com.unity.test-framework` 1.6.0. Não havia testes. Os testes novos ficam em `Assets/Editor/Testes/` (assembly `Assembly-CSharp-Editor`, sem asmdef, porque os scripts do jogo não têm asmdef) |
| Compilação | Sem erros. Avisos antigos: `FindObjectOfType` obsoleto em `GameManager`; `Caso_Tutorial` sem `caseTitle`/`npcDialogueRoute` (OnValidate) |
| Save real do jogador | Não foi lido nem alterado (nenhum teste chamou `SistemaDeSave.Salvar`) |

## 2. Premissas adotadas (seção "Decisões adotadas" do documento de prompts)

1. Finais: A = Guilhotina, B = Tirano, C = Equilíbrio (código atual já segue).
2. Tribunal: investigar → panfleto → Dupaty → tribunal.
3. "Final da Fase 2" citado na Fase 3 = erro de numeração.
4. Final C "O Exílio" (texto em `TribunalManager`) é provisório, pendente de validação autoral.
5. Dedução ativa (C) nas Fases 3 e 4, por caso; sem confronto com NPC na 1ª versão.
6. Linha editorial (D) a partir da Fase 2; tutorial com receita simples; tom não muda a verdade.
7. Receita expandida mantém 2 slots; evidências complementares como qualificadores opcionais.
8. Campanha: Fase 2 = 1 de 3 casos; Fase 3 = 2 distintos de 4; Fase 4 = 1 caso da rota.
9. Sem bloqueio por falta de pistas (duas alegações não verificadas como saída de design, se necessário).
10. Valores e textos novos são protótipos editáveis, rotulados como provisórios.

Dependências entre etapas: 1 (estabilidade/save) → 2 (conteúdo) → 3 (biblioteca) → 4 (dedução) → 5 (tom) → 6 (consequências/tribunal) → 7 (UI/tutorial) → 8 (validação).

## 3. Diagnóstico (Prompt 0)

### 3.1 Mapa de requisitos

| Requisito | Evidência (arquivo / campo) | Estado |
|---|---|---|
| Tutorial (Dupaty → Marie → Dupaty → alçapão → prensa → panfleto → escritório → mesa) | `TutorialManager` (14 etapas na cena Jogo), `GerenciadorCena1`, `Fase1Desfecho`, `IntroCutscene`, `TrapdoorInteractable.requisitosDeCaso` (Caso_Tutorial exige as 2 evidências) | existente não validado de ponta a ponta; receita da prensa **validada** (ver 4.4) |
| Mesa de casos por fase/rota | `CaseSelectionUI.availableCases` = Joalheiro, Réveillon, Operário (UI.prefab); filtro `CaseData.fase`/`rota` | parcial: só Fase 2 tem casos |
| Progressão `{1,1,2,1}` e fim de fase | `GameManager.casosPorFase` (padrão do código; não serializado), `FimDeFase` no UI.prefab (cena Jogo) | existente não validado |
| Tempo de investigação (proposta A) | `RelogioDeInvestigacao`, `HudDoRelogio`, `NPCMovement.custoEmHoras`/`LootInteractable.custoEmHoras` (padrão 1h), `horasPorCaso` 4 | **validado** após Prompt 1 |
| Despesas e dívida (proposta B) | `despesasPorFase {0,25,50,0}`, `horasPerdidasPorDivida` 1, cobrança em `FimDeFase.Start` | parcial: perda de 1h validada em teste; ordem com revelações pendente (Prompt 6) |
| Fato x boato | `Item.confiabilidade`, `ReceitaDeCaso` (3 assets), `CraftingPress.TentarPanfletoDeCaso` | parcial: **todas as 6 pistas são Fato**, nenhum `verificadaPor`, todos os `textoRevelacao` vazios |
| Revelação de boatos | `RevelacaoDeBoatos` | **ausente em cena**: o componente não está em nenhuma cena/prefab; `revelacoesPendentes` nunca é consumido |
| Biblioteca e qualidade | busca em scripts/assets/cenas | ausente |
| Dedução ativa (C) | verificação automática em `InventoryManager.PistaVerificada`/`RegistrarVerificacoes` | ausente |
| Linha editorial (D) | — | ausente |
| Tribunal e rota | `CheckpointDoTribunal` (filho do Dupaty, cena Jogo), `TribunalManager` (cena Tribunal), `GameManager.CalcularRota` (margem 20) | existente não validado |
| Save | `SistemaDeSave` (agora v3), `CatalogoDeSave` (12 itens, 4 casos, completo) | **validado** (migração/ida e volta em teste) |
| Conteúdo | 4 `CaseData` (Tutorial fase 1; três de fase 2, todos com `nextSceneName: Fase2`); 0 casos de Fase 3/4 | parcial |
| Cenas | Build: menu principal, Jogo, Fase2, Porao, Fase3, Fase4, Tribunal (todas habilitadas) | Fase3/Fase4 são cópias da Fase2 **sem NPCs** |
| Loot em cena | `LootInteractable` | ausente em todas as cenas (só o script existe) |

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

## 5. Próximo prompt liberado

**Prompt 2 — completar conteúdo e progressão das fases.** Pontos de partida do diagnóstico: criar casos de Fase 3 (4) e Fase 4 (3, um por rota) com `nextSceneName` Fase3/Fase4; popular Fase3/Fase4 (hoje sem NPCs) e colocar `LootInteractable` com IDs (rodar a ferramenta); cadastrar pistas Boato/Calúnia e `textoRevelacao`; revisar `casoObrigatorio`/`disableAfterDialogue` dos NPCs da Fase 2; preencher `itemName`/`descricao`/`fonte` dos itens.

Questões em aberto (não bloqueiam o Prompt 2): posicionamento de `RevelacaoDeBoatos` e ordem em relação à trava de rota (Prompt 6); perda silenciosa de itens quando a grade está cheia ao restaurar o inventário (Prompt 6); provas do tribunal restritas ao caso (Prompt 6); referências quebradas listadas em 3.2 item 5.
