using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace Gen90Software.Tools
{
	public class PropsPlacementTool : EditorWindow
	{
		public enum PatternType
		{
			Line,
			Circle,
			Curve,
			Grid,
			Point
		}

		public enum DrawType
		{
			Raycast,
			Position,
			Rotation,
			Group
		}

		public enum OrderType
		{
			Random,
			Noise,
			Sequential
		}

		public enum CountmentType
		{
			Count,
			Delta
		}

		public enum UpwardType
		{
			Surface,
			World
		}

		private enum DrawStatus
		{
			None,
			DrawFirst,
			DrawSecond,
			DrawThird,
			DrawForth,
			Ready
		}

		[System.Serializable]
		private class PropsObject
		{
			public GameObject prefab;
			public bool usage;

			public PropsObject(GameObject go, bool use)
			{
				prefab = go;
				usage = use;
			}
		}

		private struct PropsData
		{
			public Vector2 coord;
			public Vector3 pos;
			public Vector3 fwd;
			public Vector3 uwd;
			public Vector3 scl;
			public bool active;
			public int propsIndex;
			public GameObject go;

			public void ApplyOffset(Vector3 oP, Vector3 oR, Vector3 oS)
			{
				Vector3 lwd = Vector3.Cross(fwd, uwd);

				pos +=
					lwd * oP.x +
					uwd * oP.y +
					fwd * oP.z;

				Quaternion rotator = Quaternion.AngleAxis(oR.x, lwd) * Quaternion.AngleAxis(oR.y, uwd) * Quaternion.AngleAxis(oR.z, fwd);
				fwd = rotator * fwd;
				uwd = rotator * uwd;

				scl += oS;
			}
		}

		private const float gizmoThickness = 2.0f;
		private const float gizmoScaleMin = 0.01f;
		private const float gizmoScaleMax = 0.05f;
		private const int normalizeResolution = 20;
		private const string version = "1.3.0";


		[SerializeField] private PatternType pattern = PatternType.Line;
		[SerializeField] private DrawType drawType = DrawType.Raycast;
		[SerializeField] private Transform propsParent;
		[SerializeField] private List<PropsObject> propsCollection;
		[SerializeField] private OrderType orderPlace = OrderType.Random;
		[SerializeField] private int seedPlace = 123;
		[SerializeField] private float scalePlace = 1.0f;
		[SerializeField] private CountmentType countment = CountmentType.Count;
		[SerializeField] private UpwardType upward = UpwardType.Surface;
		[SerializeField] private int countX = 5;
		[SerializeField] private int countY = 5;
		[SerializeField] private float deltaX = 10.0f;
		[SerializeField] private float deltaY = 10.0f;
		[SerializeField] private bool normalizeCurvePosition = true;
		[SerializeField] private bool pointingCurveRotation = false;
		[SerializeField] private OrderType orderFill = OrderType.Random;
		[SerializeField] private float fillRate = 1.0f;
		[SerializeField] private int seedFill = 123;
		[SerializeField] private float scaleFill = 1.0f;
		[SerializeField] private bool useFirst = true;
		[SerializeField] private bool useMiddle = true;
		[SerializeField] private bool useLast = true;
		[SerializeField] private bool useSurface = false;
		[SerializeField] private LayerMask surfaceMask = 0;
		[SerializeField] private float surfaceDistance = 10.0f;
		[SerializeField] private bool surfaceOverridePos = true;
		[SerializeField] private bool surfaceOverrideRot = true;
		[SerializeField] private LayerMask surfaceOverrideActivity = 0;
		[SerializeField] private Vector3 offsetPos = Vector3.zero;
		[SerializeField] private Vector3 offsetRot = Vector3.zero;
		[SerializeField] private Vector3 offsetScl = Vector3.zero;
		[SerializeField] private Vector3 jitterPos = Vector3.zero;
		[SerializeField] private Vector3 jitterRot = Vector3.zero;
		[SerializeField] private Vector3 jitterScl = Vector3.zero;
		[SerializeField] private int seedJitter = 123;
		[SerializeField] private bool uniformOffsetScl = true;
		[SerializeField] private bool uniformJitterScl = true;
		[SerializeField] private LayerMask drawMask = 0;
		[SerializeField] private float drawDistance = 1000.0f;
		[SerializeField] private float gizmoScale = 0.01f;
		[SerializeField] private bool usePreview;


		[SerializeField] private Vector3 point1;
		[SerializeField] private Vector3 point2;
		[SerializeField] private Vector3 point3;
		[SerializeField] private Vector3 point4;
		[SerializeField] private Vector3 normal1;
		[SerializeField] private Vector3 normal2;
		[SerializeField] private Vector3 normal3;
		[SerializeField] private Vector3 normal4;


		[SerializeField] private DrawStatus status;
		[SerializeField] private bool initControl;


		private Vector3 pointX;
		private Vector3 normalX;
		private float[] controlRatesX;
		private float[] controlRatesY;
		private PropsData[] controlProps;
		private float[] normalizeLUT;


		private bool reposition1;
		private bool reposition2;
		private bool reposition3;
		private bool reposition4;
		private Quaternion handleRotation1;
		private Quaternion handleRotation2;
		private Quaternion handleRotation3;
		private Quaternion handleRotation4;


		private Vector2 viewScroll;
		private Vector2 collectionScroll;
		private bool foldoutSurface;
		private bool foldoutOffset;
		private bool foldoutJitter;
		private bool foldoutSettings;


		#region INITIALIZATION
		[MenuItem("Tools/Gen90Software/Props Placement Tool")]
		public static void InitPropsPlacementToolWindow()
		{
			PropsPlacementTool ppt = GetWindow<PropsPlacementTool>();
			ppt.titleContent.text = "Props Placement Tool";
			ppt.Show();

			ppt.viewScroll = Vector2.zero;
			ppt.collectionScroll = Vector2.zero;
			ppt.foldoutOffset = false;
			ppt.foldoutJitter = false;
			ppt.foldoutSettings = false;

			ppt.surfaceMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(~Physics.IgnoreRaycastLayer);
			ppt.drawMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(~Physics.IgnoreRaycastLayer);
		}

		public void OnEnable()
		{
			SceneView.duringSceneGui += OnSceneGUI;
			Undo.undoRedoPerformed += OnUndoRedo;

			CalculateControlRates();
			CalculateControlProps();
			Repaint();
			SceneView.RepaintAll();
		}

		public void OnDisable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			Undo.undoRedoPerformed -= OnUndoRedo;

			DeletePreviewPropsObjects();
			Repaint();
			SceneView.RepaintAll();
		}

		public void OnUndoRedo()
		{
			DeletePreviewPropsObjects();
			CalculateControlRates();
			CalculateControlProps();
			Repaint();
			SceneView.RepaintAll();
		}
		#endregion

		#region MAIN
		public void OnGUI()
		{
			//DRAW AREA
			EditorGUILayout.Space(10);
			if (pattern != PatternType.Point)
			{
				switch (status)
				{
					case DrawStatus.None:
						if (GUILayout.Button("Draw " + pattern.ToString(), new GUIStyle("Button") { font = EditorStyles.boldFont, fontSize = 14 }, GUILayout.MinHeight(30)))
						{
							status = DrawStatus.DrawFirst;
							drawType = DrawType.Raycast;
						}
						break;

					case DrawStatus.DrawFirst:
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("Select the first point!", MessageType.None);
						if (GUILayout.Button("Cancel", GUILayout.Width(60)))
						{
							status = initControl ? DrawStatus.Ready : DrawStatus.None;
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Space(10);
						break;

					case DrawStatus.DrawSecond:
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("Select the second point!", MessageType.None);
						if (GUILayout.Button("Cancel", GUILayout.Width(60)))
						{
							status = initControl ? DrawStatus.Ready : DrawStatus.None;
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Space(10);
						break;

					case DrawStatus.DrawThird:
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("Select the third point!", MessageType.None);
						if (GUILayout.Button("Cancel", GUILayout.Width(60)))
						{
							status = initControl ? DrawStatus.Ready : DrawStatus.None;
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Space(10);
						break;

					case DrawStatus.DrawForth:
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.HelpBox("Select the forth point!", MessageType.None);
						if (GUILayout.Button("Cancel", GUILayout.Width(60)))
						{
							status = initControl ? DrawStatus.Ready : DrawStatus.None;
						}
						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Space(10);
						break;

					case DrawStatus.Ready:
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button("Redraw " + pattern.ToString(), new GUIStyle("Button") { font = EditorStyles.boldFont, fontSize = 14 }, GUILayout.MinHeight(30)))
						{
							status = DrawStatus.DrawFirst;
							drawType = DrawType.Raycast;
						}
						if (GUILayout.Button("Clear " + pattern.ToString(), new GUIStyle("Button") { font = EditorStyles.boldFont, fontSize = 14 }, GUILayout.MinHeight(30), GUILayout.MaxWidth(120)))
						{
							status = DrawStatus.None;
							ClearControlPoints();
						}
						EditorGUILayout.EndHorizontal();
						break;
				}
			}
			else
			{
				if (!initControl)
				{
					if (status != DrawStatus.None)
					{
						status = DrawStatus.None;
					}
				}
				EditorGUILayout.HelpBox("Place the objects with cursor!", MessageType.None);
				EditorGUILayout.Space(10);
			}

			//SCROLL VIEW START
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			viewScroll = EditorGUILayout.BeginScrollView(viewScroll);

			//PATTERN AREA
			PatternAreaGUI();

			//DRAW AREA
			DrawAreaGUI();

			//OBJECTS AREA
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
			Transform _propsParent = EditorGUILayout.ObjectField("Props Parent", propsParent, typeof(Transform), true) as Transform;
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Props Placement Tool Inspector");
				if (_propsParent == null || _propsParent.gameObject.scene.rootCount > 0)
				{
					propsParent = _propsParent;
				}
			}
			DropAreaGUI();
			CollectionListGUI();

			//PLACEMENT AREA
			PlacementAreaGUI();
			PlacementAreaCommonGUI();

			//SURFACE AREA
			SurfaceAreaGUI();

			//OFFSET AREA
			OffsetAreaGUI();

			//JITTER AREA
			JitterAreaGUI();

			//SETTINGS AREA
			SettingsAreaGUI();

			//SCROLL VIEW END
			EditorGUILayout.Space(20);
			EditorGUILayout.EndScrollView();

			//PLACE AREA
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			usePreview = GUILayout.Toggle(usePreview, "Preview: " + (usePreview ? "On" : "Off"), new GUIStyle("Button"));
			if (EditorGUI.EndChangeCheck())
			{
				PreviewProps();
			}
			EditorGUILayout.Space(5);
			if (pattern != PatternType.Point)
			{
				if (GUILayout.Button("Place", new GUIStyle("Button") { font = EditorStyles.boldFont, fontSize = 14 }, GUILayout.MinHeight(30)))
				{
					PlaceProps();
				}
			}
			else
			{
				EditorGUILayout.HelpBox("Place the objects with cursor!", MessageType.None);
				EditorGUILayout.Space(12);
			}
			EditorGUILayout.Space(10);
		}

		public void OnSceneGUI(SceneView sceneView)
		{
			DrawGizmos(Camera.current.transform.position);
			sceneView.Repaint();

			if (status == DrawStatus.None && pattern != PatternType.Point)
				return;

			if (!initControl && status == DrawStatus.Ready)
			{
				if (normal3 == Vector3.zero)
				{
					point3 = point2 + Vector3.Cross(point2 - point1, Vector3.up);
					normal3 = normal2;
				}

				if (normal4 == Vector3.zero)
				{
					point4 = point1 + (point3 - point2);
					normal4 = normal1;
				}

				initControl = true;
			}

			if (Event.current.modifiers != EventModifiers.None)
				return;

			if (pattern != PatternType.Point)
			{
				if (drawType == DrawType.Position)
				{
					PositionWithHandle();
					return;
				}

				if (drawType == DrawType.Rotation)
				{
					RotationWithHandle();
					return;
				}

				if (drawType == DrawType.Group)
				{
					GroupWithHandle();
					return;
				}
			}

			if (!new Rect(0, 0, Camera.current.pixelWidth, Camera.current.pixelHeight).Contains(HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition)))
				return;

			if (!Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out RaycastHit hit, drawDistance, drawMask))
				return;

			if (pattern == PatternType.Point)
			{
				PlaceByCursor(ref hit);
				return;
			}

			if (status == DrawStatus.Ready)
			{
				RepositionControlPoints(ref hit);
				return;
			}

			DrawControlPoints(ref hit);
		}

		private void PatternAreaGUI()
		{
			EditorGUI.BeginChangeCheck();
			GUILayout.Label(new GUIContent("Pattern",
				"Set the pattern of placement.\n\n" +
				"Line:\tPlace the objects along a line.\n" +
				"Circle:\tPlace the objects along a circle line.\n" +
				"Curve:\tPlace the objects along a curved line.\n" +
				"Grid:\tPlace the objects along a grid.\n" +
				"Point:\tPlace the objects individually."
				), EditorStyles.boldLabel);
			pattern = (PatternType) GUILayout.Toolbar((int) pattern, System.Enum.GetNames(typeof(PatternType)));
			if (EditorGUI.EndChangeCheck())
			{
				DeletePreviewPropsObjects();
				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void DrawAreaGUI()
		{
			if (pattern == PatternType.Point)
				return;

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label(new GUIContent("Draw",
				"Set the type of control point placement.\n\n" +
				"Raycast:\tModify control points with raycast.\n" +
				"Position:\tModify position with handle.\n" +
				"Rotation:\tModify rotation with handle.\n" +
				"Group:\t\tModify all points in group."
				), EditorStyles.boldLabel);
			drawType = (DrawType) GUILayout.Toolbar((int) drawType, System.Enum.GetNames(typeof(DrawType)));
			if (EditorGUI.EndChangeCheck())
			{
				handleRotation1 = Quaternion.LookRotation(normal1.sqrMagnitude == 0 ? Vector3.up : normal1);
				handleRotation2 = Quaternion.LookRotation(normal2.sqrMagnitude == 0 ? Vector3.up : normal2);
				handleRotation3 = Quaternion.LookRotation(normal3.sqrMagnitude == 0 ? Vector3.up : normal3);
				handleRotation4 = Quaternion.LookRotation(normal4.sqrMagnitude == 0 ? Vector3.up : normal4);
			}
		}

		private void DropAreaGUI()
		{
			GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
			{
				alignment = TextAnchor.MiddleCenter,
				font = EditorStyles.miniFont,
				fontSize = 12
			};

			GUILayout.Box("--->   Drag Game Objects HERE   <---", boxStyle, GUILayout.Height(50), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
			Rect drop_area = GUILayoutUtility.GetLastRect();

			switch (Event.current.type)
			{
				case EventType.DragUpdated:
					if (!drop_area.Contains(Event.current.mousePosition))
						return;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
					break;

				case EventType.DragPerform:
					if (!drop_area.Contains(Event.current.mousePosition))
						return;

					Undo.RecordObject(this, "Props Placement Tool Inspector");
					propsCollection.AddRange(DragAndDrop.objectReferences.Where(_ => _ is GameObject && (_ as GameObject).scene.rootCount == 0).Select(_ => new PropsObject(_ as GameObject, true)));
					DragAndDrop.AcceptDrag();
					Event.current.Use();
					DeletePreviewPropsObjects();
					CalculateControlProps();
					Repaint();
					SceneView.RepaintAll();
					break;
			}
		}

		private void CollectionListGUI()
		{
			if (propsCollection.Count == 0)
				return;

			GUIStyle numberStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleCenter
			};

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleLeft
			};

			EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(123));
			collectionScroll = EditorGUILayout.BeginScrollView(collectionScroll);
			EditorGUILayout.Space(5, false);

			EditorGUI.BeginChangeCheck();
			for (int i = 0; i < propsCollection.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();

				bool _useage = EditorGUILayout.Toggle("", propsCollection[i].usage, GUILayout.Width(16), GUILayout.Height(32), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
				if (_useage != propsCollection[i].usage)
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					propsCollection[i].usage = _useage;
				}
				EditorGUILayout.LabelField((i + 1).ToString("#0"), numberStyle, GUILayout.Width(32), GUILayout.Height(32), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
				Texture2D previewTex = AssetPreview.GetAssetPreview(propsCollection[i].prefab);
				if (previewTex != null)
				{
					EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(32, 32, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)), previewTex);
				}
				else
				{
					EditorGUILayout.Space(32, false);
				}
				EditorGUILayout.LabelField(propsCollection[i].prefab.name, labelStyle, GUILayout.Width(32), GUILayout.Height(32), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));

				if (i != 0)
				{
					if (GUILayout.Button("↑", GUILayout.Width(32), GUILayout.Height(32), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
					{
						Undo.RecordObject(this, "Props Placement Tool Inspector");
						propsCollection.Reverse(i - 1, 2);
					}
				}

				if (i != propsCollection.Count - 1)
				{
					if (GUILayout.Button("↓", GUILayout.Width(32), GUILayout.Height(32), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
					{
						Undo.RecordObject(this, "Props Placement Tool Inspector");
						propsCollection.Reverse(i, 2);
					}
				}
				else
				{
					EditorGUILayout.Space(32, false);
				}

				if (GUILayout.Button("X", GUILayout.Width(32), GUILayout.Height(32), GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false)))
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					propsCollection.RemoveAt(i);
					break;
				}
				EditorGUILayout.Space(5, false);

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(5, false);
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

			if (GUILayout.Button("Remove All"))
			{
				Undo.RecordObject(this, "Props Placement Tool Inspector");
				propsCollection = new List<PropsObject>();
			}

			if (EditorGUI.EndChangeCheck())
			{
				DeletePreviewPropsObjects();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void PlacementAreaGUI()
		{
			if (pattern == PatternType.Point)
				return;

			EditorGUI.BeginChangeCheck();

			//Order
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label(new GUIContent("Order",
				"Set the objects ordering logic.\n\n" +
				"Random:\tPlace the objects in random order.\n" +
				"Noise:\t\tPlace the objects by perlin-noise.\n" +
				"Sequential:\tPlace the objects in sequential."
				), EditorStyles.boldLabel);
			OrderType _orderPlace = (OrderType) GUILayout.Toolbar((int) orderPlace, System.Enum.GetNames(typeof(OrderType)));
			int _seedPlace = orderPlace != OrderType.Sequential ? EditorGUILayout.IntSlider(new GUIContent("Seed", "Set the seed of order randomization."), seedPlace, 0, 999) : seedPlace;
			float _scalePlace = orderPlace == OrderType.Noise ? EditorGUILayout.FloatField(new GUIContent("Scale", "Set the scale of perlin-noise."), scalePlace) : scalePlace;

			//Countment
			EditorGUILayout.Space(10);
			GUILayout.Label(new GUIContent("Countment",
				"Set the objects counting logic.\n\n" +
				"Count:\tPlace the objects by count.\n" +
				"Delta:\tPlace the objects by distance."
				), EditorStyles.boldLabel);
			CountmentType _countment = (CountmentType) GUILayout.Toolbar((int) countment, System.Enum.GetNames(typeof(CountmentType)));
			int _countX = countX;
			int _countY = countY;
			float _deltaX = deltaX;
			float _deltaY = deltaY;
			bool _normalizeCurvePosition = normalizeCurvePosition;
			bool _pointingCurveRotation = pointingCurveRotation;
			if (pattern == PatternType.Grid)
			{
				if (_countment == CountmentType.Count)
				{
					_countX = EditorGUILayout.IntField(new GUIContent("Count X", "Set the count of placement along first axis."), countX);
					_countY = EditorGUILayout.IntField(new GUIContent("Count Y", "Set the count of placement along second axis."), countY);
				}
				else
				{
					_deltaX = EditorGUILayout.FloatField(new GUIContent("Distance X", "Set the distance of placement along first axis."), deltaX);
					_deltaY = EditorGUILayout.FloatField(new GUIContent("Distance Y", "Set the distance of placement along second axis."), deltaY);
				}
			}
			else
			{
				if (_countment == CountmentType.Count)
				{
					_countX = EditorGUILayout.IntField(new GUIContent("Count", "Set the count of placement."), countX);
					if (pattern == PatternType.Curve)
					{
						_normalizeCurvePosition = EditorGUILayout.Toggle(new GUIContent("Normalize Positions", "Normalize the distance of curved placement."), normalizeCurvePosition);
					}
				}
				else
				{
					_deltaX = EditorGUILayout.FloatField(new GUIContent("Distance", "Set the distance of placement."), deltaX);
				}

				if (pattern == PatternType.Circle || pattern == PatternType.Curve)
				{
					_pointingCurveRotation = EditorGUILayout.Toggle(new GUIContent("Pointing Rotations", "Point the object forward toward the next object."), pointingCurveRotation);
				}
			}

			//Fillment
			EditorGUILayout.Space(10);
			GUILayout.Label(new GUIContent("Fillment",
				"Set the area fillment logic.\n\n" +
				"Random:\tFill the area by random.\n" +
				"Noise:\t\tFill the area by perlin-noise.\n" +
				"Sequential:\tFill the area in order."
				), EditorStyles.boldLabel);
			OrderType _orderFill = (OrderType) GUILayout.Toolbar((int) orderFill, System.Enum.GetNames(typeof(OrderType)));
			float _fillRate = EditorGUILayout.Slider(new GUIContent("Fill Rate", "Set the rate of placement."), fillRate, 0.0f, 1.0f);
			int _seedFill = orderFill != OrderType.Sequential ? EditorGUILayout.IntSlider(new GUIContent("Seed", "Set the seed of fill randomization."), seedFill, 0, 999) : seedFill;
			float _scale = orderFill == OrderType.Noise ? EditorGUILayout.FloatField(new GUIContent("Scale", "Set the scale of perlin-noise."), scaleFill) : scaleFill;

			//Border
			EditorGUILayout.Space(10);
			GUILayout.Label(new GUIContent("Border",
				"Set the border of placement.\n\n" +
				"First:\tPlace objects in the first position.\n" +
				"Middle:\tPlace objects in middle positions.\n" +
				"Last:\tPlace objects in the last position."
				), EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			bool _useFirst = GUILayout.Toggle(useFirst, "First", new GUIStyle("Button"));
			bool _useMiddle = GUILayout.Toggle(useMiddle, "Middle", new GUIStyle("Button"));
			bool _useLast = GUILayout.Toggle(useLast, "Last", new GUIStyle("Button"));
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Props Placement Tool Inspector");
				orderPlace = _orderPlace;
				seedPlace = _seedPlace;
				scalePlace = _scalePlace;
				countment = _countment;
				countX = Mathf.Max(_countX, 2);
				countY = Mathf.Max(_countY, 2);
				deltaX = Mathf.Max(_deltaX, 0.1f);
				deltaY = Mathf.Max(_deltaY, 0.1f);
				normalizeCurvePosition = _normalizeCurvePosition;
				pointingCurveRotation = _pointingCurveRotation;
				orderFill = _orderFill;
				fillRate = _fillRate;
				seedFill = _seedFill;
				scaleFill = _scale;
				useFirst = _useFirst;
				useMiddle = _useMiddle;
				useLast = _useLast;
				DeletePreviewPropsObjects();
				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void PlacementAreaCommonGUI()
		{
			EditorGUI.BeginChangeCheck();

			//Upward
			EditorGUILayout.Space(10);
			GUILayout.Label(new GUIContent("Upward",
				"Set the objects' rotation logic.\n\n" +
				"Surface:\tPlace the objects with surface normal rotation.\n" +
				"World:\tPlace the objects with world rotation."
				), EditorStyles.boldLabel);
			UpwardType _upward = (UpwardType) GUILayout.Toolbar((int) upward, System.Enum.GetNames(typeof(UpwardType)));

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Props Placement Tool Inspector");
				upward = _upward;
				DeletePreviewPropsObjects();
				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void SurfaceAreaGUI()
		{
			if (pattern == PatternType.Point)
				return;

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			foldoutSurface = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutSurface, "Surface");
			if (foldoutSurface)
			{
				EditorGUI.BeginChangeCheck();
				bool _useSurface = EditorGUILayout.Toggle(new GUIContent("        Place On Surface", "Raycast to surface and adjust the objects transform."), useSurface);
				LayerMask _surfaceMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(new GUIContent("        Surface Mask", "Masking the surface adjust."), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceMask), InternalEditorUtility.layers));
				float _surfaceDistance = EditorGUILayout.FloatField(new GUIContent("        Surface Distance", "Limit the distance of surface adjust."), surfaceDistance);
				bool _surfaceOverridePos = EditorGUILayout.Toggle(new GUIContent("        Adjust Position", "Adjust the objects position to surface."), surfaceOverridePos);
				bool _surfaceOverrideRot = EditorGUILayout.Toggle(new GUIContent("        Adjust Rotation", "Adjust the objects rotation to surface."), surfaceOverrideRot);
				LayerMask _surfaceOverrideActivity = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(new GUIContent("        Remove On Layer", "Remove the objects on this layer."), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(surfaceOverrideActivity), InternalEditorUtility.layers));
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					useSurface = _useSurface;
					surfaceMask = _surfaceMask & ~(1 << 2);
					surfaceDistance = _surfaceDistance;
					surfaceOverridePos = _surfaceOverridePos;
					surfaceOverrideRot = _surfaceOverrideRot;
					surfaceOverrideActivity = _surfaceOverrideActivity & ~(1 << 2);
					CalculateControlProps();
					Repaint();
					SceneView.RepaintAll();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private void OffsetAreaGUI()
		{
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			foldoutOffset = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutOffset, "Offset");
			if (foldoutOffset)
			{
				EditorGUI.BeginChangeCheck();
				Vector3 _offsetPos = EditorGUILayout.Vector3Field("        Position", offsetPos);
				Vector3 _offsetRot = EditorGUILayout.Vector3Field("        Rotation", offsetRot);
				Vector3 _offsetScl;
				if (uniformOffsetScl)
				{
					EditorGUILayout.LabelField("        Scale");
					_offsetScl = new Vector3(EditorGUILayout.FloatField("        X | Y | Z", offsetScl.x), offsetScl.y, offsetScl.z);
				}
				else
				{
					_offsetScl = EditorGUILayout.Vector3Field("        Scale", offsetScl);
				}
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button("Reset", GUILayout.MaxWidth(200)))
				{
					_offsetPos = Vector3.zero;
					_offsetRot = Vector3.zero;
					_offsetScl = Vector3.zero;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				bool _uniformOffsetScl = GUILayout.Toggle(uniformOffsetScl, "Uniform Scale: " + (uniformOffsetScl ? "On" : "Off"), new GUIStyle("Button"), GUILayout.MaxWidth(200));
				EditorGUILayout.EndHorizontal();
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					offsetPos = _offsetPos;
					offsetRot = _offsetRot;
					offsetScl = _offsetScl;
					uniformOffsetScl = _uniformOffsetScl;
					CalculateControlProps();
					Repaint();
					SceneView.RepaintAll();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private void JitterAreaGUI()
		{
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			foldoutJitter = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutJitter, "Jitter");
			if (foldoutJitter)
			{
				EditorGUI.BeginChangeCheck();
				Vector3 _jitterPos = EditorGUILayout.Vector3Field("        Position", jitterPos);
				Vector3 _jitterRot = EditorGUILayout.Vector3Field("        Rotation", jitterRot);
				Vector3 _jitterScl;
				if (uniformJitterScl)
				{
					EditorGUILayout.LabelField("        Scale");
					_jitterScl = new Vector3(EditorGUILayout.FloatField("        X | Y | Z", jitterScl.x), jitterScl.y, jitterScl.z);
				}
				else
				{
					_jitterScl = EditorGUILayout.Vector3Field("        Scale", jitterScl);
				}
				int _seedJitter = pattern != PatternType.Point ? EditorGUILayout.IntSlider(new GUIContent("        Seed", "Set the seed of jitter."), seedJitter, 0, 999) : seedJitter;
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button("Reset", GUILayout.MaxWidth(200)))
				{
					_jitterPos = Vector3.zero;
					_jitterRot = Vector3.zero;
					_jitterScl = Vector3.zero;
					_seedJitter = 123;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				bool _uniformJitterScl = GUILayout.Toggle(uniformJitterScl, "Uniform Scale: " + (uniformJitterScl ? "On" : "Off"), new GUIStyle("Button"), GUILayout.MaxWidth(200));
				EditorGUILayout.EndHorizontal();
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					jitterPos = _jitterPos;
					jitterRot = _jitterRot;
					jitterScl = _jitterScl;
					seedJitter = _seedJitter;
					uniformJitterScl = _uniformJitterScl;
					CalculateControlProps();
					Repaint();
					SceneView.RepaintAll();
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		private void SettingsAreaGUI()
		{
			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			foldoutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutSettings, "Settings");
			if (foldoutSettings)
			{
				EditorGUI.BeginChangeCheck();
				LayerMask _drawMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(EditorGUILayout.MaskField(new GUIContent("        Draw Mask", "Masking the control point selection."), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(drawMask), InternalEditorUtility.layers));
				float _drawDistance = EditorGUILayout.FloatField(new GUIContent("        Draw Distance", "Limit the distance of control point selection."), drawDistance);
				float _gizmoScale = EditorGUILayout.Slider(new GUIContent("        Gizmo Scale", "Set scale of gizmos."), gizmoScale, gizmoScaleMin, gizmoScaleMax);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Props Placement Tool Inspector");
					drawMask = _drawMask & ~(1 << 2);
					drawDistance = Mathf.Max(_drawDistance, 1.0f);
					gizmoScale = _gizmoScale;
					Repaint();
					SceneView.RepaintAll();
				}
				EditorGUILayout.Space(10);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				if (GUILayout.Button("Save", GUILayout.Width(60)))
				{
					SaveToolData();
				}
				if (GUILayout.Button("Load", GUILayout.Width(60)))
				{
					LoadToolData();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(20);
				EditorGUILayout.LabelField("        Version: " + version);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}
		#endregion

		#region CONTROL_POINTS
		private void DrawControlPoints(ref RaycastHit hit)
		{
			Handles.color = new Color(1.0f, 0.75f, 0.0f, 0.5f);
			Handles.DrawSolidDisc(hit.point, hit.normal, hit.distance * gizmoScale);

			if (Event.current.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
			}

			if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
			{
				Undo.RecordObject(this, "Draw control point");
				switch (status)
				{
					case DrawStatus.DrawFirst:
						point1 = hit.point;
						normal1 = hit.normal;
						status = DrawStatus.DrawSecond;
						Repaint();
						break;

					case DrawStatus.DrawSecond:
						point2 = hit.point;
						normal2 = hit.normal;
						status = (int) pattern < 2 ? DrawStatus.Ready : DrawStatus.DrawThird;
						Repaint();
						break;

					case DrawStatus.DrawThird:
						point3 = hit.point;
						normal3 = hit.normal;
						status = (int) pattern < 3 || (pattern == PatternType.Grid && countment == CountmentType.Delta) ? DrawStatus.Ready : DrawStatus.DrawForth;
						Repaint();
						break;

					case DrawStatus.DrawForth:
						point4 = hit.point;
						normal4 = hit.normal;
						status = DrawStatus.Ready;
						Repaint();
						break;
				}
				Event.current.Use();

				CalculateControlRates();
				CalculateControlProps();
			}
		}

		private void RepositionControlPoints(ref RaycastHit hit)
		{
			float discRadius = hit.distance * gizmoScale;
			float discRadiusDouble = discRadius * 2.0f;

			if (reposition1 || Vector3.Distance(hit.point, point1) < discRadiusDouble)
			{
				RepositionControlPoint(ref hit, ref point1, ref normal1, ref discRadius, ref reposition1);
				return;
			}

			if (reposition2 || Vector3.Distance(hit.point, point2) < discRadiusDouble)
			{
				RepositionControlPoint(ref hit, ref point2, ref normal2, ref discRadius, ref reposition2);
				return;
			}

			if ((int) pattern < 2)
				return;

			if (reposition3 || Vector3.Distance(hit.point, point3) < discRadiusDouble)
			{
				RepositionControlPoint(ref hit, ref point3, ref normal3, ref discRadius, ref reposition3);
				return;
			}

			if ((int) pattern < 3 || (pattern == PatternType.Grid && countment == CountmentType.Delta))
				return;

			if (reposition4 || Vector3.Distance(hit.point, point4) < discRadiusDouble)
			{
				RepositionControlPoint(ref hit, ref point4, ref normal4, ref discRadius, ref reposition4);
				return;
			}
		}

		private void RepositionControlPoint(ref RaycastHit hit, ref Vector3 point, ref Vector3 normal, ref float radius, ref bool resposition)
		{
			Handles.color = new Color(1.0f, 0.75f, 0.0f, 0.5f);
			Handles.DrawSolidDisc(point, normal, radius);

			if (Event.current.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
			}

			if (Event.current.button == 0 && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
			{
				Undo.RecordObject(this, "Reposition control point");
				point = hit.point;
				normal = hit.normal;
				resposition = true;
				Event.current.Use();

				Repaint();
			}

			if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
			{
				resposition = false;
				Event.current.Use();

				CalculateControlRates();
				CalculateControlProps();
			}
		}

		private void PositionWithHandle()
		{
			EditorGUI.BeginChangeCheck();

			Vector3 newPoint1 = point1;
			Vector3 newPoint2 = point2;
			Vector3 newPoint3 = point3;
			Vector3 newPoint4 = point4;

			newPoint1 = Handles.PositionHandle(newPoint1, Quaternion.identity);
			newPoint2 = Handles.PositionHandle(newPoint2, Quaternion.identity);
			if ((int) pattern > 1)
			{
				newPoint3 = Handles.PositionHandle(newPoint3, Quaternion.identity);
				if ((int) pattern > 2 && countment == CountmentType.Count)
				{
					newPoint4 = Handles.PositionHandle(newPoint4, Quaternion.identity);
				}
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Reposition control point");
				point1 = newPoint1;
				point2 = newPoint2;
				point3 = newPoint3;
				point4 = newPoint4;

				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void RotationWithHandle()
		{
			EditorGUI.BeginChangeCheck();

			handleRotation1 = Handles.RotationHandle(handleRotation1, point1);
			handleRotation2 = Handles.RotationHandle(handleRotation2, point2);
			if ((int) pattern > 1)
			{
				handleRotation3 = Handles.RotationHandle(handleRotation3, point3);
				if ((int) pattern > 2 && countment == CountmentType.Count)
				{
					handleRotation4 = Handles.RotationHandle(handleRotation4, point4);
				}
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Reposition control point normal");
				normal1 = handleRotation1 * Vector3.forward;
				normal2 = handleRotation2 * Vector3.forward;
				normal3 = handleRotation3 * Vector3.forward;
				normal4 = handleRotation4 * Vector3.forward;

				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void GroupWithHandle()
		{
			EditorGUI.BeginChangeCheck();

			Vector3 delta = Handles.PositionHandle(point1, Quaternion.identity) - point1;

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "Reposition control point");
				point1 += delta;
				point2 += delta;
				point3 += delta;
				point4 += delta;

				CalculateControlRates();
				CalculateControlProps();
				Repaint();
				SceneView.RepaintAll();
			}
		}

		private void ClearControlPoints()
		{
			point1 = Vector3.zero;
			point2 = Vector3.zero;
			point3 = Vector3.zero;
			point4 = Vector3.zero;
			pointX = Vector3.zero;
			normal1 = Vector3.up;
			normal2 = Vector3.up;
			normal3 = Vector3.up;
			normal4 = Vector3.up;
			normalX = Vector3.up;

			DeletePreviewPropsObjects();
			controlProps = null;

			initControl = false;
		}
		#endregion

		#region CREATE_DATA
		private void CalculateControlRates()
		{
			if (propsCollection == null)
			{
				propsCollection = new List<PropsObject>();
			}

			if (pattern == PatternType.Point)
			{
				controlRatesX = new float[1];
				controlRatesY = new float[0];
				controlRatesX[0] = 0.0f;
				return;
			}

			CalculateControlRatesX();

			if (pattern != PatternType.Grid)
				return;

			CalculateControlRatesY();
		}

		private void CalculateControlRatesX()
		{
			if (pattern == PatternType.Curve && (normalizeCurvePosition || countment == CountmentType.Delta))
			{
				CalculateNormalizeLUT();
			}

			if (countment == CountmentType.Delta)
			{
				countX = Mathf.FloorToInt(LengthOfAxisX() / deltaX) + 1;
			}

			float distance = LengthOfAxisX();
			controlRatesX = new float[countX];
			for (int i = 0; i < countX; i++)
			{
				if (countment == CountmentType.Count)
				{
					controlRatesX[i] = i / (float) (pattern == PatternType.Circle ? countX : countX - 1);
				}
				else
				{
					controlRatesX[i] = i * deltaX / distance;
				}

				if (pattern == PatternType.Curve && (normalizeCurvePosition || countment == CountmentType.Delta))
				{
					controlRatesX[i] = GetRateOnNormalizeLUT(controlRatesX[i] * distance);
				}
			}
		}

		private void CalculateControlRatesY()
		{
			if (countment == CountmentType.Delta)
			{
				countY = Mathf.FloorToInt(LengthOfAxisY() / deltaY) + 1;
			}

			float distance = LengthOfAxisY();
			controlRatesY = new float[countY];
			for (int i = 0; i < countY; i++)
			{
				if (countment == CountmentType.Count)
				{
					controlRatesY[i] = i / (float) (countY - 1);
				}
				else
				{
					controlRatesY[i] = i * deltaY / distance;
				}
			}
		}

		private void CalculateControlProps()
		{
			int allDataCount = controlRatesX.Length;
			if (pattern == PatternType.Grid)
			{
				allDataCount *= controlRatesY.Length;
			}

			if (controlProps == null)
			{
				controlProps = new PropsData[allDataCount];
			}

			if (controlProps.Length < allDataCount)
			{
				System.Array.Resize(ref controlProps, allDataCount);
			}

			if (controlProps.Length > allDataCount)
			{
				for (int i = controlProps.Length - 1; i >= allDataCount; i--)
				{
					if (controlProps[i].go == null)
						continue;

					DestroyImmediate(controlProps[i].go);
				}
				System.Array.Resize(ref controlProps, allDataCount);
			}

			switch (pattern)
			{
				case PatternType.Line:
					CalculateControlPropsLine();
					break;

				case PatternType.Circle:
					CalculateControlPropsCircle();
					break;

				case PatternType.Curve:
					CalculateControlPropsCurve();
					break;

				case PatternType.Grid:
					CalculateControlPropsGrid();
					break;

				case PatternType.Point:
					CalculateControlPropsPoint();
					break;
			}
			ApplySurfaceAdjust();
			ApplyOffset();
			ApplyRandoms();

			PreviewProps();
		}

		private void CalculateControlPropsLine()
		{
			Vector3 dir = (point2 - point1).normalized;

			for (int x = 0; x < controlRatesX.Length; x++)
			{
				controlProps[x].coord = new Vector2(controlRatesX[x], 0.0f);
				controlProps[x].pos = Vector3.Lerp(point1, point2, controlRatesX[x]);
				SetRotationVectors(dir, Vector3.Lerp(normal1, normal2, controlRatesX[x]), ref controlProps[x]);
				controlProps[x].scl = Vector3.one;
				controlProps[x].active = IsFillmentActive(x, controlRatesX.Length);
			}
		}

		private void CalculateControlPropsCircle()
		{
			Vector3 line12 = (point2 - point1).normalized;
			Vector3 line12X = Vector3.Cross(normal1, line12);
			Vector3 circleNormal = Vector3.Cross(line12, line12X);
			float circleRadius = (point2 - point1).magnitude;

			for (int x = 0; x < controlRatesX.Length; x++)
			{
				float rad = controlRatesX[x] * 2.0f * Mathf.PI;
				float onCircleX = Mathf.Cos(rad) * circleRadius;
				float onCircleY = Mathf.Sin(rad) * circleRadius;
				Vector3 onCircleXY = line12 * onCircleX + line12X * onCircleY;

				controlProps[x].coord = new Vector2(controlRatesX[x], 0.0f);
				controlProps[x].pos = point1 + onCircleXY;
				SetRotationVectors(Vector3.Cross(circleNormal, onCircleXY).normalized, circleNormal, ref controlProps[x]);
				controlProps[x].scl = Vector3.one;
				controlProps[x].active = IsFillmentActive(x, controlRatesX.Length);
			}

			if (!pointingCurveRotation)
				return;

			for (int x = 0; x < controlRatesX.Length; x++)
			{
				if (x == controlRatesX.Length - 1)
				{
					controlProps[x].fwd = (controlProps[0].pos - controlProps[x].pos).normalized;
					continue;
				}

				controlProps[x].fwd = (controlProps[x + 1].pos - controlProps[x].pos).normalized;
			}
		}

		private void CalculateControlPropsCurve()
		{
			for (int x = 0; x < controlRatesX.Length; x++)
			{
				Vector3 pA = Vector3.Lerp(point1, point2, controlRatesX[x]);
				Vector3 pB = Vector3.Lerp(point2, point3, controlRatesX[x]);
				Vector3 nA = Vector3.Lerp(normal1, normal2, controlRatesX[x]);
				Vector3 nB = Vector3.Lerp(normal2, normal3, controlRatesX[x]);

				controlProps[x].coord = new Vector2(controlRatesX[x], 0.0f);
				controlProps[x].pos = Vector3.Lerp(pA, pB, controlRatesX[x]);
				SetRotationVectors((pB - pA).normalized, Vector3.Lerp(nA, nB, controlRatesX[x]), ref controlProps[x]);
				controlProps[x].scl = Vector3.one;
				controlProps[x].active = IsFillmentActive(x, controlRatesX.Length); ;
			}

			if (!pointingCurveRotation)
				return;

			for (int x = 0; x < controlRatesX.Length; x++)
			{
				if (x == controlRatesX.Length - 1)
					continue;

				controlProps[x].fwd = (controlProps[x + 1].pos - controlProps[x].pos).normalized;
			}
		}

		private void CalculateControlPropsGrid()
		{
			Vector3 dir = (point2 - point1).normalized;
			Vector3 v12 = point2 - point1;
			Vector3 v23 = point3 - point2;

			for (int x = 0; x < controlRatesX.Length; x++)
			{
				Vector3 pA = Vector3.Lerp(point1, point2, controlRatesX[x]);
				Vector3 pB = Vector3.Lerp(point4, point3, controlRatesX[x]);
				Vector3 nA = Vector3.Lerp(normal1, normal2, controlRatesX[x]);
				Vector3 nB = Vector3.Lerp(normal4, normal3, controlRatesX[x]);
				for (int y = 0; y < controlRatesY.Length; y++)
				{
					int xy = x * controlRatesY.Length + y;
					controlProps[xy].coord = new Vector2(controlRatesX[x], controlRatesY[y]);
					if (countment == CountmentType.Count)
					{
						controlProps[xy].pos = Vector3.Lerp(pA, pB, controlRatesY[y]);
					}
					else
					{
						controlProps[xy].pos = point1 + (controlRatesX[x] * v12.magnitude * v12.normalized) + (controlRatesY[y] * v23.magnitude * v23.normalized);
					}
					SetRotationVectors(dir, Vector3.Lerp(nA, nB, controlRatesY[y]), ref controlProps[xy]);
					controlProps[xy].scl = Vector3.one;
					controlProps[xy].active = IsFillmentActive(x, y, controlRatesX.Length, controlRatesY.Length);
				}
			}
		}

		private void CalculateControlPropsPoint()
		{
			Quaternion rotator = Quaternion.FromToRotation(Vector3.up, normalX);

			controlProps[0].coord = Vector2.zero;
			controlProps[0].pos = pointX;
			SetRotationVectors(rotator * Vector3.forward, normalX, ref controlProps[0]);
			controlProps[0].scl = Vector3.one;
			controlProps[0].active = true;
		}
		#endregion

		#region MODIFY_DATA
		private void ApplySurfaceAdjust()
		{
			if (!useSurface)
				return;

			for (int i = 0; i < controlProps.Length; i++)
			{
				if (!Physics.Raycast(controlProps[i].pos + controlProps[i].uwd * (surfaceDistance * 0.5f), -controlProps[i].uwd, out RaycastHit hit, surfaceDistance, surfaceMask))
					continue;

				if (surfaceOverridePos)
				{
					controlProps[i].pos = hit.point;
				}

				if (surfaceOverrideRot)
				{
					Quaternion rotator = Quaternion.FromToRotation(controlProps[i].uwd, hit.normal);
					controlProps[i].fwd = rotator * controlProps[i].fwd;
					controlProps[i].uwd = hit.normal;
				}

				if (surfaceOverrideActivity != 0)
				{
					controlProps[i].active &= !IsContainsLayer(surfaceOverrideActivity, hit.collider.gameObject.layer);
				}
			}
		}

		private void ApplyOffset()
		{
			for (int i = 0; i < controlProps.Length; i++)
			{
				controlProps[i].ApplyOffset(
					offsetPos,
					offsetRot,
					uniformOffsetScl ?
						offsetScl.x * Vector3.one :
						offsetScl
				);
			}
		}

		private void ApplyRandoms()
		{
			Random.State lastState = Random.state;

			Random.InitState(GetRandomSeedJitter());
			for (int i = 0; i < controlProps.Length; i++)
			{
				controlProps[i].ApplyOffset(
					new Vector3(Random.Range(-jitterPos.x, jitterPos.x), Random.Range(-jitterPos.y, jitterPos.y), Random.Range(-jitterPos.z, jitterPos.z)),
					new Vector3(Random.Range(-jitterRot.x, jitterRot.x), Random.Range(-jitterRot.y, jitterRot.y), Random.Range(-jitterRot.z, jitterRot.z)),
					uniformJitterScl ?
						Random.Range(-jitterScl.x, jitterScl.x) * Vector3.one :
						new Vector3(Random.Range(-jitterScl.x, jitterScl.x), Random.Range(-jitterScl.y, jitterScl.y), Random.Range(-jitterScl.z, jitterScl.z))
				);
			}

			Random.InitState(GetRandomSeedFill());
			for (int i = 0; i < controlProps.Length; i++)
			{
				controlProps[i].active &= GetFillResult(i);
			}

			Random.InitState(GetRandomSeedPlace());
			for (int i = 0; i < controlProps.Length; i++)
			{
				List<int> availablePropsIndex = propsCollection.Where(_ => _.usage).Select(_ => propsCollection.IndexOf(_)).ToList();
				if (availablePropsIndex.Count == 0)
				{
					availablePropsIndex.Add(-1);
				}

				controlProps[i].propsIndex = GetObjectIndex(ref availablePropsIndex, i);
			}

			Random.state = lastState;
		}
		#endregion

		#region PREVIEW
		private void PreviewProps()
		{
			for (int i = 0; i < controlProps.Length; i++)
			{
				if (!usePreview || status != DrawStatus.Ready)
				{
					if (controlProps[i].go)
					{
						controlProps[i].go.SetActive(false);
					}
					continue;
				}

				if (propsCollection == null || propsCollection.Count == 0 || controlProps[i].propsIndex == -1 || propsCollection[controlProps[i].propsIndex].prefab == null)
					continue;

				GameObject newObject;

				if (controlProps[i].go == null)
				{
					newObject = Instantiate(propsCollection[controlProps[i].propsIndex].prefab);
					newObject.hideFlags = HideFlags.HideAndDontSave;
				}
				else
				{
					newObject = controlProps[i].go;
				}

				if (newObject == null)
					continue;

				newObject.transform.localPosition = controlProps[i].pos;
				newObject.transform.localRotation = Quaternion.LookRotation(controlProps[i].fwd, controlProps[i].uwd);
				newObject.transform.localScale = controlProps[i].scl;
				foreach (Transform t in newObject.GetComponentsInChildren<Transform>(false))
				{
					t.gameObject.layer = 2;//Ignore raycast layer
				}
				newObject.SetActive(controlProps[i].active);

				controlProps[i].go = newObject;
			}
		}

		private void DeletePreviewPropsObjects()
		{
			if (controlProps == null)
				return;

			for (int i = 0; i < controlProps.Length; i++)
			{
				if (controlProps[i].go == null)
					continue;

				DestroyImmediate(controlProps[i].go);
			}
		}
		#endregion

		#region PLACEMENT
		private void PlaceProps()
		{
			if (controlProps == null || propsCollection == null || propsCollection.Count == 0)
				return;

			for (int i = 0; i < controlProps.Length; i++)
			{
				if (!controlProps[i].active || controlProps[i].propsIndex == -1 || propsCollection[controlProps[i].propsIndex].prefab == null)
					continue;

				GameObject newObject;
				PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(propsCollection[controlProps[i].propsIndex].prefab);

				if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
				{
					newObject = (GameObject) PrefabUtility.InstantiatePrefab(propsCollection[controlProps[i].propsIndex].prefab);
				}
				else
				{
					newObject = Instantiate(propsCollection[controlProps[i].propsIndex].prefab);
				}

				if (newObject == null)
				{
					Debug.LogError("Error instantiating prefab");
					return;
				}

				Undo.RegisterCreatedObjectUndo(newObject, "Place Props");
				newObject.transform.localPosition = controlProps[i].pos;
				newObject.transform.localRotation = Quaternion.LookRotation(controlProps[i].fwd, controlProps[i].uwd);
				newObject.transform.localScale = controlProps[i].scl;
				newObject.transform.parent = propsParent;
			}
		}

		private void PlaceByCursor(ref RaycastHit hit)
		{
			pointX = hit.point;
			normalX = hit.normal;

			DeletePreviewPropsObjects();
			CalculateControlRates();
			CalculateControlProps();

			if (Event.current.type == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
			}

			if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
			{
				Event.current.Use();
				PlaceProps();
			}
		}
		#endregion

		#region GIZMOS
		private void DrawGizmos(Vector3 viewPos)
		{
			if (status == DrawStatus.None && pattern != PatternType.Point)
				return;

			switch (pattern)
			{
				case PatternType.Line:
					DrawGizmosLine(Mathf.Min(Vector3.Distance(viewPos, point1), Vector3.Distance(viewPos, point2)) * gizmoScale);
					break;

				case PatternType.Circle:
					DrawGizmosCircle(Mathf.Min(Vector3.Distance(viewPos, point1), Vector3.Distance(viewPos, point2)) * gizmoScale);
					break;

				case PatternType.Curve:
					DrawGizmosCurve(Mathf.Min(Vector3.Distance(viewPos, point1), Vector3.Distance(viewPos, point2), Vector3.Distance(viewPos, point3)) * gizmoScale);
					break;

				case PatternType.Grid:
					DrawGizmosGrid(Mathf.Min(Vector3.Distance(viewPos, point1), Vector3.Distance(viewPos, point2), Vector3.Distance(viewPos, point3), Vector3.Distance(viewPos, point4)) * gizmoScale);
					break;

				case PatternType.Point:
					DrawGizmosPropsPreview(Vector3.Distance(viewPos, pointX) * gizmoScale);
					break;
			}
		}

		private void DrawGizmosLine(float gizmoRadius)
		{
			Vector3 line12 = (point2 - point1).normalized;

			Handles.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
			Handles.DrawWireDisc(point1, normal1, gizmoRadius, gizmoThickness);
			if (normal2 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point2, normal2, gizmoRadius, gizmoThickness);

			Handles.DrawLine(point1 + line12 * gizmoRadius, point2 - line12 * gizmoRadius, gizmoThickness);

			DrawGizmosPropsPreview(gizmoRadius);
		}

		private void DrawGizmosCircle(float gizmoRadius)
		{
			Vector3 line12 = (point2 - point1).normalized;

			Handles.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
			Handles.DrawWireDisc(point1, normal1, gizmoRadius, gizmoThickness);
			if (normal2 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point2, normal2, gizmoRadius, gizmoThickness);

			Handles.DrawWireDisc(point1, Vector3.Cross(line12, Vector3.Cross(line12, normal1)), (point2 - point1).magnitude, gizmoThickness);
			Handles.DrawLine(point1 + line12 * gizmoRadius, point2 - line12 * gizmoRadius, gizmoThickness);

			DrawGizmosPropsPreview(gizmoRadius);
		}

		private void DrawGizmosCurve(float gizmoRadius)
		{
			Vector3 line12 = (point2 - point1).normalized;
			Vector3 line23 = (point3 - point2).normalized;

			Handles.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
			Handles.DrawWireDisc(point1, normal1, gizmoRadius, gizmoThickness);
			if (normal2 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point2, normal2, gizmoRadius, gizmoThickness);
			if (normal3 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point3, normal3, gizmoRadius, gizmoThickness);

			Handles.DrawLine(point1 + line12 * gizmoRadius, point2 - line12 * gizmoRadius, gizmoThickness);
			Handles.DrawLine(point2 + line23 * gizmoRadius, point3 - line23 * gizmoRadius, gizmoThickness);

			DrawGizmosPropsPreview(gizmoRadius);
		}

		private void DrawGizmosGrid(float gizmoRadius)
		{
			Vector3 line12 = (point2 - point1).normalized;
			Vector3 line23 = (point3 - point2).normalized;
			Vector3 line34 = (point4 - point3).normalized;
			Vector3 line41 = (point1 - point4).normalized;

			Handles.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
			Handles.DrawWireDisc(point1, normal1, gizmoRadius, gizmoThickness);
			if (normal2 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point2, normal2, gizmoRadius, gizmoThickness);
			if (normal3 == Vector3.zero)
				return;
			Handles.DrawWireDisc(point3, normal3, gizmoRadius, gizmoThickness);
			if (normal4 == Vector3.zero)
				return;
			if (countment == CountmentType.Count)
			{
				Handles.DrawWireDisc(point4, normal4, gizmoRadius, gizmoThickness);
			}

			Handles.DrawLine(point1 + line12 * gizmoRadius, point2 - line12 * gizmoRadius, gizmoThickness);
			Handles.DrawLine(point2 + line23 * gizmoRadius, point3 - line23 * gizmoRadius, gizmoThickness);
			if (countment == CountmentType.Count)
			{
				Handles.DrawLine(point3 + line34 * gizmoRadius, point4 - line34 * gizmoRadius, gizmoThickness);
				Handles.DrawLine(point4 + line41 * gizmoRadius, point1 - line41 * gizmoRadius, gizmoThickness);
			}

			DrawGizmosPropsPreview(gizmoRadius);
		}

		private void DrawGizmosPropsPreview(float gizmoRadius)
		{
			if (controlProps == null || usePreview)
				return;

			Handles.color = new Color(0.5f, 0.25f, 0.0f, 0.5f);
			for (int i = 0; i < controlProps.Length; i++)
			{
				if (!controlProps[i].active)
					continue;

				Handles.DrawSolidDisc(controlProps[i].pos, controlProps[i].uwd, gizmoRadius * controlProps[i].scl.x);
			}
		}
		#endregion

		#region UTILITIES

		private void SaveToolData()
		{
			string path = EditorUtility.SaveFilePanelInProject("Save Tool Settings", $"ppt_data_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}", "json", "Enter a file name to save the settings.");

			if (string.IsNullOrEmpty(path))
				return;

			string json = EditorJsonUtility.ToJson(this, true);
			System.IO.File.WriteAllText(path, json);

			AssetDatabase.Refresh();
		}

		private void LoadToolData()
		{
			string path = EditorUtility.OpenFilePanelWithFilters("Load Tool Settings", Application.dataPath, new string[] { "JSON files", "json", "All files", "*" });

			if (string.IsNullOrEmpty(path))
				return;

			Undo.RecordObject(this, "Props Placement Tool Inspector");

			string json = System.IO.File.ReadAllText(path);
			EditorJsonUtility.FromJsonOverwrite(json, this);

			DeletePreviewPropsObjects();
			CalculateControlRates();
			CalculateControlProps();
			Repaint();
			SceneView.RepaintAll();
		}

		private int GetRandomSeedJitter()
		{
			if (pattern == PatternType.Point)
				return seedJitter + Mathf.RoundToInt(pointX.sqrMagnitude);

			return seedJitter;
		}

		private int GetRandomSeedPlace()
		{
			return seedPlace;
		}

		private int GetRandomSeedFill()
		{
			return seedFill;
		}

		private float GetNoisePlace(Vector2 sampleCoord)
		{
			float offset = seedPlace / 100.0f;
			sampleCoord *= scalePlace;
			return Mathf.PerlinNoise(offset + sampleCoord.x, offset + sampleCoord.y);
		}

		private float GetNoiseFill(Vector2 sampleCoord)
		{
			float offset = seedFill / 100.0f;
			sampleCoord *= scaleFill;
			return Mathf.PerlinNoise(offset + sampleCoord.x, offset + sampleCoord.y);
		}

		private float GetFillRate()
		{
			if (pattern == PatternType.Point)
				return 1.0f;

			return fillRate;
		}

		private float LengthOfAxisX()
		{
			switch (pattern)
			{
				case PatternType.Line:
					return Vector3.Distance(point1, point2);

				case PatternType.Circle:
					return 2.0f * Vector3.Distance(point1, point2) * Mathf.PI;

				case PatternType.Curve:
					if (normalizeCurvePosition || countment == CountmentType.Delta)
					{
						return normalizeLUT[normalizeResolution - 1];
					}
					return Vector3.Distance(point1, point2) + Vector3.Distance(point2, point3);

				case PatternType.Grid:
					return Vector3.Distance(point1, point2);
			}

			return 0.0f;
		}

		private float LengthOfAxisY()
		{
			if (pattern == PatternType.Grid)
				return Vector3.Distance(point2, point3);

			return 0.0f;
		}

		private bool GetFillResult(int propIndex)
		{
			return orderFill switch
			{
				OrderType.Random => Random.value < GetFillRate(),
				OrderType.Noise => GetNoiseFill(controlProps[propIndex].coord) < GetFillRate(),
				OrderType.Sequential => Mathf.InverseLerp(0, controlProps.Length, propIndex) < GetFillRate(),
				_ => false,
			};
		}

		private int GetObjectIndex(ref List<int> availables, int propIndex)
		{
			return orderPlace switch
			{
				OrderType.Random => availables[Random.Range(0, availables.Count)],
				OrderType.Noise => availables[(int) Mathf.Lerp(0, availables.Count * (1 - Mathf.Epsilon), GetNoisePlace(controlProps[propIndex].coord))],
				OrderType.Sequential => availables[(int) Mathf.Repeat(propIndex, availables.Count)],
				_ => availables[0],
			};
		}

		private bool IsFillmentActive(int index, int range)
		{
			if (index == 0)
				return useFirst;

			if (index == range - 1)
				return useLast;

			return useMiddle;
		}

		private bool IsFillmentActive(int indexX, int indexY, int rangeX, int rangeY)
		{
			if (indexX == 0 || indexY == 0)
				return useFirst;

			if (indexX == rangeX - 1 || indexY == rangeY - 1)
				return useLast;

			return useMiddle;
		}

		private void SetRotationVectors(Vector3 fwd, Vector3 uwd, ref PropsData target)
		{
			if (upward == UpwardType.Surface)
			{
				target.fwd = fwd;
				target.uwd = uwd;
				return;
			}

			target.fwd = Vector3.ProjectOnPlane(fwd, Vector3.up);
			target.uwd = Vector3.up;
		}

		private Vector3 GetPositionOnCurve(float rate)
		{
			Vector3 pA = Vector3.Lerp(point1, point2, rate);
			Vector3 pB = Vector3.Lerp(point2, point3, rate);
			return Vector3.Lerp(pA, pB, rate);
		}

		private void CalculateNormalizeLUT()
		{
			normalizeLUT = new float[normalizeResolution];
			for (int i = 0; i < normalizeLUT.Length; i++)
			{
				if (i == 0)
				{
					normalizeLUT[i] = 0.0f;
					continue;
				}

				Vector3 pLast = GetPositionOnCurve((i - 1) / (normalizeResolution - 1.0f));
				Vector3 pCurrent = GetPositionOnCurve(i / (normalizeResolution - 1.0f));

				normalizeLUT[i] = Vector3.Distance(pLast, pCurrent) + normalizeLUT[i - 1];
			}
		}

		private float GetRateOnNormalizeLUT(float distance)
		{
			float arcLength = normalizeLUT[normalizeResolution - 1];
			if (distance >= 0.0f && distance <= arcLength)
			{
				for (int i = 0; i < normalizeLUT.Length - 1; i++)
				{
					if (distance > normalizeLUT[i] && distance < normalizeLUT[i + 1])
					{
						return Mathf.Lerp(i / (normalizeLUT.Length - 1.0f), (i + 1) / (normalizeLUT.Length - 1.0f), Mathf.InverseLerp(normalizeLUT[i], normalizeLUT[i + 1], distance));
					}
				}
			}

			return distance / arcLength;
		}

		public bool IsContainsLayer(LayerMask mask, int layer)
		{
			return (mask & (1 << layer)) != 0;
		}
		#endregion
	}
}