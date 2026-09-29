# Relatório de testes — Da Pena à Guilhotina

**Data:** 28/09/2026 · **Versão testada:** `main` em `c95e055`, sem alterações locais · **Unity** 6000.3.9f1 · **Plataforma do build:** Android
**Escopo:** jogo inteiro depois do Prompt 7 e do conteúdo do GDD. Nenhum arquivo do projeto foi alterado.
**Capturas:** os arquivos `shots/*.png` citados abaixo não estão no repositório; ficaram com quem fez o teste.

---

## 1. Veredito

A lógica da campanha está correta. Em mais de 200 mil combinações simuladas com o código real do jogo e em três campanhas jogadas do menu até o fim, não houve nenhuma inconsistência de regra nem erro no console, e os **6 finais** (rotas A/B/C × réu absolvido/condenado) são alcançáveis.

O que impede a entrega está fora da lógica: **o menu não tem o botão "Continuar"** e **o build Android gira para retrato**. Depois vêm duas janelas com layout quebrado (Biblioteca e Mesa de Casos) e o balanceamento da economia e das rotas, que depende de decisão do grupo.

---

## 2. O que foi testado

| Teste | Como | Resultado |
|---|---|---|
| Testes automáticos | Test Runner, EditMode | **90/90** aprovados |
| Validador da campanha | Ferramentas > Campanha > 2 | "Campanha válida", 0 avisos (9 casos, dedução em 9, 6h por caso) |
| IDs de interação | Ferramentas > Investigação > Validar | 0 problemas |
| Itens de caso | varredura dos 62 itens com caso | todos com nome, descrição, fonte e ícone |
| **Todas as combinações** | simulação com o código real (`GameManager`, `CalculadoraDePanfleto`, `CheckpointDoTribunal`, `TribunalManager`) e os assets reais: 3 casos da F2 × 15 pares de itens × 3 linhas editoriais × 3 casos da F3 × 15 pares × 3 linhas × apoio (nenhum / Biblioteca / NPC) | **34.200 caminhos** até a rota e **173.385 variações** da Fase 4: 0 inconsistências; rota travada; Dupaty libera sempre que deve; réu coerente com a versão |
| Campanha 1 (Play Mode, do menu) | Novo Jogo → tutorial inteiro → Colar da Rainha (Fatos, dedução conferida) → Danton (boato + sensacionalista) → rota **A** → Desmoulins (Fatos + Biblioteca) → tribunal | **Final A, réu absolvido** |
| Campanha 2 | Continuar do save pós-tutorial → Réveillon (boato + Agradar a Coroa) → Kornmann (Fatos + etapa complementar do NPC) → rota **B** → Mercador (calúnia) → tribunal | **Final B, réu condenado** |
| Campanha 3 | Continuar → Operário só com as cartas do cliente → Kornmann → rota **C** com dívida (5h) → Girondinos (Fatos + notas de Vergniaud) → tribunal | **Final C, réu absolvido** |
| Finais restantes | tribunal com estado controlado | A-condenado, B-absolvido e C-condenado corretos |
| Situações de borda | jogadas no Play Mode | Pular tutorial; pause → Menu; sair do porão com o panfleto na saída da prensa; relógio zerado (recusa com aviso); compra com saldo exato e clique duplo (cobra uma vez); compra endividado (recusada); apoio equivalente não é vendido; Continuar de um save da Fase 4 |
| Console | todas as sessões | nenhum erro |

Entradas usadas no Play Mode: teclado simulado pelo Input System (A/D, E, I, Enter, Esc) e os `onClick` dos botões reais da UI. Capturas em 1920×1080.

---

## 3. O que corrigir, por urgência

### 🔴 P0 — Crítico (bloqueia a entrega)

