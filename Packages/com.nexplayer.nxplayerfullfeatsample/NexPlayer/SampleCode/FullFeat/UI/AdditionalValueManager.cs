using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NexPlayerSample
{
    public class AdditionalValueManager : MonoBehaviour
    {
        public GameObject prefab;

        public InputField keyInputField;
        public InputField valueInputField;

        private Dictionary<string, string> additionalDRMHeaders;
        private Dictionary<string, string> additionalHTTPHeaders;

        // Start is called before the first frame update
        void Start()
        {
            additionalDRMHeaders = new Dictionary<string, string>();
            additionalHTTPHeaders = new Dictionary<string, string>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void GenerateAdditionalValue(int type)
        {
            string header_key = keyInputField.text;
            string header_value = valueInputField.text;

            if (!string.IsNullOrEmpty(header_key) && !string.IsNullOrEmpty(header_value))
            {
                GameObject newObj;

                newObj = (GameObject)Instantiate(prefab, transform);

                Text[] texts = newObj.GetComponentsInChildren<Text>();

                texts[0].text = header_key;
                texts[1].text = header_value;

                if (type == 0)
                {
                    additionalDRMHeaders.Add(header_key, header_value);
                }
                else if (type == 1)
                {
                    additionalHTTPHeaders.Add(header_key, header_value);
                }

                //Initialize the text field
                keyInputField.text = "";
                valueInputField.text = "";
            }
        }

        public Dictionary<string, string> GetAdditionalDRMHeaders()
        {
            return additionalDRMHeaders;
        }

        public Dictionary<string, string> GetAdditionalHTTPHeaders()
        {
            return additionalHTTPHeaders;
        }
    }
}
