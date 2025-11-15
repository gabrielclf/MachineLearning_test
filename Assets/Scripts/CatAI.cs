using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CatAI : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public GameObject player;
    public float moveSpeed = 5f;

    private Rigidbody2D rb;

    // Rede neural pra movimentação:
    static NeuralNet neuralNet;
    List<DataSet> roundData = new();
    bool trainedAtLeastOnce = false;
    Vector2 lastPlayerPos;

    // Replay buffer (memória de experiência)
    [Header("Replay / Training")]
    public int maxMemorySize = 2000; // quantos exemplos memorizar
    public int batchSize = 128; // quantos exemplos usar por treino
    public int trainEpochsPerRound = 20; // quantas épocas por rodada de treino
    [Range(0.005f, 0.3f)] public float onlineLearnRate = 0.05f; // taxa de aprendizado para treinamento online (ajustável)
    List<DataSet> replayBuffer = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindWithTag("Player");

        if (neuralNet == null) neuralNet = new NeuralNet(4, 8, 2, 2, onlineLearnRate, 0.9);
        else neuralNet.LearnRate = onlineLearnRate;

        lastPlayerPos = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 diff = playerPos - (Vector2)transform.position;

        Vector2 playerVel = (playerPos - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = playerPos;
        
        double[] input =
        {
            Mathf.Clamp(diff.x / 10f, -1, 1),
            Mathf.Clamp(diff.y / 10f, -1, 1),
            Mathf.Clamp(playerVel.x / 10f, -1, 1),
            Mathf.Clamp(playerVel.y / 10f, -1, 1),
        };

        if (!trainedAtLeastOnce)
        {
            TrainRound();
            NewRound();
        } else
        {
            SmartMovement(input);
        }

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0f || Mathf.Abs(rb.linearVelocity.y) > 0f;
        animator.SetBool("isMoving", isMoving);
        if(rb.linearVelocity.x < 0) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;

        CollectRoundData(input, diff);
    }

    void CollectRoundData(double[] input, Vector2 diff)
    {
        double targetX = diff.x > 0 ? -1.0 : 1.0;
        double targetY = diff.y > 0 ? -1.0 : 1.0;

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
        // 1) adiciona roundData ao replayBuffer
        foreach (var ds in roundData)
        {
            replayBuffer.Add(ds);
        }

        // 2) limita tamanho do replayBuffer (FIFO)
        if (replayBuffer.Count > maxMemorySize)
        {
            int overflow = replayBuffer.Count - maxMemorySize;
            replayBuffer.RemoveRange(0, overflow);
        }

        // 3) escolhe batch
        List<DataSet> batch;
        if (replayBuffer.Count <= batchSize) batch = new List<DataSet>(replayBuffer);
        else batch = SampleRandom(replayBuffer, batchSize);

        // Diagnóstico: calcula erro médio antes do treino
        float beforeErr = CalculateBatchError(batch);

        // 4) treina por algumas épocas (use poucas épocas por round; 3-8 costuma ser bom)
        neuralNet.LearnRate = onlineLearnRate;
        int epochs = Mathf.Clamp(trainEpochsPerRound, 1, 10); // protege contra valores muito altos
        neuralNet.Train(batch, epochs);

        float afterErr = CalculateBatchError(batch);
        Debug.Log($"[TrainRound] batch {batch.Count} epochs {epochs} LR {neuralNet.LearnRate:F4}  err before {beforeErr:F4} after {afterErr:F4}");

        trainedAtLeastOnce = true;
    }

    float CalculateBatchError(List<DataSet> batch)
    {
        if (batch == null || batch.Count == 0) return 0f;
        double sum = 0.0;
        foreach (var ds in batch)
        {
            var output = neuralNet.Compute(ds.Values);
            // soma erro absoluto por saída
            sum += System.Math.Abs(output[0] - ds.Targets[0]);
            sum += System.Math.Abs(output[1] - ds.Targets[1]);
        }
        // retorna erro médio por saída
        return (float)(sum / (batch.Count * 2));
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

    void SmartMovement(double[] input)
    {
        double[] output = neuralNet.Compute(input);
        
        // Mapeia saída 0..1 pra -1..1 (caso a rede esteja usando sigmoid)
        float ox = (float)output[0];
        float oy = (float)output[1];

        // Se a rede já usa tanh, ela já está no range certo — mas esse clamp protege de valores bizarros
        ox = Mathf.Clamp(ox, -1f, 1f);
        oy = Mathf.Clamp(oy, -1f, 1f);

        Vector2 fleeDirection = new(ox, oy);

        if (fleeDirection.sqrMagnitude < 0.01f) fleeDirection = Vector2.zero;

        rb.linearVelocity = fleeDirection.normalized * moveSpeed;
    }
}
