using UnityEngine;

namespace Hikari.Puzzle {
    public class CellBlock : MonoBehaviour {
        public static Material[] materials = new Material[15]; //I,O,T,J,L,S,Z,Gbg

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadCellMaterials() {
            materials[0] = Resources.Load<Material>("Cells/CellI");
            materials[1] = Resources.Load<Material>("Cells/CellO");
            materials[2] = Resources.Load<Material>("Cells/CellT");
            materials[3] = Resources.Load<Material>("Cells/CellJ");
            materials[4] = Resources.Load<Material>("Cells/CellL");
            materials[5] = Resources.Load<Material>("Cells/CellS");
            materials[6] = Resources.Load<Material>("Cells/CellZ");
            materials[7] = Resources.Load<Material>("Cells/CellGarbage");
            materials[8] = Resources.Load<Material>("Cells/CellI_G");
            materials[9] = Resources.Load<Material>("Cells/CellO_G");
            materials[10] = Resources.Load<Material>("Cells/CellT_G");
            materials[11] = Resources.Load<Material>("Cells/CellJ_G");
            materials[12] = Resources.Load<Material>("Cells/CellL_G");
            materials[13] = Resources.Load<Material>("Cells/CellS_G");
            materials[14] = Resources.Load<Material>("Cells/CellZ_G");
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellI.mat").Completed += handle => materials[0] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellO.mat").Completed += handle => materials[1] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellT.mat").Completed += handle => materials[2] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellJ.mat").Completed += handle => materials[3] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellL.mat").Completed += handle => materials[4] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellS.mat").Completed += handle => materials[5] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellZ.mat").Completed += handle => materials[6] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellGarbage.mat").Completed += handle => materials[7] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellI_G.mat").Completed += handle => materials[8] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellO_G.mat").Completed += handle => materials[9] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellT_G.mat").Completed += handle => materials[10] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellJ_G.mat").Completed += handle => materials[11] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellL_G.mat").Completed += handle => materials[12] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellS_G.mat").Completed += handle => materials[13] = handle.Result;
            // Addressables.LoadAssetAsync<Material>("Assets/Materials/Cell/CellZ_G.mat").Completed += handle => materials[14] = handle.Result;
        }

        public int materialIndex;

        [ContextMenu("Apply Material")]
        private void Start() {
            UpdateMaterial();
        }

        public void UpdateMaterial() {
            GetComponent<Renderer>().material = materials[materialIndex];
        }
    }
}