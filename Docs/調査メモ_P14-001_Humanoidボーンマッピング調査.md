# P14-001: Humanoidボーンマッピング調査メモ

**作成日**: 2026-01-27
**対応要件**: FR-090, FR-091, FR-092
**依存**: P13-008（Phase 13完了）

---

## 1. 概要

本調査メモでは、UnityのHumanoidリグにおけるボーンマッピング仕様を調査し、Humanoid形式のアニメーションをGeneric形式のFBXにエクスポートするための変換ロジックに必要な情報をまとめる。

---

## 2. 要件分析

### 2.1 対象要件

| 要件ID | 要件内容 | 調査項目 |
|--------|----------|----------|
| FR-090 | FBX出力は常にGeneric形式とする | Humanoid→Generic変換の必要性 |
| FR-091 | HumanoidボーンをTransformパスに変換してエクスポート | HumanBodyBonesとTransformの対応関係 |
| FR-092 | ルートモーションが含まれる場合は適切にエクスポート | ルートモーションのカーブ形式 |

---

## 3. HumanBodyBones列挙型

### 3.1 基本情報

`UnityEngine.HumanBodyBones`はHumanoidリグの各ボーンを表す列挙型。

```csharp
namespace UnityEngine
{
    public enum HumanBodyBones
    {
        // 体幹
        Hips = 0,
        Spine = 7,
        Chest = 8,
        UpperChest = 54,

        // 頭部
        Neck = 9,
        Head = 10,
        LeftEye = 21,
        RightEye = 22,
        Jaw = 23,

        // 左腕
        LeftShoulder = 11,
        LeftUpperArm = 13,
        LeftLowerArm = 15,
        LeftHand = 17,

        // 右腕
        RightShoulder = 12,
        RightUpperArm = 14,
        RightLowerArm = 16,
        RightHand = 18,

        // 左脚
        LeftUpperLeg = 1,
        LeftLowerLeg = 3,
        LeftFoot = 5,
        LeftToes = 19,

        // 右脚
        RightUpperLeg = 2,
        RightLowerLeg = 4,
        RightFoot = 6,
        RightToes = 20,

        // 左手指
        LeftThumbProximal = 24,
        LeftThumbIntermediate = 25,
        LeftThumbDistal = 26,
        LeftIndexProximal = 27,
        LeftIndexIntermediate = 28,
        LeftIndexDistal = 29,
        LeftMiddleProximal = 30,
        LeftMiddleIntermediate = 31,
        LeftMiddleDistal = 32,
        LeftRingProximal = 33,
        LeftRingIntermediate = 34,
        LeftRingDistal = 35,
        LeftLittleProximal = 36,
        LeftLittleIntermediate = 37,
        LeftLittleDistal = 38,

        // 右手指
        RightThumbProximal = 39,
        RightThumbIntermediate = 40,
        RightThumbDistal = 41,
        RightIndexProximal = 42,
        RightIndexIntermediate = 43,
        RightIndexDistal = 44,
        RightMiddleProximal = 45,
        RightMiddleIntermediate = 46,
        RightMiddleDistal = 47,
        RightRingProximal = 48,
        RightRingIntermediate = 49,
        RightRingDistal = 50,
        RightLittleProximal = 51,
        RightLittleIntermediate = 52,
        RightLittleDistal = 53,

        // 終端マーカー（使用しない）
        LastBone = 55
    }
}
```

### 3.2 ボーン総数

- **必須ボーン（Required）**: 15個（Hips, Spine, Chest, Neck, Head, 両腕3ボーン×2, 両脚3ボーン×2）
- **オプションボーン（Optional）**: UpperChest, 目, 顎, 足指, 手指など
- **合計**: 55ボーン（LastBone除く）

---

## 4. AnimatorからのTransform取得

### 4.1 Animator.GetBoneTransform()

```csharp
// HumanBodyBonesからTransformを取得
public Transform GetBoneTransform(HumanBodyBones humanBoneId);
```

**使用例**:
```csharp
Animator animator = GetComponent<Animator>();
Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
```

**戻り値**:
- 対応するTransform（ボーンがマッピングされている場合）
- null（ボーンがマッピングされていない場合、オプションボーンで未設定など）

### 4.2 Transformパスの取得

Transformからアニメーションパスを取得するには、Animatorを基準とした相対パスを構築する。

