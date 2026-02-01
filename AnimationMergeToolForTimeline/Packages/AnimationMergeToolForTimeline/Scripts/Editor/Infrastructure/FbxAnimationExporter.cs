using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using AnimationMergeTool.Editor.Domain;
using AnimationMergeTool.Editor.Domain.Models;
#if UNITY_FORMATS_FBX
using UnityEditor.Formats.Fbx.Exporter;
using Autodesk.Fbx;
#endif

namespace AnimationMergeTool.Editor.Infrastructure
{
    /// <summary>
    /// FBXアニメーションエクスポーターの基本クラス
    /// FBX Exporter API（com.unity.formats.fbx）のラッパーとして機能する
    /// </summary>
    public class FbxAnimationExporter
    {
        private readonly SkeletonExtractor _skeletonExtractor;
        private readonly HumanoidToGenericConverter _humanoidConverter;
        private readonly BlendShapeDetector _blendShapeDetector;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public FbxAnimationExporter()
        {
            _skeletonExtractor = new SkeletonExtractor();
            _humanoidConverter = new HumanoidToGenericConverter();
            _blendShapeDetector = new BlendShapeDetector();
        }

        /// <summary>
        /// SkeletonExtractorを指定するコンストラクタ（テスト用）
        /// </summary>
        /// <param name="skeletonExtractor">スケルトン抽出器</param>
        public FbxAnimationExporter(SkeletonExtractor skeletonExtractor)
        {
            _skeletonExtractor = skeletonExtractor ?? new SkeletonExtractor();
            _humanoidConverter = new HumanoidToGenericConverter();
            _blendShapeDetector = new BlendShapeDetector();
        }

        /// <summary>
        /// SkeletonExtractorとHumanoidToGenericConverterを指定するコンストラクタ（テスト用）
        /// </summary>
        /// <param name="skeletonExtractor">スケルトン抽出器</param>
        /// <param name="humanoidConverter">Humanoid→Generic変換器</param>
        public FbxAnimationExporter(SkeletonExtractor skeletonExtractor, HumanoidToGenericConverter humanoidConverter)
        {
            _skeletonExtractor = skeletonExtractor ?? new SkeletonExtractor();
            _humanoidConverter = humanoidConverter ?? new HumanoidToGenericConverter();
            _blendShapeDetector = new BlendShapeDetector();
        }

        /// <summary>
        /// Animatorからスケルトン情報を取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>スケルトン情報（取得できない場合は空のSkeletonData）</returns>
        public SkeletonData ExtractSkeleton(Animator animator)
        {
            return _skeletonExtractor.Extract(animator);
        }

        /// <summary>
        /// ボーンのAnimator相対パスを取得する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="bone">パスを取得するボーン</param>
        /// <returns>Animator相対パス（Animator自身の場合は空文字）</returns>
        public string GetBonePath(Animator animator, Transform bone)
        {
            return _skeletonExtractor.GetBonePath(animator, bone);
        }

        /// <summary>
        /// FBXエクスポート機能が利用可能かどうかを確認する
        /// </summary>
        /// <returns>FBX Exporterパッケージがインストールされている場合はtrue</returns>
        public bool IsAvailable()
        {
            return FbxPackageChecker.IsPackageInstalled();
        }

        /// <summary>
        /// 指定されたエクスポートデータがエクスポート可能かどうかを確認する
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <returns>エクスポート可能な場合はtrue</returns>
        public bool CanExport(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return false;
            }

            return exportData.HasExportableData;
        }

        /// <summary>
        /// FBXファイルにエクスポートする
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool Export(FbxExportData exportData, string outputPath)
        {
            // 入力検証
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            if (!exportData.HasExportableData)
            {
                Debug.LogError("エクスポート可能なデータがありません。");
                return false;
            }

            // パッケージがインストールされているか確認
            if (!IsAvailable())
            {
                Debug.LogError("FBX Exporterパッケージがインストールされていません。");
                return false;
            }

            // P12-007で実装予定: 実際のFBXエクスポート処理
            return ExportInternal(exportData, outputPath);
        }

        /// <summary>
        /// エクスポートデータの検証を行う
        /// </summary>
        /// <param name="exportData">検証対象のエクスポートデータ</param>
        /// <returns>エラーメッセージ（問題がない場合はnull）</returns>
        public string ValidateExportData(FbxExportData exportData)
        {
            if (exportData == null)
            {
                return "FbxExportDataがnullです。";
            }

            if (!exportData.HasExportableData)
            {
                return "エクスポート可能なデータがありません。";
            }

            return null;
        }

        /// <summary>
        /// サポートされているエクスポートオプションを取得する
        /// </summary>
        /// <returns>エクスポートオプションのリスト</returns>
        public IReadOnlyList<string> GetSupportedExportOptions()
        {
            return new List<string>
            {
                "AnimationOnly",
                "WithSkeleton",
                "WithBlendShapes"
            };
        }

