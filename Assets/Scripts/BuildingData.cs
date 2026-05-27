using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Scriptable Objects/BuildingData")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string buildingName;


    public global::System.String BuildingName { get => buildingName; set => buildingName = value; }

}
