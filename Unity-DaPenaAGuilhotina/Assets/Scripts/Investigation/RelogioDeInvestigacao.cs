using System;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>
/// Orçamento de horas por caso: a primeira interação com cada NPC/objeto custa horas, repetir no mesmo caso é grátis.
/// Só vale na cena de investigação do caso em andamento (CaseData.nextSceneName). Pagamentos ficam no
/// RegistroDaInvestigacao e sobrevivem a troca de cena e save.
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

    public static bool JaPaga(Component alvo, string idDaInteracao)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || alvo == null) return false;
        return gm.registroDaInvestigacao.InteracaoPaga(gm.casoEscolhido, IdDeInteracao.ChaveDaCena(alvo, idDaInteracao)) ||
               gm.registroDaInvestigacao.PagaNoSaveAntigo(gm.casoEscolhido, IdDeInteracao.ChaveAntiga(alvo));
    }

    /// <summary>Falso (com aviso) se não houver horas suficientes.</summary>
    public static bool TentarGastar(Component alvo, string idDaInteracao, int custo)
    {
        if (!Ativo || custo <= 0 || alvo == null) return true;

        GameManager gm = GameManager.Instance;
        RegistroDaInvestigacao registro = gm.registroDaInvestigacao;
        CaseData caso = gm.casoEscolhido;
        string chave = IdDeInteracao.ChaveDaCena(alvo, idDaInteracao);
        if (registro.InteracaoPaga(caso, chave)) return true;

        // Save v2: paga com a chave antiga (cena/nome); migra para a nova sem cobrar.
        string chaveAntiga = IdDeInteracao.ChaveAntiga(alvo);
        if (registro.PagaNoSaveAntigo(caso, chaveAntiga))
        {
            registro.RegistrarPagamento(caso, chave);
            AvisarHomonimos(alvo, chaveAntiga);
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

    // A chave antiga não distingue homônimos: todos contam como pagos; só registra a ambiguidade.
    private static void AvisarHomonimos(Component alvo, string chaveAntiga)
    {
        var ids = new System.Collections.Generic.List<string>();
        foreach (var par in IdDeInteracao.InteracoesDaCena(alvo.gameObject.scene))
            if (IdDeInteracao.ChaveAntiga(par.Key) == chaveAntiga) ids.Add(IdDeInteracao.ChaveDaCena(par.Key, par.Value));
        if (ids.Count > 1)
            Debug.LogWarning($"[SistemaDeSave] Chave antiga ambígua '{chaveAntiga}' (save v2): vale para {string.Join(", ", ids)}. " +
                             "Todas contam como pagas, como no jogo antigo.");
    }
}