        /// <summary>
        /// 内部エクスポート処理
        /// ModelExporter APIを使用してFBXをエクスポートする
        /// AnimationClipを一時的なAnimatorControllerにバインドしてエクスポートする
        /// </summary>
        private bool ExportInternal(FbxExportData exportData, string outputPath)
        {
#if UNITY_FORMATS_FBX
            string tempClipPath = null;
            string tempControllerPath = null;
            string tempDirPath = null;
            RuntimeAnimatorController previousController = null;
            Animator targetAnimator = null;
            bool isTemporaryObject = false;
            GameObject exportTarget = null;
            List<GameObject> tempBlendShapeObjects = null;
            List<Mesh> tempMeshes = null;
            Dictionary<SkinnedMeshRenderer, Mesh> replacedMeshes = null;

            try
            {
                // Animatorがない場合は一時オブジェクトを作成、ある場合はAnimatorのGameObjectを使用
                if (exportData.SourceAnimator == null)
                {
                    exportTarget = CreateTemporaryExportObject(exportData);
                    isTemporaryObject = true;
                }
                else
                {
                    exportTarget = GetExportTarget(exportData);
                }

                if (exportTarget == null)
                {
                    Debug.LogError("エクスポート対象のGameObjectを取得できませんでした。");
                    return false;
                }

                // マテリアル情報をエクスポート前に収集（FBXエクスポート後の順序修正用）
                var materialInfo = CollectRendererMaterialInfo(exportTarget);

                // BlendShapeカーブが参照するSkinnedMeshRendererが存在しない場合、一時的に作成する
                tempBlendShapeObjects = CreateTemporaryBlendShapeObjects(
                    exportTarget, exportData, out tempMeshes, out replacedMeshes);

                // エクスポート用AnimationClipを作成
                AnimationClip exportClip = CreateExportAnimationClip(exportData);
                if (exportClip == null)
                {
                    Debug.LogError("エクスポート用AnimationClipの作成に失敗しました。");
                    return false;
                }

                // FBX出力ファイル名をアニメーション名として使用する
                // AssetDatabase.CreateAssetはオブジェクト名をファイル名で上書きするため、
                // 一時ファイルのファイル名自体をFBX名と一致させる必要がある
                var desiredClipName = System.IO.Path.GetFileNameWithoutExtension(outputPath);

                // 一時ディレクトリを作成（GUIDで一意にし、ファイル名の衝突を回避）
                var tempDirName = $"_temp_merge_{System.Guid.NewGuid():N}";
                AssetDatabase.CreateFolder("Assets", tempDirName);
                tempDirPath = $"Assets/{tempDirName}";

                // AnimationClipを一時アセットとして保存（ModelExporterがシリアライズできるように）
                var tempClip = Object.Instantiate(exportClip);
                tempClip.name = desiredClipName;
                tempClipPath = $"{tempDirPath}/{desiredClipName}.anim";
                AssetDatabase.CreateAsset(tempClip, tempClipPath);

                // 一時的なAnimatorControllerを作成
                tempControllerPath = $"{tempDirPath}/{desiredClipName}.controller";
                var tempController = AnimatorController.CreateAnimatorControllerAtPath(tempControllerPath);
                var stateMachine = tempController.layers[0].stateMachine;
                var state = stateMachine.AddState(desiredClipName);
                state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(tempClipPath);

                // Animatorにバインド
                targetAnimator = exportTarget.GetComponent<Animator>();
                if (targetAnimator == null)
                {
                    targetAnimator = exportTarget.AddComponent<Animator>();
                }
                previousController = targetAnimator.runtimeAnimatorController;
                targetAnimator.runtimeAnimatorController = tempController;

                // FBXエクスポート実行
                // ExportModelOptionsを明示的に指定し、Maya互換ネーミングを無効化する
                // デフォルトのUseMayaCompatibleNames=trueでは日本語マテリアル名がアンダースコアに変換され、
                // 再インポート時にマテリアルマッチングが失敗するため
                var exportOptions = new ExportModelOptions
                {
                    ExportFormat = ExportFormat.Binary,
                    ModelAnimIncludeOption = Include.ModelAndAnim,
                    UseMayaCompatibleNames = false,
                    KeepInstances = false,
                    EmbedTextures = false,
                    ExportUnrendered = true,
                    ObjectPosition = ObjectPosition.LocalCentered,
                };
                string result = ModelExporter.ExportObject(outputPath, exportTarget, exportOptions);

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogError($"FBXエクスポートに失敗しました: {outputPath}");
                    return false;
                }

                // Post-processing: マテリアル順序修正 + BlendShapeカーブ書き込み
                // FBXファイルを1回だけ再オープンして、必要な修正をすべて適用する
                var blendShapeCurves = exportData.BlendShapeCurves;
                PostProcessFbxFile(outputPath, materialInfo, blendShapeCurves);

                // アセットデータベースを更新
                AssetDatabase.Refresh();

                Debug.Log($"FBXエクスポート完了: {result}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"FBXエクスポートに失敗しました: {ex.Message}");
                return false;
            }
            finally
            {
                // 元のAnimatorControllerを復元
                if (targetAnimator != null && !isTemporaryObject)
                {
                    targetAnimator.runtimeAnimatorController = previousController;
                }

                // 一時ディレクトリごと削除（中の一時アセットも一括削除される）
                if (!string.IsNullOrEmpty(tempDirPath))
                {
                    AssetDatabase.DeleteAsset(tempDirPath);
                }

                // 差し替えたsharedMeshを元に戻す
                if (replacedMeshes != null)
                {
                    foreach (var kvp in replacedMeshes)
                    {
                        if (kvp.Key != null)
                        {
                            kvp.Key.sharedMesh = kvp.Value;
                        }
                    }
                }

                // 一時BlendShapeオブジェクトを削除
                if (tempBlendShapeObjects != null)
                {
                    foreach (var obj in tempBlendShapeObjects)
                    {
                        if (obj != null)
                        {
                            Object.DestroyImmediate(obj);
                        }
                    }
                }

                // 一時Meshを削除
                if (tempMeshes != null)
                {
                    foreach (var mesh in tempMeshes)
                    {
                        if (mesh != null)
                        {
                            Object.DestroyImmediate(mesh);
                        }
                    }
                }

                // 一時オブジェクトを削除
                if (isTemporaryObject && exportTarget != null)
                {
                    Object.DestroyImmediate(exportTarget);
                }
            }
#else
            Debug.LogError("FBX Exporterパッケージがインストールされていません。");
            return false;
#endif
        }

        /// <summary>
        /// エクスポート用のAnimationClipを作成する
        /// TransformカーブとBlendShapeカーブの両方を含むAnimationClipを生成する
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <returns>エクスポート用AnimationClip</returns>
        private AnimationClip CreateExportAnimationClip(FbxExportData exportData)
        {
            // MergedClipが存在し、追加するカーブがない場合はそのまま使用
            if (exportData.MergedClip != null &&
                (exportData.TransformCurves == null || exportData.TransformCurves.Count == 0) &&
                (exportData.BlendShapeCurves == null || exportData.BlendShapeCurves.Count == 0))
            {
                return exportData.MergedClip;
            }

            // 新しいAnimationClipを作成
            var clip = new AnimationClip();
            clip.name = exportData.MergedClip != null ? exportData.MergedClip.name + "_Export" : "ExportedAnimation";

            // Transformカーブを設定
            if (exportData.TransformCurves != null)
            {
                foreach (var curveData in exportData.TransformCurves)
                {
                    if (curveData.Curve == null)
                    {
                        continue;
                    }

                    clip.SetCurve(
                        curveData.Path,
                        typeof(Transform),
                        curveData.PropertyName,
                        curveData.Curve);
                }
            }

            // BlendShapeカーブを設定
            if (exportData.BlendShapeCurves != null)
            {
                foreach (var curveData in exportData.BlendShapeCurves)
                {
                    if (curveData.Curve == null)
                    {
                        continue;
                    }

                    // BlendShapeカーブをAnimationClipに設定
                    var binding = EditorCurveBinding.FloatCurve(
                        curveData.Path,
                        typeof(SkinnedMeshRenderer),
                        $"blendShape.{curveData.BlendShapeName}");
                    AnimationUtility.SetEditorCurve(clip, binding, curveData.Curve);
                }
            }

            return clip;
        }

        /// <summary>
        /// エクスポート対象のGameObjectを取得する
        /// </summary>
        private GameObject GetExportTarget(FbxExportData exportData)
        {
            if (exportData.SourceAnimator != null)
            {
                return exportData.SourceAnimator.gameObject;
            }

            // スケルトンがある場合はルートボーンのGameObjectを返す
            if (exportData.Skeleton != null && exportData.Skeleton.RootBone != null)
            {
                return exportData.Skeleton.RootBone.gameObject;
            }

            return null;
        }

        /// <summary>
        /// エクスポート用の一時GameObjectを作成する
        /// </summary>
        private GameObject CreateTemporaryExportObject(FbxExportData exportData)
        {
            // 一時的なGameObjectを作成
            var tempObject = new GameObject("TempExportObject");

            // スケルトン情報がある場合はボーン階層を構築
            if (exportData.Skeleton != null && exportData.Skeleton.HasSkeleton)
            {
                // ボーン階層の複製は複雑なため、ルートボーンを参照
                // （Phase 13で詳細実装予定）
                tempObject.transform.position = exportData.Skeleton.RootBone.position;
                tempObject.transform.rotation = exportData.Skeleton.RootBone.rotation;
            }

            // Animatorを追加してAnimationClipを設定
            if (exportData.MergedClip != null)
            {
                var animator = tempObject.AddComponent<Animator>();
                // RuntimeAnimatorControllerの設定はPhase 13以降で実装
            }

            return tempObject;
        }

        /// <summary>
        /// exportTarget配下の全Rendererのマテリアル名と順序を収集する。
        /// FBXエクスポート後のマテリアル順序修正に使用する。
        /// </summary>
        /// <param name="exportTarget">エクスポート対象のルートGameObject</param>
        /// <returns>
        /// key: Rendererが存在するオブジェクトのexportTargetからの相対パス
        /// value: マテリアル名のリスト（sharedMaterialsの順序）
        /// </returns>
        private Dictionary<string, List<string>> CollectRendererMaterialInfo(GameObject exportTarget)
        {
            var result = new Dictionary<string, List<string>>();
            if (exportTarget == null) return result;

            var renderers = exportTarget.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                    continue;

                // exportTargetからの相対パスを取得
                var path = GetRelativePath(exportTarget.transform, renderer.transform);

                var materialNames = new List<string>();
                foreach (var mat in renderer.sharedMaterials)
                {
                    materialNames.Add(mat != null ? mat.name : "");
                }

                result[path] = materialNames;
            }

