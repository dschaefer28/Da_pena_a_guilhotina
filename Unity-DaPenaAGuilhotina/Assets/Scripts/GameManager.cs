using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // NOVO: Permite gerenciar eventos de carregamento de cena

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dados da Investigação (Fase Atual)")]
    public CaseData casoEscolhido; 

    [Header("Status Globais (HUD)")]
    public int capitalAtual = 0;
    public int opiniaoPublicaAtual = 50; 
    public int opiniaoEstadoAtual = 50; 

    [Header("Persistência (Entre Cenas)")]
    public List<Item> inventarioSalvo = new List<Item>(); 

    [Header("Dependências Globais")]
    public InventoryManager inventoryManager;
    public DialogueSystem dialogueSystem;

    public event Action OnStatusChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Torna o GameManager imortal
    }

    // ARQUITETURA: Assina o evento nativo da Unity para troca de cenas
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Gatilho automático disparado sempre que uma nova cena abre (ex: Fase2 -> Porao)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SISTEMA] Cena '{scene.name}' carregada. Sincronizando HUD...");
        // Força todas as UIs da nova cena a buscarem os valores salvos
        ForcarAtualizacaoUI();
    }

    public void ForcarAtualizacaoUI()
    {
        OnStatusChanged?.Invoke();
    }

    public void ConfirmarCaso(CaseData caso)
    {
        casoEscolhido = caso;
        Debug.Log($"Caso escolhido e salvo: {caso.caseTitle}");
    }

    public void AplicarImpactoPanfleto(int impactoPublico, int impactoEstado, int ouro)
    {
        opiniaoPublicaAtual += impactoPublico;
        opiniaoEstadoAtual += impactoEstado;
        capitalAtual += ouro;

        opiniaoPublicaAtual = Mathf.Clamp(opiniaoPublicaAtual, 0, 100);
        opiniaoEstadoAtual = Mathf.Clamp(opiniaoEstadoAtual, 0, 100);

        Debug.Log($"[PANFLETO] Povo: {opiniaoPublicaAtual} | Estado: {opiniaoEstadoAtual} | Ouro: {capitalAtual}");
        ForcarAtualizacaoUI(); // Atualiza a tela imediatamente após o craft
    }
}