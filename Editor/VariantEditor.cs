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

        #region 工具方法
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
                var path_src = AssetDatabase.GetAssetPath(go);
                // 创建temp变体，用于应用子嵌套预设的变体修改信息
                var path_temp = VariantUtility.GetTempPath(path_src);
                var variant_temp = VariantUtility.CreateVariant(go, path_temp);
                // 创建customized变体，用于保存此预设的变体修改信息
                var path_customized = VariantUtility.GetCustomizedPath(path_src);
                var variant_customized = VariantUtility.CreateVariant(variant_temp, path_customized);

                Debug.Log("生成定制变体成功 : " + path_customized);
            }
        }

        [MenuItem("Assets/定制化变体/重置定制预制体变体名Tag")]
        public static void ResetCustomizedVariantTag()
        {
            VariantUtility.VariantTag = "";
            PrintCustomizedVariantTag();
        }

        [MenuItem("Assets/定制化变体/输出定制预制体变体名Tag")]
        public static void PrintCustomizedVariantTag()
        {
            Debug.LogFormat("当前预制体变体名Tag为 ： {0}", VariantUtility.VariantTag);
        }
        #endregion
    }
}
