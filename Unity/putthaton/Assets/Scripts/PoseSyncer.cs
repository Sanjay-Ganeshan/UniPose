using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace JulAI.UniPose
{
    public class PoseSyncer : MonoBehaviour
    {

        private PoseFrameMMapSyncer Syncer;
        public string MMapTag = "jpysync";

        public static PoseSyncer Instance;

        public int MaxRetries = 3;
        private int NumRetries;

        // Expose the event, but just forward it to the syncer's event
        public event PoseFrameMMapSyncer.OnSyncEventHandler OnSync
        {
            add
            {
                Syncer.OnSync += value;
            }
            remove
            {
                Syncer.OnSync -= value;
            }
        }

        void Awake()
        {
            Syncer = new PoseFrameMMapSyncer(MMapTag);
            Instance = this;
            NumRetries = MaxRetries;
            // Other code can access the event by forwarding through this!
            // The singleton instance helps other classes find it.
        }

        // Start is called before the first frame update
        void Start()
        {
            Connect();
        }

        void Connect()
        {
            Debug.Log("Connecting to shared memory...");
            bool worked = Syncer.Connect();
            if(worked)
            {
                Debug.Log("Connection successful!");
                NumRetries = MaxRetries;
            }
            else
            {
                Debug.Log("Connection failed. Retrying in 5 seconds...");
                if(NumRetries > 0)
                {
                    Invoke("Connect", 5.0f);
                    NumRetries--;
                }
                else
                {
                    Debug.Log("Max retries exceeded... Staying disconnected");
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(Syncer.IsOpen)
            {
                Syncer.Update();
            }
            else
            {
                if(NumRetries > 0 && !IsInvoking("Connect"))
                {
                    Debug.Log("Attempting reconnection");
                    Invoke("Connect", 5.0f);
                }
            }
        }

        private void Disconnect()
        {
            Syncer.Disconnect();
        }

        private void OnDestroy()
        {
            Disconnect(); 
        }

        private void OnDisable()
        {
            Disconnect();
        }
        private void OnApplicationQuit()
        {
            Disconnect();
        }
    }
}
