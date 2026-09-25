using System;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// Documento (Fase 2, "Dificuldade de Investigação"): o risco de "conversar com as pessoas erradas" vem do tempo.
/// Cada caso tem um orçamento de horas (GameManager.horasDoCaso). A primeira conversa com cada NPC e cada objeto
/// investigado gastam horas; repetir a mesma interação no mesmo caso é de graça. Sem horas, o jogador precisa
/// voltar à tipografia com as pistas que conseguiu.
///
/// Só vale na cena de investigação do caso em andamento (CaseData.nextSceneName): no escritório, no porão e no
/// tutorial as interações não custam nada, sem precisar configurar as cenas.
///
/// O que já foi pago fica em GameManager.registroDaInvestigacao por caso + "cena/Id Da Interacao" (IdDeInteracao):
/// sair e voltar da cena, salvar/carregar e reabrir a UI não cobram de novo; homônimos têm IDs próprios.
/// </summary>
public static class RelogioDeInvestigacao
{
    public static event Action OnHorasMudaram;

    public static bool Ativo
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.horasDoCaso > 0 && gm.CasoAtualEmAndamento &&
                   gm.casoEscolhido.nextSceneName == SceneManager.GetActiveScene().name;
        }
    }

    /// <summary>Verdadeiro se esta interação já foi paga no caso atual (repetir não custa nada).</summary>
    public static bool JaPaga(Component alvo, string idDaInteracao)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || alvo == null) return false;
        return gm.registroDaInvestigacao.InteracaoPaga(gm.casoEscolhido, IdDeInteracao.ChaveDaCena(alvo, idDaInteracao)) ||
               gm.registroDaInvestigacao.PagaNoSaveAntigo(gm.casoEscolhido, IdDeInteracao.ChaveAntiga(alvo));
    }

    /// <summary>Cobra o tempo de uma interação. Falso (com aviso) se não houver horas suficientes.</summary>
    public static bool TentarGastar(Component alvo, string idDaInteracao, int custo)
    {
        if (!Ativo || custo <= 0 || alvo == null) return true;

        GameManager gm = GameManager.Instance;
        RegistroDaInvestigacao registro = gm.registroDaInvestigacao;
        CaseData caso = gm.casoEscolhido;
        string chave = IdDeInteracao.ChaveDaCena(alvo, idDaInteracao);
        if (registro.InteracaoPaga(caso, chave)) return true;

        // Save v2: a interação foi paga com a chave antiga (cena/nome). Converte para a chave nova sem cobrar.
        if (registro.PagaNoSaveAntigo(caso, IdDeInteracao.ChaveAntiga(alvo)))
        {
            registro.RegistrarPagamento(caso, chave);
            return true;
        }

        if (gm.horasRestantes < custo)
        {
            AvisoNaTela.Mostrar(gm.horasRestantes <= 0
                ? "Já anoiteceu. Não há mais tempo para investigar hoje: volte à tipografia."
                : $"Não há tempo para isso hoje (precisa de {custo}h, resta {gm.horasRestantes}h).");
            return false;
        }

        gm.horasRestantes -= custo;
        registro.RegistrarPagamento(caso, chave);
        OnHorasMudaram?.Invoke();

        if (gm.horasRestantes == 0)
            AvisoNaTela.Mostrar("O sol se põe sobre Paris. Não há mais tempo para investigar: volte à tipografia.");
        return true;
    }

    public static void NotificarMudanca() => OnHorasMudaram?.Invoke();
}
