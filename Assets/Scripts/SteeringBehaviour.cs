using System.Collections.Generic;
using UnityEngine;

enum Behavior { Idle, Evade }
enum State { Idle, Evade }

[RequireComponent(typeof(Rigidbody2D))]
public class SteeringBehaviour : MonoBehaviour
{
    [SerializeField] Behavior behavior = Behavior.Evade;
    [SerializeField] Transform ToAvoid = null;
    [SerializeField, Range(0.1f, 0.99f)] float decelerationFactor = 0.75f;
    [SerializeField] float evadeRange = 5f;

    public float moveSpeed = 5f;

    State state = State.Idle;
    Rigidbody2D physics;
    void FixedUpdate()
    {
        if (ToAvoid != null)
        {
            switch (behavior)
            {
                case Behavior.Idle: IdleBehavior(); break;
                case Behavior.Evade: EvadeBehavior(); break;
            }
        }

        physics.linearVelocity = Vector2.ClampMagnitude(physics.linearVelocity, moveSpeed);
    }

    void IdleBehavior()
    {
        physics.linearVelocity = physics.linearVelocity * decelerationFactor;
    }
    void EvadeBehavior()
    {
        Vector2 delta = ToAvoid.position - transform.position;
        Vector2 steering = delta.normalized * moveSpeed - physics.linearVelocity;
        float distance = delta.magnitude;

        if (distance > evadeRange)
        {
            state = State.Idle;
        }
        else
        {
            state = State.Evade;
        }

        switch (state)
        {
            case State.Idle:
                IdleBehavior();
                break;
            case State.Evade:
                physics.linearVelocity -= steering * Time.fixedDeltaTime;
                break;
        }
    }

    void Awake()
    {
        physics = GetComponent<Rigidbody2D>();
    }

    void OnDrawGizmos()
    {
        if (ToAvoid == null)
        {
            return;
        }

        switch (behavior)
        {
            case Behavior.Idle:
                break;
            case Behavior.Evade:
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, evadeRange);
                break;
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, ToAvoid.position);
    }
}
