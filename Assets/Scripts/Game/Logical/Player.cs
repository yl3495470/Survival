using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour, IPlayer
{
    int hp;
    int sp;
    float speed;
    Move direct;

    public Transform selfTran;
    public Transform bodyTran;
    public Transform cameraTran;


    public int HP { get { return hp; } set { hp = value; } }
    public int SP { get { return sp; } set { sp = value; } }
    public float Speed { get { return speed; } set { speed = value; } }
    public Move Direct { get { return direct; } set { direct = value; } }

}