```csharp
/// <summary>
/// AnimatorからのTransformパスを取得する
/// </summary>
public static string GetTransformPath(Animator animator, Transform bone)
{
    if (bone == null || animator == null)
        return string.Empty;

    // Animator自身の場合は空文字
    if (bone == animator.transform)
        return string.Empty;

    // パスを構築
    var path = new System.Collections.Generic.List<string>();
    Transform current = bone;

    while (current != null && current != animator.transform)
    {
        path.Insert(0, current.name);
        current = current.parent;
    }

    // Animator配下にない場合
    if (current == null)
        return string.Empty;

    return string.Join("/", path);
}
```

---

## 5. Humanoidマッスルカーブ

### 5.1 マッスルカーブの概念

Humanoidアニメーションでは、ボーンの回転を直接指定するのではなく「マッスル値」で指定する。

- **マッスル値**: -1.0～1.0の正規化された値
- **目的**: 異なるボーン構造のキャラクター間でアニメーションを共有可能にする

### 5.2 マッスルカーブのプロパティパス形式

Humanoidアニメーションカーブのパス形式:
```
// ボーンの回転（マッスル）
"<ボーン名>.<軸>"

例:
"LeftUpperArm.x"       // 左上腕のtwist
"LeftUpperArm.y"       // 左上腕の左右曲げ
"LeftUpperArm.z"       // 左上腕の前後曲げ
"Spine.x"              // 背骨のtwist
"Head.x"               // 頭のtwist
```

### 5.3 マッスル名の一覧

HumanTraitクラスを使用してマッスル名を取得できる:

```csharp
// 全マッスル名を取得
string[] muscleNames = HumanTrait.MuscleName;

// マッスルインデックスからHumanBodyBonesへのマッピング
int boneIndex = HumanTrait.MuscleFromBone((int)HumanBodyBones.LeftUpperArm, 0);
```

**主要なマッスル** (全95種類):
```
Spine Front-Back
Spine Left-Right
Spine Twist Left-Right
Chest Front-Back
Chest Left-Right
Chest Twist Left-Right
...
Left Arm Down-Up
Left Arm Front-Back
Left Forearm Stretch
Left Forearm Twist In-Out
Left Hand Down-Up
Left Hand In-Out
...
```

### 5.4 マッスル値からRotationへの変換

```csharp
/// <summary>
/// マッスル値をTransformの回転に変換する
/// </summary>
public static Quaternion MuscleToRotation(
    Animator animator,
    HumanBodyBones bone,
    float muscleX,
    float muscleY,
    float muscleZ)
{
    // HumanPoseHandlerを使用してポーズを取得・適用
    var humanPose = new HumanPose();
    var handler = new HumanPoseHandler(animator.avatar, animator.transform);

    // 現在のポーズを取得
    handler.GetHumanPose(ref humanPose);

    // マッスル値を設定
    int muscleIndexX = HumanTrait.MuscleFromBone((int)bone, 0);
    int muscleIndexY = HumanTrait.MuscleFromBone((int)bone, 1);
    int muscleIndexZ = HumanTrait.MuscleFromBone((int)bone, 2);

    if (muscleIndexX >= 0) humanPose.muscles[muscleIndexX] = muscleX;
    if (muscleIndexY >= 0) humanPose.muscles[muscleIndexY] = muscleY;
    if (muscleIndexZ >= 0) humanPose.muscles[muscleIndexZ] = muscleZ;

    // ポーズを適用
    handler.SetHumanPose(ref humanPose);

    // Transformの回転を取得
    Transform boneTransform = animator.GetBoneTransform(bone);
    return boneTransform.localRotation;
}
```

---

## 6. ルートモーション

### 6.1 ルートモーションのカーブパス

Humanoidアニメーションにおけるルートモーションのカーブパス:

```csharp
// 位置
"RootT.x"  // X軸位置
"RootT.y"  // Y軸位置
"RootT.z"  // Z軸位置

// 回転（Quaternion）
"RootQ.x"  // Quaternion X
"RootQ.y"  // Quaternion Y
"RootQ.z"  // Quaternion Z
"RootQ.w"  // Quaternion W
```

### 6.2 ルートモーションの取得

```csharp
// ルートモーションカーブの取得
var bindings = AnimationUtility.GetCurveBindings(clip);

foreach (var binding in bindings)
{
    // ルートモーションのパスは空文字
    if (string.IsNullOrEmpty(binding.path))
    {
        if (binding.propertyName.StartsWith("RootT"))
        {
            // ルートの位置カーブ
        }
        else if (binding.propertyName.StartsWith("RootQ"))
        {
            // ルートの回転カーブ
        }
    }
}
```

