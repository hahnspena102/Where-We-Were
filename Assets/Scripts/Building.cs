using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private BuildingData buildingData;

    public BuildingData BuildingData { get => buildingData; set => buildingData = value; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
