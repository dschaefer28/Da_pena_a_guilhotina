public interface IInteractable
{
    void Interact();

    /// <summary>Falso quando não responde agora: fica fora da escolha do alvo e do PromptDeInteracao.</summary>
    bool PodeInteragir => true;

    /// <summary>Nome abaixo do aviso de interação. Vazio/null = só a tecla.</summary>
    string NomeExibido => null;
}
