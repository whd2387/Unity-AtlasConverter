# Unity-Atlas Converter
------------

## 작성 이유
> UI 이미지들을 Atlas형태의 Multiple Sprite(Texture2D) 이미지들로 사용하고 있었습니다.   
> 기존 작업 방식으로는 이미지를 추가 및 삭제 등 변경할 때 마다 Art팀의 수작업이 필요했습니다.   
> 이 작업을 불필요한 작업으로 판단하고 Atlas 방식을 Unity Sprite Atlas 기능으로 사용하기로 결정하였습니다.   
> 그러기 위해선 기존 Atlas형태의 이미지들을 추출하여 새로운 PNG파일로 생성, Sprite 형태로 변환, 프리팹에 링크되어있는 모든 이미지들을 대체하는 작업이 필요했습니다.


## 사용 방법
> 1. 스크립트의 경로를 설정합니다.  
> 2. 유니티 상단의 메뉴의 Tools/Atlas Converter에서 필요한 기능을 선택합니다.   

<br><br />
## Code 설명
1. 이미지, 프리팹들의 해당 경로를 설정합니다.   
``` C#
        private const string basePath = "Assets/Art/UI";                        // UI 리소스 위치 경로를 설정합니다.
        private const string atlasPath = "Assets/Art/UI/Atlas";                 // UI 리소스 위치 내 Atlas형태의 Multiple Sprite(Texture2D) 이미지 위치 경로를 설정합니다.
        private const string prefabResourcePath = "Assets/SGResources/Prefabs"; // 프리팹의 위치 경로를 설정합니다.
```
<br><br />          
2. Texture2D로부터 PNG파일을 추출합니다.   
``` C#
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
```
<br><br />          
3. 추출한 PNG파일의 Import Settings를 변경합니다.   
+ 추출한 PNG파일들이 있는 폴더를 찾습니다.
+ 폴더 내 이미지들의 textureType을 Sprite 형태로 변경시켜줍니다.   
+ 이미지의 크기에 알맞게 Max Size를 변경합니다.
``` C#
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
```


<br><br />          
4. 프리팹의 이미지들을 새로 생성한 Texture로 변경합니다.   
+ 추출한 PNG파일들의 이름과 경로를 찾습니다.
+ 처음 설정한 프리팹 경로 내의 프리팹들을 찾습니다.
+ 프리팹들의 자식 오브젝트중에 Image, Raw Image Component 및 스크립트가 참조하고있는 Sprite를 모두 찾습니다.
+ 각각 링크되어있는 이미지들의 이름이 추출한 이미지 파일의 이름과 같으면 추출한 이미지 파일로 변경 후 저장해줍니다..
``` C#
[MenuItem("Tools/Atlas Converter/Change Prefab Images")]
        static void DoChangePrefabsImage()
        {
            DoSearchImageData();
            DoSearchImageFolder();

            string[] prefabPaths = Directory.GetFiles(prefabResourcePath);
            foreach (string prefabPath in prefabPaths)
            {
                if (prefabPath.Contains(".meta")) continue;

                GameObject myPrefab = (GameObject)AssetDatabase.LoadMainAssetAtPath(prefabPath);
                Image[] images = myPrefab.GetComponentsInChildren<Image>(true);
                RawImage[] rawImages = myPrefab.GetComponentsInChildren<RawImage>(true);
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
```
