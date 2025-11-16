using UnityEngine;

public class DetectFence : Detector
{ //Detectar a cerca ao redor do espaço que se pode movimentar

        //Fazendo gizmos para ajudar na visualização da area de detecção
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private bool showGizmos = true;

    Collider2D[] coliders; //determinar se colidiu com a cerca? precisa ser vetor para detectar o jogador tbm?


    public override void Detect(AIData aIData)
    {
        coliders = Physics2D.OverlapCircleAll(transform.position,detectionRadius,layerMask);
        aIData.obstaculos = coliders;
    }
    private void OnDrawGizmos()
    { //Desenhar esferas vermelhas em objetos detectados
        if (showGizmos == false){ return;}
        if (Application.isPlaying && coliders != null)
        {
            Gizmos.color = Color.red;
            foreach(Collider2D obstacleCollider in coliders)
            {
                Gizmos.DrawSphere(obstacleCollider.transform.position,0.2f);
            }
        }
    }
}
