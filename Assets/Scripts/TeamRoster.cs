using UnityEngine;

[CreateAssetMenu(fileName = "TeamRoster", menuName = "Basketball/TeamRoster")]
public class TeamRoster : ScriptableObject
{
    public string[] teamNames = new string[16];
    public Sprite[] teamFlags = new Sprite[16];
}