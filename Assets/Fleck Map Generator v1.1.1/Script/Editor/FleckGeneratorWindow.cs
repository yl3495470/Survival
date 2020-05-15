namespace MoenenGames.FleckMapGenerator {
#if UNITY_EDITOR
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	public class FleckGeneratorWindow : EditorWindow {



		#region --- SUB ---

		enum TaskType {
			None = 0,
			ExportPrefab = 1,
			ExportJson = 2,
			ExportText = 3,

		}


		enum TaskResult {
			None = 0,
			Success = 1,
			GenerationError = 2,
			FileError = 3,

		}



		#endregion



		#region --- VAR ---


		// Const
		// Title
		const string MAIN_TITLE = "Fleck Map Generator";
		const string MAIN_TITLE_SHORT = "Fleck Generator";
		const string MAIN_TITLE_UNRICH = "Fleck Map Generator";
		const string MAIN_TITLE_RICH = "<color=#ff3333>F</color><color=#ffcc00>l</color><color=#ffff33>e</color><color=#33ff33>c</color><color=#33ffff>k</color> Map Generator";
		//Iteration
		const int MIN_ITERATION = 1;
		const int MAX_ITERATION = 6;
		// Width
		const int MIN_WIDTH = 16;
		const int MAX_WIDTH = 256;
		// Height
		const int MIN_HEIGHT = 16;
		const int MAX_HEIGHT = 256;
		// Threshold
		const float MIN_THRESHOLD = 0f;
		const float MAX_THRESHOLD = 20f;
		// Bridge
		const int MIN_BRIDGE = 0;
		const int MAX_BRIDGE = 10;
		// Birth Point
		const int MAX_BIRTH_POINT_NUM = 12;


		// Shot Cut
		Texture2D PreviewTexture {
			get {
				if (!m_PreviewTexture) {
					FreshPreview();
				}
				return m_PreviewTexture;
			}
			set {
				m_PreviewTexture = value;
			}
		}

		FleckMap CurrentMapData {
			get {
				if (m_CurrentMapData == null) {
					m_CurrentMapData = new FleckMap();
				}
				return m_CurrentMapData;
			}
			set {
				m_CurrentMapData = value;
			}
		}


		// Saving Data
		bool ModifyPanelOpen = true;
		bool PoolPanelOpen = true;
		bool ExportPanelOpen = true;
		bool SettingPanelOpen = false;
		bool LogMessage = true;
		bool ShowDialog = true;
		bool ColorfulTitle = true;
		bool HighLightAfterExport = true;
		int ZoomLevel = 0;
		string ExportPath = "Assets";
		Color ItemColorA = new Color(0.045f, 0.045f, 0.045f);
		Color ItemColorB = new Color(1, 1, 1);
		Color ItemColorC = new Color(1, 0, 0);


		// Data
		FleckMap m_CurrentMapData = null;
		Vector2 MasterScrollPosition = Vector2.zero;
		Texture2D m_PreviewTexture = null;
		GameObject PrefabA = null;
		GameObject PrefabB = null;
		GameObject PrefabC = null;
		float[] Seeds = null;


		#endregion



		#region --- GUI ---




		[MenuItem("Tools/" + MAIN_TITLE)]
		public static void OpenWindow () {
			var inspector = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			FleckGeneratorWindow window = inspector != null ?
				GetWindow<FleckGeneratorWindow>(MAIN_TITLE_SHORT, true, inspector) :
				GetWindow<FleckGeneratorWindow>(MAIN_TITLE_SHORT, true);
			window.minSize = new Vector2(275, 400);
			window.maxSize = new Vector2(600, 1000);
		}



		void OnEnable () {
			EditorLoad();
			FreshPreview();
			Repaint();
		}



		void OnFocus () {
			FreshPreview();
			Repaint();
		}



		void OnGUI () {

			MasterScrollPosition = GUILayout.BeginScrollView(MasterScrollPosition, GUI.skin.scrollView);

			TitleGUI();

			PreviewGUI();

			ModifyGUI();

			ExportGUI();

			SettingGUI();


			// Clear Focus
			if (Event.current.type == EventType.MouseDown) {
				GUI.FocusControl("");
				Repaint();
			}

			// System
			if (GUI.changed) {
				EditorSave();
			}

			GUILayout.EndScrollView();

		}




		void TitleGUI () {
			Space(6);
			LayoutV(() => {
				GUIStyle style = new GUIStyle() {
					alignment = TextAnchor.LowerCenter,
					fontSize = 13,
					fontStyle = FontStyle.Bold
				};
				style.normal.textColor = Color.white;
				style.richText = true;
				Rect rect = GUIRect(0, 18);

				GUIStyle shadowStyle = new GUIStyle(style) {
					richText = false
				};

				EditorGUI.DropShadowLabel(rect, MAIN_TITLE_UNRICH, shadowStyle);
				GUI.Label(rect, ColorfulTitle ? MAIN_TITLE_RICH : MAIN_TITLE_UNRICH, style);

			});
			Space(6);
		}



		void PreviewGUI () {

			Space(2);

			// Zoom
			LayoutH(() => {
				Space(12);
				EditorGUI.LabelField(GUIRect(40, 18), "Zoom");
				Space(2);
				int newZoom = EditorGUI.IntSlider(GUIRect(0, 18), ZoomLevel, -2, 2);
				if (newZoom != ZoomLevel) {
					ZoomLevel = newZoom;
					Repaint();
				}
				Space(12);
			});
			Space(2);

			// Cheat Offset
			int cheatOffset = (int)(6 + ((CurrentMapData.Width % 2) - (EditorGUIUtility.currentViewWidth % 2)));

			// Texture
			float zoomMuti =
				ZoomLevel <= -2 ? 0.5f :
				ZoomLevel <= -1 ? 1f :
				ZoomLevel <= 0 ? 2f :
				ZoomLevel <= 1 ? 3f : 4f;
			float width = Mathf.Min(CurrentMapData.Width * zoomMuti, EditorGUIUtility.currentViewWidth - cheatOffset);
			float height = CurrentMapData.Height * zoomMuti;
			Rect rect = GUIRect(width, height);
			rect.x = (EditorGUIUtility.currentViewWidth - cheatOffset - width) * 0.5f + cheatOffset * 0.5f;
			if (GUI.Button(rect, "", GUIStyle.none)) {
				GenerateSeedAndMap();
				FreshPreview();
				Repaint();
			}
			GUI.DrawTexture(rect, PreviewTexture, ScaleMode.ScaleAndCrop);
			Space(4);
		}



		void ModifyGUI () {
			LayoutF(() => {

				// Main Buttons
				const int ITEM_HEIGHT = 24;
				LayoutH(() => {

					if (GUI.Button(GUIRect(0, ITEM_HEIGHT), "New")) {
						// New Map
						NewMapTask();
					}

					Space(4);

					if (GUI.Button(GUIRect(0, ITEM_HEIGHT), "Open")) {
						// Open
						LoadMapFromJsonTask();
					}

					Space(4);

					if (GUI.Button(GUIRect(0, 24), "Refresh")) {
						// Refresh
						GenerateSeedAndMap();
						FreshPreview();
						Repaint();
					}

				});

				Space(12);


				// Basic
				LayoutH(() => {
					// Name
					EditorGUI.LabelField(GUIRect(0, 18), "Name");
					CurrentMapData.name = EditorGUI.TextField(GUIRect(0, 18), CurrentMapData.name);
					Space(6);
					// Tile Size
					EditorGUI.LabelField(GUIRect(0, 18), "Tile Size");
					CurrentMapData.tileSize = Mathf.Abs(EditorGUI.DelayedFloatField(GUIRect(0, 18), CurrentMapData.tileSize));
				});
				Space(4);


				// Width Height
				LayoutH(() => {

					// Width
					EditorGUI.LabelField(GUIRect(0, 18), "Width");
					int newWidth = EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.Width);
					newWidth = Mathf.Clamp(newWidth, MIN_WIDTH, MAX_WIDTH);
					Space(6);

					// Height
					EditorGUI.LabelField(GUIRect(0, 18), "Height");
					int newHeight = EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.Height);
					newHeight = Mathf.Clamp(newHeight, MIN_HEIGHT, MAX_HEIGHT);
					if (newWidth != CurrentMapData.Width || newHeight != CurrentMapData.Height) {
						GenerateSeedAndMap(newWidth, newHeight);
						FreshPreview();
						Repaint();
					}
				});
				Space(12);


				// Iteration Threshold
				bool needRegenerate = false;
				LayoutH(() => {

					// Iteration
					EditorGUI.LabelField(GUIRect(0, 18), "Iteration");
					int newIteration = EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.Iteration);
					newIteration = Mathf.Clamp(newIteration, MIN_ITERATION, MAX_ITERATION);
					if (newIteration != CurrentMapData.Iteration) {
						CurrentMapData.Iteration = newIteration;
						needRegenerate = true;
					}
					Space(6);

					// Threshold
					EditorGUI.LabelField(GUIRect(0, 18), "Threshold");
					float newThreshold = Mathf.Clamp(
						EditorGUI.DelayedFloatField(GUIRect(0, 18), CurrentMapData.threshold),
						MIN_THRESHOLD, MAX_THRESHOLD
					);
					if (newThreshold != CurrentMapData.threshold) {
						CurrentMapData.threshold = newThreshold;
						needRegenerate = true;
					}

				});
				Space(4);

				// BridgeWidth CloseEdge
				LayoutH(() => {
					// BridgeWidth
					EditorGUI.LabelField(GUIRect(0, 18), "Bridge");
					int newBWidth = Mathf.Clamp(
						EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.bridgeWidth),
						MIN_BRIDGE, MAX_BRIDGE
					);
					if (newBWidth != CurrentMapData.bridgeWidth) {
						CurrentMapData.bridgeWidth = newBWidth;
						needRegenerate = true;
					}
					Space(6);
					// Edge
					EditorGUI.LabelField(GUIRect(0, 18), "Edge");
					int newEdge = Mathf.Abs(EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.edge));
					if (CurrentMapData.edge != newEdge) {
						CurrentMapData.edge = newEdge;
						needRegenerate = true;
					}
				});
				Space(12);

				// Mountain
				LayoutH(() => {
					// Altitude
					EditorGUI.LabelField(GUIRect(0, 18), "Altitude");
					int newHeight = Mathf.Clamp(
						EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.maxAltitude),
						FleckMap.MIN_MOUNTAIN_HEIGHT, FleckMap.MAX_MOUNTAIN_HEIGHT
					);
					if (newHeight != CurrentMapData.maxAltitude) {
						CurrentMapData.maxAltitude = newHeight;
						needRegenerate = true;
					}

					Space(6);
					// Bump
					EditorGUI.LabelField(GUIRect(0, 18), "Bump");
					float newBump = Mathf.Clamp01(EditorGUI.DelayedFloatField(GUIRect(0, 18), CurrentMapData.Bump));
					if (CurrentMapData.Bump != newBump) {
						CurrentMapData.Bump = newBump;
						needRegenerate = true;
					}
				});
				Space(4);


				// BirthPoint
				LayoutH(() => {
					EditorGUI.LabelField(GUIRect(0, 18), "Max BirthPoint Num");
					int newBPNum = Mathf.Clamp(
						EditorGUI.DelayedIntField(GUIRect(0, 18), CurrentMapData.maxBirthPointNum),
						0, MAX_BIRTH_POINT_NUM
					);
					if (newBPNum != CurrentMapData.maxBirthPointNum) {
						CurrentMapData.maxBirthPointNum = newBPNum;
						needRegenerate = true;
					}
				});
				Space(12);


				// Wall
				LayoutH(() => {

					// ColorA
					ItemColorA = EditorGUI.ColorField(
						GUIRect(16, 16),
						GUIContent.none,
						ItemColorA,
						false, false, false, null
					);
					Space(6);

					// Label
					EditorGUI.LabelField(GUIRect(76, 16), "Wall/Water");

					// PrefabA
					PrefabA = EditorGUI.ObjectField(
						GUIRect(0, 16),
						PrefabA,
						typeof(GameObject),
						false
					) as GameObject;

				});
				Space(4);

				// Ground
				LayoutH(() => {

					// ColorB
					ItemColorB = EditorGUI.ColorField(
						GUIRect(16, 16),
						GUIContent.none,
						ItemColorB,
						false, false, false, null
					);
					Space(6);

					// Label
					EditorGUI.LabelField(GUIRect(76, 16), "Ground");

					// PrefabB
					PrefabB = EditorGUI.ObjectField(
						GUIRect(0, 16),
						PrefabB,
						typeof(GameObject),
						false
					) as GameObject;

				});
				Space(4);

				// Point
				LayoutH(() => {

					// ColorC
					ItemColorC = EditorGUI.ColorField(
						GUIRect(16, 16),
						GUIContent.none,
						ItemColorC,
						false, false, false, null
					);
					Space(6);

					// Label
					EditorGUI.LabelField(GUIRect(76, 16), "Birth Point");

					// PrefabC
					PrefabC = EditorGUI.ObjectField(
						GUIRect(0, 16),
						PrefabC,
						typeof(GameObject),
						false
					) as GameObject;

				});
				Space(2);

				// Final
				if (needRegenerate) {
					GenerateMap();
					FreshPreview();
					Repaint();
				}

			}, "Modify", ref ModifyPanelOpen, true);
			Space(4);
		}



		void ExportGUI () {
			LayoutF(() => {

				Space(6);

				LayoutH(() => {
					const int BUTTON_HEIGHT = 34;
					if (GUI.Button(GUIRect(0, BUTTON_HEIGHT), "Save Json")) {
						// Export Json
						TaskForAll(TaskType.ExportJson);
					}
					Space(4);
					if (GUI.Button(GUIRect(0, BUTTON_HEIGHT), "Save Text")) {
						// Export Text
						TaskForAll(TaskType.ExportText);
					}
					Space(4);
					if (GUI.Button(GUIRect(0, BUTTON_HEIGHT), "Save Prefab")) {
						// Export Prefab
						TaskForAll(TaskType.ExportPrefab);
					}
				});

				Space(6);

				// Export To
				LayoutV(() => {
					GUI.Label(GUIRect(0, 18), "Export To:");
					Space(4);
					LayoutH(() => {
						Space(6);
						EditorGUI.SelectableLabel(GUIRect(0, 18), ExportPath, GUI.skin.textField);
						if (GUI.Button(GUIRect(60, 18), "Browse", EditorStyles.miniButtonMid)) {
							string newPath = Util.FixPath(EditorUtility.OpenFolderPanel("Select Export Path", ExportPath, ""));
							if (!string.IsNullOrEmpty(newPath)) {
								newPath = Util.RelativePath(newPath);
								if (!string.IsNullOrEmpty(newPath)) {
									ExportPath = newPath;
								} else {
									Util.Dialog("Warning", "Export path must in Assets folder.", "OK");
								}
							}
						}
					});
					Space(4);
				}, true);


			}, "Export", ref ExportPanelOpen, true);
			Space(4);
		}



		void SettingGUI () {

			//const int FIELD_WIDTH = 65;
			const int ITEM_HEIGHT = 16;

			LayoutF(() => {
				Space(2);
				LayoutH(() => {
					LogMessage = EditorGUI.Toggle(GUIRect(ITEM_HEIGHT, ITEM_HEIGHT), LogMessage);
					GUI.Label(GUIRect(0, 18), "Log To Console");
					Space(2);
					ShowDialog = EditorGUI.Toggle(GUIRect(ITEM_HEIGHT, ITEM_HEIGHT), ShowDialog);
					GUI.Label(GUIRect(0, 18), "Dialog Window");
				});
				Space(2);

				LayoutH(() => {
					HighLightAfterExport = EditorGUI.Toggle(GUIRect(ITEM_HEIGHT, ITEM_HEIGHT), HighLightAfterExport);
					GUI.Label(GUIRect(0, 18), "Highlight Result");
					Space(2);
					ColorfulTitle = EditorGUI.Toggle(GUIRect(ITEM_HEIGHT, ITEM_HEIGHT), ColorfulTitle);
					GUI.Label(GUIRect(0, 18), "Colorful Title");
				});
				Space(4);

			}, "Setting", ref SettingPanelOpen, true);
			Space(4);

		}




		#endregion



		#region --- TSK ---




		void TaskForAll (TaskType task) {

			// Do it
			TaskResult result = TaskResult.None;
			switch (task) {
				default:
				case TaskType.None:
					break;
				case TaskType.ExportPrefab:
					result = ExportPrefabTask();
					break;
				case TaskType.ExportJson:
					result = ExportJsonTask();
					break;
				case TaskType.ExportText:
					result = ExportTextTask();
					break;
			}

			// Get Msg
			string msg = "";
			switch (result) {
				default:
				case TaskResult.None:
					return;
				case TaskResult.Success:
					msg = "Success !\nFile created successfully.";
					break;
				case TaskResult.FileError:
					msg = "Fail.\nCan not create file.";
					break;
				case TaskResult.GenerationError:
					msg = "Fail.\nError on map generation.";
					break;
			}

			// Show Message
			if (LogMessage) {
				Debug.Log("[" + MAIN_TITLE + "] " + msg);
			}
			if (ShowDialog) {
				Util.Dialog("Success", msg, "OK");
			}

		}



		// Export Prefab
		TaskResult ExportPrefabTask () {

			string resultPath = Util.CombinePaths(ExportPath, CurrentMapData.name + ".prefab");
			Util.CreateFolder(Util.GetRelativeParentPath(resultPath));



			// Warning
			if (!PrefabA && !PrefabB) {
				if (!Util.Dialog(
					"Warning",
					"No source prefab was linked.\nThe map will be a group of empty GameObject.",
					"Still Continue",
					"Cancel"
				)) {
					return TaskResult.None;
				}
			} else if (CurrentMapData.Width * CurrentMapData.Height > 64 * 32) {
				if (!Util.Dialog(
					"Warning",
					"Map is too large.\nIt may takes a long time to create the prefab.",
					"Continue",
					"Cancel"
				)) {
					return TaskResult.None;
				}
			}

			// Spawn To Scene
			GameObject g = CurrentMapData.SpawnToScene(PrefabA, PrefabB, PrefabC);
			PrefabUtility.CreatePrefab(resultPath, g);
			DestroyImmediate(g, false);

			// Refresh
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// HightLight
			if (HighLightAfterExport) {
				Object obj = AssetDatabase.LoadAssetAtPath<Object>(resultPath);
				if (obj) {
					EditorGUIUtility.PingObject(obj);
				}
			}

			return TaskResult.Success;
		}



		// Export Json
		TaskResult ExportJsonTask () {

			string resultPath = Util.CombinePaths(ExportPath, CurrentMapData.name + ".json");
			Util.CreateFolder(Util.GetRelativeParentPath(resultPath));

			// Export Json
			try {
				string json = CurrentMapData.GetJson();
				if (Util.FileExist(resultPath)) {
					int index = Util.DialogComplex("Warning", "File is already exists.", "Cover", "Rename", "cancel");
					if (index == 1) {
						resultPath = Util.RenameForCreate(resultPath);
					} else if (index == 2) {
						return TaskResult.None;
					}
				}
				Util.Write(json, resultPath);
			} catch {
				return TaskResult.GenerationError;
			}

			// Refresh
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// HightLight
			if (HighLightAfterExport) {
				Object obj = AssetDatabase.LoadAssetAtPath<Object>(resultPath);
				if (obj) {
					EditorGUIUtility.PingObject(obj);
				}
			}

			return TaskResult.Success;
		}



		// Export Text
		TaskResult ExportTextTask () {

			string resultPath = Util.CombinePaths(ExportPath, CurrentMapData.name + "_Text.txt");
			Util.CreateFolder(Util.GetRelativeParentPath(resultPath));

			// Export Text
			try {
				string text = CurrentMapData.ToString();
				if (Util.FileExist(resultPath)) {
					int index = Util.DialogComplex("Warning", "File is already exists.", "Cover", "Rename", "cancel");
					if (index == 1) {
						resultPath = Util.RenameForCreate(resultPath);
					} else if (index == 2) {
						return TaskResult.None;
					}
				}
				Util.Write(text, resultPath);
			} catch {
				return TaskResult.GenerationError;
			}

			// Refresh
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			// HightLight
			if (HighLightAfterExport) {
				Object obj = AssetDatabase.LoadAssetAtPath<Object>(resultPath);
				if (obj) {
					EditorGUIUtility.PingObject(obj);
				}
			}

			return TaskResult.Success;
		}



		void NewMapTask () {
			if (Util.Dialog("", "Create a new map?\nYou will lose the unsaved change.", "Create new", "Cancel")) {
				GUI.FocusControl("");
				EditorApplication.delayCall += () => {
					CurrentMapData = new FleckMap();
					GenerateSeedAndMap();
					FreshPreview();
					Repaint();
				};
			}
		}



		void LoadMapFromJsonTask () {
			string path = EditorUtility.OpenFilePanel("Load From Json", Application.dataPath, "json");
			if (Util.FileExist(path)) {
				string json = Util.Read(path);
				try {
					CurrentMapData.LoadFromJson(json);
					FreshPreview();
					Repaint();
				} catch {
					Debug.LogWarning("This json file is NOT a fleck-map.");
				}
				Repaint();
			}
		}




		#endregion



		#region --- LGC ---



		void GenerateSeedAndMap (int width = -1, int height = -1) {
			width = width > 0 ? width : CurrentMapData.Width;
			height = height > 0 ? height : CurrentMapData.Height;
			Seeds = FleckMap.GetRandomSeeds(width, height);
			GenerateMap(width, height);
		}



		void GenerateMap (int width = -1, int height = -1) {
			width = width > 0 ? width : CurrentMapData.Width;
			height = height > 0 ? height : CurrentMapData.Height;
			if (Seeds == null) {
				GenerateSeedAndMap();
			} else {
				Util.ClearProgressBar();
				CurrentMapData.Generate(width, height, Seeds, (progress) => {
					Util.ProgressBar(
						MAIN_TITLE,
						string.Format("Generating map... {0}%", (progress * 100f).ToString("0")),
						progress
					);
				});
				Util.ClearProgressBar();
			}
		}



		void FreshPreview () {
			if (Seeds == null) {
				GenerateSeedAndMap();
			}
			PreviewTexture = new Texture2D(CurrentMapData.Width, CurrentMapData.Height) {
				filterMode = FilterMode.Point
			};
			Color[] colors = new Color[CurrentMapData.Width * CurrentMapData.Height];
			int w = CurrentMapData.Width;
			int h = CurrentMapData.Height;
			// Map Data
			for (int i = 0; i < w; i++) {
				for (int j = 0; j < h; j++) {
					Color groundColor = Color.Lerp(
						ItemColorB, Color.black,
						(CurrentMapData[i, j] - 1) / (CurrentMapData.maxAltitude + 3f)
					);
					colors[j * w + i] = CurrentMapData[i, j] == 0 ? ItemColorA : groundColor;
				}
			}
			// Birth Point
			Color c = Color.Lerp(ItemColorC, ItemColorB, 0.5f);
			for (int i = 0; i < CurrentMapData.BirthPoints.Count; i++) {
				int x = (int)CurrentMapData.BirthPoints[i].x;
				int y = (int)CurrentMapData.BirthPoints[i].y;
				colors[y * w + x] = ItemColorC;
				if (x > 0) {
					colors[y * w + x - 1] = c;
				}
				if (y > 0) {
					colors[(y - 1) * w + x] = c;
				}
				if (x < w - 1) {
					colors[y * w + x + 1] = c;
				}
				if (y < h - 1) {
					colors[(y + 1) * w + x] = c;
				}

			}
			PreviewTexture.SetPixels(colors);
			PreviewTexture.Apply();
			GUI.FocusControl("");
		}




		#endregion



		#region --- SYS ---


		void EditorLoad () {

			// Bool
			ModifyPanelOpen = EditorPrefs.GetBool(MAIN_TITLE + ".ModifyPanelOpen", ModifyPanelOpen);
			ModifyPanelOpen = EditorPrefs.GetBool(MAIN_TITLE + ".PoolPanelOpen", PoolPanelOpen);
			ExportPanelOpen = EditorPrefs.GetBool(MAIN_TITLE + ".ExportPanelOpen", ExportPanelOpen);
			SettingPanelOpen = EditorPrefs.GetBool(MAIN_TITLE + ".SettingPanelOpen", SettingPanelOpen);
			LogMessage = EditorPrefs.GetBool(MAIN_TITLE + ".LogMessage", LogMessage);
			ShowDialog = EditorPrefs.GetBool(MAIN_TITLE + ".ShowDialog", ShowDialog);
			ColorfulTitle = EditorPrefs.GetBool(MAIN_TITLE + ".ColorfulTitle", ColorfulTitle);
			HighLightAfterExport = EditorPrefs.GetBool(MAIN_TITLE + ".HighLightAfterExport", HighLightAfterExport);

			// String
			ExportPath = EditorPrefs.GetString(MAIN_TITLE + ".ExportPath", ExportPath);

			// Int
			ZoomLevel = EditorPrefs.GetInt(MAIN_TITLE + ".ZoomLevel", ZoomLevel);

			// Color
			ItemColorA = new Color(
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorA.r", ItemColorA.r),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorA.g", ItemColorA.g),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorA.b", ItemColorA.b)
			);
			ItemColorB = new Color(
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorB.r", ItemColorB.r),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorB.g", ItemColorB.g),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorB.b", ItemColorB.b)
			);
			ItemColorC = new Color(
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorC.r", ItemColorC.r),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorC.g", ItemColorC.g),
				EditorPrefs.GetFloat(MAIN_TITLE + "ItemColorC.b", ItemColorC.b)
			);

			// Prefabs
			{
				string id = EditorPrefs.GetString(MAIN_TITLE + "PrefabA", "");
				string path = AssetDatabase.GUIDToAssetPath(id);
				PrefabA = null;
				if (!string.IsNullOrEmpty(path)) {
					GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (g) {
						PrefabA = g;
					}
				}
			}
			{
				string id = EditorPrefs.GetString(MAIN_TITLE + "PrefabB", "");
				string path = AssetDatabase.GUIDToAssetPath(id);
				PrefabB = null;
				if (!string.IsNullOrEmpty(path)) {
					GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (g) {
						PrefabB = g;
					}
				}
			}
			{
				string id = EditorPrefs.GetString(MAIN_TITLE + "PrefabC", "");
				string path = AssetDatabase.GUIDToAssetPath(id);
				PrefabC = null;
				if (!string.IsNullOrEmpty(path)) {
					GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if (g) {
						PrefabC = g;
					}
				}
			}

		}



		void EditorSave () {

			// Bool
			EditorPrefs.SetBool(MAIN_TITLE + ".ModifyPanelOpen", ModifyPanelOpen);
			EditorPrefs.SetBool(MAIN_TITLE + ".PoolPanelOpen", PoolPanelOpen);
			EditorPrefs.SetBool(MAIN_TITLE + ".ExportPanelOpen", ExportPanelOpen);
			EditorPrefs.SetBool(MAIN_TITLE + ".SettingPanelOpen", SettingPanelOpen);
			EditorPrefs.SetBool(MAIN_TITLE + ".LogMessage", LogMessage);
			EditorPrefs.SetBool(MAIN_TITLE + ".ShowDialog", ShowDialog);
			EditorPrefs.SetBool(MAIN_TITLE + ".ColorfulTitle", ColorfulTitle);
			EditorPrefs.SetBool(MAIN_TITLE + ".HighLightAfterExport", HighLightAfterExport);

			// String
			EditorPrefs.SetString(MAIN_TITLE + ".ExportPath", ExportPath);

			// Int
			EditorPrefs.SetInt(MAIN_TITLE + ".ZoomLevel", ZoomLevel);

			// Color
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorA.r", ItemColorA.r);
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorA.g", ItemColorA.g);
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorA.b", ItemColorA.b);
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorB.r", ItemColorB.r);
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorB.g", ItemColorB.g);
			EditorPrefs.SetFloat(MAIN_TITLE + "ItemColorB.b", ItemColorB.b);

			// Prefabs
			if (PrefabA) {
				string path = AssetDatabase.GetAssetPath(PrefabA);
				string id = AssetDatabase.AssetPathToGUID(path);
				EditorPrefs.SetString(MAIN_TITLE + "PrefabA", id);
			} else {
				EditorPrefs.SetString(MAIN_TITLE + "PrefabA", "");
			}
			if (PrefabB) {
				string path = AssetDatabase.GetAssetPath(PrefabB);
				string id = AssetDatabase.AssetPathToGUID(path);
				EditorPrefs.SetString(MAIN_TITLE + "PrefabB", id);
			} else {
				EditorPrefs.SetString(MAIN_TITLE + "PrefabB", "");
			}
			if (PrefabC) {
				string path = AssetDatabase.GetAssetPath(PrefabC);
				string id = AssetDatabase.AssetPathToGUID(path);
				EditorPrefs.SetString(MAIN_TITLE + "PrefabC", id);
			} else {
				EditorPrefs.SetString(MAIN_TITLE + "PrefabC", "");
			}

		}



		#endregion



		#region --- UTL ---



		Rect GUIRect (float width, float height) {
			return GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(width <= 0), GUILayout.ExpandHeight(height <= 0));
		}



		void LayoutV (System.Action action, bool box = false) {
			if (box) {
				GUIStyle style = new GUIStyle(GUI.skin.box) {
					padding = new RectOffset(6, 6, 2, 2)
				};
				GUILayout.BeginVertical(style);
			} else {
				GUILayout.BeginVertical();
			}
			action();
			GUILayout.EndVertical();
		}



		void LayoutH (System.Action action, bool box = false) {
			if (box) {
				GUIStyle style = new GUIStyle(GUI.skin.box);
				GUILayout.BeginHorizontal(style);
			} else {
				GUILayout.BeginHorizontal();
			}
			action();
			GUILayout.EndHorizontal();
		}



		void LayoutF (System.Action action, string label, ref bool open, bool box = false) {
			bool _open = open;
			LayoutV(() => {
				_open = GUILayout.Toggle(
					_open,
					label,
					GUI.skin.GetStyle("foldout"),
					GUILayout.ExpandWidth(true),
					GUILayout.Height(18)
				);
				if (_open) {
					action();
				}
			}, box);
			open = _open;
		}



		void Space (float space = 4f) {
			GUILayout.Space(space);
		}


		string TheS (int num) {
			return num > 1 ? "s" : "";
		}





		#endregion



	}


#endif
}