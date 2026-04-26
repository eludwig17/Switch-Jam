using System.Collections;
using UnityEngine;

public class RaveFX : MonoBehaviour{
    [SerializeField] private Light[] raveLights;
    [SerializeField] private float cycleInterval = 0.15f;

    [Header("Palettes")]
    [SerializeField] private Color[] ravePalette ={
        new Color(1f, 0.2f, 0.6f),
        new Color(0.2f, 0.8f, 1f),
        new Color(1f, 1f, 0.2f),
        new Color(0.6f, 0.2f, 1f),
        new Color(0.2f, 1f, 0.4f)
    };
    [SerializeField] private Color[] switchPalette ={
        new Color(1f, 0f, 0f),
        new Color(1f, 0.5f, 0f),
        new Color(1f, 1f, 1f)
    };

    private Coroutine _cycleRoutine;
    private Color[] _normColors;

    void Awake(){
        _normColors = new Color[raveLights.Length];
        for (int i = 0; i < raveLights.Length; i++)
            if (raveLights[i] != null) _normColors[i] = raveLights[i].color;
    }

    void OnEnable(){
        GameEvents.OnEnterNormalMode += StopCycle;
        GameEvents.OnEnterRaveMode += HandleEnterRaveMode;
        GameEvents.OnEnterSwitchMode += HandleEnterSwitchMode;
    }

    void OnDisable(){
        GameEvents.OnEnterNormalMode -= StopCycle;
        GameEvents.OnEnterRaveMode -= HandleEnterRaveMode;
        GameEvents.OnEnterSwitchMode -= HandleEnterSwitchMode;
        StopCycle();
    }

    private void HandleEnterRaveMode() => StartCycle(ravePalette);
    private void HandleEnterSwitchMode() => StartCycle(switchPalette);

    private void StartCycle(Color[] palette){
        if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
        _cycleRoutine = StartCoroutine(CycleColors(palette));
    }

    private void StopCycle(){
        if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
        _cycleRoutine = null;
        for (int i = 0; i < raveLights.Length; i++)
            if (raveLights[i] != null) raveLights[i].color = _normColors[i];
    }

    private IEnumerator CycleColors(Color[] palette){
        int tick = 0;
        while (true){
            for (int i = 0; i < raveLights.Length; i++){
                if (raveLights[i] == null) continue;
                raveLights[i].color = palette[(i + tick) % palette.Length];
            }
            tick++;
            yield return new WaitForSeconds(cycleInterval);
        }
    }
}