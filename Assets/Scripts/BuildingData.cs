using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Scriptable Objects/BuildingData")]
public class BuildingData : ScriptableObject
{
    [SerializeField] private string buildingName;
    [SerializeField] private Sprite buildingSprite;

    public global::System.String BuildingName { get => buildingName; set => buildingName = value; }
    public Sprite BuildingSprite { get => buildingSprite; set => buildingSprite = value; }
}
