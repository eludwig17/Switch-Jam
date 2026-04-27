using System.Collections;
using UnityEngine;

public class TeleportCooldown : MonoBehaviour{
	public bool IsOnCooldown { get; private set; }

	public void StartCooldown(float duration){
		StopAllCoroutines();
		StartCoroutine(CooldownRoutine(duration));
	}

	private IEnumerator CooldownRoutine(float duration){
		IsOnCooldown = true;
		yield return new WaitForSeconds(duration);
		IsOnCooldown = false;
	}
}