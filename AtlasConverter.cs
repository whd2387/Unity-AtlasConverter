using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

namespace stanworld
{
    public class AtlasConverter
    {
#if UNITY_EDITOR

        /// <summary>
        /// Key : fileName, Value : filePath
        /// </summary>
        private static Dictionary<string, string> imageDatas = new Dictionary<string, string>();
        private static List<int> textureSizes = new List<int>();
        private static List<string> folderNames;
        private const string basePath = "Assets/Art/UI";
        private const string atlasPath = "Assets/Art/UI/Atlas";
        private const string prefabResourcePath = "Assets/SGResources/Prefabs";
        private const string resourcePath = "Assets/Resources";

        /// <summary>
        /// Change Atlas Sprites To PNG Files
        /// </summary>
        [MenuItem("Tools/Atlas Converter/Extract Texture To PNG")]
        static void DoTextureMultipleSpritesToPNG()
        {
            // Find Atlas
            string[] spriteDirectoryInfos = Directory.GetFiles(atlasPath);
            List<Texture2D> atlasTextures = new List<Texture2D>();
            foreach (string directory in spriteDirectoryInfos)
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(directory, typeof(Texture2D));
                Texture2D texture = obj as Texture2D;
                if (texture != null)
                {
                    TextureImporter textureImporter = AssetImporter.GetAtPath(directory) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.SaveAndReimport();

                        atlasTextures.Add(texture);
                    }
                }
            }

            foreach (Texture2D atlasTexture in atlasTextures)
            {
                string spriteSheet = AssetDatabase.GetAssetPath(atlasTexture);
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheet)
                    .OfType<Sprite>().ToArray();

                string folderPath = $"Assets/Art/UI/{atlasTexture.name}";

