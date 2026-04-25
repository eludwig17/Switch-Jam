using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Normal, Rave, Switch, Paused, GameOver }

    [Header("Config")]
    [SerializeField] private GameConfig config;

    [Header("Runtime information | readonly")]
    [SerializeField] private GameState currState = GameState.MainMenu;
    [SerializeField] private int score;
    [SerializeField] private int lives = 3;
    [SerializeField] private int powerPelletCount;
    [SerializeField] private bool controlsInverted;

    private Coroutine _modeCoroutine;
    private Coroutine _invertCoroutine;
    private GameState _stateBeforePause;

    public GameConfig Config => config;
    public GameState CurrentState => currState;
    public int Score => score;
    public int Lives => lives;
    public bool ControlsInverted => controlsInverted;
    public bool IsInSwitchMode => currState == GameState.Switch;
    public bool IsInRaveMode => currState == GameState.Rave;

    void Awake(){
        if (Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable(){
        GameEvents.OnPelletEaten += HandlePelletEaten;
        GameEvents.OnPowerPelletEaten += HandlePowerPelletEaten;
        GameEvents.OnCherryEaten += HandleCherryEaten;
        GameEvents.OnGhostEaten += HandleGhostEaten;
        GameEvents.OnPlayerCaught += HandlePlayerCaught;
    }

    void OnDisable(){
        GameEvents.OnPelletEaten -= HandlePelletEaten;
        GameEvents.OnPowerPelletEaten -= HandlePowerPelletEaten;
        GameEvents.OnCherryEaten -= HandleCherryEaten;
        GameEvents.OnGhostEaten -= HandleGhostEaten;
        GameEvents.OnPlayerCaught -= HandlePlayerCaught;
    }

    public void StartGame(){
        if (config == null){
            Debug.LogError("GameManager is missing GameConfig reference.");
            return;
        }

        score = 0;
        lives = config.startingLives;
        powerPelletCount = 0;

        GameEvents.ScoreChanged(score);
        GameEvents.LivesChanged(lives);
        GameEvents.PowerPelletProgress(0);
        GameEvents.GameStart();

        TransitionTo(GameState.Normal);
    }

    public void TogglePause(){
        if (currState == GameState.Paused){
            Time.timeScale = 1f;
            TransitionTo(_stateBeforePause);
            GameEvents.PauseToggled(false);
        }
        else if (currState == GameState.Normal || currState == GameState.Rave || currState == GameState.Switch){
            _stateBeforePause = currState;
            Time.timeScale = 0f;
            currState = GameState.Paused;
            GameEvents.PauseToggled(true);
        }
    }

    private void HandlePelletEaten(){
        int add = config.pelletScore;
        if (currState == GameState.Rave) add = Mathf.RoundToInt(add * config.raveScoreMultiplier);
        AddScore(add);
    }

    private void HandlePowerPelletEaten(){
        AddScore(config.powerPelletScore);
        powerPelletCount++;
        GameEvents.PowerPelletProgress(powerPelletCount % config.powerPelletsPerSwitch);
        TransitionTo(powerPelletCount % config.powerPelletsPerSwitch == 0 ? GameState.Switch : GameState.Rave);
    }

    private void HandleCherryEaten(){
        AddScore(config.cherryScore);
    }

    private void HandleGhostEaten(){
        int points = (currState == GameState.Switch) ? config.switchModeGhostScore : config.ghostEatScore;
        AddScore(points);
    }

    private void HandlePlayerCaught(){
        // only vulnerable in normal mode
        if (currState != GameState.Normal) return;

        lives--;
        GameEvents.LivesChanged(lives);
        if (lives <= 0){
            TransitionTo(GameState.GameOver);
            GameEvents.GameOver();
        }
    }

    private void TransitionTo(GameState next){
        if (_modeCoroutine != null){
            StopCoroutine(_modeCoroutine);
            _modeCoroutine = null;
        }
        if (_invertCoroutine != null){
            StopCoroutine(_invertCoroutine);
            _invertCoroutine = null;
            SetControlsInverted(false);
        }

        currState = next;

        switch (next){
            case GameState.Normal:
                GameEvents.EnterNormalMode();
                break;
            case GameState.Rave:
                GameEvents.EnterRaveMode();
                _modeCoroutine = StartCoroutine(RaveRoutine());
                _invertCoroutine = StartCoroutine(InvertControlRoutine());
                break;
            case GameState.Switch:
                GameEvents.EnterSwitchMode();
                _modeCoroutine = StartCoroutine(SwitchRoutine());
                break;
            case GameState.GameOver:
                break;
        }
    }

    private IEnumerator RaveRoutine(){
        yield return new WaitForSeconds(config.raveDuration);
        if (currState == GameState.Rave)
            TransitionTo(GameState.Normal);
    }

    private IEnumerator SwitchRoutine(){
        yield return new WaitForSeconds(config.switchDuration);
        if (currState == GameState.Switch)
            TransitionTo(GameState.Normal);
    }

    // randomly inverts controls during rave
    private IEnumerator InvertControlRoutine(){
        while (currState == GameState.Rave){
            float wait = Random.Range(1f, 3f);
            yield return new WaitForSeconds(wait);

            if (currState != GameState.Rave) yield break;

            if (Random.value < config.invertControlChance){
                SetControlsInverted(true);
                float invertTime = Random.Range(
                    config.invertControlMinDuration,
                    config.invertControlMaxDuration);
                yield return new WaitForSeconds(invertTime);
                SetControlsInverted(false);
            }
        }
    }

    private void SetControlsInverted(bool inverted){
        if (controlsInverted == inverted) return;
        controlsInverted = inverted;
        GameEvents.ControlsInvertedChanged(inverted);
    }

    private void AddScore(int amount){
        score += amount;
        GameEvents.ScoreChanged(score);
    }
}