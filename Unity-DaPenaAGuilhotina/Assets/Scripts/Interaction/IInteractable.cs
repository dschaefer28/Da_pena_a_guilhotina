public interface IInteractable
{
    void Interact();

    /// <summary>
    /// Falso quando o objeto está na cena mas não aceita interação agora (ex: NPC que já entregou a
    /// pista). O Player usa isso para escolher o alvo mais próximo entre os que realmente respondem, e o
    /// aviso de interação (PromptDeInteracao) só aparece para esses.
    /// </summary>
    bool PodeInteragir => true;
}
