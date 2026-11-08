using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Relatório de testes de 28/09, P0 1 e 2. Usa a cena "menu principal" real: o botão Continuar e a pergunta do Novo Jogo
/// precisam continuar ligados ao MenuPrincipalManager (o Continuar já foi apagado sem aviso numa edição do menu, commit
/// 27cfe24). E o build do celular não pode girar para retrato.
/// </summary>
public class MenuPrincipalTests
{
    private const string CenaMenu = "Assets/Scenes/menu principal.unity";

    private static void ComMenu(System.Action<MenuPrincipalManager> acao)
    {
        Scene cena = SceneManager.GetSceneByPath(CenaMenu);
        bool abriu = false;
        if (!cena.isLoaded) { cena = EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Additive); abriu = true; }
        try
        {
            MenuPrincipalManager menu = null;
            foreach (GameObject raiz in cena.GetRootGameObjects())
                if (menu == null) menu = raiz.GetComponentInChildren<MenuPrincipalManager>(true);
            Assert.IsNotNull(menu, "MenuPrincipalManager não está na cena do menu.");
            acao(menu);
        }
        finally
        {
            if (abriu) EditorSceneManager.CloseScene(cena, true);
        }
    }

    private static bool Chama(Button botao, MenuPrincipalManager menu, string metodo)
    {
        for (int i = 0; i < botao.onClick.GetPersistentEventCount(); i++)
            if (botao.onClick.GetPersistentTarget(i) == menu && botao.onClick.GetPersistentMethodName(i) == metodo &&
                botao.onClick.GetPersistentListenerState(i) != UnityEventCallState.Off)
                return true;
        return false;
    }

    private static Object Campo(MenuPrincipalManager menu, string nome) => new SerializedObject(menu).FindProperty(nome).objectReferenceValue;

    [Test]
    public void Menu_ContinuarLigadoNaColunaDoJogar()
    {
        ComMenu(menu =>
        {
            var continuar = Campo(menu, "botaoContinuar") as GameObject;
            Assert.IsNotNull(continuar, "Campo \"Botao Continuar\" vazio: o menu não mostra o Continuar.");
            Button botao = continuar.GetComponent<Button>();
            Assert.IsNotNull(botao, "O Continuar não tem Button.");
            Assert.IsTrue(Chama(botao, menu, nameof(MenuPrincipalManager.Continuar)), "OnClick do Continuar não chama MenuPrincipalManager.Continuar.");

            Button jogar = null;
            foreach (Button b in continuar.transform.parent.GetComponentsInChildren<Button>(true))
                if (Chama(b, menu, nameof(MenuPrincipalManager.Jogar))) jogar = b;
            Assert.IsNotNull(jogar, "O Continuar não está na mesma coluna do Jogar.");
        });
    }

    [Test]
    public void Menu_JogarTemConfirmacaoComSimENao()
    {
        ComMenu(menu =>
        {
            var painel = Campo(menu, "painelConfirmarNovoJogo") as GameObject;
            var cancelar = Campo(menu, "botaoCancelarNovoJogo") as GameObject;
            Assert.IsNotNull(painel, "Campo \"Painel Confirmar Novo Jogo\" vazio: o Jogar começa sem perguntar.");
            Assert.IsNotNull(cancelar, "Campo \"Botao Cancelar Novo Jogo\" vazio.");
            Assert.IsFalse(painel.activeSelf, "A pergunta do Novo Jogo não pode começar aberta.");
            Assert.IsTrue(cancelar.transform.IsChildOf(painel.transform), "O botão de cancelar não é o da pergunta do Novo Jogo.");
            Assert.IsTrue(Chama(cancelar.GetComponent<Button>(), menu, nameof(MenuPrincipalManager.FecharConfirmacaoNovoJogo)),
                "O NÃO não chama FecharConfirmacaoNovoJogo.");

            bool temSim = false;
            foreach (Button b in painel.GetComponentsInChildren<Button>(true))
                temSim |= Chama(b, menu, nameof(MenuPrincipalManager.ConfirmarNovoJogo));
            Assert.IsTrue(temSim, "Nenhum botão da pergunta chama ConfirmarNovoJogo.");
        });
    }

    [Test]
    public void Menu_TelaDeSavesComUmCartaoPorSlot()
    {
        ComMenu(menu =>
        {
            var painel = Campo(menu, "painelSaves") as GameObject;
            Assert.IsNotNull(painel, "Campo \"Painel Saves\" vazio: o Jogar não abre a tela de saves.");
            Assert.IsFalse(painel.activeSelf, "A tela de saves não pode começar aberta.");
            var ui = painel.GetComponent<SlotsDeSaveUI>();
            Assert.IsNotNull(ui, "A tela de saves não tem SlotsDeSaveUI.");

            var so = new SerializedObject(ui);
            Assert.AreEqual(menu, so.FindProperty("menu").objectReferenceValue, "SlotsDeSaveUI sem o MenuPrincipalManager.");
            SerializedProperty cartoes = so.FindProperty("cartoes");
            Assert.AreEqual(SistemaDeSave.NumeroDeSlots, cartoes.arraySize, "Um cartão por slot.");
            for (int i = 0; i < cartoes.arraySize; i++)
                foreach (string campo in new[] { "titulo", "detalhe", "botaoCarregar", "botaoNovoJogo", "botaoExcluir" })
                    Assert.IsNotNull(cartoes.GetArrayElementAtIndex(i).FindPropertyRelative(campo).objectReferenceValue,
                        $"Cartão {i + 1} sem \"{campo}\".");

            var pergunta = so.FindProperty("painelConfirmarExclusao").objectReferenceValue as GameObject;
            var nao = so.FindProperty("botaoNaoExcluir").objectReferenceValue as GameObject;
            Assert.IsNotNull(pergunta, "Sem a pergunta de exclusão: o EXCLUIR apagaria sem confirmar.");
            Assert.IsFalse(pergunta.activeSelf, "A pergunta de exclusão não pode começar aberta.");
            Assert.IsNotNull(nao, "Sem o NÃO da pergunta de exclusão.");
            Assert.IsTrue(nao.transform.IsChildOf(pergunta.transform), "O NÃO não é o da pergunta de exclusão.");

            bool voltar = false;
            foreach (Button b in painel.GetComponentsInChildren<Button>(true))
                voltar |= Chama(b, menu, nameof(MenuPrincipalManager.FecharSaves));
            Assert.IsTrue(voltar, "Nenhum botão da tela de saves chama FecharSaves.");
        });
    }

    [Test]
    public void Celular_SoGiraEntreAsPaisagens()
    {
        Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
        Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait, "Retrato permitido.");
        Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown, "Retrato invertido permitido.");
        Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft, "Landscape Left desligado.");
        Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight, "Landscape Right desligado.");
    }
}
