using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Liga uma etapa do tutorial (pelo ID) aos objetos desta cena que devem ficar visíveis a partir dela.</summary>
[Serializable]
public class RevelacaoDeEtapa
{
    [Tooltip("Deve ser exatamente igual ao 'Etapa Id' de uma das etapas configuradas no TutorialManager.")]
    public string etapaId;

    [Tooltip("Objetos que ficam visíveis (SetActive true) a partir desta etapa. Ex: o botão de inventário, " +
             "o botão de pause, uma seta apontando para o alçapão, o painel da prensa, etc. " +
             "Deixe estes objetos DESATIVADOS na cena por padrão — o tutorial os ativa na hora certa.")]
    public List<GameObject> objetosParaRevelar;
}

/// <summary>
/// Componente local de cena que mostra o popup do tutorial e revela botões/objetos aos poucos, de acordo
/// com a etapa atual do TutorialManager (persistente entre cenas). Vive no prefab UI (existe em todas as
/// cenas); só a lista "Revelacoes" é configurada por cena, porque aponta para objetos daquela cena.
///
/// Universal (Windows/Android): o texto passa por DispositivoDeControle.Substituir e a etapa pode
/// destacar um controle como "tecla" desenhada (GlifoDeControle). Segue as práticas de tutorial
/// contextual: uma ação por etapa, popup fora do caminho (rodapé), some durante diálogos e cutscenes
/// e volta sozinho, e no PC também avança com Enter/Espaço.
/// </summary>
public class TutorialStepUI : MonoBehaviour
{
    [Header("Popup (reutilizado para todas as etapas desta cena)")]
    public GameObject painelPopup;
    [Tooltip("Opcional: Image usada para mostrar o ícone de cada etapa (TutorialStep.icone).")]
    public Image imagemIcone;
    public TextMeshProUGUI textoMensagem;
    [Tooltip("Botão que avança etapas de 'Clique Do Jogador' e que apenas esconde o popup nas demais etapas " +
             "(o jogo continua esperando a ação real: falar com o NPC, criar o panfleto, etc.).")]
    public Button botaoContinuar;

    [Header("Glifos de controle (opcional)")]
    [Tooltip("Container (com layout vertical) onde as 'teclas' da etapa são desenhadas. Vazio = sem glifos.")]
    public Transform containerGlifos;
    [Tooltip("Fonte das teclas/rótulos. Vazio = fonte do texto da mensagem.")]
    public TMP_FontAsset fonteGlifos;

    [Header("Revelação de UI por Etapa")]
    public List<RevelacaoDeEtapa> revelacoes;

    private string etapaExibidaAtualmente;
    private bool popupDispensado;   // jogador clicou Continuar numa etapa de evento: não reabrir sozinho
    private DialogueSystem dialogoObservado;

    void OnEnable()
    {
        Inscrever();
        CutsceneLegendas.OnTerminou += HandleCutsceneTerminou;
    }

