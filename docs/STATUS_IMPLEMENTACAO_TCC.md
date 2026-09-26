# Status de implementação — Da Pena à Guilhotina

Atualizado em 25/09/2026. Referência: `docs/PROMPTS_CLAUDE_CODE_TCC.md`. Caminhos relativos a `Unity-DaPenaAGuilhotina/`.

Estados usados: **validado** (testado em runtime ou em teste automatizado), **existente não validado** (código/asset presente, sem teste de runtime), **parcial**, **ausente**.

Etapas: Prompt 0 (auditoria) concluído; Prompt 1 concluído; Prompt 2 concluído; **Prompt 3 concluído**; **verificação dos Prompts 0–3 concluída, com os achados corrigidos (§7)**; próximo liberado: **Prompt 4**.

Regras que mudaram na verificação e prevalecem sobre o texto das seções 4–6:
- **Alegações:** qualquer impressão com alegação usa a versão Só Alegações.
- **Arquivamento:** sobras dos casos anteriores à Fase 4 são arquivadas ao concluir.
- **Inventário cheio:** interação sem espaço para o que entregaria não cobra horas.
- **Save:** passou para a versão 5.
- **Biblioteca:** tem estados por situação.
- **Pistas:** os nomes são neutros.

Documento de autor com a matriz dos casos: `docs/MATRIZ_DE_CASOS_TCC.md` (não exibir ao jogador).

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
| Tutorial (Dupaty → Marie → Dupaty → alçapão → prensa → panfleto → escritório → mesa) | `TutorialManager` (15 etapas na cena Jogo), `GerenciadorCena1`, `Fase1Desfecho`, `IntroCutscene`, `TrapdoorInteractable.requisitosDeCaso` (Caso_Tutorial exige as 2 evidências) | existente não validado de ponta a ponta; receita da prensa **validada** (ver 4.4) |
| Mesa de casos por fase/rota | `CaseSelectionUI.availableCases` (UI.prefab) com 10 casos; filtro `CaseData.fase`/`rota`; estados disponível / em andamento / concluído | **validado** após Prompt 2 (antes: só Fase 2) |
| Progressão `{1,1,2,1}` e fim de fase | `GameManager.casosPorFase` (padrão do código; não serializado), `FimDeFase` no UI.prefab (cena Jogo) | existente não validado |
| Tempo de investigação (proposta A) | `RelogioDeInvestigacao`, `HudDoRelogio`, `NPCMovement.custoEmHoras`/`LootInteractable.custoEmHoras` (padrão 1h), `horasPorCaso` 4 | **validado** após Prompt 1 |
| Despesas e dívida (proposta B) | `despesasPorFase {0,25,50,0}`, `horasPerdidasPorDivida` 1, cobrança em `FimDeFase.Start` | parcial: perda de 1h validada em teste; ordem com revelações pendente (Prompt 6) |
| Fato x boato | `Item.confiabilidade`, `ReceitaDeCaso` (10 assets), `CraftingPress.TentarPanfletoDeCaso` | parcial: após o Prompt 2 cada caso tem 2 fatos, 1 boato, 1 calúnia e textos de revelação; nenhum `verificadaPor` (a verificação automática será substituída pela dedução no Prompt 4) |
| Revelação de boatos | `RevelacaoDeBoatos` | **ausente em cena**: o componente não está em nenhuma cena/prefab; `revelacoesPendentes` nunca é consumido |
| Biblioteca e qualidade | `BibliotecaUI`, `OfertaDaBiblioteca` (7), `Item.qualidade`/`documentoDeSuporte`, `ReceitaDeCaso.qualificadores`, `CalculadoraDePanfleto`, `SuportesDaPrensaUI` | **validado** após Prompt 3 (testes + Play Mode); visual não conferido em tela |
| Dedução ativa (C) | verificação automática em `InventoryManager.PistaVerificada`/`RegistrarVerificacoes` | ausente |
| Linha editorial (D) | — | ausente |
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

## 8. Próximo prompt liberado

**Prompt 4 — dedução ativa e pistas conflitantes (C).** Pontos de partida:
- **Histórico:** `GameManager.evidenciasObtidas` já guarda as pistas obtidas, inclusive as gastas.
- **Verificação atual:** a verificação automática está em `InventoryManager.PistaVerificada`/`RegistrarVerificacoes` e na `FichaDaPista`; hoje nenhuma pista tem `verificadaPor`.
- **Fora dos conjuntos de dedução:** alegações e documentos de apoio.
- **Pistas:** os nomes já são neutros. A **fonte** continua sendo a dica de confiabilidade prevista no desenho original (anônimo/boato versus documento/testemunha); avaliar no Prompt 4 se ela deve ficar menos determinística.

Questões em aberto (não bloqueiam o Prompt 4):
- **Revelações:** onde colocar `RevelacaoDeBoatos` e a ordem em relação à trava de rota (Prompt 6).
- **Inventário ao restaurar:** perda silenciosa de itens quando a grade está cheia (Prompt 6).
- **Tribunal:** provas restritas ao caso, incluindo documentos comprados (Prompt 6).
- **Referências quebradas:** as listadas em 3.2, item 5.
