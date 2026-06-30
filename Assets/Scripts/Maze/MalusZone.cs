using UnityEngine;
using System.Collections;

// Attach to: the MalusTilemap GameObject
// Required: TilemapCollider2D (Is Trigger = true) on same object
// Dependencies: PlayerController
public class MalusZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<PlayerController>()?.ApplyMalus();
    }
}
