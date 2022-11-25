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
        /// 默认变体定制化标签名
        /// </summary>
        private const string Default_Tag = "Customized";
        /// <summary>
        /// 预制体文件扩展名
        /// </summary>
        public const string Extension_Prefab = ".prefab";

        /// <summary>
        /// 定制化变体标签名
        /// </summary>
        private static string m_VariantTag = Default_Tag;


        /// <summary>
        /// 变体标签名，变体名后缀为 "_" + VariantTag 
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
        /// 定制化变体后缀
        /// </summary>
        public static string Postfix_Customized
        {
            get
            {
                return string.Format("_{0}", VariantTag);
            }
        }

        /// <summary>
        /// 临时变体后缀
        /// </summary>
        public static string Postfix_Temp
        {
            get
            {
                return string.Format("_{0}Temp", VariantTag);
            }
        }

        /// <summary>
        /// 定制化变体后缀带扩展名
        /// </summary>
        public static string PostfixWithExtension_Customized
        {
            get
            {
                return Postfix_Customized + Extension_Prefab;
            }
        }

        /// <summary>
        /// 临时变体后缀带扩展名
        /// </summary>
        public static string PostfixWithExtension_Temp
        {
            get
            {
                return Postfix_Temp + Extension_Prefab;
            }
        }


        /// <summary>
        /// 创建变体
        /// </summary>
        /// <param name="pref">预制体</param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GameObject CreateVariant(GameObject pref, string path)
        {
            var t = typeof(PrefabUtility);
            var method = t.GetMethod("CreateVariant", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method == null)
            {
                Debug.LogError("获取 PrefabUtility.CreateVariant 方法失败!");
                return null;
            }
            var variant = method.Invoke(null, new object[] { pref, path }) as GameObject;
            if (variant == null)
            {
                Debug.LogError("生成定制变体失败: " + AssetDatabase.GetAssetPath(pref));
                return null;
            }
            var rtf_pref = pref.GetComponent<RectTransform>();
            var rtf_variant = variant.GetComponent<RectTransform>();
            if (rtf_pref != null && rtf_variant != null)
            {
                // RectTransform 数据会丢失，创建变体后重新设置
                EditorUtility.CopySerialized(rtf_pref, rtf_variant);
                EditorUtility.SetDirty(variant);
                AssetDatabase.SaveAssets();
            }
            return variant;
        }

        /// <summary>
        /// 应用temp的修改（将子预制体的定制修改应用到temp预制体上）
        /// </summary>
        /// <param name="path_customized">定制预制体变体路径</param>
        public static void ApplyTempModifications(string path_customized)
        {
            var pref_temp = AssetDatabase.LoadAssetAtPath<GameObject>(GetTempPath(path_customized));
            if (pref_temp == null)
            {
                return;
            }

            GameObject instance_temp = PrefabUtility.InstantiatePrefab(pref_temp) as GameObject;

            #region 1. 初始化相关结构，_pref 用于保存 temp prefab 的相关信息，_instance 用于保存 temp instance 的相关信息
            // 待检查的子节点队列，检查子节点的GameObject是否为某个prefab的根节点
            Queue<Transform> queue_pref = new Queue<Transform>();
            Queue<Transform> queue_instance = new Queue<Transform>();
            // 所有嵌套预制的栈，从上往下存入栈中
            Stack<GameObject> stack_pref = new Stack<GameObject>();
            Stack<GameObject> stack_instance = new Stack<GameObject>();
            #endregion

            #region 2. 遍历所有子节点，加入队列中
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

            #region 3. 检查所有子节点，检查是否为某个prefab的根节点，如果是则加入栈中。如果不为叶子节点，则继续将其子节点加入队列
            while (queue_pref.Count > 0)
            {
                var child = queue_pref.Dequeue();
                var child_instance = queue_instance.Dequeue();
                // 检查是否为prefab的根节点
                bool isPrefabRoot = PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject);
                if (isPrefabRoot)
                {
                    stack_pref.Push(child.gameObject);
                    stack_instance.Push(child_instance.gameObject);
                }
                // 如果此节点还有子节点，则继续加入队列中
                var childCount = child.childCount;
                for (int j = 0; j < childCount; j++)
                {
                    queue_pref.Enqueue(child.GetChild(j));
                    queue_instance.Enqueue(child_instance.GetChild(j));
                }
            }
            #endregion

            #region 4. 自底向上检查所有嵌套预制体，将customized变体的信息应用到temp变体上 
            #region 4.1 处理AddComponent、RemoveComponent、AddGameObject，在instance上修改，保存更新到 prefab 上，供后续 PropertyModification 修改使用
            while (stack_instance.Count > 0)
            {
                var pref_child_instance = stack_instance.Pop();
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pref_child_instance);
                var customized = AssetDatabase.LoadAssetAtPath<GameObject>(GetCustomizedPath(path));
                if (customized != null)
                {
                    // 应用新增的GameObject节点，只能在每个子节点最后的位置添加（先应用新增，保证后续Component能引用到）
                    var addGameObjects = PrefabUtility.GetAddedGameObjects(customized);
                    foreach (var addGameObject in addGameObjects)
                    {
                        var target_old = addGameObject.instanceGameObject;
                        Transform parent_old = target_old.transform.parent;
                        Transform parent_new = (GetCorrespondingObject(parent_old, pref_child_instance.transform) as Transform);
                        var target_new = GameObject.Instantiate(target_old, parent_new);
                        target_new.name = target_old.name;
                    }

                    // 应用移除的Component组件（先移除再添加，防止新加的组件和现有组件冲突）
                    var removeComponents = PrefabUtility.GetRemovedComponents(customized);
                    foreach (var removeComponent in removeComponents)
                    {
                        Transform target_old = removeComponent.containingInstanceGameObject.transform;
                        Transform target_new = (GetCorrespondingObject(target_old, pref_child_instance.transform) as Transform);
                        var type = removeComponent.assetComponent.GetType();
                        var assetComponent = target_new.GetComponent(type);
                        GameObject.DestroyImmediate(assetComponent);
                    }

                    // 应用新增的Component组件
                    var addComponnets = PrefabUtility.GetAddedComponents(customized);
                    foreach (var addComponent in addComponnets)
                    {
                        Transform target_old = addComponent.instanceComponent.transform;
                        Transform target_new = (GetCorrespondingObject(target_old, pref_child_instance.transform) as Transform);
                        var type = addComponent.instanceComponent.GetType();
                        var assetComponent = target_new.gameObject.AddComponent(type);
                        EditorUtility.CopySerialized(addComponent.instanceComponent, assetComponent);
                        // 将新增组件原来引用的 customized 子节点修改为此实例 instance 下的子节点
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

            #region 4.2 处理 PropertyModification 修改，在 prefab 上修改数据，再应用到 instance 上（SetPropertyModifications 方法的需求）
            List<PropertyModification> childProperties = new List<PropertyModification>();
            while (stack_pref.Count > 0)
            {
                var pref_child = stack_pref.Pop();
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pref_child);
                var customized = AssetDatabase.LoadAssetAtPath<GameObject>(GetCustomizedPath(path));
                if (customized != null)
                {
                    // 应用修改的属性
                    var properties = PrefabUtility.GetPropertyModifications(customized);
                    foreach (var property in properties)
                    {
                        // 名字修改 "_Temp" "_Customized" 不应用
                        if (property.propertyPath.Equals("m_Name"))
                        {
                            if (property.value.EndsWith(Postfix_Customized) || property.value.EndsWith(Postfix_Temp))
                            {
                                continue;
                            }
                        }

                        // 顺序变化不应用
                        if (property.propertyPath.Equals("m_RootOrder"))
                        {
                            continue;
                        }

                        // 修改 property.target 为 temp 变体的对应 object
                        property.target = GetCorrespondingObject(property.target, pref_child.transform);

                        // 修改 property.objectReference 为 temp 变体的对应 object
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
        /// 还原temp预制体的修改
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
                    // RectTransform 数据会丢失，创建变体后重新设置
                    EditorUtility.CopySerialized(rtf_origin, rtf_temp);
                    EditorUtility.SetDirty(pref_temp);
                    AssetDatabase.SaveAssets();
                }
            }

        }

        /// <summary>
        /// 修正customized预制体信息
        /// </summary>
        /// <param name="path_customized"></param>
        public static void CorrectCustomizedModifications(string path_customized)
        {
            var pref_customized = AssetDatabase.LoadAssetAtPath<GameObject>(path_customized);
            if (pref_customized == null)
            {
                return;
            }
            // 修正 customized 预制体新增的 GameObject ，排在子节点 customized 新增的 GameObject 后面
            var addedGameObjects = PrefabUtility.GetAddedGameObjects(pref_customized);
            foreach (var addedGameObject in addedGameObjects)
            {
                addedGameObject.instanceGameObject.transform.SetAsLastSibling();
            }
            EditorUtility.SetDirty(pref_customized);
            AssetDatabase.SaveAssets();

            // 如果当前打开着预制体，则将数据更新到当前预制体中
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
        /// 解除定制变体的关系
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
        /// 获取 prefab 和 target 对应层级的 transform
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
                throw new Exception(string.Format("target_old 未处理的对象类型：{0}  {1}", target.name, target.GetType()));

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
                throw new Exception(string.Format("target_new 未处理的对象类型：{0}  {1}", target.name, target.GetType()));
            }

            return correspondingObject;
        }

        /// <summary>
        /// 是否为子节点物体
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
        /// 获取定制预制体路径
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
        /// 获取临时预制体路径
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
        /// 获取源预制体路径
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

