using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class Play : MonoBehaviour
{
    private Player player;    
    private Avatar avatar;
    private float       trackingInterval = 1f;
    public GameObject   avatarAsset;
    public Text         avatarDistText;
    public Text         playerDistText;

    private IEnumerator Start()
    {
        player = new Player();
        avatar = new Avatar(Path.Combine(Application.persistentDataPath, "log4.txt"));
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
        playerDistText.text = player.getTotalDist().ToString();
        avatarDistText.text = avatar.getDist().ToString();
        Debug.Log("hihi");
        if (avatar == null)
            return; 
        avatar.moveAvatar(player.getRoute(), player.getSize());
    }
}