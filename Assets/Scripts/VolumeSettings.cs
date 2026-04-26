using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeSettings : MonoBehaviour{
	[SerializeField] private Slider musicSlider;
	[SerializeField] private TMP_Text volumeLabel;

	void Start(){
		if (musicSlider == null) return;
		if (AudioManager.Instance == null) return;
		musicSlider.value = AudioManager.Instance.musicVolume;
		UpdateLabel(musicSlider.value);
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