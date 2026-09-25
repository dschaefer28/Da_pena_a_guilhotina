/// <summary>
/// Rota da campanha, travada ao entrar na Fase 4 pelo desnível entre Opinião Pública e Opinião do Estado
/// (GameManager.CalcularRota). Define qual caso a mesa entrega na Fase 4 e o resultado do Tribunal.
/// </summary>
public enum RotaFinal
{
    Nenhuma,      // ainda não chegou à Fase 4 (em CaseData: caso serve a qualquer rota)
    A_Guilhotina, // Povo alto, Estado baixo: a bancada explode e o jogador é condenado
    B_Tirano,     // Estado alto, Povo baixo: a bancada é receptiva e o jogador sobrevive
    C_Equilibrio  // barras equilibradas: terceiro desfecho
}
