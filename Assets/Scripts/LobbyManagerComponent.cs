using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManagerComponent : MonoBehaviour
{
    public InputField InputFieldObject;
    private GameObject TextObject;
    public Text MainTextObject;
    private string Name = "";
    private string RoomID = "";
    private int TeamAttempt = 0;
    // Update is called once per frame

    public int GetTeamAttempt()
    {
        return TeamAttempt;
    }

    public string GetName()
    {
        return Name;
    }

    public string GetRoomID()
    {
        return RoomID;
    }

    public void Accept()
    {
        if (InputFieldObject.text.Length !=0)
        {
            Name = InputFieldObject.text;
            Debug.Log("Name: " + Name);
        }
    }
    public void Public()
    {
        MainTextObject.text = "Attempting to join an open public game...";
        DontDestroyOnLoad(this.gameObject);
        SceneManager.LoadScene("EuchreGame");
    }

    public void JoinTeam(int Team)
    {
        TeamAttempt = Team;
        MainTextObject.text = "Attempting to join room of ID code: " + RoomID;
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene("EuchreGame");
    }
}
