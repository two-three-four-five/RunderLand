using UnityEngine;
using System.Collections;
using System.IO;

public class Play : MonoBehaviour
{
    public Player       player = new Player();
    // Android: /data/data/package_name/files or /sdcard/Android/data/package_name/files (depending on the device and app permissions)
    public Avatar       avatar = new Avatar(Path.Combine(Application.streamingAssetsPath, "log.gpx"));
    public GameObject   avatarAsset;
    public float        trackingInterval = 1f;

    private IEnumerator Start()
    {
        // 1. Set the starting position       
        avatar.setAvatarAsset(avatarAsset);
        player.SetPosition();

        while (true)
        {
            // Update Player's next location            
            player.UpdateLocation();            
            yield return new WaitForSeconds(trackingInterval);
        }
    }

    private void FixedUpdate()
    {
        avatar.moveAvatar(player.getRoute(), player.getSize());
    }
}