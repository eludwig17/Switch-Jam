using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PelletCollectible : MonoBehaviour {
    [SerializeField] private bool isPowerPellet = false;

    public bool IsPowerPellet => isPowerPellet;

    void Reset(){
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other){
        if (!other.CompareTag("Player")) return;

        if (isPowerPellet)
            GameEvents.PowerPelletEaten();
        else
            GameEvents.PelletEaten();

        gameObject.SetActive(false);
    }
}