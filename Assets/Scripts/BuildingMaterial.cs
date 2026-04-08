using UnityEngine;

[CreateAssetMenu(fileName = "BuildingMaterial", menuName = "Scriptable Objects/BuildingMaterial")]
public class BuildingMaterial : ScriptableObject
{
    [SerializeField]
    private string materialName;
    [SerializeField]
    private Material faceMaterial;
    [SerializeField]
    private Material sideMaterial;

    public Material FaceMaterial { get => faceMaterial; set => faceMaterial = value; }
    public Material SideMaterial { get => sideMaterial; set => sideMaterial = value; }
    public global::System.String MaterialName { get => materialName; set => materialName = value; }
}
