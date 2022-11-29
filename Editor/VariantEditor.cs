using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;

namespace CustomizedVariant
{

    public class VariantEditor : Editor
    {
        /// <summary>
        /// 打开中的资源路径
        /// </summary>
        private static string m_OpenedAssetPath = "";

        [InitializeOnLoadMethod]
        public static void Init()
        {
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        }

        public static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            // 避免更新打开中的预制体时触发更新循环打开
            if (prefabStage.assetPath.Equals(m_OpenedAssetPath))
            {
                return;
            }
            m_OpenedAssetPath = prefabStage.assetPath;
            //Debug.Log("OnPrefabStageOpened :" + m_OpenedAssetPath);
            if (IsCustomizedVariant(prefabStage) == false)
            {
                return;
            }
            VariantUtility.RevertTempModifications(prefabStage.assetPath);
            VariantUtility.ApplyTempModifications(prefabStage.assetPath);
            VariantUtility.CorrectCustomizedModifications(prefabStage.assetPath);
        }

        public static void OnPrefabStageClosing(PrefabStage prefabStage)
        {
            // 关闭预制体时，更新打开中的资源路径
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                m_OpenedAssetPath = PrefabStageUtility.GetCurrentPrefabStage().assetPath;
            }
            else
            {
                m_OpenedAssetPath = "";
            }
            if (IsCustomizedVariant(prefabStage) == false)
            {
                return;
            }
            VariantUtility.RevertTempModifications(prefabStage.assetPath);
        }

        public static bool IsCustomizedVariant(PrefabStage prefabStage)
        {
            var path = prefabStage.assetPath;
            if (path.EndsWith(VariantUtility.PostfixWithExtension_Customized) == false)
            {
                return false;
            }
            var root = prefabStage.prefabContentsRoot;
            if (PrefabUtility.IsPartOfVariantPrefab(root) == false)
            {
                return false;
            }
            return true;
        }

    }
}
