using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorVisiblity : MonoBehaviour
{
    [SerializeField] InputControler inputs;

    private void Start()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        if (inputs.Pause()) Cursor.visible = true;
        else Cursor.visible = false;
    }
}
