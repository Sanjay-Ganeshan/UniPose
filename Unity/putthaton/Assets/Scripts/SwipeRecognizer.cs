using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


using JulAI.UniPose;

public enum ESwipeDirection
{
    UP,
    DOWN,
    LEFT,
    RIGHT
}

namespace JulAI.PutThatOn
{
    public class SwipeRecognizer : MonoBehaviour
    {
        public static SwipeRecognizer Instance;


        public class OnSwipeEventArgs
        {
            public readonly ESwipeDirection Direction;
            public OnSwipeEventArgs(ESwipeDirection direction)
            {
                this.Direction = direction;
            }
        }

        public delegate void OnSwipeEventHandler(object sender, OnSwipeEventArgs e);

        public event OnSwipeEventHandler OnSwipe;

        // The third is time!
        private LinkedList<Vector3> PrevLeftHandPositions;
        private LinkedList<Vector3> PrevRightHandPositions;

        [Header("Swipe Parameters")]
        [Range(0.0f, 1.0f)]
        [Tooltip("Confidence threshold to record a hand position")]
        public float ConfidenceThreshold;
        [Range(0.1f, FastPoseFrame.EXPECTED_WIDTH * FastPoseFrame.Pix2World)]
        [Tooltip("How far a horizontal swipe must traverse IN WORLD UNITS to be counted")]
        public float WorldUnitsForHorizontalSwipe = 2.00f;
        [Range(0.1f, FastPoseFrame.EXPECTED_HEIGHT * FastPoseFrame.Pix2World)]
        [Tooltip("How far a vertical swipe must traverse IN WORLD UNITS to be counted")]
        public float WorldUnitsForVerticalSwipe = 2.00f;
        [Range(0.1f, 1.0f)]
        [Tooltip("The amount of real-time allowed to elapse in a swipe")]
        // This might not be a great idea, since getting pose data is slow (~= 25 fps)
        public float RealTimePerSwipe = 0.5f;

        [Range(0.1f, 5.0f)]
        [Tooltip("The amount of seconds that must pass in between swipes being reported")]
        public float SwipeCooldown = 2.0f;

        private float CurrentSwipeCooldown = 0.0f;

        private void Awake()
        {
            Instance = this;
            PrevLeftHandPositions = new LinkedList<Vector3>();
            PrevRightHandPositions = new LinkedList<Vector3>();
        }

        // Start is called before the first frame update
        void Start()
        {
            PoseSyncer.Instance.OnSync += OnPoseSync;
            OnSwipe += SwipeRecognizer_OnSwipe;
        }

        private void SwipeRecognizer_OnSwipe(object sender, OnSwipeEventArgs e)
        {
            //Debug.Log("Recognized Swipe in: " + e.Direction);
        }

        private void OnPoseSync(object sender, PoseFrameMMapSyncer.OnSyncEventArgs e)
        {
            Vector2 temp;
            // Track the left wrist
            if(CurrentSwipeCooldown >= SwipeCooldown)
            {
                // don't track points on cooldown
                if (e.SyncData.GetConfidence(EJointName.LEFT_WRIST) > ConfidenceThreshold)
                {
                    temp = e.SyncData.GetWorldPosition(EJointName.LEFT_WRIST);
                    PrevLeftHandPositions.AddLast(new Vector3(temp.x, temp.y, Time.time));
                }
                if (e.SyncData.GetConfidence(EJointName.RIGHT_WRIST) > ConfidenceThreshold)
                {
                    temp = e.SyncData.GetWorldPosition(EJointName.RIGHT_WRIST);
                    PrevRightHandPositions.AddLast(new Vector3(temp.x, temp.y, Time.time));
                }
            }

            while(PrevLeftHandPositions.Count > 0 && Mathf.Abs(Time.time - PrevLeftHandPositions.First.Value.z) > RealTimePerSwipe )
            {
                // We do an abs in case there's overflow. Time otherwise doesn't flow backwards so 
                // this is fine
                PrevLeftHandPositions.RemoveFirst();
            }

            while (PrevRightHandPositions.Count > 0 && Mathf.Abs(Time.time - PrevRightHandPositions.First.Value.z) > RealTimePerSwipe)
            {
                // We do an abs in case there's overflow. Time otherwise doesn't flow backwards so 
                // this is fine
                PrevRightHandPositions.RemoveFirst();
            }

            TryRecognize();

        }

        void TryRecognize()
        {
            List<ESwipeDirection> detectedSwipes = new List<ESwipeDirection>();
            // For now, super basic!
            for(int i = 0; i < 2; i++)
            {
                LinkedList<Vector3> listOfInterest = i == 0 ? PrevLeftHandPositions : PrevRightHandPositions;
                if(listOfInterest.Count >= 3)
                {
                    // Need AT LEAST 3 positions
                    Vector2 start = listOfInterest.First.Value;
                    Vector2 end = listOfInterest.Last.Value;
                    Vector2 diff = end - start;
                    if (diff.x > WorldUnitsForHorizontalSwipe)
                    {
                        // Swipe right!
                        detectedSwipes.Add(ESwipeDirection.RIGHT);
                    }
                    else if (diff.x < -WorldUnitsForHorizontalSwipe)
                    { 
                        detectedSwipes.Add(ESwipeDirection.LEFT);
                    }
                }
            }
            // If multiple swipes detected, only send one of them, for now
            if(detectedSwipes.Count > 0)
            {
                ESwipeDirection selected = (ESwipeDirection)detectedSwipes.Select((esw) => (int)esw).Min();
                SendSwipeEvent(selected);

                // Clear out the current lists so we don't get confused
                PrevLeftHandPositions.Clear();
                PrevRightHandPositions.Clear();
                CurrentSwipeCooldown = 0.0f;
            }
        }

        void SendSwipeEvent(ESwipeDirection dir)
        {
            OnSwipe?.Invoke(this, new OnSwipeEventArgs(dir));
        }

        // Update is called once per frame
        void Update()
        {
            if(CurrentSwipeCooldown < SwipeCooldown)
            {
                CurrentSwipeCooldown += Time.deltaTime;
            }
        }
    }
}
