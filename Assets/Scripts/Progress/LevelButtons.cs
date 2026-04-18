using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public int levelIndex;

    public GameObject lockIcon;
    public GameObject realButtonImage;

    public Animator unlockAnimator;

    void Start()
    {
        int highestLevel = ProgressStore.GetHighestLevel();
        int justUnlocked = ProgressStore.GetJustUnlockedLevel();

        // already unlocked before
        if (levelIndex <= highestLevel && levelIndex != justUnlocked)
        {
            lockIcon.SetActive(false);
            realButtonImage.SetActive(true);
        }

        // play unlock animation
        else if (levelIndex == justUnlocked)
        {
            lockIcon.SetActive(true);
            realButtonImage.SetActive(false);

            StartCoroutine(PlayUnlock());
        }
        else
        {
            lockIcon.SetActive(true);
            realButtonImage.SetActive(false);
        }
    }

    System.Collections.IEnumerator PlayUnlock()
    {
        unlockAnimator.SetTrigger("Unlock");

        yield return new WaitForSeconds(1.5f); // animation length

        lockIcon.SetActive(false);
        realButtonImage.SetActive(true);

        ProgressStore.DeleteJustUnlockedLevel();
    }
}