### 6.3 ルートモーションをTransformカーブに変換

```csharp
/// <summary>
/// ルートモーションカーブをTransformカーブに変換する
/// </summary>
public static void ConvertRootMotionToTransformCurves(
    AnimationClip sourceClip,
    AnimationClip destClip,
    string rootBonePath)
{
    var bindings = AnimationUtility.GetCurveBindings(sourceClip);

    foreach (var binding in bindings)
    {
        if (!string.IsNullOrEmpty(binding.path)) continue;

        var curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
        if (curve == null) continue;

        string newPropertyName = null;

        // 位置カーブの変換
        if (binding.propertyName == "RootT.x")
            newPropertyName = "localPosition.x";
        else if (binding.propertyName == "RootT.y")
            newPropertyName = "localPosition.y";
        else if (binding.propertyName == "RootT.z")
            newPropertyName = "localPosition.z";
        // 回転カーブの変換
        else if (binding.propertyName == "RootQ.x")
            newPropertyName = "localRotation.x";
        else if (binding.propertyName == "RootQ.y")
            newPropertyName = "localRotation.y";
        else if (binding.propertyName == "RootQ.z")
            newPropertyName = "localRotation.z";
        else if (binding.propertyName == "RootQ.w")
            newPropertyName = "localRotation.w";

        if (newPropertyName != null)
        {
            destClip.SetCurve(
                rootBonePath,
                typeof(Transform),
                newPropertyName,
                curve);
        }
    }
}
```

---

## 7. HumanoidToGenericConverter 設計方針

### 7.1 クラス責務

`HumanoidToGenericConverter`クラスの責務:

1. **HumanBodyBones → Transformパス変換**
   - Animator.GetBoneTransform()でTransformを取得
   - Animatorからの相対パスを構築

2. **マッスルカーブ → Rotationカーブ変換**
   - HumanPoseHandlerを使用してマッスル値を適用
   - 各フレームでTransformの回転を取得してカーブを生成

3. **ルートモーション → Transformカーブ変換**
   - RootT/RootQカーブをlocalPosition/localRotationカーブに変換

### 7.2 変換フロー

```
Humanoid AnimationClip
    │
    ├─ マッスルカーブ検出
    │       │
    │       └─ HumanPoseHandlerで各フレームをサンプリング
    │               │
    │               └─ Transformの回転値を取得
    │                       │
    │                       └─ Rotationカーブを生成
    │
    ├─ ルートモーションカーブ検出
    │       │
    │       └─ RootT/RootQをlocalPosition/localRotationに変換
    │
    └─ Generic AnimationClip出力
```

### 7.3 サンプリング方針

マッスルカーブからRotationカーブへの変換では、全フレームをサンプリングする必要がある:

```csharp
/// <summary>
/// マッスルカーブをTransformカーブに変換する
/// </summary>
public AnimationClip ConvertToGenericClip(Animator animator, AnimationClip humanoidClip)
{
    var genericClip = new AnimationClip();
    genericClip.frameRate = humanoidClip.frameRate;

    float duration = humanoidClip.length;
    float sampleRate = 1.0f / humanoidClip.frameRate;

    var handler = new HumanPoseHandler(animator.avatar, animator.transform);
    var humanPose = new HumanPose();

    // 各ボーンのカーブを準備
    var rotationCurves = new Dictionary<HumanBodyBones, Dictionary<string, AnimationCurve>>();

    foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
    {
        if (bone == HumanBodyBones.LastBone) continue;

        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform == null) continue;

        rotationCurves[bone] = new Dictionary<string, AnimationCurve>
        {
            { "localRotation.x", new AnimationCurve() },
            { "localRotation.y", new AnimationCurve() },
            { "localRotation.z", new AnimationCurve() },
            { "localRotation.w", new AnimationCurve() }
        };
    }

    // 各フレームをサンプリング
    for (float time = 0; time <= duration; time += sampleRate)
    {
        // クリップをサンプリング
        humanoidClip.SampleAnimation(animator.gameObject, time);

        // 各ボーンの回転を取得
        foreach (var kvp in rotationCurves)
        {
            Transform boneTransform = animator.GetBoneTransform(kvp.Key);
            if (boneTransform == null) continue;

            Quaternion rotation = boneTransform.localRotation;
            kvp.Value["localRotation.x"].AddKey(time, rotation.x);
            kvp.Value["localRotation.y"].AddKey(time, rotation.y);
            kvp.Value["localRotation.z"].AddKey(time, rotation.z);
            kvp.Value["localRotation.w"].AddKey(time, rotation.w);
        }
    }

    // カーブをAnimationClipに設定
    foreach (var kvp in rotationCurves)
    {
        Transform boneTransform = animator.GetBoneTransform(kvp.Key);
        string path = GetTransformPath(animator, boneTransform);

        foreach (var curveKvp in kvp.Value)
        {
            if (curveKvp.Value.keys.Length > 0)
            {
                genericClip.SetCurve(path, typeof(Transform), curveKvp.Key, curveKvp.Value);
            }
        }
    }

    return genericClip;
}
```

