using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

namespace Shell.Protector
{
#if UNITY_EDITOR
    public class ProtectorCore : MonoBehaviour
    {
        public string asset_dir = "Assets/ShellProtect";
        public string pwd = "password"; //password

        public MeshRenderer m_renderer;

        EncryptTexture encrypt = new EncryptTexture();

        public static void SetRWEnableTexture(Texture2D texture)
        {
            if (texture.isReadable)
                return;
            string path = AssetDatabase.GetAssetPath(texture);
            string meta = File.ReadAllText(path + ".meta");

            meta = Regex.Replace(meta, "isReadable: 0", "isReadable: 1");
            File.WriteAllText(path + ".meta", meta);

            AssetDatabase.Refresh();
        }

        public void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder(asset_dir + '/' + gameObject.name))
                AssetDatabase.CreateFolder(asset_dir, gameObject.name);
            if (!AssetDatabase.IsValidFolder(Path.Combine(asset_dir, gameObject.name, "tex")))
                AssetDatabase.CreateFolder(Path.Combine(asset_dir, gameObject.name), "tex");
            if (!AssetDatabase.IsValidFolder(Path.Combine(asset_dir, gameObject.name, "mat")))
                AssetDatabase.CreateFolder(Path.Combine(asset_dir, gameObject.name), "mat");
            if (!AssetDatabase.IsValidFolder(Path.Combine(asset_dir, gameObject.name, "shader")))
                AssetDatabase.CreateFolder(Path.Combine(asset_dir, gameObject.name), "shader");
        }
        public GameObject DuplicateObject(GameObject obj)
        {
            GameObject cpy = Instantiate(obj);
            if (!obj.name.Contains("_encrypted"))
                cpy.name = obj.name + "_encrypted";
            return cpy;
        }

        Material CreateMaterial(Material mat, Shader shader, Texture2D main, Texture2D encrypt, Texture mip)
        {
            Material new_mat = new Material(mat.shader);
            new_mat.CopyPropertiesFromMaterial(mat);
            new_mat.shader = shader;
            var original_tex = new_mat.mainTexture;
            new_mat.mainTexture = main;
            new_mat.SetTexture("_EncryptTex", encrypt);
            new_mat.SetTexture("_MipTex", mip);

            return new_mat;
        }

        public void Encrypt()
        {
            CreateFolders();
            var mats = m_renderer.sharedMaterials;

            byte[] key = KeyGenerator.MakeKeyBytes("", pwd, 16);
            foreach(var i in key)
            {
                Debug.Log("Key bytes: " + string.Join(", ", key));
            }

            foreach (var mat in mats)
            {
                Texture2D main_texture = (Texture2D)mat.mainTexture;
                SetRWEnableTexture(main_texture);

                int size = main_texture.width;
                Texture2D mip = encrypt.GenerateRefMipmap(size, size);
                if (mip == null)
                    Debug.LogErrorFormat("{0} : Can't generate mip tex{1}.", mat.name, size);
                else
                {
                    AssetDatabase.CreateAsset(mip, Path.Combine(asset_dir, gameObject.name, "tex", "mip_" + size + ".asset"));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                var encrypted_tex = encrypt.TextureEncryptXXTEA(main_texture, key);
                if (encrypted_tex[0] == null)
                {
                    Debug.LogErrorFormat("{0} : encrypt failed.", main_texture.name);
                    continue;
                }
                string encrypt_tex_path = Path.Combine(asset_dir, gameObject.name, "tex", main_texture.name + "_encrypt.asset");
                string encrypt_tex2_path = Path.Combine(asset_dir, gameObject.name, "tex", main_texture.name + "_encrypt2.asset");
                //var png = encrypted_tex[0].EncodeToPNG();
                //File.WriteAllBytes(Path.Combine(asset_dir, gameObject.name, "tex", main_texture.name + "_encrypt.png"), png);
                AssetDatabase.CreateAsset(encrypted_tex[0], encrypt_tex_path);
                if (encrypted_tex[1] != null)
                    AssetDatabase.CreateAsset(encrypted_tex[1], encrypt_tex2_path);

                EditorGUIUtility.PingObject(encrypted_tex[0]);
            }
        }
    }

    [CustomEditor(typeof(ProtectorCore))]
    public class ProtectorCoreEditor : Editor
    {
        ProtectorCore root;

        readonly string[] filters = new string[2];

        bool show_pwd = false;

        private void OnEnable()
        {
            root = target as ProtectorCore;
            MonoScript monoScript = MonoScript.FromMonoBehaviour(root);
            string script_path = AssetDatabase.GetAssetPath(monoScript);
            root.asset_dir = Path.GetDirectoryName(Path.GetDirectoryName(script_path));
        }
        public override void OnInspectorGUI()
        {
            root.m_renderer = EditorGUILayout.ObjectField(root.m_renderer, typeof(MeshRenderer), true) as MeshRenderer;

            GUILayout.Label("Password", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (!show_pwd)
                root.pwd = GUILayout.PasswordField(root.pwd, '*', 16, GUILayout.Width(140));
            else
                root.pwd = GUILayout.TextField(root.pwd, 16, GUILayout.Width(140));

            if (GUILayout.Button("Show"))
                show_pwd = !show_pwd;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("암호화"))
            {
                root.Encrypt();
            }
        }
    }
}
#endif