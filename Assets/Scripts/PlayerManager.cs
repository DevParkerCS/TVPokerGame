using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public Player Player { get; set; } = null;
    public bool IsTurn { get; set; } = false;
    private TextMeshProUGUI betText;

    #region Serialized Fields
    [SerializeField] private TextMeshProUGUI tmpName;
    [SerializeField] private TextMeshProUGUI tmpBalance;
    [SerializeField] private GameObject button;
    [SerializeField] private Image iconImg;
    [SerializeField] private GameObject outlineIndicator;
    [SerializeField] private Image firstCard;
    [SerializeField] private Image secondCard;
    [SerializeField] private GameObject betIndicator;
    [SerializeField] private AvatarLibrary avatarLib;
    [SerializeField] private GameObject inactiveIndicator;
    #endregion

    private void Awake()
    {
        button.SetActive(false);
        outlineIndicator.SetActive(false);
        betText = betIndicator.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        betIndicator.SetActive(false);
    }

    private void Start()
    {
        HideCards();
    }

    #region Public Methods
    public void InitializePlayer()
    {
        iconImg.sprite = avatarLib.GetSprite(Player.IconCode);
        tmpName.text = Player.PlayerName;
        tmpBalance.text = Player.ChipBalance.ToString();
        inactiveIndicator.SetActive(false);
    }

    public void ToggleButton()
    {
        Debug.Log($"Button Active: {button.activeSelf}");
        button.SetActive(!button.activeSelf);
    }

    public void UpdatePlayerBalance()
    {
        tmpBalance.text = Player.ChipBalance.ToString();
    }

    public void HideCards()
    {
        Color FirstColor = firstCard.color;
        FirstColor.a = 0;
        firstCard.color = FirstColor;
        Color SecondColor = secondCard.color;
        SecondColor.a = 0;
        secondCard.color = SecondColor;
    }

    public void ShowCard(int cardNum)
    {
        if(cardNum == 0)
        {
            Color FirstColor = firstCard.color;
            FirstColor.a = 1;
            firstCard.color = FirstColor;
        }
        else
        {
            Color SecondColor = secondCard.color;
            SecondColor.a = 1;
            secondCard.color = SecondColor;
        }
    }

    public void ToggleTurn(bool isActive)
    {
        if(isActive)
        {
            IsTurn = true;
            outlineIndicator.SetActive(true);
        }else
        {
            IsTurn = false;
            outlineIndicator.SetActive(false);
        }
    }

    public void DisplayBet(int amount, BetManager.BetType type)
    {
        betIndicator.SetActive(true);
        betText.text = "$" + amount.ToString();
        UpdatePlayerBalance();

        switch (type)
        {
            case BetManager.BetType.Blind:
                betIndicator.GetComponent<Image>().color = Color.lightBlue;
                break;
            case BetManager.BetType.Bet:
                betIndicator.GetComponent<Image>().color = Color.yellow;
                break;
            case BetManager.BetType.Call:
                betIndicator.GetComponent<Image>().color = Color.lightGreen;
                break;
            case BetManager.BetType.Raise:
                betIndicator.GetComponent<Image>().color = Color.orange;
                break;
            case BetManager.BetType.AllIn:
                betIndicator.GetComponent<Image>().color = Color.red;
                break;
        }
    }

    public void DisplayCheck()
    {
        betIndicator.SetActive(true);
        betText.text = "CHECK";
        betIndicator.GetComponent<Image>().color = Color.beige;
    }

    public void DisplayFold()
    {
        HideCards();
        inactiveIndicator.SetActive(true);
        betText.text = "FOLD";
        betIndicator.GetComponent<Image>().color = Color.grey;
        betIndicator.SetActive(true);
    }

    public void Raise(int amount)
    {

    }

    public void ResetForStreet()
    {
        betIndicator.SetActive(false);
        outlineIndicator.SetActive(false);
    }

    public void ResetAllVisual()
    {
        betIndicator.SetActive(false);
        outlineIndicator.SetActive(false);
        inactiveIndicator.SetActive(false);
    }
    #endregion

    #region Private Methods

    #endregion
}
