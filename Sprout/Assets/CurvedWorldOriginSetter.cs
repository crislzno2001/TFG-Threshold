using UnityEngine;

public class CurvedWorldOriginSetter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Curved World Materials")]
    [SerializeField] private Material[] curvedWorldMaterials;

    private static readonly int CurveOriginID = Shader.PropertyToID("_CurveOrigin");

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 origin = player.position;

        foreach (Material material in curvedWorldMaterials)
        {
            if (material != null)
            {
                material.SetVector(CurveOriginID, origin);
            }
        }
    }
}