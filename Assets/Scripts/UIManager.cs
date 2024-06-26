using Mirror;
using Org.BouncyCastle.Utilities.Encoders;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class UIManager : NetworkBehaviour
{
    [SerializeField] private GameObject canvas;
    [HideInInspector] public GameObject LevelUp;
    [HideInInspector] public GameObject Info;

    public Player player;

    public void StartGame()
    {
        Info.SetActive(true);
    }

    public void UpgradeStat(int stat)
    {
        player.UpgradeStat((CharacterStat)stat);

        LevelUp.transform.GetChild(stat).GetChild(0).GetChild(1).
            GetChild(player.UpgradeStats[(CharacterStat)stat] - 1).gameObject.SetActive(true);

        if (player.UpgradeStats[(CharacterStat)stat] >= 8)
        {
            LevelUp.transform.GetChild(stat).GetChild(0).GetChild(0).GetChild(0).
                GetComponent<Button>().interactable = false;
        }
    }

    public void UpdateExp(int curExp, int exp)
    {
        Info.transform.GetChild(1).GetChild(0).GetComponent<Slider>().value = curExp / (float)exp;
    }

    public void UpdateLevel(int level)
    {
        Info.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "Lv. " + level;
    }
}