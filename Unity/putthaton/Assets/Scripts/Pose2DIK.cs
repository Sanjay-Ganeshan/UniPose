using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace JulAI.UniPose
{
    public enum ESSDJointName
    {
        NOSE = 0,
        LEFT_EYE,
        RIGHT_EYE,
        LEFT_EAR,
        RIGHT_EAR,
        LEFT_SHOULDER,
        RIGHT_SHOULDER,
        LEFT_ELBOW,
        RIGHT_ELBOW,
        LEFT_WRIST,
        RIGHT_WRIST,
        LEFT_HIP,
        RIGHT_HIP,
        LEFT_KNEE,
        RIGHT_KNEE,
        LEFT_ANKLE,
        RIGHT_ANKLE,
        ROOT,
        COUNT
    }

    public class Pose2DIK : MonoBehaviour
    {
        private ESSDJointName[] jointParents;

        // Now, the skeleton is **fully provided** - we don't need any IK!
        private Vector2[] BindBonePositions;

        private Transform[] jointTransforms;
        
        public Transform RootTransform;

        private Vector2[] currentRawPositions;
        private float[] currentScores;
        public bool ShouldReset;
        const int NumBones = (int) ESSDJointName.COUNT;

        [Header("Smoothing")]
        [Range(0.0f, 1.0f)]
        [Tooltip("Minimum confidence to use new data")]
        public float ConfidenceThreshold = 0.4f;
        [Range(0.1f, 1.0f)]
        [Tooltip("How much to smooth new data with previous data. 1 = use only new, 0 = use only old")]
        public float SmoothingFactor = 0.1f;        

        private void Awake()
        {
            InitJointParents();
            FindHierarchy();
            ReadBindPose();
            InitializeCurrent();
            ShouldReset = false;
        }


        private void Start()
        {
            PoseSyncer.Instance.OnSync += Pose_OnSync;
        }

        private void Pose_OnSync(object sender, PoseFrameMMapSyncer.OnSyncEventArgs e)
        {
            // Save the sync data into a local variable
            UpdateCurrentPose(e.SyncData);
            AlignPose();
        }

        private void AlignPose()
        {
            //ResetPose();
            float scale = CalculateOptimalScale();
            ApplyScale(scale);
            MoveBones();
        }

        private void ResetPose()
        {
            // Go back to bind pose
            SetBones(this.BindBonePositions);
        }

        private Vector2[] RememberPose()
        {
            // Save the current pose in local-space
            Vector2[] localPositions = new Vector2[NumBones];
            for(int i = 0; i < NumBones; i++)
            {
                localPositions[i] = this.jointTransforms[i].localPosition;
            }
            return localPositions;
        }

        private void MoveBones()
        {
            // Now everything has been scaled
            SetBones(this.currentRawPositions);
        }

        private void ApplyScale(float scale)
        {
            this.transform.localScale = Vector3.one * scale;
        }

        private void UpdateCurrentPose(FastPoseFrame syncData)
        {
            Vector2[] prevPositions = new Vector2[NumBones];

            // Save all the previous positions in local space
            for(int i = 0; i < NumBones - 1; i++)
            {
                // parent + "local" position = "global" position
                prevPositions[i] = currentRawPositions[i] - currentRawPositions[(int)jointParents[i]];
            }

            for(int i = 0; i < FastPoseFrame.NUM_KEYPOINTS; i++)
            {
                float score = syncData.GetConfidence(i);
                if(score < ConfidenceThreshold)
                {
                    // We probably can't see this joint. Use the same local position, with the parent's
                    // new global position
                    currentRawPositions[i] = currentRawPositions[(int)jointParents[i]] + prevPositions[i];
                }
                else
                {
                    // We see this joint! Update with data from the frame
                    if(currentScores[i] > ConfidenceThreshold)
                    {
                        // We saw it before, smooth the two together
                        currentRawPositions[i] = Vector2.Lerp(currentRawPositions[i], syncData.GetWorldPosition(i), SmoothingFactor);
                    }
                    else
                    {
                        // We didn't see it before, use only new data
                        currentRawPositions[i] = syncData.GetWorldPosition(i);
                    }
                }
                currentScores[i] = score;
            }
            // Estimate the "root" position too as just the average of the hips/shoulders
            currentRawPositions[(int)ESSDJointName.ROOT] = (
                currentRawPositions[(int)ESSDJointName.RIGHT_SHOULDER] +
                currentRawPositions[(int)ESSDJointName.LEFT_SHOULDER] +
                currentRawPositions[(int)ESSDJointName.RIGHT_HIP] +
                currentRawPositions[(int)ESSDJointName.LEFT_HIP]
                ) / 4.0f;
            currentScores[(int)ESSDJointName.ROOT] = 1.0f;
        }

        private void InitJointParents()
        {
            //  0) NOSE = 0,
            //  1) LEFT_EYE,
            //  2) RIGHT_EYE,
            //  3) LEFT_EAR,
            //  4) RIGHT_EAR,
            //  5) LEFT_SHOULDER,
            //  6) RIGHT_SHOULDER,
            //  7) LEFT_ELBOW,
            //  8) RIGHT_ELBOW,
            //  9) LEFT_WRIST,
            // 10) RIGHT_WRIST,
            // 11) LEFT_HIP,
            // 12) RIGHT_HIP,
            // 13) LEFT_KNEE,
            // 14) RIGHT_KNEE,
            // 15) LEFT_ANKLE,
            // 16) RIGHT_ANKLE
            // The nose is the root ^.^'
            jointParents = new ESSDJointName[]{
                ESSDJointName.ROOT, // nose
                ESSDJointName.NOSE, // nose -> LEye
                ESSDJointName.NOSE, // nose -> REye
                ESSDJointName.LEFT_EYE, // LEye -> LEar
                ESSDJointName.RIGHT_EYE, // REye -> REar
                ESSDJointName.ROOT, // Nose -> LShoulder
                ESSDJointName.ROOT, // Nose -> RShoulder
                ESSDJointName.LEFT_SHOULDER, // LShoulder -> LElbow
                ESSDJointName.RIGHT_SHOULDER, // RShoulder -> RElbow
                ESSDJointName.LEFT_ELBOW,
                ESSDJointName.RIGHT_ELBOW,
                ESSDJointName.ROOT,
                ESSDJointName.ROOT,
                ESSDJointName.LEFT_HIP,
                ESSDJointName.RIGHT_HIP,
                ESSDJointName.LEFT_KNEE,
                ESSDJointName.RIGHT_KNEE,
                ESSDJointName.ROOT // root -> root
                };
        }

        private void FindHierarchy()
        {
            jointTransforms = new Transform[NumBones];

            jointTransforms[(int)ESSDJointName.ROOT] = RootTransform;
            // Find the nose! This is the root
            for (int i = 0; i < jointTransforms.Length; i++)
            {
                // Don't find the root, we already know it
                if (i == (int)ESSDJointName.ROOT) continue;

                // Everything references something previously known, so this will work okay.
                jointTransforms[i] = jointTransforms[(int)jointParents[i]].Find(((EJointName)i).ToString());
            }
        }
        
        private void LookInSameDir(ESSDJointName toModify, ESSDJointName toCopy)
        {
            this.jointTransforms[(int)toModify].right = this.jointTransforms[(int)toCopy].right;
        }

        private void LookTowards(ESSDJointName toModify, ESSDJointName lookAt)
        {
            this.jointTransforms[(int)toModify].right = 
                (this.jointTransforms[(int)lookAt].position - 
                this.jointTransforms[(int)lookAt].position);
        }

        private Vector2 FromTo(ESSDJointName from, ESSDJointName to, Vector2[] virtualPositions)
        {
            return virtualPositions[(int)to] - virtualPositions[(int)from];
        }

        void SetBones(Vector2[] NewPositions)
        {
            // NewPositions should be length NumBones

            // First, let's calculate all the "right" directions

            Vector2[] rights = GetRightVectors(NewPositions);

            this.jointTransforms[(int)ESSDJointName.ROOT].position = NewPositions[(int)ESSDJointName.ROOT];
            this.jointTransforms[(int)ESSDJointName.ROOT].right = rights[(int)ESSDJointName.ROOT];

            for(int i = 0; i < NumBones - 1; i++)
            {
                this.jointTransforms[i].position = NewPositions[i];
                this.jointTransforms[i].right = rights[i];
            }


        }

        private void OnDestroy()
        {
            // Unsubscribe
            PoseSyncer.Instance.OnSync -= Pose_OnSync;
        }

        private Vector2[] GetRightVectors(Vector2[] NewPositions)
        {
            Vector2[] rights = new Vector2[NumBones];
            rights[(int)ESSDJointName.ROOT] = Vector2.up;
            rights[(int)ESSDJointName.NOSE] = rights[(int)ESSDJointName.ROOT];
            rights[(int)ESSDJointName.LEFT_EYE] = rights[(int)ESSDJointName.NOSE];
            rights[(int)ESSDJointName.LEFT_EAR] = rights[(int)ESSDJointName.LEFT_EYE];
            rights[(int)ESSDJointName.RIGHT_EYE] = rights[(int)ESSDJointName.NOSE];
            rights[(int)ESSDJointName.RIGHT_EAR] = rights[(int)ESSDJointName.RIGHT_EYE];

            rights[(int)ESSDJointName.LEFT_SHOULDER] = FromTo(
                                                        ESSDJointName.LEFT_SHOULDER,
                                                        ESSDJointName.LEFT_ELBOW,
                                                        NewPositions);

            rights[(int)ESSDJointName.LEFT_ELBOW] = FromTo(
                                                        ESSDJointName.LEFT_ELBOW,
                                                        ESSDJointName.LEFT_WRIST,
                                                        NewPositions);

            rights[(int)ESSDJointName.LEFT_WRIST] = rights[(int)ESSDJointName.LEFT_ELBOW];

            rights[(int)ESSDJointName.RIGHT_SHOULDER] = FromTo(
                                                        ESSDJointName.RIGHT_SHOULDER,
                                                        ESSDJointName.RIGHT_ELBOW,
                                                        NewPositions);

            rights[(int)ESSDJointName.RIGHT_ELBOW] = FromTo(
                                                        ESSDJointName.RIGHT_ELBOW,
                                                        ESSDJointName.RIGHT_WRIST,
                                                        NewPositions);

            rights[(int)ESSDJointName.RIGHT_WRIST] = rights[(int)ESSDJointName.RIGHT_ELBOW];

            rights[(int)ESSDJointName.LEFT_HIP] = FromTo(
                                                        ESSDJointName.LEFT_HIP,
                                                        ESSDJointName.LEFT_KNEE,
                                                        NewPositions);

            rights[(int)ESSDJointName.LEFT_KNEE] = FromTo(
                                                        ESSDJointName.LEFT_KNEE,
                                                        ESSDJointName.LEFT_ANKLE,
                                                        NewPositions);

            rights[(int)ESSDJointName.LEFT_ANKLE] = rights[(int)ESSDJointName.LEFT_KNEE];

            rights[(int)ESSDJointName.RIGHT_HIP] = FromTo(
                                                        ESSDJointName.RIGHT_HIP,
                                                        ESSDJointName.RIGHT_KNEE,
                                                        NewPositions);

            rights[(int)ESSDJointName.RIGHT_KNEE] = FromTo(
                                                        ESSDJointName.RIGHT_KNEE,
                                                        ESSDJointName.RIGHT_ANKLE,
                                                        NewPositions);

            rights[(int)ESSDJointName.RIGHT_ANKLE] = rights[(int)ESSDJointName.RIGHT_KNEE];

            return rights;
        }


        
        void ReadBindPose()
        {
            // Called while in Bind pose to initialize everything we know
            BindBonePositions = new Vector2[jointTransforms.Length];
            for(int i = 0; i < BindBonePositions.Length; i++)
            {
                // Assume everything starts off scale 1
                BindBonePositions[i] = this.jointTransforms[i].position;
            }
        }

        void InitializeCurrent()
        {
            currentRawPositions = new Vector2[NumBones];
            currentScores = new float[NumBones];

            // Set the confidence scores low, since we haven't seen anything
            // and the raw positions to the bind pose
            for(int i = 0; i < NumBones; i++)
            {
                currentRawPositions[i] = BindBonePositions[i];
                currentScores[i] = 0.1f;
            }
        }

        /// <summary>
        /// Gets the length of the bone from the parent to the given joint
        /// </summary>
        /// <param name="joint">A joint, with a parent</param>
        /// <returns>The distance from the parent to this joint</returns>
        float GetBindLength(ESSDJointName jointA, ESSDJointName jointB)
        {
            return (BindBonePositions[(int)jointA] - BindBonePositions[(int)jointB]).magnitude;
        }

        float GetCurrentLength(ESSDJointName jointA, ESSDJointName jointB)
        {
            return (currentRawPositions[(int)jointA] - currentRawPositions[(int)jointB]).magnitude;
        }


        float CalculateOptimalScale()
        {
            // We'll calculate these lengths to compare, and then try and 
            // scale accordingly
            ESSDJointName[] lengthPairs = new ESSDJointName[]
            {
                ESSDJointName.LEFT_HIP, ESSDJointName.RIGHT_HIP,
                ESSDJointName.LEFT_SHOULDER, ESSDJointName.RIGHT_SHOULDER,
                ESSDJointName.LEFT_SHOULDER, ESSDJointName.LEFT_HIP,
                ESSDJointName.RIGHT_SHOULDER, ESSDJointName.RIGHT_HIP
            };

            List<float> scales = new List<float>();
            for(int i = 0; i < lengthPairs.Length; i+=2)
            {
                // If we can see both end-points in this pair
                if(currentScores[(int) lengthPairs[i]] > 0.2f && currentScores[(int)lengthPairs[i+1]] > 0.2f)
                {
                    float bindLength = GetBindLength(lengthPairs[i], lengthPairs[i+1]);
                    float currLength = GetCurrentLength(lengthPairs[i], lengthPairs[i+1]);
                    float scaleAmount;
                    if(bindLength < 0.001f)
                    {
                        // bind Length * scale amount = curr length
                        scaleAmount = 1.0f;
                    }
                    else
                    {
                        scaleAmount = currLength / bindLength;
                    }
                    scales.Add(scaleAmount);
                }
            }
            float optimalScale;
            if (scales.Count > 0)
                optimalScale = scales.Aggregate((s1, s2) => s1 + s2) / scales.Count;
            else
            {
                optimalScale = 1.0f;
            }
            return optimalScale;
        }

        // Update is called once per frame
        void Update()
        {
            if(ShouldReset)
            {
                ResetPose();
                ShouldReset = false;
            }
        }
    }
}
