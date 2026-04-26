using UnityEngine;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour {
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text powerPelletProgressText;
    [SerializeField] private GameObject invertedText;
    private Coroutine _invertedTextRoutine;


    [Header("End Screen")]
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text highScoreText;
    

    void Start(){
        ShowOnly(mainMenuPanel);
    }

    void OnEnable(){
        GameEvents.OnScoreChanged += UpdateScoreText;
        GameEvents.OnLivesChanged += UpdateLivesText;
        GameEvents.OnPowerPelletProgress += UpdatePowerProgress;
        GameEvents.OnControlsInvertedChanged += SetInvertedLabelVisible;
        GameEvents.OnEnterNormalMode += SetModeNormal;
        GameEvents.OnEnterRaveMode += SetModeRave;
        GameEvents.OnEnterSwitchMode += SetModeSwitch;

        GameEvents.OnGameStart += HandleGameStart;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnPauseToggled += HandlePause;
    }

    void OnDisable(){
        GameEvents.OnScoreChanged -= UpdateScoreText;
        GameEvents.OnLivesChanged -= UpdateLivesText;
        GameEvents.OnPowerPelletProgress -= UpdatePowerProgress;
        GameEvents.OnControlsInvertedChanged -= SetInvertedLabelVisible;
        GameEvents.OnEnterNormalMode -= SetModeNormal;
        GameEvents.OnEnterRaveMode -= SetModeRave;
        GameEvents.OnEnterSwitchMode -= SetModeSwitch;
        GameEvents.OnGameStart -= HandleGameStart;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnPauseToggled -= HandlePause;
    }

    private void HandleGameStart(){
        ShowOnly(hudPanel);
    }

    private void UpdateScoreText(int scoreValue){
        if (scoreText) scoreText.text = "Score: " + scoreValue;
    }

    private void UpdateLivesText(int livesValue){
        if (livesText) livesText.text = "Lives: " + livesValue;
    }

    private void SetInvertedLabelVisible(bool visible){
        if (invertedText == null) return;
    
        if (visible){
            if (_invertedTextRoutine != null) StopCoroutine(_invertedTextRoutine);
            _invertedTextRoutine = StartCoroutine(ShowInvertedBriefly());
        }
        else{
            if (_invertedTextRoutine != null) StopCoroutine(_invertedTextRoutine);
            invertedText.SetActive(false);
        }
    }
    
    private IEnumerator ShowInvertedBriefly(){
        invertedText.SetActive(true);
        yield return new WaitForSeconds(1f);
        invertedText.SetActive(false);
    }

    private void SetModeNormal(){
        if (modeText) modeText.text = "NORMAL";
    }

    private void SetModeRave(){
        if (modeText) modeText.text = "RAVE!";
    }

    private void SetModeSwitch(){
        if (modeText) modeText.text = "SWITCH!!!";
    }

    private void UpdatePowerProgress(int count){
        if (powerPelletProgressText == null || GameManager.Instance == null) return;
        int required = GameManager.Instance.Config.powerPelletsPerSwitch;
        powerPelletProgressText.text = $"Switch: {count}/{required}";
    }

    private void HandleGameOver(){
        if (gameOverScoreText != null && GameManager.Instance != null)
            gameOverScoreText.text = "SCORE: " + GameManager.Instance.Score;
        if (highScoreText != null && GameManager.Instance != null)
            highScoreText.text = "BEST: " + GameManager.Instance.HighScore;
        ShowOnly(gameOverPanel);
    }

    private void HandlePause(bool paused){
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(paused);
    }

    void Update(){
        if (Input.GetKeyDown(KeyCode.Escape)){
            if (GameManager.Instance != null &&
                (GameManager.Instance.CurrentState == GameManager.GameState.Normal 
                 || GameManager.Instance.CurrentState == GameManager.GameState.Rave 
                 || GameManager.Instance.CurrentState == GameManager.GameState.Switch 
                 || GameManager.Instance.CurrentState == GameManager.GameState.Paused)){ 
                GameManager.Instance.TogglePause();
            }
        }
    }

    private void ShowOnly(GameObject panel){
        if (mainMenuPanel) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(panel == gameOverPanel);
        if (hudPanel) hudPanel.SetActive(panel == hudPanel);
    }

    public void PlayButton(){
        Time.timeScale = 1f;
        GameManager.Instance.StartGame();
    }
  
    public void TogglePause() => GameManager.Instance.TogglePause();

    public void RestartButton(){
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenuButton(){
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitButton(){
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}