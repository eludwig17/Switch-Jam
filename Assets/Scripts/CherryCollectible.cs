using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CherryCollectible : MonoBehaviour{
    [Tooltip("will be respawned after eaten")]
    [SerializeField] private bool respawns = true;
    [SerializeField] private float respawnDelay = 15f;

    private Vector3 startPos;
    private MeshRenderer[] renderers;
    private Collider col;

    void Awake(){
        startPos = transform.position;
        renderers = GetComponentsInChildren<MeshRenderer>();
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other){
        if (!other.CompareTag("Player")) return;

        GameEvents.CherryEaten();

        if (respawns){
            SetVisible(false);
            Invoke(nameof(Respawn), respawnDelay);
        }
        else{
            gameObject.SetActive(false);
        }
    }

    private void Respawn(){
        transform.position = startPos;
        SetVisible(true);
    }

    private void SetVisible(bool visible){
        foreach (var r in renderers) r.enabled = visible;
        col.enabled = visible;
    }
}