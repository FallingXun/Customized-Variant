using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CustomizedVariant;

public class VariantEditorTests : Editor
{
    /// <summary>
    /// Ĭ�ϱ��嶨�ƻ���ǩ��
    /// </summary>
    private const string Default_Tag = "Customized";

    [MenuItem("Assets/���ƻ�����/���ö���Ԥ���������Tag")]
    public static void SetCustomizedVariantTag()
    {
        VariantUtility.SetCustomizedVariantTag(Default_Tag);

    }

    [MenuItem("Assets/���ƻ�����/��������Ԥ�������")]
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

    [MenuItem("Assets/���ƻ�����/�������Ԥ���������Tag")]
    public static void PrintCustomizedVariantTag()
    {
        VariantUtility.PrintCustomizedVariantTag();
    }
}
