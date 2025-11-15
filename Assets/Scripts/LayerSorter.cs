using UnityEngine;

/**
* Script responsável por organizar a prioridade de qual sprite será renderizado no topo.
* Utiliza da posição Y do objeto ao qual foi anexado para determinar o "Order in Layer"
* do sprite do objeto, assim evitando que os sprites fiquem piscando, disputando por prioridade
* caso estejam "um em cima do outro".
**/
[RequireComponent(typeof(SpriteRenderer))]
public class LayerSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    void LateUpdate()
    {
        // Como o campo "Order in Layer não aceita float, multiplicar o valor por 100 evita erros caso os objetos estejam muito próximos.
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
    }
}
