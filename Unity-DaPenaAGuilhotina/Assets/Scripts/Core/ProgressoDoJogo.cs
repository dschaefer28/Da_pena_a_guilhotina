using UnityEngine;

/// <summary>
/// Progresso da partida em PlayerPrefs (tutorial, cutscenes, dicas). Novo Jogo zera; Continuar zera e
/// restaura as chaves gravadas dentro do save (SistemaDeSave.Carregar).
/// </summary>
public static class ProgressoDoJogo
{
    // Só chaves da partida; opções (volume, velocidade do texto) ficam de fora.
    public static readonly string[] ChavesDaPartida =
    {
        TutorialManager.CHAVE_TUTORIAL_CONCLUIDO,
        TutorialManager.CHAVE_DICA_FATO_BOATO,
        TutorialManager.CHAVE_DICA_TEMPO,
        TutorialManager.CHAVE_DICA_DESPESAS,
        TutorialManager.CHAVE_DICA_BIBLIOTECA,
        TutorialManager.CHAVE_DICA_DEDUCAO,
        TutorialManager.CHAVE_DICA_LINHA_EDITORIAL,
        IntroCutscene.Chave,
        Fase1Desfecho.Chave
    };

    public static void ApagarProgresso()
    {
        foreach (string chave in ChavesDaPartida) PlayerPrefs.DeleteKey(chave);
        PlayerPrefs.Save();
    }

    // Destrói os singletons persistentes de uma partida anterior (volta ao menu pelo pause) para os da
    // cena Jogo assumirem com valores iniciais.
    public static void ComecarNovoJogo()
    {
        ModoDev.EncerrarDemonstracao();
        ApagarProgresso();
        if (TutorialManager.Instance != null) Object.Destroy(TutorialManager.Instance.gameObject);
        if (GameManager.Instance != null) Object.Destroy(GameManager.Instance.gameObject);
    }
}
