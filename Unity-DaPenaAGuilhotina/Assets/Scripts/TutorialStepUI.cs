using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
/// com a etapa atual do TutorialManager (persistente entre cenas). Coloque uma cópia deste script em cada
/// cena que participa do tutorial (Jogo e Porao) — cada uma só precisa configurar as etapas que dizem
/// respeito a ela em "Revelacoes".
///
/// Setup no Editor:
/// 1. No Canvas da cena, monte UM painel de popup reutilizável: um fundo, uma Image (ícone, opcional)
///    e um texto TMP (mensagem) e um Button "Continuar". Deixe o painel desativado por padrão.
/// 2. Coloque este script em qualquer objeto do Canvas e arraste o painel/imagem/texto/botão nos campos abaixo.
/// 3. Na lista "Revelacoes", crie uma entrada para cada etapa do tutorial que acontece NESTA cena e arraste
///    os botões/objetos que devem aparecer quando ela começar (ex: etapaId = "botao_inventario" -> InventoryButton).
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

    [Header("Revelação de UI por Etapa")]
    public List<RevelacaoDeEtapa> revelacoes;

    private string etapaExibidaAtualmente;

    void OnEnable()
    {
        Inscrever();
    }

    void OnDisable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
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

    private void Inscrever()
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        TutorialManager.Instance.OnEtapaAlterada += HandleEtapaAlterada;
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
        AplicarRevelacoes();

        TutorialStep etapa = TutorialManager.Instance != null ? TutorialManager.Instance.ObterEtapa(etapaId) : null;
        Debug.Log($"[TutorialStepUI:{gameObject.scene.name}/{name}] HandleEtapaAlterada('{etapaId}') -> " +
                  $"{(etapa != null ? "vou mostrar o popup" : "etapa nula, escondendo popup (painelPopup atribuído: " + (painelPopup != null) + ")")}.", this);

        if (etapa == null)
        {
            if (painelPopup != null) painelPopup.SetActive(false);
            return;
        }

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

        if (textoMensagem != null) textoMensagem.text = etapa.mensagem;

        painelPopup.SetActive(true);
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
            if (painelPopup != null) painelPopup.SetActive(false);
        }
    }
}
