using UnityEngine;
using System.Collections;
using System.IO;

public class Play : MonoBehaviour
{
    private Player      player = new Player();
    // Android: /data/data/package_name/files or /sdcard/Android/data/package_name/files (depending on the device and app permissions)
    private Avatar      avatar = new Avatar(Path.Combine(Application.streamingAssetsPath, "log.gpx"));
    private float       trackingInterval = 1f;
    public GameObject   avatarAsset;

    private IEnumerator Start()
    {
        // 1. Set the starting position
        avatar.FindAsset();
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