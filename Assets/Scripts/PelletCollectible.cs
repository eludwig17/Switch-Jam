using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PelletCollectible : MonoBehaviour {
	[SerializeField] private bool isPowerPellet = false;

	[Tooltip("Respawn timer for pellets")]
	[SerializeField] private float respawnDelay = 30f;

	public bool IsPowerPellet => isPowerPellet;

	private Collider col;
	private Renderer[] renderers;

	void Awake(){
		col = GetComponent<Collider>();
		col.isTrigger = true;
		renderers = GetComponentsInChildren<Renderer>();
	}

	void Reset(){
		var c = GetComponent<Collider>();
		if (c != null) c.isTrigger = true;
	}

	void OnTriggerEnter(Collider other){
		if (!other.CompareTag("Player")) return;
		if (!col.enabled) return;

		if (isPowerPellet)
			GameEvents.PowerPelletEaten();
		else
			GameEvents.PelletEaten();
		StartCoroutine(RespawnRoutine());
	}

	private IEnumerator RespawnRoutine(){
		SetVisible(false);
		yield return new WaitForSeconds(respawnDelay);
		SetVisible(true);
	}

	private void SetVisible(bool visible){
		col.enabled = visible;
		foreach (var r in renderers) r.enabled = visible;
	}
}