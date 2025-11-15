using UnityEditor;

public class DataManagerReloader : AssetPostprocessor
{
    // Projedeki herhangi bir varlık değiştiğinde bu fonksiyon OTOMATİK çalışır.
    private static void OnPostprocessAllAssets(
        string[] importedAssets, 
        string[] deletedAssets, 
        string[] movedAssets, 
        string[] movedFromAssetPaths)
    {
        // Eğer DataManagerWindow penceresi şu anda açıksa
        if (EditorWindow.HasOpenInstances<IAWindow>())
        {
            // Açık olan pencereyi bul
            IAWindow window = EditorWindow.GetWindow<IAWindow>();
            
            // ve ona listeyi yenilemesini emret.
            window.RefreshDataLists();
        }
    }
}