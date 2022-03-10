using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; protected set; }

    public TMP_Text PlayerTurnText;

    public TMP_Text UsingManualDollyText;


    private void Awake()
    {
        Instance = this;
        UsingManualDollyText.enabled = false;
    }
}
