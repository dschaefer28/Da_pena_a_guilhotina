using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ao voltar ao escritório, posiciona o jogador no alçapão (vindo do porão) ou na porta (vindo de uma cena "Fase").
/// Outras origens mantêm a posição da cena. Auto-registrado, sem configuração nas cenas.
/// </summary>
public static class PontoDeChegadaNaCena
{
    private static string cenaAnterior = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        SceneManager.sceneUnloaded -= AoDescarregarCena;
        SceneManager.sceneUnloaded += AoDescarregarCena;
    }

    private static void AoDescarregarCena(Scene cena)
    {
        cenaAnterior = cena.name;
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;

        TrapdoorInteractable alcapao = Object.FindFirstObjectByType<TrapdoorInteractable>();
        if (alcapao == null) return; // só a cena do escritório tem alçapão

        PlayerMove jogador = Object.FindFirstObjectByType<PlayerMove>();
        if (jogador == null) return;

        Vector2? destino = null;
        if (cenaAnterior == alcapao.nomeCenaPorao)
        {
            // Só o X: o centro do alçapão fica abaixo do chão.
            destino = new Vector2(CentroDe(alcapao.gameObject).x, jogador.transform.position.y);
        }
        else if (cenaAnterior.StartsWith("Fase"))
        {
            DoorInteractable porta = PortaDaRua();
            if (porta != null)
            {
                // Só o X: o Y da porta (sprite alto) não é o chão onde o jogador anda.
                destino = new Vector2(CentroDe(porta.gameObject).x, jogador.transform.position.y);
            }
        }

        if (destino == null) return;
        Posicionar(jogador, destino.Value);
    }

    private static DoorInteractable PortaDaRua()
    {
        DoorInteractable[] portas = Object.FindObjectsByType<DoorInteractable>(FindObjectsSortMode.None);
        foreach (DoorInteractable porta in portas)
            if (porta.destinoPeloCaso) return porta;
        return portas.Length > 0 ? portas[0] : null;
    }

    private static Vector2 CentroDe(GameObject alvo)
    {
        Collider2D col = alvo.GetComponentInChildren<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)alvo.transform.position;
    }

    private static void Posicionar(PlayerMove jogador, Vector2 destino)
    {
        Vector3 pos = new Vector3(destino.x, destino.y, jogador.transform.position.z);
        jogador.transform.position = pos;
        Rigidbody2D rb = jogador.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = destino;

        // Sem isso a câmera cruzaria a sala suavizando até o jogador.
        CameraFollow seguidor = Object.FindFirstObjectByType<CameraFollow>();
        if (seguidor != null && seguidor.target != null)
            seguidor.transform.position = seguidor.target.position + seguidor.offset;
    }
}
