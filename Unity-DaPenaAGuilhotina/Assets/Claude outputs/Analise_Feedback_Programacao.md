# Análise do feedback dos professores — parte de programação

Li os 32 scripts da pasta `Scripts` (2433 linhas) e cruzei com a lista de feedback das duas fases. Abaixo separo o que é de fato tarefa de código do que é arte/áudio/conteúdo/level design (para você não gastar tempo em algo que não é seu), e para cada item de programação aponto onde mexer.

## O que NÃO é sua parte (arte, conteúdo, level design)

Ficam de fora da programação: animação dos personagens, sprites e ícones faltando, o design "não parecer francês", posicionamento visual do nome nas caixas de diálogo (é layout de prefab, não código), textos dos casos em caixa alta (é conteúdo do ScriptableObject `CaseData`, não há nenhuma linha de código forçando uppercase), diálogos dos NPCs faltando (conteúdo dos `DialogueData`), a mesa faltando na cena, e "incluir mais elementos/NPC por caso" (povoamento de cena). Composição de música/ambiência também é trabalho do sound designer — mas o sistema que *toca* esses sons no código não existe ainda, e isso é seu (ver seção Som).

## Fase 1 — itens de programação

**Falta feedback quando recebe itens e vão para inventário.** Hoje `LootInteractable.Interact()` e `NPCMovement.HandleDialogueEnded()` só dão `Debug.Log($"Você encontrou: {itemCorreto.name}!")` — não existe nenhum popup, toast ou animação visível ao jogador. Sugiro criar um evento tipo `OnItemReceived(Item item)` no `InventoryManager` (mesmo padrão que já usam com `OnInventoryToggled`), disparado dentro de `AddItem()`, e uma UI simples (um popup "Item obtido: X" com ícone) escutando esse evento. Isso cobre loot, recompensas de diálogo e crafting no mesmo lugar.

**Incluir transição para parte de baixo da casa** e **Incluir transição de cena** (Fase 2) são o mesmo problema em dois lugares: `DoorInteractable.Interact()` e `TrapdoorInteractable.Interact()` chamam `SceneManager.LoadScene(cena)` direto, sem fade — a troca é um corte seco. Vale criar um `SceneTransitionManager` (singleton simples, no padrão do `GameManager`/`PauseManager`) com um `CanvasGroup` preto full-screen, um método `LoadSceneWithFade(string cena)` que faz fade-out → `LoadSceneAsync` → fade-in. Depois trocar as chamadas diretas em `DoorInteractable`, `TrapdoorInteractable` (e de quebra em `MenuPrincipalManager.Jogar()` e `PauseMenu.MainMenuButton()`, que também são cortes secos) para passar por esse manager.

**Mecânica da prensa → rever questão de sempre acertar.** Olhando `CraftingPress.cs`, o código em si está correto (monta um dicionário de receitas por par de `itemID` e só libera o resultado se a combinação bater). O "sempre acerta" tende a ser questão de balanceamento/conteúdo — quantas receitas erradas possíveis existem vs. quantos itens o jogador carrega ao mesmo tempo — não um bug de código. Se quiserem que existam combinações erradas "tentadoras", isso é configuração de `Recipe` (ScriptableObject) e de quais itens colocam no inventário, não mudança de lógica.

**Falta feedback da prensa.** Esse sim é código: `CombineItems()` só usa `Debug.Log` para "faltam ingredientes", "combinação inválida" e "slot de saída ocupado", e `ProduceItem()`/`ConsumeItem()` não disparam nada visual/sonoro no sucesso. Sugiro expor eventos `OnCraftSuccess` / `OnCraftFail` em `CraftingPress` (mesmo padrão observer já usado no projeto) para a UI/animação/som reagirem, em vez de só logar no console.

**Rever HUD** — o `HUDManager.cs` em si está bem arquitetado (assina `OnStatusChanged`, atualiza sliders e texto). Se o problema for só de layout/visual, é UI/design; se quiserem novos elementos na HUD (ex.: indicador do caso atual, ícone de item-chave), isso é código simples de adicionar ao `AtualizarTela()`.

## Fase 2 — itens de programação

**Feedback na tela de casos indicando qual foi aceito.** `CaseSelectionUI.ConfirmarEscolha()` chama `GameManager.Instance.ConfirmarCaso(casoEscolhido)` e imediatamente `FecharPainel()` — não há nenhum destaque visual de qual carta foi escolhida antes do painel fechar. Dá pra resolver fácil: ao clicar, desabilitar os outros botões, aplicar um highlight/borda na carta escolhida, esperar meio segundo (`Invoke` ou coroutine) e só então fechar o painel.

**Ajustar a câmera.** `CameraFollow.cs` é um lerp simples atrás do jogador, sem nenhum limite de mundo — se o professor está vendo a câmera "vazando" para fora do cenário, é porque não existe clamping. Sugiro adicionar `minBounds`/`maxBounds` (Vector2) configuráveis no Inspector e aplicar `Mathf.Clamp` na posição final antes de atribuir a `transform.position`.

