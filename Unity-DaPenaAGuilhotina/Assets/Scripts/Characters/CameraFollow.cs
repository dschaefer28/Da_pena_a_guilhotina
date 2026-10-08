using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Alvo e Configurações")]
    public Transform target;       // O jogador que a câmera vai seguir
    public float smoothSpeed = 5f; // Velocidade da suavização (maior = mais "presa" ao jogador)
    
    // O recuo da câmera. Em 2D, o Z precisa ficar em -10 para a câmera não entrar dentro dos sprites!
    public Vector3 offset = new Vector3(0f, 0f, -10f); 

    // Usamos LateUpdate em vez de Update para câmeras. 
    // Isso garante que a câmera só se mova DEPOIS que o jogador já terminou de se mover no frame atual, evitando "engasgos" (jitter).
    void LateUpdate()
    {
        if (target != null)
        {
            // Posição exata onde a câmera deveria estar (jogador + recuo)
            Vector3 desiredPosition = target.position + offset;

            // Interpola (suaviza) a transição da posição atual da câmera para a posição desejada
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Aplica a nova posição
            transform.position = smoothedPosition;
        }
    }
}