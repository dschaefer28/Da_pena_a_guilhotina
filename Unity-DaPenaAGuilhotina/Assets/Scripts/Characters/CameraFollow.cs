using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Alvo e Configurações")]
    public Transform target;
    public float smoothSpeed = 5f;

    // Z em -10 para a câmera não ficar dentro do plano dos sprites.
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    // LateUpdate: move depois do jogador no frame, evitando jitter.
    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;

            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            transform.position = smoothedPosition;
        }
    }
}