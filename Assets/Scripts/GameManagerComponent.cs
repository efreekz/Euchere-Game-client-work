using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;

public class GameManagerComponent : MonoBehaviourPunCallbacks
{
    // Existing variables
    private readonly List<string> Types = new List<string>() { "9", "T", "J", "Q", "K", "A" };
    private readonly List<string> Suits = new List<string>() { "D", "H", "S", "C" };
    private List<string> Deck = new List<string>();
    private List<string> Hands = new List<string>();
    private List<string> PlayedCards = new List<string>();
    private List<GameObject> DisplayedCards = new List<GameObject>();
    private readonly List<GameObject> PickSuitButtons = new List<GameObject>();
    public Transform placedPile;
    public GameObject Card;
    public Transform cardsParent;
    public GameObject PickSuitButton;
    public GameObject MainTextObject;
    public Text WaitingTextObject;
    public GameObject StatsTextObject;
    public GameObject drawPile;
    private bool Host;
    private int PlayerNum;
    private int Players;
    private bool Loaded;
    private int Turn;
    private int Dealer;
    private int Team1Score;
    private int Team2Score;
    private int Team1Tricks;
    private int Team2Tricks;
    private char Trump;
    private int TrumpPickerTeam;
    private int Cycled;
    public GameObject PassButtonObject;
    public GameObject PickButtonObject;
    public GameObject GoAloneButtonObject;
    private GameObject LobbyManagerObject;
    private readonly Dictionary<char, char> AlternateSuits = new Dictionary<char, char> { { 'S', 'C' }, { 'C', 'S' }, { 'H', 'D' }, { 'D', 'H' } };
    private int Ticks = 0;
    private readonly Queue<List<string>> Waits = new Queue<List<string>>();
    private Dictionary<int, string> PlayerNames = new Dictionary<int, string>();
    private bool Skip;
    private string PlayerID;
    private int PlayerToSkip = -1;
    private readonly List<int> PlayersJoined = new List<int>();

    // New Unity Event for Victory
    public UnityEvent OnVictory;

    // Hourglasses for each player
    public GameObject[] Hourglasses = new GameObject[4];

    // New UI elements for dealer, trump, and scores
    public Text dealerText;
    public Text trumpText;
    public Text scoreText; // New Text component for displaying scores

    // Start is called before the first frame update
    void Start()
    {
        LobbyManagerObject = GameObject.Find("LobbyManager");
        Host = false;
        Loaded = false;
        PhotonNetwork.ConnectUsingSettings();
        Team1Score = 0;
        Team2Score = 0;

        // Initialize the OnVictory event
        if (OnVictory == null)
        {
            OnVictory = new UnityEvent();
        }

        // You can add listeners to the OnVictory event here or in the Unity Editor
        OnVictory.AddListener(DisplayVictoryScreen);

        // Initialize hourglasses
        InitializeHourglasses();

        // Initialize UI elements
        UpdateDealerText();
        UpdateTrumpText();
        UpdateScoreText(); // Update the score text on start
    }

    // Method to initialize hourglasses
    private void InitializeHourglasses()
    {
        for (int i = 0; i < Hourglasses.Length; i++)
        {
            Hourglasses[i].SetActive(false);
        }
    }

    // Method to activate the hourglass of the current player
    private void ActivateHourglass(int playerIndex)
    {
        for (int i = 0; i < Hourglasses.Length; i++)
        {
            // Activate the hourglass if it's not the local player's turn
            Hourglasses[i].SetActive(i == playerIndex && playerIndex != PlayerNum);
        }
    }

    // FixedUpdate method
    void FixedUpdate()
    {
        if (Waits.Count > 0)
        {
            Ticks += 1;
            if (Ticks > 120)
            {
                Skip = true;
                HandleWaitQueue();
                Waits.Dequeue();
                Ticks = 0;
                Skip = false;
            }
        }
    }

