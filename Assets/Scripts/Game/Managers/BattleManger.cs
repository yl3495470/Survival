using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XLua;
using System;
using System.IO;
using DG.Tweening;
using Tangzx.ABSystem;

public class BattleManger : Singleton<BattleManger>
{
    private Action _luaUpdate;
    private Action _luaOnDestroy;
    private Action<Move> _luaUpdatePos;

    internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
    internal static float lastGCTime = 0;
    internal const float GCInterval = 1;//1 second 

    private LuaTable scriptEnv;

    AssetBundleManager manager;

    private Move _move = new Move();
    private float _horiAxis = 0;
    private float _vertAxis = 0;

    public void Init()
    {
        ResourceManger.LoadTextAsset("Assets.XLua.MyLua.Resources.GameManger.lua.txt", InitLua);
        Debug.Log("Create Battle");
    }

    void Awake()
    {

    }

    void InitLua(TextAsset luaStr)
    {
        scriptEnv = luaEnv.NewTable();

        // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
        LuaTable meta = luaEnv.NewTable();
        meta.Set("__index", luaEnv.Global);
        scriptEnv.SetMetaTable(meta);
        meta.Dispose();

        scriptEnv.Set("self", this);
        
        luaEnv.DoString(luaStr.text, "ABS", scriptEnv);

        Action luaInit = scriptEnv.Get<Action>("init");
        scriptEnv.Get("update", out _luaUpdate);
        scriptEnv.Get("ondestroy", out _luaOnDestroy);
        scriptEnv.Get("moveFun", out _luaUpdatePos);

        if (luaInit != null)
        {
            luaInit();
        }
    }

    void Update()
    {
        if (_luaUpdate != null)
        {
            _luaUpdate();
            UpdatePos();
        }
        if (Time.time - BattleManger.lastGCTime > GCInterval)
        {
            luaEnv.Tick();
            BattleManger.lastGCTime = Time.time;
        }
    }

    private void UpdatePos()
    {
        _move.HoriAxis = Input.GetAxis("Horizontal");
        _move.VertAxis = Input.GetAxis("Vertical");

        Debug.Log(_move.HoriAxis + "," + _move.VertAxis);
        if(_move.HoriAxis != 0 || _move.VertAxis != 0)
            _move.Angle = Mathf.Rad2Deg * Mathf.Atan2(_move.HoriAxis, _move.VertAxis);

        if (_luaUpdatePos != null)
        {
            _luaUpdatePos(_move);
        }
    }

    void OnDestroy()
    {
        if (_luaOnDestroy != null)
        {
            _luaOnDestroy();
        }
        _luaOnDestroy = null;
        _luaUpdate = null;
        scriptEnv.Dispose();
    }
}
