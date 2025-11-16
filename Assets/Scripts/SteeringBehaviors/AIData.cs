using System.Collections.Generic;
using UnityEngine;

public class AIData : MonoBehaviour
{ //Código AIData vai guardar informações relacionadas à cerca que delimita o espaço e a posição do jogador
    public List<Transform> targets = null;
    public Transform fugindoDe;
    public Collider2D[] obstaculos = null;
    
    //evitar null exception
    public int GetTargetsCount() => targets == null ? 0: targets.Count;
}
