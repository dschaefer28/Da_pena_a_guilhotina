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

    /// <summary>Cobra o tempo de uma interação. Falso (com aviso) se não houver horas suficientes.</summary>
    public static bool TentarGastar(Component alvo, int custo)
    {
        if (!Ativo || custo <= 0 || alvo == null) return true;

        GameManager gm = GameManager.Instance;
        string chave = SceneManager.GetActiveScene().name + "/" + alvo.gameObject.name;
        if (gm.interacoesPagas.Contains(chave)) return true;

        if (gm.horasRestantes < custo)
        {
            AvisoNaTela.Mostrar(gm.horasRestantes <= 0
                ? "Já anoiteceu. Não há mais tempo para investigar hoje: volte à tipografia."
                : $"Não há tempo para isso hoje (precisa de {custo}h, resta {gm.horasRestantes}h).");
            return false;
        }

        gm.horasRestantes -= custo;
        gm.interacoesPagas.Add(chave);
        OnHorasMudaram?.Invoke();

        if (gm.horasRestantes == 0)
            AvisoNaTela.Mostrar("O sol se põe sobre Paris. Não há mais tempo para investigar: volte à tipografia.");
        return true;
    }

    public static void NotificarMudanca() => OnHorasMudaram?.Invoke();
}
