using UnityEngine;

/** <summary>
* Script da vida. Seta um valor de vida máximo para o objeto, com variáveis pra controlar se
* o objeto pode tomar dano, pode morrer e se está vivo. Tem também métodos pra mudar a vida,
* pegar, curar e dar dano. É necessário alterar a função de Die(), que por padrão, deleta o gameobject do jogador.
* </summary>
**/
public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    private float health; 
    [SerializeField] private float maxHealth = 3f;
    public bool canDie = true, canTakeDamage = true, isAlive = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = maxHealth;
    }

    public void SetMaxHealth(float pMaxHealth)
    {
        maxHealth = pMaxHealth;
    }
    public void SetHealth(float pHealth)
    {
        health = pHealth;
    }
    public float GetHealth()
    {
        return health;
    }

    public void TakeDamage(float pDamage)
    {
        if (!canTakeDamage || !isAlive) return;

        health = Mathf.Max(health - pDamage, 0);

        if (ShouldDie() && canDie == true) Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
    public bool ShouldDie()
    {
        return health <= 0;
    }

    public void HealHealth(float pHealing)
    {
        health = Mathf.Min(health + pHealing, maxHealth);
    }

}
