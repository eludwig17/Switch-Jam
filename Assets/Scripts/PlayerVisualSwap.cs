using UnityEngine;

public class PlayerVisualSwap : MonoBehaviour{
    [SerializeField] private GameObject chompVisual;
    [SerializeField] private GameObject ghostVisual;

    void Awake(){
        ShowChomp();
    }

    void OnEnable(){
        GameEvents.OnEnterNormalMode += ShowChomp;
        GameEvents.OnEnterRaveMode += ShowChomp; // remains player in rave mode
        GameEvents.OnEnterSwitchMode += ShowGhost; // player becomes a ghost during switch
    }

    void OnDisable(){
        GameEvents.OnEnterNormalMode -= ShowChomp;
        GameEvents.OnEnterRaveMode -= ShowChomp;
        GameEvents.OnEnterSwitchMode -= ShowGhost;
    }

    private void ShowChomp(){
        if (chompVisual) chompVisual.SetActive(true);
        if (ghostVisual) ghostVisual.SetActive(false);
    }

    private void ShowGhost(){
        if (chompVisual) chompVisual.SetActive(false);
        if (ghostVisual) ghostVisual.SetActive(true);
    }
}