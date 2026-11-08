using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tela de saves do menu principal (aberta pelo JOGAR): um cartão por slot do SistemaDeSave.
/// Slot vazio: NOVO JOGO (a partida passa a gravar nele). Slot ocupado: CARREGAR (volta ao ponto salvo e continua
/// gravando nele) e EXCLUIR (pede confirmação). Um jogo já concluído no tribunal não carrega; só pode ser excluído.
///
/// Todo o visual (moldura, cartões, botões, textos e a pergunta de exclusão) já existe na cena, montado por
/// Ferramentas > Menu principal > Criar tela de Saves. Este script só preenche os textos e liga/desliga os botões.
/// </summary>
public class SlotsDeSaveUI : MonoBehaviour
{
    [Serializable]
    public class CartaoDoSlot
    {
        public TMP_Text titulo;
        public TMP_Text detalhe;
        public Button botaoCarregar;
        public Button botaoNovoJogo;
        public Button botaoExcluir;
    }

    [SerializeField] private MenuPrincipalManager menu;
    [Tooltip("Um cartão por slot, de cima para baixo (o primeiro é o slot 1).")]
    [SerializeField] private CartaoDoSlot[] cartoes;

    [Header("Pergunta de exclusão")]
    [Tooltip("Painel \"Excluir este save?\": substitui a tela de saves enquanto está aberto.")]
    [SerializeField] private GameObject painelConfirmarExclusao;
    [SerializeField] private TMP_Text tituloConfirmarExclusao;
    [Tooltip("Recebe o foco do teclado quando a pergunta abre (o \"Não\", para o Enter não apagar o save).")]
    [SerializeField] private GameObject botaoNaoExcluir;

    [Header("Textos")]
    [SerializeField] private string textoVazio = "Vazio";
    [SerializeField] private string textoIlegivel = "Save danificado: não pode ser carregado.";
    [SerializeField] private string textoConcluido = "Jogo concluído";
    [SerializeField] private string formatoPergunta = "EXCLUIR O SAVE {0}?";

    private int slotParaExcluir;

    private void OnEnable() => Mostrar();

    /// <summary>Relê os slots e preenche os cartões.</summary>
    public void Mostrar()
    {
        if (cartoes == null) return;
        GameObject primeiroBotao = null;
        for (int i = 0; i < cartoes.Length; i++)
        {
            CartaoDoSlot c = cartoes[i];
            if (c == null) continue;
            int slot = i + 1;
            bool existe = slot <= SistemaDeSave.NumeroDeSlots && SistemaDeSave.ExisteSlot(slot);
            SistemaDeSave.DadosDeSave dados = existe ? SistemaDeSave.LerSlot(slot) : null;
            bool podeCarregar = SistemaDeSave.PodeContinuar(dados);

            if (c.titulo != null) c.titulo.text = $"SAVE {slot}";
            if (c.detalhe != null) c.detalhe.text = !existe ? textoVazio : dados == null ? textoIlegivel : Descricao(dados);

            Ativar(c.botaoNovoJogo, !existe && slot <= SistemaDeSave.NumeroDeSlots);
            Ativar(c.botaoCarregar, existe);
            if (c.botaoCarregar != null) c.botaoCarregar.interactable = podeCarregar;
            Ativar(c.botaoExcluir, existe);

            if (primeiroBotao == null)
            {
                if (existe && podeCarregar && c.botaoCarregar != null) primeiroBotao = c.botaoCarregar.gameObject;
                else if (!existe && c.botaoNovoJogo != null) primeiroBotao = c.botaoNovoJogo.gameObject;
            }
        }

        EventSystem eventos = EventSystem.current;
        if (eventos != null && primeiroBotao != null) eventos.SetSelectedGameObject(primeiroBotao);
    }