**1. O menu não tem botão "Continuar".**
- O menu tem só Jogar, Configuração, Créditos e Sair. Em `MenuPrincipalManager`, o campo `botaoContinuar` está vazio e nenhum botão chama `Continuar()`. O jogo grava o save (fim do tutorial, fim de cada fase, ida ao tribunal), mas o jogador não tem como carregá-lo.
- "Jogar" chama `ComecarNovoJogo()` sem confirmação: apaga as chaves de progresso, e o save antigo é sobrescrito no próximo ponto de save.
- **Origem:** o commit `27cfe24` ("menu", 25/09) removeu o `ContinuarButton` da cena; ele existia em `05dd985`.
- **O carregamento em si funciona:** testei `SistemaDeSave.Carregar()` com o save pós-tutorial e com um save da Fase 4 (estado, Marie ausente, tutorial e cutscenes corretos). Só falta o botão.
- **Correção:** recriar o botão em `Assets/Scenes/menu principal.unity`, com OnClick em `MenuPrincipalManager.Continuar` e arrastado para o campo "Botao Continuar". Pedir confirmação no "Jogar" quando `SistemaDeSave.ExisteSave`.
- Captura: `shots/00_menu.png`

**2. No Android o jogo gira para retrato e fica injogável.**
- Player Settings (Android): Default Orientation = *Auto Rotation*, com **Portrait** e **Portrait Upside Down** permitidos. A UI é só para paisagem (referência 1920×1080). Em retrato, a câmera fica com zoom enorme e a HUD minúscula.
- **Correção:** Player Settings > Android > Resolution and Presentation → Default Orientation = *Landscape Left* (ou Auto Rotation só com Landscape Left/Right).
- Captura: `shots/41_fase2_retrato.png`

### 🟠 P1 — Alta (antes da banca e dos testes com jogadores)

**3. Janela da Biblioteca com layout quebrado, já em 1920×1080.**
- O botão "Comprar" quebra a palavra ("COM/PRAR"), o preço aparece como "25 DE OUR/O", o estado como "Dispon/ível" e "Termine o caso atual primeir/o", e "Ouro insuficie…" sai cortado. O botão Fechar tem 200×267 px e sobra uma faixa vazia sob o título.
- **Causa:** em `BibliotecaUI.CriarLinha`, o preço (`preferredWidth = 260`) e o botão (`preferredWidth = 220`) não têm `minWidth`. A descrição longa tem `flexibleWidth = 1` e largura preferida enorme, então o HorizontalLayoutGroup encolhe as colunas fixas para 80–100 px. No cabeçalho, o título com quebra de linha infla a altura, e o Fechar estica junto.
- **Correção:** `minWidth` de ~240 no preço e ~200 no botão (o Quadro de pistas já faz isso, com `minWidth = 190`); altura fixa e `childForceExpandHeight = false` no cabeçalho.
- As regras funcionam: saldo insuficiente, saldo exato, clique duplo, dívida e apoio equivalente se comportam como esperado.
- Capturas: `shots/19_biblioteca.png`, `shots/24_biblioteca_f4.png`

**4. Mesa de Casos pouco legível.**
- Os cartões ocupam a largura toda, com fundo cinza-claro e texto bege-claro (baixo contraste). Em 1080p cabe um cartão e meio: o "Aceitar Caso" do 2º fica cortado e o 3º só aparece rolando, sem indicação de que há rolagem. O "Fechar" usa outra fonte (padrão), diferente do resto da UI.
- A mesa funciona: os estados disponível, bloqueado, em andamento e concluído estão certos, e a Fase 4 mostra só o caso da rota.
- **Correção:** fundo escuro e opaco nos cartões; cartões mais baixos ou em colunas; indicador de rolagem; fonte Cinzel no Fechar.
- Captura: `shots/09_mesa_f2.png`

**5. A economia pune o jogador honesto pró-povo (decisão do grupo).** Números da simulação:
- Depois da Fase 2 publicando Fatos (linha Defesa ou Agradar): Colar = 15 de ouro, Operário = 5, Réveillon = 45. Os documentos da Fase 3 custam 20–25. Quem joga honesto a favor do povo não consegue comprar nada na Biblioteca: **45,7%** de todas as tentativas de compra na Fase 3 são recusadas por saldo. O exemplo-vitrine do Prompt 3 (Danton + "Folhas da petição") fica inalcançável depois do Colar ou do Operário, a menos que o jogador tenha escolhido Sensacionalista.
- Ao fim da Fase 3, esse mesmo jogador chega endividado: Operário + Danton −15, Operário + Sirven −10, Colar + Danton −5. Começa a Fase 4 com 5h e sem Biblioteca. Quem faz Réveillon + Kornmann (pró-Estado) chega com 35 a 95 de ouro.
- A dívida quase não pesa: é −1h num orçamento de 6h, com caminho mínimo de 1–2h e o quadro inteiro em 4h. O relógio praticamente nunca aperta.
- O ouro não influi em nenhum final e só acumula (chega a 285).
- **Sugestões:** baixar os preços da F3 (≤ 15) ou a despesa da F2; dar mais ouro às receitas pró-povo; dívida de −2h ou orçamento de 5h; dar algum uso ao ouro no desfecho.

