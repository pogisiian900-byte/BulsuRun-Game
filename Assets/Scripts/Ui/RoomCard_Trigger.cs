using System.Collections;
using TMPro;
using UnityEngine;

public class RoomCard_Trigger : MonoBehaviour
{

    [SerializeField]private GameObject roomPanel;
    [SerializeField]private TextMeshProUGUI roomPanelText;
    [SerializeField] private string roomTitleText = "";
    [SerializeField] private float duration = 2f;
    
    private bool hasShown = false;


    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player") && !hasShown)
        {
            hasShown = true;
            StartCoroutine(showRoomName());
        }
    }
    private IEnumerator showRoomName()
    {
        roomPanelText.text = roomTitleText;
        roomPanel.SetActive(true);
        yield return new WaitForSeconds(duration);  

        roomPanel.SetActive(false);
    }
}