    void OnDisable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        CutsceneLegendas.OnTerminou -= HandleCutsceneTerminou;
        ObservarDialogo(null);
    }

    void Start()
    {
        if (painelPopup == null) Debug.LogWarning("[TutorialStepUI] 'Painel Popup' não foi atribuído no Inspector.", this);
        if (textoMensagem == null) Debug.LogWarning("[TutorialStepUI] 'Texto Mensagem' não foi atribuído no Inspector.", this);

        if (painelPopup != null) painelPopup.SetActive(false);
        if (botaoContinuar != null) botaoContinuar.onClick.AddListener(OnContinuarClicado);

        // Reforça a inscrição aqui (além do OnEnable) para o caso de o TutorialManager ainda
        // não existir/estar pronto no momento em que este objeto foi habilitado.
        Inscrever();

        var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : FindAnyObjectByType<DialogueSystem>();
        ObservarDialogo(dialogo);

        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[TutorialStepUI] Nenhum TutorialManager encontrado na cena. Crie um GameObject com " +
                              "o script 'Tutorial Manager' na cena Jogo (ele é persistente e não precisa existir nas outras cenas).", this);
            return;
        }

        ValidarEtapaIds();

        // Sincroniza imediatamente com o progresso já salvo (ex: o jogador voltou do porão pro
        // escritório e os botões de inventário/pause já ensinados precisam continuar visíveis).
        HandleEtapaAlterada(TutorialManager.Instance.EtapaAtualId);
    }

    void Update()
    {
        // No PC, Enter/Espaço equivalem ao clique em Continuar (o texto da etapa diz isso).
        if (painelPopup == null || !painelPopup.activeSelf || DispositivoDeControle.EhToque) return;
        var teclado = Keyboard.current;
        if (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame))
            OnContinuarClicado();
    }

    private void Inscrever()
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        TutorialManager.Instance.OnEtapaAlterada += HandleEtapaAlterada;
    }

    private void ObservarDialogo(DialogueSystem dialogo)
    {
        if (dialogoObservado != null)
        {
            dialogoObservado.OnDialogueStarted -= HandleDialogoComecou;
            dialogoObservado.OnDialogueEnded -= HandleDialogoTerminou;
        }
        dialogoObservado = dialogo;
        if (dialogoObservado != null)
        {
            dialogoObservado.OnDialogueStarted += HandleDialogoComecou;
            dialogoObservado.OnDialogueEnded += HandleDialogoTerminou;
        }
    }

    // O popup sai da frente enquanto uma conversa ou cutscene estiver na tela e volta depois,
    // se a etapa ainda for a mesma e o jogador não a tiver dispensado.
    private void HandleDialogoComecou() { if (painelPopup != null) painelPopup.SetActive(false); }
    private void HandleDialogoTerminou() => ReexibirSePrecisar();
    private void HandleCutsceneTerminou() => ReexibirSePrecisar();

    private void ReexibirSePrecisar()
    {
        if (popupDispensado || TutorialManager.Instance == null || CutsceneLegendas.EmExibicao) return;
        TutorialStep etapa = TutorialManager.Instance.ObterEtapa(etapaExibidaAtualmente);
        if (etapa != null && etapa.etapaId == TutorialManager.Instance.EtapaAtualId) MostrarPopup(etapa);
    }

    // Avisa no Console se algum "Etapa Id" digitado em Revelacoes não bate com nenhuma etapa
    // real do TutorialManager — a causa mais comum de "o botão não aparece" é um erro de digitação aqui.
    private void ValidarEtapaIds()
    {
        if (revelacoes == null) return;
        foreach (var revelacao in revelacoes)
        {
            if (revelacao == null || string.IsNullOrEmpty(revelacao.etapaId)) continue;
            if (TutorialManager.Instance.ObterEtapa(revelacao.etapaId) == null)
            {
                Debug.LogWarning($"[TutorialStepUI] O 'Etapa Id' \"{revelacao.etapaId}\" configurado em Revelacoes não " +
                                  "existe na lista de Etapas do TutorialManager. Confira a grafia (maiúsculas/minúsculas e espaços contam).", this);
            }
        }
    }

    private void HandleEtapaAlterada(string etapaId)
    {
        etapaExibidaAtualmente = etapaId;
        popupDispensado = false;
        AplicarRevelacoes();

        TutorialStep etapa = TutorialManager.Instance != null ? TutorialManager.Instance.ObterEtapa(etapaId) : null;
        if (etapa == null)
        {
            if (painelPopup != null) painelPopup.SetActive(false);
            return;
        }

        // Numa conversa ou cutscene, espera terminar (ReexibirSePrecisar) em vez de aparecer por cima.
        bool dialogoAberto = dialogoObservado != null && dialogoObservado.IsDialogueActive;
        if (dialogoAberto || CutsceneLegendas.EmExibicao) { if (painelPopup != null) painelPopup.SetActive(false); return; }

        MostrarPopup(etapa);
    }

    // Revela permanentemente (nunca esconde) tudo que pertence a uma etapa já alcançada — não só a etapa
    // exata que acabou de começar — para sobreviver a troca de cena (Jogo -> Porao -> Jogo de novo).
    private void AplicarRevelacoes()
    {
        if (revelacoes == null || TutorialManager.Instance == null) return;

        foreach (var revelacao in revelacoes)
        {
            if (revelacao == null || revelacao.objetosParaRevelar == null) continue;
            if (!TutorialManager.Instance.EtapaJaFoiAlcancada(revelacao.etapaId)) continue;

            foreach (var objeto in revelacao.objetosParaRevelar)
            {
                if (objeto != null) objeto.SetActive(true);
            }
        }
    }

    private void MostrarPopup(TutorialStep etapa)
    {
        if (painelPopup == null)
        {
            Debug.LogWarning($"[TutorialStepUI:{gameObject.scene.name}/{name}] Deveria mostrar a etapa '{etapa.etapaId}' mas 'Painel Popup' está vazio no Inspector.", this);
            return;
        }

        if (imagemIcone != null)
        {
            imagemIcone.sprite = etapa.icone;
            imagemIcone.gameObject.SetActive(etapa.icone != null);
        }

        if (textoMensagem != null) textoMensagem.text = DispositivoDeControle.Substituir(etapa.mensagem);

        MontarGlifos(etapa.controle);

        painelPopup.SetActive(true);
    }

    private void MontarGlifos(ControleTutorial controle)
    {
        if (containerGlifos == null) return;

        for (int i = containerGlifos.childCount - 1; i >= 0; i--)
        {
            // Destroy só age no fim do frame; desativar antes evita a linha antiga aparecer junto da nova.
            GameObject antigo = containerGlifos.GetChild(i).gameObject;
            antigo.SetActive(false);
            Destroy(antigo);
        }

        TMP_FontAsset fonte = fonteGlifos != null ? fonteGlifos : (textoMensagem != null ? textoMensagem.font : null);
        bool algum = false;
        if (controle == ControleTutorial.Principais)
        {
            // Três linhas no cartão: teclas menores para caberem na coluna do popup.
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Mover, fonte, 36f);
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Interagir, fonte, 36f);
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Inventario, fonte, 36f);
            algum = true;
        }
        else if (controle != ControleTutorial.Nenhum)
        {
            GlifoDeControle.Criar(containerGlifos, controle, fonte);
            algum = true;
        }
        containerGlifos.gameObject.SetActive(algum);
    }

    private void OnContinuarClicado()
    {
        if (TutorialManager.Instance == null) return;

        TutorialStep etapa = TutorialManager.Instance.ObterEtapa(etapaExibidaAtualmente);
        if (etapa != null && etapa.tipoDeAvanco == TipoDeAvanco.CliqueDoJogador)
        {
            TutorialManager.Instance.CompletarEtapaAtual();
        }
        else
        {
            // Etapas que avançam por evento/cena: o botão só esconde a mensagem; o TutorialManager
            // continua esperando a ação real acontecer para avançar de fato.
            popupDispensado = true;
            if (painelPopup != null) painelPopup.SetActive(false);
        }
    }
}
