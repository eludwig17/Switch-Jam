using System;

public static class GameEvents{
	public static event Action<int> OnScoreChanged;
	public static event Action<int> OnLivesChanged;
	public static event Action<int> OnPowerPelletProgress;

	public static event Action OnEnterNormalMode;
	public static event Action OnEnterRaveMode;
	public static event Action OnEnterSwitchMode;

	public static event Action<bool> OnControlsInvertedChanged;

	public static event Action OnPelletEaten;
	public static event Action OnPowerPelletEaten;
	public static event Action OnCherryEaten;
	public static event Action OnGhostEaten;
	public static event Action OnPlayerCaught;

	public static event Action OnGameStart;
	public static event Action OnGameOver;
	public static event Action<bool> OnPauseToggled;

	public static void ScoreChanged(int s) => OnScoreChanged?.Invoke(s);
	public static void LivesChanged(int l) => OnLivesChanged?.Invoke(l);
	public static void PowerPelletProgress(int c) => OnPowerPelletProgress?.Invoke(c);

	public static void EnterNormalMode() => OnEnterNormalMode?.Invoke();
	public static void EnterRaveMode() => OnEnterRaveMode?.Invoke();
	public static void EnterSwitchMode() => OnEnterSwitchMode?.Invoke();

	public static void ControlsInvertedChanged(bool b) => OnControlsInvertedChanged?.Invoke(b);

	public static void PelletEaten() => OnPelletEaten?.Invoke();
	public static void PowerPelletEaten() => OnPowerPelletEaten?.Invoke(); 
	public static void CherryEaten() => OnCherryEaten?.Invoke();
	public static void GhostEaten() => OnGhostEaten?.Invoke();
	public static void PlayerCaught() => OnPlayerCaught?.Invoke();

	public static void GameStart() => OnGameStart?.Invoke();
	public static void GameOver() => OnGameOver?.Invoke();
	public static void PauseToggled(bool paused) => OnPauseToggled?.Invoke(paused);
}