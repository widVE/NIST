using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.QR;
namespace QRTracking
{
    public class QRCodesVisualizer : MonoBehaviour
    {
		[SerializeField]
		EasyVizARHeadsetManager _manager;
		
		[SerializeField]
		string _qrCodeWeWant;
		
        public GameObject qrCodePrefab;

        private System.Collections.Generic.SortedDictionary<System.Guid, GameObject> qrCodesObjectsList;
        private bool clearExisting = false;

        struct ActionData
        {
            public enum Type
            {
                Added,
                Updated,
                Removed
            };
            public Type type;
            public Microsoft.MixedReality.QR.QRCode qrCode;

            public ActionData(Type type, Microsoft.MixedReality.QR.QRCode qRCode) : this()
            {
                this.type = type;
                qrCode = qRCode;
            }
        }

        private System.Collections.Generic.Queue<ActionData> pendingActions = new Queue<ActionData>();
        void Awake()
        {

        }

        // Use this for initialization
        void Start()
        {
            Debug.Log("QRCodesVisualizer start");
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;
            if (qrCodePrefab == null)
            {
                throw new System.Exception("Prefab not assigned");
            }
			
			//StartCoroutine(DelayStartAdd(5f));
        }
		
		/*IEnumerator DelayStartAdd(float timeToDelay)
		{
			yield return new WaitForSeconds(timeToDelay);
			SetupComplete = true;
		}*/

		public GameObject GetQRCodeGameObjectForID(System.Guid id)
		{
			return qrCodesObjectsList[id];
		}
		
        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status)
            {
                clearExisting = true;
            }
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeAdded");

			lock (pendingActions)
			{
				pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
			}
		
        }

        private void Instance_QRCodeUpdated(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeUpdated");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Updated, e.Data));
            }
        }

        private void Instance_QRCodeRemoved(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeRemoved");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Removed, e.Data));
            }
        }

        private void HandleEvents()
        {
            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();
                    if (action.type == ActionData.Type.Added)
                    {
						if(action.qrCode.Data == _qrCodeWeWant)
						{
							Debug.Log("QR Code Added!");
							
							GameObject qrCodeObject = qrCodePrefab;//Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity);
							//GameObject model = Instantiate(_objectToChild, new Vector3(0,0,0), Quaternion.identity);
							
							//if(model != null)
							//{
							//	model.transform.SetParent(qrCodeObject.transform, false);
							//}
							
							Microsoft.MixedReality.QR.QRCode qr = qrCodeObject.transform.GetChild(0).GetComponent<QRCode>().qrCode;
							if(qr != null)
							{
								qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
								qrCodeObject.transform.GetChild(0).GetComponent<QRCode>().qrCode = action.qrCode;
								qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
							}
							
							qrCodeObject.transform.GetChild(1).gameObject.SetActive(true);
						}
					
						
						/*if(_manager != null)
						{
							System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeDetected.txt"), "Detected QRCode at " + qrCodeObject.transform.position.ToString("F4"));
							_manager.transform.SetParent(qrCodeObject.transform.parent);
							System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeDetected2.txt"), "Our new tranform: " + _manager.transform.position.ToString("F4"));
						}*/
                    }
                    else if (action.type == ActionData.Type.Updated)
                    {
						if(action.qrCode.Data == _qrCodeWeWant)
						{
							if (!qrCodesObjectsList.ContainsKey(action.qrCode.Id))
							{
								GameObject qrCodeObject = qrCodePrefab;//Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity);
								qrCodeObject.GetComponent<SpatialGraphCoordinateSystem>().Id = action.qrCode.SpatialGraphNodeId;
								qrCodeObject.transform.GetChild(0).GetComponent<QRCode>().qrCode = action.qrCode;
								qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
							}
						}
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            Destroy(qrCodesObjectsList[action.qrCode.Id]);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
                        }
                    }
                }
            }
			
            if (clearExisting)
            {
                clearExisting = false;
                foreach (var obj in qrCodesObjectsList)
                {
                    Destroy(obj.Value);
                }
                qrCodesObjectsList.Clear();

            }
        }

        // Update is called once per frame
        void Update()
        {
            HandleEvents();
        }
    }

}