**Falta lógica de progressão de acordo com escolha de casos.** Aqui o framework de código já existe e é sólido: `NPCMovement.reacoesDeCaso` (struct `CasoReacao`), `TrapdoorInteractable.requisitosDeCaso` (struct `RequisitosDoPorao`) e `LootInteractable.pistasPossiveis` (struct `LootDeCaso`) já ramificam diálogo/pistas/acesso por `CaseData`. O que provavelmente falta é: (1) preencher essas listas no Inspector para os 3 casos em cada NPC/porta/estante de cada cena — isso é configuração, mas é você quem mexe nesses componentes; (2) não há nenhum aviso caso um caso fique sem configuração em algum desses componentes (diferente de `CaseData`, `Item` e `Recipe`, que têm `OnValidate` avisando no console). Vale adicionar um `OnValidate` parecido em `NPCMovement` e `TrapdoorInteractable` avisando se `reacoesDeCaso`/`requisitosDeCaso` não cobre um dos casos disponíveis — evita esquecer de configurar um caso e só descobrir em playtest.

## Som — o que falta no código (a parte que é sua, mesmo sem compor os áudios)

Hoje o único lugar do projeto que toca áudio é a voz de diálogo em `DialogueSystem.Next()` (via FMOD `EventReference`). Não existe nenhum outro gatilho de som em nenhum script — nem inventário, nem alçapão, nem hover, nem passos. Ou seja, os itens de som do feedback não são só "faltam os arquivos de áudio": falta o sistema no código que dispararia esses sons quando os áudios existirem.

- **Música/ambiência**: não existe nenhum `MusicManager`/`AmbienceManager`. Sugiro um singleton simples que toca um evento FMOD de música/ambiência ao carregar cada cena (pode escutar `SceneManager.sceneLoaded`, igual o `GameManager` já faz).
- **Som de saída do inventário / alçapão**: `InventoryManager.ToggleInventory()` e `TrapdoorInteractable.Interact()` não chamam nenhum FMOD event. Precisa expor um `EventReference` no Inspector de cada um e chamar `RuntimeManager.PlayOneShot(...)` no momento certo. "Diversificar" o som do alçapão/inventário é mais fácil de resolver do lado do FMOD Studio (multi-instrument com randomização), mas o gatilho em C# ainda precisa existir.
- **Hover do inventário com volume reduzido e pitch randomizado**: `UISlotHandler` hoje só implementa `IPointerClickHandler` — não há `IPointerEnterHandler`. Precisa adicionar essa interface, criar uma instância FMOD no hover, e usar `instance.setVolume(...)` / `instance.setPitch(Random.Range(min, max))` antes do `start()`.
- **Footstep**: `PlayerMove.cs` não tem nenhuma chamada de áudio. Pode ser disparado por Animation Event no clipe de "andando" (chamando um método público tipo `PlayFootstep()`) ou por tempo dentro do próprio `PlayerMove`, checando `move != Vector2.zero`.

**"Às vezes não sai som na voz do personagem — BUG":** esse é o único item já rotulado como bug pelos professores, e faz sentido ser um bug de código. Em `DialogueSystem.Next()`:

```csharp
if (!currentDialogue.dialogueAudio.IsNull) 
{
    currentAudioInstance = FMODUnity.RuntimeManager.CreateInstance(currentDialogue.dialogueAudio);
    currentAudioInstance.start();
    currentAudioInstance.release(); 
}
```

Não há nenhuma verificação de que os bancos do FMOD já terminaram de carregar antes de tocar (`RuntimeManager.HaveAllBanksLoaded`), nem checagem do retorno de `.start()`/`.release()` (`FMOD.RESULT`). Minha suspeita principal é uma corrida de inicialização: se `Next()` é chamado muito cedo (primeira fala logo que a cena carrega), o FMOD pode ainda não ter os bancos prontos, e o `.start()` falha silenciosamente — como o código não verifica o resultado, isso passa despercebido e "às vezes" não sai som (normalmente na primeira fala de uma cena, é o padrão clássico desse tipo de bug). Vale logar o `FMOD.RESULT` retornado por `start()` para confirmar, e considerar esperar `RuntimeManager.HaveAllBanksLoaded` antes de permitir a primeira fala.

## Achados extras (não estão na lista do professor, mas vi lendo o código)

**`Time.timeScale` mexido direto em vários lugares além do `PauseManager`.** Vocês já construíram um `PauseManager` bem feito (contagem de solicitações, para não haver "guerra do timeScale"), mas `InventoryManager.ToggleInventory()`, `PressInteractable.Interact()` (fallback), `PauseMenu.Pausar()/ResumirButton()` e `TrapdoorInteractable.Interact()` ainda setam `Time.timeScale` na mão em vez de usar `PauseManager.RequestPause(...)`. Isso pode causar bugs sutis tipo: abrir o inventário com o menu de pause já aberto, fechar só o inventário, e o jogo despausar sozinho mesmo com o menu de pause ainda na tela. Vale migrar essas chamadas para o `PauseManager` já existente.

**`NPCInvestigacao.cs` está inteiro comentado (dead code).** Não faz nada hoje; se não for mais usado, dá pra deletar o arquivo para não confundir.

## Sugestão de prioridade

Eu começaria pelo bug de áudio (é o único rotulado "BUG" e é rápido de investigar), depois o sistema de transição de cena (usado em vários lugares do feedback ao mesmo tempo — Fase 1 e Fase 2), depois feedback de prensa/inventário/seleção de caso (mesmo padrão de evento, dá pra fazer os três juntos), e por último os gatilhos de som (que dependem de você já ter os `EventReference` do FMOD para apontar, então vale alinhar com quem está fazendo o áudio antes).
