using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima o retrato de um juiz na bancada do tribunal: respira o tempo todo e pisca de vez em quando.
///
/// Os quadros seguem a ordem das spritesheets das caras (Art/Characters/Fase 4/Caras): a primeira metade é a respiração
/// de olhos abertos (do repouso ao peito cheio) e a segunda metade são as mesmas poses, na ordem inversa, de olhos
/// fechados. A respiração vai e volta pela primeira metade; a piscada troca o quadro pelo de olhos fechados da mesma
/// pose, então o retrato não "pula" ao piscar.
/// </summary>
public class RetratoAnimado : MonoBehaviour
{
    private Image imagem;
    private Sprite[] quadros;
    private int poses;            // quadros de olhos abertos (primeira metade)
    private float segundosPorQuadro;
    private Vector2 intervaloDaPiscada;
    private float duracaoDaPiscada;

    private float tempo;
    private float proximaPiscada;
    private float fimDaPiscada = -1f;
    private int quadroAtual = -1;

    public void Iniciar(Image imagem, Sprite[] quadros, float segundosPorQuadro, Vector2 intervaloDaPiscada, float duracaoDaPiscada)
    {
        this.imagem = imagem;
        this.quadros = quadros;
        this.segundosPorQuadro = Mathf.Max(0.02f, segundosPorQuadro);
        this.intervaloDaPiscada = intervaloDaPiscada;
        this.duracaoDaPiscada = duracaoDaPiscada;
        poses = (quadros.Length + 1) / 2;

        // Cada juiz começa num ponto diferente do ciclo, para a bancada não respirar e piscar em coro.
        tempo = Random.value * Passos * this.segundosPorQuadro;
        proximaPiscada = Random.Range(0.5f, Mathf.Max(0.5f, intervaloDaPiscada.y));
        Mostrar(0);
    }

    // Vai e volta: 0, 1, ... poses-1, ... 1.
    private int Passos => poses > 1 ? 2 * (poses - 1) : 1;

    private void Update()
    {
        if (imagem == null || quadros == null || quadros.Length == 0) return;

        tempo += Time.unscaledDeltaTime;
        int passo = Mathf.FloorToInt(tempo / segundosPorQuadro) % Passos;
        int pose = passo < poses ? passo : Passos - passo;

        if (tempo >= proximaPiscada)
        {
            fimDaPiscada = tempo + duracaoDaPiscada;
            proximaPiscada = fimDaPiscada + Random.Range(intervaloDaPiscada.x, Mathf.Max(intervaloDaPiscada.x, intervaloDaPiscada.y));
        }

        // Olhos fechados da pose p = quadro (total - 1 - p). Folha com um quadro fechado a menos usa o mais próximo.
        bool piscando = tempo < fimDaPiscada && quadros.Length > poses;
        Mostrar(piscando ? Mathf.Max(poses, quadros.Length - 1 - pose) : pose);
    }

    private void Mostrar(int quadro)
    {
        if (quadro == quadroAtual) return;
        quadroAtual = quadro;
        imagem.sprite = quadros[quadro];
    }
}
