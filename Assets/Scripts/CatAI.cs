using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SteeringBehaviour))]
public class CatAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public GameObject player;
    public SteeringBehaviour steering;

    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("quanto mais alto, mais 'instantâneo' é o ajuste da velocidade")]
    public float acceleration = 20f;

    private Rigidbody2D rb;

    // Rede neural
    static NeuralNet neuralNet;
    List<DataSet> roundData = new();
    bool trainedAtLeastOnce = false;
    Vector2 lastPlayerPos;

    // Replay buffer (memória de experiência)
    [Header("Training")]
    public int maxMemorySize = 2000; // quantos exemplos memorizar
    public int batchSize = 128; // quantos exemplos usar por treino
    [Range(1, 10)] public int trainEpochsPerRound = 3; // quantas épocas por rodada de treino
    [Range(0.005f, 0.3f)] public float onlineLearnRate = 0.05f; // taxa de aprendizado; Quanto menor, mais lento ele aprende, mas de forma mais precisa.
    List<DataSet> replayBuffer = new();

    //Movimento interno
    private Vector2 networkDesiredDir = Vector2.zero; // calculado no CalculateDesiredDir()
    private Vector2 appliedVelocity = Vector2.zero; // usado no FixedUpdate()

    // quanto peso dar à avoidance na hora de montar a target direction pra treinar
    [Range(0f, 3f)] public float avoidanceInfluenceOnTarget = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindWithTag("Player");
        steering = GetComponent<SteeringBehaviour>();

        if (neuralNet == null) neuralNet = new NeuralNet(7, 12, 2, 2, onlineLearnRate, 0.9);
        else neuralNet.LearnRate = onlineLearnRate;

        lastPlayerPos = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        Vector2 playerPos = player.transform.position;
        Vector2 diff = playerPos - (Vector2)transform.position;
        Vector2 playerVel = (playerPos - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = playerPos;

        Vector2 avoidance = steering.GetAvoidanceValue();
        float avoidWeight = Mathf.Max(steering.avoidanceWeight, 0.0001f);
        Vector2 avoidanceNormalized = avoidance / avoidWeight;
        float avoidanceMagnitude = Mathf.Clamp01(avoidance.magnitude / avoidWeight);
        
        double[] input =
        {
            Mathf.Clamp(diff.x / 10f, -1, 1),
            Mathf.Clamp(diff.y / 10f, -1, 1),
            Mathf.Clamp(playerVel.x / 10f, -1, 1),
            Mathf.Clamp(playerVel.y / 10f, -1, 1),
            Mathf.Clamp(avoidanceNormalized.x, -1f, 1f),
            Mathf.Clamp(avoidanceNormalized.y, -1f, 1f),
            Mathf.Clamp(avoidanceMagnitude, 0.0f, 1.0f),
        };

        if (!trainedAtLeastOnce)
        {
            TrainRound();
            NewRound();
        } else
        {
            CalculateDesiredDir(input);
        }

        bool isMoving = rb.linearVelocity.sqrMagnitude > 0.0001f;
        animator.SetBool("isMoving", isMoving);
        if(rb.linearVelocity.x < 0) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;

        CollectRoundData(input, diff, avoidanceNormalized);
    }

    void FixedUpdate()
    {
        // combina network output com steering behaviour
        Vector2 net = networkDesiredDir;
        Vector2 avoidance = steering.GetAvoidanceValue();
        Vector2 finalDir;

        if (avoidance.sqrMagnitude > 0.0001f)
        {
            // o cálculo abaixo prioriza avoidance, mas mistura com a rede.
            float avoidPriority = Mathf.Clamp01(avoidance.magnitude / steering.avoidanceWeight);
            finalDir = Vector2.Lerp(net, avoidance.normalized, avoidPriority);
        }
        else
        {
            finalDir = net;
        }

        if (finalDir.sqrMagnitude < 0.0001f)
        {
            appliedVelocity = Vector2.MoveTowards(appliedVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 targetVel = finalDir.normalized * moveSpeed;
            appliedVelocity = Vector2.MoveTowards(appliedVelocity, targetVel, acceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = appliedVelocity;
    }

    void CollectRoundData(double[] input, Vector2 diff, Vector2 avoidanceNormalized)
    {
        // direção oposta ao diff: fugir do player
        Vector2 fleeDir = (-diff).normalized;

        // combina flee + avoidance pra criar uma direção pra treinar
        Vector2 avoidanceComponent = avoidanceNormalized * avoidanceInfluenceOnTarget;
        Vector2 desired = (fleeDir + avoidanceComponent).normalized;

        double targetX = Mathf.Clamp(desired.x, -1f, 1f);
        double targetY = Mathf.Clamp(desired.y, -1f, 1f);

        roundData.Add(new DataSet(input, new double[]
        {
            targetX,
            targetY,
        }));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            TrainRound();
            NewRound();
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        OnTriggerEnter2D(collision);
    }

    void TrainRound()
    {
        // adiciona roundData ao replayBuffer
        foreach (var ds in roundData)
        {
            replayBuffer.Add(ds);
        }

        // limita tamanho do replayBuffer (FIFO)
        if (replayBuffer.Count > maxMemorySize)
        {
            int overflow = replayBuffer.Count - maxMemorySize;
            replayBuffer.RemoveRange(0, overflow);
        }

        // escolhe batch aleatoriamente
        List<DataSet> batch;
        if (replayBuffer.Count <= batchSize) batch = new List<DataSet>(replayBuffer);
        else batch = SampleRandom(replayBuffer, batchSize);

        // 4) treina por algumas épocas
        neuralNet.LearnRate = onlineLearnRate;
        neuralNet.Train(batch, trainEpochsPerRound);

        trainedAtLeastOnce = true;
    }

    List<DataSet> SampleRandom(List<DataSet> source, int n)
    {
        var selected = new List<DataSet>(n);
        int count = source.Count;
        if (n >= count) return new List<DataSet>(source);

        var used = new HashSet<int>();
        for (int i = 0; i < n; i++)
        {
            int idx;
            do { idx = Random.Range(0, count); } while (!used.Add(idx));
            selected.Add(source[idx]);
        }
        return selected;
    }

    void NewRound()
    {
        roundData.Clear();

        transform.position = new Vector3(Random.Range(-7f, 7f), Random.Range(-3f, 3f));
        player.transform.position = new Vector3(Random.Range(-7f, 7f), Random.Range(-3f, 3f));

        rb.linearVelocity = Vector2.zero;

        lastPlayerPos = player.transform.position;
    }

    void CalculateDesiredDir(double[] input)
    {
        // calcula direção desejada
        double[] output = neuralNet.Compute(input);
        
        float ox = (float)output[0];
        float oy = (float)output[1];

        networkDesiredDir = new Vector2(ox, oy);
        if (networkDesiredDir.sqrMagnitude < 0.0001f) networkDesiredDir = Vector2.zero;
    }
}
