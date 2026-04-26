using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSettings : MonoBehaviour{
	[SerializeField] private Slider musicSlider;
	[SerializeField] private TMP_Text volumeLabel;

	void Start(){
		if (musicSlider == null) return;
		float saved = AudioManager.Instance != null ? AudioManager.Instance.musicVolume : PlayerPrefs.GetFloat("MusicVolume", 0.5f);
		musicSlider.value = saved;
		UpdateLabel(saved);
	}

	public void SetMusicVolume(float value){
		if (AudioManager.Instance == null) return;
		AudioManager.Instance.musicVolume = value;
		AudioManager.Instance.UpdateMusicVolume(value);
		UpdateLabel(value);
	}

	private void UpdateLabel(float value){
		if (volumeLabel == null) return;
		volumeLabel.text = "VOLUME: " + Mathf.RoundToInt(value * 100) + "%";
	}
}