---

## 8. Unity API参照

### 8.1 関連クラス・構造体

| クラス/構造体 | 説明 |
|---------------|------|
| `HumanBodyBones` | Humanoidボーンの列挙型 |
| `HumanTrait` | Humanoidトレイト情報へのアクセス |
| `HumanPose` | Humanoidポーズデータ |
| `HumanPoseHandler` | Humanoidポーズの取得・設定 |
| `Avatar` | Avatarアセット |
| `AvatarBuilder` | Avatarの動的作成 |

### 8.2 HumanTraitクラスの主要メソッド

```csharp
// 全マッスル名を取得
string[] HumanTrait.MuscleName { get; }

// マッスル数を取得
int HumanTrait.MuscleCount { get; }

// ボーンからマッスルインデックスを取得
// dofIndex: 0=X, 1=Y, 2=Z
int HumanTrait.MuscleFromBone(int boneIndex, int dofIndex);

// マッスルからボーンインデックスを取得
int HumanTrait.BoneFromMuscle(int muscleIndex);

// ボーンの親を取得
int HumanTrait.GetParentBone(int boneIndex);

// 必須ボーンかどうか
bool HumanTrait.RequiredBone(int boneIndex);

// マッスルの最小・最大値を取得
float HumanTrait.GetMuscleDefaultMin(int muscleIndex);
float HumanTrait.GetMuscleDefaultMax(int muscleIndex);
```

---

## 9. 制限事項・注意点

### 9.1 既知の制限

1. **パフォーマンス**
   - 全フレームサンプリングはコストが高い
   - 長時間アニメーションでは処理時間に注意

2. **精度**
   - マッスル→Rotation変換で微小な誤差が発生する可能性
   - キーフレーム補間方式の違いによる差異

3. **非対応項目**
   - IK（Inverse Kinematics）カーブは変換不可
   - レイヤーウェイトカーブは変換不可

### 9.2 エッジケース

| ケース | 対応 |
|--------|------|
| オプションボーンが未設定 | スキップして処理続行 |
| マッスルカーブがない | Transform既存カーブのみ出力 |
| ルートモーションがない | 警告なしでスキップ |
| 非Humanoidクリップ | そのままコピー（変換不要） |

---

## 10. 参考資料

### 10.1 Unity公式ドキュメント

- [HumanBodyBones](https://docs.unity3d.com/ScriptReference/HumanBodyBones.html)
- [HumanTrait](https://docs.unity3d.com/ScriptReference/HumanTrait.html)
- [HumanPose](https://docs.unity3d.com/ScriptReference/HumanPose.html)
- [HumanPoseHandler](https://docs.unity3d.com/ScriptReference/HumanPoseHandler.html)
- [Animator.GetBoneTransform](https://docs.unity3d.com/ScriptReference/Animator.GetBoneTransform.html)
- [AnimationUtility](https://docs.unity3d.com/ScriptReference/AnimationUtility.html)

### 10.2 Mecanim関連

- [Mecanim Humanoids](https://docs.unity3d.com/Manual/AvatarCreationandSetup.html)
- [Muscle definitions](https://docs.unity3d.com/Manual/MuscleDefinitions.html)
- [Root Motion](https://docs.unity3d.com/Manual/RootMotion.html)

---

## 11. 次のステップ

1. **P14-002**: HumanoidToGenericConverterクラス設計
   - 本調査結果を元にクラス設計を行う
   - 変換フロー、インターフェース定義

2. **P14-003**: ボーン名→Transformパス変換のテスト作成
   - HumanBodyBones全種に対するパス変換テスト
   - オプションボーン未設定時のテスト

3. **P14-005**: マッスルカーブ→Rotation変換のテスト作成
   - サンプリング精度のテスト
   - 変換結果の正確性検証

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|------------|------|----------|
| 1.0 | 2026-01-27 | 初版作成 |
