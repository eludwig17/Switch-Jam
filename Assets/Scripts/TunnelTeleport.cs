using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TunnelTeleporter : MonoBehaviour{
	[SerializeField] private Transform exitPoint;
	[SerializeField] private float cooldown = 0.4f;

	void Awake(){
		var col = GetComponent<Collider>();
		col.isTrigger = true;
	}

	void OnTriggerEnter(Collider other){
		if (!other.CompareTag("Player") && !other.CompareTag("Ghost")) return;
		if (exitPoint == null) return;

		var tracker = other.GetComponent<TeleportCooldown>();
		if (tracker != null && tracker.IsOnCooldown) return;

		if (tracker == null) tracker = other.gameObject.AddComponent<TeleportCooldown>();
		tracker.StartCooldown(cooldown);

		Vector3 dest = exitPoint.position + exitPoint.forward * 0.6f;
		dest.y = other.transform.position.y;

		var rb = other.GetComponent<Rigidbody>();
		if (rb != null) rb.position = dest;
		else other.transform.position = dest;
	}
}