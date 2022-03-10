using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExitHelp : MonoBehaviour
{
    public Button exitButton;
    public GameObject helpMenu;
    // Start is called before the first frame update
    void Start()
    {
       exitButton.onClick.AddListener(ExitPressed); 
    }

    void ExitPressed()
    {
        helpMenu.SetActive(false);
    }
}
