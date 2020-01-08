using Harmony12;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using XLShredLib;
using GameManagement;

namespace XLSkinEditor {
	enum CapType {
		defaultCap,
		forwardSnapback,
		backwardSnapback,
		beanie
	}

	class Preset {
		public string hatPath = "", shirtPath = "", pantsPath = "", shoesPath = "", boardPath = "";
		public bool usingHoodie = false;
		public CapType cap = CapType.defaultCap;
	}

	class SkinEditor : MonoBehaviour{
		Rect SkinEditorWindowRect = new Rect(20, 10, 180, 0);
		public KeyCode toggleKey = KeyCode.O;

		public bool show = false;

		SkinEditorWindowShow selectedWindow;

		enum SkinEditorWindowShow {
			MainSelector,
			Skateboard,
			TeeShirt,
			Hoodie,
			Pants,
			Shoes,
			Hat,
			Beanie,
			Snapback,
			SelectHatType
		}


		string mainPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SkaterXL\\Skin\\";

		readonly string[] SkateboardMaterials = new string[] { "GripTape", "Deck", "Hanger", "Wheel1 Mesh", "Wheel2 Mesh", "Wheel3 Mesh", "Wheel4 Mesh" };

		public const string MainTextureName = "Texture2D_4128E5C7";
		public const string NormalTextureName = "Texture2D_BEC07F52";
		public const string rgmtaoTextureName = "Texture2D_B56F9766";

		public const string MainDeckTextureName = "Texture2D_694A07B4";
		public const string NormalDeckTextureName = "Texture2D_BBD4D99B";
		public const string rgmtaoDeckTextureName = "Texture2D_EDCB0FF8";

		string[] boardFiles = null;
		string[] tShirtFiles = null;
		string[] hoodieFiles = null;
		string[] shoesFiles = null;
		string[] hatFiles = null;
		string[] beanieFiles = null;
		string[] snapbackFiles = null;
		string[] pantsFiles = null;

		Preset preset = new Preset();

		GameObject skater, board, skaterMeshesObject;

		CapType currentCap = CapType.defaultCap;

		AssetBundle assetsBundle;

		private CharacterCustomizer _characterCustomizer;

		public CharacterCustomizer characterCustomizer {
			get {
				if (_characterCustomizer == null) {
					_characterCustomizer = this.skater.GetComponent<CharacterCustomizer>();
				}
				return _characterCustomizer;
			}
		}

		public List<Tuple<CharacterGear, GameObject>> gearList {
			get {
				return Traverse.Create(characterCustomizer).Field("equippedGear").GetValue() as List<Tuple<CharacterGear, GameObject>>;
			}
		}
		
		public CharacterCustomizer replayCharacterCustomizer {
			get {
				if (ReplayEditor.ReplayEditorController.Instance != null) {
					return ReplayEditor.ReplayEditorController.Instance.playbackController.characterCustomizer;
				}
				return null;
			}
		}

		public List<Tuple<CharacterGear, GameObject>> replayGearList {
			get {
				return replayCharacterCustomizer == null ? null : Traverse.Create(replayCharacterCustomizer).Field("equippedGear").GetValue() as List<Tuple<CharacterGear, GameObject>>;
			}
		}

		bool updatedReplayMesh = false;

		void Start() {
			LoadFiles();

			assetsBundle = AssetBundle.LoadFromFile(Directory.GetCurrentDirectory() + "\\Mods\\Silentbaws.XLSkinEditor\\skineditorassets");

			Transform[] componentsInChildren = PlayerController.Instance.gameObject.GetComponentsInChildren<Transform>();
			bool foundSkater = false;
			bool foundBoard = false;

			//Get the actual skater and skateboard from the root object
			for (int i = 0; i < componentsInChildren.Length; i++) {
				if (componentsInChildren[i].gameObject.name.Equals("NewSkater")) {
					if (!foundSkater) {
						if (componentsInChildren[i].Find("NewSteezeIK")) {
							this.skater = componentsInChildren[i].gameObject;
							foundSkater = true;
						}
					}
				} else if (componentsInChildren[i].gameObject.name.Equals("Skateboard")) {
					this.board = componentsInChildren[i].gameObject;
					foreach (Transform t in componentsInChildren[i].GetComponentsInChildren<Transform>()) {
						if (t.name.Equals(SkateboardMaterials[0])) {
							foundBoard = true;
							break;
						}
					}
				}

				if (foundBoard && foundSkater) {
					break;
				}
			}

			foreach (Transform child in skater.transform) {
				foreach (Transform t in child) {
					if (t.name.StartsWith("PAX", true, null)) {
						skaterMeshesObject = child.gameObject;
						break;
					}
				}
				if (skaterMeshesObject != null) break;
			}

			LoadPreset();
		}

