using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum Layer
    {
        Monster = 8,
        Ground = 9,
        Block = 10,
    }


    public enum ServiceLifetime
    {
        Singleton,
        Scoped,
        Transient,
    }

    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Game,
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum CameraMode
    {
        QuarterView,
    }
    public enum TouchEvent
    {
        Touch,
        OnDragBegin,
        OnDrag,
        OnDragEnd,
        UnTouch,
    }

    public enum Buttons
    {
        UnDefine,
        Attack,
        Skill1,
        Skill2,
        Skill3,
        Inventory,
        Stat,
    }

    public enum ObjectTypeLocal
    {
        None,
        Creature,
        Projectile,
        Env,
    }

    public enum KeyBoardEvent
    {
        None,
        W,
        A,
        S,
        D
    }

    public enum MouseEvent
    {
        Press,
        Click,
        Drag,
    }

    public enum BuildingType
    {
        House,
        Office,
        Shop,
        Warehouse,
        Hospital,
    }

    public enum Dir : byte
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    [Flags]
    public enum CellLinks : byte
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
    }
}