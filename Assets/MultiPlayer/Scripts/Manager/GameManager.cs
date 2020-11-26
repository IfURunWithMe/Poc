﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PlayerNetObj PlayerPrefab;

    public bool BeginGame = false;

    private HintData m_HintData;
    private ZMarkerHelper m_MarkerHelper;
    private PlayerMe m_PlayerMe;


    public delegate void S2CFuncAction<T>(T info);
    public Dictionary<string, S2CFuncAction<string>> S2CFuncTable = new Dictionary<string, S2CFuncAction<string>>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        S2CFuncTable.Add(S2CFuncName.Fire, S2C_Fire);
    }

    public void Initialized()
    {
        m_HintData = new HintData();
        m_MarkerHelper = ZMarkerHelper.Instance;
        m_PlayerMe = new PlayerMe();
    }


    #region S2CFunc

    public void S2C_Fire(string param)
    {
        var arr = param.Split(',');
        string pid = arr[0];
        int type = int.Parse(arr[1]);

        Fire(pid, type);
    }
    private void Fire(string pid, int type)
    {
        var obj = m_PlayerMe.GetPlayerNetObj(pid);
        obj.Shoot();
    }

    #endregion


    #region Data opera

    public void AddPlayerData(string playerid , PlayerNetObj obj)
    {
        m_PlayerMe.AddPlayer(playerid, obj);
    }
    public void RemovePlayerData(string playerid)
    {
        m_PlayerMe.RemovePlayer(playerid);
    }
    public void ClearPlayerData()
    {
        m_PlayerMe.ClearPlayerData();
    }

    #endregion

    public void CreateRoom()
    {
        if(ZGlobal.ClientMode == ZClientMode.Master)
        {
            MessageManager.Instance.SendCreateRoomMsg();
        }
    }

    // 扫秒成功 开始等待房间创建
    public void OnScanSuccess()
    {
        if(ZGlobal.ClientMode == ZClientMode.Visiter)
        {
            

        }
    }

    #region UI 
   
    public void ShowHint(HintType t, bool show = true)
    {
        UIManager.Instance.SetHintLabel(m_HintData.GetData(t), show);
    }

    public void OpenScan()
    {
        ShowHint(HintType.ScanMarker);
        m_MarkerHelper.ResetScanStatus();
        m_MarkerHelper.OpenScan = true;
        m_MarkerHelper.ScanSuccessEvent = OnScanSuccess;
    }

    #endregion


}
