using UnityEngine;

[CreateAssetMenu(fileName = "TestEntryDatabase", menuName = "Scriptable Objects/TestEntryDatabase")]
public class TestEntryDatabase : ScriptableObject
{
    public Entry[] entries;
}
