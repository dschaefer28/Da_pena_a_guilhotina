using UnityEngine;

/// <summary>
/// Progresso que vale para o jogo inteiro (tutorial, cutscenes já vistas, dicas) e que um "Novo Jogo" zera.
///
/// Novo Jogo  -> MenuPrincipalManager.Jogar chama ComecarNovoJogo: o tutorial e as cutscenes voltam do zero.
/// Continuar  -> SistemaDeSave.Carregar zera tudo também, mas em seguida marca de volta as chaves que estavam
///               marcadas quando o jogo foi salvo (elas ficam dentro do save).
/// </summary>
public static class ProgressoDoJogo
{
    /// <summary>Chaves do PlayerPrefs que pertencem a uma partida (não inclui opções como volume).</summary>
    public static readonly string[] ChavesDaPartida =
    {
        TutorialManager.CHAVE_TUTORIAL_CONCLUIDO,
        TutorialManager.CHAVE_DICA_FATO_BOATO,
        IntroCutscene.Chave,
        Fase1Desfecho.Chave
    };

    public static void ApagarProgresso()
    {
        foreach (string chave in ChavesDaPartida) PlayerPrefs.DeleteKey(chave);
        PlayerPrefs.Save();
    }

    /// <summary>Zera o progresso e descarta os objetos persistentes de uma partida anterior (quem voltou ao
    /// menu pelo pause), para os da cena Jogo assumirem com os valores iniciais.</summary>
    public static void ComecarNovoJogo()
    {
        ApagarProgresso();
        if (TutorialManager.Instance != null) Object.Destroy(TutorialManager.Instance.gameObject);
        if (GameManager.Instance != null) Object.Destroy(GameManager.Instance.gameObject);
    }
}