    /// <summary>Ligado no OnClick do NOVO JOGO de cada cartão (índice do cartão, de cima para baixo).</summary>
    public void NovoJogo(int indice)
    {
        if (menu == null || SistemaDeSave.ExisteSlot(indice + 1)) return; // slot ocupado: só carrega ou exclui
        menu.ComecarNovoJogoNoSlot(indice + 1);
    }

    /// <summary>Ligado no OnClick do CARREGAR de cada cartão.</summary>
    public void Carregar(int indice)
    {
        if (menu == null) return;
        if (!menu.CarregarSlot(indice + 1)) Mostrar(); // não deu para carregar: o cartão mostra o porquê
    }

    /// <summary>Ligado no OnClick do EXCLUIR de cada cartão: abre a pergunta.</summary>
    public void PedirExclusao(int indice)
    {
        slotParaExcluir = indice + 1;
        if (painelConfirmarExclusao == null)
        {
            ConfirmarExclusao();
            return;
        }
        if (tituloConfirmarExclusao != null) tituloConfirmarExclusao.text = string.Format(formatoPergunta, slotParaExcluir);
        gameObject.SetActive(false);
        painelConfirmarExclusao.SetActive(true);
        EventSystem eventos = EventSystem.current;
        if (eventos != null && botaoNaoExcluir != null) eventos.SetSelectedGameObject(botaoNaoExcluir);
    }

    /// <summary>"Sim" da pergunta.</summary>
    public void ConfirmarExclusao()
    {
        if (slotParaExcluir > 0) SistemaDeSave.ExcluirSlot(slotParaExcluir);
        slotParaExcluir = 0;
        FecharPergunta();
        if (menu != null) menu.AtualizarContinuar();
    }

    /// <summary>"Não" da pergunta.</summary>
    public void CancelarExclusao()
    {
        slotParaExcluir = 0;
        FecharPergunta();
    }

    private void FecharPergunta()
    {
        if (painelConfirmarExclusao != null) painelConfirmarExclusao.SetActive(false);
        gameObject.SetActive(true); // OnEnable relê os slots
    }

    private string Descricao(SistemaDeSave.DadosDeSave dados)
    {
        string quando = DataLegivel(dados.dataHora);
        if (dados.jogoConcluido)
        {
            string desfecho = string.IsNullOrWhiteSpace(dados.desfechoTitulo) ? textoConcluido : $"{textoConcluido}: {dados.desfechoTitulo.Trim()}";
            return Juntar(desfecho, quando);
        }

        string fase = $"Fase {Mathf.Max(1, dados.faseAtual)}";
        string caso = TituloDoCaso(dados);
        return Juntar(string.IsNullOrEmpty(caso) ? fase : $"{fase} · {caso}", quando);
    }

    private static string Juntar(string linha1, string quando) =>
        string.IsNullOrEmpty(quando) ? linha1 : $"{linha1}\n<size=80%>Salvo em {quando}</size>";

    private static string TituloDoCaso(SistemaDeSave.DadosDeSave dados)
    {
        if (dados.faseAtual <= 1) return "Tutorial";
        if (string.IsNullOrEmpty(dados.casoEscolhido)) return null;
        CatalogoDeSave catalogo = CatalogoDeSave.Instancia;
        CaseData caso = catalogo != null ? catalogo.BuscarCaso(dados.casoEscolhido) : null;
        // Só o caso da fase atual: o escolhido da fase anterior fica no save até o jogador aceitar outro.
        if (caso == null || caso.fase != dados.faseAtual || string.IsNullOrWhiteSpace(caso.caseTitle)) return null;
        return caso.caseTitle.Trim();
    }

    // O save grava "yyyy-MM-dd HH:mm"; a tela mostra "dd/MM/yyyy HH:mm".
    private static string DataLegivel(string dataHora)
    {
        if (string.IsNullOrEmpty(dataHora)) return null;
        return DateTime.TryParseExact(dataHora, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
            ? d.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : dataHora;
    }

    private static void Ativar(Button b, bool ativo)
    {
        if (b != null) b.gameObject.SetActive(ativo);
    }
}
