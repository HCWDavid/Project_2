using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class VBTn : MonoBehaviour
{
    public GameObject text;
    public VirtualButtonBehaviour Vb;
    // Start is called before the first frame update
    void Start()
    {
        
        
        Vb.RegisterOnButtonPressed(OnButtonPressed);
        Vb.RegisterOnButtonReleased(OnButtonReleased);

        text.SetActive(false);
    }

    public void OnButtonPressed(VirtualButtonBehaviour vb)
    {
        text.SetActive(true);
    }

    public void OnButtonReleased(VirtualButtonBehaviour vb)
    {
        text.SetActive(false);
    }



}
