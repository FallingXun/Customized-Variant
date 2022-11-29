using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CustomizedVariant;

public class VariantEditorTests : Editor
{
    /// <summary>
    /// 默认变体定制化标签名
    /// </summary>
    private const string Default_Tag = "Customized";

    [MenuItem("Assets/定制化变体/设置定制预制体变体名Tag")]
    public static void SetCustomizedVariantTag()
    {
        VariantUtility.SetCustomizedVariantTag(Default_Tag);

    }

    [MenuItem("Assets/定制化变体/创建定制预制体变体")]
    public static void CreateCustomizedVariantPrefab()
    {
        var objs = Selection.gameObjects;
        if (objs == null)
        {
            return;
        }
        foreach (var go in objs)
        {
            if (go == null)
            {
                continue;
            }
            VariantUtility.CreateCustomizedVariantPrefab(go);
        }
    }

    [MenuItem("Assets/定制化变体/输出定制预制体变体名Tag")]
    public static void PrintCustomizedVariantTag()
    {
        VariantUtility.PrintCustomizedVariantTag();
    }
}
