using UnityEngine;
using System.Collections;

public class Play : MonoBehaviour
{
    public Player       player = new Player();
    public Avatar       avatar = new Avatar("log.gpx");
    public float        trackingInterval = 1f;
    public GameObject   avatarObj;

    private IEnumerator Start()
    {
        // 1. Set the starting position
        // Player : Where Player stands
        player.SetPosition();
        avatar.SetPosition(player.getRoute());
        // Avatar : Few steps behind the Player's Position
        //          and Set Player's Position as a first destination

        // 2. Wait for 1s to receive new GPS.
        // To prevent Position overlapping.
        while (true)
        {
            // Update Player's next location
            // using that location, set Avatar's Next location
            player.UpdateLocation();
            avatar.FindNextPoint(player.getRoute(), player.getTotalDist());
            yield return new WaitForSeconds(trackingInterval);
        }
    }

    private void FixedUpdate()
    {
        avatar.moveAvatar(avatarObj);
    }
}