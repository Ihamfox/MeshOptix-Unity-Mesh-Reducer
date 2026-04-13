/*
MIT License

Copyright (c) 2025 Hamed Khalifa

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Brainy.MeshOptix.Simplification;

namespace Brainy.EditorTools
{
    public sealed class SceneMeshOptimizerWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/Project/Prefabs/Optimized_Meshes";
        private const string ToolTitle = "MeshOptix : Scene Mesh Optimizer";
        private static readonly string[] WindowTabs = { "Optimizer", "Credits" };

        [SerializeField] private float targetTriangleRatio = 0.5f;
        [SerializeField] private bool replaceSelection = true;
        [SerializeField] private bool keepDisabledBackup;
        [SerializeField] private bool includeInactiveChildren = true;
        [SerializeField] private bool updateMeshColliders = true;
        [SerializeField] private bool preserveUvSeams = true;
        [SerializeField] private bool preserveLightmapUvSeams = true;
        [SerializeField] private bool preserveHardEdges = true;
        [SerializeField] private bool preserveSkinningBoundaries = true;
        [SerializeField] private bool recalculateNormals;
        [SerializeField] private bool recalculateTangents = true;
        [SerializeField] private bool optimizeMeshBuffers = true;
        [SerializeField] private bool autoMakeSourceMeshesReadable = true;
        [SerializeField] private string outputFolder = DefaultOutputFolder;
        [SerializeField] private int selectedTabIndex;
        [SerializeField] private string creditCreatorName = "Hamed Khalifa";
        [SerializeField] private string creditCompanyName = "Hamfox inc";
        [SerializeField] private string creditCopyright = "Copyright (c) 2025 Hamed Khalifa";
        [SerializeField] private string creditLinkedInUrl = "https://www.linkedin.com/in/hamed-khalifa/";
        [SerializeField] private string creditGitHubUrl = "https://github.com/Ihamfox";
        [SerializeField] private bool showAdvancedPreservationOptions;

        private Vector2 windowScrollPosition;
        private Vector2 creditsScrollPosition;
        private Vector2 reportScrollPosition;
        private OptimizationReport lastReport;

        private bool stylesInitialized;
        private bool stylesForProSkin;
        private GUIStyle headerContainerStyle;
        private GUIStyle sectionContainerStyle;
        private GUIStyle windowTitleStyle;
        private GUIStyle windowSubtitleStyle;
        private GUIStyle sectionTitleStyle;
        private GUIStyle sectionSubtitleStyle;
        private GUIStyle subsectionTitleStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle reportEntryStyle;
        private GUIStyle summaryMetricLabelStyle;
        private GUIStyle summaryMetricValueStyle;

        [MenuItem("Tools/Optimization/MeshOptix : Scene Mesh Optimizer")]
        public static void ShowWindow()
        {
            SceneMeshOptimizerWindow window = GetWindow<SceneMeshOptimizerWindow>(ToolTitle);
            window.minSize = new Vector2(480f, 620f);
        }

        [MenuItem("GameObject/Optimization/Open MeshOptix : Scene Mesh Optimizer", false, 49)]
        private static void ShowWindowFromGameObjectMenu()
        {
            ShowWindow();
        }

        [MenuItem("GameObject/Optimization/Open MeshOptix : Scene Mesh Optimizer", true)]
        private static bool ValidateShowWindowFromGameObjectMenu()
        {
            GameObject selectedObject = Selection.activeGameObject;
            return selectedObject != null && !EditorUtility.IsPersistent(selectedObject);
        }

        private void EnsureStyles()
        {
            bool isProSkin = EditorGUIUtility.isProSkin;
            if (stylesInitialized && stylesForProSkin == isProSkin)
            {
                return;
            }

            headerContainerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(8, 8, 8, 6)
            };

            sectionContainerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(8, 8, 0, 0)
            };

            windowTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            windowSubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            windowSubtitleStyle.normal.textColor = isProSkin ? new Color(0.82f, 0.82f, 0.82f) : new Color(0.25f, 0.25f, 0.25f);

            sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            sectionSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
            sectionSubtitleStyle.normal.textColor = isProSkin ? new Color(0.74f, 0.74f, 0.74f) : new Color(0.32f, 0.32f, 0.32f);

            subsectionTitleStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                fontSize = 11
            };

            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 34f
            };

            reportEntryStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                richText = false
            };

            summaryMetricLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                richText = false
            };
            summaryMetricLabelStyle.normal.textColor = isProSkin ? new Color(0.88f, 0.88f, 0.88f) : new Color(0.18f, 0.18f, 0.18f);

            summaryMetricValueStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleRight,
                richText = false
            };
            summaryMetricValueStyle.normal.textColor = isProSkin ? new Color(0.94f, 0.94f, 0.94f) : new Color(0.1f, 0.1f, 0.1f);

            stylesForProSkin = isProSkin;
            stylesInitialized = true;
        }

        private static Color GetAccentColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.25f, 0.58f, 0.95f, 1f)
                : new Color(0.16f, 0.47f, 0.86f, 1f);
        }

        private void DrawWindowHeader()
        {
            EditorGUILayout.BeginVertical(headerContainerStyle);

            Rect accentRect = EditorGUILayout.GetControlRect(false, 3f);
            EditorGUI.DrawRect(accentRect, GetAccentColor());

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(ToolTitle, windowTitleStyle);
            EditorGUILayout.LabelField(
                "Optimize selected scene meshes with quality-aware simplification and clean asset output.",
                windowSubtitleStyle);

            EditorGUILayout.EndVertical();
        }

        private void DrawSectionHeader(string title, string subtitle)
        {
            EditorGUILayout.LabelField(title, sectionTitleStyle);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, sectionSubtitleStyle);
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawSelectionSummary(SelectionSummary summary)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Hierarchy Statistics", subsectionTitleStyle);
            EditorGUILayout.Space(2f);

            Color primaryMetricColor = GetAccentColor();
            Color geometryMetricColor = EditorGUIUtility.isProSkin
                ? new Color(0.98f, 0.75f, 0.36f, 1f)
                : new Color(0.75f, 0.42f, 0.05f, 1f);

            DrawSelectionMetricRow("Renderers", summary.RendererCount.ToString("N0"), primaryMetricColor, false);
            DrawSelectionMetricRow("Mesh Colliders", summary.MeshColliderCount.ToString("N0"), primaryMetricColor, true);
            DrawSelectionMetricRow("Unique Meshes", summary.UniqueMeshCount.ToString("N0"), primaryMetricColor, false);

            EditorGUILayout.Space(4f);
            DrawSelectionMetricRow("Scene Triangles", summary.TriangleCount.ToString("N0"), geometryMetricColor, true);
            DrawSelectionMetricRow("Scene Vertices", summary.VertexCount.ToString("N0"), geometryMetricColor, false);
        }

        private void DrawSelectionMetricRow(string label, string value, Color valueColor, bool alternateRow)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, 24f);
            Rect backgroundRect = new Rect(rowRect.x + 2f, rowRect.y + 1f, rowRect.width - 4f, rowRect.height - 2f);

            Color rowBackground = EditorGUIUtility.isProSkin
                ? (alternateRow ? new Color(1f, 1f, 1f, 0.05f) : new Color(1f, 1f, 1f, 0.025f))
                : (alternateRow ? new Color(0f, 0f, 0f, 0.05f) : new Color(0f, 0f, 0f, 0.025f));
            EditorGUI.DrawRect(backgroundRect, rowBackground);

            Rect labelRect = new Rect(backgroundRect.x + 8f, backgroundRect.y + 2f, Mathf.Max(150f, backgroundRect.width * 0.57f), backgroundRect.height - 4f);
            Rect valueRect = new Rect(labelRect.xMax + 6f, backgroundRect.y + 2f, Mathf.Max(60f, backgroundRect.xMax - labelRect.xMax - 12f), backgroundRect.height - 4f);

            EditorGUI.LabelField(labelRect, label, summaryMetricLabelStyle);

            Color previousColor = GUI.color;
            GUI.color = valueColor;
            EditorGUI.LabelField(valueRect, value, summaryMetricValueStyle);
            GUI.color = previousColor;
        }

        private void DrawCreditsTab()
        {
            creditsScrollPosition = EditorGUILayout.BeginScrollView(creditsScrollPosition);

            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawSectionHeader("Credits", "Project and author information.");

            EditorGUILayout.LabelField("Creator", creditCreatorName);
            EditorGUILayout.LabelField("Company", creditCompanyName);
            EditorGUILayout.LabelField("Copyright", creditCopyright);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Profiles", subsectionTitleStyle);
            DrawProfileRow("LinkedIn", creditLinkedInUrl);
            DrawProfileRow("GitHub", creditGitHubUrl);

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("Thanks for using MeshOptix.\nThis tool is distributed under the MIT License.", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawProfileRow(string label, string url)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(url);
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(url)))
            {
                if (GUILayout.Button("Open", GUILayout.Width(54f)))
                {
                    Application.OpenURL(url);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawWindowHeader();

            selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, WindowTabs, GUILayout.Height(24f));
            EditorGUILayout.Space(4f);

            if (selectedTabIndex == 1)
            {
                DrawCreditsTab();
                return;
            }

            GameObject selectedObject = Selection.activeGameObject;
            SelectionSummary summary = SelectionSummary.Build(selectedObject, includeInactiveChildren);

            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);

            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawSectionHeader("Selected Scene Object", "Select a scene instance to inspect and optimize.");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(selectedObject, typeof(GameObject), true);
            }

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject in the scene to simplify its mesh hierarchy.", MessageType.Info);
            }
            else if (EditorUtility.IsPersistent(selectedObject))
            {
                EditorGUILayout.HelpBox("Select a scene instance, not a prefab asset from the Project window.", MessageType.Warning);
            }
            else if (!summary.HasSupportedMeshes)
            {
                EditorGUILayout.HelpBox("The selected object does not contain MeshFilter/MeshRenderer or SkinnedMeshRenderer components with meshes.", MessageType.Warning);
            }
            else
            {
                DrawSelectionSummary(summary);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawSectionHeader("Simplification Settings", "Tune reduction amount, preservation behavior, and replacement strategy.");

            targetTriangleRatio = EditorGUILayout.Slider(
                new GUIContent("Target Triangle Ratio", "1 keeps the original density, 0.5 aims for about half the triangles."),
                targetTriangleRatio,
                0.05f,
                1f);

            includeInactiveChildren = EditorGUILayout.Toggle(
                new GUIContent("Include Inactive Children", "Process meshes on inactive children too."),
                includeInactiveChildren);

            updateMeshColliders = EditorGUILayout.Toggle(
                new GUIContent("Update Matching MeshColliders", "If a MeshCollider references the same source mesh, switch it to the simplified mesh too."),
                updateMeshColliders);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Preservation", subsectionTitleStyle);
            EditorGUILayout.LabelField("Essentials first. Expand advanced controls only when needed.", sectionSubtitleStyle);

            bool preserveAnyUvSeams = preserveUvSeams || preserveLightmapUvSeams;
            bool preserveAnyUvSeamsNext = EditorGUILayout.Toggle(
                new GUIContent("Preserve UV Seams (Recommended)", "Enables both UV0 and UV1 seam preservation to protect texture and lightmap seams."),
                preserveAnyUvSeams);

            if (preserveAnyUvSeamsNext != preserveAnyUvSeams)
            {
                preserveUvSeams = preserveAnyUvSeamsNext;
                preserveLightmapUvSeams = preserveAnyUvSeamsNext;
            }

            preserveHardEdges = EditorGUILayout.Toggle(
                new GUIContent("Preserve Hard Edges", "Keeps vertices with clearly different normals separated."),
                preserveHardEdges);

            preserveSkinningBoundaries = EditorGUILayout.Toggle(
                new GUIContent("Preserve Skinning Boundaries", "Helps skinned meshes keep bone regions separated."),
                preserveSkinningBoundaries);

            recalculateTangents = EditorGUILayout.Toggle(
                new GUIContent("Recalculate Tangents", "Recommended if the mesh uses normal maps."),
                recalculateTangents);

            EditorGUILayout.Space(4f);
            showAdvancedPreservationOptions = EditorGUILayout.Foldout(
                showAdvancedPreservationOptions,
                "Advanced Preservation & Processing",
                true);

            if (showAdvancedPreservationOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.HelpBox("Use these options for specific artifact fixes or import constraints.", MessageType.None);

                    preserveUvSeams = EditorGUILayout.Toggle(
                        new GUIContent("Preserve UV0 Seams", "Keeps primary texture seams safer at the cost of less reduction."),
                        preserveUvSeams);

                    preserveLightmapUvSeams = EditorGUILayout.Toggle(
                        new GUIContent("Preserve UV1 Seams", "Useful when the mesh depends on secondary UVs/lightmap UVs."),
                        preserveLightmapUvSeams);

                    recalculateNormals = EditorGUILayout.Toggle(
                        new GUIContent("Recalculate Normals", "Useful if the simplified result looks faceted or noisy."),
                        recalculateNormals);

                    optimizeMeshBuffers = EditorGUILayout.Toggle(
                        new GUIContent("Optimize Mesh Buffers", "Runs Unity's mesh buffer optimizer after simplification."),
                        optimizeMeshBuffers);

                    autoMakeSourceMeshesReadable = EditorGUILayout.Toggle(
                        new GUIContent("Auto Enable Read/Write", "Temporarily toggles model importers to readable when needed, then restores them."),
                        autoMakeSourceMeshesReadable);
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Replacement", subsectionTitleStyle);

            replaceSelection = EditorGUILayout.Toggle(
                new GUIContent("Replace Selected Object", "Duplicates the selection, optimizes the duplicate, then removes or disables the original."),
                replaceSelection);

            using (new EditorGUI.DisabledScope(!replaceSelection))
            {
                keepDisabledBackup = EditorGUILayout.Toggle(
                    new GUIContent("Keep Disabled Backup", "Keeps the original object disabled instead of deleting it."),
                    keepDisabledBackup);
            }

            if (replaceSelection)
            {
                EditorGUILayout.HelpBox(
                    "Replace mode matches your requested workflow, but external scene references to the original object will not automatically move to the duplicate. Use in-place mode if other objects reference this selection.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("In-place mode is safer for scene references because it keeps the same GameObject.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawSectionHeader("Mesh Asset Output", "Choose where optimized mesh assets are generated.");

            EditorGUILayout.BeginHorizontal();
            outputFolder = EditorGUILayout.TextField(outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(78f)))
            {
                BrowseForOutputFolder();
            }

            if (GUILayout.Button("Default", GUILayout.Width(78f)))
            {
                outputFolder = DefaultOutputFolder;
            }

            EditorGUILayout.EndHorizontal();

            bool isOutputFolderValid = AssetPathUtility.IsValidAssetsPath(outputFolder);

            if (!isOutputFolderValid)
            {
                EditorGUILayout.HelpBox("Output folder must stay inside the project's Assets folder.", MessageType.Error);
            }

            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(
                selectedObject == null ||
                EditorUtility.IsPersistent(selectedObject) ||
                !summary.HasSupportedMeshes ||
                !isOutputFolderValid))
            {
                Color previousBackground = GUI.backgroundColor;
                GUI.backgroundColor = GetAccentColor();

                if (GUILayout.Button("Optimize Selected", primaryButtonStyle))
                {
                    RunOptimization(selectedObject);
                }

                GUI.backgroundColor = previousBackground;
            }

            EditorGUILayout.EndVertical();

            if (lastReport != null)
            {
                EditorGUILayout.Space(8f);
                DrawLastReport();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.EndScrollView();
        }

        private void BrowseForOutputFolder()
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("Optimized Mesh Output Folder", Application.dataPath, string.Empty);

            if (string.IsNullOrEmpty(selectedFolder))
            {
                return;
            }

            if (!AssetPathUtility.TryGetCanonicalAssetsPath(selectedFolder, out string canonicalAssetsPath))
            {
                EditorUtility.DisplayDialog(ToolTitle, "Choose a folder inside this Unity project's Assets folder.", "OK");
                return;
            }

            outputFolder = canonicalAssetsPath;
        }

        private void RunOptimization(GameObject selectedObject)
        {
            if (!AssetPathUtility.TryGetCanonicalAssetsPath(outputFolder, out string canonicalOutputFolder))
            {
                EditorUtility.DisplayDialog(ToolTitle, "Output folder must stay inside this project's Assets folder.", "OK");
                return;
            }

            outputFolder = canonicalOutputFolder;
            lastReport = null;
            Repaint();

            SceneMeshOptimizerOptions options = new SceneMeshOptimizerOptions(
                targetTriangleRatio,
                replaceSelection,
                keepDisabledBackup,
                includeInactiveChildren,
                updateMeshColliders,
                preserveUvSeams,
                preserveLightmapUvSeams,
                preserveHardEdges,
                preserveSkinningBoundaries,
                recalculateNormals,
                recalculateTangents,
                optimizeMeshBuffers,
                autoMakeSourceMeshesReadable,
                canonicalOutputFolder);

            try
            {
                lastReport = SceneMeshOptimizer.Optimize(selectedObject, options);
                Debug.Log(lastReport.BuildLogMessage());
                OptimizationResultPopupWindow.Show(lastReport);
                Repaint();
            }
            catch (OperationCanceledException exception)
            {
                Debug.Log("[SceneMeshOptimizer] " + exception.Message);
                EditorUtility.DisplayDialog(ToolTitle, exception.Message, "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(ToolTitle, exception.Message, "OK");
            }
        }

        private void DrawLastReport()
        {
            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawSectionHeader("Last Run", "Summary and detailed messages from the most recent optimization.");
            EditorGUILayout.HelpBox(lastReport.BuildSummaryLine(), MessageType.None);

            if (lastReport.Messages.Count == 0)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            reportScrollPosition = EditorGUILayout.BeginScrollView(reportScrollPosition, GUILayout.MinHeight(120f), GUILayout.MaxHeight(220f));

            for (int i = 0; i < lastReport.Messages.Count; i++)
            {
                EditorGUILayout.LabelField("- " + lastReport.Messages[i], reportEntryStyle);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }

    internal sealed class OptimizationResultPopupWindow : EditorWindow
    {
        private OptimizationReport report;
        private Vector2 messagesScrollPosition;

        private GUIStyle sectionContainerStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle valueStyle;
        private GUIStyle beforeValueStyle;
        private GUIStyle afterValueStyle;
        private GUIStyle messageEntryStyle;

        public static void Show(OptimizationReport report)
        {
            if (report == null)
            {
                return;
            }

            OptimizationResultPopupWindow window = CreateInstance<OptimizationResultPopupWindow>();
            window.titleContent = new GUIContent("Optimization Results");
            window.minSize = new Vector2(560f, 470f);
            window.maxSize = new Vector2(760f, 760f);
            window.report = report;
            window.ShowUtility();
            window.Focus();
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (report == null)
            {
                EditorGUILayout.HelpBox("No optimization report is available.", MessageType.Info);
                if (GUILayout.Button("Close", GUILayout.Height(28f)))
                {
                    Close();
                }

                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Optimization Complete", titleStyle);
            EditorGUILayout.LabelField(report.SelectionName, subtitleStyle);

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(sectionContainerStyle);
            DrawMetricRow("Renderers touched", report.RendererCount.ToString("N0"));
            DrawMetricRow("Unique meshes simplified", report.UniqueMeshesSimplified.ToString("N0"));
            DrawMetricRow("Unique meshes skipped", report.UniqueMeshesSkipped.ToString("N0"));
            DrawBeforeAfterMetricRow("Triangles", report.OriginalTriangleCount, report.OptimizedTriangleCount);
            DrawBeforeAfterMetricRow("Vertices", report.OriginalVertexCount, report.OptimizedVertexCount);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginVertical(sectionContainerStyle);
            EditorGUILayout.LabelField("Output Folder", EditorStyles.miniBoldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(report.OutputFolder);
            }

            EditorGUILayout.HelpBox("Scene object changes can be undone. Generated mesh assets remain in the project.", MessageType.None);
            EditorGUILayout.EndVertical();

            if (report.Messages.Count > 0)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.BeginVertical(sectionContainerStyle);
                EditorGUILayout.LabelField("Details", EditorStyles.miniBoldLabel);
                messagesScrollPosition = EditorGUILayout.BeginScrollView(messagesScrollPosition, GUILayout.MinHeight(120f), GUILayout.MaxHeight(260f));

                for (int i = 0; i < report.Messages.Count; i++)
                {
                    EditorGUILayout.LabelField("- " + report.Messages[i], messageEntryStyle);
                }

                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Close", GUILayout.Height(30f)))
            {
                Close();
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawMetricRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(190f));
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBeforeAfterMetricRow(string label, int before, int after)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(190f));
            EditorGUILayout.LabelField(before.ToString("N0"), beforeValueStyle, GUILayout.Width(100f));
            EditorGUILayout.LabelField("->", GUILayout.Width(20f));
            EditorGUILayout.LabelField(after.ToString("N0"), afterValueStyle, GUILayout.Width(100f));
            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            sectionContainerStyle ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            subtitleStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.82f, 0.82f, 0.82f) : new Color(0.26f, 0.26f, 0.26f);

            valueStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            beforeValueStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            beforeValueStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.98f, 0.45f, 0.45f) : new Color(0.75f, 0.14f, 0.14f);

            afterValueStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            afterValueStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.45f, 0.9f, 0.56f) : new Color(0.1f, 0.55f, 0.2f);

            messageEntryStyle ??= new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                richText = false
            };
        }
    }

    internal static class SceneMeshOptimizer
    {
        private const string ToolTitle = "MeshOptix : Scene Mesh Optimizer";

        public static OptimizationReport Optimize(GameObject selectedObject, SceneMeshOptimizerOptions options)
        {
            if (selectedObject == null)
            {
                throw new ArgumentNullException(nameof(selectedObject));
            }

            if (EditorUtility.IsPersistent(selectedObject))
            {
                throw new InvalidOperationException("Select a GameObject in the scene hierarchy.");
            }

            AssetPathUtility.EnsureFolderExists(options.OutputFolder);

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(ToolTitle);

            GameObject originalRoot = selectedObject;
            GameObject workingRoot = selectedObject;
            OptimizationReport report = new OptimizationReport(selectedObject.name, options.OutputFolder);

            try
            {
                if (options.ReplaceSelection)
                {
                    workingRoot = DuplicateForReplacement(selectedObject);
                }

                List<MeshTarget> initialTargets = MeshTargetCollector.Collect(workingRoot, options.IncludeInactiveChildren);
                if (initialTargets.Count == 0)
                {
                    throw new InvalidOperationException("No supported meshes were found in the selected hierarchy.");
                }

                List<Mesh> sourceMeshes = new List<Mesh>(initialTargets.Count);
                for (int i = 0; i < initialTargets.Count; i++)
                {
                    Mesh sourceMesh = initialTargets[i].SharedMesh;
                    if (sourceMesh != null)
                    {
                        sourceMeshes.Add(sourceMesh);
                    }
                }

                using (MeshReadabilityRestorer restorer = MeshReadabilityRestorer.Enable(sourceMeshes, options.AutoMakeSourceMeshesReadable, report.Messages))
                {
                    List<MeshTarget> targets = MeshTargetCollector.Collect(workingRoot, options.IncludeInactiveChildren);
                    Dictionary<int, CachedMeshResult> meshCache = new Dictionary<int, CachedMeshResult>();

                    for (int i = 0; i < targets.Count; i++)
                    {
                        MeshTarget target = targets[i];

                        if (EditorUtility.DisplayCancelableProgressBar(
                            ToolTitle,
                            "Processing " + target.DisplayName,
                            (i + 1f) / Mathf.Max(1, targets.Count)))
                        {
                            throw new OperationCanceledException("Optimization canceled. Scene changes were reverted.");
                        }

                        Mesh sourceMesh = target.SharedMesh;

                        if (sourceMesh == null)
                        {
                            continue;
                        }

                        report.RendererCount++;
                        report.OriginalVertexCount += sourceMesh.vertexCount;
                        report.OriginalTriangleCount += MeshStats.CountTriangles(sourceMesh);

                        int cacheKey = sourceMesh.GetInstanceID();
                        if (!meshCache.TryGetValue(cacheKey, out CachedMeshResult cachedResult))
                        {
                            cachedResult = CreateCachedMeshResult(sourceMesh, selectedObject.name, options, report);
                            meshCache.Add(cacheKey, cachedResult);
                        }

                        Undo.RecordObject(target.Component, "Assign Optimized Mesh");
                        target.AssignMesh(cachedResult.OutputMesh);

                        report.OptimizedVertexCount += cachedResult.OutputMesh.vertexCount;
                        report.OptimizedTriangleCount += MeshStats.CountTriangles(cachedResult.OutputMesh);
                    }

                    if (options.UpdateMeshColliders)
                    {
                        UpdateMatchingMeshColliders(workingRoot, meshCache, options.IncludeInactiveChildren);
                    }
                }

                if (report.UniqueMeshesSimplified == 0)
                {
                    string failureMessage = report.BuildFailureMessage();
                    Debug.LogWarning("[SceneMeshOptimizer] " + failureMessage);
                    throw new InvalidOperationException(failureMessage);
                }

                if (options.ReplaceSelection)
                {
                    FinalizeReplacement(originalRoot, workingRoot, options.KeepDisabledBackup);
                }

                EditorUtility.SetDirty(workingRoot);
                AssetDatabase.SaveAssets();

                Selection.activeGameObject = workingRoot;
                EditorGUIUtility.PingObject(workingRoot);

                Undo.CollapseUndoOperations(undoGroup);
                return report;
            }
            catch
            {
                CleanupFailedRun(report, undoGroup);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static GameObject DuplicateForReplacement(GameObject sourceRoot)
        {
            Transform sourceParent = sourceRoot.transform.parent;
            GameObject duplicate = UnityEngine.Object.Instantiate(sourceRoot, sourceParent);
            duplicate.name = sourceRoot.name + "_Optimized";
            duplicate.transform.SetSiblingIndex(sourceRoot.transform.GetSiblingIndex());
            Undo.RegisterCreatedObjectUndo(duplicate, "Create Optimized Duplicate");
            return duplicate;
        }

        private static void FinalizeReplacement(GameObject originalRoot, GameObject optimizedRoot, bool keepDisabledBackup)
        {
            string originalName = originalRoot.name;

            if (keepDisabledBackup)
            {
                Undo.RegisterFullObjectHierarchyUndo(originalRoot, "Disable Original Object");
                originalRoot.name = originalName + "_Original";
                originalRoot.SetActive(false);
            }
            else
            {
                Undo.DestroyObjectImmediate(originalRoot);
            }

            Undo.RecordObject(optimizedRoot, "Rename Optimized Object");
            optimizedRoot.name = originalName;
        }

        private static CachedMeshResult CreateCachedMeshResult(
            Mesh sourceMesh,
            string selectionName,
            SceneMeshOptimizerOptions options,
            OptimizationReport report)
        {
            if (!QualityFirstMeshSimplifier.TrySimplify(sourceMesh, options, out Mesh optimizedMesh, out MeshSimplificationResult result))
            {
                report.Messages.Add(result.Message);
                report.UniqueMeshesSkipped++;
                return new CachedMeshResult(sourceMesh, false, string.Empty, result);
            }

            return SaveSimplifiedMeshAsset(
                optimizedMesh,
                selectionName,
                sourceMesh.name,
                options.OutputFolder,
                report,
                result,
                string.Empty);
        }

        private static CachedMeshResult SaveSimplifiedMeshAsset(
            Mesh optimizedMesh,
            string selectionName,
            string sourceMeshName,
            string outputFolder,
            OptimizationReport report,
            MeshSimplificationResult result,
            string messageSuffix)
        {
            string assetPath = AssetPathUtility.MakeUniqueMeshAssetPath(outputFolder, selectionName, sourceMeshName);

            try
            {
                AssetDatabase.CreateAsset(optimizedMesh, assetPath);
            }
            catch
            {
                if (optimizedMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(optimizedMesh);
                }

                throw;
            }

            report.UniqueMeshesSimplified++;
            report.GeneratedAssetPaths.Add(assetPath);

            string message = result.Message + " Saved to " + assetPath;
            if (!string.IsNullOrEmpty(messageSuffix))
            {
                message += " " + messageSuffix;
            }

            report.Messages.Add(message);
            return new CachedMeshResult(optimizedMesh, true, assetPath, result);
        }

        private static void CleanupFailedRun(OptimizationReport report, int undoGroup)
        {
            try
            {
                DeleteGeneratedAssets(report);
            }
            catch (Exception cleanupException)
            {
                Debug.LogWarning("[SceneMeshOptimizer] Failed to remove generated assets after an aborted run: " + cleanupException.Message);
            }

            try
            {
                Undo.RevertAllDownToGroup(undoGroup);
            }
            catch (Exception cleanupException)
            {
                Debug.LogWarning("[SceneMeshOptimizer] Failed to revert scene changes after an aborted run: " + cleanupException.Message);
            }
        }

        private static void DeleteGeneratedAssets(OptimizationReport report)
        {
            if (report == null || report.GeneratedAssetPaths.Count == 0)
            {
                return;
            }

            HashSet<string> deletedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = report.GeneratedAssetPaths.Count - 1; i >= 0; i--)
            {
                string assetPath = report.GeneratedAssetPaths[i];
                if (string.IsNullOrWhiteSpace(assetPath) || !deletedPaths.Add(assetPath))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.SaveAssets();
        }

        private static void UpdateMatchingMeshColliders(
            GameObject root,
            Dictionary<int, CachedMeshResult> meshCache,
            bool includeInactiveChildren)
        {
            MeshCollider[] colliders = root.GetComponentsInChildren<MeshCollider>(includeInactiveChildren);
            for (int i = 0; i < colliders.Length; i++)
            {
                MeshCollider collider = colliders[i];
                Mesh sharedMesh = collider.sharedMesh;

                if (sharedMesh == null)
                {
                    continue;
                }

                if (!meshCache.TryGetValue(sharedMesh.GetInstanceID(), out CachedMeshResult result))
                {
                    continue;
                }

                if (!result.WasSimplified || result.OutputMesh == null || result.OutputMesh == sharedMesh)
                {
                    continue;
                }

                Undo.RecordObject(collider, "Assign Optimized MeshCollider");
                collider.sharedMesh = result.OutputMesh;
            }
        }
    }

    internal readonly struct SceneMeshOptimizerOptions
    {
        public readonly float TargetTriangleRatio;
        public readonly bool ReplaceSelection;
        public readonly bool KeepDisabledBackup;
        public readonly bool IncludeInactiveChildren;
        public readonly bool UpdateMeshColliders;
        public readonly bool PreserveUvSeams;
        public readonly bool PreserveLightmapUvSeams;
        public readonly bool PreserveHardEdges;
        public readonly bool PreserveSkinningBoundaries;
        public readonly bool RecalculateNormals;
        public readonly bool RecalculateTangents;
        public readonly bool OptimizeMeshBuffers;
        public readonly bool AutoMakeSourceMeshesReadable;
        public readonly string OutputFolder;

        public bool UsesAnyPreservation
        {
            get
            {
                return
                    PreserveUvSeams ||
                    PreserveLightmapUvSeams ||
                    PreserveHardEdges ||
                    PreserveSkinningBoundaries;
            }
        }

        public SceneMeshOptimizerOptions(
            float targetTriangleRatio,
            bool replaceSelection,
            bool keepDisabledBackup,
            bool includeInactiveChildren,
            bool updateMeshColliders,
            bool preserveUvSeams,
            bool preserveLightmapUvSeams,
            bool preserveHardEdges,
            bool preserveSkinningBoundaries,
            bool recalculateNormals,
            bool recalculateTangents,
            bool optimizeMeshBuffers,
            bool autoMakeSourceMeshesReadable,
            string outputFolder)
        {
            TargetTriangleRatio = Mathf.Clamp(targetTriangleRatio, 0.05f, 1f);
            ReplaceSelection = replaceSelection;
            KeepDisabledBackup = keepDisabledBackup;
            IncludeInactiveChildren = includeInactiveChildren;
            UpdateMeshColliders = updateMeshColliders;
            PreserveUvSeams = preserveUvSeams;
            PreserveLightmapUvSeams = preserveLightmapUvSeams;
            PreserveHardEdges = preserveHardEdges;
            PreserveSkinningBoundaries = preserveSkinningBoundaries;
            RecalculateNormals = recalculateNormals;
            RecalculateTangents = recalculateTangents;
            OptimizeMeshBuffers = optimizeMeshBuffers;
            AutoMakeSourceMeshesReadable = autoMakeSourceMeshesReadable;
            OutputFolder = outputFolder;
        }

        public SceneMeshOptimizerOptions WithRelaxedPreservation()
        {
            return new SceneMeshOptimizerOptions(
                TargetTriangleRatio,
                ReplaceSelection,
                KeepDisabledBackup,
                IncludeInactiveChildren,
                UpdateMeshColliders,
                false,
                false,
                false,
                false,
                RecalculateNormals,
                RecalculateTangents,
                OptimizeMeshBuffers,
                AutoMakeSourceMeshesReadable,
                OutputFolder);
        }

        public SceneMeshOptimizerOptions WithTargetTriangleRatio(float targetTriangleRatio)
        {
            return new SceneMeshOptimizerOptions(
                targetTriangleRatio,
                ReplaceSelection,
                KeepDisabledBackup,
                IncludeInactiveChildren,
                UpdateMeshColliders,
                PreserveUvSeams,
                PreserveLightmapUvSeams,
                PreserveHardEdges,
                PreserveSkinningBoundaries,
                RecalculateNormals,
                RecalculateTangents,
                OptimizeMeshBuffers,
                AutoMakeSourceMeshesReadable,
                OutputFolder);
        }
    }

    internal sealed class OptimizationReport
    {
        public readonly List<string> Messages = new List<string>();
        public readonly List<string> GeneratedAssetPaths = new List<string>();

        public readonly string SelectionName;
        public readonly string OutputFolder;

        public int RendererCount;
        public int UniqueMeshesSimplified;
        public int UniqueMeshesSkipped;
        public int OriginalVertexCount;
        public int OptimizedVertexCount;
        public int OriginalTriangleCount;
        public int OptimizedTriangleCount;

        public OptimizationReport(string selectionName, string outputFolder)
        {
            SelectionName = selectionName;
            OutputFolder = outputFolder;
        }

        public string BuildSummaryLine()
        {
            return
                SelectionName + ": " +
                FormatReduction(OriginalTriangleCount, OptimizedTriangleCount, "tris") + ", " +
                FormatReduction(OriginalVertexCount, OptimizedVertexCount, "verts") + ".";
        }

        public string BuildDialogMessage()
        {
            return
                "Optimized " + SelectionName + Environment.NewLine + Environment.NewLine +
                "Renderers touched: " + RendererCount + Environment.NewLine +
                "Unique meshes simplified: " + UniqueMeshesSimplified + Environment.NewLine +
                "Unique meshes skipped: " + UniqueMeshesSkipped + Environment.NewLine +
                "Triangles: " + OriginalTriangleCount.ToString("N0") + " -> " + OptimizedTriangleCount.ToString("N0") + Environment.NewLine +
                "Vertices: " + OriginalVertexCount.ToString("N0") + " -> " + OptimizedVertexCount.ToString("N0") + Environment.NewLine +
                "Output folder: " + OutputFolder + Environment.NewLine + Environment.NewLine +
                "Undo restores the scene object changes, but generated mesh assets remain in the project.";
        }

        public string BuildLogMessage()
        {
            string logMessage =
                "[SceneMeshOptimizer] " + BuildSummaryLine() + Environment.NewLine +
                "Unique meshes simplified: " + UniqueMeshesSimplified + ", skipped: " + UniqueMeshesSkipped + Environment.NewLine +
                "Output folder: " + OutputFolder;

            if (Messages.Count > 0)
            {
                logMessage += Environment.NewLine + string.Join(Environment.NewLine, Messages);
            }

            return logMessage;
        }

        public string BuildFailureMessage()
        {
            const int maxMessages = 8;

            string message =
                "No meshes were simplified for " + SelectionName + "." + Environment.NewLine +
                "Checked renderers: " + RendererCount + Environment.NewLine +
                "Unique meshes skipped: " + UniqueMeshesSkipped;

            if (Messages.Count == 0)
            {
                return message;
            }

            int displayedMessageCount = Mathf.Min(maxMessages, Messages.Count);
            message += Environment.NewLine + Environment.NewLine + "Skip reasons:";

            for (int i = 0; i < displayedMessageCount; i++)
            {
                message += Environment.NewLine + "- " + Messages[i];
            }

            if (Messages.Count > displayedMessageCount)
            {
                message += Environment.NewLine + "- ...and " + (Messages.Count - displayedMessageCount) + " more.";
            }

            return message;
        }

        private static string FormatReduction(int before, int after, string label)
        {
            if (before <= 0)
            {
                return "0 " + label;
            }

            float ratio = 1f - (after / (float)before);
            return before.ToString("N0") + " -> " + after.ToString("N0") + " " + label + " (" + ratio.ToString("P0") + ")";
        }
    }

    internal readonly struct SelectionSummary
    {
        public readonly int RendererCount;
        public readonly int MeshColliderCount;
        public readonly int UniqueMeshCount;
        public readonly int TriangleCount;
        public readonly int VertexCount;

        public bool HasSupportedMeshes
        {
            get { return RendererCount > 0; }
        }

        public SelectionSummary(int rendererCount, int meshColliderCount, int uniqueMeshCount, int triangleCount, int vertexCount)
        {
            RendererCount = rendererCount;
            MeshColliderCount = meshColliderCount;
            UniqueMeshCount = uniqueMeshCount;
            TriangleCount = triangleCount;
            VertexCount = vertexCount;
        }

        public static SelectionSummary Build(GameObject root, bool includeInactiveChildren)
        {
            if (root == null)
            {
                return default;
            }

            List<MeshTarget> targets = MeshTargetCollector.Collect(root, includeInactiveChildren);
            HashSet<int> uniqueMeshes = new HashSet<int>();
            int triangleCount = 0;
            int vertexCount = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                Mesh mesh = targets[i].SharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                uniqueMeshes.Add(mesh.GetInstanceID());
                triangleCount += MeshStats.CountTriangles(mesh);
                vertexCount += mesh.vertexCount;
            }

            MeshCollider[] colliders = root.GetComponentsInChildren<MeshCollider>(includeInactiveChildren);
            int meshColliderCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].sharedMesh != null)
                {
                    meshColliderCount++;
                }
            }

            return new SelectionSummary(targets.Count, meshColliderCount, uniqueMeshes.Count, triangleCount, vertexCount);
        }

        public string BuildMessage()
        {
            return
                "Renderers: " + RendererCount + Environment.NewLine +
                "MeshColliders: " + MeshColliderCount + Environment.NewLine +
                "Unique meshes: " + UniqueMeshCount + Environment.NewLine +
                "Scene triangles: " + TriangleCount.ToString("N0") + Environment.NewLine +
                "Scene vertices: " + VertexCount.ToString("N0");
        }
    }

    internal static class MeshTargetCollector
    {
        public static List<MeshTarget> Collect(GameObject root, bool includeInactiveChildren)
        {
            List<MeshTarget> targets = new List<MeshTarget>();

            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(includeInactiveChildren);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                if (meshRenderer == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                targets.Add(new MeshTarget(meshFilter));
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactiveChildren);
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
                if (skinnedMeshRenderer.sharedMesh == null)
                {
                    continue;
                }

                targets.Add(new MeshTarget(skinnedMeshRenderer));
            }

            return targets;
        }
    }

    internal readonly struct MeshTarget
    {
        private readonly MeshFilter meshFilter;
        private readonly SkinnedMeshRenderer skinnedMeshRenderer;
        private readonly Component component;

        public Component Component
        {
            get { return component; }
        }

        public Mesh SharedMesh
        {
            get
            {
                if (meshFilter != null)
                {
                    return meshFilter.sharedMesh;
                }

                if (skinnedMeshRenderer != null)
                {
                    return skinnedMeshRenderer.sharedMesh;
                }

                return null;
            }
        }

        public string DisplayName
        {
            get
            {
                string ownerName = component != null ? component.gameObject.name : "Unknown";
                Mesh mesh = SharedMesh;
                string meshName = mesh != null ? mesh.name : "No Mesh";
                return ownerName + " (" + meshName + ")";
            }
        }

        public MeshTarget(MeshFilter meshFilter)
        {
            this.meshFilter = meshFilter;
            skinnedMeshRenderer = null;
            component = meshFilter;
        }

        public MeshTarget(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            meshFilter = null;
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            component = skinnedMeshRenderer;
        }

        public void AssignMesh(Mesh mesh)
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = mesh;
                return;
            }

            if (skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.sharedMesh = mesh;
            }
        }
    }

    internal sealed class CachedMeshResult
    {
        public readonly Mesh OutputMesh;
        public readonly bool WasSimplified;
        public readonly string AssetPath;
        public readonly MeshSimplificationResult Result;

        public CachedMeshResult(Mesh outputMesh, bool wasSimplified, string assetPath, MeshSimplificationResult result)
        {
            OutputMesh = outputMesh;
            WasSimplified = wasSimplified;
            AssetPath = assetPath;
            Result = result;
        }
    }

    internal readonly struct MeshSimplificationResult
    {
        public readonly bool Succeeded;
        public readonly int OriginalVertexCount;
        public readonly int OptimizedVertexCount;
        public readonly int OriginalTriangleCount;
        public readonly int OptimizedTriangleCount;
        public readonly string Message;

        public MeshSimplificationResult(
            bool succeeded,
            int originalVertexCount,
            int optimizedVertexCount,
            int originalTriangleCount,
            int optimizedTriangleCount,
            string message)
        {
            Succeeded = succeeded;
            OriginalVertexCount = originalVertexCount;
            OptimizedVertexCount = optimizedVertexCount;
            OriginalTriangleCount = originalTriangleCount;
            OptimizedTriangleCount = optimizedTriangleCount;
            Message = message;
        }
    }

    internal static class QualityFirstMeshSimplifier
    {
        private const float TargetSlack = 1.05f;

        public static bool TrySimplify(
            Mesh sourceMesh,
            SceneMeshOptimizerOptions options,
            out Mesh optimizedMesh,
            out MeshSimplificationResult result)
        {
            optimizedMesh = null;

            if (TrySimplifyWithReferenceBackend(sourceMesh, options, out optimizedMesh, out result))
            {
                return true;
            }

            string backendFailureMessage = result.Message;
            if (TrySimplifyWithStagedQuadric(sourceMesh, options, out optimizedMesh, out MeshSimplificationResult stagedResult))
            {
                result = stagedResult;
                return true;
            }

            result = new MeshSimplificationResult(
                false,
                stagedResult.OriginalVertexCount,
                stagedResult.OptimizedVertexCount,
                stagedResult.OriginalTriangleCount,
                stagedResult.OptimizedTriangleCount,
                backendFailureMessage + " Fallback result: " + stagedResult.Message);
            return false;
        }

        private static bool TrySimplifyWithReferenceBackend(
            Mesh sourceMesh,
            SceneMeshOptimizerOptions options,
            out Mesh optimizedMesh,
            out MeshSimplificationResult result)
        {
            optimizedMesh = null;

            if (sourceMesh == null)
            {
                result = new MeshSimplificationResult(false, 0, 0, 0, 0, "Skipping Unknown Mesh: mesh reference is missing.");
                return false;
            }

            int originalTriangleCount = MeshStats.CountTriangles(sourceMesh);
            int originalVertexCount = sourceMesh.vertexCount;
            int targetTriangleCount = Mathf.Max(1, Mathf.RoundToInt(originalTriangleCount * options.TargetTriangleRatio));
            if (targetTriangleCount >= originalTriangleCount)
            {
                result = new MeshSimplificationResult(
                    false,
                    originalVertexCount,
                    originalVertexCount,
                    originalTriangleCount,
                    originalTriangleCount,
                    "Skipping " + sourceMesh.name + ": target ratio is too close to the original mesh.");
                return false;
            }

            if (!sourceMesh.isReadable)
            {
                result = new MeshSimplificationResult(
                    false,
                    originalVertexCount,
                    0,
                    originalTriangleCount,
                    0,
                    "Skipping " + sourceMesh.name + ": mesh is not readable.");
                return false;
            }

            BoneWeight[] sourceBoneWeights = sourceMesh.boneWeights;
            if (sourceBoneWeights != null && sourceBoneWeights.Length == sourceMesh.vertexCount)
            {
                result = new MeshSimplificationResult(
                    false,
                    originalVertexCount,
                    originalVertexCount,
                    originalTriangleCount,
                    originalTriangleCount,
                    "Skipping " + sourceMesh.name + ": smart-linked backend is disabled for skinned meshes to avoid bone-weight interpolation artifacts.");
                return false;
            }

            MeshSimplifier simplifier = new MeshSimplifier(sourceMesh);
            simplifier.SimplificationOptions = BuildReferenceOptions(sourceMesh, options);
            simplifier.SimplifyMesh(options.TargetTriangleRatio);

            optimizedMesh = simplifier.ToMesh();
            optimizedMesh.name = sourceMesh.name + "_Optimized";
            FinalizeMeshPostProcessing(optimizedMesh, sourceMesh, options);

            int optimizedTriangleCount = MeshStats.CountTriangles(optimizedMesh);
            int optimizedVertexCount = optimizedMesh.vertexCount;
            bool hasReduction =
                optimizedTriangleCount < originalTriangleCount ||
                optimizedVertexCount < originalVertexCount;

            if (!hasReduction)
            {
                UnityEngine.Object.DestroyImmediate(optimizedMesh);
                optimizedMesh = null;
                result = new MeshSimplificationResult(
                    false,
                    originalVertexCount,
                    originalVertexCount,
                    originalTriangleCount,
                    originalTriangleCount,
                    "Skipping " + sourceMesh.name + ": project simplifier backend did not find a meaningful reduction.");
                return false;
            }

            string message =
                sourceMesh.name + ": " +
                originalTriangleCount.ToString("N0") + " -> " + optimizedTriangleCount.ToString("N0") + " tris, " +
                originalVertexCount.ToString("N0") + " -> " + optimizedVertexCount.ToString("N0") + " verts via smart-linked quadric simplification.";

            if (optimizedTriangleCount > Mathf.CeilToInt(targetTriangleCount * TargetSlack))
            {
                message +=
                    " Stopped above the requested " +
                    targetTriangleCount.ToString("N0") +
                    " tris to preserve mesh quality.";
            }

            result = new MeshSimplificationResult(
                true,
                originalVertexCount,
                optimizedVertexCount,
                originalTriangleCount,
                optimizedTriangleCount,
                message);
            return true;
        }

        private static bool TrySimplifyWithStagedQuadric(
            Mesh sourceMesh,
            SceneMeshOptimizerOptions options,
            out Mesh optimizedMesh,
            out MeshSimplificationResult result)
        {
            optimizedMesh = null;

            if (sourceMesh == null)
            {
                result = new MeshSimplificationResult(false, 0, 0, 0, 0, "Skipping Unknown Mesh: mesh reference is missing.");
                return false;
            }

            int originalTriangleCount = MeshStats.CountTriangles(sourceMesh);
            int originalVertexCount = sourceMesh.vertexCount;
            int finalTargetTriangleCount = Mathf.Max(1, Mathf.RoundToInt(originalTriangleCount * options.TargetTriangleRatio));
            int protectedTargetTriangleCount = Mathf.Max(
                finalTargetTriangleCount,
                Mathf.RoundToInt(originalTriangleCount * Mathf.Sqrt(options.TargetTriangleRatio)));

            Mesh currentMesh = sourceMesh;
            bool ownsCurrentMesh = false;
            bool anyStageSucceeded = false;
            string failureMessage = string.Empty;

            try
            {
                if (TryRunStage(
                    currentMesh,
                    options.WithTargetTriangleRatio(GetStageRatio(currentMesh, protectedTargetTriangleCount)),
                    out Mesh stageMesh,
                    out MeshSimplificationResult stageResult))
                {
                    currentMesh = ReplaceIntermediateMesh(currentMesh, stageMesh, ref ownsCurrentMesh);
                    anyStageSucceeded = true;
                }
                else
                {
                    failureMessage = stageResult.Message;
                }

                if (ShouldContinueReducing(currentMesh, finalTargetTriangleCount))
                {
                    if (TryRunStage(
                        currentMesh,
                        options.WithTargetTriangleRatio(GetStageRatio(currentMesh, finalTargetTriangleCount)),
                        out stageMesh,
                        out stageResult))
                    {
                        currentMesh = ReplaceIntermediateMesh(currentMesh, stageMesh, ref ownsCurrentMesh);
                        anyStageSucceeded = true;
                    }
                    else if (!anyStageSucceeded)
                    {
                        failureMessage = stageResult.Message;
                    }
                }

                if (options.UsesAnyPreservation && ShouldContinueReducing(currentMesh, finalTargetTriangleCount))
                {
                    SceneMeshOptimizerOptions relaxedOptions = options.WithRelaxedPreservation();
                    if (TryRunStage(
                        currentMesh,
                        relaxedOptions.WithTargetTriangleRatio(GetStageRatio(currentMesh, finalTargetTriangleCount)),
                        out stageMesh,
                        out stageResult))
                    {
                        currentMesh = ReplaceIntermediateMesh(currentMesh, stageMesh, ref ownsCurrentMesh);
                        anyStageSucceeded = true;
                    }
                    else if (!anyStageSucceeded)
                    {
                        failureMessage = stageResult.Message;
                    }
                }

                if (!anyStageSucceeded || !ownsCurrentMesh || currentMesh == null)
                {
                    result = new MeshSimplificationResult(
                        false,
                        originalVertexCount,
                        originalVertexCount,
                        originalTriangleCount,
                        originalTriangleCount,
                        string.IsNullOrEmpty(failureMessage)
                            ? "Skipping " + sourceMesh.name + ": the staged quadric pass did not find a meaningful reduction."
                            : failureMessage);
                    optimizedMesh = null;
                    return false;
                }

                int optimizedTriangleCount = MeshStats.CountTriangles(currentMesh);
                int optimizedVertexCount = currentMesh.vertexCount;
                bool reachedRequestedTarget = optimizedTriangleCount <= Mathf.CeilToInt(finalTargetTriangleCount * TargetSlack);

                string message =
                    sourceMesh.name + ": " +
                    originalTriangleCount.ToString("N0") + " -> " + optimizedTriangleCount.ToString("N0") + " tris, " +
                    originalVertexCount.ToString("N0") + " -> " + optimizedVertexCount.ToString("N0") + " verts via staged quadric edge collapse.";

                if (!reachedRequestedTarget)
                {
                    message +=
                        " Stopped above the requested " +
                        finalTargetTriangleCount.ToString("N0") +
                        " tris to avoid the artifact-prone fallback path.";
                }

                optimizedMesh = currentMesh;
                result = new MeshSimplificationResult(
                    true,
                    originalVertexCount,
                    optimizedVertexCount,
                    originalTriangleCount,
                    optimizedTriangleCount,
                    message);
                return true;
            }
            catch
            {
                if (ownsCurrentMesh && currentMesh != null)
                {
                    UnityEngine.Object.DestroyImmediate(currentMesh);
                }

                throw;
            }
        }

        private static SimplificationOptions BuildReferenceOptions(Mesh sourceMesh, SceneMeshOptimizerOptions options)
        {
            Bounds bounds = sourceMesh.bounds;
            float maximumAxisLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maximumAxisLength <= 0f)
            {
                maximumAxisLength = 1f;
            }

            double smartLinkDistance = Math.Max(maximumAxisLength * 0.000001d, 0.000000001d);

            return new SimplificationOptions
            {
                PreserveBorderEdges = true,
                PreserveUVSeamEdges = options.PreserveUvSeams || options.PreserveLightmapUvSeams,
                PreserveUVFoldoverEdges = options.PreserveUvSeams,
                PreserveSurfaceCurvature = options.PreserveHardEdges,
                EnableSmartLink = true,
                VertexLinkDistance = smartLinkDistance,
                MaxIterationCount = 100,
                Agressiveness = 7.0d,
                ManualUVComponentCount = false,
                UVComponentCount = 2
            };
        }

        private static void FinalizeMeshPostProcessing(Mesh optimizedMesh, Mesh sourceMesh, SceneMeshOptimizerOptions options)
        {
            if (optimizedMesh == null)
            {
                return;
            }

            if (options.RecalculateNormals || sourceMesh.normals == null || sourceMesh.normals.Length != sourceMesh.vertexCount)
            {
                optimizedMesh.RecalculateNormals();
            }

            if ((options.RecalculateTangents || sourceMesh.tangents == null || sourceMesh.tangents.Length != sourceMesh.vertexCount) &&
                MeshHasUv0(optimizedMesh))
            {
                optimizedMesh.RecalculateTangents();
            }

            optimizedMesh.RecalculateBounds();

            if (options.OptimizeMeshBuffers)
            {
                MeshUtility.Optimize(optimizedMesh);
            }
        }

        private static bool MeshHasUv0(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            List<Vector4> uvBuffer = new List<Vector4>();
            mesh.GetUVs(0, uvBuffer);
            return uvBuffer.Count == mesh.vertexCount;
        }

        private static bool TryRunStage(
            Mesh currentMesh,
            SceneMeshOptimizerOptions options,
            out Mesh stageMesh,
            out MeshSimplificationResult result)
        {
            stageMesh = null;

            int currentTriangleCount = MeshStats.CountTriangles(currentMesh);
            int targetTriangleCount = Mathf.Max(1, Mathf.RoundToInt(currentTriangleCount * options.TargetTriangleRatio));
            if (targetTriangleCount >= currentTriangleCount)
            {
                result = new MeshSimplificationResult(
                    false,
                    currentMesh.vertexCount,
                    currentMesh.vertexCount,
                    currentTriangleCount,
                    currentTriangleCount,
                    "Skipping " + currentMesh.name + ": staged target is already at the current mesh density.");
                return false;
            }

            return QuadricErrorMeshSimplifier.TrySimplify(currentMesh, options, out stageMesh, out result);
        }

        private static Mesh ReplaceIntermediateMesh(Mesh currentMesh, Mesh nextMesh, ref bool ownsCurrentMesh)
        {
            if (ownsCurrentMesh && currentMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(currentMesh);
            }

            ownsCurrentMesh = true;
            return nextMesh;
        }

        private static bool ShouldContinueReducing(Mesh mesh, int finalTargetTriangleCount)
        {
            if (mesh == null)
            {
                return false;
            }

            return MeshStats.CountTriangles(mesh) > Mathf.CeilToInt(finalTargetTriangleCount * TargetSlack);
        }

        private static float GetStageRatio(Mesh mesh, int absoluteTargetTriangleCount)
        {
            int currentTriangleCount = Mathf.Max(1, MeshStats.CountTriangles(mesh));
            float ratio = absoluteTargetTriangleCount / (float)currentTriangleCount;
            return Mathf.Clamp(ratio, 0.05f, 0.999f);
        }
    }

    internal static class QuadricErrorMeshSimplifier
    {
        private const float MinimumNormalDot = 0.6f;
        private const float MinimumTriangleArea = 0.000000000001f;
        private const float MinimumAreaRatio = 0.08f;
        private const float FeatureEdgeDotThreshold = 0.8f;
        private const double BoundaryWeight = 48d;
        private const double FeatureWeight = 28d;
        private const double ProtectedVertexPenalty = 4d;
        private const double BorderVertexPenalty = 2d;
        private const int MaxStalledPopFactor = 24;

        public static bool TrySimplify(
            Mesh sourceMesh,
            SceneMeshOptimizerOptions options,
            out Mesh optimizedMesh,
            out MeshSimplificationResult result)
        {
            optimizedMesh = null;

            if (!MeshSourceData.TryCreate(sourceMesh, out MeshSourceData sourceData, out string failureReason))
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceMesh != null ? sourceMesh.vertexCount : 0,
                    0,
                    MeshStats.CountTriangles(sourceMesh),
                    0,
                    "Skipping " + SafeMeshName(sourceMesh) + ": " + failureReason);
                return false;
            }

            int targetTriangleCount = Mathf.Max(1, Mathf.RoundToInt(sourceData.TriangleCount * options.TargetTriangleRatio));
            if (targetTriangleCount >= sourceData.TriangleCount)
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    sourceData.VertexCount,
                    sourceData.TriangleCount,
                    sourceData.TriangleCount,
                    "Skipping " + sourceMesh.name + ": target ratio is too close to the original mesh.");
                return false;
            }

            List<QemVertex> vertices = CreateVertices(sourceData, options);
            List<QemTriangle> triangles = CreateTriangles(sourceData, vertices, out int liveTriangleCount);
            if (liveTriangleCount <= 0)
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    0,
                    sourceData.TriangleCount,
                    0,
                    "Skipping " + sourceMesh.name + ": no valid triangles were found.");
                return false;
            }

            InitializeQuadrics(vertices, triangles, options);

            EdgeCollapseHeap heap = new EdgeCollapseHeap();
            int nextCandidateId = 1;
            EnqueueAllEdges(vertices, triangles, heap, ref nextCandidateId);

            int stalledPops = 0;
            int maxStalledPops = Mathf.Max(sourceData.TriangleCount * MaxStalledPopFactor, 1024);

            while (liveTriangleCount > targetTriangleCount && heap.Count > 0)
            {
                if (!heap.TryPop(out EdgeCollapseCandidate candidate))
                {
                    break;
                }

                if (!TryRefreshCandidate(candidate, vertices, triangles, out EdgeCollapseCandidate refreshedCandidate))
                {
                    stalledPops++;
                    if (stalledPops > maxStalledPops)
                    {
                        break;
                    }

                    continue;
                }

                stalledPops = 0;

                int removedTriangleCount = CollapseEdge(
                    refreshedCandidate.VertexA,
                    refreshedCandidate.VertexB,
                    refreshedCandidate.Position,
                    vertices,
                    triangles,
                    out List<int> touchedVertices);

                if (removedTriangleCount <= 0)
                {
                    continue;
                }

                liveTriangleCount -= removedTriangleCount;

                for (int i = 0; i < touchedVertices.Count; i++)
                {
                    int vertexIndex = touchedVertices[i];
                    if (vertexIndex < 0 || vertexIndex >= vertices.Count)
                    {
                        continue;
                    }

                    QemVertex vertex = vertices[vertexIndex];
                    if (!vertex.Alive)
                    {
                        continue;
                    }

                    vertex.Version++;
                    vertex.IsBorder = ComputeIsBorder(vertexIndex, vertices, triangles);
                }

                EnqueueAffectedEdges(touchedVertices, vertices, triangles, heap, ref nextCandidateId);
            }

            if (!BuildOptimizedMesh(sourceData, options, vertices, triangles, out optimizedMesh, out int optimizedVertexCount, out int optimizedTriangleCount))
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    0,
                    sourceData.TriangleCount,
                    0,
                    "Skipping " + sourceMesh.name + ": the edge-collapse pass could not build a valid result.");
                return false;
            }

            bool hasReduction =
                optimizedTriangleCount < sourceData.TriangleCount ||
                optimizedVertexCount < sourceData.VertexCount;

            if (!hasReduction)
            {
                UnityEngine.Object.DestroyImmediate(optimizedMesh);
                optimizedMesh = null;
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    sourceData.VertexCount,
                    sourceData.TriangleCount,
                    sourceData.TriangleCount,
                    "Skipping " + sourceMesh.name + ": quadric edge collapse did not find a meaningful reduction.");
                return false;
            }

            result = new MeshSimplificationResult(
                true,
                sourceData.VertexCount,
                optimizedVertexCount,
                sourceData.TriangleCount,
                optimizedTriangleCount,
                sourceMesh.name + ": " +
                sourceData.TriangleCount.ToString("N0") + " -> " + optimizedTriangleCount.ToString("N0") + " tris, " +
                sourceData.VertexCount.ToString("N0") + " -> " + optimizedVertexCount.ToString("N0") + " verts via quadric edge collapse.");

            return true;
        }

        private static List<QemVertex> CreateVertices(MeshSourceData sourceData, SceneMeshOptimizerOptions options)
        {
            List<QemVertex> vertices = new List<QemVertex>(sourceData.VertexCount);
            for (int vertexIndex = 0; vertexIndex < sourceData.VertexCount; vertexIndex++)
            {
                QemVertex vertex = new QemVertex(sourceData, vertexIndex);
                vertex.IsProtected = ShouldProtectCoincidentVertex(sourceData, options, vertexIndex);
                vertices.Add(vertex);
            }

            return vertices;
        }

        private static bool ShouldProtectCoincidentVertex(MeshSourceData sourceData, SceneMeshOptimizerOptions options, int vertexIndex)
        {
            return sourceData.HasCoincidentVertices[vertexIndex] && options.UsesAnyPreservation;
        }

        private static List<QemTriangle> CreateTriangles(MeshSourceData sourceData, List<QemVertex> vertices, out int liveTriangleCount)
        {
            List<QemTriangle> triangles = new List<QemTriangle>(sourceData.TriangleCount);
            liveTriangleCount = 0;

            for (int subMeshIndex = 0; subMeshIndex < sourceData.SubMeshIndices.Length; subMeshIndex++)
            {
                int[] indices = sourceData.SubMeshIndices[subMeshIndex];
                for (int index = 0; index < indices.Length; index += 3)
                {
                    int a = indices[index];
                    int b = indices[index + 1];
                    int c = indices[index + 2];

                    if (a == b || b == c || a == c)
                    {
                        continue;
                    }

                    QemTriangle triangle = new QemTriangle(a, b, c, subMeshIndex, vertices);
                    if (!triangle.Alive)
                    {
                        continue;
                    }

                    int triangleIndex = triangles.Count;
                    triangles.Add(triangle);
                    liveTriangleCount++;

                    AddTriangleConnectivity(vertices, triangleIndex, triangle);
                }
            }

            return triangles;
        }

        private static void InitializeQuadrics(List<QemVertex> vertices, List<QemTriangle> triangles, SceneMeshOptimizerOptions options)
        {
            Dictionary<EdgeKey, EdgeInfo> edges = BuildEdgeInfo(triangles);

            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                QemTriangle triangle = triangles[triangleIndex];
                if (!triangle.Alive)
                {
                    continue;
                }

                double faceWeight = Math.Max(Math.Sqrt(triangle.AreaSqr), 0.0001d);
                SimplifySymmetricMatrix faceQuadric = SimplifySymmetricMatrix.FromPlane(triangle.Normal, vertices[triangle.A].Position) * faceWeight;
                vertices[triangle.A].Quadric += faceQuadric;
                vertices[triangle.B].Quadric += faceQuadric;
                vertices[triangle.C].Quadric += faceQuadric;
            }

            foreach (KeyValuePair<EdgeKey, EdgeInfo> pair in edges)
            {
                EdgeKey edge = pair.Key;
                Vector3 pointA = vertices[edge.VertexA].Position;
                Vector3 pointB = vertices[edge.VertexB].Position;
                Vector3 edgeDirection = pointB - pointA;
                if (edgeDirection.sqrMagnitude <= 0f)
                {
                    continue;
                }

                edgeDirection.Normalize();

                if (pair.Value.Count == 1)
                {
                    QemTriangle triangle = triangles[pair.Value.TriangleIndexA];
                    if (!triangle.Alive)
                    {
                        continue;
                    }

                    vertices[edge.VertexA].IsBorder = true;
                    vertices[edge.VertexB].IsBorder = true;

                    Vector3 boundaryNormal = Vector3.Cross(edgeDirection, triangle.Normal);
                    if (boundaryNormal.sqrMagnitude <= 0f)
                    {
                        continue;
                    }

                    boundaryNormal.Normalize();
                    SimplifySymmetricMatrix boundaryQuadric = SimplifySymmetricMatrix.FromPlane(boundaryNormal, pointA) * BoundaryWeight;
                    vertices[edge.VertexA].Quadric += boundaryQuadric;
                    vertices[edge.VertexB].Quadric += boundaryQuadric;
                    continue;
                }

                if (pair.Value.Count != 2)
                {
                    continue;
                }

                QemTriangle triangleA = triangles[pair.Value.TriangleIndexA];
                QemTriangle triangleB = triangles[pair.Value.TriangleIndexB];
                if (!triangleA.Alive || !triangleB.Alive)
                {
                    continue;
                }

                bool isMaterialBoundary = triangleA.SubMesh != triangleB.SubMesh;
                bool isSharpFeature =
                    options.PreserveHardEdges &&
                    Vector3.Dot(triangleA.Normal, triangleB.Normal) < FeatureEdgeDotThreshold;
                if (!isMaterialBoundary && !isSharpFeature)
                {
                    continue;
                }

                vertices[edge.VertexA].IsProtected = true;
                vertices[edge.VertexB].IsProtected = true;

                Vector3 featureNormal = triangleA.Normal + triangleB.Normal;
                if (featureNormal.sqrMagnitude <= 0f)
                {
                    featureNormal = triangleA.Normal;
                }

                Vector3 featurePlaneNormal = Vector3.Cross(edgeDirection, featureNormal.normalized);
                if (featurePlaneNormal.sqrMagnitude <= 0f)
                {
                    continue;
                }

                featurePlaneNormal.Normalize();
                SimplifySymmetricMatrix featureQuadric = SimplifySymmetricMatrix.FromPlane(featurePlaneNormal, pointA) * FeatureWeight;
                vertices[edge.VertexA].Quadric += featureQuadric;
                vertices[edge.VertexB].Quadric += featureQuadric;
            }
        }

        private static Dictionary<EdgeKey, EdgeInfo> BuildEdgeInfo(List<QemTriangle> triangles)
        {
            Dictionary<EdgeKey, EdgeInfo> edges = new Dictionary<EdgeKey, EdgeInfo>();

            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                QemTriangle triangle = triangles[triangleIndex];
                if (!triangle.Alive)
                {
                    continue;
                }

                AddEdgeInfo(edges, new EdgeKey(triangle.A, triangle.B), triangleIndex);
                AddEdgeInfo(edges, new EdgeKey(triangle.B, triangle.C), triangleIndex);
                AddEdgeInfo(edges, new EdgeKey(triangle.C, triangle.A), triangleIndex);
            }

            return edges;
        }

        private static void AddEdgeInfo(Dictionary<EdgeKey, EdgeInfo> edges, EdgeKey key, int triangleIndex)
        {
            if (edges.TryGetValue(key, out EdgeInfo existing))
            {
                edges[key] = existing.AddTriangle(triangleIndex);
                return;
            }

            edges.Add(key, EdgeInfo.CreateFirst(triangleIndex));
        }

        private static void EnqueueAllEdges(
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            EdgeCollapseHeap heap,
            ref int nextCandidateId)
        {
            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                EnqueueIncidentEdges(vertexIndex, vertices, triangles, heap, ref nextCandidateId);
            }
        }

        private static void EnqueueAffectedEdges(
            List<int> touchedVertices,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            EdgeCollapseHeap heap,
            ref int nextCandidateId)
        {
            HashSet<int> visitedVertices = new HashSet<int>();

            for (int i = 0; i < touchedVertices.Count; i++)
            {
                int vertexIndex = touchedVertices[i];
                if (!visitedVertices.Add(vertexIndex))
                {
                    continue;
                }

                EnqueueIncidentEdges(vertexIndex, vertices, triangles, heap, ref nextCandidateId);
            }
        }

        private static void EnqueueIncidentEdges(
            int vertexIndex,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            EdgeCollapseHeap heap,
            ref int nextCandidateId)
        {
            if (vertexIndex < 0 || vertexIndex >= vertices.Count)
            {
                return;
            }

            QemVertex vertex = vertices[vertexIndex];
            if (!vertex.Alive)
            {
                return;
            }

            foreach (int neighborIndex in vertex.Neighbors)
            {
                if (neighborIndex == vertexIndex || neighborIndex < 0 || neighborIndex >= vertices.Count)
                {
                    continue;
                }

                if (!vertices[neighborIndex].Alive)
                {
                    continue;
                }

                int a = Mathf.Min(vertexIndex, neighborIndex);
                int b = Mathf.Max(vertexIndex, neighborIndex);

                if (!TryCreateCandidate(a, b, vertices, triangles, nextCandidateId, out EdgeCollapseCandidate candidate))
                {
                    continue;
                }

                heap.Push(candidate);
                nextCandidateId++;
            }
        }

        private static bool TryCreateCandidate(
            int vertexA,
            int vertexB,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            int candidateId,
            out EdgeCollapseCandidate candidate)
        {
            candidate = default;

            if (!TryEvaluateCollapse(vertexA, vertexB, vertices, triangles, out Vector3 collapsePosition, out double error))
            {
                return false;
            }

            candidate = new EdgeCollapseCandidate(
                vertexA,
                vertexB,
                collapsePosition,
                error,
                vertices[vertexA].Version,
                vertices[vertexB].Version,
                candidateId);

            return true;
        }

        private static bool TryRefreshCandidate(
            EdgeCollapseCandidate candidate,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            out EdgeCollapseCandidate refreshedCandidate)
        {
            refreshedCandidate = default;

            if (candidate.VertexA < 0 || candidate.VertexA >= vertices.Count || candidate.VertexB < 0 || candidate.VertexB >= vertices.Count)
            {
                return false;
            }

            QemVertex vertexA = vertices[candidate.VertexA];
            QemVertex vertexB = vertices[candidate.VertexB];

            if (!vertexA.Alive || !vertexB.Alive)
            {
                return false;
            }

            if (vertexA.Version != candidate.VersionA || vertexB.Version != candidate.VersionB)
            {
                return false;
            }

            if (!TryEvaluateCollapse(candidate.VertexA, candidate.VertexB, vertices, triangles, out Vector3 collapsePosition, out double error))
            {
                return false;
            }

            refreshedCandidate = new EdgeCollapseCandidate(
                candidate.VertexA,
                candidate.VertexB,
                collapsePosition,
                error,
                candidate.VersionA,
                candidate.VersionB,
                candidate.Id);

            return true;
        }

        private static bool TryEvaluateCollapse(
            int vertexA,
            int vertexB,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            out Vector3 collapsePosition,
            out double error)
        {
            collapsePosition = default;
            error = 0d;

            if (!HasSharedTriangle(vertexA, vertexB, vertices, triangles))
            {
                return false;
            }

            if (!HasValidCollapseTopology(vertexA, vertexB, vertices, triangles))
            {
                return false;
            }

            bool isBorderA = vertices[vertexA].IsBorder;
            bool isBorderB = vertices[vertexB].IsBorder;

            if (isBorderA != isBorderB)
            {
                return false;
            }

            if (isBorderA && !IsBoundaryEdge(vertexA, vertexB, vertices, triangles))
            {
                return false;
            }

            SimplifySymmetricMatrix quadric = vertices[vertexA].Quadric + vertices[vertexB].Quadric;
            collapsePosition = SelectCollapsePosition(vertexA, vertexB, quadric, vertices, isBorderA);
            error = quadric.Evaluate(collapsePosition);
            error *= CalculateCollapsePenalty(vertexA, vertexB, vertices);

            if (WouldFlipTriangles(vertexA, vertexB, collapsePosition, vertices, triangles))
            {
                return false;
            }

            return true;
        }

        private static Vector3 SelectCollapsePosition(
            int vertexA,
            int vertexB,
            SimplifySymmetricMatrix quadric,
            List<QemVertex> vertices,
            bool constrainToEdge)
        {
            Vector3 pointA = vertices[vertexA].Position;
            Vector3 pointB = vertices[vertexB].Position;
            Vector3 midpoint = (pointA + pointB) * 0.5f;

            if (constrainToEdge || !quadric.TrySolveOptimalPosition(out Vector3 optimalPosition))
            {
                return SelectLowestErrorPoint(quadric, pointA, pointB, midpoint);
            }

            Vector3 projectedOptimal = ProjectPointOntoSegment(pointA, pointB, optimalPosition);
            return SelectLowestErrorPoint(quadric, projectedOptimal, midpoint, pointA, pointB);
        }

        private static Vector3 SelectLowestErrorPoint(SimplifySymmetricMatrix quadric, params Vector3[] positions)
        {
            Vector3 bestPosition = positions[0];
            double bestError = quadric.Evaluate(bestPosition);

            for (int i = 1; i < positions.Length; i++)
            {
                double error = quadric.Evaluate(positions[i]);
                if (error < bestError)
                {
                    bestError = error;
                    bestPosition = positions[i];
                }
            }

            return bestPosition;
        }

        private static double CalculateCollapsePenalty(int vertexA, int vertexB, List<QemVertex> vertices)
        {
            double penalty = 1d;

            if (vertices[vertexA].IsBorder || vertices[vertexB].IsBorder)
            {
                penalty *= BorderVertexPenalty;
            }

            if (vertices[vertexA].IsProtected || vertices[vertexB].IsProtected)
            {
                penalty *= ProtectedVertexPenalty;
            }

            return penalty;
        }

        private static Vector3 ProjectPointOntoSegment(Vector3 pointA, Vector3 pointB, Vector3 point)
        {
            Vector3 edge = pointB - pointA;
            float edgeLengthSqr = edge.sqrMagnitude;
            if (edgeLengthSqr <= 0f)
            {
                return pointA;
            }

            float t = Vector3.Dot(point - pointA, edge) / edgeLengthSqr;
            t = Mathf.Clamp01(t);
            return pointA + (edge * t);
        }

        private static bool WouldFlipTriangles(
            int vertexA,
            int vertexB,
            Vector3 collapsePosition,
            List<QemVertex> vertices,
            List<QemTriangle> triangles)
        {
            HashSet<int> visitedTriangles = new HashSet<int>();

            foreach (int triangleIndex in vertices[vertexA].TriangleIds)
            {
                visitedTriangles.Add(triangleIndex);
            }

            foreach (int triangleIndex in vertices[vertexB].TriangleIds)
            {
                visitedTriangles.Add(triangleIndex);
            }

            foreach (int triangleIndex in visitedTriangles)
            {
                if (triangleIndex < 0 || triangleIndex >= triangles.Count)
                {
                    continue;
                }

                QemTriangle triangle = triangles[triangleIndex];
                if (!triangle.Alive)
                {
                    continue;
                }

                bool containsA = triangle.Contains(vertexA);
                bool containsB = triangle.Contains(vertexB);
                if (containsA && containsB)
                {
                    continue;
                }

                Vector3 a = triangle.A == vertexA || triangle.A == vertexB ? collapsePosition : vertices[triangle.A].Position;
                Vector3 b = triangle.B == vertexA || triangle.B == vertexB ? collapsePosition : vertices[triangle.B].Position;
                Vector3 c = triangle.C == vertexA || triangle.C == vertexB ? collapsePosition : vertices[triangle.C].Position;

                Vector3 newNormal = Vector3.Cross(b - a, c - a);
                float area = newNormal.sqrMagnitude;
                if (area <= MinimumTriangleArea)
                {
                    return true;
                }

                if (triangle.AreaSqr > 0f && area < triangle.AreaSqr * MinimumAreaRatio)
                {
                    return true;
                }

                newNormal.Normalize();
                if (Vector3.Dot(newNormal, triangle.Normal) < MinimumNormalDot)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSharedTriangle(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            HashSet<int> source = vertices[vertexA].TriangleIds.Count <= vertices[vertexB].TriangleIds.Count
                ? vertices[vertexA].TriangleIds
                : vertices[vertexB].TriangleIds;

            HashSet<int> target = ReferenceEquals(source, vertices[vertexA].TriangleIds)
                ? vertices[vertexB].TriangleIds
                : vertices[vertexA].TriangleIds;

            foreach (int triangleIndex in source)
            {
                if (!target.Contains(triangleIndex))
                {
                    continue;
                }

                if (triangles[triangleIndex].Alive)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBoundaryEdge(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            return CountSharedAliveTriangles(vertexA, vertexB, vertices, triangles) == 1;
        }

        private static bool HasValidCollapseTopology(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            int sharedTriangleCount = CountSharedAliveTriangles(vertexA, vertexB, vertices, triangles);
            if (sharedTriangleCount <= 0 || sharedTriangleCount > 2)
            {
                return false;
            }

            int commonNeighborCount = CountCommonAliveNeighbors(vertexA, vertexB, vertices, triangles);
            return commonNeighborCount == sharedTriangleCount;
        }

        private static int CountSharedAliveTriangles(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            int sharedCount = 0;

            HashSet<int> source = vertices[vertexA].TriangleIds.Count <= vertices[vertexB].TriangleIds.Count
                ? vertices[vertexA].TriangleIds
                : vertices[vertexB].TriangleIds;

            HashSet<int> target = ReferenceEquals(source, vertices[vertexA].TriangleIds)
                ? vertices[vertexB].TriangleIds
                : vertices[vertexA].TriangleIds;

            foreach (int triangleIndex in source)
            {
                if (!target.Contains(triangleIndex))
                {
                    continue;
                }

                if (!triangles[triangleIndex].Alive)
                {
                    continue;
                }

                sharedCount++;
            }

            return sharedCount;
        }

        private static int CountCommonAliveNeighbors(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            int commonCount = 0;

            HashSet<int> source = vertices[vertexA].Neighbors.Count <= vertices[vertexB].Neighbors.Count
                ? vertices[vertexA].Neighbors
                : vertices[vertexB].Neighbors;

            HashSet<int> target = ReferenceEquals(source, vertices[vertexA].Neighbors)
                ? vertices[vertexB].Neighbors
                : vertices[vertexA].Neighbors;

            foreach (int neighborIndex in source)
            {
                if (neighborIndex == vertexA || neighborIndex == vertexB)
                {
                    continue;
                }

                if (!target.Contains(neighborIndex))
                {
                    continue;
                }

                if (neighborIndex < 0 || neighborIndex >= vertices.Count || !vertices[neighborIndex].Alive)
                {
                    continue;
                }

                if (!SharesAliveTriangle(vertexA, neighborIndex, vertices, triangles))
                {
                    continue;
                }

                if (!SharesAliveTriangle(vertexB, neighborIndex, vertices, triangles))
                {
                    continue;
                }

                commonCount++;
            }

            return commonCount;
        }

        private static bool SharesAliveTriangle(int vertexA, int vertexB, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            HashSet<int> source = vertices[vertexA].TriangleIds.Count <= vertices[vertexB].TriangleIds.Count
                ? vertices[vertexA].TriangleIds
                : vertices[vertexB].TriangleIds;

            HashSet<int> target = ReferenceEquals(source, vertices[vertexA].TriangleIds)
                ? vertices[vertexB].TriangleIds
                : vertices[vertexA].TriangleIds;

            foreach (int triangleIndex in source)
            {
                if (!target.Contains(triangleIndex))
                {
                    continue;
                }

                if (triangleIndex < 0 || triangleIndex >= triangles.Count || !triangles[triangleIndex].Alive)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool ComputeIsBorder(int vertexIndex, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            if (vertexIndex < 0 || vertexIndex >= vertices.Count || !vertices[vertexIndex].Alive)
            {
                return false;
            }

            foreach (int neighborIndex in vertices[vertexIndex].Neighbors)
            {
                if (neighborIndex < 0 || neighborIndex >= vertices.Count || !vertices[neighborIndex].Alive)
                {
                    continue;
                }

                if (IsBoundaryEdge(vertexIndex, neighborIndex, vertices, triangles))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CollapseEdge(
            int vertexA,
            int vertexB,
            Vector3 collapsePosition,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            out List<int> touchedVertices)
        {
            touchedVertices = new List<int>();

            if (vertexA == vertexB || vertexA < 0 || vertexA >= vertices.Count || vertexB < 0 || vertexB >= vertices.Count)
            {
                return 0;
            }

            int keepIndex = Mathf.Min(vertexA, vertexB);
            int removeIndex = Mathf.Max(vertexA, vertexB);

            QemVertex keepVertex = vertices[keepIndex];
            QemVertex removeVertex = vertices[removeIndex];

            if (!keepVertex.Alive || !removeVertex.Alive)
            {
                return 0;
            }

            HashSet<int> triangleSet = new HashSet<int>();
            foreach (int triangleIndex in keepVertex.TriangleIds)
            {
                triangleSet.Add(triangleIndex);
            }

            foreach (int triangleIndex in removeVertex.TriangleIds)
            {
                triangleSet.Add(triangleIndex);
            }

            HashSet<int> affectedVertices = new HashSet<int>();

            keepVertex.Position = collapsePosition;
            keepVertex.Quadric += removeVertex.Quadric;
            keepVertex.Attributes.Merge(removeVertex.Attributes);

            int removedTriangleCount = 0;

            foreach (int triangleIndex in triangleSet)
            {
                if (triangleIndex < 0 || triangleIndex >= triangles.Count)
                {
                    continue;
                }

                QemTriangle triangle = triangles[triangleIndex];
                if (!triangle.Alive)
                {
                    continue;
                }

                affectedVertices.Add(triangle.A);
                affectedVertices.Add(triangle.B);
                affectedVertices.Add(triangle.C);

                bool changed = triangle.Replace(removeIndex, keepIndex);
                if (!changed && !triangle.Contains(keepIndex))
                {
                    continue;
                }

                if (triangle.IsDegenerate || !triangle.Recalculate(vertices))
                {
                    MarkTriangleDead(triangleIndex, vertices, triangles);
                    removedTriangleCount++;
                    continue;
                }

                keepVertex.TriangleIds.Add(triangleIndex);
                removeVertex.TriangleIds.Remove(triangleIndex);

                affectedVertices.Add(triangle.A);
                affectedVertices.Add(triangle.B);
                affectedVertices.Add(triangle.C);
            }

            removeVertex.Alive = false;
            removeVertex.TriangleIds.Clear();
            removeVertex.Neighbors.Clear();

            affectedVertices.Add(keepIndex);
            affectedVertices.Add(removeIndex);
            touchedVertices.AddRange(affectedVertices);

            for (int i = 0; i < touchedVertices.Count; i++)
            {
                int vertexIndex = touchedVertices[i];
                if (vertexIndex < 0 || vertexIndex >= vertices.Count)
                {
                    continue;
                }

                if (!vertices[vertexIndex].Alive)
                {
                    vertices[vertexIndex].Neighbors.Clear();
                    continue;
                }

                RebuildVertexNeighborhood(vertexIndex, vertices, triangles);
            }

            return removedTriangleCount;
        }

        private static void MarkTriangleDead(int triangleIndex, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            if (triangleIndex < 0 || triangleIndex >= triangles.Count)
            {
                return;
            }

            QemTriangle triangle = triangles[triangleIndex];
            if (!triangle.Alive)
            {
                return;
            }

            triangle.Alive = false;
            RemoveTriangleConnectivity(vertices, triangleIndex, triangle);
        }

        private static void AddTriangleConnectivity(List<QemVertex> vertices, int triangleIndex, QemTriangle triangle)
        {
            vertices[triangle.A].TriangleIds.Add(triangleIndex);
            vertices[triangle.B].TriangleIds.Add(triangleIndex);
            vertices[triangle.C].TriangleIds.Add(triangleIndex);

            vertices[triangle.A].Neighbors.Add(triangle.B);
            vertices[triangle.A].Neighbors.Add(triangle.C);
            vertices[triangle.B].Neighbors.Add(triangle.A);
            vertices[triangle.B].Neighbors.Add(triangle.C);
            vertices[triangle.C].Neighbors.Add(triangle.A);
            vertices[triangle.C].Neighbors.Add(triangle.B);
        }

        private static void RemoveTriangleConnectivity(List<QemVertex> vertices, int triangleIndex, QemTriangle triangle)
        {
            vertices[triangle.A].TriangleIds.Remove(triangleIndex);
            vertices[triangle.B].TriangleIds.Remove(triangleIndex);
            vertices[triangle.C].TriangleIds.Remove(triangleIndex);
        }

        private static void RebuildVertexNeighborhood(int vertexIndex, List<QemVertex> vertices, List<QemTriangle> triangles)
        {
            QemVertex vertex = vertices[vertexIndex];
            vertex.Neighbors.Clear();

            List<int> invalidTriangleIndices = null;

            foreach (int triangleIndex in vertex.TriangleIds)
            {
                if (triangleIndex < 0 || triangleIndex >= triangles.Count || !triangles[triangleIndex].Alive)
                {
                    if (invalidTriangleIndices == null)
                    {
                        invalidTriangleIndices = new List<int>();
                    }

                    invalidTriangleIndices.Add(triangleIndex);
                    continue;
                }

                QemTriangle triangle = triangles[triangleIndex];
                if (triangle.A != vertexIndex)
                {
                    vertex.Neighbors.Add(triangle.A);
                }

                if (triangle.B != vertexIndex)
                {
                    vertex.Neighbors.Add(triangle.B);
                }

                if (triangle.C != vertexIndex)
                {
                    vertex.Neighbors.Add(triangle.C);
                }
            }

            if (invalidTriangleIndices == null)
            {
                return;
            }

            for (int i = 0; i < invalidTriangleIndices.Count; i++)
            {
                vertex.TriangleIds.Remove(invalidTriangleIndices[i]);
            }
        }

        private static bool BuildOptimizedMesh(
            MeshSourceData sourceData,
            SceneMeshOptimizerOptions options,
            List<QemVertex> vertices,
            List<QemTriangle> triangles,
            out Mesh optimizedMesh,
            out int optimizedVertexCount,
            out int optimizedTriangleCount)
        {
            optimizedMesh = null;
            optimizedVertexCount = 0;
            optimizedTriangleCount = 0;

            int[] remap = new int[vertices.Count];
            for (int i = 0; i < remap.Length; i++)
            {
                remap[i] = -1;
            }

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                if (!vertices[vertexIndex].Alive || vertices[vertexIndex].TriangleIds.Count == 0)
                {
                    continue;
                }

                remap[vertexIndex] = optimizedVertexCount;
                optimizedVertexCount++;
            }

            if (optimizedVertexCount <= 0)
            {
                return false;
            }

            Vector3[] positions = new Vector3[optimizedVertexCount];
            Vector3[] normals = sourceData.HasNormals ? new Vector3[optimizedVertexCount] : null;
            Vector4[] tangents = sourceData.HasTangents ? new Vector4[optimizedVertexCount] : null;
            Color[] colors = sourceData.HasColors ? new Color[optimizedVertexCount] : null;
            Vector4[][] uvChannels = new Vector4[sourceData.UvChannels.Length][];
            BoneWeight[] boneWeights = sourceData.HasBoneWeights ? new BoneWeight[optimizedVertexCount] : null;

            for (int channelIndex = 0; channelIndex < sourceData.UvChannels.Length; channelIndex++)
            {
                if (sourceData.UvChannels[channelIndex] == null)
                {
                    continue;
                }

                uvChannels[channelIndex] = new Vector4[optimizedVertexCount];
            }

            for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
            {
                int outputIndex = remap[vertexIndex];
                if (outputIndex < 0)
                {
                    continue;
                }

                QemVertex vertex = vertices[vertexIndex];
                positions[outputIndex] = vertex.Position;

                if (normals != null)
                {
                    normals[outputIndex] = vertex.Attributes.GetAverageNormal();
                }

                if (tangents != null)
                {
                    tangents[outputIndex] = vertex.Attributes.GetAverageTangent();
                }

                if (colors != null)
                {
                    colors[outputIndex] = vertex.Attributes.GetAverageColor();
                }

                for (int channelIndex = 0; channelIndex < uvChannels.Length; channelIndex++)
                {
                    if (uvChannels[channelIndex] == null)
                    {
                        continue;
                    }

                    uvChannels[channelIndex][outputIndex] = vertex.Attributes.GetAverageUv(channelIndex);
                }

                if (boneWeights != null)
                {
                    boneWeights[outputIndex] = vertex.Attributes.GetAverageBoneWeight();
                }
            }

            SnapCoincidentOutputVertices(sourceData, remap, positions);

            List<int>[] subMeshIndices = new List<int>[sourceData.SubMeshIndices.Length];
            HashSet<TriangleIndexKey>[] uniqueTriangles = new HashSet<TriangleIndexKey>[sourceData.SubMeshIndices.Length];
            for (int subMeshIndex = 0; subMeshIndex < subMeshIndices.Length; subMeshIndex++)
            {
                subMeshIndices[subMeshIndex] = new List<int>();
                uniqueTriangles[subMeshIndex] = new HashSet<TriangleIndexKey>();
            }

            for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
            {
                QemTriangle triangle = triangles[triangleIndex];
                if (!triangle.Alive)
                {
                    continue;
                }

                int a = remap[triangle.A];
                int b = remap[triangle.B];
                int c = remap[triangle.C];

                if (a < 0 || b < 0 || c < 0 || a == b || b == c || a == c)
                {
                    continue;
                }

                if (Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]).sqrMagnitude <= MinimumTriangleArea)
                {
                    continue;
                }

                TriangleIndexKey triangleKey = new TriangleIndexKey(a, b, c);
                if (!uniqueTriangles[triangle.SubMesh].Add(triangleKey))
                {
                    continue;
                }

                subMeshIndices[triangle.SubMesh].Add(a);
                subMeshIndices[triangle.SubMesh].Add(b);
                subMeshIndices[triangle.SubMesh].Add(c);
                optimizedTriangleCount++;
            }

            if (optimizedTriangleCount <= 0)
            {
                return false;
            }

            CompactVertexData(
                ref optimizedVertexCount,
                ref positions,
                ref normals,
                ref tangents,
                ref colors,
                ref uvChannels,
                ref boneWeights,
                subMeshIndices);

            optimizedMesh = new Mesh
            {
                name = sourceData.SourceMesh.name + "_Optimized"
            };

            optimizedMesh.indexFormat = optimizedVertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            optimizedMesh.vertices = positions;
            optimizedMesh.subMeshCount = subMeshIndices.Length;

            if (colors != null)
            {
                optimizedMesh.colors = colors;
            }

            if (normals != null && !options.RecalculateNormals)
            {
                optimizedMesh.normals = normals;
            }

            if (tangents != null && !options.RecalculateTangents)
            {
                optimizedMesh.tangents = tangents;
            }

            for (int channelIndex = 0; channelIndex < uvChannels.Length; channelIndex++)
            {
                if (uvChannels[channelIndex] == null)
                {
                    continue;
                }

                optimizedMesh.SetUVs(channelIndex, new List<Vector4>(uvChannels[channelIndex]));
            }

            if (boneWeights != null)
            {
                optimizedMesh.boneWeights = boneWeights;
                optimizedMesh.bindposes = sourceData.BindPoses;
            }

            for (int subMeshIndex = 0; subMeshIndex < subMeshIndices.Length; subMeshIndex++)
            {
                optimizedMesh.SetIndices(subMeshIndices[subMeshIndex], MeshTopology.Triangles, subMeshIndex, false);
            }

            if (options.RecalculateNormals || !sourceData.HasNormals)
            {
                optimizedMesh.RecalculateNormals();
            }

            if ((options.RecalculateTangents || !sourceData.HasTangents) && sourceData.HasUv0)
            {
                optimizedMesh.RecalculateTangents();
            }

            optimizedMesh.RecalculateBounds();

            if (options.OptimizeMeshBuffers)
            {
                MeshUtility.Optimize(optimizedMesh);
            }

            return true;
        }

        private static void SnapCoincidentOutputVertices(MeshSourceData sourceData, int[] remap, Vector3[] positions)
        {
            if (sourceData.PositionGroupMembers == null || sourceData.PositionGroupMembers.Length == 0)
            {
                return;
            }

            HashSet<int> uniqueOutputIndices = new HashSet<int>();

            for (int groupIndex = 0; groupIndex < sourceData.PositionGroupMembers.Length; groupIndex++)
            {
                int[] groupMembers = sourceData.PositionGroupMembers[groupIndex];
                if (groupMembers == null || groupMembers.Length <= 1)
                {
                    continue;
                }

                uniqueOutputIndices.Clear();
                Vector3 snappedPositionSum = Vector3.zero;

                for (int i = 0; i < groupMembers.Length; i++)
                {
                    int sourceVertexIndex = groupMembers[i];
                    if (sourceVertexIndex < 0 || sourceVertexIndex >= remap.Length)
                    {
                        continue;
                    }

                    int outputIndex = remap[sourceVertexIndex];
                    if (outputIndex < 0 || outputIndex >= positions.Length || !uniqueOutputIndices.Add(outputIndex))
                    {
                        continue;
                    }

                    snappedPositionSum += positions[outputIndex];
                }

                if (uniqueOutputIndices.Count <= 1)
                {
                    continue;
                }

                Vector3 snappedPosition = snappedPositionSum / uniqueOutputIndices.Count;
                foreach (int outputIndex in uniqueOutputIndices)
                {
                    positions[outputIndex] = snappedPosition;
                }
            }
        }

        private static void CompactVertexData(
            ref int vertexCount,
            ref Vector3[] positions,
            ref Vector3[] normals,
            ref Vector4[] tangents,
            ref Color[] colors,
            ref Vector4[][] uvChannels,
            ref BoneWeight[] boneWeights,
            List<int>[] subMeshIndices)
        {
            bool[] usedVertices = new bool[vertexCount];

            for (int subMeshIndex = 0; subMeshIndex < subMeshIndices.Length; subMeshIndex++)
            {
                List<int> indices = subMeshIndices[subMeshIndex];
                for (int index = 0; index < indices.Count; index++)
                {
                    int vertexIndex = indices[index];
                    if (vertexIndex >= 0 && vertexIndex < usedVertices.Length)
                    {
                        usedVertices[vertexIndex] = true;
                    }
                }
            }

            int[] compactRemap = new int[vertexCount];
            Array.Fill(compactRemap, -1);

            int compactVertexCount = 0;
            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                if (!usedVertices[vertexIndex])
                {
                    continue;
                }

                compactRemap[vertexIndex] = compactVertexCount;
                compactVertexCount++;
            }

            if (compactVertexCount == vertexCount)
            {
                return;
            }

            Vector3[] compactPositions = new Vector3[compactVertexCount];
            Vector3[] compactNormals = normals != null ? new Vector3[compactVertexCount] : null;
            Vector4[] compactTangents = tangents != null ? new Vector4[compactVertexCount] : null;
            Color[] compactColors = colors != null ? new Color[compactVertexCount] : null;
            BoneWeight[] compactBoneWeights = boneWeights != null ? new BoneWeight[compactVertexCount] : null;
            Vector4[][] compactUvChannels = null;

            if (uvChannels != null)
            {
                compactUvChannels = new Vector4[uvChannels.Length][];
                for (int channelIndex = 0; channelIndex < uvChannels.Length; channelIndex++)
                {
                    if (uvChannels[channelIndex] == null)
                    {
                        continue;
                    }

                    compactUvChannels[channelIndex] = new Vector4[compactVertexCount];
                }
            }

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                int compactIndex = compactRemap[vertexIndex];
                if (compactIndex < 0)
                {
                    continue;
                }

                compactPositions[compactIndex] = positions[vertexIndex];

                if (compactNormals != null)
                {
                    compactNormals[compactIndex] = normals[vertexIndex];
                }

                if (compactTangents != null)
                {
                    compactTangents[compactIndex] = tangents[vertexIndex];
                }

                if (compactColors != null)
                {
                    compactColors[compactIndex] = colors[vertexIndex];
                }

                if (compactBoneWeights != null)
                {
                    compactBoneWeights[compactIndex] = boneWeights[vertexIndex];
                }

                if (compactUvChannels == null)
                {
                    continue;
                }

                for (int channelIndex = 0; channelIndex < compactUvChannels.Length; channelIndex++)
                {
                    if (compactUvChannels[channelIndex] == null)
                    {
                        continue;
                    }

                    compactUvChannels[channelIndex][compactIndex] = uvChannels[channelIndex][vertexIndex];
                }
            }

            for (int subMeshIndex = 0; subMeshIndex < subMeshIndices.Length; subMeshIndex++)
            {
                List<int> indices = subMeshIndices[subMeshIndex];
                for (int index = 0; index < indices.Count; index++)
                {
                    indices[index] = compactRemap[indices[index]];
                }
            }

            vertexCount = compactVertexCount;
            positions = compactPositions;
            normals = compactNormals;
            tangents = compactTangents;
            colors = compactColors;
            uvChannels = compactUvChannels ?? Array.Empty<Vector4[]>();
            boneWeights = compactBoneWeights;
        }

        private static string SafeMeshName(Mesh mesh)
        {
            return mesh != null ? mesh.name : "Unknown Mesh";
        }
    }

    internal readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly int VertexA;
        public readonly int VertexB;

        public EdgeKey(int vertexA, int vertexB)
        {
            VertexA = Mathf.Min(vertexA, vertexB);
            VertexB = Mathf.Max(vertexA, vertexB);
        }

        public bool Equals(EdgeKey other)
        {
            return VertexA == other.VertexA && VertexB == other.VertexB;
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (VertexA * 397) ^ VertexB;
            }
        }
    }

    internal readonly struct TriangleIndexKey : IEquatable<TriangleIndexKey>
    {
        private readonly int a;
        private readonly int b;
        private readonly int c;

        public TriangleIndexKey(int first, int second, int third)
        {
            a = Mathf.Min(first, Mathf.Min(second, third));
            c = Mathf.Max(first, Mathf.Max(second, third));
            b = first + second + third - a - c;
        }

        public bool Equals(TriangleIndexKey other)
        {
            return a == other.a && b == other.b && c == other.c;
        }

        public override bool Equals(object obj)
        {
            return obj is TriangleIndexKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + a;
                hash = (hash * 31) + b;
                hash = (hash * 31) + c;
                return hash;
            }
        }
    }

    internal readonly struct EdgeInfo
    {
        public readonly int Count;
        public readonly int TriangleIndexA;
        public readonly int TriangleIndexB;

        public EdgeInfo(int count, int triangleIndexA, int triangleIndexB)
        {
            Count = count;
            TriangleIndexA = triangleIndexA;
            TriangleIndexB = triangleIndexB;
        }

        public static EdgeInfo CreateFirst(int triangleIndex)
        {
            return new EdgeInfo(1, triangleIndex, -1);
        }

        public EdgeInfo AddTriangle(int triangleIndex)
        {
            if (Count == 1)
            {
                return new EdgeInfo(2, TriangleIndexA, triangleIndex);
            }

            return new EdgeInfo(Count + 1, TriangleIndexA, TriangleIndexB);
        }
    }

    internal readonly struct EdgeCollapseCandidate
    {
        public readonly int VertexA;
        public readonly int VertexB;
        public readonly Vector3 Position;
        public readonly double Error;
        public readonly int VersionA;
        public readonly int VersionB;
        public readonly int Id;

        public EdgeCollapseCandidate(int vertexA, int vertexB, Vector3 position, double error, int versionA, int versionB, int id)
        {
            VertexA = vertexA;
            VertexB = vertexB;
            Position = position;
            Error = error;
            VersionA = versionA;
            VersionB = versionB;
            Id = id;
        }
    }

    internal sealed class EdgeCollapseHeap
    {
        private readonly List<EdgeCollapseCandidate> items = new List<EdgeCollapseCandidate>();

        public int Count
        {
            get { return items.Count; }
        }

        public void Push(EdgeCollapseCandidate item)
        {
            items.Add(item);
            SiftUp(items.Count - 1);
        }

        public bool TryPop(out EdgeCollapseCandidate item)
        {
            if (items.Count == 0)
            {
                item = default;
                return false;
            }

            item = items[0];
            int lastIndex = items.Count - 1;
            items[0] = items[lastIndex];
            items.RemoveAt(lastIndex);

            if (items.Count > 0)
            {
                SiftDown(0);
            }

            return true;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (Compare(items[index], items[parentIndex]) >= 0)
                {
                    break;
                }

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int leftChild = index * 2 + 1;
                int rightChild = leftChild + 1;
                int smallest = index;

                if (leftChild < items.Count && Compare(items[leftChild], items[smallest]) < 0)
                {
                    smallest = leftChild;
                }

                if (rightChild < items.Count && Compare(items[rightChild], items[smallest]) < 0)
                {
                    smallest = rightChild;
                }

                if (smallest == index)
                {
                    break;
                }

                Swap(index, smallest);
                index = smallest;
            }
        }

        private int Compare(EdgeCollapseCandidate left, EdgeCollapseCandidate right)
        {
            int comparison = left.Error.CompareTo(right.Error);
            if (comparison != 0)
            {
                return comparison;
            }

            return left.Id.CompareTo(right.Id);
        }

        private void Swap(int leftIndex, int rightIndex)
        {
            EdgeCollapseCandidate item = items[leftIndex];
            items[leftIndex] = items[rightIndex];
            items[rightIndex] = item;
        }
    }

    internal struct SimplifySymmetricMatrix
    {
        private readonly double m00;
        private readonly double m01;
        private readonly double m02;
        private readonly double m03;
        private readonly double m11;
        private readonly double m12;
        private readonly double m13;
        private readonly double m22;
        private readonly double m23;
        private readonly double m33;

        private SimplifySymmetricMatrix(
            double m00,
            double m01,
            double m02,
            double m03,
            double m11,
            double m12,
            double m13,
            double m22,
            double m23,
            double m33)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m03 = m03;
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;
            this.m22 = m22;
            this.m23 = m23;
            this.m33 = m33;
        }

        public static SimplifySymmetricMatrix FromPlane(Vector3 normal, Vector3 point)
        {
            double a = normal.x;
            double b = normal.y;
            double c = normal.z;
            double d = -Vector3.Dot(normal, point);

            return new SimplifySymmetricMatrix(
                a * a,
                a * b,
                a * c,
                a * d,
                b * b,
                b * c,
                b * d,
                c * c,
                c * d,
                d * d);
        }

        public double Evaluate(Vector3 position)
        {
            double x = position.x;
            double y = position.y;
            double z = position.z;

            return
                (m00 * x * x) +
                (2d * m01 * x * y) +
                (2d * m02 * x * z) +
                (2d * m03 * x) +
                (m11 * y * y) +
                (2d * m12 * y * z) +
                (2d * m13 * y) +
                (m22 * z * z) +
                (2d * m23 * z) +
                m33;
        }

        public bool TrySolveOptimalPosition(out Vector3 position)
        {
            double determinant =
                (m00 * ((m11 * m22) - (m12 * m12))) -
                (m01 * ((m01 * m22) - (m12 * m02))) +
                (m02 * ((m01 * m12) - (m11 * m02)));

            if (Math.Abs(determinant) <= 0.000000000001d)
            {
                position = default;
                return false;
            }

            double inverseDeterminant = 1d / determinant;

            double i00 = ((m11 * m22) - (m12 * m12)) * inverseDeterminant;
            double i01 = ((m02 * m12) - (m01 * m22)) * inverseDeterminant;
            double i02 = ((m01 * m12) - (m02 * m11)) * inverseDeterminant;
            double i11 = ((m00 * m22) - (m02 * m02)) * inverseDeterminant;
            double i12 = ((m02 * m01) - (m00 * m12)) * inverseDeterminant;
            double i22 = ((m00 * m11) - (m01 * m01)) * inverseDeterminant;

            double bx = -m03;
            double by = -m13;
            double bz = -m23;

            position = new Vector3(
                (float)((i00 * bx) + (i01 * by) + (i02 * bz)),
                (float)((i01 * bx) + (i11 * by) + (i12 * bz)),
                (float)((i02 * bx) + (i12 * by) + (i22 * bz)));

            return true;
        }

        public static SimplifySymmetricMatrix operator +(SimplifySymmetricMatrix left, SimplifySymmetricMatrix right)
        {
            return new SimplifySymmetricMatrix(
                left.m00 + right.m00,
                left.m01 + right.m01,
                left.m02 + right.m02,
                left.m03 + right.m03,
                left.m11 + right.m11,
                left.m12 + right.m12,
                left.m13 + right.m13,
                left.m22 + right.m22,
                left.m23 + right.m23,
                left.m33 + right.m33);
        }

        public static SimplifySymmetricMatrix operator *(SimplifySymmetricMatrix value, double scale)
        {
            return new SimplifySymmetricMatrix(
                value.m00 * scale,
                value.m01 * scale,
                value.m02 * scale,
                value.m03 * scale,
                value.m11 * scale,
                value.m12 * scale,
                value.m13 * scale,
                value.m22 * scale,
                value.m23 * scale,
                value.m33 * scale);
        }
    }

    internal sealed class QemVertex
    {
        public readonly HashSet<int> TriangleIds = new HashSet<int>();
        public readonly HashSet<int> Neighbors = new HashSet<int>();
        public readonly VertexAttributeAccumulator Attributes;
        public Vector3 Position;
        public SimplifySymmetricMatrix Quadric;
        public bool Alive = true;
        public bool IsBorder;
        public bool IsProtected;
        public int Version;

        public QemVertex(MeshSourceData sourceData, int vertexIndex)
        {
            Position = sourceData.Vertices[vertexIndex];
            Quadric = default;
            Attributes = new VertexAttributeAccumulator(sourceData, vertexIndex);
            IsProtected = false;
        }
    }

    internal sealed class QemTriangle
    {
        public int A;
        public int B;
        public int C;
        public readonly int SubMesh;
        public bool Alive;
        public Vector3 Normal;
        public float AreaSqr;

        public bool IsDegenerate
        {
            get { return A == B || B == C || A == C; }
        }

        public QemTriangle(int a, int b, int c, int subMesh, List<QemVertex> vertices)
        {
            A = a;
            B = b;
            C = c;
            SubMesh = subMesh;
            Alive = Recalculate(vertices);
        }

        public bool Contains(int vertexIndex)
        {
            return A == vertexIndex || B == vertexIndex || C == vertexIndex;
        }

        public bool Replace(int fromVertex, int toVertex)
        {
            bool changed = false;

            if (A == fromVertex)
            {
                A = toVertex;
                changed = true;
            }

            if (B == fromVertex)
            {
                B = toVertex;
                changed = true;
            }

            if (C == fromVertex)
            {
                C = toVertex;
                changed = true;
            }

            return changed;
        }

        public bool Recalculate(List<QemVertex> vertices)
        {
            if (IsDegenerate)
            {
                Normal = Vector3.up;
                AreaSqr = 0f;
                return false;
            }

            Vector3 ab = vertices[B].Position - vertices[A].Position;
            Vector3 ac = vertices[C].Position - vertices[A].Position;
            Vector3 normal = Vector3.Cross(ab, ac);
            float areaSqr = normal.sqrMagnitude;
            if (areaSqr <= 0.000000000001f)
            {
                Normal = Vector3.up;
                AreaSqr = 0f;
                return false;
            }

            normal.Normalize();
            Normal = normal;
            AreaSqr = areaSqr;
            Alive = true;
            return true;
        }
    }

    internal sealed class VertexAttributeAccumulator
    {
        private readonly bool hasNormals;
        private readonly bool hasTangents;
        private readonly bool hasColors;
        private readonly bool hasBoneWeights;
        private readonly bool[] hasUvChannels;
        private readonly Vector4[] uvSums;
        private readonly Dictionary<int, float> boneWeights = new Dictionary<int, float>(4);

        private Vector3 normalSum;
        private Vector3 tangentSum;
        private float tangentHandednessSum;
        private Color colorSum;
        private int sampleCount;

        public VertexAttributeAccumulator(MeshSourceData sourceData, int vertexIndex)
        {
            hasNormals = sourceData.HasNormals;
            hasTangents = sourceData.HasTangents;
            hasColors = sourceData.HasColors;
            hasBoneWeights = sourceData.HasBoneWeights;
            hasUvChannels = new bool[sourceData.UvChannels.Length];
            uvSums = new Vector4[sourceData.UvChannels.Length];

            sampleCount = 1;

            if (hasNormals)
            {
                normalSum = sourceData.Normals[vertexIndex];
            }

            if (hasTangents)
            {
                Vector4 tangent = sourceData.Tangents[vertexIndex];
                tangentSum = new Vector3(tangent.x, tangent.y, tangent.z);
                tangentHandednessSum = tangent.w;
            }

            if (hasColors)
            {
                colorSum = sourceData.Colors[vertexIndex];
            }

            for (int channelIndex = 0; channelIndex < sourceData.UvChannels.Length; channelIndex++)
            {
                if (sourceData.UvChannels[channelIndex] == null)
                {
                    continue;
                }

                hasUvChannels[channelIndex] = true;
                uvSums[channelIndex] = sourceData.UvChannels[channelIndex][vertexIndex];
            }

            if (!hasBoneWeights)
            {
                return;
            }

            BoneWeight sourceWeight = sourceData.BoneWeights[vertexIndex];
            AddBoneWeight(sourceWeight.boneIndex0, sourceWeight.weight0);
            AddBoneWeight(sourceWeight.boneIndex1, sourceWeight.weight1);
            AddBoneWeight(sourceWeight.boneIndex2, sourceWeight.weight2);
            AddBoneWeight(sourceWeight.boneIndex3, sourceWeight.weight3);
        }

        public void Merge(VertexAttributeAccumulator other)
        {
            sampleCount += other.sampleCount;
            normalSum += other.normalSum;
            tangentSum += other.tangentSum;
            tangentHandednessSum += other.tangentHandednessSum;
            colorSum += other.colorSum;

            for (int channelIndex = 0; channelIndex < uvSums.Length; channelIndex++)
            {
                if (!hasUvChannels[channelIndex] && !other.hasUvChannels[channelIndex])
                {
                    continue;
                }

                hasUvChannels[channelIndex] = hasUvChannels[channelIndex] || other.hasUvChannels[channelIndex];
                uvSums[channelIndex] += other.uvSums[channelIndex];
            }

            foreach (KeyValuePair<int, float> pair in other.boneWeights)
            {
                AddBoneWeight(pair.Key, pair.Value);
            }
        }

        public Vector3 GetAverageNormal()
        {
            if (!hasNormals)
            {
                return Vector3.up;
            }

            Vector3 averageNormal = normalSum / Mathf.Max(1, sampleCount);
            if (averageNormal.sqrMagnitude <= 0f)
            {
                return Vector3.up;
            }

            return averageNormal.normalized;
        }

        public Vector4 GetAverageTangent()
        {
            if (!hasTangents)
            {
                return new Vector4(1f, 0f, 0f, 1f);
            }

            Vector3 averageTangent = tangentSum / Mathf.Max(1, sampleCount);
            if (averageTangent.sqrMagnitude <= 0f)
            {
                averageTangent = Vector3.right;
            }
            else
            {
                averageTangent.Normalize();
            }

            float handedness = tangentHandednessSum >= 0f ? 1f : -1f;
            return new Vector4(averageTangent.x, averageTangent.y, averageTangent.z, handedness);
        }

        public Color GetAverageColor()
        {
            if (!hasColors)
            {
                return Color.white;
            }

            return colorSum * (1f / Mathf.Max(1, sampleCount));
        }

        public Vector4 GetAverageUv(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= uvSums.Length || !hasUvChannels[channelIndex])
            {
                return Vector4.zero;
            }

            return uvSums[channelIndex] / Mathf.Max(1, sampleCount);
        }

        public BoneWeight GetAverageBoneWeight()
        {
            if (!hasBoneWeights || boneWeights.Count == 0)
            {
                return default;
            }

            List<KeyValuePair<int, float>> sortedWeights = new List<KeyValuePair<int, float>>(boneWeights);
            sortedWeights.Sort((left, right) => right.Value.CompareTo(left.Value));

            float totalWeight = 0f;
            int weightCount = Mathf.Min(4, sortedWeights.Count);
            for (int i = 0; i < weightCount; i++)
            {
                totalWeight += sortedWeights[i].Value;
            }

            if (totalWeight <= 0f)
            {
                return default;
            }

            BoneWeight averagedWeight = default;

            if (weightCount > 0)
            {
                averagedWeight.boneIndex0 = sortedWeights[0].Key;
                averagedWeight.weight0 = sortedWeights[0].Value / totalWeight;
            }

            if (weightCount > 1)
            {
                averagedWeight.boneIndex1 = sortedWeights[1].Key;
                averagedWeight.weight1 = sortedWeights[1].Value / totalWeight;
            }

            if (weightCount > 2)
            {
                averagedWeight.boneIndex2 = sortedWeights[2].Key;
                averagedWeight.weight2 = sortedWeights[2].Value / totalWeight;
            }

            if (weightCount > 3)
            {
                averagedWeight.boneIndex3 = sortedWeights[3].Key;
                averagedWeight.weight3 = sortedWeights[3].Value / totalWeight;
            }

            return averagedWeight;
        }

        private void AddBoneWeight(int boneIndex, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (boneWeights.TryGetValue(boneIndex, out float currentWeight))
            {
                boneWeights[boneIndex] = currentWeight + weight;
                return;
            }

            boneWeights.Add(boneIndex, weight);
        }
    }

    internal static class VertexClusterMeshSimplifier
    {
        private const float MinimumCellSize = 0.00001f;

        public static bool TrySimplify(
            Mesh sourceMesh,
            SceneMeshOptimizerOptions options,
            out Mesh optimizedMesh,
            out MeshSimplificationResult result)
        {
            optimizedMesh = null;

            if (!MeshSourceData.TryCreate(sourceMesh, out MeshSourceData sourceData, out string failureReason))
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceMesh != null ? sourceMesh.vertexCount : 0,
                    0,
                    MeshStats.CountTriangles(sourceMesh),
                    0,
                    "Skipping " + SafeMeshName(sourceMesh) + ": " + failureReason);
                return false;
            }

            int targetTriangleCount = Mathf.Max(1, Mathf.RoundToInt(sourceData.TriangleCount * options.TargetTriangleRatio));
            SimplificationEstimate estimate = FindEstimate(sourceData, options, targetTriangleCount);

            if (estimate.TriangleCount <= 0 || estimate.VertexCount <= 0)
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    0,
                    sourceData.TriangleCount,
                    0,
                    "Skipping " + sourceMesh.name + ": simplification collapsed all geometry.");
                return false;
            }

            if (estimate.TriangleCount >= sourceData.TriangleCount &&
                estimate.VertexCount >= sourceData.VertexCount &&
                options.TargetTriangleRatio < 0.999f)
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    sourceData.VertexCount,
                    sourceData.TriangleCount,
                    sourceData.TriangleCount,
                    "Skipping " + sourceMesh.name + ": no meaningful reduction was found with the current preservation settings.");
                return false;
            }

            if (!BuildSimplifiedMesh(sourceData, options, estimate.CellSize, out optimizedMesh, out int optimizedVertexCount, out int optimizedTriangleCount))
            {
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    0,
                    sourceData.TriangleCount,
                    0,
                    "Skipping " + sourceMesh.name + ": failed to build the simplified mesh.");
                return false;
            }

            bool hasReduction =
                optimizedTriangleCount < sourceData.TriangleCount ||
                optimizedVertexCount < sourceData.VertexCount;

            if (!hasReduction && options.TargetTriangleRatio < 0.999f)
            {
                UnityEngine.Object.DestroyImmediate(optimizedMesh);
                optimizedMesh = null;
                result = new MeshSimplificationResult(
                    false,
                    sourceData.VertexCount,
                    sourceData.VertexCount,
                    sourceData.TriangleCount,
                    sourceData.TriangleCount,
                    "Skipping " + sourceMesh.name + ": simplification produced the same mesh density.");
                return false;
            }

            result = new MeshSimplificationResult(
                true,
                sourceData.VertexCount,
                optimizedVertexCount,
                sourceData.TriangleCount,
                optimizedTriangleCount,
                sourceMesh.name + ": " +
                sourceData.TriangleCount.ToString("N0") + " -> " + optimizedTriangleCount.ToString("N0") + " tris, " +
                sourceData.VertexCount.ToString("N0") + " -> " + optimizedVertexCount.ToString("N0") + " verts.");

            return true;
        }

        private static SimplificationEstimate FindEstimate(MeshSourceData sourceData, SceneMeshOptimizerOptions options, int targetTriangleCount)
        {
            float minimumCellSize = Mathf.Max(MinimumCellSize, sourceData.MaximumAxisLength / 4096f);
            float maximumCellSize = Mathf.Max(minimumCellSize * 2f, sourceData.MaximumAxisLength * 1.25f);

            SimplificationEstimate bestEstimate = Estimate(sourceData, options, maximumCellSize);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                float middleCellSize = (minimumCellSize + maximumCellSize) * 0.5f;
                SimplificationEstimate middleEstimate = Estimate(sourceData, options, middleCellSize);

                if (middleEstimate.TriangleCount <= targetTriangleCount)
                {
                    bestEstimate = middleEstimate;
                    maximumCellSize = middleCellSize;
                }
                else
                {
                    minimumCellSize = middleCellSize;
                }
            }

            return bestEstimate;
        }

        private static SimplificationEstimate Estimate(MeshSourceData sourceData, SceneMeshOptimizerOptions options, float cellSize)
        {
            Dictionary<VertexClusterKey, int> clusters = new Dictionary<VertexClusterKey, int>(sourceData.VertexCount);
            int[] remap = new int[sourceData.VertexCount];
            int nextClusterIndex = 0;

            for (int vertexIndex = 0; vertexIndex < sourceData.VertexCount; vertexIndex++)
            {
                VertexClusterKey key = BuildClusterKey(sourceData, options, vertexIndex, cellSize);
                if (!clusters.TryGetValue(key, out int clusterIndex))
                {
                    clusterIndex = nextClusterIndex++;
                    clusters.Add(key, clusterIndex);
                }

                remap[vertexIndex] = clusterIndex;
            }

            int triangleCount = 0;
            for (int subMeshIndex = 0; subMeshIndex < sourceData.SubMeshIndices.Length; subMeshIndex++)
            {
                int[] indices = sourceData.SubMeshIndices[subMeshIndex];
                for (int index = 0; index < indices.Length; index += 3)
                {
                    int a = remap[indices[index]];
                    int b = remap[indices[index + 1]];
                    int c = remap[indices[index + 2]];

                    if (a == b || b == c || a == c)
                    {
                        continue;
                    }

                    triangleCount++;
                }
            }

            return new SimplificationEstimate(cellSize, nextClusterIndex, triangleCount);
        }

        private static bool BuildSimplifiedMesh(
            MeshSourceData sourceData,
            SceneMeshOptimizerOptions options,
            float cellSize,
            out Mesh optimizedMesh,
            out int optimizedVertexCount,
            out int optimizedTriangleCount)
        {
            Dictionary<VertexClusterKey, int> clusterLookup = new Dictionary<VertexClusterKey, int>(sourceData.VertexCount);
            List<VertexClusterAccumulator> accumulators = new List<VertexClusterAccumulator>(sourceData.VertexCount);
            int[] remap = new int[sourceData.VertexCount];

            for (int vertexIndex = 0; vertexIndex < sourceData.VertexCount; vertexIndex++)
            {
                VertexClusterKey key = BuildClusterKey(sourceData, options, vertexIndex, cellSize);

                if (!clusterLookup.TryGetValue(key, out int clusterIndex))
                {
                    clusterIndex = accumulators.Count;
                    clusterLookup.Add(key, clusterIndex);
                    accumulators.Add(new VertexClusterAccumulator(sourceData));
                }

                remap[vertexIndex] = clusterIndex;
                accumulators[clusterIndex].AddVertex(sourceData, vertexIndex);
            }

            optimizedVertexCount = accumulators.Count;
            optimizedTriangleCount = 0;

            Vector3[] vertices = new Vector3[optimizedVertexCount];
            Vector3[] normals = sourceData.HasNormals ? new Vector3[optimizedVertexCount] : null;
            Vector4[] tangents = sourceData.HasTangents ? new Vector4[optimizedVertexCount] : null;
            Color[] colors = sourceData.HasColors ? new Color[optimizedVertexCount] : null;
            Vector4[][] uvChannels = CreateUvChannelArray(sourceData.UvChannels.Length, optimizedVertexCount, sourceData);
            BoneWeight[] boneWeights = sourceData.HasBoneWeights ? new BoneWeight[optimizedVertexCount] : null;

            for (int clusterIndex = 0; clusterIndex < accumulators.Count; clusterIndex++)
            {
                VertexClusterAccumulator accumulator = accumulators[clusterIndex];
                vertices[clusterIndex] = accumulator.GetAveragePosition();

                if (normals != null)
                {
                    normals[clusterIndex] = accumulator.GetAverageNormal();
                }

                if (tangents != null)
                {
                    tangents[clusterIndex] = accumulator.GetAverageTangent();
                }

                if (colors != null)
                {
                    colors[clusterIndex] = accumulator.GetAverageColor();
                }

                for (int uvChannel = 0; uvChannel < uvChannels.Length; uvChannel++)
                {
                    if (uvChannels[uvChannel] == null)
                    {
                        continue;
                    }

                    uvChannels[uvChannel][clusterIndex] = accumulator.GetAverageUv(uvChannel);
                }

                if (boneWeights != null)
                {
                    boneWeights[clusterIndex] = accumulator.GetAverageBoneWeight();
                }
            }

            int[][] simplifiedSubMeshes = new int[sourceData.SubMeshIndices.Length][];

            for (int subMeshIndex = 0; subMeshIndex < sourceData.SubMeshIndices.Length; subMeshIndex++)
            {
                int[] sourceIndices = sourceData.SubMeshIndices[subMeshIndex];
                List<int> simplifiedIndices = new List<int>(sourceIndices.Length);

                for (int index = 0; index < sourceIndices.Length; index += 3)
                {
                    int a = remap[sourceIndices[index]];
                    int b = remap[sourceIndices[index + 1]];
                    int c = remap[sourceIndices[index + 2]];

                    if (a == b || b == c || a == c)
                    {
                        continue;
                    }

                    if (IsDegenerateTriangle(vertices[a], vertices[b], vertices[c]))
                    {
                        continue;
                    }

                    simplifiedIndices.Add(a);
                    simplifiedIndices.Add(b);
                    simplifiedIndices.Add(c);
                }

                optimizedTriangleCount += simplifiedIndices.Count / 3;
                simplifiedSubMeshes[subMeshIndex] = simplifiedIndices.ToArray();
            }

            if (optimizedTriangleCount <= 0)
            {
                optimizedMesh = null;
                return false;
            }

            optimizedMesh = new Mesh
            {
                name = sourceData.SourceMesh.name + "_Optimized"
            };

            optimizedMesh.indexFormat = optimizedVertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            optimizedMesh.vertices = vertices;
            optimizedMesh.subMeshCount = sourceData.SubMeshIndices.Length;

            if (colors != null)
            {
                optimizedMesh.colors = colors;
            }

            if (normals != null && !options.RecalculateNormals)
            {
                optimizedMesh.normals = normals;
            }

            if (tangents != null && !options.RecalculateTangents)
            {
                optimizedMesh.tangents = tangents;
            }

            for (int uvChannel = 0; uvChannel < uvChannels.Length; uvChannel++)
            {
                if (uvChannels[uvChannel] == null)
                {
                    continue;
                }

                optimizedMesh.SetUVs(uvChannel, new List<Vector4>(uvChannels[uvChannel]));
            }

            if (boneWeights != null)
            {
                optimizedMesh.boneWeights = boneWeights;
                optimizedMesh.bindposes = sourceData.BindPoses;
            }

            for (int subMeshIndex = 0; subMeshIndex < simplifiedSubMeshes.Length; subMeshIndex++)
            {
                optimizedMesh.SetIndices(simplifiedSubMeshes[subMeshIndex], MeshTopology.Triangles, subMeshIndex, false);
            }

            if (options.RecalculateNormals || !sourceData.HasNormals)
            {
                optimizedMesh.RecalculateNormals();
            }

            if ((options.RecalculateTangents || !sourceData.HasTangents) && sourceData.HasUv0)
            {
                optimizedMesh.RecalculateTangents();
            }

            optimizedMesh.RecalculateBounds();

            if (options.OptimizeMeshBuffers)
            {
                MeshUtility.Optimize(optimizedMesh);
            }

            return true;
        }

        private static Vector4[][] CreateUvChannelArray(int channelCount, int vertexCount, MeshSourceData sourceData)
        {
            Vector4[][] uvChannels = new Vector4[channelCount][];

            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                if (sourceData.UvChannels[channelIndex] == null)
                {
                    continue;
                }

                uvChannels[channelIndex] = new Vector4[vertexCount];
            }

            return uvChannels;
        }

        private static VertexClusterKey BuildClusterKey(
            MeshSourceData sourceData,
            SceneMeshOptimizerOptions options,
            int vertexIndex,
            float cellSize)
        {
            Vector3 vertex = sourceData.Vertices[vertexIndex];
            float inverseCellSize = 1f / Mathf.Max(MinimumCellSize, cellSize);

            int cellX = Mathf.FloorToInt((vertex.x - sourceData.BoundsMin.x) * inverseCellSize);
            int cellY = Mathf.FloorToInt((vertex.y - sourceData.BoundsMin.y) * inverseCellSize);
            int cellZ = Mathf.FloorToInt((vertex.z - sourceData.BoundsMin.z) * inverseCellSize);

            int hardEdgeGroup = options.PreserveHardEdges ? sourceData.HardEdgeGroups[vertexIndex] : 0;
            int uv0Group = options.PreserveUvSeams ? sourceData.Uv0SeamGroups[vertexIndex] : 0;
            int uv1Group = options.PreserveLightmapUvSeams ? sourceData.Uv1SeamGroups[vertexIndex] : 0;
            int skinningGroup = options.PreserveSkinningBoundaries ? sourceData.SkinningGroups[vertexIndex] : 0;
            // Keep seam-split twins from being averaged with unrelated nearby vertices.
            int positionGroup = sourceData.HasCoincidentVertices[vertexIndex] ? sourceData.PositionGroupIds[vertexIndex] + 1 : 0;

            return new VertexClusterKey(
                cellX,
                cellY,
                cellZ,
                hardEdgeGroup,
                uv0Group,
                uv1Group,
                skinningGroup,
                positionGroup);
        }

        private static bool IsDegenerateTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            return Vector3.Cross(ab, ac).sqrMagnitude <= 0.00000001f;
        }

        private static string SafeMeshName(Mesh mesh)
        {
            return mesh != null ? mesh.name : "Unknown Mesh";
        }
    }

    internal readonly struct SimplificationEstimate
    {
        public readonly float CellSize;
        public readonly int VertexCount;
        public readonly int TriangleCount;

        public SimplificationEstimate(float cellSize, int vertexCount, int triangleCount)
        {
            CellSize = cellSize;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
        }
    }

    internal readonly struct VertexClusterKey : IEquatable<VertexClusterKey>
    {
        private readonly int cellX;
        private readonly int cellY;
        private readonly int cellZ;
        private readonly int hardEdgeGroup;
        private readonly int uv0Group;
        private readonly int uv1Group;
        private readonly int skinningGroup;
        private readonly int positionGroup;

        public VertexClusterKey(
            int cellX,
            int cellY,
            int cellZ,
            int hardEdgeGroup,
            int uv0Group,
            int uv1Group,
            int skinningGroup,
            int positionGroup)
        {
            this.cellX = cellX;
            this.cellY = cellY;
            this.cellZ = cellZ;
            this.hardEdgeGroup = hardEdgeGroup;
            this.uv0Group = uv0Group;
            this.uv1Group = uv1Group;
            this.skinningGroup = skinningGroup;
            this.positionGroup = positionGroup;
        }

        public bool Equals(VertexClusterKey other)
        {
            return
                cellX == other.cellX &&
                cellY == other.cellY &&
                cellZ == other.cellZ &&
                hardEdgeGroup == other.hardEdgeGroup &&
                uv0Group == other.uv0Group &&
                uv1Group == other.uv1Group &&
                skinningGroup == other.skinningGroup &&
                positionGroup == other.positionGroup;
        }

        public override bool Equals(object obj)
        {
            return obj is VertexClusterKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + cellX;
                hash = (hash * 31) + cellY;
                hash = (hash * 31) + cellZ;
                hash = (hash * 31) + hardEdgeGroup;
                hash = (hash * 31) + uv0Group;
                hash = (hash * 31) + uv1Group;
                hash = (hash * 31) + skinningGroup;
                hash = (hash * 31) + positionGroup;
                return hash;
            }
        }
    }

    internal readonly struct VertexPositionKey : IEquatable<VertexPositionKey>
    {
        private const float QuantizationScale = 100000f;

        private readonly int x;
        private readonly int y;
        private readonly int z;

        public VertexPositionKey(Vector3 position)
        {
            x = Mathf.RoundToInt(position.x * QuantizationScale);
            y = Mathf.RoundToInt(position.y * QuantizationScale);
            z = Mathf.RoundToInt(position.z * QuantizationScale);
        }

        public bool Equals(VertexPositionKey other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is VertexPositionKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + x;
                hash = (hash * 31) + y;
                hash = (hash * 31) + z;
                return hash;
            }
        }
    }

    internal sealed class MeshSourceData
    {
        public readonly Mesh SourceMesh;
        public readonly Vector3[] Vertices;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;
        public readonly Color[] Colors;
        public readonly Vector4[][] UvChannels;
        public readonly BoneWeight[] BoneWeights;
        public readonly Matrix4x4[] BindPoses;
        public readonly int[][] SubMeshIndices;
        public readonly bool[] HasCoincidentVertices;
        public readonly int[] PositionGroupIds;
        public readonly int[][] PositionGroupMembers;
        public readonly int[] HardEdgeGroups;
        public readonly int[] Uv0SeamGroups;
        public readonly int[] Uv1SeamGroups;
        public readonly int[] SkinningGroups;
        public readonly Vector3 BoundsMin;
        public readonly float MaximumAxisLength;
        public readonly int VertexCount;
        public readonly int TriangleCount;

        public bool HasNormals
        {
            get { return Normals != null; }
        }

        public bool HasTangents
        {
            get { return Tangents != null; }
        }

        public bool HasColors
        {
            get { return Colors != null; }
        }

        public bool HasBoneWeights
        {
            get { return BoneWeights != null; }
        }

        public bool HasUv0
        {
            get { return UvChannels.Length > 0 && UvChannels[0] != null; }
        }

        public bool HasUv1
        {
            get { return UvChannels.Length > 1 && UvChannels[1] != null; }
        }

        private MeshSourceData(
            Mesh sourceMesh,
            Vector3[] vertices,
            Vector3[] normals,
            Vector4[] tangents,
            Color[] colors,
            Vector4[][] uvChannels,
            BoneWeight[] boneWeights,
            Matrix4x4[] bindPoses,
            int[][] subMeshIndices,
            bool[] hasCoincidentVertices,
            int[] positionGroupIds,
            int[][] positionGroupMembers,
            int[] hardEdgeGroups,
            int[] uv0SeamGroups,
            int[] uv1SeamGroups,
            int[] skinningGroups,
            Vector3 boundsMin,
            float maximumAxisLength,
            int triangleCount)
        {
            SourceMesh = sourceMesh;
            Vertices = vertices;
            Normals = normals;
            Tangents = tangents;
            Colors = colors;
            UvChannels = uvChannels;
            BoneWeights = boneWeights;
            BindPoses = bindPoses;
            SubMeshIndices = subMeshIndices;
            HasCoincidentVertices = hasCoincidentVertices;
            PositionGroupIds = positionGroupIds;
            PositionGroupMembers = positionGroupMembers;
            HardEdgeGroups = hardEdgeGroups;
            Uv0SeamGroups = uv0SeamGroups;
            Uv1SeamGroups = uv1SeamGroups;
            SkinningGroups = skinningGroups;
            BoundsMin = boundsMin;
            MaximumAxisLength = maximumAxisLength;
            TriangleCount = triangleCount;
            VertexCount = vertices.Length;
        }

        public static bool TryCreate(Mesh sourceMesh, out MeshSourceData sourceData, out string failureReason)
        {
            sourceData = null;
            failureReason = string.Empty;

            if (sourceMesh == null)
            {
                failureReason = "mesh reference is missing.";
                return false;
            }

            if (!sourceMesh.isReadable)
            {
                failureReason = "mesh is not readable.";
                return false;
            }

            if (sourceMesh.blendShapeCount > 0)
            {
                failureReason = "blend shapes are not supported by this simplifier.";
                return false;
            }

            Vector3[] vertices = sourceMesh.vertices;
            if (vertices == null || vertices.Length < 3)
            {
                failureReason = "mesh has too few vertices to simplify.";
                return false;
            }

            Vector3[] normals = sourceMesh.normals;
            if (normals == null || normals.Length != vertices.Length)
            {
                normals = null;
            }

            Vector4[] tangents = sourceMesh.tangents;
            if (tangents == null || tangents.Length != vertices.Length)
            {
                tangents = null;
            }

            Color[] colors = sourceMesh.colors;
            if (colors == null || colors.Length != vertices.Length)
            {
                colors = null;
            }

            BoneWeight[] boneWeights = sourceMesh.boneWeights;
            if (boneWeights == null || boneWeights.Length != vertices.Length)
            {
                boneWeights = null;
            }

            Matrix4x4[] bindPoses = sourceMesh.bindposes;
            if (boneWeights == null)
            {
                bindPoses = Array.Empty<Matrix4x4>();
            }

            Vector4[][] uvChannels = ReadUvChannels(sourceMesh, vertices.Length);

            int[][] subMeshIndices = new int[sourceMesh.subMeshCount][];
            int triangleCount = 0;

            for (int subMeshIndex = 0; subMeshIndex < sourceMesh.subMeshCount; subMeshIndex++)
            {
                if (sourceMesh.GetTopology(subMeshIndex) != MeshTopology.Triangles)
                {
                    failureReason = "submesh " + subMeshIndex + " does not use triangle topology.";
                    return false;
                }

                int[] indices = sourceMesh.GetIndices(subMeshIndex);
                if (indices.Length == 0)
                {
                    subMeshIndices[subMeshIndex] = Array.Empty<int>();
                    continue;
                }

                subMeshIndices[subMeshIndex] = indices;
                triangleCount += indices.Length / 3;
            }

            if (triangleCount == 0)
            {
                failureReason = "mesh has no triangles.";
                return false;
            }

            Bounds bounds = BuildBounds(vertices);
            float maximumAxisLength = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maximumAxisLength <= 0f)
            {
                maximumAxisLength = 1f;
            }

            Dictionary<VertexPositionKey, List<int>> positionGroups = GroupVerticesByPosition(vertices);
            bool[] hasCoincidentVertices = new bool[vertices.Length];
            int[] positionGroupIds = new int[vertices.Length];
            int[][] positionGroupMembers = new int[positionGroups.Count][];
            int nextPositionGroupIndex = 0;

            foreach (KeyValuePair<VertexPositionKey, List<int>> pair in positionGroups)
            {
                int[] groupMembers = pair.Value.ToArray();
                positionGroupMembers[nextPositionGroupIndex] = groupMembers;
                bool hasCoincidentGroupMembers = groupMembers.Length > 1;

                for (int index = 0; index < groupMembers.Length; index++)
                {
                    int vertexIndex = groupMembers[index];
                    positionGroupIds[vertexIndex] = nextPositionGroupIndex;
                    hasCoincidentVertices[vertexIndex] = hasCoincidentGroupMembers;
                }

                nextPositionGroupIndex++;
            }

            int[] hardEdgeGroups = BuildAttributeGroups(positionGroups, vertices.Length, (left, right) => NormalsMatch(normals, left, right));
            int[] uv0SeamGroups = BuildAttributeGroups(positionGroups, vertices.Length, (left, right) => UvsMatch(uvChannels, 0, left, right));
            int[] uv1SeamGroups = BuildAttributeGroups(positionGroups, vertices.Length, (left, right) => UvsMatch(uvChannels, 1, left, right));
            int[] skinningGroups = BuildAttributeGroups(positionGroups, vertices.Length, (left, right) => BoneWeightsMatch(boneWeights, left, right));

            sourceData = new MeshSourceData(
                sourceMesh,
                vertices,
                normals,
                tangents,
                colors,
                uvChannels,
                boneWeights,
                bindPoses,
                subMeshIndices,
                hasCoincidentVertices,
                positionGroupIds,
                positionGroupMembers,
                hardEdgeGroups,
                uv0SeamGroups,
                uv1SeamGroups,
                skinningGroups,
                bounds.min,
                maximumAxisLength,
                triangleCount);

            return true;
        }

        private static Vector4[][] ReadUvChannels(Mesh mesh, int vertexCount)
        {
            const int UvChannelCount = 8;
            Vector4[][] uvChannels = new Vector4[UvChannelCount][];

            for (int channelIndex = 0; channelIndex < UvChannelCount; channelIndex++)
            {
                List<Vector4> sourceUvs = new List<Vector4>();
                mesh.GetUVs(channelIndex, sourceUvs);

                if (sourceUvs.Count != vertexCount)
                {
                    continue;
                }

                uvChannels[channelIndex] = sourceUvs.ToArray();
            }

            return uvChannels;
        }

        private static Dictionary<VertexPositionKey, List<int>> GroupVerticesByPosition(Vector3[] vertices)
        {
            Dictionary<VertexPositionKey, List<int>> positionGroups = new Dictionary<VertexPositionKey, List<int>>();

            for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                VertexPositionKey key = new VertexPositionKey(vertices[vertexIndex]);
                if (!positionGroups.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>(2);
                    positionGroups.Add(key, indices);
                }

                indices.Add(vertexIndex);
            }

            return positionGroups;
        }

        private static int[] BuildAttributeGroups(
            Dictionary<VertexPositionKey, List<int>> positionGroups,
            int vertexCount,
            Func<int, int, bool> attributesMatch)
        {
            int[] groups = new int[vertexCount];

            foreach (KeyValuePair<VertexPositionKey, List<int>> pair in positionGroups)
            {
                List<int> indices = pair.Value;
                if (indices.Count <= 1)
                {
                    continue;
                }

                List<int> representatives = new List<int>(indices.Count);
                int nextLocalGroup = 1;

                for (int i = 0; i < indices.Count; i++)
                {
                    int vertexIndex = indices[i];
                    bool assigned = false;

                    for (int representativeIndex = 0; representativeIndex < representatives.Count; representativeIndex++)
                    {
                        int representativeVertex = representatives[representativeIndex];
                        if (!attributesMatch(vertexIndex, representativeVertex))
                        {
                            continue;
                        }

                        groups[vertexIndex] = groups[representativeVertex];
                        assigned = true;
                        break;
                    }

                    if (assigned)
                    {
                        continue;
                    }

                    groups[vertexIndex] = nextLocalGroup;
                    nextLocalGroup++;
                    representatives.Add(vertexIndex);
                }
            }

            return groups;
        }

        private static bool NormalsMatch(Vector3[] normals, int leftIndex, int rightIndex)
        {
            if (normals == null)
            {
                return true;
            }

            const float hardEdgeDotThreshold = 0.999f;
            return Vector3.Dot(normals[leftIndex].normalized, normals[rightIndex].normalized) >= hardEdgeDotThreshold;
        }

        private static bool UvsMatch(Vector4[][] uvChannels, int channelIndex, int leftIndex, int rightIndex)
        {
            if (uvChannels == null || channelIndex < 0 || channelIndex >= uvChannels.Length || uvChannels[channelIndex] == null)
            {
                return true;
            }

            const float uvTolerance = 0.0001f;
            Vector4 leftUv = uvChannels[channelIndex][leftIndex];
            Vector4 rightUv = uvChannels[channelIndex][rightIndex];
            return (leftUv - rightUv).sqrMagnitude <= uvTolerance * uvTolerance;
        }

        private static bool BoneWeightsMatch(BoneWeight[] boneWeights, int leftIndex, int rightIndex)
        {
            if (boneWeights == null)
            {
                return true;
            }

            BoneWeight left = boneWeights[leftIndex];
            BoneWeight right = boneWeights[rightIndex];

            return
                left.boneIndex0 == right.boneIndex0 &&
                left.boneIndex1 == right.boneIndex1 &&
                left.boneIndex2 == right.boneIndex2 &&
                left.boneIndex3 == right.boneIndex3 &&
                Mathf.Abs(left.weight0 - right.weight0) <= 0.001f &&
                Mathf.Abs(left.weight1 - right.weight1) <= 0.001f &&
                Mathf.Abs(left.weight2 - right.weight2) <= 0.001f &&
                Mathf.Abs(left.weight3 - right.weight3) <= 0.001f;
        }

        private static Bounds BuildBounds(Vector3[] vertices)
        {
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            for (int i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(vertices[i]);
            }

            return bounds;
        }
    }

    internal sealed class VertexClusterAccumulator
    {
        private readonly Vector4[] uvSums;
        private readonly Dictionary<int, float> boneWeightSums;

        private Vector3 positionSum;
        private Vector3 normalSum;
        private Vector3 tangentSum;
        private float tangentHandednessSum;
        private Color colorSum;
        private int sampleCount;

        public VertexClusterAccumulator(MeshSourceData sourceData)
        {
            uvSums = new Vector4[sourceData.UvChannels.Length];
            if (sourceData.HasBoneWeights)
            {
                boneWeightSums = new Dictionary<int, float>(4);
            }
        }

        public void AddVertex(MeshSourceData sourceData, int vertexIndex)
        {
            positionSum += sourceData.Vertices[vertexIndex];

            if (sourceData.HasNormals)
            {
                normalSum += sourceData.Normals[vertexIndex];
            }

            if (sourceData.HasTangents)
            {
                Vector4 tangent = sourceData.Tangents[vertexIndex];
                tangentSum += new Vector3(tangent.x, tangent.y, tangent.z);
                tangentHandednessSum += tangent.w;
            }

            if (sourceData.HasColors)
            {
                colorSum += sourceData.Colors[vertexIndex];
            }

            for (int uvChannel = 0; uvChannel < sourceData.UvChannels.Length; uvChannel++)
            {
                if (sourceData.UvChannels[uvChannel] == null)
                {
                    continue;
                }

                uvSums[uvChannel] += sourceData.UvChannels[uvChannel][vertexIndex];
            }

            if (sourceData.HasBoneWeights)
            {
                BoneWeight sourceWeight = sourceData.BoneWeights[vertexIndex];
                AccumulateBoneWeight(sourceWeight.boneIndex0, sourceWeight.weight0);
                AccumulateBoneWeight(sourceWeight.boneIndex1, sourceWeight.weight1);
                AccumulateBoneWeight(sourceWeight.boneIndex2, sourceWeight.weight2);
                AccumulateBoneWeight(sourceWeight.boneIndex3, sourceWeight.weight3);
            }

            sampleCount++;
        }

        public Vector3 GetAveragePosition()
        {
            return positionSum / Mathf.Max(1, sampleCount);
        }

        public Vector3 GetAverageNormal()
        {
            Vector3 averageNormal = normalSum / Mathf.Max(1, sampleCount);
            if (averageNormal.sqrMagnitude <= 0f)
            {
                return Vector3.up;
            }

            return averageNormal.normalized;
        }

        public Vector4 GetAverageTangent()
        {
            Vector3 averageTangent = tangentSum / Mathf.Max(1, sampleCount);
            if (averageTangent.sqrMagnitude <= 0f)
            {
                averageTangent = Vector3.right;
            }
            else
            {
                averageTangent.Normalize();
            }

            float handedness = tangentHandednessSum >= 0f ? 1f : -1f;
            return new Vector4(averageTangent.x, averageTangent.y, averageTangent.z, handedness);
        }

        public Color GetAverageColor()
        {
            float inverseCount = 1f / Mathf.Max(1, sampleCount);
            return colorSum * inverseCount;
        }

        public Vector4 GetAverageUv(int channel)
        {
            return uvSums[channel] / Mathf.Max(1, sampleCount);
        }

        public BoneWeight GetAverageBoneWeight()
        {
            if (boneWeightSums == null || boneWeightSums.Count == 0)
            {
                return default;
            }

            List<KeyValuePair<int, float>> weights = new List<KeyValuePair<int, float>>(boneWeightSums);
            weights.Sort((left, right) => right.Value.CompareTo(left.Value));

            float totalWeight = 0f;
            int weightCount = Mathf.Min(4, weights.Count);
            for (int i = 0; i < weightCount; i++)
            {
                totalWeight += weights[i].Value;
            }

            if (totalWeight <= 0f)
            {
                return default;
            }

            BoneWeight averagedWeight = default;

            if (weightCount > 0)
            {
                averagedWeight.boneIndex0 = weights[0].Key;
                averagedWeight.weight0 = weights[0].Value / totalWeight;
            }

            if (weightCount > 1)
            {
                averagedWeight.boneIndex1 = weights[1].Key;
                averagedWeight.weight1 = weights[1].Value / totalWeight;
            }

            if (weightCount > 2)
            {
                averagedWeight.boneIndex2 = weights[2].Key;
                averagedWeight.weight2 = weights[2].Value / totalWeight;
            }

            if (weightCount > 3)
            {
                averagedWeight.boneIndex3 = weights[3].Key;
                averagedWeight.weight3 = weights[3].Value / totalWeight;
            }

            return averagedWeight;
        }

        private void AccumulateBoneWeight(int boneIndex, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (boneWeightSums.TryGetValue(boneIndex, out float currentWeight))
            {
                boneWeightSums[boneIndex] = currentWeight + weight;
                return;
            }

            boneWeightSums.Add(boneIndex, weight);
        }
    }

    internal sealed class MeshReadabilityRestorer : IDisposable
    {
        private readonly List<string> modifiedImporterPaths = new List<string>();

        private MeshReadabilityRestorer()
        {
        }

        public static MeshReadabilityRestorer Enable(IEnumerable<Mesh> sourceMeshes, bool autoEnable, List<string> messages)
        {
            MeshReadabilityRestorer restorer = new MeshReadabilityRestorer();
            HashSet<string> visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Mesh sourceMesh in sourceMeshes)
            {
                if (sourceMesh == null || sourceMesh.isReadable)
                {
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(sourceMesh);
                if (string.IsNullOrEmpty(assetPath))
                {
                    messages.Add("Skipping " + sourceMesh.name + ": the mesh is not readable and is not backed by an importable asset.");
                    continue;
                }

                if (!visitedPaths.Add(assetPath))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    messages.Add("Skipping " + sourceMesh.name + ": only model importer meshes can be toggled readable automatically.");
                    continue;
                }

                if (!autoEnable)
                {
                    messages.Add("Skipping " + sourceMesh.name + ": enable Read/Write on " + assetPath + " or turn on Auto Enable Read/Write.");
                    continue;
                }

                if (importer.isReadable)
                {
                    continue;
                }

                importer.isReadable = true;
                importer.SaveAndReimport();
                restorer.modifiedImporterPaths.Add(assetPath);
            }

            return restorer;
        }

        public void Dispose()
        {
            for (int i = 0; i < modifiedImporterPaths.Count; i++)
            {
                string assetPath = modifiedImporterPaths[i];
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null || !importer.isReadable)
                {
                    continue;
                }

                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    internal static class MeshStats
    {
        public static int CountTriangles(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0;
            }

            int triangleCount = 0;
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                if (mesh.GetTopology(subMeshIndex) != MeshTopology.Triangles)
                {
                    continue;
                }

                triangleCount += (int)(mesh.GetIndexCount(subMeshIndex) / 3);
            }

            return triangleCount;
        }
    }

    internal static class AssetPathUtility
    {
        public static bool IsValidAssetsPath(string path)
        {
            return TryGetCanonicalAssetsPath(path, out _);
        }

        public static bool TryGetCanonicalAssetsPath(string path, out string canonicalPath)
        {
            canonicalPath = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalizedPath = NormalizePath(path);
            string projectRoot = GetTrimmedFullPath(Path.Combine(Application.dataPath, ".."));
            string assetsRoot = GetTrimmedFullPath(Application.dataPath);
            string candidateFullPath;

            try
            {
                if (Path.IsPathRooted(normalizedPath))
                {
                    candidateFullPath = GetTrimmedFullPath(normalizedPath);
                }
                else
                {
                    if (!IsAssetsRelativePath(normalizedPath))
                    {
                        return false;
                    }

                    candidateFullPath = GetTrimmedFullPath(Path.Combine(projectRoot, normalizedPath));
                }
            }
            catch (Exception)
            {
                return false;
            }

            if (string.Equals(candidateFullPath, assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                canonicalPath = "Assets";
                return true;
            }

            string assetsRootWithSeparator = assetsRoot + Path.DirectorySeparatorChar;
            if (!candidateFullPath.StartsWith(assetsRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relativeAssetsPath = candidateFullPath.Substring(assetsRootWithSeparator.Length);
            canonicalPath = "Assets/" + NormalizePath(relativeAssetsPath);
            return true;
        }

        public static string GetCanonicalAssetsPathOrThrow(string path)
        {
            if (TryGetCanonicalAssetsPath(path, out string canonicalPath))
            {
                return canonicalPath;
            }

            throw new InvalidOperationException("Output folder must stay inside Assets.");
        }

        public static void EnsureFolderExists(string assetFolderPath)
        {
            string normalizedPath = GetCanonicalAssetsPathOrThrow(assetFolderPath);

            if (normalizedPath == "Assets")
            {
                return;
            }

            string[] folders = normalizedPath.Split('/');
            string currentPath = folders[0];

            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }

                currentPath = nextPath;
            }
        }

        public static string MakeUniqueMeshAssetPath(string outputFolder, string selectionName, string meshName)
        {
            string canonicalOutputFolder = GetCanonicalAssetsPathOrThrow(outputFolder);
            string safeSelectionName = SanitizeFileName(selectionName);
            string safeMeshName = SanitizeFileName(meshName);
            string fileName = safeSelectionName + "_" + safeMeshName + "_Optimized.asset";
            string candidatePath = canonicalOutputFolder + "/" + fileName;
            return AssetDatabase.GenerateUniqueAssetPath(candidatePath);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.Trim().Replace('\\', '/').TrimEnd('/');
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unnamed";
            }

            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            char[] characters = value.ToCharArray();

            for (int i = 0; i < characters.Length; i++)
            {
                for (int j = 0; j < invalidCharacters.Length; j++)
                {
                    if (characters[i] == invalidCharacters[j])
                    {
                        characters[i] = '_';
                        break;
                    }
                }
            }

            string sanitizedValue = new string(characters).Replace(' ', '_');
            return string.IsNullOrWhiteSpace(sanitizedValue) ? "Unnamed" : sanitizedValue;
        }

        private static bool IsAssetsRelativePath(string path)
        {
            return
                string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTrimmedFullPath(string path)
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}
#endif