            return result;
        }

        /// <summary>
        /// rootからtargetへの相対パスを取得する。
        /// targetがroot自身の場合は空文字を返す。
        /// </summary>
        private string GetRelativePath(Transform root, Transform target)
        {
            if (root == target) return "";

            var parts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (current != root) return target.name; // rootの子孫でない場合

            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>
        /// BlendShapeカーブが参照するパスに対応するSkinnedMeshRendererが
        /// エクスポート対象のGameObject階層に存在しない場合、一時的なダミーオブジェクトを作成する。
        /// FBXエクスポーターがBlendShapeアニメーションカーブを正しく出力するために必要。
        /// </summary>
        /// <param name="exportTarget">エクスポート対象のルートGameObject</param>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="tempMeshes">作成した一時Meshのリスト（cleanup用、out引数）</param>
        /// <param name="replacedMeshes">差し替えた既存SkinnedMeshRendererの元のsharedMesh（復元用、out引数）</param>
        /// <returns>作成した一時GameObjectのリスト（cleanup用）</returns>
        private List<GameObject> CreateTemporaryBlendShapeObjects(
            GameObject exportTarget, FbxExportData exportData, out List<Mesh> tempMeshes,
            out Dictionary<SkinnedMeshRenderer, Mesh> replacedMeshes)
        {
            var createdObjects = new List<GameObject>();
            tempMeshes = new List<Mesh>();
            replacedMeshes = new Dictionary<SkinnedMeshRenderer, Mesh>();

            if (exportData.BlendShapeCurves == null || exportData.BlendShapeCurves.Count == 0)
            {
                return createdObjects;
            }

            // パス別にBlendShape名をグループ化
            var pathToBlendShapes = new Dictionary<string, List<string>>();
            foreach (var curveData in exportData.BlendShapeCurves)
            {
                if (!pathToBlendShapes.ContainsKey(curveData.Path))
                {
                    pathToBlendShapes[curveData.Path] = new List<string>();
                }

                if (!pathToBlendShapes[curveData.Path].Contains(curveData.BlendShapeName))
                {
                    pathToBlendShapes[curveData.Path].Add(curveData.BlendShapeName);
                }
            }

            foreach (var kvp in pathToBlendShapes)
            {
                var path = kvp.Key;
                var blendShapeNames = kvp.Value;

                // パスが空の場合はexportTarget自体を対象とする
                Transform targetTransform;
                if (string.IsNullOrEmpty(path))
                {
                    targetTransform = exportTarget.transform;
                }
                else
                {
                    targetTransform = exportTarget.transform.Find(path);
                }

                // 既にSkinnedMeshRendererが存在し、必要なBlendShapeを持っているか確認
                if (targetTransform != null)
                {
                    var existingRenderer = targetTransform.GetComponent<SkinnedMeshRenderer>();
                    if (existingRenderer != null && existingRenderer.sharedMesh != null)
                    {
                        // 既存のMeshが全てのBlendShapeを持っているか確認
                        bool allFound = true;
                        foreach (var shapeName in blendShapeNames)
                        {
                            if (existingRenderer.sharedMesh.GetBlendShapeIndex(shapeName) < 0)
                            {
                                allFound = false;
                                break;
                            }
                        }

                        if (allFound)
                        {
                            continue; // 既に必要なBlendShapeが全て存在する
                        }
                    }
                }

                // パスに対応する階層が存在しない場合は作成する
                GameObject leafObject;
                if (targetTransform == null)
                {
                    leafObject = CreateHierarchyForPath(exportTarget, path, createdObjects);
                }
                else
                {
                    leafObject = targetTransform.gameObject;
                }

                // SkinnedMeshRendererを追加（既に存在しない場合）
                var renderer = leafObject.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null)
                {
                    renderer = leafObject.AddComponent<SkinnedMeshRenderer>();
                }

                Mesh mesh;

                if (renderer.sharedMesh != null)
                {
                    // 既存メッシュがある場合: クローンして不足BlendShapeを追加
                    // 元のメッシュ構造（頂点・サブメッシュ・マテリアルスロット対応）を保持するため
                    mesh = Object.Instantiate(renderer.sharedMesh);
                    mesh.name = $"TempClonedMesh_{path}";

                    // 不足しているBlendShapeのみを追加
                    var vertexCount = mesh.vertexCount;
                    var deltaVertices = new Vector3[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        deltaVertices[i] = new Vector3(0f, 0.001f, 0f);
                    }

                    foreach (var shapeName in blendShapeNames.Where(name => mesh.GetBlendShapeIndex(name) < 0))
                    {
                        mesh.AddBlendShapeFrame(shapeName, 100f, deltaVertices, null, null);
                    }

                    // 元のメッシュを保存（後で復元するため）
                    replacedMeshes[renderer] = renderer.sharedMesh;
                }
                else
                {
                    // 既存メッシュがない場合: ダミーMeshを新規作成
                    mesh = new Mesh();
                    mesh.name = $"TempBlendShapeMesh_{path}";

                    // 最低限の頂点データが必要（BlendShapeFrameの追加に必要）
                    mesh.vertices = new Vector3[] { Vector3.zero, Vector3.right, Vector3.up };
                    mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up };
                    mesh.triangles = new int[] { 0, 1, 2 };

                    // デルタ頂点は非ゼロである必要がある
                    // FBX SDKはデルタがすべてゼロのBlendShapeを空として扱い、エクスポート時にスキップするため
                    var deltaVertices = new Vector3[]
                    {
                        new Vector3(0f, 0.001f, 0f),
                        new Vector3(0f, 0.001f, 0f),
                        new Vector3(0f, 0.001f, 0f)
                    };

                    foreach (var shapeName in blendShapeNames)
                    {
                        mesh.AddBlendShapeFrame(shapeName, 100f, deltaVertices, null, null);
                    }
                }

                renderer.sharedMesh = mesh;
                tempMeshes.Add(mesh);
            }

            return createdObjects;
        }

        /// <summary>
        /// 指定されたパスに対応するGameObject階層を作成する。
        /// 例: "Body/Face" → exportTarget/Body/Face を作成。
        /// 途中のオブジェクトも含めて作成し、最も浅い新規作成オブジェクトをcreatedObjectsに追加する。
        /// </summary>
        /// <param name="root">ルートGameObject</param>
        /// <param name="path">作成するパス（"/"区切り）</param>
        /// <param name="createdObjects">作成したルートレベルの一時オブジェクトリスト</param>
        /// <returns>パスの末端のGameObject</returns>
        private GameObject CreateHierarchyForPath(GameObject root, string path, List<GameObject> createdObjects)
        {
            var segments = path.Split('/');
            var currentTransform = root.transform;
            GameObject firstCreated = null;

            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                var child = currentTransform.Find(segment);
                if (child == null)
                {
                    var newObject = new GameObject(segment);
                    newObject.transform.SetParent(currentTransform, false);
                    child = newObject.transform;

                    if (firstCreated == null)
                    {
                        firstCreated = newObject;
                    }
                }

                currentTransform = child;
            }

            // 最初に作成したオブジェクトをcleanupリストに追加
            if (firstCreated != null)
            {
                createdObjects.Add(firstCreated);
            }

            return currentTransform.gameObject;
        }

        #region Transformカーブ出力機能 (P13-006, P13-007)

        /// <summary>
        /// TransformカーブをFBXにエクスポートする
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportTransformCurvesToFbx(Animator animator, AnimationClip clip, string outputPath)
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClipがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Transformカーブを抽出
            var transformCurves = ExtractTransformCurves(clip, animator);

            if (transformCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なTransformカーブがありません。");
                return false;
            }

            // FbxExportDataを準備
            var exportData = PrepareTransformCurvesForExport(animator, clip, transformCurves);

            // エクスポート実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// TransformカーブデータからAnimationClipを生成する
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="transformCurves">Transformカーブ情報のリスト</param>
        /// <param name="clipName">生成するAnimationClipの名前</param>
        /// <returns>生成されたAnimationClip</returns>
        public AnimationClip CreateAnimationClipFromTransformCurves(
            List<TransformCurveData> transformCurves,
            string clipName = "ExportedAnimation")
        {
            if (transformCurves == null || transformCurves.Count == 0)
            {
                return null;
            }

            var clip = new AnimationClip();
            clip.name = clipName;

            foreach (var curveData in transformCurves)
            {
                if (curveData.Curve == null)
                {
                    continue;
                }

                // カーブをAnimationClipに設定
                clip.SetCurve(
                    curveData.Path,
                    typeof(Transform),
                    curveData.PropertyName,
                    curveData.Curve);
            }

            return clip;
        }

        /// <summary>
        /// TransformカーブデータをFbxExportDataに適用してエクスポートを実行する
        /// タスク P13-007で実装
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportWithTransformCurves(FbxExportData exportData, string outputPath)
        {
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Transformカーブが存在するか確認
            if (exportData.TransformCurves == null || exportData.TransformCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なTransformカーブがありません。");
                return false;
            }

            // 通常のエクスポート処理を実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// AnimationClipからTransformカーブを抽出する
        /// タスク P13-006で実装
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>Transformカーブ情報のリスト</returns>
        public List<TransformCurveData> ExtractTransformCurves(AnimationClip clip, Animator animator)
        {
            var result = new List<TransformCurveData>();

            // clipがnullの場合は空のリストを返す
            // animatorはnullでも、AnimationClipからカーブ抽出は可能
            if (clip == null)
            {
                return result;
            }

            // AnimationClipからカーブバインディングを取得
            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // Transform型のカーブのみを抽出
                if (binding.type != typeof(Transform))
                {
                    continue;
                }

                // カーブを取得
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                // カーブタイプを判定
                var curveType = DetermineTransformCurveType(binding.propertyName);

                // TransformCurveDataを作成
                var curveData = new TransformCurveData(
                    binding.path,
                    binding.propertyName,
                    curve,
                    curveType);

                result.Add(curveData);
            }

            return result;
        }

        /// <summary>
        /// プロパティ名からTransformカーブタイプを判定する
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        /// <returns>TransformCurveType</returns>
        private TransformCurveType DetermineTransformCurveType(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return TransformCurveType.Position;
            }

            if (propertyName.StartsWith("localPosition") || propertyName.StartsWith("m_LocalPosition"))
            {
                return TransformCurveType.Position;
            }

            if (propertyName.StartsWith("localRotation") || propertyName.StartsWith("m_LocalRotation"))
            {
                return TransformCurveType.Rotation;
            }

            if (propertyName.StartsWith("localScale") || propertyName.StartsWith("m_LocalScale"))
            {
                return TransformCurveType.Scale;
            }

            if (propertyName.StartsWith("localEulerAngles") || propertyName.StartsWith("m_LocalEulerAngles"))
            {
                return TransformCurveType.EulerAngles;
            }

            // デフォルトはPosition
            return TransformCurveType.Position;
        }

        /// <summary>
        /// Transformカーブ情報からFbxExportDataを準備する
        /// タスク P13-006で実装
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="transformCurves">Transformカーブ情報のリスト</param>
        /// <returns>FbxExportData</returns>
        public FbxExportData PrepareTransformCurvesForExport(
            Animator animator,
            AnimationClip clip,
            List<TransformCurveData> transformCurves)
        {
            // スケルトンを抽出（Animatorがある場合）
            SkeletonData skeleton = null;
            bool isHumanoid = false;

            if (animator != null)
            {
                skeleton = ExtractSkeleton(animator);
                isHumanoid = animator.isHuman;
            }

            // FbxExportDataを作成
            return new FbxExportData(
                animator,
                clip,
                skeleton,
                transformCurves ?? new List<TransformCurveData>(),
                new List<BlendShapeCurveData>(),
                isHumanoid);
        }

        #endregion

        #region Humanoid→Generic変換統合 (P14-009)

        /// <summary>
        /// HumanoidアニメーションをGeneric形式に変換してFBXエクスポート用データを準備する
        /// タスク P14-009: FbxAnimationExporterとの統合
        /// </summary>
        /// <param name="animator">対象のAnimator（Humanoidリグ）</param>
        /// <param name="humanoidClip">変換元のHumanoidアニメーションクリップ</param>
        /// <returns>エクスポート用に変換されたFbxExportData</returns>
        public FbxExportData PrepareHumanoidForExport(Animator animator, AnimationClip humanoidClip)
        {
            if (animator == null || humanoidClip == null)
            {
                return new FbxExportData(
                    animator,
                    humanoidClip,
                    null,
                    new List<TransformCurveData>(),
                    new List<BlendShapeCurveData>(),
                    false);
            }

            // スケルトンを抽出
            var skeleton = ExtractSkeleton(animator);
            bool isHumanoid = animator.isHuman;

            // Transformカーブのリストを準備
            var transformCurves = new List<TransformCurveData>();

            // Humanoidアニメーションの場合、マッスルカーブをRotationカーブに変換
            // isHumanoidClipチェックはマージ後クリップでfalseになり得るため、
            // animator.isHumanで判定し、SampleAnimationでボーン変換を行う
            if (isHumanoid)
            {
                // マッスルカーブを変換（SampleAnimationで各ボーンのlocalRotation/localPositionを記録）
                // SampleAnimationはルートモーションを含む全ボーンの状態を正しく反映するため、
                // Hipsのposition/rotationはHips親空間の正しい値が取得される。
                // ConvertRootMotionCurvesはRootT/RootQ（Animator空間の値）を直接localPosition/localRotationに
                // 変換するため、中間ボーンが存在する場合に座標系の不一致が発生する。
                // そのためルートモーションカーブの個別変換は行わない。
                var muscleCurves = _humanoidConverter.ConvertMuscleCurvesToRotation(animator, humanoidClip);
                transformCurves.AddRange(muscleCurves);
            }
            else
            {
                // 非Humanoidの場合は通常のTransformカーブ抽出
                transformCurves = ExtractTransformCurves(humanoidClip, animator);
            }

            // BlendShapeカーブを抽出（P17-002）
            var blendShapeCurves = ExtractBlendShapeCurves(humanoidClip, animator);

            return new FbxExportData(
                animator,
                humanoidClip,
                skeleton,
                transformCurves,
                blendShapeCurves,
                isHumanoid);
        }

        /// <summary>
        /// AnimatorがHumanoidリグかどうかを確認する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>Humanoidリグの場合true</returns>
        public bool IsHumanoidAnimator(Animator animator)
        {
            return animator != null && animator.isHuman;
        }

        /// <summary>
        /// AnimationClipがHumanoid形式かどうかを確認する
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <returns>Humanoid形式の場合true</returns>
        public bool IsHumanoidClip(AnimationClip clip)
        {
            return _humanoidConverter.IsHumanoidClip(clip);
        }

        /// <summary>
        /// HumanoidアニメーションをGenericカーブに変換する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="humanoidClip">変換元のAnimationClip</param>
        /// <returns>変換されたTransformカーブのリスト</returns>
        public List<TransformCurveData> ConvertHumanoidToGenericCurves(Animator animator, AnimationClip humanoidClip)
        {
            var result = new List<TransformCurveData>();

            if (animator == null || humanoidClip == null)
            {
                return result;
            }

            if (!animator.isHuman)
            {
                return result;
            }

            // マッスルカーブを変換（SampleAnimationでルートモーション含む全ボーンの状態を取得）
            var muscleCurves = _humanoidConverter.ConvertMuscleCurvesToRotation(animator, humanoidClip);
            result.AddRange(muscleCurves);

            return result;
        }

        /// <summary>
        /// HumanoidアニメーションをFBXにエクスポートする
        /// </summary>
        /// <param name="animator">対象のAnimator（Humanoidリグ）</param>
        /// <param name="humanoidClip">エクスポートするHumanoidアニメーションクリップ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合true</returns>
        public bool ExportHumanoidAnimation(Animator animator, AnimationClip humanoidClip, string outputPath)
        {
            if (animator == null)
            {
                Debug.LogError("Animatorがnullです。");
                return false;
            }

            if (humanoidClip == null)
            {
                Debug.LogError("AnimationClipがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // Humanoid用のエクスポートデータを準備
            var exportData = PrepareHumanoidForExport(animator, humanoidClip);

            if (!exportData.HasExportableData)
            {
                Debug.LogError("エクスポート可能なデータがありません。");
                return false;
            }

            // 通常のエクスポート処理を実行
            return Export(exportData, outputPath);
        }

        #endregion

        #region BlendShapeカーブFBX出力機能 (P15-004, P15-005)

        /// <summary>
        /// BlendShapeカーブをFBXにエクスポートする
        /// タスク P15-005で本格実装予定
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportBlendShapeCurvesToFbx(Animator animator, AnimationClip clip, string outputPath)
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClipがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // BlendShapeカーブを抽出
            var blendShapeCurves = ExtractBlendShapeCurves(clip, animator);

            if (blendShapeCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なBlendShapeカーブがありません。");
                return false;
            }

            // FbxExportDataを準備
            var exportData = PrepareBlendShapeCurvesForExport(animator, clip, blendShapeCurves);

            // エクスポート実行
            return Export(exportData, outputPath);
        }

        /// <summary>
        /// TransformカーブとBlendShapeカーブの両方を含むFbxExportDataを準備する
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <returns>FbxExportData</returns>
        public FbxExportData PrepareAllCurvesForExport(Animator animator, AnimationClip clip)
        {
            if (clip == null)
            {
                return new FbxExportData(
                    animator,
                    clip,
                    null,
                    new List<TransformCurveData>(),
                    new List<BlendShapeCurveData>(),
                    false);
            }

            // スケルトンを抽出（Animatorがある場合）
            SkeletonData skeleton = null;
            bool isHumanoid = false;

            if (animator != null)
            {
                skeleton = ExtractSkeleton(animator);
                isHumanoid = animator.isHuman;
            }

            // Transformカーブを抽出
            var transformCurves = ExtractTransformCurves(clip, animator);

            // BlendShapeカーブを抽出
            var blendShapeCurves = ExtractBlendShapeCurves(clip, animator);

            // FbxExportDataを作成
            return new FbxExportData(
                animator,
                clip,
                skeleton,
                transformCurves,
                blendShapeCurves,
                isHumanoid);
        }

        /// <summary>
        /// BlendShapeカーブデータからAnimationClipを生成する
        /// </summary>
        /// <param name="blendShapeCurves">BlendShapeカーブ情報のリスト</param>
        /// <param name="clipName">生成するAnimationClipの名前</param>
        /// <returns>生成されたAnimationClip（データがない場合はnull）</returns>
        public AnimationClip CreateAnimationClipFromBlendShapeCurves(
            List<BlendShapeCurveData> blendShapeCurves,
            string clipName = "ExportedBlendShapeAnimation")
        {
            if (blendShapeCurves == null || blendShapeCurves.Count == 0)
            {
                return null;
            }

            var clip = new AnimationClip();
            clip.name = clipName;

            foreach (var curveData in blendShapeCurves)
            {
                if (curveData.Curve == null)
                {
                    continue;
                }

                // BlendShapeカーブをAnimationClipに設定
                var binding = EditorCurveBinding.FloatCurve(
                    curveData.Path,
                    typeof(SkinnedMeshRenderer),
                    $"blendShape.{curveData.BlendShapeName}");
                AnimationUtility.SetEditorCurve(clip, binding, curveData.Curve);
            }

            // カーブが設定されたか確認
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                Object.DestroyImmediate(clip);
                return null;
            }

            return clip;
        }

        /// <summary>
        /// BlendShapeカーブを含むFbxExportDataでエクスポートを実行する
        /// </summary>
        /// <param name="exportData">エクスポートデータ</param>
        /// <param name="outputPath">出力先パス</param>
        /// <returns>エクスポートが成功した場合はtrue</returns>
        public bool ExportWithBlendShapeCurves(FbxExportData exportData, string outputPath)
        {
            if (exportData == null)
            {
                Debug.LogError("FbxExportDataがnullです。");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("出力パスが指定されていません。");
                return false;
            }

            // BlendShapeカーブが存在するか確認
            if (exportData.BlendShapeCurves == null || exportData.BlendShapeCurves.Count == 0)
            {
                Debug.LogError("エクスポート可能なBlendShapeカーブがありません。");
                return false;
            }

            // 通常のエクスポート処理を実行
            return Export(exportData, outputPath);
        }

        #endregion

        #region Autodesk.Fbx低レベルAPIによるBlendShapeカーブ書き込み・マテリアル順序修正

