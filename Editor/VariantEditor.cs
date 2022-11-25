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
        /// ���е���Դ·��
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
            // ������´��е�Ԥ����ʱ��������ѭ����
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
            // �ر�Ԥ����ʱ�����´��е���Դ·��
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

        #region ���߷���
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
                var path_src = AssetDatabase.GetAssetPath(go);
                // ����temp���壬����Ӧ����Ƕ��Ԥ��ı����޸���Ϣ
                var path_temp = VariantUtility.GetTempPath(path_src);
                var variant_temp = VariantUtility.CreateVariant(go, path_temp);
                // ����customized���壬���ڱ����Ԥ��ı����޸���Ϣ
                var path_customized = VariantUtility.GetCustomizedPath(path_src);
                var variant_customized = VariantUtility.CreateVariant(variant_temp, path_customized);

                Debug.Log("���ɶ��Ʊ���ɹ� : " + path_customized);
            }
        }

        [MenuItem("Assets/���ƻ�����/���ö���Ԥ���������Tag")]
        public static void ResetCustomizedVariantTag()
        {
            VariantUtility.VariantTag = "";
            PrintCustomizedVariantTag();
        }

        [MenuItem("Assets/���ƻ�����/�������Ԥ���������Tag")]
        public static void PrintCustomizedVariantTag()
        {
            Debug.LogFormat("��ǰԤ���������TagΪ �� {0}", VariantUtility.VariantTag);
        }
        #endregion
    }
}
