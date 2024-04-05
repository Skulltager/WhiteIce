using SheetCodes;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlasGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Texture Atlas")]
    public static void ShowWindow()
    {
        TextureAtlasGenerator textureAtlasWindow = GetWindow<TextureAtlasGenerator>();
        textureAtlasWindow.Show();
    }

    private const int THREADS = 16;
    private const int TEXTURE_SIZE = 256;
    private string EDITOR_KEY_FILE_NAME => projectName + "TEXTURE ATLAS FILE NAME";
    private string EDITOR_KEY_WIDTH => projectName + "TEXTURE ATLAS WIDTH";
    private string EDITOR_KEY_TEXTURE_COUNT => projectName + "TEXTURE ATLAS TEXTURE COUNT";
    private string EDITOR_KEY_TEXTURE => projectName + "TEXTURE ATLAS TEXTURE {0}";

    private ComputeShader textureResizerShader;
    private int resizeTextureKernel;
    private string projectName;

    [SerializeField] private string fileName;
    [SerializeField] private Texture[] textures;
    [SerializeField] private int width;

    private void Awake()
    {
        projectName = PlayerSettings.productName;
        fileName = EditorPrefs.GetString(EDITOR_KEY_FILE_NAME, "");
        width = EditorPrefs.GetInt(EDITOR_KEY_WIDTH, 1);

        int textureCount = EditorPrefs.GetInt(EDITOR_KEY_TEXTURE_COUNT, 0);
        textures = new Texture[textureCount];
        for (int i = 0; i < textureCount; i++)
        {
            string key = string.Format(EDITOR_KEY_TEXTURE, i);
            string texturePath = EditorPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(texturePath))
                continue;

            textures[i] = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
        }

        //textureResizerShader = ComputeshaderIdentifier.TextureResizer.GetRecord().Shader;
        resizeTextureKernel = textureResizerShader.FindKernel("ResizeTexture");
    }

    private void OnGUI()
    {
        EditorWindow target = this;
        SerializedObject serializedObject = new SerializedObject(target);
        
        EditorGUILayout.BeginVertical();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fileName"), false);
        GUI.enabled = true;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), false);
        width = Mathf.Max(1, width);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("textures"), true);

        if (GUILayout.Button("Change Directory"))
        {
            string directory = string.IsNullOrEmpty(fileName) ? "" : Path.GetDirectoryName(fileName);
            string file = string.IsNullOrEmpty(fileName) ? "" : Path.GetFileName(fileName);
            string result = EditorUtility.SaveFilePanelInProject("what", file, "jpg", "message", directory);
            if (!string.IsNullOrEmpty(result))
                fileName = result;
        }

        GUI.enabled = !string.IsNullOrEmpty(fileName);
        if (GUILayout.Button("Generate Atlas"))
        {
            GenerateAtlas();
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void GenerateAtlas()
    {
        int height = Mathf.CeilToInt((float)textures.Length / width);
        RenderTexture renderTexture = new RenderTexture(width * TEXTURE_SIZE, height * TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, 0);
        renderTexture.enableRandomWrite = true;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Point;
        textureResizerShader.SetInt("targetWidth", TEXTURE_SIZE);
        textureResizerShader.SetInt("targetHeight", TEXTURE_SIZE);

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] == null)
                continue;

            int xIndex = i % width;
            int yIndex = (i - xIndex) / width;
            Texture texture = textures[i];
            if (texture.width != TEXTURE_SIZE || texture.height != TEXTURE_SIZE)
            {
                RenderTexture resizedTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0, RenderTextureFormat.ARGB32, 0);
                resizedTexture.enableRandomWrite = true;
                resizedTexture.wrapMode = TextureWrapMode.Clamp;
                resizedTexture.filterMode = FilterMode.Point;
                float widthRatio = (float) texture.width / TEXTURE_SIZE;
                float heightRatio = (float) texture.height / TEXTURE_SIZE;

                textureResizerShader.SetInt("sourceWidth", texture.width);
                textureResizerShader.SetInt("sourceHeight", texture.height);
                textureResizerShader.SetFloat("widthRatio", widthRatio);
                textureResizerShader.SetFloat("heightRatio", heightRatio);
                textureResizerShader.SetTexture(resizeTextureKernel, "targetTexture", resizedTexture);
                textureResizerShader.SetTexture(resizeTextureKernel, "sourceTexture", texture);
                textureResizerShader.Dispatch(resizeTextureKernel, Mathf.CeilToInt((float)TEXTURE_SIZE / THREADS), Mathf.CeilToInt((float)TEXTURE_SIZE / THREADS), 1);

                Graphics.CopyTexture(resizedTexture, 0, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE, renderTexture, 0, 0, xIndex * TEXTURE_SIZE, yIndex * TEXTURE_SIZE);
            }
            else
            {

                Graphics.CopyTexture(texture, 0, 0, 0, 0, TEXTURE_SIZE, TEXTURE_SIZE, renderTexture, 0, 0, xIndex * TEXTURE_SIZE, yIndex * TEXTURE_SIZE);
            }
        }

        RenderTexture.active = renderTexture;
        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(fileName, bytes);
        AssetDatabase.ImportAsset(fileName);

        TextureImporter textureImporter = TextureImporter.GetAtPath(fileName) as TextureImporter;

        TextureImporterSettings test = new TextureImporterSettings();
        test.filterMode = FilterMode.Point;
        test.wrapMode = TextureWrapMode.Repeat;
        test.textureType = TextureImporterType.Default;
        test.textureShape = TextureImporterShape.Texture2DArray;
        test.mipmapEnabled = true;
        test.readable = true;
        test.flipbookColumns = width;
        test.flipbookRows = height;

        textureImporter.SetTextureSettings(test);
        textureImporter.SaveAndReimport();
    }

    private void OnDestroy()
    {
        EditorPrefs.SetString(EDITOR_KEY_FILE_NAME, fileName);
        EditorPrefs.SetInt(EDITOR_KEY_WIDTH, width);
        EditorPrefs.SetInt(EDITOR_KEY_TEXTURE_COUNT, textures.Length);
    
        for (int i = 0; i < textures.Length; i++)
        {
            string key = string.Format(EDITOR_KEY_TEXTURE, i);
            string texturePath = textures[i] == null ? "" : AssetDatabase.GetAssetPath(textures[i]);
            EditorPrefs.SetString(key, texturePath);
        }
    }
}
