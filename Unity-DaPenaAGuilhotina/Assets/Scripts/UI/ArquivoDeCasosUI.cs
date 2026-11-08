using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Painel "Casos resolvidos" do menu principal: lista os casos concluídos numa partida (um botão por caso, na ordem das
/// fases) e mostra o texto do caso escolhido — relato do cliente, o que o jogador fez e o desfecho (ArquivoDeCasos).
/// As setas trocam entre as partidas guardadas; abre sempre na mais recente.
///
/// Todo o visual (moldura, botões, textos, rolagem) já existe na cena, montado por
/// Ferramentas > Menu principal > Criar painel Casos resolvidos. Este script só preenche os textos e liga/desliga os
/// botões que já estão lá.
/// </summary>
public class ArquivoDeCasosUI : MonoBehaviour
{
    [Header("Lista de casos (um botão por caso, de cima para baixo)")]
    [SerializeField] private Button[] botoesDosCasos;
    [SerializeField] private TMP_Text[] rotulosDosCasos;
    [Tooltip("Cor do botão do caso aberto; os outros ficam com a Cor Normal.")]
    [SerializeField] private Color corSelecionado = Color.white;
    [SerializeField] private Color corNormal = new Color(0.72f, 0.72f, 0.72f, 1f);

    [Header("Detalhe do caso")]
    [SerializeField] private TMP_Text textoDoCaso;
    [SerializeField] private ScrollRect rolagem;

    [Header("Partidas")]
    [SerializeField] private TMP_Text textoDaPartida;
    [SerializeField] private Button botaoPartidaAnterior;
    [SerializeField] private Button botaoPartidaSeguinte;

    [Header("Textos")]
    [TextArea(2, 4)]
    [SerializeField] private string textoSemCasos =
        "Nenhum caso resolvido ainda.\n\nQuando um panfleto sair da prensa, o caso aparece aqui para você reler o relato e o desfecho.";

    private List<ArquivoDeCasos.PartidaArquivada> partidas = new List<ArquivoDeCasos.PartidaArquivada>();
    private readonly List<ArquivoDeCasos.CasoArquivado> casosVisiveis = new List<ArquivoDeCasos.CasoArquivado>();
    private int partidaAtual;
    private int casoAtual;

    private void OnEnable() => Mostrar();

    /// <summary>Relê o arquivo e abre a partida mais recente, no último caso concluído.</summary>
    public void Mostrar()
    {
        partidas = ArquivoDeCasos.Partidas();
        partidaAtual = partidas.Count - 1;
        MostrarPartida(selecionarUltimo: true);
    }

    public void PartidaAnterior()
    {
        if (partidaAtual <= 0) return;
        partidaAtual--;
        MostrarPartida(selecionarUltimo: true);
    }

    public void PartidaSeguinte()
    {
        if (partidaAtual >= partidas.Count - 1) return;
        partidaAtual++;
        MostrarPartida(selecionarUltimo: true);
    }

    /// <summary>Ligado no OnClick de cada botão da lista (índice do botão, de cima para baixo).</summary>
    public void SelecionarCaso(int indice)
    {
        if (indice < 0 || indice >= casosVisiveis.Count) return;
        casoAtual = indice;
        AtualizarSelecao();

        ArquivoDeCasos.PartidaArquivada partida = partidas[partidaAtual];
        if (textoDoCaso != null)
            textoDoCaso.text = ArquivoDeCasos.TextoDoCaso(partida, casosVisiveis[indice], CatalogoDeSave.Instancia);
        VoltarAoTopo();
    }

    private void MostrarPartida(bool selecionarUltimo)
    {
        casosVisiveis.Clear();
        bool vazio = partidas.Count == 0;

        if (!vazio)
        {
            ArquivoDeCasos.PartidaArquivada partida = partidas[partidaAtual];
            CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
            casosVisiveis.AddRange(partida.casos);
            // Na ordem das fases (o tutorial primeiro); casos da mesma fase ficam na ordem em que foram concluídos.
            casosVisiveis.Sort((a, b) => FaseDe(a, catalogo).CompareTo(FaseDe(b, catalogo)));
            if (casosVisiveis.Count > Capacidade)
                Debug.LogWarning($"[ArquivoDeCasosUI] A partida tem {casosVisiveis.Count} casos e o painel só tem {Capacidade} botões; os últimos não aparecem.");

            if (textoDaPartida != null) textoDaPartida.text = ArquivoDeCasos.RotuloDaPartida(partida, partidaAtual + 1, partidas.Count);
        }

        if (textoDaPartida != null) textoDaPartida.gameObject.SetActive(!vazio);
        if (botaoPartidaAnterior != null)
        {
            botaoPartidaAnterior.gameObject.SetActive(partidas.Count > 1);
            botaoPartidaAnterior.interactable = partidaAtual > 0;
        }
        if (botaoPartidaSeguinte != null)
        {
            botaoPartidaSeguinte.gameObject.SetActive(partidas.Count > 1);
            botaoPartidaSeguinte.interactable = partidaAtual < partidas.Count - 1;
        }

        for (int i = 0; i < Capacidade; i++)
        {
            bool usado = i < casosVisiveis.Count;
            botoesDosCasos[i].gameObject.SetActive(usado);
            if (!usado || rotulosDosCasos == null || i >= rotulosDosCasos.Length || rotulosDosCasos[i] == null) continue;
            CaseData caso = CatalogoDeSave.Instancia != null ? CatalogoDeSave.Instancia.BuscarCaso(casosVisiveis[i].caso) : null;
            string fase = caso != null ? $"Fase {caso.fase}" : string.Empty;
            rotulosDosCasos[i].text = $"<size=75%>{fase}</size>\n{ArquivoDeCasos.TituloDo(caso, casosVisiveis[i].caso)}";
        }

        if (vazio || casosVisiveis.Count == 0)
        {
            if (textoDoCaso != null) textoDoCaso.text = textoSemCasos;
            VoltarAoTopo();
            return;
        }

        int primeiro = selecionarUltimo ? Mathf.Min(casosVisiveis.Count, Capacidade) - 1 : 0;
        SelecionarCaso(primeiro);
    }

    private int Capacidade => botoesDosCasos != null ? botoesDosCasos.Length : 0;

    private static int FaseDe(ArquivoDeCasos.CasoArquivado arquivado, CatalogoDeSave catalogo)
    {
        CaseData caso = catalogo != null ? catalogo.BuscarCaso(arquivado.caso) : null;
        return caso != null ? caso.fase : int.MaxValue;
    }

    private void AtualizarSelecao()
    {
        for (int i = 0; i < Capacidade; i++)
        {
            Image fundo = botoesDosCasos[i].targetGraphic as Image;
            if (fundo != null) fundo.color = i == casoAtual ? corSelecionado : corNormal;
        }
    }

    private void VoltarAoTopo()
    {
        if (rolagem == null) return;
        Canvas.ForceUpdateCanvases();
        rolagem.verticalNormalizedPosition = 1f;
    }
}
