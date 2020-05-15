using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayer
{
    int HP { get; set; }
    int SP { get; set; }
    float Speed { get; set; }

    Move Direct { get; set; }
}
