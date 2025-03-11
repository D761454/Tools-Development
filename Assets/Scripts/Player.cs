using UnityEngine;

public class Player : MonoBehaviour
{
    //[Header("General Parameters")]
    //[Tooltip("Player Hp"), Range(0.0f, 100.0f)]
    [SerializeField] private float m_health = 50.0f;

    //[Header("Jump Parameters")]
    //[Tooltip("Whether or not a player can jump")]
    //[SerializeField] private bool m_canJump = false;
    //[Tooltip("Whether or not fall damage is applied")]
    //[SerializeField] private bool m_hasFallDamage = false;
    //[Tooltip("The Jump height of the player, in game Units")]
    //[SerializeField] private float m_jumpHeight = 10.0f;
    //[Tooltip("The delay in milliseconds between input to jump")]
    //[SerializeField] private float m_jumpDelayMS = 15.0f;
    //[Tooltip("The coyote time value, in milliseconds")]
    //[SerializeField] private float m_coyoteTimeMS = 100.0f;

    //[Header("Move Parameters")]
    //[Tooltip("Whether the player is sprinting")]
    //[SerializeField] private bool m_isSprinting = false;
    //[Tooltip("The move speed of the player")]
    [SerializeField] private float m_moveSpeed = 10.0f;
    //[Tooltip("How much faster the sprint speed of the player is"), Range(1f, 5f)]
    //[SerializeField] private float m_sprintMultiplier = 1.5f;
}
