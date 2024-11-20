using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sign
{
    public class SignItemView : MonoBehaviour
    {

        public Image dirImage;
        public Image signImage;
        public TextMeshProUGUI LocationNameTextMeshPro;
        

        public void SetData(SignItemData data)
        {
            dirImage.transform.localRotation = Quaternion.Euler(0, 0, (int)data.arrow_direction * 90);
            LocationNameTextMeshPro.text = data.locationName;
            signImage.sprite = data.img;
        }




    }
}