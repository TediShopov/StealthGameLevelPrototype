using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<CharacterController2D>() != null) 
        {
            Debug.Log("Game is won");
            //SceneManager.LoadScene(0);
        }
    }
}
