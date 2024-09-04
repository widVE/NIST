using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEditor;


public class PrefabSystem 
{
    public List<SerializableObjectData> serializedGameObjects;
    ObjectData root; 
    int cont = 0;

    public void SaveGameObject(string path, GameObject go) 
    {
        BeforeSerialize(go);
        SerializedPrefab data = new SerializedPrefab(serializedGameObjects);
        if (path.Contains("json"))
        {
            string jSon = JsonUtility.ToJson(data);
            File.WriteAllText(path , jSon);
        }
        else
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = new FileStream(path, FileMode.Create);
            bf.Serialize(fs, data);
            fs.Close();
        }
        AssetDatabase.Refresh();
    }

    public GameObject CreatePrefab(string path)
    {
        SerializedPrefab serializedPrefab = LoadGameObject(path);
        GameObject[] gos = new GameObject[serializedPrefab.Objects.Count];
        for (int i = 0; i < serializedPrefab.Objects.Count; i++)
        {
            gos[i] = new GameObject(serializedPrefab.Objects[i].Name);
            gos[i].tag = serializedPrefab.Objects[i].Tag;
            gos[i].layer = serializedPrefab.Objects[i].Layer;
            // Parent
            if (serializedPrefab.Objects[i].ParentIndex >= 0)
            {
                gos[i].transform.SetParent(gos[serializedPrefab.Objects[i].ParentIndex].transform);
            }
            // Components
            for (int j = 0; j < serializedPrefab.Objects[i].Components.Length; j++)
            {
                if (serializedPrefab.Objects[i].Components[j].Name.Contains("PrefabDataCreator")) 
                    continue;

                string componentName = GetFormatedType(serializedPrefab.Objects[i].Components[j].Name);
                Component component;
                if (!componentName.Contains("UnityEngine.Transform"))
                    component = gos[i].AddComponent(Type.GetType(componentName));
                else
                    component = gos[i].transform;
                SerializedObject so = new SerializedObject(component);
                foreach (SerializedField field in serializedPrefab.Objects[i].Components[j].Fields)
                {
                    SerializedProperty sp = so.FindProperty(field.Name);
                    if (sp == null)
                        continue;
                    
                    switch (sp.propertyType)
                    {
                        case SerializedPropertyType.String:
                            sp.stringValue = field.Value;
                            break;
                        case SerializedPropertyType.Boolean:
                            sp.boolValue = Boolean.Parse(field.Value);
                            break;
                        case SerializedPropertyType.Enum:
                            sp.enumValueIndex = Int32.Parse(field.Value, CultureInfo.InvariantCulture);
                            break;
                        case SerializedPropertyType.Integer:
                            sp.intValue = Int32.Parse(field.Value, CultureInfo.InvariantCulture);
                            break;
                        case SerializedPropertyType.Float:
                            sp.floatValue = float.Parse(field.Value, CultureInfo.InvariantCulture);
                            break;
                        case SerializedPropertyType.Vector2:
                            sp.vector2Value = GetV2Value(field.Value);
                            break;
                        case SerializedPropertyType.Vector3:
                            sp.vector3Value = GetV3Value(field.Value);
                            break;
                        case SerializedPropertyType.Quaternion:
                            sp.quaternionValue = GetQuatValue(field.Value);
                            break;
                        case SerializedPropertyType.Color:
                            sp.colorValue = GetColorValue(field.Value);
                            break;
                        default:
                            break;
                    }
                }
                so.ApplyModifiedProperties();
            }
        }
        return gos[0];
    }

    void BeforeSerialize(GameObject go)
    {
        if (serializedGameObjects == null)
            serializedGameObjects = new List<SerializableObjectData>();
        if (root == null)
            root = new ObjectData(go);
        serializedGameObjects.Clear();
        cont = 0;
        AddGOToSerializedGameObjects(root, -1);
    }

    void AddGOToSerializedGameObjects(ObjectData d, int parent)
    {
        SerializableObjectData serializedGO = new SerializableObjectData(d, parent, ref cont);
        
        serializedGameObjects.Add(serializedGO);
        if (d.Children.Count > 0) {
            foreach (var child in d.Children)
                AddGOToSerializedGameObjects(child, serializedGO.Index);
        }
    }

    SerializedPrefab LoadGameObject(string path)
    {
        if (File.Exists(path))
        {
            if (path.Contains("json"))
            {
                string dataAsJson = File.ReadAllText(path);
                SerializedPrefab data = JsonUtility.FromJson<SerializedPrefab>(dataAsJson);
                return data;
            }
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(path, FileMode.Open);
                SerializedPrefab data = (SerializedPrefab)bf.Deserialize(fs);
                fs.Close();
                return data;
            }   
        }
        else
            return new SerializedPrefab();
    }

    #region String Helpers
    string GetFormatedType(string name)
    {
        string assembly = "NexPlayer.FullSample";
        string[] split = name.Split('.');
        if (split[0].Contains("Unity"))
        {
            string[] list = new string[split.Length - 1];
            for (int i = 0; i < split.Length - 1; i++) {
                list[i] = split[i];
            }
            assembly = string.Join(".", list);
        }
        return string.Concat(name, ", ", assembly);
    }

    Vector2 GetV2Value(string s)
    {
        var stringArray = s.Split(',', ' ', '(', ')');
        Vector2 result = new Vector2();
        result.x = float.Parse(stringArray[1], CultureInfo.InvariantCulture);
        result.y = float.Parse(stringArray[3], CultureInfo.InvariantCulture);
        return result;
    }

    Vector3 GetV3Value(string s)
    {
        var stringArray = s.Split(',', ' ', '(', ')');
        Vector3 result = new Vector3();
        result.x = float.Parse(stringArray[1], CultureInfo.InvariantCulture);
        result.y = float.Parse(stringArray[3], CultureInfo.InvariantCulture);
        result.z = float.Parse(stringArray[5], CultureInfo.InvariantCulture);
        return result;
    }

    Quaternion GetQuatValue(string s)
    {
        var stringArray = s.Split(',', ' ', '(', ')');
        Quaternion result = new Quaternion();
        result.x = float.Parse(stringArray[1], CultureInfo.InvariantCulture);
        result.y = float.Parse(stringArray[3], CultureInfo.InvariantCulture);
        result.z = float.Parse(stringArray[5], CultureInfo.InvariantCulture);
        result.w = float.Parse(stringArray[7], CultureInfo.InvariantCulture);
        return result;
    }

    Color GetColorValue(string s)
    {
        var stringArray = s.Split(',', ' ', '(', ')');
        Color result = new Color();
        result.r = float.Parse(stringArray[1], CultureInfo.InvariantCulture);
        result.g = float.Parse(stringArray[3], CultureInfo.InvariantCulture);
        result.b = float.Parse(stringArray[5], CultureInfo.InvariantCulture);
        result.a = float.Parse(stringArray[7], CultureInfo.InvariantCulture);
        return result;
    }
    #endregion

    #region DATA STRUCTURE
    public class ObjectData
    {
        public string Name;
        public string Tag;
        public int Layer;
        public List<ObjectData> Children;
        public Component[] Components;

        public ObjectData(GameObject obj) {
            Name = obj.name;
            Tag = obj.tag;
            Layer = obj.layer;
            // CHILDREN
            Children = new List<ObjectData>();
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Children.Add(new ObjectData(obj.transform.GetChild(i).gameObject));
            }
            // COMPONENTS
            Components = obj.GetComponents<Component>();
        }

        public ObjectData() {
            Name = "NewObject";
            Tag = "Untagged";
            Layer = 0;
            Components = null;
            Children = new List<ObjectData>();
        }
    }
    // Struct serializable que evita "no support for null"
    [Serializable] public struct SerializableObjectData
    {
        public string Name;
        public string Tag;
        public int Layer;
        public int Index;
        public int ParentIndex;
        public SerializedComponent[] Components;

        public SerializableObjectData(ObjectData data, int parent, ref int c)
        {
            Name = data.Name;
            Tag = data.Tag;
            Layer = data.Layer;
            Index = c++;
            ParentIndex = parent;
            if (data.Components == null) Components = null;
            else
            {
                Components = new SerializedComponent[data.Components.Length];
                for (int i = 0; i < data.Components.Length; i++)
                {
                    Components[i] = new SerializedComponent(data.Components[i]);
                }
            }
        }
    }

    [Serializable] public struct SerializedPrefab
    {
        public List<SerializableObjectData> Objects;
        public SerializedPrefab(List<SerializableObjectData> list)
        {
            Objects = list;
        }
    }

    [Serializable] public struct SerializedComponent
    {
        public string Name;
        public List<SerializedField> Fields;

        public SerializedComponent(Component component)
        {
            Name = component.GetType().ToString();
            Fields = new List<SerializedField>();
            SerializedObject so = new SerializedObject(component);
            SerializedProperty sp = so.GetIterator();
            while (sp.NextVisible(true))
            {
                if (sp.name.Length<2) continue;
                if(sp.name.Contains("Script"))continue;
                SerializedField sf = new SerializedField(sp);
                if (sf.Value.Equals("NonSupportedType")) continue;
                Fields.Add(new SerializedField(sp));
            }
        }
    }

    [Serializable] public struct SerializedField
    {
        public string Name;
        public string Value;

        public SerializedField(SerializedProperty sp)
        {
            Name = sp.propertyPath;
            string value = "NonSupportedType";
            switch (sp.propertyType)
            {
                case SerializedPropertyType.String:
                    value = sp.stringValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    value = sp.boolValue.ToString();
                    break;
                case SerializedPropertyType.Enum:
                    value = sp.enumValueIndex.ToString();
                    break;
                case SerializedPropertyType.Integer:
                    value = sp.intValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    value = sp.floatValue.ToString();
                    break;
                case SerializedPropertyType.Vector2:
                    value = sp.vector2Value.ToString();
                    break;
                case SerializedPropertyType.Vector3:
                    value = sp.vector3Value.ToString();
                    break;
                case SerializedPropertyType.Quaternion:
                    value = sp.quaternionValue.ToString();
                    break;
                case SerializedPropertyType.Color:
                    value = sp.colorValue.ToString();
                    break;
                default:
                    break;
            }
            Value = value;
        }
    }
    #endregion
}
