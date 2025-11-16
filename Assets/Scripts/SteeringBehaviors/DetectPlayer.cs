using System.Collections.Generic;
using UnityEngine;

public class DetectPlayer : Detector
{ //Fazer com que o gato detecte o jogador que o está perseguindo

    [SerializeField] private float playerDetectionRange = 4;
    [SerializeField] LayerMask fenceLayerMask, playerLayerMask;
    [SerializeField] private bool showGizmos = true;

    //parametros para gizmos 
    private List<Transform> colliders;
    public override void Detect(AIData aiData)
    {
        //checar se o jogador está por perto através de um circulo desenhado por gizmo
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, playerDetectionRange,playerLayerMask);
        if(playerCollider != null){
            Vector2 direction = (playerCollider.transform.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, playerDetectionRange,fenceLayerMask); //ver se tem jogador ou cerca naquela area

        //Certificar se o colider detectado está no layer Player
        if (hit.collider != null && (playerLayerMask & (1 << hit.collider.gameObject.layer)) != 0)
        {
            colliders = new List<Transform>()
            {
                playerCollider.transform
            };
        } else
        {
            colliders = null;
        }
        } else
        {
            //inimigo não vê o jogador
            colliders = null;
        }
    aiData.targets = colliders;
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmos == false){return;}

        Gizmos.DrawWireSphere(transform.position,playerDetectionRange);
        if(colliders == null) {return;}
        Gizmos.color = Color.magenta;
        foreach(var item in colliders)
        {
            Gizmos.DrawSphere(item.position,0.3f);
        }
    }

}
