using UnityEngine;

public class HealPickup : MonoBehaviour
{
    public int healAmount = 30;

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerStats>();
        if (player == null) return;

        player.Heal(healAmount);
        Destroy(gameObject);
    }
}