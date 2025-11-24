using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

enum Behavior { Idle, Evade }
enum State { Idle, Evade }

[RequireComponent(typeof(Rigidbody2D))]
public class SteeringBehaviour : MonoBehaviour
{
    [Header("Wall Avoidance")]
    [Tooltip("Distância dos raycasts para detectar paredes")]
    public float raycastDistance = 1.8f;
    [Tooltip("Layers que representam paredes/obstáculos")]
    public LayerMask avoidLayer;
    [Tooltip("Peso multiplicador da direção de evasão (quanto maior, mais prioridade)")]
    [Range(0f, 3f)]
    public float avoidanceWeight = 1.5f;
    [Tooltip("Raio para CircleCast (opcional, 0 usa Raycast simples)")]
    public float castRadius = 0f;
    [Tooltip("Se true, vai desenhar gizmos para debug")]
    public bool debugDraw = true;

    private static readonly List<Vector2> eightDirections = new()
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

    public Vector2 GetAvoidanceValue()
    {
        Vector2 origin = transform.position;
        Vector2 combinedAway = Vector2.zero;
        bool found = false;

        for (int i = 0; i < eightDirections.Count; i++)
        {
            Vector2 dir = eightDirections[i];
            RaycastHit2D hit;
            if (castRadius > 0f)
                hit = Physics2D.CircleCast(origin, castRadius, dir, raycastDistance, avoidLayer);
            else
                hit = Physics2D.Raycast(origin, dir, raycastDistance, avoidLayer);

            if (debugDraw)
            {
                Debug.DrawRay(origin, dir * raycastDistance, hit.collider != null ? Color.red : Color.green, 0.1f);
            }

            if (hit.collider != null)
            {
                // vetor que aponta do ponto de contato para o centro (ou seja, para fora da parede)
                Vector2 away = origin - hit.point;
                float distanceFactor = Mathf.Clamp01((raycastDistance - hit.distance) / raycastDistance); // mais perto => maior força
                if (away.sqrMagnitude > 0.0001f)
                    combinedAway += away.normalized * distanceFactor;
                else
                    combinedAway += -dir * distanceFactor; // fallback
                found = true;
            }
        }

        if (!found) return Vector2.zero;

        // normaliza e aplica peso
        return combinedAway.normalized * avoidanceWeight;
    }
}
