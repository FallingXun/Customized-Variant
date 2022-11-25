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
    public class VariantUtility
    {
        /// <summary>
        /// Ĭ�ϱ��嶨�ƻ���ǩ��
        /// </summary>
        private const string Default_Tag = "Customized";
        /// <summary>
        /// Ԥ�����ļ���չ��
        /// </summary>
        public const string Extension_Prefab = ".prefab";

        /// <summary>
        /// ���ƻ������ǩ��
        /// </summary>
        private static string m_VariantTag = Default_Tag;


        /// <summary>
        /// �����ǩ������������׺Ϊ "_" + VariantTag 
        /// </summary>
        /// <param name="tag"></param>
        public static string VariantTag
        {
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    m_VariantTag = Default_Tag;
                }
                else
                {
                    m_VariantTag = value;
                }
            }
            get
            {
                return m_VariantTag;
            }
        }

        /// <summary>
        /// ���ƻ������׺
        /// </summary>
        public static string Postfix_Customized
        {
            get
            {
                return string.Format("_{0}", VariantTag);
            }
        }

        /// <summary>
        /// ��ʱ�����׺
        /// </summary>
        public static string Postfix_Temp
        {
            get
            {
                return string.Format("_{0}Temp", VariantTag);
            }
        }

        /// <summary>
        /// ���ƻ������׺����չ��
        /// </summary>
        public static string PostfixWithExtension_Customized
        {
            get
            {
                return Postfix_Customized + Extension_Prefab;
            }
        }

        /// <summary>
        /// ��ʱ�����׺����չ��
        /// </summary>
        public static string PostfixWithExtension_Temp
        {
            get
            {
                return Postfix_Temp + Extension_Prefab;
            }
        }


        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="pref">Ԥ����</param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GameObject CreateVariant(GameObject pref, string path)
        {
            var t = typeof(PrefabUtility);
            var method = t.GetMethod("CreateVariant", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError("��ȡ PrefabUtility.CreateVariant ����ʧ��!");
                return null;
            }
            var variant = method.Invoke(null, new object[] { pref, path }) as GameObject;
            if (variant == null)
            {
                Debug.LogError("���ɶ��Ʊ���ʧ��: " + AssetDatabase.GetAssetPath(pref));
                return null;
            }
            var rtf_pref = pref.GetComponent<RectTransform>();
            var rtf_variant = variant.GetComponent<RectTransform>();
            if (rtf_pref != null && rtf_variant != null)
            {
                // RectTransform ���ݻᶪʧ�������������������
                EditorUtility.CopySerialized(rtf_pref, rtf_variant);
                EditorUtility.SetDirty(variant);
                AssetDatabase.SaveAssets();
            }
            return variant;
        }

        /// <summary>
        /// Ӧ��temp���޸ģ�����Ԥ����Ķ����޸�Ӧ�õ�tempԤ�����ϣ�
        /// </summary>
        /// <param name="path_customized">����Ԥ�������·��</param>
        public static void ApplyTempModifications(string path_customized)
        {
            var pref_temp = AssetDatabase.LoadAssetAtPath<GameObject>(GetTempPath(path_customized));
            if (pref_temp == null)
            {
                return;
            }

            GameObject instance_temp = PrefabUtility.InstantiatePrefab(pref_temp) as GameObject;

            #region 1. ��ʼ����ؽṹ��_pref ���ڱ��� temp prefab �������Ϣ��_instance ���ڱ��� temp instance �������Ϣ
            // �������ӽڵ���У�����ӽڵ��GameObject�Ƿ�Ϊĳ��prefab�ĸ��ڵ�
            Queue<Transform> queue_pref = new Queue<Transform>();
            Queue<Transform> queue_instance = new Queue<Transform>();
            // ����Ƕ��Ԥ�Ƶ�ջ���������´���ջ��
            Stack<GameObject> stack_pref = new Stack<GameObject>();
            Stack<GameObject> stack_instance = new Stack<GameObject>();
            #endregion

            #region 2. ���������ӽڵ㣬���������
            Transform tf_temp = pref_temp.transform;
            Transform tf_temp_instance = instance_temp.transform;
            var count = tf_temp.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = tf_temp.GetChild(i);
                queue_pref.Enqueue(child);
                var child_instance = tf_temp_instance.GetChild(i);
                queue_instance.Enqueue(child_instance);
            }
            #endregion

            #region 3. ��������ӽڵ㣬����Ƿ�Ϊĳ��prefab�ĸ��ڵ㣬����������ջ�С������ΪҶ�ӽڵ㣬����������ӽڵ�������
            while (queue_pref.Count > 0)
            {
                var child = queue_pref.Dequeue();
                var child_instance = queue_instance.Dequeue();
                // ����Ƿ�Ϊprefab�ĸ��ڵ�
                bool isPrefabRoot = PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject);
                if (isPrefabRoot)
                {
                    stack_pref.Push(child.gameObject);
                    stack_instance.Push(child_instance.gameObject);
                }
                // ����˽ڵ㻹���ӽڵ㣬��������������
                var childCount = child.childCount;
                for (int j = 0; j < childCount; j++)
                {
                    queue_pref.Enqueue(child.GetChild(j));
                    queue_instance.Enqueue(child_instance.GetChild(j));
                }
            }
            #endregion

            #region 4. �Ե����ϼ������Ƕ��Ԥ���壬��customized�������ϢӦ�õ�temp������ 
            #region 4.1 ����AddComponent��RemoveComponent��AddGameObject����instance���޸ģ�������µ� prefab �ϣ������� PropertyModification �޸�ʹ��
            while (stack_instance.Count > 0)
            {
                var pref_child_instance = stack_instance.Pop();
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pref_child_instance);
                var customized = AssetDatabase.LoadAssetAtPath<GameObject>(GetCustomizedPath(path));
                if (customized != null)
                {
                    // Ӧ��������GameObject�ڵ㣬ֻ����ÿ���ӽڵ�����λ����ӣ���Ӧ����������֤����Component�����õ���
                    var addGameObjects = PrefabUtility.GetAddedGameObjects(customized);
                    foreach (var addGameObject in addGameObjects)
                    {
                        var target_old = addGameObject.instanceGameObject;
                        Transform parent_old = target_old.transform.parent;
                        Transform parent_new = (GetCorrespondingObject(parent_old, pref_child_instance.transform) as Transform);
                        var target_new = GameObject.Instantiate(target_old, parent_new);
                        target_new.name = target_old.name;
                    }

                    // Ӧ���Ƴ���Component��������Ƴ�����ӣ���ֹ�¼ӵ���������������ͻ��
                    var removeComponents = PrefabUtility.GetRemovedComponents(customized);
                    foreach (var removeComponent in removeComponents)
                    {
                        Transform target_old = removeComponent.containingInstanceGameObject.transform;
                        Transform target_new = (GetCorrespondingObject(target_old, pref_child_instance.transform) as Transform);
                        var type = removeComponent.assetComponent.GetType();
                        var assetComponent = target_new.GetComponent(type);
                        GameObject.DestroyImmediate(assetComponent);
                    }

                    // Ӧ��������Component���
                    var addComponnets = PrefabUtility.GetAddedComponents(customized);
                    foreach (var addComponent in addComponnets)
                    {
                        Transform target_old = addComponent.instanceComponent.transform;
                        Transform target_new = (GetCorrespondingObject(target_old, pref_child_instance.transform) as Transform);
                        var type = addComponent.instanceComponent.GetType();
                        var assetComponent = target_new.gameObject.AddComponent(type);
                        EditorUtility.CopySerialized(addComponent.instanceComponent, assetComponent);
                        // ���������ԭ�����õ� customized �ӽڵ��޸�Ϊ��ʵ�� instance �µ��ӽڵ�
                        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        if (fields != null)
                        {
                            foreach (var field in fields)
                            {
                                var value = field.GetValue(addComponent.instanceComponent);
                                if (value is Object)
                                {
                                    var obj_old = value as Object;
                                    if (obj_old != null && IsChildren(obj_old, customized))
                                    {
                                        var obj_new = GetCorrespondingObject(obj_old, pref_child_instance.transform);
                                        field.SetValue(assetComponent, obj_new);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            PrefabUtility.ApplyPrefabInstance(instance_temp, InteractionMode.UserAction);
            #endregion

            #region 4.2 ���� PropertyModification �޸ģ��� prefab ���޸����ݣ���Ӧ�õ� instance �ϣ�SetPropertyModifications ����������
            List<PropertyModification> childProperties = new List<PropertyModification>();
            while (stack_pref.Count > 0)
            {
                var pref_child = stack_pref.Pop();
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pref_child);
                var customized = AssetDatabase.LoadAssetAtPath<GameObject>(GetCustomizedPath(path));
                if (customized != null)
                {
                    // Ӧ���޸ĵ�����
                    var properties = PrefabUtility.GetPropertyModifications(customized);
                    foreach (var property in properties)
                    {
                        // �����޸� "_Temp" "_Customized" ��Ӧ��
                        if (property.propertyPath.Equals("m_Name"))
                        {
                            if (property.value.EndsWith(Postfix_Customized) || property.value.EndsWith(Postfix_Temp))
                            {
                                continue;
                            }
                        }

                        // ˳��仯��Ӧ��
                        if (property.propertyPath.Equals("m_RootOrder"))
                        {
                            continue;
                        }

                        // �޸� property.target Ϊ temp ����Ķ�Ӧ object
                        property.target = GetCorrespondingObject(property.target, pref_child.transform);

                        // �޸� property.objectReference Ϊ temp ����Ķ�Ӧ object
                        if (property.objectReference != null)
                        {
                            if (IsChildren(property.objectReference, customized))
                            {
                                property.objectReference = GetCorrespondingObject(property.objectReference, pref_child.transform);
                            }
                        }

                        childProperties.Add(property);
                    }
                }
            }
            PrefabUtility.SetPropertyModifications(instance_temp, childProperties.ToArray());
            PrefabUtility.ApplyPrefabInstance(instance_temp, InteractionMode.UserAction);
            #endregion
            #endregion

            GameObject.DestroyImmediate(instance_temp);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// ��ԭtempԤ������޸�
        /// </summary>
        /// <param name="path_customized"></param>
        public static void RevertTempModifications(string path_customized)
        {
            var pref_temp = AssetDatabase.LoadAssetAtPath<GameObject>(GetTempPath(path_customized));
            if (pref_temp == null)
            {
                return;
            }
            PrefabUtility.RevertPrefabInstance(pref_temp, InteractionMode.UserAction);
            AssetDatabase.SaveAssets();
            var pref_origin = AssetDatabase.LoadAssetAtPath<GameObject>(GetOriginPath(path_customized));
            if (pref_origin.GetComponent<Canvas>() != null)
            {
                var rtf_origin = pref_origin.GetComponent<RectTransform>();
                var rtf_temp = pref_temp.GetComponent<RectTransform>();
                if (rtf_origin != null && rtf_temp != null)
                {
                    // RectTransform ���ݻᶪʧ�������������������
                    EditorUtility.CopySerialized(rtf_origin, rtf_temp);
                    EditorUtility.SetDirty(pref_temp);
                    AssetDatabase.SaveAssets();
                }
            }

        }

        /// <summary>
        /// ����customizedԤ������Ϣ
        /// </summary>
        /// <param name="path_customized"></param>
        public static void CorrectCustomizedModifications(string path_customized)
        {
            var pref_customized = AssetDatabase.LoadAssetAtPath<GameObject>(path_customized);
            if (pref_customized == null)
            {
                return;
            }
            // ���� customized Ԥ���������� GameObject �������ӽڵ� customized ������ GameObject ����
            var addedGameObjects = PrefabUtility.GetAddedGameObjects(pref_customized);
            foreach (var addedGameObject in addedGameObjects)
            {
                addedGameObject.instanceGameObject.transform.SetAsLastSibling();
            }
            EditorUtility.SetDirty(pref_customized);
            AssetDatabase.SaveAssets();

            // �����ǰ����Ԥ���壬�����ݸ��µ���ǰԤ������
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.assetPath.Equals(path_customized))
            {
                var addedGameObjects_stage = PrefabUtility.GetAddedGameObjects(prefabStage.prefabContentsRoot);
                for (int i = addedGameObjects_stage.Count - 1; i >= 0; i--)
                {
                    addedGameObjects_stage[i].instanceGameObject.transform.SetSiblingIndex(addedGameObjects[i].instanceGameObject.transform.GetSiblingIndex());
                }
            }
        }

        /// <summary>
        /// ������Ʊ���Ĺ�ϵ
        /// </summary>
        /// <param name="path_customized"></param>
        public static void UnpackCustomizedVariant(string path_customized)
        {
            var customized = AssetDatabase.LoadAssetAtPath<GameObject>(path_customized);
            GameObject instance = PrefabUtility.InstantiatePrefab(customized) as GameObject;
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            PrefabUtility.SaveAsPrefabAsset(instance, path_customized);
            GameObject.DestroyImmediate(instance);
        }


        /// <summary>
        /// ��ȡ prefab �� target ��Ӧ�㼶�� transform
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pref"></param>
        /// <returns></returns>
        private static Object GetCorrespondingObject(Object target, Transform pref)
        {
            Transform target_old = null;
            Transform target_new = null;
            Object correspondingObject = null;
            if (target is GameObject)
            {
                target_old = (target as GameObject).transform;
            }
            else if (target is Component)
            {
                target_old = (target as Component).transform;
            }
            else
            {
                throw new Exception(string.Format("target_old δ����Ķ������ͣ�{0}  {1}", target.name, target.GetType()));

            }
            Stack<int> indexStack = new Stack<int>();
            target_new = pref.transform;
            while (target_old.parent != null)
            {
                indexStack.Push(target_old.GetSiblingIndex());
                target_old = target_old.parent;
            }
            while (indexStack.Count > 0)
            {
                var index = indexStack.Pop();
                target_new = target_new.GetChild(index);
            }
            if (target is GameObject)
            {
                correspondingObject = target_new.gameObject;
            }
            else if (target is Component)
            {
                correspondingObject = target_new.GetComponent(target.GetType());
            }
            else
            {
                throw new Exception(string.Format("target_new δ����Ķ������ͣ�{0}  {1}", target.name, target.GetType()));
            }

            return correspondingObject;
        }

        /// <summary>
        /// �Ƿ�Ϊ�ӽڵ�����
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="go"></param>
        /// <returns></returns>
        private static bool IsChildren(Object obj, GameObject go)
        {
            Transform tf = null;
            if (obj is GameObject)
            {
                tf = (obj as GameObject).transform;
            }
            else if (obj is Component)
            {
                tf = (obj as Component).transform;
            }
            while (tf != null)
            {
                if (tf == go.transform)
                {
                    return true;
                }
                tf = tf.parent;
            }
            return false;
        }

        /// <summary>
        /// ��ȡ����Ԥ����·��
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetCustomizedPath(string path)
        {
            if (path.EndsWith(PostfixWithExtension_Customized))
            {
                return path;
            }
            if (path.EndsWith(PostfixWithExtension_Temp))
            {
                return path.Replace(PostfixWithExtension_Temp, PostfixWithExtension_Customized);
            }
            else
            {
                return path.Replace(Extension_Prefab, PostfixWithExtension_Customized);
            }
        }

        /// <summary>
        /// ��ȡ��ʱԤ����·��
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTempPath(string path)
        {
            if (path.EndsWith(PostfixWithExtension_Temp))
            {
                return path;
            }
            if (path.EndsWith(PostfixWithExtension_Customized))
            {
                return path.Replace(PostfixWithExtension_Customized, PostfixWithExtension_Temp);
            }
            else
            {
                return path.Replace(Extension_Prefab, PostfixWithExtension_Temp);
            }
        }

        /// <summary>
        /// ��ȡԴԤ����·��
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetOriginPath(string path)
        {
            if (path.EndsWith(PostfixWithExtension_Temp))
            {
                return path.Replace(PostfixWithExtension_Temp, Extension_Prefab);
            }
            if (path.EndsWith(PostfixWithExtension_Customized))
            {
                return path.Replace(PostfixWithExtension_Customized, Extension_Prefab);
            }
            else
            {
                return path;
            }
        }
    }
}