                // Create Folder
                if (sprites.Length != 0)
                {
                    if (false == Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                }
                // Make PNG File
                foreach (Sprite sprite in sprites)
                {
                    int width = (int)sprite.rect.width;
                    int height = (int)sprite.rect.height;

                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    Color[] colors = sprite.texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
                    tex.SetPixels(colors);
                    tex.Apply();

                    byte[] bytes = tex.EncodeToPNG();
                    string path = string.Format($"{folderPath}/{sprite.name}.png");
                    File.WriteAllBytes(path, bytes);
                }
            }
        }

        /// <summary>
        /// Change Import Setting : TextureImporterType To Sprite, Fit Max Size
        /// </summary>
        [MenuItem("Tools/Atlas Converter/Change Texture Import Settings")]
        static void DoChangeTextureImportSetting()
        {
            DoInitTextureSize();
            DoSearchImageFolder();

            foreach (string folder in folderNames)
            {
                string folderPath = $"{basePath}/{folder}";
                if (Directory.Exists(folderPath))
                {
                    string[] filePaths = Directory.GetFiles(folderPath);

                    foreach (string filepath in filePaths)
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(filepath, typeof(Texture2D));
                        string path = AssetDatabase.GetAssetPath(obj);

                        Texture2D texture = obj as Texture2D;
                        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (textureImporter != null)
                        {
                            textureImporter.textureType = TextureImporterType.Sprite;

                            int biggerSize = texture.width > texture.height ? texture.width : texture.height;
                            textureImporter.maxTextureSize = DoCheckMaxSize(biggerSize);
                            textureImporter.SaveAndReimport();
                        }
                    }
                }
            }
        }

        static void DoInitTextureSize()
        {
            textureSizes.Clear();
            int initSize = 16;
            for (int i = 1; i <= 10; i++)
            {
                textureSizes.Add(initSize *= 2);
            }
            textureSizes.Sort();
        }

        static void DoSearchImageFolder()
        {
            folderNames = new List<string>();

            DirectoryInfo directoryInfo = new DirectoryInfo(atlasPath);
            foreach (var file in directoryInfo.GetFiles())
            {
                if (file.Name.Contains("meta")) continue;
                folderNames.Add(file.Name.Replace(".png", ""));
            }
        }

        /// <summary>
        /// Check Texture Max Size
        /// </summary>
        static int DoCheckMaxSize(int in_size)
        {
            for (int i = 0; i < textureSizes.Count; i++)
            {
                if (textureSizes[i] > in_size)
                {
                    return textureSizes[i];
                }
            }
            return textureSizes[textureSizes.Count - 1];
        }

        /// <summary>
        /// Change Prefab Image To New Image
        /// </summary>
        [MenuItem("Tools/Atlas Converter/Change Prefab Images")]
        static void DoChangePrefabsImage()
        {
            DoSearchImageFolder();
            DoSearchImageData();

            List<string> prefabPaths = Directory.GetFiles(prefabResourcePath).ToList();
            prefabPaths.AddRange(Directory.GetFiles(resourcePath).ToList());
            
            foreach (string prefabPath in prefabPaths)
            {
                if (!prefabPath.Contains(".prefab")) continue;
                if (prefabPath.Contains(".meta")) continue;

                GameObject myPrefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(prefabPath);
                Image[] images = myPrefab.GetComponentsInChildren<Image>(true);
                RawImage[] rawImages = myPrefab.GetComponentsInChildren<RawImage>(true);
                SpriteRenderer[] spriteRenderers = myPrefab.GetComponentsInChildren<SpriteRenderer>(true);
                MonoBehaviour[] scripts = myPrefab.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (Image imageScript in images)
                {
                    if (imageScript.sprite != null && imageDatas.ContainsKey(imageScript.sprite.name))
                    {
                        UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[imageScript.sprite.name], typeof(Sprite));
                        if (imageObject != null)
                            imageScript.sprite = imageObject as Sprite;
                        EditorUtility.SetDirty(imageScript);
                    }
                }

                foreach (RawImage rawImageScript in rawImages)
                {
                    if (rawImageScript.texture != null && imageDatas.ContainsKey(rawImageScript.texture.name))
                    {
                        UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[rawImageScript.texture.name], typeof(Texture2D));
                        if (imageObject != null)
                            rawImageScript.texture = imageObject as Texture2D;
                        EditorUtility.SetDirty(rawImageScript);
                    }
                }

                foreach (SpriteRenderer spriteRenderer in spriteRenderers)
                {
                    if (spriteRenderer.sprite != null && imageDatas.ContainsKey(spriteRenderer.sprite.name))
                    {
                        UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[spriteRenderer.sprite.name], typeof(Sprite));
                        if (imageObject != null) 
                        {
                            Sprite sprite = imageObject as Sprite;
                            if(sprite != null)
                                spriteRenderer.sprite = sprite; 
                        }
                        EditorUtility.SetDirty(spriteRenderer);
                    }
                }

                foreach (MonoBehaviour script in scripts)
                {
                    try
                    {
                        Type myType = script.GetType();
                        foreach (FieldInfo field in myType.GetFields())
                        {
                            object value = field.GetValue(script);
                            if (value as Sprite != null)
                            {
                                object spriteObj = value;
                                Sprite sprite = spriteObj as Sprite;
                                if (sprite != null && imageDatas.ContainsKey(sprite.name))
                                {
                                    UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[sprite.name], typeof(Sprite));
                                    if (imageObject != null)
                                        sprite = imageObject as Sprite;

                                    EditorUtility.SetDirty(sprite);
                                }
                            }

                            if (value as Sprite[] != null)
                            {
                                object spriteArrayObj = value;
                                Sprite[] array = spriteArrayObj as Sprite[];
                                for (int i = 0; i < array.Length; i++)
                                {
                                    if (array != null && imageDatas.ContainsKey(array[i].name))
                                    {
                                        UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[array[i].name], typeof(Sprite));
                                        if (imageObject != null)
                                            array[i] = imageObject as Sprite;

                                        EditorUtility.SetDirty(array[i]);
                                    }
                                }
                            }

                            if (value as List<Sprite> != null)
                            {
                                object spriteArrayObj = value;
                                List<Sprite> list = spriteArrayObj as List<Sprite>;
                                for (int i = 0; i < list.Count; i++)
                                {
                                    if (list != null && imageDatas.ContainsKey(list[i].name))
                                    {
                                        UnityEngine.Object imageObject = AssetDatabase.LoadAssetAtPath(imageDatas[list[i].name], typeof(Sprite));
                                        if (imageObject != null)
                                            list[i] = imageObject as Sprite;

                                        EditorUtility.SetDirty(list[i]);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                PrefabUtility.SavePrefabAsset(myPrefab);
            }
        }

        /// <summary>
        /// 이미지 파일 경로를 imageDatas에 저장.
        /// </summary>
        static void DoSearchImageData()
        {
            imageDatas.Clear();
            foreach (string folderName in folderNames)
            {
                string folderPath = $"{basePath}/{folderName}";
                if (Directory.Exists(folderPath))
                {
                    string[] filePaths = Directory.GetFiles(folderPath);
                    foreach (string path in filePaths)
                    {
                        string fileName = path.Replace(folderPath, "");
                        fileName = fileName.Replace("\\", "");
                        fileName = fileName.Replace(".png", "");
                        if (imageDatas.ContainsKey(fileName)) continue;
                        imageDatas.Add(fileName, path);
                    }
                }
            }
        }
#endif
    }
}