    public void ScoreTrick()
    {
        List<int> Best = new List<int> { 0, 0 }; // 0th - best player, 1st - best card
        char StartingSuit = PlayedCards[0][1];
        if (PlayedCards[0][0] == 'J' && PlayedCards[0][1] == AlternateSuits[Trump])
        {
            StartingSuit = AlternateSuits[Trump];
        }
        PlayedCards.Reverse();
        for (int i = 0; i < PlayedCards.Count; i++)
        {
            if (EvaluateCard(PlayedCards[i], StartingSuit) > Best[1])
            {
                Best[1] = EvaluateCard(PlayedCards[i], StartingSuit);
                Best[0] = Turn;
                for (int i2 = 0; i2 < i; i2++)
                {
                    Best[0] -= 1;
                    if (Best[0] < 0)
                    {
                        Best[0] += 4;
                    }
                    else if (Best[0] > 3)
                    {
                        Best[0] -= 4;
                    }
                    if (Best[0] == PlayerToSkip)
                    {
                        Best[0] -= 1;
                    }
                    if (Best[0] < 0)
                    {
                        Best[0] += 4;
                    }
                    else if (Best[0] > 3)
                    {
                        Best[0] -= 4;
                    }
                }
            }
        }
        Turn = Best[0] - 1;
        if ((Best[0] % 2) == 0)
        {
            Team1Tricks += 1;
        }
        else
        {
            Team2Tricks += 1;
        }

        // Move cards to the winning player's side
        MoveCardsToWinner(Best[0]);

        PlayedCards = new List<string>();
        int Removed = 0;
        int Temp = DisplayedCards.Count;
        for (int i = 0; i < Temp; i++)
        {
            if (!(DisplayedCards[i - Removed].GetComponent<CardComponent>().GetTargetPosition().y == -600))
            {
                Destroy(DisplayedCards[i - Removed]);
                DisplayedCards.RemoveAt(i - Removed);
                Removed += 1;
            }
        }
        if (Team1Tricks + Team2Tricks == 5)
        {
            int Temp2 = DisplayedCards.Count;
            for (int i = 0; i < Temp2; i++)
            {
                Destroy(DisplayedCards[0]);
                DisplayedCards.RemoveAt(0);
            }
            if (TrumpPickerTeam == 0)
            {
                if (Team1Tricks == 5)
                {
                    if (PlayerToSkip != -1)
                    {
                        Team1Score += 4;
                    }
                    else
                    {
                        Team1Score += 2;
                    }
                }
                else if (Team1Tricks > 2)
                {
                    Team1Score += 1;
                }
                else
                {
                    Team2Score += 2;
                }
            }
            else
            {
                if (Team2Tricks == 5)
                {
                    if (PlayerToSkip != -1)
                    {
                        Team2Score += 4;
                    }
                    else
                    {
                        Team2Score += 2;
                    }
                }
                else if (Team2Tricks > 2)
                {
                    Team2Score += 1;
                }
                else
                {
                    Team1Score += 2;
                }
            }

            // Check for victory after updating the scores
            CheckForVictory();

            // Update score display after scoring
            UpdateScoreText();

            Loaded = false;
            Deck = new List<string>();
            Hands = new List<string>();
            PlayedCards = new List<string>();
            DisplayedCards = new List<GameObject>();
            if (Host)
            {
                Initialise();
                PhotonView.Get(this).RPC("SyncOtherPlayer", RpcTarget.Others, string.Join("", Hands), string.Join("", Deck), -1, "", -1, -2);
                PhotonView.Get(this).RPC("GameStart", RpcTarget.All, PlayerNames[0], PlayerNames[1], PlayerNames[2], PlayerNames[3]);
            }
        }
        else
        {
            DoRound();
        }
    }

    // Method to handle the wait queue
    private void HandleWaitQueue()
    {
        switch (Waits.Peek()[0])
        {
            case "Score":
                ScoreTrick();
                break;
            case "JoinTimeout":
                HandleJoinTimeout();
                break;
            case "Play":
                PlayCard(Waits.Peek()[1]);
                Ticks = 60;
                break;
            case "Sync":
                SyncOtherPlayer(Waits.Peek()[1], Waits.Peek()[2], int.Parse(Waits.Peek()[3]), Waits.Peek()[4], int.Parse(Waits.Peek()[5]), int.Parse(Waits.Peek()[6]));
                break;
            case "Start":
                GameStart(Waits.Peek()[1], Waits.Peek()[2], Waits.Peek()[3], Waits.Peek()[4]);
                break;
            case "Picked":
                PickedUp(int.Parse(Waits.Peek()[1]), int.Parse(Waits.Peek()[2]));
                break;
            case "Phase":
                PhaseOne();
                break;
            case "Begin":
                BeginRounds(Waits.Peek()[1], int.Parse(Waits.Peek()[2]));
                break;
            case "Turnover":
                TurnoverTrump(int.Parse(Waits.Peek()[1]));
                break;
            case "Join":
                PlayerJoin(Waits.Peek()[1], Waits.Peek()[2], int.Parse(Waits.Peek()[3]));
                break;
        }
    }

