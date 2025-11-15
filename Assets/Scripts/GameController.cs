using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public GameObject player;
    public TextMeshProUGUI timeScaleText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetTimeScale(float value)
    {
        Time.timeScale = value;
        timeScaleText.text = "Time Scale: " + (int)value;
    }
    
    public void SetAutoMove(bool autoMove)
    {
        player.GetComponent<AutoPlayerMove>().active = autoMove;
    }
}
