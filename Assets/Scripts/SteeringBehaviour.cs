using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

enum Behavior { Idle, Evade }
enum State { Idle, Evade }

[RequireComponent(typeof(Rigidbody2D))]

public static class Directions
{
    public static List<Vector2> eightDirections = new List<Vector2>
    {//Direções cardeais e diagonais da movimentação do npc
        new Vector2(0,1).normalized,
        new Vector2(1,1).normalized,
        new Vector2(1,0).normalized,
        new Vector2(1,-1).normalized,
        new Vector2(0,-1).normalized,
        new Vector2(-1,-1).normalized,
        new Vector2(-1,0).normalized,
        new Vector2(-1,1).normalized
    };
}
public class SteeringBehaviour : MonoBehaviour
{
    [Header("Evade Behaviour")]
    [SerializeField] Behavior behavior = Behavior.Evade;
    [SerializeField] Transform ToAvoid = null;
    [SerializeField, Range(0.1f, 0.99f)] float decelerationFactor = 0.75f;
    [SerializeField] float evadeRange = 5f;
    private float moveSpeed = 5f;

    [Header("Wall Avoid Behaviour")]
    [SerializeField] private float _raycastDistance = 2.2f;
    [SerializeField] private LayerMask _avoidLayer;
    private bool _nearWall;

    State state = State.Idle;
    Rigidbody2D physics;
    RaycastHit2D pointHit;
    void FixedUpdate()
    {
        if (ToAvoid != null)
        {
            for (int i = 0; i < 8; i++)
            {

                _nearWall = Physics2D.Raycast(transform.position, Directions.eightDirections[i], Vector2.Distance(Directions.eightDirections[i] * _raycastDistance, transform.position), _avoidLayer);
                pointHit = Physics2D.Raycast(transform.position, Directions.eightDirections[i], Vector2.Distance(Directions.eightDirections[i] * _raycastDistance, transform.position), _avoidLayer);
                Debug.DrawRay(transform.position, Directions.eightDirections[i] * _raycastDistance, Color.greenYellow);
            }
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
        //Fugindo do Player
        Vector2 delta = ToAvoid.position - transform.position;
        Vector2 steering = delta.normalized * moveSpeed - physics.linearVelocity;
        

        if (_nearWall)
        { //Em teoria, se afastar da parede que o npc detectou
            //Debug.Log("Wall hit");
            Vector2 hitWall = pointHit.point;
            delta = (Vector2)transform.position - hitWall;
        }
        else
        {
            return;
        }
        float distance = delta.magnitude;

        if (distance > evadeRange && !_nearWall)
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
                for (int i = 0; i < 8; i++)
                {
                    Gizmos.DrawRay(transform.position, Directions.eightDirections[i] * _raycastDistance);
                }

                break;
        }

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, ToAvoid.position);
    }
}
