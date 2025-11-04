using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;


public class QuaterniousUtils {

    [MenuItem("Tools/QuaterniousUtility/Apply Import Steps to Universal Animation Library")]
    public static void ApplyLoopToEveryClip() {
        var paths = Selection.GetFiltered<Object>(SelectionMode.Assets);
        if (paths.Length == 0) {
            Debug.LogWarning("No file selected.");
            return;
        }
        string path = AssetDatabase.GetAssetPath(paths[0]);
        
        if(string.IsNullOrEmpty(path)) {
            Debug.LogWarning("No file selected.");
            return;
        }
        
        
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer == null) {
            Debug.LogError("Selected file is not a valid model.");
            return;
        }
        importer.bakeAxisConversion = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.motionNodeName = "Rig/root";
        
        var animationImported = importer.clipAnimations;
        if (animationImported.Length == 0) {
            animationImported = importer.defaultClipAnimations;
        }
        
        foreach(var clipAnimation in animationImported) {


            if(clipAnimation != null && clipAnimation.name.EndsWith("Loop")) {
                clipAnimation.loopTime = true;  
            } else {
                clipAnimation.loopTime = false;
            }
        }
        importer.clipAnimations = animationImported;
        AssetDatabase.SaveAssetIfDirty(importer);
    }

    [MenuItem("Tools/QuaterniousUtility/Apply Collision Prefabs to Selected Quaternius Models")]
    public static void CombineCollisions()
    {
        Debug.Log("CombineCollisions called");
        QuaterniousUtils applyCollisionPrefabs = new QuaterniousUtils();
        applyCollisionPrefabs.ApplyCollisionPrefabsToAllImportedModels();
    }

    public void ApplyCollisionPrefabsToModel(GameObject modelInstance, GameObject collisionInstance)
    {
        MeshRenderer[] meshRenderers = collisionInstance.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {

            if (renderer.gameObject.GetComponent<Collider>() != null)
            {
                renderer.enabled = false;
                continue;

            }
            renderer.gameObject.AddComponent<MeshCollider>();
            renderer.enabled = false;
        }
        

        collisionInstance.transform.parent = modelInstance.transform;
        collisionInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        collisionInstance.transform.localScale = Vector3.one;


    }
    public void ApplyCollisionPrefabsToAllImportedModels()
    {


        string[] importedModelPaths = Selection.GetFiltered<Object>(SelectionMode.Assets)
            .Where(obj => AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GetAssetPath(obj)) == typeof(GameObject))
            .Select(obj => AssetDatabase.GetAssetPath(obj))
            .ToArray();
        foreach (string modelPath in importedModelPaths)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model != null)
            {
                GameObject modelInstance = Object.Instantiate(model);
                modelInstance.name = model.name;
                string prefabPath = Path.Join("Assets", "Prefabs", model.name + "_WithCollisions.prefab");
                string collisionpath = Path.Join(Path.GetDirectoryName(modelPath), "Collisions", "Collision_" + modelPath.Split(Path.AltDirectorySeparatorChar).Last());
                GameObject collisionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(collisionpath);
                if (collisionPrefab == null)
                {
                    Debug.LogWarning("No collision prefab found for model: " + model.name + " at path: " + collisionpath + "\n"
                    + "Creating prefab without collisions.");
                    PrefabUtility.SaveAsPrefabAsset(modelInstance, prefabPath);
                    GameObject.DestroyImmediate(modelInstance);
                    continue;
                }
                GameObject collisionInstance = Object.Instantiate(collisionPrefab);


                // Apply collision prefabs to the model instance
                ApplyCollisionPrefabsToModel(modelInstance, collisionInstance);

                // Save the modified model instance as a new prefab
                
                PrefabUtility.SaveAsPrefabAsset(modelInstance, prefabPath);

                // Clean up the instantiated model instance
                Object.DestroyImmediate(modelInstance);

                Debug.Log("Processed model: " + model.name);
            }
            else
            {
                Debug.LogError("Failed to load model at path: " + modelPath);
            }
        }
    }
}