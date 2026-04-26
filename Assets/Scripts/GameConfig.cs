using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ChompSwap/Game Config")]
public class GameConfig : ScriptableObject{
	[Header("Scoring")]
	public int pelletScore = 10;
	public int powerPelletScore = 50;
	public int cherryScore = 200;
	public int ghostEatScore = 300;
	public int switchModeGhostScore = 500;
	public float raveScoreMultiplier = 2f;

	[Header("Switch Mechanic")]
	public int powerPelletsPerSwitch = 3;

	[Header("Timing")]
	public float raveDuration = 8f;
	public float switchDuration = 10f;
	public float invertControlChance = 0.3f;
	public float invertControlMinDuration = 2f;
	public float invertControlMaxDuration = 4f;

	[Header("Player")]
	public float chompMoveSpeed = 4f;
	public float ghostFormMoveSpeed = 6f;
	public int startingLives = 3;

	[Header("Ghost AI")]
	public float ghostNormalSpeed = 4f;
	public float ghostFrightenedSpeed = 2.5f;
	public float ghostRespawnDelay = 3f;
}