#if UNITY_FORMATS_FBX
        /// <summary>
        /// FBXファイルにBlendShapeアニメーションカーブを追記する。
        /// ModelExporter.ExportObject()で出力済みのFBXファイルを再オープンし、
        /// 既存のBlendShapeChannelのDeformPercentプロパティにアニメーションカーブを接続する。
        /// テスト互換性のために残しているメソッド。内部的にはWriteBlendShapeCurvesToSceneを使用する。
        /// </summary>
        /// <param name="fbxPath">対象FBXファイルパス（Assetsからの相対パス）</param>
        /// <param name="blendShapeCurves">BlendShapeカーブデータ</param>
        /// <returns>成功時true</returns>
        internal bool WriteBlendShapeCurvesToFbx(
            string fbxPath,
            IReadOnlyList<BlendShapeCurveData> blendShapeCurves)
        {
            if (blendShapeCurves == null || blendShapeCurves.Count == 0)
            {
                return true; // 書き込むカーブがないため成功扱い
            }

            // FBXファイルの絶対パスを取得
            var absolutePath = System.IO.Path.GetFullPath(fbxPath);
            if (!System.IO.File.Exists(absolutePath))
            {
                Debug.LogError($"FBXファイルが見つかりません: {absolutePath}");
                return false;
            }

            // FbxManagerの作成
            using (var fbxManager = FbxManager.Create())
            {
                if (fbxManager == null)
                {
                    Debug.LogError("FbxManagerの作成に失敗しました。");
                    return false;
                }

                // IOSettings作成
                var ioSettings = FbxIOSettings.Create(fbxManager, Autodesk.Fbx.Globals.IOSROOT);
                fbxManager.SetIOSettings(ioSettings);

                // FBXファイルの読み込み
                var fbxScene = FbxScene.Create(fbxManager, "BlendShapeScene");
                if (fbxScene == null)
                {
                    Debug.LogError("FbxSceneの作成に失敗しました。");
                    return false;
                }

                var fbxImporter = FbxImporter.Create(fbxManager, "BlendShapeImporter");
                if (!fbxImporter.Initialize(absolutePath))
                {
                    Debug.LogError($"FBXファイルの読み込み初期化に失敗しました: {absolutePath}");
                    fbxImporter.Destroy();
                    return false;
                }

                if (!fbxImporter.Import(fbxScene))
                {
                    Debug.LogError($"FBXファイルのインポートに失敗しました: {absolutePath}");
                    fbxImporter.Destroy();
                    return false;
                }
                fbxImporter.Destroy();

                // BlendShapeカーブの書き込み
                int writtenCount = WriteBlendShapeCurvesToScene(fbxScene, blendShapeCurves);

                if (writtenCount == 0)
                {
                    Debug.LogWarning("書き込み可能なBlendShapeカーブがありませんでした。");
                    return false;
                }

                // FBXファイルを保存
                var fbxExporter = FbxExporter.Create(fbxManager, "BlendShapeExporter");
                if (!fbxExporter.Initialize(absolutePath))
                {
                    Debug.LogError($"FBXエクスポーターの初期化に失敗しました: {absolutePath}");
                    fbxExporter.Destroy();
                    return false;
                }

                // バイナリ形式で保存
                if (!fbxExporter.Export(fbxScene))
                {
                    Debug.LogError($"FBXファイルの保存に失敗しました: {absolutePath}");
                    fbxExporter.Destroy();
                    return false;
                }
                fbxExporter.Destroy();

                Debug.Log($"BlendShapeアニメーションカーブを{writtenCount}個書き込みました: {fbxPath}");
                return true;
            }
        }

        /// <summary>
        /// FBXシーンにBlendShapeアニメーションカーブを書き込む（ファイルI/Oなし）。
        /// PostProcessFbxFileとWriteBlendShapeCurvesToFbxの両方から使用される共通コアロジック。
        /// </summary>
        /// <param name="fbxScene">書き込み先のFBXシーン</param>
        /// <param name="blendShapeCurves">BlendShapeカーブデータ</param>
        /// <returns>書き込んだカーブ数</returns>
        private int WriteBlendShapeCurvesToScene(
            FbxScene fbxScene,
            IReadOnlyList<BlendShapeCurveData> blendShapeCurves)
        {
            if (fbxScene == null || blendShapeCurves == null || blendShapeCurves.Count == 0)
            {
                return 0;
            }

            // 既存のFbxAnimStackを取得、なければ作成
            FbxAnimStack fbxAnimStack = fbxScene.GetCurrentAnimationStack();
            if (fbxAnimStack == null)
            {
                fbxAnimStack = FbxAnimStack.Create(fbxScene, "Take 001");
            }

            // FbxAnimLayerを取得、なければ作成
            FbxAnimLayer fbxAnimLayer = null;
            if (fbxAnimStack.GetMemberCount() > 0)
            {
                fbxAnimLayer = fbxAnimStack.GetAnimLayerMember(0);
            }
            if (fbxAnimLayer == null)
            {
                fbxAnimLayer = FbxAnimLayer.Create(fbxScene, "Base Layer");
                fbxAnimStack.AddMember(fbxAnimLayer);
            }

            var rootNode = fbxScene.GetRootNode();
            int writtenCount = 0;

            // パス別にBlendShapeカーブをグループ化
            var pathGroups = new Dictionary<string, List<BlendShapeCurveData>>();
            foreach (var curveData in blendShapeCurves)
            {
                if (curveData.Curve == null) continue;

                if (!pathGroups.ContainsKey(curveData.Path))
                {
                    pathGroups[curveData.Path] = new List<BlendShapeCurveData>();
                }
                pathGroups[curveData.Path].Add(curveData);
            }

            foreach (var group in pathGroups)
            {
                var path = group.Key;
                var curves = group.Value;

                // パスからFbxNodeを検索
                var targetNode = FindFbxNodeByPath(rootNode, path);
                if (targetNode == null)
                {
                    Debug.LogWarning($"BlendShapeカーブのパスに対応するFBXノードが見つかりません: {path}");
                    continue;
                }

                // ノードのBlendShapeチャネルを取得
                var blendShapeChannels = CollectBlendShapeChannels(targetNode);
                if (blendShapeChannels.Count == 0)
                {
                    Debug.LogWarning($"FBXノードにBlendShapeチャネルが見つかりません: {targetNode.GetName()}");
                    continue;
                }

                foreach (var curveData in curves)
                {
                    // BlendShape名に一致するチャネルを検索
                    FbxBlendShapeChannel targetChannel = null;
                    foreach (var channel in blendShapeChannels)
                    {
                        if (channel.GetName() == curveData.BlendShapeName)
                        {
                            targetChannel = channel;
                            break;
                        }
                    }

                    if (targetChannel == null)
                    {
                        Debug.LogWarning(
                            $"BlendShapeチャネルが見つかりません: {curveData.BlendShapeName} (ノード: {targetNode.GetName()})");
                        continue;
                    }

                    // DeformPercentプロパティにアニメーションカーブを設定
                    var deformPercent = targetChannel.DeformPercent;
                    if (!deformPercent.IsValid())
                    {
                        Debug.LogWarning(
                            $"DeformPercentプロパティが無効です: {curveData.BlendShapeName}");
                        continue;
                    }

                    // FbxAnimCurveを作成してDeformPercentに接続
                    var fbxAnimCurve = deformPercent.GetCurve(fbxAnimLayer, true);
                    if (fbxAnimCurve == null)
                    {
                        Debug.LogWarning(
                            $"FbxAnimCurveの作成に失敗しました: {curveData.BlendShapeName}");
                        continue;
                    }

                    // キーフレームを書き込み
                    WriteAnimationKeys(curveData.Curve, fbxAnimCurve);
                    writtenCount++;
                }
            }

            return writtenCount;
        }

        /// <summary>
        /// FBXファイルのpost-processing（マテリアル順序修正 + BlendShapeカーブ書き込み）。
        /// FBXファイルを1回だけ再オープンし、必要な修正をすべて適用して保存する。
        /// </summary>
        /// <param name="fbxPath">対象FBXファイルパス</param>
        /// <param name="expectedMaterialOrder">Unity側の期待するマテリアル順序</param>
        /// <param name="blendShapeCurves">BlendShapeカーブデータ</param>
        /// <returns>成功時true</returns>
        private bool PostProcessFbxFile(
            string fbxPath,
            Dictionary<string, List<string>> expectedMaterialOrder,
            IReadOnlyList<BlendShapeCurveData> blendShapeCurves)
        {
            bool hasBlendShapeCurves = blendShapeCurves != null && blendShapeCurves.Count > 0;
            bool hasMaterialOrder = expectedMaterialOrder != null && expectedMaterialOrder.Count > 0;

            // 両方不要ならスキップ
            if (!hasBlendShapeCurves && !hasMaterialOrder)
            {
                return true;
            }

            // FBXファイルの絶対パスを取得
            var absolutePath = System.IO.Path.GetFullPath(fbxPath);
            if (!System.IO.File.Exists(absolutePath))
            {
                Debug.LogError($"FBXファイルが見つかりません: {absolutePath}");
                return false;
            }

            using (var fbxManager = FbxManager.Create())
            {
                if (fbxManager == null)
                {
                    Debug.LogError("FbxManagerの作成に失敗しました。");
                    return false;
                }

                var ioSettings = FbxIOSettings.Create(fbxManager, Autodesk.Fbx.Globals.IOSROOT);
                fbxManager.SetIOSettings(ioSettings);

                var fbxScene = FbxScene.Create(fbxManager, "PostProcessScene");
                if (fbxScene == null)
                {
                    Debug.LogError("FbxSceneの作成に失敗しました。");
                    return false;
                }

                var fbxImporter = FbxImporter.Create(fbxManager, "PostProcessImporter");
                if (!fbxImporter.Initialize(absolutePath))
                {
                    Debug.LogError($"FBXファイルの読み込み初期化に失敗しました: {absolutePath}");
                    fbxImporter.Destroy();
                    return false;
                }

                if (!fbxImporter.Import(fbxScene))
                {
                    Debug.LogError($"FBXファイルのインポートに失敗しました: {absolutePath}");
                    fbxImporter.Destroy();
                    return false;
                }
                fbxImporter.Destroy();

                bool modified = false;

                // マテリアル順序修正
                if (hasMaterialOrder)
                {
                    var rootNode = fbxScene.GetRootNode();
                    bool materialFixed = FixMaterialOrder(rootNode, expectedMaterialOrder);
                    if (materialFixed)
                    {
                        modified = true;
                    }
                }

                // BlendShapeカーブ書き込み
                if (hasBlendShapeCurves)
                {
                    int writtenCount = WriteBlendShapeCurvesToScene(fbxScene, blendShapeCurves);
                    if (writtenCount > 0)
                    {
                        Debug.Log($"BlendShapeアニメーションカーブを{writtenCount}個書き込みました: {fbxPath}");
                        modified = true;
                    }
                    else
                    {
                        Debug.LogWarning($"BlendShapeアニメーションの書き込みに失敗しました: {fbxPath}");
                    }
                }

                // 変更がなければ保存不要
                if (!modified)
                {
                    return true;
                }

                // FBXファイルを保存
                var fbxExporter = FbxExporter.Create(fbxManager, "PostProcessExporter");
                if (!fbxExporter.Initialize(absolutePath))
                {
                    Debug.LogError($"FBXエクスポーターの初期化に失敗しました: {absolutePath}");
                    fbxExporter.Destroy();
                    return false;
                }

                if (!fbxExporter.Export(fbxScene))
                {
                    Debug.LogError($"FBXファイルの保存に失敗しました: {absolutePath}");
                    fbxExporter.Destroy();
                    return false;
                }
                fbxExporter.Destroy();

                return true;
            }
        }

        /// <summary>
        /// FBXシーン内の各メッシュノードのマテリアル順序を修正する。
        /// Unity側のsharedMaterialsの順序と一致するようにマテリアルの接続順序を変更し、
        /// ポリゴンマテリアルインデックスも更新する。
        /// </summary>
        /// <param name="rootNode">FBXシーンのルートノード</param>
        /// <param name="expectedOrder">期待するマテリアル順序（キー: 相対パス、値: マテリアル名リスト）</param>
        /// <returns>いずれかのノードでマテリアル順序を修正した場合true</returns>
        private bool FixMaterialOrder(
            FbxNode rootNode,
            Dictionary<string, List<string>> expectedOrder)
        {
            if (rootNode == null || expectedOrder == null || expectedOrder.Count == 0)
            {
                return false;
            }

            bool anyFixed = false;

            // 各期待エントリに対してFBXノードを検索して修正
            foreach (var kvp in expectedOrder)
            {
                var path = kvp.Key;
                var expectedMaterialNames = kvp.Value;

                if (expectedMaterialNames == null || expectedMaterialNames.Count <= 1)
                {
                    continue; // マテリアルが0〜1個なら順序修正不要
                }

                // パスからFBXノードを検索
                var targetNode = FindFbxNodeByPath(rootNode, path);
                if (targetNode == null)
                {
                    continue;
                }

                bool fixed_ = FixNodeMaterialOrder(targetNode, expectedMaterialNames);
                if (fixed_)
                {
                    anyFixed = true;
                }
            }

            return anyFixed;
        }

        /// <summary>
        /// 単一のFBXノードのマテリアル順序を修正する。
        /// </summary>
        /// <param name="node">対象ノード</param>
        /// <param name="expectedMaterialNames">期待するマテリアル名リスト</param>
        /// <returns>マテリアル順序を修正した場合true</returns>
        private bool FixNodeMaterialOrder(FbxNode node, List<string> expectedMaterialNames)
        {
            int materialCount = node.GetMaterialCount();
            if (materialCount <= 1 || materialCount != expectedMaterialNames.Count)
            {
                return false;
            }

            // 現在のマテリアル順序を取得
            var currentMaterials = new List<FbxSurfaceMaterial>();
            var currentNames = new List<string>();
            for (int i = 0; i < materialCount; i++)
            {
                var mat = node.GetMaterial(i);
                currentMaterials.Add(mat);
                currentNames.Add(mat != null ? mat.GetName() : "");
            }

            // 順序が一致しているか確認
            bool needsFix = false;
            for (int i = 0; i < materialCount; i++)
            {
                if (currentNames[i] != expectedMaterialNames[i])
                {
                    needsFix = true;
                    break;
                }
            }

            if (!needsFix)
            {
                return false;
            }

            // リマッピングテーブルを作成: currentIndex → expectedIndex
            // 例: current=["B","A","C"], expected=["A","B","C"] の場合
            // remap[0]=1 (Bはexpected[1]), remap[1]=0 (Aはexpected[0]), remap[2]=2 (Cはexpected[2])
            var remap = new int[materialCount];
            bool remapValid = true;
            for (int currentIdx = 0; currentIdx < materialCount; currentIdx++)
            {
                int expectedIdx = expectedMaterialNames.IndexOf(currentNames[currentIdx]);
                if (expectedIdx < 0)
                {
                    // Unity側に対応するマテリアル名が見つからない場合はスキップ
                    remapValid = false;
                    break;
                }
                remap[currentIdx] = expectedIdx;
            }

            if (!remapValid)
            {
                return false;
            }

            // 重複チェック（remapが1対1対応であること）
            var usedIndices = new HashSet<int>();
            foreach (var idx in remap)
            {
                if (!usedIndices.Add(idx))
                {
                    // 重複がある場合はスキップ（同名マテリアルが複数ある等）
                    return false;
                }
            }

            // マテリアルを切断して正しい順序で再接続
            // 1. 全マテリアルを切断（FbxNode.RemoveMaterialは存在しないためDisconnectSrcObjectを使用）
            for (int i = materialCount - 1; i >= 0; i--)
            {
                node.DisconnectSrcObject(currentMaterials[i]);
            }

            // 2. 正しい順序で再接続（expectedの順序に並べ替え）
            var sortedMaterials = new FbxSurfaceMaterial[materialCount];
            for (int currentIdx = 0; currentIdx < materialCount; currentIdx++)
            {
                sortedMaterials[remap[currentIdx]] = currentMaterials[currentIdx];
            }

            for (int i = 0; i < materialCount; i++)
            {
                node.AddMaterial(sortedMaterials[i]);
            }

            // 3. ポリゴンマテリアルインデックスを更新
            var mesh = node.GetMesh();
            if (mesh != null)
            {
                var layer = mesh.GetLayer(0);
                if (layer != null)
                {
                    var materialElement = layer.GetMaterials();
                    if (materialElement != null)
                    {
                        var indexArray = materialElement.GetIndexArray();
                        int count = indexArray.GetCount();
                        for (int i = 0; i < count; i++)
                        {
                            int oldIndex = indexArray.GetAt(i);
                            if (oldIndex >= 0 && oldIndex < materialCount)
                            {
                                indexArray.SetAt(i, remap[oldIndex]);
                            }
                        }
                    }
                }
            }

            Debug.Log($"マテリアル順序を修正しました: {node.GetName()} " +
                      $"[{string.Join(", ", currentNames)}] → [{string.Join(", ", expectedMaterialNames)}]");
            return true;
        }

        /// <summary>
        /// パス文字列からFBXノードを検索する。
        /// パスが空の場合はルートノードの最初の子ノードを返す。
        /// パスの最後のセグメントに一致するノードを再帰的に検索する。
        /// </summary>
        private FbxNode FindFbxNodeByPath(FbxNode rootNode, string path)
        {
            if (rootNode == null) return null;

            if (string.IsNullOrEmpty(path))
            {
                // パスが空の場合、ルートノードの直下の子ノードを返す
                // （FBXエクスポートではルートの子がエクスポート対象のルートになる）
                if (rootNode.GetChildCount() > 0)
                {
                    return rootNode.GetChild(0);
                }
                return rootNode;
            }

            // パスの最後のセグメントを取得（例: "Body/Face" → "Face"）
            var segments = path.Split('/');
            var targetName = segments[segments.Length - 1];

            // 再帰的にノードを検索
            return FindFbxNodeRecursive(rootNode, targetName);
        }

        /// <summary>
        /// FBXノードツリーを再帰的に検索して、指定された名前のノードを見つける
        /// </summary>
        private FbxNode FindFbxNodeRecursive(FbxNode node, string name)
        {
            if (node == null) return null;

            // FindChildメソッドで再帰検索
            var found = node.FindChild(name, true);
            return found;
        }

        /// <summary>
        /// FbxNodeに関連するBlendShapeチャネルをすべて収集する
        /// </summary>
        private List<FbxBlendShapeChannel> CollectBlendShapeChannels(FbxNode node)
        {
            var channels = new List<FbxBlendShapeChannel>();

            if (node == null) return channels;

            // ノードのMeshを取得
            var mesh = node.GetMesh();
            if (mesh == null) return channels;

            // BlendShapeタイプのDeformer数を取得
            int blendShapeCount = mesh.GetDeformerCount(FbxDeformer.EDeformerType.eBlendShape);
            for (int i = 0; i < blendShapeCount; i++)
            {
                var blendShape = mesh.GetBlendShapeDeformer(i);
                if (blendShape == null) continue;

                int channelCount = blendShape.GetBlendShapeChannelCount();
                for (int j = 0; j < channelCount; j++)
                {
                    var channel = blendShape.GetBlendShapeChannel(j);
                    if (channel != null)
                    {
                        channels.Add(channel);
                    }
                }
            }

            return channels;
        }

        /// <summary>
        /// Infinity・NaNを安全な値（0f）に置換するヘルパー
        /// FBX SDKにInfinity/NaNを渡すとC++ Runtime Errorが発生するため
        /// </summary>
        private static float SanitizeFloat(float value)
        {
            return float.IsInfinity(value) || float.IsNaN(value) ? 0f : value;
        }

        /// <summary>
        /// UnityのAnimationCurveからFbxAnimCurveにキーフレームを書き込む
        /// </summary>
        private void WriteAnimationKeys(AnimationCurve uniCurve, FbxAnimCurve fbxCurve)
        {
            fbxCurve.KeyModifyBegin();
            try
            {
                for (int i = 0; i < uniCurve.length; i++)
                {
                    var keyframe = uniCurve[i];
                    var fbxTime = FbxTime.FromSecondDouble(keyframe.time);
                    int keyIndex = fbxCurve.KeyAdd(fbxTime);

                    // 補間モードの設定
                    FbxAnimCurveDef.EInterpolationType interpMode =
                        FbxAnimCurveDef.EInterpolationType.eInterpolationCubic;

                    var rightTangent = AnimationUtility.GetKeyRightTangentMode(uniCurve, i);
                    switch (rightTangent)
                    {
                        case AnimationUtility.TangentMode.Linear:
                            interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationLinear;
                            break;
                        case AnimationUtility.TangentMode.Constant:
                            interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationConstant;
                            break;
                    }

                    // outTangentがInfinityの場合はステップ補間として扱う
                    // Unityでは Infinity タンジェント = 瞬時変化（離散的な値の切り替え）
                    if (float.IsInfinity(keyframe.outTangent) || float.IsNaN(keyframe.outTangent))
                    {
                        interpMode = FbxAnimCurveDef.EInterpolationType.eInterpolationConstant;
                    }

                    // タンジェントモード: キーごとに左右独立かどうかを判定
                    // eTangentBreak: 左右独立でユーザー指定値を使用
                    // eTangentUser: 左右統一でユーザー指定値を使用
                    // ※eTangentAutoはFBX SDKがタンジェントを自動計算し、渡した値を無視するため使わない
                    bool isBroken = AnimationUtility.GetKeyBroken(uniCurve, i);
                    var tangentMode = isBroken
                        ? FbxAnimCurveDef.ETangentMode.eTangentBreak
                        : FbxAnimCurveDef.ETangentMode.eTangentUser;

                    // ウェイトモード: 現在キーのOutと次キーのInに基づいて判定
                    bool hasOutWeight = (keyframe.weightedMode & WeightedMode.Out) != 0;
                    bool hasNextInWeight = false;
                    if (i < uniCurve.length - 1)
                    {
                        hasNextInWeight = (uniCurve[i + 1].weightedMode & WeightedMode.In) != 0;
                    }

                    FbxAnimCurveDef.EWeightedMode weightedMode;
                    if (hasOutWeight && hasNextInWeight)
                        weightedMode = FbxAnimCurveDef.EWeightedMode.eWeightedAll;
                    else if (hasOutWeight)
                        weightedMode = FbxAnimCurveDef.EWeightedMode.eWeightedRight;
                    else if (hasNextInWeight)
                        weightedMode = FbxAnimCurveDef.EWeightedMode.eWeightedNextLeft;
                    else
                        weightedMode = FbxAnimCurveDef.EWeightedMode.eWeightedNone;

                    // 値・タンジェント・ウェイトをサニタイズしてFBX SDKに渡す
                    float safeValue = SanitizeFloat(keyframe.value);
                    float safeOutTangent = SanitizeFloat(keyframe.outTangent);
                    float safeNextInTangent = i < uniCurve.length - 1
                        ? SanitizeFloat(uniCurve[i + 1].inTangent)
                        : 0f;
                    float safeOutWeight = SanitizeFloat(keyframe.outWeight);
                    float safeNextInWeight = i < uniCurve.length - 1
                        ? SanitizeFloat(uniCurve[i + 1].inWeight)
                        : 0f;

                    fbxCurve.KeySet(keyIndex,
                        fbxTime,
                        safeValue,
                        interpMode,
                        tangentMode,
                        safeOutTangent,
                        safeNextInTangent,
                        weightedMode,
                        safeOutWeight,
                        safeNextInWeight
                    );
                }
            }
            finally
            {
                fbxCurve.KeyModifyEnd();
            }
        }
