using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace NexPlayerAPI
{
    public class PostAndroidManifest : IPostGenerateGradleAndroidProject
    {
        private static AndroidSdkVersions targetSdkVersion;

        public int callbackOrder { get { return 0; } }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var androidManifest = new AndroidManifest(GetManifestPath(path));

            if (targetSdkVersion > AndroidSdkVersions.AndroidApiLevel28 || targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto)
            {
                androidManifest.SetPermisions();
            }

            androidManifest.Save();
        }

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {

                var pathBuilder = new StringBuilder(basePath);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }
            return _manifestFilePath;
        }

    }


    internal class AndroidXmlDocument : XmlDocument
    {
        private string m_Path;
        protected XmlNamespaceManager nsMgr;
        public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        public AndroidXmlDocument(string path)
        {
            m_Path = path;
            using (var reader = new XmlTextReader(m_Path))
            {
                reader.Read();
                Load(reader);
            }
            nsMgr = new XmlNamespaceManager(NameTable);
            nsMgr.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save()
        {
            return SaveAs(m_Path);
        }

        public string SaveAs(string path)
        {
            using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
            {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }

    internal class AndroidManifest : AndroidXmlDocument
    {
        private readonly XmlElement ApplicationElement;

        public AndroidManifest(string path) : base(path)
        {
            ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;

        }

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            XmlAttribute attr = CreateAttribute("android", key, AndroidXmlNamespace);
            attr.Value = value;
            return attr;
        }

        internal XmlNode GetActivityWithLaunchIntent()
        {
            return SelectSingleNode("/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                    "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", nsMgr);
        }
        internal void SetApplicationTheme(string appTheme)
        {
            ApplicationElement.Attributes.Append(CreateAndroidAttribute("theme", appTheme));
        }

        internal void SetStartingActivityName(string activityName)
        {
            GetActivityWithLaunchIntent().Attributes.Append(CreateAndroidAttribute("name", activityName));
        }
        internal void SetPermisions()
        {
            ApplicationElement.Attributes.Append(CreateAndroidAttribute("requestLegacyExternalStorage", "true")); // Template to set permissions on Android
        }
    }
}