    // Method to handle join timeout
    private void HandleJoinTimeout()
    {
        if (Loaded)
        {
            // Existing logic
        }
        else
        {
            RoomOptions roomOptions = new RoomOptions { IsVisible = true, MaxPlayers = 4 };
            string RoomID = "";
            for (int _ = 0; _ < 10; _++)
            {
                RoomID += Random.Range(0, 9).ToString();
            }
            PhotonNetwork.JoinOrCreateRoom(RoomID, roomOptions, TypedLobby.Default);
        }
    }

    // Existing methods...

    // Method to check for victory
    public void CheckForVictory()
    {
        int winningScore = 10; // Set your winning score here
        if (Team1Score >= winningScore || Team2Score >= winningScore)
        {
            OnVictory.Invoke();
        }
    }

    // Method to display the victory screen
    public void DisplayVictoryScreen()
    {
        // You can create and show a UI element indicating victory here
        // For example, activating a VictoryPanel GameObject
        GameObject victoryPanel = GameObject.Find("VictoryPanel");
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            TextMeshProUGUI victoryText = victoryPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (Team1Score >= 10)
            {
                victoryText.text = "Team 1 Wins!";
            }
            else
            {
                victoryText.text = "Team 2 Wins!";
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if (LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetRoomID().Length == 0)
        {
            PhotonNetwork.JoinRandomRoom();
            Waits.Enqueue(new List<string> { "JoinTimeout" });
        }
        else
        {
            RoomOptions roomOptions = new RoomOptions { IsVisible = false, MaxPlayers = 4 };
            PhotonNetwork.JoinOrCreateRoom(LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetRoomID(), roomOptions, TypedLobby.Default);
        }
    }

    [PunRPC]
    public void PlayerJoin(string NewName, string JoiningPlayerID, int JoiningTeamAttempt)
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Join", NewName, JoiningPlayerID, JoiningTeamAttempt.ToString() });
        }
        else
        {
            HandlePlayerJoin(NewName, JoiningPlayerID, JoiningTeamAttempt);
        }
    }

    private void HandlePlayerJoin(string NewName, string JoiningPlayerID, int JoiningTeamAttempt)
    {
        if (Players < 4) //Prevents issues with Photon letting too many people any a game
        {
            Players += 1;
            WaitingTextObject.text = "Waiting for users (" + Players.ToString() + "/4)";
            Debug.Log("Waiting for users (" + Players.ToString() + "/4)");
            if (Host)
            {
                int Temp = DetermineTeamPosition(JoiningTeamAttempt);
                PlayerNames[Temp] = NewName;
                PlayersJoined.Add(Temp);
                PhotonView.Get(this).RPC("SyncOtherPlayer", RpcTarget.Others, string.Join("", Hands), string.Join("", Deck), Players, JoiningPlayerID, Temp, Dealer);
                if (Players == 4)
                {
                    PhotonView.Get(this).RPC("GameStart", RpcTarget.All, PlayerNames[0], PlayerNames[1], PlayerNames[2], PlayerNames[3]);
                }
            }
        }
    }

    private int DetermineTeamPosition(int JoiningTeamAttempt)
    {
        int Temp;
        if (PlayersJoined.Contains(JoiningTeamAttempt))
        {
            if (PlayersJoined.Contains(JoiningTeamAttempt + 2))
            {
                if (PlayersJoined.Contains(JoiningTeamAttempt + 1))
                {
                    if (JoiningTeamAttempt + 3 > 3)
                    {
                        Temp = JoiningTeamAttempt - 1;
                    }
                    else
                    {
                        Temp = JoiningTeamAttempt + 3;
                    }
                }
                else
                {
                    Temp = JoiningTeamAttempt + 1;
                }
            }
            else
            {
                Temp = JoiningTeamAttempt + 2;
            }
        }
        else
        {
            Temp = JoiningTeamAttempt;
        }
        return Temp;
    }

    [PunRPC]
    public void GameStart(string P1Name, string P2Name, string P3Name, string P4Name)
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Start", P1Name, P2Name, P3Name, P4Name });
        }
        else
        {
            HandleGameStart(P1Name, P2Name, P3Name, P4Name);
        }
    }

    private void HandleGameStart(string P1Name, string P2Name, string P3Name, string P4Name)
    {
        GameObject.Find("transition").GetComponent<Animator>().SetTrigger("out");
        PlayerNames = new Dictionary<int, string> { { 0, P1Name }, { 1, P2Name }, { 2, P3Name }, { 3, P4Name } };
        MainTextObject.GetComponent<UnityEngine.UI.Text>().text = "Start!";
        Team1Tricks = 0;
        Team2Tricks = 0;
        PlayerToSkip = -1;
        Trump = 'N';
        Dealer += 1;
        if (Dealer > 3)
        {
            Dealer = 0;
        }
        Turn = Dealer;
        Cycled = 0;
        PhaseOne();
    }

    [PunRPC]
    public void PhaseOne()
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Phase" });
        }
        else
        {
            HandlePhaseOne();
        }
    }

    private void HandlePhaseOne()
    {
        Turn += 1;
        if (Turn == Dealer + 1)
        {
            Cycled += 1;
            if (Cycled == 2)
            {
                TurnoverTrump(-1); //-1 is placeholder as turnover trump and set picker team are not seperate yet
            }
        }
        if (Turn > 3)
        {
            Turn = 0;
        }
        DisplayStatsText();
        DisplayTurnText();
        if (Turn == PlayerNum)
        {
            PickButtonObject.GetComponent<PickButtonComponent>().SetVisible(true);
            GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetVisible(true);
            if (Cycled != 2 || Turn != Dealer)
            {
                PassButtonObject.GetComponent<PassButtonComponent>().SetVisible(true);
            }
        }
        ActivateHourglass(Turn);
    }

    public void Pass()
    {
        PassButtonObject.GetComponent<PassButtonComponent>().SetVisible(false);
        PassButtonObject.GetComponent<PassButtonComponent>().SetSelected(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetSelected(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetVisible(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetSelected(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetVisible(false);
        PhotonView.Get(this).RPC("PhaseOne", RpcTarget.All);
    }

    public void PickButtonClicked()
    {
        PassButtonObject.GetComponent<PassButtonComponent>().SetSelected(false);
        PassButtonObject.GetComponent<PassButtonComponent>().SetVisible(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetSelected(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetVisible(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetSelected(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetVisible(false);
        if (Cycled == 2)
        {
            List<Vector3> TempPositions = new List<Vector3> { new Vector3(-1000, 525, 0), new Vector3(-500, 525, 0), new Vector3(-750, 100, 0) };
            foreach (string Suit in Suits)
            {
                if (Suit != Deck[0][1].ToString())
                {
                    PickSuitButtons.Add(Instantiate(PickSuitButton, TempPositions[0], transform.rotation));
                    PickSuitButtons[PickSuitButtons.Count - 1].GetComponent<PickSuitButtonComponent>().SetSuit(Suit);
                    TempPositions.RemoveAt(0);
                }
            }
        }
        else
        {
            PhotonView.Get(this).RPC("PickedUp", RpcTarget.All, PlayerNum % 2, PlayerToSkip);
        }
    }

    public void GoAloneButtonClicked()
    {
        PassButtonObject.GetComponent<PassButtonComponent>().SetSelected(false);
        PassButtonObject.GetComponent<PassButtonComponent>().SetVisible(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetSelected(false);
        PickButtonObject.GetComponent<PickButtonComponent>().SetVisible(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetSelected(false);
        GoAloneButtonObject.GetComponent<GoAloneButtonComponent>().SetVisible(false);
        PlayerToSkip = PlayerNum + 2;
        if (PlayerToSkip > 3)
        {
            PlayerToSkip -= 4;
        }
        if (Cycled == 2)
        {
            List<Vector3> TempPositions = new List<Vector3> { new Vector3(-1000, 525, 0), new Vector3(-500, 525, 0), new Vector3(-750, 100, 0) };
            foreach (string Suit in Suits)
            {
                if (Suit != Deck[0][1].ToString())
                {
                    PickSuitButtons.Add(Instantiate(PickSuitButton, TempPositions[0], transform.rotation));
                    PickSuitButtons[PickSuitButtons.Count - 1].GetComponent<PickSuitButtonComponent>().SetSuit(Suit);
                    TempPositions.RemoveAt(0);
                }
            }
        }
        else
        {
            PhotonView.Get(this).RPC("PickedUp", RpcTarget.All, PlayerNum % 2, PlayerToSkip);
        }
    }

    public void PickTrumpSuit(string Suit)
    {
        int Temp = PickSuitButtons.Count;
        for (int i = 0; i < Temp; i++)
        {
            Destroy(PickSuitButtons[0]);
            PickSuitButtons.RemoveAt(0);
        }
        Trump = Suit[0];
        TrumpPickerTeam = PlayerNum % 2;
        UpdateTrumpText(); // Update the trump text on UI
        PhotonView.Get(this).RPC("BeginRounds", RpcTarget.All, Trump.ToString(), PlayerToSkip);
    }

    [PunRPC]
    public void PickedUp(int NewTrumpPickerTeam, int NewPlayerToSkip)
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Picked", NewTrumpPickerTeam.ToString(), NewPlayerToSkip.ToString() });
        }
        else
        {
            HandlePickedUp(NewTrumpPickerTeam, NewPlayerToSkip);
        }
    }

    private void HandlePickedUp(int NewTrumpPickerTeam, int NewPlayerToSkip)
    {
        if (PlayerNum == Dealer)
        {
            DisplayedCards[DisplayedCards.Count - 1].GetComponent<CardComponent>().SetTargetPosition(drawPile.transform.position);
            Trump = Deck[0][1];
            TrumpPickerTeam = NewTrumpPickerTeam;
            PlayerToSkip = NewPlayerToSkip;
            Hands.Insert((PlayerNum + 1) * (Hands.Count / 4) - 1, Deck[0]);
            MainTextObject.GetComponent<UnityEngine.UI.Text>().text = "Discard a card in your hand";
            DisplayStatsText();
            foreach (GameObject Card in DisplayedCards)
            {
                Card.GetComponent<CardComponent>().SetUnlocked(new List<bool> { false, true, false });
            }
            PhotonView.Get(this).RPC("TurnoverTrump", RpcTarget.Others, TrumpPickerTeam);
        }
        else
        {
            MainTextObject.GetComponent<UnityEngine.UI.Text>().text = "Dealer is discarding a card";
        }
    }

    public void Discarded(string CardType)
    {
        Hands.Remove(CardType);
        foreach (GameObject Card in DisplayedCards)
        {
            Destroy(Card);
        }
        DisplayedCards = new List<GameObject>();
        CreateHand(PlayerNum);
        Trump = Deck[0][1];
        PhotonView.Get(this).RPC("BeginRounds", RpcTarget.All, Trump.ToString(), PlayerToSkip);
    }

    [PunRPC]
    public void BeginRounds(string TrumpSuit, int NewPlayerToSkip)
    {
        if (Waits.Count < 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Begin", TrumpSuit, NewPlayerToSkip.ToString() });
        }
        else
        {
            HandleBeginRounds(TrumpSuit, NewPlayerToSkip);
        }
    }

    private void HandleBeginRounds(string TrumpSuit, int NewPlayerToSkip)
    {
        PlayerToSkip = NewPlayerToSkip;
        Trump = char.Parse(TrumpSuit);
        Turn = Dealer;
        DoRound();
    }

    [PunRPC]
    public void PlayCard(string CardType)
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Play", CardType });
        }
        else
        {
            HandlePlayCard(CardType);
        }
    }

    private void HandlePlayCard(string CardType)
    {
        PlayedCards.Add(CardType);
        if (!(Turn == PlayerNum))
        {
            Vector3 Position = new Vector3(-3000, 300, PlayedCards.Count * -1 - 1);
            if ((Turn + 1) % 2 == (PlayerNum + 1) % 2)
            {
                Position = new Vector3(-100 + PlayedCards.Count * 100, 2000, PlayedCards.Count * -1 - 1);
            }
            if (PlayerNum == 0 && Turn == 3)
            {
                Position = new Vector3(3000, 300, PlayedCards.Count * -1 - 1);
            }
            else if (PlayerNum == 3 && Turn == 2)
            {
                Position = new Vector3(3000, 300, PlayedCards.Count * -1 - 1);
            }
            else if ((PlayerNum == 1 || PlayerNum == 2) && Turn < PlayerNum)
            {
                Position = new Vector3(3000, 300, PlayedCards.Count * -1 - 1);
            }
            PlaceCard(CardType, Position, new Vector3(-100 + PlayedCards.Count * 100, 300, PlayedCards.Count * -1 - 1));
        }
        int Num = 4;
        if (PlayerToSkip != -1)
        {
            Num -= 1;
        }
        if (PlayedCards.Count == Num)
        {
            Ticks = 0;
            Waits.Enqueue(new List<string> { "Score" });
        }
        else
        {
            DoRound();
        }
    }

    public int EvaluateCard(string CardType, char StartSuit)
    {
        if (CardType[1] == AlternateSuits[Trump] && CardType[0] == 'J')
        {
            return 14;
        }
        else if (CardType[1] == Trump)
        {
            if (CardType[0] == 'J')
            {
                return 15;
            }
            else if (CardType[0] == 'A')
            {
                return 13;
            }
            else if (CardType[0] == 'K')
            {
                return 12;
            }
            else if (CardType[0] == 'Q')
            {
                return 11;
            }
            else if (CardType[0] == 'T')
            {
                return 10;
            }
            else if (CardType[0] == '9')
            {
                return 9;
            }
            else if (CardType[0] == '8')
            {
                return 8;
            }
        }
        else if (CardType[1] == StartSuit)
        {
            if (CardType[0] == 'A')
            {
                return 7;
            }
            else if (CardType[0] == 'K')
            {
                return 6;
            }
            else if (CardType[0] == 'Q')
            {
                return 5;
            }
            else if (CardType[0] == 'J')
            {
                return 4;
            }
            else if (CardType[0] == 'T')
            {
                return 3;
            }
            else if (CardType[0] == '9')
            {
                return 2;
            }
            else if (CardType[0] == '8')
            {
                return 1;
            }
        }
        return 0;
    }

    public int GetPlayedCardsCount()
    {
        return PlayedCards.Count;
    }

    public void DoRound()
    {
        Turn += 1;
        if (Turn > 3)
        {
            Turn = 0;
        }
        if (Turn == PlayerToSkip)
        {
            Turn += 1;
            if (Turn > 3)
            {
                Turn = 0;
            }
        }
        DisplayStatsText();
        DisplayTurnText();
        if (Turn == PlayerNum)
        {
            foreach (GameObject Card in DisplayedCards)
            {
                if (Card.GetComponent<CardComponent>().GetTargetPosition().y == -600)
                {
                    Card.GetComponent<CardComponent>().SetUnlocked(new List<bool> { false, false, true });
                }
            }
        }
        ActivateHourglass(Turn);
    }

    [PunRPC]
    public void TurnoverTrump(int NewTrumpPickerTeam)
    {
        if (Waits.Count > 0 && !Skip)
        {
            Waits.Enqueue(new List<string> { "Turnover", NewTrumpPickerTeam.ToString() });
        }
        else
        {
            TrumpPickerTeam = NewTrumpPickerTeam;
            Destroy(DisplayedCards[DisplayedCards.Count - 1]);
            DisplayedCards.RemoveAt(DisplayedCards.Count - 1);
        }
    }

    public void DisplayTurnText()
    {
        if (Turn == PlayerNum)
        {
            MainTextObject.GetComponent<UnityEngine.UI.Text>().text = "Your turn";
        }
        else
        {
            MainTextObject.GetComponent<UnityEngine.UI.Text>().text = "Player " + (Turn + 1).ToString() + " (" + PlayerNames[Turn] + "'s) turn";
        }
    }

    public void DisplayStatsText()
    {
        string TrumpText = "Undecided";
        if (Trump == 'C')
        {
            TrumpText = "Clubs";
        }
        else if (Trump == 'H')
        {
            TrumpText = "Hearts";
        }
        else if (Trump == 'D')
        {
            TrumpText = "Diamonds";
        }
        else if (Trump == 'S')
        {
            TrumpText = "Spades";
        }
        StatsTextObject.GetComponent<UnityEngine.UI.Text>().text = "You are player: " + (PlayerNum + 1).ToString() +
            "\n\nYou are in: Team: " + (2 - ((PlayerNum + 1) % 2)).ToString() +
            "\n\nDealer is: Player " + (Dealer + 1).ToString() + " (" + PlayerNames[Dealer] + ")" +
            "\n\nTrump is: " + TrumpText +
            "\nPicked by: Team " + (TrumpPickerTeam + 1).ToString() +
            "\n\nTeam 1 Tricks: " + Team1Tricks.ToString() +
            "\nTeam 2 Tricks: " + Team2Tricks.ToString() +
            "\n\nTeam 1 Score: " + Team1Score.ToString() +
            "\nTeam 2 Score: " + Team2Score.ToString();

        // Update dealer, trump, and score UI elements
        UpdateDealerText();
        UpdateTrumpText();
        UpdateScoreText();
    }

    private void UpdateDealerText()
    {
        dealerText.text = $"Dealer: Player {Dealer + 1} ({PlayerNames[Dealer]})";
    }

    private void UpdateTrumpText()
    {
        string trumpName = "None";
        switch (Trump)
        {
            case 'C':
                trumpName = "Clubs";
                break;
            case 'D':
                trumpName = "Diamonds";
                break;
            case 'H':
                trumpName = "Hearts";
                break;
            case 'S':
                trumpName = "Spades";
                break;
        }
        trumpText.text = $"Trump: {trumpName}";
    }

    private void UpdateScoreText()
    {
        scoreText.text = $"Team 1 Score: {Team1Score} | Team 2 Score: {Team2Score}";
    }

    public override void OnCreatedRoom()
    {
        Host = true;
        Loaded = true;
        PlayerNum = LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetTeamAttempt();
        PlayersJoined.Add(PlayerNum);
        Players = 1;
        Dealer = Random.Range(-1, 3);
        WaitingTextObject.text = "Waiting for users (" + Players.ToString() + "/4)";
        PlayerNames[LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetTeamAttempt()] = LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetName();
        Initialise();
    }

    public override void OnJoinedRoom()
    {
        if (!Host)
        {
            WaitingTextObject.text = "Waiting for users (" + Players.ToString() + "/4)";
            PlayerID = "";
            for (int _ = 0; _ < 10; _++)
            {
                PlayerID += Random.Range(0, 9).ToString();
            }
            PhotonView.Get(this).RPC("PlayerJoin", RpcTarget.Others, LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetName(), PlayerID, LobbyManagerObject.GetComponent<LobbyManagerComponent>().GetTeamAttempt());
        }
    }

    public void Initialise()
    {
        foreach (string Type in Types)
        {
            foreach (string Suit in Suits)
            {
                Deck.Add(Type + Suit);
            }
        }
        for (int _ = 0; _ < 4; _++)
        {
            for (int __ = 0; __ < 5; __++)
            {
                int CardNum = Random.Range(0, Deck.Count);
                Hands.Add(Deck[CardNum]);
                Deck.RemoveAt(CardNum);
            }
        }
        CreateHand(PlayerNum);
        PlaceCard(Deck[0], new Vector3(0, 292.2f, 0), new Vector3(0, 292.2f, 0));
    }

    [PunRPC]
    public void SyncOtherPlayer(string NewHands, string NewDeck, int NewPlayers, string JoiningPlayerID, int NewPlayerNum, int NewDealer)
    {
        if (Waits.Count > 0 && Waits.Peek()[0] != "JoinTimeout" && !Skip)
        {
            Waits.Enqueue(new List<string> { "Sync", NewHands, NewDeck, NewPlayers.ToString(), JoiningPlayerID, NewPlayerNum.ToString(), NewDealer.ToString() });
        }
        else
        {
            HandleSyncOtherPlayer(NewHands, NewDeck, NewPlayers, JoiningPlayerID, NewPlayerNum, NewDealer);
        }
    }

    private void HandleSyncOtherPlayer(string NewHands, string NewDeck, int NewPlayers, string JoiningPlayerID, int NewPlayerNum, int NewDealer)
    {
        if (NewPlayers != -1)
        {
            Players = NewPlayers;
        }
        WaitingTextObject.text = "Waiting for users (" + Players.ToString() + "/4)";
        if (!Loaded && (JoiningPlayerID == PlayerID || JoiningPlayerID.Length == 0))
        {
            List<string> TempNewHands = new List<string>();
            for (int i = 0; i < (NewHands.Length / 2); i++)
            {
                TempNewHands.Add(NewHands[i * 2].ToString() + NewHands[i * 2 + 1].ToString());
            }
            Hands = TempNewHands;
            List<string> TempNewDeck = new List<string>();
            for (int i = 0; i < (NewDeck.Length / 2); i++)
            {
                TempNewDeck.Add(NewDeck[i * 2].ToString() + NewDeck[i * 2 + 1].ToString());
            }
            Deck = TempNewDeck;
            if (NewPlayers != -1)
            {
                PlayerNum = NewPlayerNum;
            }
            CreateHand(PlayerNum);
            PlaceCard(Deck[0], new Vector3(0, 292.2f, 0), new Vector3(0, 292.2f, 0));
            Loaded = true;
            if (NewDealer != -2)
            {
                Dealer = NewDealer;
            }
        }
    }

    public void PlaceCard(string CardType, Vector3 StartPosition, Vector3 TargetPosition)
    {
        DisplayedCards.Add(Instantiate(Card, StartPosition, transform.rotation, cardsParent));
        DisplayedCards[DisplayedCards.Count - 1].GetComponent<CardComponent>().BuildCardDictionary();
        DisplayedCards[DisplayedCards.Count - 1].GetComponent<CardComponent>().SetTargetPosition(TargetPosition);
        DisplayedCards[DisplayedCards.Count - 1].GetComponent<CardComponent>().SetCard(CardType);
        DisplayedCards[DisplayedCards.Count - 1].GetComponent<CardComponent>().SetUnlocked(new List<bool> { false, false, false });
    }

    public void CreateHand(int PlayerNum)
    {
        for (int i = 0; i < (Hands.Count / 4); i++)
        {
            PlaceCard(Hands[i + PlayerNum * (Hands.Count / 4)], new Vector3(-700, -600, 0), new Vector3(-700 + 350 * i, -600, -1 * i));
        }
    }

    public bool CheckIfValid(string CardType)
    {
        char StartingSuit = 'N';
        if (PlayedCards.Count > 0)
        {
            if (PlayedCards[0][0] == 'J' && PlayedCards[0][1] == AlternateSuits[Trump])
            {
                StartingSuit = Trump;
            }
            else
            {
                StartingSuit = PlayedCards[0][1];
            }
        }
        if (!((PlayedCards.Count == 0) || (StartingSuit == CardType[1] && !(CardType[0] == 'J' && CardType[1] == AlternateSuits[Trump] && StartingSuit == AlternateSuits[Trump]))
            || (CardType[1] == AlternateSuits[Trump] && CardType[0] == 'J' && StartingSuit == Trump)))
        {
            foreach (GameObject Card in DisplayedCards)
            {
                if ((Card.GetComponent<CardComponent>().GetTargetPosition().y == -600) &&
                    (((Card.GetComponent<CardComponent>().GetCardType()[1] == StartingSuit) &&
                    !(Card.GetComponent<CardComponent>().GetCardType()[0] == 'J' &&
                    Card.GetComponent<CardComponent>().GetCardType()[1] == AlternateSuits[Trump] &&
                    StartingSuit == AlternateSuits[Trump])) || (Card.GetComponent<CardComponent>().GetCardType()[0] == 'J' &&
                    Card.GetComponent<CardComponent>().GetCardType()[1] == AlternateSuits[Trump] &&
                    StartingSuit == Trump)))
                {
                    return false;
                }
            }
        }
        PhotonView.Get(this).RPC("PlayCard", RpcTarget.Others, CardType);
        PlayedCards.Add(CardType);
        int Num = 4;
        if (PlayerToSkip != -1)
        {
            Num -= 1;
        }
        if (PlayedCards.Count == Num)
        {
            Ticks = 0;
            Waits.Enqueue(new List<string> { "Score" });
        }
        else
        {
            DoRound();
        }
        if (PlayedCards.Count > 0) //If the player who plays the card begins the new trick, don't make all their cards locked
        {
            foreach (GameObject Card in DisplayedCards)
            {
                Card.GetComponent<CardComponent>().SetUnlocked(new List<bool> { false, false, false });
            }
        }
        return true;
    }

    public List<GameObject> GetDisplayedCards()
    {
        return DisplayedCards;
    }

    // Method to move cards to the winning player's side
    private void MoveCardsToWinner(int winnerIndex)
    {
        Vector3 targetPosition = GetPlayerPosition(winnerIndex);

        foreach (var card in DisplayedCards)
        {
            StartCoroutine(MoveCardToPosition(card, targetPosition));
        }
    }

    // Coroutine to smoothly move a card to a target position
    private IEnumerator MoveCardToPosition(GameObject card, Vector3 targetPosition)
    {
        float duration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startingPosition = card.transform.position;

        while (elapsedTime < duration)
        {
            card.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        card.transform.position = targetPosition;
    }

    // Determine player's position on the screen
    private Vector3 GetPlayerPosition(int playerIndex)
    {
        // Assume you have predefined positions for each player
        switch (playerIndex)
        {
            case 0:
                return new Vector3(-300, 0, 0); // Example position for player 1
            case 1:
                return new Vector3(300, 0, 0);  // Example position for player 2
            case 2:
                return new Vector3(-300, 300, 0); // Example position for player 3
            case 3:
                return new Vector3(300, 300, 0);  // Example position for player 4
            default:
                return Vector3.zero;
        }
    }
}
