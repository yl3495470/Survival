namespace MoenenGames.FleckMapGenerator {

	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;


	[System.Serializable]
	public class FleckMap {



		#region --- SUB ---


		private struct Fleck {


			public int A, B;
			public bool IsEdge;

			public Fleck (int a, int b) {
				A = a;
				B = b;
				IsEdge = false;
			}


			public static Fleck Lerp (Fleck a, Fleck b, float t) {
				return new Fleck(
					(int)Mathf.Lerp(a.A, b.A, t),
					(int)Mathf.Lerp(a.B, b.B, t)
				);
			}


			public static float Distance (Fleck a, Fleck b) {
				return Vector2.Distance(new Vector2(a.A, a.B), new Vector2(b.A, b.B));
			}


		}



		#endregion




		#region --- VAR ---


		// Const
		const int DEFAULT_WIDTH = 128;
		const int DEFAULT_HEIGHT = 64;
		const int MAX_BRIDGE_COUNT = 400;
		const float BUMP_MUTI = 0.3f;
		public const int MIN_MOUNTAIN_HEIGHT = 1;
		public const int MAX_MOUNTAIN_HEIGHT = 9;


		// Shot Cut
		public int this[int x, int y] {
			get {
				return data[y * Width + x];
			}
			set {
				data[y * Width + x] = value;
			}
		}

		public int Width {
			get {
				return width;
			}
		}

		public int Height {
			get {
				return height;
			}
		}

		public int Iteration {
			get {
				return iteration;
			}
			set {
				iteration = Mathf.Max(value, 0);
			}
		}

		public float GenerationComplexity {
			get {
				return threshold * Width * Height * Iteration;
			}
		}

		public List<Vector2> BirthPoints {
			get {
				return birthPoints;
			}
		}


		// Serialize
		public string name = "New Fleck Map";
		public float tileSize = 1f;
		public int iteration = 3;
		public float threshold = 2;
		public int edge = 3;
		public int bridgeWidth = 1;
		public int maxBirthPointNum = 12;
		public int maxAltitude = 3;
		public float Bump = 0.5f;
		[SerializeField]
		private int[] data = new int[DEFAULT_WIDTH * DEFAULT_HEIGHT];
		[SerializeField]
		private int width = DEFAULT_WIDTH;
		[SerializeField]
		private int height = DEFAULT_HEIGHT;
		[SerializeField]
		private List<Vector2> birthPoints = new List<Vector2>();


		#endregion




		#region --- API ---




		/// <summary>
		/// Create a random seed for fleck map system.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public static float[] GetRandomSeeds (int width, int height) {
			float[] seeds = new float[width * height];
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					seeds[j * width + i] = Random.value;
				}
			}
			return seeds;
		}

		/// <summary>
		/// Create a random seed without suddenly change for fleck map altitude.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public static float[] GetSmoothRandomSeeds (int width, int height, float bump) {
			float[] seeds = new float[width * height];
			float randomOffsetX = Random.value * width;
			float randomOffsetY = Random.value * height;
			for (int i = 0; i < width; i++) {
				for (int j = 0; j < height; j++) {
					seeds[j * width + i] = Mathf.PerlinNoise(
						i * bump * BUMP_MUTI + randomOffsetX,
						j * bump * BUMP_MUTI + randomOffsetY
					);
				}
			}
			return seeds;
		}

		/// <summary>
		/// Generate Data with current setting and random seed.
		/// </summary>
		/// <param name="progressCallback">Will call this action for every iteration over.</param>
		/// <returns>Success or not</returns>
		public bool Generate (System.Action<float> progressCallback = null) {
			return Generate(Width, Height, GetRandomSeeds(Width, Height), progressCallback);
		}



		/// <summary>
		/// Generate Data with given width and height and random seed.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="progressCallback">Will call this action for every iteration over.</param>
		/// <returns>Success or not</returns>
		public bool Generate (int width, int height, System.Action<float> progressCallback = null) {
			return Generate(width, height, GetRandomSeeds(Width, Height), progressCallback);
		}



		/// <summary>
		/// Generate Data with given width and height and seed.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="seeds"></param>
		/// <param name="progressCallback">Will call this action for every iteration over.</param>
		/// <returns>Success or not</returns>
		public bool Generate (int width, int height, float[] seeds, System.Action<float> progressCallback = null) {

			if (seeds.Length < width * height) {
				return false;
			}
			this.width = width;
			this.height = height;
			data = new int[Width * Height];
			// Init by seed
			int[] tempData = new int[Width * Height];
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					int seedData = (int)(seeds[j * Width + i] * (2f - 0.0001f));
					tempData[j * Width + i] = seedData;
				}
			}

			// 擦除超出边界和限制的
			if (edge > 0) {
				for (int i = 0; i < Width; i++) {
					for (int j = 0; j < Height; j++) {
						if (i < edge || i > width - 1 - edge || j < edge || j > Height - 1 - edge) {
							tempData[j * Width + i] = 0;
						}
					}
				}
			}

			// Copy to Data
			tempData.CopyTo(data, 0);

			// 迭代，将缝隙填充，迭代次数越多越圆滑，面积也越大
			float iterRadius = threshold * 0.25f + 1f;
			int groundCount = 0;
			for (int iter = 0; iter < Iteration; iter++) {
				// SetData
				if (iter != Iteration - 1) {
					// Loop
					for (int i = 0; i < Width; i++) {
						for (int j = 0; j < Height; j++) {
							tempData[j * Width + i] = IterateFleck(i, j, iterRadius);
						}
					}
				} else {
					// Last Loop
					for (int i = 0; i < Width; i++) {
						for (int j = 0; j < Height; j++) {
							int fleck = IterateFleck(i, j, iterRadius);
							if (fleck == 1) {
								groundCount++;
							}
							tempData[j * Width + i] = fleck;
						}
					}
				}
				tempData.CopyTo(data, 0);
				// Callback
				if (progressCallback != null) {
					progressCallback((iter + 1f) / (Iteration + 1f));
				}
			}

			// 搭桥与建立出生点
			BridgeAndBirthPoint(groundCount);

			// 随机高度图
			if (maxAltitude > 0) {
				float[] altSeed = GetSmoothRandomSeeds(Width, Height, Bump);
				for (int i = 0; i < Width; i++) {
					for (int j = 0; j < Height; j++) {
						int alt = 0;
						if (data[j * Width + i] != 0) {
							alt = (int)(altSeed[j * Width + i] * (maxAltitude - 1f + 0.999f)) + 1;
						}
						tempData[j * Width + i] = alt;
					}
				}
			}
			tempData.CopyTo(data, 0);

			return true;
		}



		/// <summary>
		/// Overwrite current map Data with given json.
		/// </summary>
		/// <param name="json"></param>
		public void LoadFromJson (string json) {
			JsonUtility.FromJsonOverwrite(json, this);
		}



		/// <summary>
		/// Get json string from current map Data.
		/// </summary>
		/// <returns></returns>
		public string GetJson () {
			return JsonUtility.ToJson(this);
		}



		/// <summary>
		/// Get string from current map Data.
		/// </summary>
		/// <returns></returns>
		public override string ToString () {
			var sBuilder = new System.Text.StringBuilder();
			for (int j = 0; j < Height; j++) {
				for (int i = 0; i < Width; i++) {
					sBuilder.Append(data[j * Width + i] == 0 ? ".": data[j * Width + i].ToString());
				}
				if (j != Height - 1) {
					sBuilder.Append('\r');
					sBuilder.Append('\n');
				}
			}
			return sBuilder.ToString();
		}



		/// <summary>
		/// Create a new gameObject by given prefabs.
		/// </summary>
		/// <param name="prefabWall"></param>
		/// <param name="prefabGround"></param>
		/// <returns>Root of those prefabs</returns>
		public GameObject SpawnToScene (
			GameObject prefabWall,
			GameObject prefabGround,
			GameObject prefabBirthPoint = null
		) {
			GameObject root = new GameObject();
			root.transform.position = Vector3.zero;
			root.transform.rotation = Quaternion.identity;
			root.transform.localScale = Vector3.one;
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					int alt = this[i, j];
					for (int k = 0; k < alt; k++) {
						GameObject source = data[j * Width + i] == 0 ? prefabWall : prefabGround;
						GameObject g = source ? Object.Instantiate(source) : new GameObject();
						g.name = (data[j * Width + i] == 0 ? "Wall" : "Ground ") + i + "_" + j + "_" + k;
						g.transform.SetParent(root.transform);
						g.transform.localPosition = new Vector3(
							i * tileSize,
							k * tileSize,
							j * tileSize
						);
						g.transform.localRotation = Quaternion.identity;
						g.transform.localScale = Vector3.one;
					}
				}
			}
			if (prefabBirthPoint) {
				for (int i = 0; i < BirthPoints.Count; i++) {
					GameObject g = Object.Instantiate(prefabBirthPoint);
					g.name = "BirthPoint " + i;
					g.transform.SetParent(root.transform);
					g.transform.localPosition = new Vector3(
						BirthPoints[i].x * tileSize,
						this[(int)BirthPoints[i].x, (int)BirthPoints[i].y] * tileSize,
						BirthPoints[i].y * tileSize
					);
					g.transform.localRotation = Quaternion.identity;
					g.transform.localScale = Vector3.one;
				}
			}
			return root;
		}





		#endregion




		#region --- LGC ---



		private int IterateFleck (int x, int y, float radius) {
			int radiusUp = Mathf.CeilToInt(radius);
			int u = Mathf.Min(y + radiusUp, Height - 1);
			int d = Mathf.Max(y - radiusUp, 0);
			int l = Mathf.Max(x - radiusUp, 0);
			int r = Mathf.Min(x + radiusUp, Width - 1);
			int balance = 0;
			for (int i = l; i <= r; i++) {
				for (int j = d; j <= u; j++) {
					balance += data[j * Width + i] == 1 ? 1 : -1;
				}
			}
			return balance > 0 ? 1 : 0;
		}



		private void BridgeAndBirthPoint (int groundCount) {

			int mainCount = 0;
			int currentIndex = 2;
			int[] tempData = new int[Width * Height];
			data.CopyTo(tempData, 0);
			List<List<Fleck>> zoneList = new List<List<Fleck>>();
			List<Vector2> pivotList = new List<Vector2>();

			while (mainCount < groundCount) {
				// Start a new zone
				for (int i = 0; i < Width; i++) {
					for (int j = 0; j < Height; j++) {
						if (tempData[j * Width + i] == 1) {
							List<Fleck> zone;
							Vector2 pivot;
							ZoneGrowRecursion(
								currentIndex, i, j,
								ref tempData, ref mainCount,
								out zone, out pivot
							);
							zoneList.Add(zone);
							pivotList.Add(pivot);
							currentIndex++;

							// Jump
							i = Width;
							break;
						}
					}
				}
			}

			// Remove Small Zone
			int minBridgeNum = (bridgeWidth + 1) * 4;
			for (int i = 0; i < zoneList.Count; i++) {
				if (zoneList[i].Count < minBridgeNum) {
					zoneList.RemoveAt(i);
					pivotList.RemoveAt(i);
					i--;
				}
			}

			// Get Birth Points
			birthPoints.Clear();
			if (maxBirthPointNum > 0) {
				int birthCheckAdd = zoneList.Count <= maxBirthPointNum ? 1 : zoneList.Count / maxBirthPointNum;
				for (int i = 0; i < zoneList.Count; i += birthCheckAdd) {
					if (zoneList[i].Count <= 0) {
						continue;
					}
					int x = (int)pivotList[i].x;
					int y = (int)pivotList[i].y;
					int zoneIndex = tempData[zoneList[i][0].B * Width + zoneList[i][0].A];
					Fleck target = ExpandToGet(tempData, x, y, zoneIndex, zoneList[i].Count);
					if (target.A > 0) {
						birthPoints.Add(new Vector2(target.A, target.B));
						if (birthPoints.Count >= maxBirthPointNum) {
							break;
						}
					}
				}
			}

			// Return if no need to bridge
			if (bridgeWidth <= 0 || Iteration <= 0) {
				return;
			}

			// Bridge Zones
			int len = zoneList.Count;
			if (len <= 1) {
				return;
			}
			List<Fleck> BridgedZone = new List<Fleck>(zoneList[0]);
			zoneList.RemoveAt(0);
			for (int count = 0; count < len - 1 && count < MAX_BRIDGE_COUNT; count++) {

				int indexB = -1;
				float finalMinDistance = float.MaxValue;
				Fleck pointA = new Fleck();
				Fleck pointB = new Fleck();
				for (int b = 0; b < zoneList.Count; b++) {
					// Get The Two Points
					List<Fleck> zoneB = zoneList[b];
					int zoneLenA = BridgedZone.Count;
					int zoneLenB = zoneB.Count;
					if (zoneLenA == 0 || zoneLenB == 0) {
						continue;
					}
					Fleck tempPointA = BridgedZone[0];
					Fleck tempPointB = zoneB[0];
					float minDis = Fleck.Distance(tempPointA, tempPointB);
					int addA = zoneLenA / 100 + 1;
					int addB = zoneLenB / 100 + 1;
					for (int i = 0; i < zoneLenA; i += addA) {
						for (int j = 0; j < zoneLenB; j += addB) {
							float dis = Fleck.Distance(BridgedZone[i], zoneB[j]);
							if (dis < minDis) {
								minDis = dis;
								tempPointA = BridgedZone[i];
								tempPointB = zoneB[j];
							}
						}
					}
					// Final
					if (minDis < finalMinDistance) {
						finalMinDistance = minDis;
						indexB = b;
						pointA = tempPointA;
						pointB = tempPointB;
					}
				}

				// Check
				if (indexB < 0) {
					break;
				}

				// Bridge Them
				float add = 0.5f / Mathf.Max(
					Mathf.Abs(pointA.A - pointB.A),
					Mathf.Abs(pointA.B - pointB.B)
				);
				Fleck prevF = new Fleck();
				for (float t = 0f; t < 1f + add * 2f; t += add) {

					Fleck f = Fleck.Lerp(pointA, pointB, t);

					// Concat Prev
					if (t != 0f) {
						if (f.A != prevF.A && f.B != prevF.B) {
							data[f.B * Width + prevF.A] = 1;
						}
					} else {
						data[Mathf.Max(f.B - 1, 0) * Width + f.A] = 1;
						data[Mathf.Min(f.B + 1, Height - 1) * Width + f.A] = 1;
						data[f.B * Width + Mathf.Max(f.A - 1, 0)] = 1;
						data[f.B * Width + Mathf.Min(f.A + 1, Width - 1)] = 1;
					}
					prevF = f;

					// Width
					int bWidth = bridgeWidth - 1;
					int d = Mathf.Max(f.B - bWidth, 0);
					int u = Mathf.Min(f.B + bWidth, Height - 1);
					int l = Mathf.Max(f.A - bWidth, 0);
					int r = Mathf.Min(f.A + bWidth, Width - 1);

					// Draw them
					for (int x = l; x <= r; x++) {
						for (int y = d; y <= u; y++) {
							data[y * Width + x] = 1;
						}
					}

				}

				// Merge Them
				BridgedZone.AddRange(zoneList[indexB]);
				zoneList.RemoveAt(indexB);

			}


		}



		private void ZoneGrowRecursion (
			int currentIndex, int _x, int _y,
			ref int[] tempData, ref int mainCount,
			out List<Fleck> zone, out Vector2 pivot
		) {

			int pivotFleckCount = 0;
			pivot = new Vector2();
			zone = new List<Fleck>();
			Queue<Fleck> queue = new Queue<Fleck>();
			Fleck first = new Fleck(_x, _y);
			queue.Enqueue(first);

			while (queue.Count > 0) {

				Fleck f = queue.Dequeue();
				if (tempData[f.B * Width + f.A] != 1) {
					continue;
				}
				int x = f.A;
				int y = f.B;
				tempData[y * Width + x] = currentIndex;
				mainCount++;
				pivot += new Vector2(x, y);
				pivotFleckCount++;
				bool isEdge = CheckNeighbors(tempData, x, y, 0);
				f.IsEdge = isEdge;
				if (isEdge) {
					zone.Add(f);
				}

				// Check Neighbors
				bool u, d, l, r;
				CheckNeighbors(tempData, x, y, 1, out d, out u, out l, out r);
				if (d) {
					queue.Enqueue(new Fleck(x, y - 1));
				}
				if (u) {
					queue.Enqueue(new Fleck(x, y + 1));
				}
				if (l) {
					queue.Enqueue(new Fleck(x - 1, y));
				}
				if (r) {
					queue.Enqueue(new Fleck(x + 1, y));
				}

			}

			pivot /= pivotFleckCount;

		}



		private void CheckNeighbors (
			int[] tempData, int x, int y, int targetIndex,
			out bool d, out bool u, out bool l, out bool r
		) {
			d = y > 0 && tempData[(y - 1) * Width + x] == targetIndex;
			u = y < Height - 1 && tempData[(y + 1) * Width + x] == targetIndex;
			l = x > 0 && tempData[y * Width + x - 1] == targetIndex;
			r = x < Width - 1 && tempData[y * Width + x + 1] == targetIndex;
		}



		private bool CheckNeighbors (
			int[] tempData, int x, int y, int targetIndex
		) {
			return
				(y > 0 && tempData[(y - 1) * Width + x] == targetIndex) ||
				(y < Height - 1 && tempData[(y + 1) * Width + x] == targetIndex) ||
				(x > 0 && tempData[y * Width + x - 1] == targetIndex) ||
				(x < Width - 1 && tempData[y * Width + x + 1] == targetIndex);
		}


		private bool CheckNeighbors8 (
			int[] tempData, int x, int y, int targetIndex
		) {
			return
				(y > 0 && tempData[(y - 1) * Width + x] == targetIndex) ||
				(y < Height - 1 && tempData[(y + 1) * Width + x] == targetIndex) ||
				(x > 0 && tempData[y * Width + x - 1] == targetIndex) ||
				(x < Width - 1 && tempData[y * Width + x + 1] == targetIndex) ||

				(y > 0 && x > 0 && tempData[(y - 1) * Width + x - 1] == targetIndex) ||
				(y < Height - 1 && x < width - 1 && tempData[(y + 1) * Width + x + 1] == targetIndex) ||
				(y < Height - 1 && x > 0 && tempData[(y + 1) * Width + x - 1] == targetIndex) ||
				(y > 0 && x < Width - 1 && tempData[(y - 1) * Width + x + 1] == targetIndex);
		}


		private Fleck ExpandToGet (
			int[] tempData, int _x, int _y, int targetIndex, int maxCount
		) {

			int checkedNum = 0;
			Dictionary<Fleck, byte> CheckedFleck = new Dictionary<Fleck, byte>();
			Queue<Fleck> queue = new Queue<Fleck>();
			queue.Enqueue(new Fleck(_x, _y));

			while (queue.Count > 0) {

				Fleck f = queue.Dequeue();
				int x = f.A;
				int y = f.B;

				if (CheckedFleck.ContainsKey(f)) {
					continue;
				} else {
					CheckedFleck.Add(f, 0);
					checkedNum++;
					if (checkedNum >= maxCount) {
						break;
					}
				}

				if (tempData[y * Width + x] == targetIndex) {
					if (!CheckNeighbors8(tempData, x, y, 0)) {
						return f;
					}
				}

				if (y > 0) {
					queue.Enqueue(new Fleck(x, y - 1));
				}
				if (x > 0) {
					queue.Enqueue(new Fleck(x - 1, y));
				}
				if (y < Height - 1) {
					queue.Enqueue(new Fleck(x, y + 1));
				}
				if (x < Width - 1) {
					queue.Enqueue(new Fleck(x + 1, y));
				}
			}

			return new Fleck(-1, -1);
		}



		#endregion




	}



}
