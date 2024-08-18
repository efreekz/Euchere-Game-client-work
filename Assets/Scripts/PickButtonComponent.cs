using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PickButtonComponent : MonoBehaviour
{
    // Start is called before the first frame update
    private bool Visible;
    private bool Selected;
    private GameObject GameManagerObject;

    void Start()
    {
        Visible = false;
        gameObject.GetComponent<Text>().enabled = false;
        //GameManagerObject = GameObject.Find("GameManager");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Selected && Visible && Input.GetButton("Click"))
        {
            //GameManagerObject.GetComponent<GameManagerComponent>().PickButtonClicked();
        }
    }

    public void SetVisible(bool NewVisible)
    {
        gameObject.GetComponent<Text>().enabled = NewVisible;
        Visible = NewVisible;
    }

    public void SetSelected(bool NewSelected)
    {
        Selected = NewSelected;
    }

    void OnMouseEnter()
    {
        Selected = true;
    }

    void OnMouseExit()
    {
        Selected = false;
    }
}