**6. A rota é decidida quase só pela escolha da Fase 2 (decisão do grupo).**
- Distribuição nos 34.200 caminhos: **A 47,4% · B 34,6% · C 18,0%**.
- O tutorial termina em Povo 65 × Estado 40 (+25): o jogador já está na zona da rota A antes de qualquer escolha (margem de 20).
- Colar ou Operário, seguidos de Danton ou Sirven, dão rota A em ~95% dos caminhos (com Danton, 99%: 2.980 A × 35 C; com Sirven, 91%). Jogando honesto nesses casos, dá sempre A, com qualquer linha editorial.
- O primeiro caso pró-povo já satura o Povo em 100 (Colar Fatos + Defesa: aplica +35 de +60). Depois disso, publicar a favor do povo não move mais a barra; só as penalidades movem.
- **Sugestões:** tutorial neutro ou margem maior; valores menores (±10 a 30) para as barras não saturarem; confirmar se a rota C deve ser a mais rara.

### 🟡 P2 — Média

**7. Um save antigo (v6) retoma o tutorial na etapa errada.**
- O save de 26/09 que está na sua máquina (Fase 2, etapa 7) carrega, mas o tutorial mostra "Com as duas pistas… desça ao porão". O alçapão responde com o pensamento "faltam pistas", porque o caso do tutorial já foi impresso. O tutorial só destrava quando o jogador aceita um caso e desce, e aí repete as etapas da prensa na Fase 2.
- Saves feitos na versão atual não têm o problema, porque gravam na etapa 14.
- **Correção:** ao carregar, se `GameManager.TutorialConcluido`, avançar o tutorial pelo menos até `mesa_de_casos`. Ou só descartar os saves de teste antigos.
- Captura: `shots/35_save_antigo_usuario.png`

**8. O pop-up do tutorial vaza da moldura com o inventário aberto.**
- Com o inventário aberto, o pop-up vai para o canto esquerdo e encolhe para ~530 px. Nas etapas com glifo ("E Interagir"), o texto passa da borda (ex.: "Feche o inventário e volte até Charles Dupaty…").
- **Correção** em `TutorialStepUI.AfastarDoInventario`: esconder ou empilhar os glifos no modo compacto, ou crescer em altura em vez de estreitar.
- Captura: `shots/04_inventario.png`

**9. Dá para terminar o jogo sem investigar nada.**
- Basta aceitar o caso, descer ao porão e imprimir as duas cartas do cliente: o caso é concluído com 0h gastas (testado no Operário). Vale em todas as fases, inclusive na 4, onde o Dupaty libera o tribunal.
- É a "saída sem bloqueio" do design, mas hoje ela não custa nada. **Sugestão:** liberar a versão Só Alegações só com as horas esgotadas, ou exigir ao menos uma pista.

**10. A dedução é resolvível só pela fonte.**
- Nos 9 casos, todo boato começa com "Segundo X…" (ouvir dizer), toda calúnia vem de fonte anônima ("Cartaz sem assinatura", "Bilhete…", "Denúncia anônima") e todo fato é documento ou testemunha direta. Com a conferência grátis (no máximo 4 combinações), o risco de imprimir boato desaparece depois do primeiro caso.
- Pode ser intencional, para ensinar crítica de fontes. Se for, avaliar um custo por conferência ou variar mais as fontes.

**11. O boato publicado na Fase 4 nunca é revelado.**
- A revelação é agendada, mas não há fim de fase na F4: ela nunca é aplicada nem mostrada e fica pendente no save para sempre (visto no caso do Mercador). A única consequência é o réu condenado.
- **Decidir:** mostrar na cutscene final ou não agendar revelação na Fase 4.

**12. No Porão, a câmera mostra área vazia cinza** à esquerda e à direita da sala. Provável `CameraConfiner` ausente (já citado no STATUS §3.2). Captura: `shots/05_porao.png`

