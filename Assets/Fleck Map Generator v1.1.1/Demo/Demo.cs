using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour {


	public GameObject Ground;


	private GameObject LastMap = null;



	public void Start () {
		UI_GenerateMap();
	}


	public void UI_GenerateMap () {
		if (LastMap) {
			Destroy(LastMap);
		}
		var map = new MoenenGames.FleckMapGenerator.FleckMap() {
			Bump = 0.1f
		};
		map.Generate(64, 64);
		LastMap = map.SpawnToScene(null, Ground);
	}


}
