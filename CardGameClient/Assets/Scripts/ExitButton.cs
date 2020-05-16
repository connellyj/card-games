using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour
{
    void Start()
    {
        gameObject.GetComponent<Button>().onClick.AddListener(Application.Quit);
    }
}
