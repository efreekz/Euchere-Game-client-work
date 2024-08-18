using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using TMPro;

public class ExitButton : MonoBehaviourPun
{
    public TextMeshProUGUI infoText;
    
    public void OnExitButtonClicked()
    {
        // Use Photon to notify all players to load Scene 1
        PhotonView photonView = PhotonView.Get(this);
        if(PhotonNetwork.InRoom)
        {
            photonView.RPC("LoadScene", RpcTarget.All);
        }
        else
        {
            SceneManager.LoadScene(0);
            PhotonNetwork.Disconnect();
        }
    }

    [PunRPC]
    public void LoadScene()
    {
        PhotonNetwork.Disconnect();
        // Load the specified scene
        SceneManager.LoadScene(0);
    }
}
