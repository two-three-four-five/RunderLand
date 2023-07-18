using UnityEngine;
using System.Collections;

public class Play : MonoBehaviour
{
    public Player       player = new Player();
    public Avatar       avatar = new Avatar("log.gpx");
    public GameObject   avatarAsset;
    public float        trackingInterval = 1f;

    private IEnumerator Start()
    {
        // 1. Set the starting position
        // Player : Where Player stands
        avatar.setAvatarAsset(avatarAsset);
        player.SetPosition();

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
        avatar.moveAvatar(player.getRoute(), player.getSize());
    }
}