		void LoadFiles() {
			if(Directory.Exists(mainPath + "Skateboard"))
				boardFiles = Directory.EnumerateFiles(mainPath + "Skateboard").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "TeeShirt"))
				tShirtFiles = Directory.EnumerateFiles(mainPath + "TeeShirt").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "Hoodie"))
				hoodieFiles = Directory.EnumerateFiles(mainPath + "Hoodie").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if(Directory.Exists(mainPath + "Shoes"))
				shoesFiles = Directory.EnumerateFiles(mainPath + "Shoes").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "Hat"))
				hatFiles = Directory.EnumerateFiles(mainPath + "Hat").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "Beanie"))
				beanieFiles = Directory.EnumerateFiles(mainPath + "Beanie").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "Snapback"))
				snapbackFiles = Directory.EnumerateFiles(mainPath + "Snapback").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();

			if (Directory.Exists(mainPath + "Pants"))
				pantsFiles = Directory.EnumerateFiles(mainPath + "Pants").Where(file => file.ToLower().EndsWith("png") || file.ToLower().EndsWith("xlse")).ToArray();
		}

		void Update() {
			if (Input.GetKeyDown(toggleKey)) {
				show = !show;
				if (show) ModMenu.Instance.ShowCursor(Main.modId);
				else ModMenu.Instance.HideCursor(Main.modId);
			}

			UpdateReplayhat();
		}

		private void UpdateReplayhat() {
			if (replayGearList != null && !updatedReplayMesh && GameStateMachine.Instance.CurrentState.GetType() == typeof(ReplayState)) {
				Debug.Log("update that shit my dude");

				Tuple<Mesh, Texture2D[]> hat = GetHatMesh();

				foreach (Tuple<CharacterGear, GameObject> t in replayGearList) {
					if (t.Item1.categoryName.Equals("Hat")) {
						t.Item2.GetComponent<SkinnedMeshRenderer>().sharedMesh = hat.Item1;
						t.Item2.GetComponent<Renderer>().sharedMaterial.SetTexture(NormalTextureName, hat.Item2[0]);
						t.Item2.GetComponent<Renderer>().sharedMaterial.SetTexture(rgmtaoTextureName, hat.Item2[1]);
						updatedReplayMesh = true;
						break;
					}
				}
			} else if (GameStateMachine.Instance.CurrentState.GetType() != typeof(ReplayState)) {
				updatedReplayMesh = false;
			}
		}

		private void OnGUI() {
			if (show) {
				GUI.backgroundColor = Color.black;
				SkinEditorWindowRect = GUI.Window(0, SkinEditorWindowRect, SkinEditorWindow, "Skin Editor");
			}
		}

		private void SkinEditorWindow(int windowID) {
			GUI.DragWindow(new Rect(0, 0, 10000, 20));

			switch (selectedWindow) {
				case SkinEditorWindowShow.MainSelector:
					GUI.Label(new Rect(50, 20, 500, 30), "Select type");

					if (GUI.Button(new Rect(15, 40, 150, 25), "Skateboard")) {
						selectedWindow = SkinEditorWindowShow.Skateboard;
					}
					if (GUI.Button(new Rect(15, 70, 150, 25), "TeeShirt")) {
						selectedWindow = SkinEditorWindowShow.TeeShirt;
					}
					if (GUI.Button(new Rect(15, 100, 150, 25), "Hoodie")) {
						selectedWindow = SkinEditorWindowShow.Hoodie;
					}
					if (GUI.Button(new Rect(15, 130, 150, 25), "Pants")) {
						selectedWindow = SkinEditorWindowShow.Pants;
					}
					if (GUI.Button(new Rect(15, 160, 150, 25), "Shoes")) {
						selectedWindow = SkinEditorWindowShow.Shoes;
					}
					if (GUI.Button(new Rect(15, 190, 150, 25), "Hat")) {
						selectedWindow = SkinEditorWindowShow.Hat;
					}
					if (GUI.Button(new Rect(15, 220, 150, 25), "Beanie")) {
						selectedWindow = SkinEditorWindowShow.Beanie;
					}
					if (GUI.Button(new Rect(15, 250, 150, 25), "Snapback")) {
						selectedWindow = SkinEditorWindowShow.Snapback;
					}
					if (GUI.Button(new Rect(15, 280, 150, 25), "Select Hat Type")) {
						selectedWindow = SkinEditorWindowShow.SelectHatType;
					}
					GUI.backgroundColor = Color.cyan;
					if (GUI.Button(new Rect(15, 310, 150, 25), "Fuck hats!")) {
						foreach (Tuple<CharacterGear, GameObject> t in gearList) {
							if (t.Item1.categoryName.Equals("Hat")) {
								RemoveTupleFromGear(t);
								break;
							}
						}
					}
					if (GUI.Button(new Rect(15, 340, 150, 25), "Fuck Clothes!")) {
						List<Tuple<CharacterGear, GameObject>> newList = new List<Tuple<CharacterGear, GameObject>>();

						foreach (Tuple<CharacterGear, GameObject> t in gearList) {
							if (t.Item1.categoryName.Equals("Hoodie") || t.Item1.categoryName.Equals("Shirt") || t.Item1.categoryName.Equals("Pants") || t.Item1.categoryName.Equals("Shoes")) {
								newList.Add(t);
							}
						}
						foreach (Tuple<CharacterGear, GameObject> t in newList) {
							RemoveTupleFromGear(t);
						}

						Traverse.Create(characterCustomizer).Method("UpdateBodyMesh").GetValue();
					}
					GUI.backgroundColor = Color.red;
					if (GUI.Button(new Rect(15, 370, 150, 25), "Exit")) {
						show = false;
						ModMenu.Instance.HideCursor(Main.modId);
					}
					SkinEditorWindowRect.height = 400;
					break;
				case SkinEditorWindowShow.SelectHatType:
					GUI.Label(new Rect(50, 20, 500, 30), "Select hat type");

					if (GUI.Button(new Rect(15, 40, 150, 25), "Default Hat")) {
						currentCap = CapType.defaultCap;
						ApplyNewHat();
					}
					if (GUI.Button(new Rect(15, 70, 150, 25), "Forward Snapback")) {
						currentCap = CapType.forwardSnapback;
						ApplyNewHat();
					}
					if (GUI.Button(new Rect(15, 100, 150, 25), "Backward Snapback")) {
						currentCap = CapType.backwardSnapback;
						ApplyNewHat();
					}
					if (GUI.Button(new Rect(15, 130, 150, 25), "Beanie")) {
						currentCap = CapType.beanie;
						ApplyNewHat();
					}

					GUI.backgroundColor = Color.red;
					if (GUI.Button(new Rect(10, 280, 150, 25), "Exit")) {
						selectedWindow = SkinEditorWindowShow.MainSelector;
					}
					SkinEditorWindowRect.height = 315;
					break;
				default:
					DrawSelectSkin();
					break;
			}

		}

		Vector2 scrollPosition = Vector2.zero;

		void DrawSelectSkin() {

			string[] files = null;

			switch (selectedWindow) {
				case SkinEditorWindowShow.Hat:
					files = hatFiles;
					break;
				case SkinEditorWindowShow.Beanie:
					files = beanieFiles;
					break;
				case SkinEditorWindowShow.Hoodie:
					files = hoodieFiles;
					break;
				case SkinEditorWindowShow.Pants:
					files = pantsFiles;
					break;
				case SkinEditorWindowShow.Shoes:
					files = shoesFiles;
					break;
				case SkinEditorWindowShow.Skateboard:
					files = boardFiles;
					break;
				case SkinEditorWindowShow.Snapback:
					files = snapbackFiles;
					break;
				case SkinEditorWindowShow.TeeShirt:
					files = tShirtFiles;
					break;
			}

			GUI.Label(new Rect(50, 20, 500, 30), selectedWindow.ToString());

			if (files != null && files.Length > 0) {
				GUIStyle style = new GUIStyle(GUI.skin.verticalScrollbar);
				scrollPosition = GUI.BeginScrollView(new Rect(0, 40, 180, 200), scrollPosition, new Rect(0, 0, 170, 30 * files.Length), false, true, GUIStyle.none, style);
				for (int i = 0; i < files.Length; i++) {
					string CurrentSkinName = Path.GetFileNameWithoutExtension(files[i]);
					if (GUI.Button(new Rect(10, (30 * i), 150, 25), CurrentSkinName)) {

						Texture2D loadedTexture = new Texture2D(0, 0, TextureFormat.RGB24, false);
						Texture2D loadedNormal = null;
						Texture2D loadedRgmtao = null;

						if (Path.GetExtension(files[i]).Trim().Equals(".png", StringComparison.CurrentCultureIgnoreCase))
							loadedTexture.LoadImage(File.ReadAllBytes(files[i]));
						else
							ParseXLSE(files[i], out loadedTexture, out loadedNormal, out loadedRgmtao);

						switch (selectedWindow) {
							case SkinEditorWindowShow.Hat:
								updateTexture("Hat", loadedTexture, loadedNormal, loadedRgmtao);
								preset.hatPath = files[i];
								preset.cap = currentCap;
								break;
							case SkinEditorWindowShow.Beanie:
								updateTexture("Hat", loadedTexture, loadedNormal, loadedRgmtao);
								preset.hatPath = files[i];
								preset.cap = currentCap;
								break;
							case SkinEditorWindowShow.Snapback:
								updateTexture("Hat", loadedTexture, loadedNormal, loadedRgmtao);
								preset.hatPath = files[i];
								preset.cap = currentCap;
								break;
							case SkinEditorWindowShow.Hoodie:
								updateTexture("Hoodie", loadedTexture, loadedNormal, loadedRgmtao);
								preset.shirtPath = files[i];
								preset.usingHoodie = true;
								break;
							case SkinEditorWindowShow.Pants:
								updateTexture("Pants", loadedTexture, loadedNormal, loadedRgmtao);
								preset.pantsPath = files[i];
								break;
							case SkinEditorWindowShow.Shoes:
								updateTexture("Shoes", loadedTexture, loadedNormal, loadedRgmtao);
								preset.shoesPath = files[i];
								break;
							case SkinEditorWindowShow.Skateboard:
								updateTexture("Skateboard", loadedTexture, loadedNormal, loadedRgmtao);
								preset.boardPath = files[i];
								break;
							case SkinEditorWindowShow.TeeShirt:
								updateTexture("Shirt", loadedTexture, loadedNormal, loadedRgmtao);
								preset.shirtPath = files[i];
								preset.usingHoodie = false;
								break;
						}

						SaveCurrentPreset();
					}

				}
				GUI.EndScrollView();
			}
			GUI.backgroundColor = Color.green;
			if (GUI.Button(new Rect(10, 250, 150, 25), "Reload")) {
				LoadFiles();
			}
			GUI.backgroundColor = Color.red;
			if (GUI.Button(new Rect(10, 280, 150, 25), "Exit")) {
				selectedWindow = SkinEditorWindowShow.MainSelector;
			}
			SkinEditorWindowRect.height = 315;
		}

		private void SaveCurrentPreset() {
			string json = JsonUtility.ToJson(preset, true);

			File.WriteAllText(mainPath + "preset.json", json);
		}

		private void LoadPreset() {
			if (!File.Exists(mainPath + "preset.json")) return;
			preset = JsonUtility.FromJson<Preset>(File.ReadAllText(mainPath + "preset.json"));

			Texture2D loadedTexture, loadedNormal, loadedRgMtAo;

			if(preset.hatPath != "" && File.Exists(preset.hatPath)) {
				LoadTextures(preset.hatPath, out loadedTexture, out loadedNormal, out loadedRgMtAo);
				switch (preset.cap) {
					case CapType.defaultCap:
						selectedWindow = SkinEditorWindowShow.Hat;
						break;
					case CapType.beanie:
						selectedWindow = SkinEditorWindowShow.Beanie;
						break;
					case CapType.forwardSnapback:
						selectedWindow = SkinEditorWindowShow.Snapback;
						break;
					case CapType.backwardSnapback:
						selectedWindow = SkinEditorWindowShow.Snapback;
						break;
				}
				updateTexture("Hat", loadedTexture, loadedNormal, loadedRgMtAo);
				currentCap = preset.cap;
				ApplyNewHat();
			}
			if(preset.shirtPath != "" && File.Exists(preset.shirtPath)) {
				LoadTextures(preset.shirtPath, out loadedTexture, out loadedNormal, out loadedRgMtAo);
				updateTexture(preset.usingHoodie ? "Hoodie" : "Shirt", loadedTexture, loadedNormal, loadedRgMtAo);
			}
			if(preset.pantsPath != "" && File.Exists(preset.pantsPath)) {
				selectedWindow = SkinEditorWindowShow.Pants;
				LoadTextures(preset.pantsPath, out loadedTexture, out loadedNormal, out loadedRgMtAo);
				updateTexture("Pants", loadedTexture, loadedNormal, loadedRgMtAo);
			}
			if(preset.shoesPath != "" && File.Exists(preset.shoesPath)) {
				LoadTextures(preset.shoesPath, out loadedTexture, out loadedNormal, out loadedRgMtAo);
				updateTexture("Shoes", loadedTexture, loadedNormal, loadedRgMtAo);
			}
			if(preset.boardPath != "" && File.Exists(preset.boardPath)) {
				LoadTextures(preset.boardPath, out loadedTexture, out loadedNormal, out loadedRgMtAo);
				updateTexture("Skateboard", loadedTexture, loadedNormal, loadedRgMtAo);
			}

			selectedWindow = SkinEditorWindowShow.MainSelector;
		}

		private void LoadTextures(string file, out Texture2D loadedTexture, out Texture2D loadedNormal, out Texture2D loadedRgmtao) {
			loadedTexture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
			loadedTexture.wrapMode = TextureWrapMode.Clamp;
			loadedNormal = null;
			loadedRgmtao = null;

			if (Path.GetExtension(file).Trim().Equals(".png", StringComparison.CurrentCultureIgnoreCase))
				loadedTexture.LoadImage(File.ReadAllBytes(file));
			else
				ParseXLSE(file, out loadedTexture, out loadedNormal, out loadedRgmtao);

			loadedTexture.wrapMode = TextureWrapMode.Clamp;
			if (loadedNormal != null) {
				loadedNormal.wrapMode = TextureWrapMode.Clamp;
			}
			if (loadedRgmtao != null) loadedRgmtao.wrapMode = TextureWrapMode.Clamp;
		}

		private void updateTexture(string GearItem, Texture2D texture, Texture2D normal, Texture2D rgmtao) {
			if (!GearItem.Equals("Skateboard")) {
				if (!GearItem.Equals("Shirt") && !GearItem.Equals("Hoodie")) {
					foreach (Tuple<CharacterGear, GameObject> t in gearList) {
						if (t.Item1.categoryName.Equals(GearItem)) {
							if(GearItem == "Shoes") {
								RemoveTupleFromGear(t);

								CharacterGear newGear = CreateGear("PAX_1", "Black White Sole", "CharacterCustomization/shoes/PAX_1", GearItem);
								GameObject newObject = CustomLoadPrefab(Resources.Load<GameObject>(newGear.path), skaterMeshesObject.transform);

								AddGear(newGear, newObject);

								GameObject Shoe_L = newObject.transform.Find("Shoe_L").gameObject;
								GameObject Shoe_R = newObject.transform.Find("Shoe_R").gameObject;

								if (normal == null) {
									normal = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Shoe_Normal.png");
								}
								if (rgmtao == null) {
									rgmtao = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Shoe_RgMtAo.png");
								}

								SetNewTextures(Shoe_L, texture, normal, rgmtao);
								SetNewTextures(Shoe_R, texture, normal, rgmtao);

								break;
							} else {
								RemoveTupleFromGear(t);

								string name = "";
								string path = "";

								CharacterGear newGear = null;
								switch (GearItem) {
									case "Hat":
										name = "Black";
										path = "CharacterCustomization/Hat/PAX_1";
										newGear = CreateGear("PAX_1", name, path, GearItem);

										break;
									case "Pants":
										name = "Black";
										path = "CharacterCustomization/pants/PAX_1";
										newGear = CreateGear("PAX_1", name, path, GearItem);

										break;
								}

								GameObject prefab = Resources.Load<GameObject>(newGear.path);
								GameObject newObject = CustomLoadPrefab(prefab, skaterMeshesObject.transform);

								Tuple<Mesh, Texture2D[]> hat = null;
								if (selectedWindow == SkinEditorWindowShow.Hat) {
									if (currentCap != CapType.defaultCap)
										currentCap = CapType.defaultCap;
									hat = GetHatMesh();
								} else if (selectedWindow == SkinEditorWindowShow.Beanie) {
									currentCap = CapType.beanie;
									hat = GetHatMesh();
								}else if(selectedWindow == SkinEditorWindowShow.Snapback) {
									if(currentCap != CapType.backwardSnapback || currentCap != CapType.forwardSnapback) {
										currentCap = CapType.forwardSnapback;
									}
									hat = GetHatMesh();
								}

								if(hat != null) {
									newObject.GetComponent<SkinnedMeshRenderer>().sharedMesh = hat.Item1;

									if (normal == null) {
										normal = hat.Item2[0];
									}
									if(rgmtao == null) {
										rgmtao = hat.Item2[1];
									}

									UpdateReplayhat();
								} else {
									if (normal == null) {
										normal = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Pants_Normal.png");
									}
									if (rgmtao == null) {
										rgmtao = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Pants_RgMtAo.png");
									}
								}

								AddGear(newGear, newObject);

								SetNewTextures(newObject, texture, normal, rgmtao);

								break;
							}
						}
					}
				} else {
					foreach (Tuple<CharacterGear, GameObject> t in gearList) {
						if (t.Item1.categoryName.Equals("Hoodie") || t.Item1.categoryName.Equals("Shirt")) {
							RemoveTupleFromGear(t);

							string name = GearItem.Equals("Hoodie") ? "Black" : "Black";
							string path = GearItem.Equals("Hoodie") ? "CharacterCustomization/Hoodie/PAX_1" : "CharacterCustomization/Shirt/PAX_1";
							normal = normal != null ? normal : GearItem.Equals("Hoodie") ? assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Hoodie_Normal.png") : assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Shirt_Normal.png");
							rgmtao = rgmtao != null ? rgmtao : GearItem.Equals("Hoodie") ? assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Hoodie_RgMtAo.png") : assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Shirt_RgMtAo.png");

							CharacterGear newGear = CreateGear("PAX_1", name, path, GearItem);

							GameObject newObject = CustomLoadPrefab(Resources.Load<GameObject>(newGear.path), skaterMeshesObject.transform);

							AddGear(newGear, newObject);

							SetNewTextures(newObject, texture, normal, rgmtao);

							Traverse.Create(characterCustomizer).Method("UpdateBodyMesh").GetValue();

							break;
						}
					}
				}
			} else {
				foreach (Transform t in board.GetComponentsInChildren<Transform>()) {
					if (SkateboardMaterials.Contains(t.name)) {
						Renderer r = t.GetComponent<Renderer>();
						if (r != null) {
							if (t.name.Equals(SkateboardMaterials[0])) {
								UnityEngine.Object.Destroy(r.sharedMaterial.GetTexture(MainDeckTextureName));
							}

							r.sharedMaterial.SetTexture(MainDeckTextureName, texture);
							if (normal != null) {
								r.sharedMaterial.SetTexture(NormalDeckTextureName, normal);
							}
							if (rgmtao != null) {
								r.sharedMaterial.SetTexture(rgmtaoDeckTextureName, rgmtao);
							}
						}
					}
				}
			}
		}

		private void ApplyNewHat() {
			Tuple<Mesh, Texture2D[]> hat = GetHatMesh();
			foreach (Tuple<CharacterGear, GameObject> t in gearList) {
				if (t.Item1.categoryName.Equals("Hat")) {
					t.Item2.GetComponent<SkinnedMeshRenderer>().sharedMesh = hat.Item1;
					t.Item2.GetComponent<Renderer>().sharedMaterial.SetTexture(NormalTextureName, hat.Item2[0]);
					t.Item2.GetComponent<Renderer>().sharedMaterial.SetTexture(rgmtaoTextureName, hat.Item2[1]);
				}
			}

			preset.cap = currentCap;
			SaveCurrentPreset();
			UpdateReplayhat();
		}

		private Tuple<Mesh, Texture2D[]> GetHatMesh() {
			Mesh mesh = null;
			Texture2D[] defaultTextures = new Texture2D[2];
			switch (currentCap) {
				case CapType.defaultCap:
					mesh = assetsBundle.LoadAsset<Mesh>("Assets/DefaultMeshes/cap.obj");
					defaultTextures[0] = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Cap_Normal.png");
					defaultTextures[1] = assetsBundle.LoadAsset<Texture2D>("Assets/DefaultTextures/Cap_RgMtAo.png");
					break;
				case CapType.backwardSnapback:
					mesh = assetsBundle.LoadAsset<Mesh>("Assets/Snapback/capbackward.obj");
					defaultTextures[0] = assetsBundle.LoadAsset<Texture2D>("Assets/Snapback/Cap_Normal.png");
					defaultTextures[1] = assetsBundle.LoadAsset<Texture2D>("Assets/Snapback/Cap_RgMtAo.png");
					break;
				case CapType.forwardSnapback:
					mesh = assetsBundle.LoadAsset<Mesh>("Assets/Snapback/capforward.obj");
					defaultTextures[0] = assetsBundle.LoadAsset<Texture2D>("Assets/Snapback/Cap_Normal.png");
					defaultTextures[1] = assetsBundle.LoadAsset<Texture2D>("Assets/Snapback/Cap_RgMtAo.png");
					break;
				case CapType.beanie:
					mesh = assetsBundle.LoadAsset<Mesh>("Assets/Beanie/Beanie.obj");
					defaultTextures[0] = assetsBundle.LoadAsset<Texture2D>("Assets/Beanie/Beanie_Normal.png");
					defaultTextures[1] = assetsBundle.LoadAsset<Texture2D>("Assets/Beanie/Beanie_RgMtAo.png");
					break;
			}

			return Tuple.Create(mesh, defaultTextures);
		}

		private CharacterGear CreateGear(string id, string name, string path, string category) {
			CharacterGear newGear = new CharacterGear();
			newGear.id = id;
			newGear.name = name;
			newGear.path = path;
			newGear.categoryName = category;
			switch (category) {
				case "Hat":
					newGear.category = GearCategory.Hat;
					break;
				case "Shirt":
					newGear.category = GearCategory.Shirt;
					break;
				case "Hoodie":
					newGear.category = GearCategory.Hoodie;
					break;
				case "Pants":
					newGear.category = GearCategory.Pants;
					break;
				case "Shoes":
					newGear.category = GearCategory.Shoes;
					break;
			}

			return newGear;
		}

		private void RemoveTupleFromGear(Tuple<CharacterGear, GameObject> t) {
			gearList.Remove(t);
			GameObject.Destroy(t.Item2);
		}

		private GameObject CustomLoadPrefab(GameObject prefab, Transform root) {
			if (prefab.transform.Find("Shoe_R")) {
				UnityEngine.Object.Destroy(prefab.transform.Find("Shoe_R").GetComponent<Renderer>().sharedMaterial.GetTexture(MainTextureName));
				UnityEngine.Object.Destroy(prefab.transform.Find("Shoe_L").GetComponent<Renderer>().sharedMaterial.GetTexture(MainTextureName));
			} else {
				UnityEngine.Object.Destroy(prefab.GetComponent<Renderer>().sharedMaterial.GetTexture(MainTextureName));
			}

			GameObject newObject = GameObject.Instantiate(prefab, root);
			foreach (GearPrefabController gearPrefabController in newObject.GetComponentsInChildren<GearPrefabController>()) {
				try {
					gearPrefabController.SetBonesFromDict(Traverse.Create(characterCustomizer).Property("BonesDict").GetValue() as Dictionary<string, Transform>);
				} catch (Exception ex) {
				}
			}
			return newObject;
		}

		private void AddGear(CharacterGear newGear, GameObject newObject) {
			Tuple<CharacterGear, GameObject> newTuple = Tuple.Create<CharacterGear, GameObject>(newGear, newObject);

			gearList.Add(newTuple);
		}

		private void SetNewTextures(GameObject gameObject, Texture2D diffuse, Texture2D normal, Texture2D RgMtAo) {
			gameObject.GetComponent<Renderer>().sharedMaterial.SetTexture(MainTextureName, diffuse);
			if(normal != null)
				gameObject.GetComponent<Renderer>().sharedMaterial.SetTexture(NormalTextureName, normal);
			if(RgMtAo != null)
				gameObject.GetComponent<Renderer>().sharedMaterial.SetTexture(rgmtaoTextureName, RgMtAo);
		}

		private void ParseXLSE(string file, out Texture2D texture, out Texture2D normal, out Texture2D rgmtao) {
			texture = null;
			normal = null;
			rgmtao = null;

			byte[] fileBytes = File.ReadAllBytes(file);
			string start = ASCIIEncoding.ASCII.GetString(fileBytes, 0, 4);

			if (!start.Equals("XLSE")) {
				UnityModManagerNet.UnityModManager.Logger.Log("ERROR PARSING XLSE FILE: Invalid file format");
				return;
			}

			uint textureSize = BitConverter.ToUInt32(fileBytes, 4);
			uint normalSize = BitConverter.ToUInt32(fileBytes, 8);
			uint rgmtaoSize = BitConverter.ToUInt32(fileBytes, 12);

			uint startOffset = 16;
			uint normalStartOffset = startOffset + textureSize;
			uint rgmtaoStartOffset = normalStartOffset + normalSize;


			if(textureSize == 0) {
				UnityModManagerNet.UnityModManager.Logger.Log("ERROR PARSING XLSE FILE: " + file + " no diffuse texture exists");
				return;
			}

			byte[] textureData = new byte[(int)textureSize];
			Array.Copy(fileBytes, startOffset, textureData, 0, (int)textureSize);

			texture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.LoadImage(textureData);
			
			if(normalSize != 0) {
				textureData = new byte[(int)normalSize];
				Array.Copy(fileBytes, normalStartOffset, textureData, 0, (int)normalSize);

				normal = new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
				normal.wrapMode = TextureWrapMode.Clamp;
				normal.LoadImage(textureData);
			}

			if(rgmtaoSize != 0) {
				textureData = new byte[(int)rgmtaoSize];
				Array.Copy(fileBytes, rgmtaoStartOffset, textureData, 0, (int)rgmtaoSize);

				rgmtao = new Texture2D(0, 0, TextureFormat.RGBA32, false, true);
				rgmtao.wrapMode = TextureWrapMode.Clamp;
				rgmtao.LoadImage(textureData);
			}
		}
	}
}
