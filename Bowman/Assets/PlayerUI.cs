using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField]
    GameObject pauseMenu, winnerText;
    [SerializeField]
    RectTransform HealthAmount, rechargeAmount;
    [SerializeField]
    GameObject scoreboard, DamageNumbers, Slider, SensText;
    [SerializeField]
    Image crosshair, rechargeColor;
    float rechargeScale = 1f, startTimerForPowerUp, rechargeTime;
    public UnityStandardAssets.Characters.FirstPerson.MouseLook mouseLook;
    private bool rechargeAvailable, showDamageOnce;

    private Player player;
    private SpawnArrow spawnArrow;
    Color holdRechargeColor;

    public void SetPlayer(Player _player)
    {
        player = _player;
        spawnArrow = player.GetComponent<SpawnArrow>();
    }

    void Start()
    {
        Pause.IsOn = false;
        rechargeAvailable = true;
        rechargeTime = Time.time;
        holdRechargeColor = rechargeColor.color;
        Cursor.lockState = CursorLockMode.Locked;
        Slider.GetComponent<Slider>().value = 10;
        SensText.GetComponent<Text>().text = "10";
    }

    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
        if (player.hit)
        {
            crosshair.color = Color.red;
            if (!showDamageOnce)
            {
                showDamageOnce = true;
                ShowDamage();
            }
        }
        else if (player.headshot)
        {
            crosshair.color = Color.yellow;
            if (!showDamageOnce)
            {
                showDamageOnce = true;
                ShowDamage();
            }
        }
        else
        {
            showDamageOnce = false;
            crosshair.color = Color.white;
        }
        if (player.hasWon)
            SetWinner();
        if (spawnArrow.usedPowerUp)
        {
            SetTime();
            spawnArrow.usedPowerUp = false;
            rechargeAvailable = true;
        }
        if (rechargeAvailable && Time.time - rechargeTime < 5)
        {
            float y = (Time.time - rechargeTime) / 5;
            if (y > 1)
            {
                rechargeAvailable = false;
                rechargeAmount.localScale = new Vector3(1f, 1f, 1f);
            }
            rechargeAmount.localScale = new Vector3(1f, y, 1f);
        }
        if (spawnArrow.powerUpActive)
        {
            rechargeColor.color = new Color32(47, 224, 255, 214);
        }
        else
            rechargeColor.color = holdRechargeColor;

        SetHealthAmount(player.GetHealthAmount());
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            scoreboard.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreboard.SetActive(false);
        }
    }
    private void ShowDamage()
    {
        GameObject damageText = Instantiate(DamageNumbers, transform);
        damageText.transform.parent = gameObject.transform;
        damageText.GetComponent<Text>().text = player.SendDamageAmountToUI.ToString();
        damageText.GetComponent<Rigidbody2D>().AddForce(new Vector3(Random.Range(-60, 60), 200, 0), ForceMode2D.Impulse);
        Destroy(damageText, 1f);
    }
    void TogglePauseMenu()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        Pause.IsOn = pauseMenu.activeSelf;
    }
    void SetTime()
    {
        rechargeTime = Time.time;
    }
    void SetHealthAmount(float _amount)
    {
        if (_amount < 0)
            _amount = 0;
        _amount = _amount / 100f;
        HealthAmount.localScale = new Vector3(_amount, 1f, 1f);
    }
    void SetWinner()
    {
        player.hasWon = false;
        winnerText.SetActive(true);
        Text showText = winnerText.GetComponent<Text>();
        showText.text = player.GetWinner() + " IS THE WINNER!";
        StartCoroutine(SetWinnerToFalse());
    }
    public void SetPlayerSensitivity()
    {
        float val = Slider.GetComponent<Slider>().value;
        player.SetSensitivity(val);
        SensText.GetComponent<Text>().text = val.ToString();
    }
    IEnumerator SetWinnerToFalse()
    {
        yield return new WaitForSeconds(3);
        winnerText.SetActive(false);
    }
}