**13. O panfleto é o mesmo em todas as versões.** Fatos, Com Boato, Calúnia e Só Alegações geram o mesmo item, com o mesmo nome e ícone: nem o jogador nem o tribunal veem diferença entre um panfleto honesto e um calunioso. Os panfletos da Fase 2 também têm outro padrão de nome ("Panfleto: X") em relação aos títulos do GDD das Fases 3 e 4.

### 🟢 P3 — Baixa (acabamento)

14. O "E" da porta aparece sobre o relógio ao chegar nas ruas (`shots/11_fase2.png`).
15. O pop-up "Você recebeu" cobre o título do inventário (`shots/07_panfleto.png`).
16. As janelas do Quadro de pistas, da Biblioteca e da Linha editorial são translúcidas: o cenário aparece por trás.
17. No tribunal, as provas já apresentadas não se distinguem das outras, e o "Encerrar a defesa" parece texto solto, não botão.
18. O veredito da rota B com réu condenado não diz que ele foi condenado ("o tribunal decidirá conforme o interesse da República"); só a tela final diz.
19. No caso dos Girondinos, o veredito fala em "o acusado", no singular (pendência já citada no STATUS §18.7).
20. Não há *FMOD Studio Listener* no menu nem no Tribunal (aviso no console).
21. O fim de jogo não fica registrado: o save continua parado antes do tribunal.
22. Avisos antigos no console: `Caso_Tutorial` sem título e sem rota de diálogo; `FindObjectOfType` obsoleto no `GameManager`; os testes EditMode deixam Items na memória, o que gera ~25 avisos ao dar Play depois de rodá-los.
23. `PauseMenu.MainMenuButton` carrega "Menu principal", mas a cena se chama "menu principal". Funciona hoje, porque a busca por nome não diferencia maiúsculas, mas vale padronizar.
24. Textos: a legenda do fim da Fase 2 ("Paris, 1789. A Bastilha caiu…") abre a Fase 3, que se passa em 1791–92; a dica do relógio diz "não dá tempo de investigar tudo", mas 6h cobrem 6 dos 7–8 lugares.

---

## 4. O que funciona (validado neste teste)

Tutorial completo com teclado, inclusive a pausa sobre o pop-up e a pausa durante a cutscene · Marie some e Dupaty silencia depois do tutorial · relógio de 6h (5h endividado), repetir não cobra, 0h recusa · quadro de dedução (marcação errada ou parcial dá resposta genérica, certa confirma e trava) · linha editorial (Cancelar não consome nada; sensacionalista agrava a penalidade em 50%) · revelações aplicadas no fim da fase, antes da rota · despesas de 25 e 50 · rota travada e mesa da Fase 4 com um cartão só · etapas complementares de Kornmann e Vergniaud · Biblioteca (regras de compra) · arquivamento de sobras · panfleto esquecido na saída da prensa volta ao inventário · Dupaty recusa antes do panfleto e libera depois · tribunal (mínimo de 2 provas para encerrar, barra da rota, explosão na A, 6 desfechos) · voltar ao menu · Continuar (pela função) · pular tutorial · pause → menu.

---

## 5. Não coberto por este teste

- Aparelho Android real, toque físico e área segura com notch.
- Áudio: o FMOD roda, mas o som não foi ouvido.
- Resoluções além de 1920×1080 e retrato.
- Telas de Configuração (volume, velocidade do texto) e Créditos do menu.
- Leitura integral das falas: percorri ramos de 8 casos no Play Mode; o de Sirven só pela simulação e pelo validador.
- Inventário cheio em runtime (coberto pelos testes EditMode).

---

## 6. Efeitos no seu ambiente

- `save.json`: copiado antes dos testes, usado durante as campanhas e **restaurado com MD5 idêntico** (`7511C8D6…`, 26/09 02:42).
- PlayerPrefs: restauradas. Só os contadores de sessão da própria Unity mudaram.
- `TestResults.xml` (persistentDataPath) foi regravado pelo Test Runner.
- Game View de volta a "16:9 Landscape"; `runInBackground` desligado; o console foi limpo no início do Play.
- Git sem nenhuma alteração; nada commitado.
- A ferramenta UnityMCP não conectou no início da sessão; o Editor foi controlado pelo endpoint HTTP do mesmo servidor MCP (`127.0.0.1:8080`).
