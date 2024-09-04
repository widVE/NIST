using UnityEngine;

namespace NexPlayerSample
{
    public class ExpandMenu : MonoBehaviour
    {
        private Animator anim;
        private bool isExpanded = false;
        private int previousClick = -1;
        public GameObject[] contents;
        public bool isClosed = true;

        void Start()
        {
            anim = GetComponent<Animator>();
        }

        public void CheckPreviousContent(int otherContent, int thisContent)
        {
            if (isClosed && previousClick != thisContent)
            {
                ExpandAnimation();
                ActivateContent(otherContent);
                isClosed = !isClosed;
            }


            ExpandAnimation();
            ActivateContent(thisContent);

            isClosed = !isClosed;
            previousClick = thisContent;
        }

        public void ShowContent(string content)
        {

            switch (content)
            {
                case "Languages":

                    CheckPreviousContent(1, 0);

                    break;
                case "Subtitles":
                    CheckPreviousContent(0, 1);
                    break;
                default:
                    break;
            }

        }

        public void ActivateContent(int content)
        {
            GameObject contentObject = contents[content];

            if (contentObject.activeInHierarchy)
            {
                contentObject.SetActive(false);
            }
            else
            {
                contentObject.SetActive(true);
            }
        }

        public void ExpandAnimation()
        {
            isExpanded = !isExpanded;
            anim.SetBool("Expanded", isExpanded);
        }

        public void ResetSubs(int content)
        {
            foreach (Transform child in contents[content].transform)
            {
                GameObject.Destroy(child.gameObject);
            }

        }
    }
}