#endif

        #endregion

        #region BlendShapeカーブ抽出機能 (P15-002, P15-003)

        /// <summary>
        /// AnimationClipからBlendShapeカーブを抽出する
        /// タスク P15-003で本格実装予定
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="animator">対象のAnimator</param>
        /// <returns>BlendShapeカーブ情報のリスト</returns>
        public List<BlendShapeCurveData> ExtractBlendShapeCurves(AnimationClip clip, Animator animator)
        {
            var result = new List<BlendShapeCurveData>();

            // clipがnullの場合は空のリストを返す
            // animatorはnullでも、AnimationClipからBlendShapeカーブ抽出は可能
            if (clip == null)
            {
                return result;
            }

            // AnimationClipからカーブバインディングを取得
            var bindings = AnimationUtility.GetCurveBindings(clip);

            foreach (var binding in bindings)
            {
                // BlendShapeカーブのみを抽出
                if (!IsBlendShapeBinding(binding))
                {
                    continue;
                }

                // カーブを取得
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                // BlendShape名を抽出（"blendShape."プレフィックスを除去）
                string blendShapeName = ExtractBlendShapeName(binding);

                // BlendShapeCurveDataを作成
                var curveData = new BlendShapeCurveData(
                    binding.path,
                    blendShapeName,
                    curve);

                result.Add(curveData);
            }

            return result;
        }

        /// <summary>
        /// AnimationClipにBlendShapeカーブが含まれているかを確認する
        /// </summary>
        /// <param name="clip">対象のAnimationClip</param>
        /// <returns>BlendShapeカーブが存在する場合true</returns>
        public bool HasBlendShapeCurves(AnimationClip clip)
        {
            return _blendShapeDetector.HasBlendShapeCurves(clip);
        }

        /// <summary>
        /// BlendShapeカーブ情報からFbxExportDataを準備する
        /// タスク P15-003で本格実装予定
        /// </summary>
        /// <param name="animator">対象のAnimator</param>
        /// <param name="clip">対象のAnimationClip</param>
        /// <param name="blendShapeCurves">BlendShapeカーブ情報のリスト</param>
        /// <returns>FbxExportData</returns>
        public FbxExportData PrepareBlendShapeCurvesForExport(
            Animator animator,
            AnimationClip clip,
            List<BlendShapeCurveData> blendShapeCurves)
        {
            // スケルトンを抽出（Animatorがある場合）
            SkeletonData skeleton = null;
            bool isHumanoid = false;

            if (animator != null)
            {
                skeleton = ExtractSkeleton(animator);
                isHumanoid = animator.isHuman;
            }

            // FbxExportDataを作成
            return new FbxExportData(
                animator,
                clip,
                skeleton,
                new List<TransformCurveData>(),
                blendShapeCurves ?? new List<BlendShapeCurveData>(),
                isHumanoid);
        }

        /// <summary>
        /// EditorCurveBindingがBlendShapeカーブかどうかを判定する
        /// BlendShapeDetectorに処理を委譲する
        /// </summary>
        /// <param name="binding">判定対象のバインディング</param>
        /// <returns>BlendShapeカーブの場合true</returns>
        private bool IsBlendShapeBinding(EditorCurveBinding binding)
        {
            return _blendShapeDetector.IsBlendShapeProperty(binding);
        }

        /// <summary>
        /// EditorCurveBindingからBlendShape名を抽出する
        /// BlendShapeDetectorに処理を委譲する
        /// </summary>
        /// <param name="binding">BlendShapeプロパティのEditorCurveBinding</param>
        /// <returns>BlendShape名（"blendShape."プレフィックスを除いた部分）</returns>
        private string ExtractBlendShapeName(EditorCurveBinding binding)
        {
            return _blendShapeDetector.GetBlendShapeName(binding) ?? string.Empty;
        }

        #endregion
    }
}
