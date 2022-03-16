﻿using PaintIn3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Linq;

/// <summary>
/// Orignally derived from 'testSceneOperations.cs'
/// </summary>
public class globalOperators : MonoBehaviour
{
    /// sphereJoint models
    public int m_numSphereModels = 0; // specified when creating sphereJointModels
    public GameObject[] m_sphereJointObjs;
    public sphereJointModel[] m_sphereJointModels;
    public List<GameObject> m_sphereJointObjects;
    public List<List<Vector3>> colonLayerAveragePosition; // Average of position for spheres in each layer

    /// meshes
    public int m_numBindColonMeshes = 0; // #colon meshs bound to sphereJointModels
    public GameObject[] m_colonMeshObjs;
    public colonMesh[] m_colonMeshes;

    /// opeartors related variables
    // Corner-cut (Enterotomy)
    public bool m_bCornerCutStarted = false; // true: scissors touches the colon for the FIRST TIME; false: both required corners are cut
    public bool[] m_bCornerCut = { false, false }; // whether right-corner of left colon or left-corner of right colon is cut
    public (int, int)[] m_cornerSphereIdxRange; // predefined spheres at corners {(left-corner sphereIdx-range), (right-corner range)}
    public int[][] m_cutCornerSphereIndices = new int[2][]; // predefined spheres to be cut {left corner: [x1, x2, x3], right corner [y1, y2, y3]}
    public List<bool[]> m_LRCornerCutIdices = new List<bool[]>(); // {(objIdx0: left-corner, right-corner), (objIdx1: left-corner, right-corner)}
    // LS tool insertion
    public bool m_bLSInsertStarted = false; // true: LS bInsert[0] * bInsert[1] > 0 for the first time; false: LS bLock==true, before button pushing
    public static int[] m_bInsert = { 0, 0 }; // 1 means top part inserted, 2 means bottom part
    public static float[] m_insertDepth = { 0, 0 }; // Unity world distance starting from colon opening position (scale from 0 to 1 for the full colon length)
    // LS stapling: Split & Join (Staple-Anastomosis)
    public bool m_bSAStarted = false; // true: LS locked, about to push the button
    public bool m_bSplit = false;
    public bool m_bJoin = false;
    public int[] m_layers2Split = { -1, -1 }; // same for both models
    public int[] m_sphereIdx2Split = { -1, -1 }; // the first splitting sphere index for each model
    // Holding (Opening scure)
    public bool m_bOpeningSecure = false;
    private int m_numSpherePairsProcesed = 0; // for opening secure
    private int m_numSecureOpenings;
    public int[] m_sphereIdx4EachOpening;// sphere currently being held for each opening
    private int m_numHoldingForceps = 0; // #forceps holding the opening during final closure
    private List<List<int>> m_sphIndices4Secure = new List<List<int>>(); //[[opening0], [opening1], [opening2]]
    // Final closure
    public bool m_bFinalClosureStarted = false; // true: LS bottom part is just transversely placed
    public int m_layer2FinalClose = -1;
    private bool m_bLSFullGraspFinalClosure = false;
    public static bool m_bFinalClosure = false; // if the final-closure step is done

    /// haptics device inputs
    private int m_numSurgTools = 0; // actual surgical tools in the scene
    public string[] m_surgToolNames = { "Forceps", "Forceps1", "Forceps2", "Scissors" };
    public Dictionary<string, HapticSurgTools> m_hapticSurgTools = new Dictionary<string, HapticSurgTools>();

    // Linear Stapler stuff
    public LinearStaplerTool lsController;
    public bool m_bLSButtonPushing = false; // true: button is pushing
    public bool m_bLSButtonFullDown = false; // true: button full down/ false: button partially down
    public bool m_bLSButtonPulling = false; // true: button is pulling
    public bool m_bLSInserting = false; // true: LS is inserting (with motion)
    public bool m_bLSRemoving = false; // true: LS is removing (with motion)
    public bool m_bLSTransversing = false; // if LS is on transverse motion for final-closing
    public bool m_bLSLocked = false; // true: LS is locked
    public float m_LSButtonValue = 0.0f; // 0 ~ 1
    public float m_LSGraspLengthFinalClosure = 0.0f;
    public int[] m_LSStates; // update for each frame

    // Scoring metrics
    public bool m_bEnableMetricsScoring = true;
    public LSMetricsScoring MetricsScoringManager = null;
    public static float m_startTime; // Record the time when user click "Start" button on start menu
    public static bool m_bSimStart = false; // Start when the user clicks "Start"
    public static bool m_bSimEnd = false; // manual end by the user by pressing space

    // Cutting stuff
    public Transform scissorsValidator;
    public float upDirectionAngle;
    public float upDirectionThreshold;
    public float rightDirectionAngle;
    public float rightDirectionThreshold;
    public float forwardDirectionAngle;
    public float forwardDirectionThreshold;
    public float xDistanceThreshold;
    public float yDistanceThreshold;
    public float zDistanceThreshold;
    public List<GameObject> validCuttingSpheres; // Which colon spheres are valid for cutting
    public List<GameObject> leftCornerSpheres; // Spheres which belongs to the left cutting corner of each colon
    public List<GameObject> rightCornerSpheres;
    public GameObject scissorTouchingSphere;
    // Testing
    public float scissorUpDiff;
    public float scissorRightDiff;
    public float scissorForwardDiff;

    // Improved grabbing stuff
    public List<GameObject> colon0Layers;
    public List<List<Transform>> colon0Spheres;
    public List<GameObject> colon1Layers;
    public List<List<Transform>> colon1Spheres;

    // UI control
    public LS_UIcontroller uiController;

    // Colon joining
    public List<List<Transform>> supportedColonSpheresAfterJoining; // Colon spheres that's been connected with extra fixedjoints to support mesh after joining
    public List<List<FixedJoint>> supportingJointsForJoining;

    public static globalOperators instance;

    /// <summary>
    /// Start simulation logic
    /// </summary>
    public static void SimStart()
    {
        m_startTime = Time.time;
        m_bSimStart = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        /// access sphereJointModels and colon meshes
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error, m_numSphereModels not specified!");
            return;
        }

        // ==> always assume models to be processed are sphereJointGameObj0 (outer0), sphereJointGameObj1 (outer1)
        m_sphereJointObjs = new GameObject[m_numSphereModels];
        m_sphereJointModels = new sphereJointModel[m_numSphereModels];
        m_colonMeshObjs = new GameObject[m_numSphereModels];
        m_colonMeshes = new colonMesh[m_numSphereModels];
        m_sphereJointObjects = new List<GameObject>();
        for (int i = 0; i < m_numSphereModels; i++)
        {
            m_sphereJointObjs[i] = GameObject.Find("sphereJointGameObj" + i.ToString());
            m_sphereJointModels[i] = m_sphereJointObjs[i].GetComponent<sphereJointModel>();
            m_sphereJointObjects.AddRange(m_sphereJointObjs[i].GetComponentsInChildren<sphereJointsInfo>().Select(s => s.gameObject));

            if (!m_sphereJointObjs[i] || !m_sphereJointModels[i])
                return;

            // restore the bound colon mesh
            if (m_sphereJointModels[i].m_bFindColonMesh)
            {
                m_colonMeshObjs[i] = GameObject.Find("outer" + i.ToString());
                m_colonMeshes[i] = m_colonMeshObjs[i].GetComponent<colonMesh>();
                m_numBindColonMeshes += 1;
            }
        }

        colon0Spheres = new List<List<Transform>>();
        colon0Layers.ForEach(l => colon0Spheres.Add(l.transform.GetComponentsInChildren<Transform>().ToList()));
        colon0Spheres.ForEach(l => l.RemoveAt(0));
        colon1Spheres = new List<List<Transform>>();
        colon1Layers.ForEach(l => colon1Spheres.Add(l.transform.GetComponentsInChildren<Transform>().ToList()));
        colon1Spheres.ForEach(l => l.RemoveAt(0));
        colonLayerAveragePosition = new List<List<Vector3>>();
        for (int i = 0; i < 2; i++)
        {
            colonLayerAveragePosition.Add(new List<Vector3>());
            for (int l = 0; l < 20; l++)
            {
                colonLayerAveragePosition[i].Add(Vector3.zero);
            }
        }

        /// initialize opeartor variables
        // Corner-cut
        m_cornerSphereIdxRange = new (int, int)[] { (11, 18), (1, 8) };// sphereIdx range, {(left-corner), (right-corner)}
        m_cutCornerSphereIndices[0] = new int[] { 13, 12, 11 }; // left-corner to be cut
        m_cutCornerSphereIndices[1] = new int[] { 6, 7, 8 }; // right-corner to be cut
        for (int i = 0; i < m_numSphereModels; i++)
        {
            m_LRCornerCutIdices.Add(new bool[] { false, false }); //(left, right)
        }

        // split & joint
        m_layers2Split[0] = 0;
        m_layers2Split[1] = 10;//14
        m_sphereIdx2Split[0] = 3;//4 //5
        m_sphereIdx2Split[1] = 14;//16 //14
        m_layer2FinalClose = 1;

        // holding (opening secure)
        m_sphereIdx4EachOpening = new int[] { -1, -1, -1 };
        m_sphIndices4Secure.Add(new List<int> { 3, 4, 6, 13, 14, 15, 16 });
        m_sphIndices4Secure.Add(new List<int> { 1, 2, 7, 8 });
        m_sphIndices4Secure.Add(new List<int> { 11, 12, 17, 18 });
        m_numSecureOpenings = m_sphereIdx4EachOpening.Length;

        /// initialize haptic device variables
        int numSurgTool = m_surgToolNames.Length; // {forceps, forceps1, forceps2, scissors}
        for (int j = 0; j < 4; j++)
        {
            GameObject hapticSurgToolObj = GameObject.Find(m_surgToolNames[j]);
            if (hapticSurgToolObj)
            {
                m_hapticSurgTools.Add(m_surgToolNames[j], hapticSurgToolObj.GetComponent<HapticSurgTools>());
                m_numSurgTools += 1;
            }
        }
        if (m_numSurgTools != numSurgTool)
        {
            Debug.Log("Error (globalOperators): Cannot find all 4 surgical tools!");
        }

        // Find linear stapler controller
        lsController = FindObjectOfType<LinearStaplerTool>();
        m_LSStates = new int[] { 0, 0, 0, 0, 0, 0 }; // {inserting, removing, buttongPushing, buttonPulling, lock, transversing}

        // Metrics scoring manager
        MetricsScoringManager = FindObjectOfType<globalOperators>().GetComponent<LSMetricsScoring>();

        // List initialization
        supportedColonSpheresAfterJoining = new List<List<Transform>>();
        supportingJointsForJoining = new List<List<FixedJoint>>();
    }

    /// <summary>
    /// Split all colon models in the scene at once 
    ///     Note: need to identify which ones to split when more than 2 models
    /// Input: 
    ///     int[] m_layers2Split: assume the splitting layers for both models are the same
    ///     int[] m_sphereIdx2Split: first, second sphereIdx to split
    /// </summary>
    bool split()
    {
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error(split): no models to split!");
            return false;
        }
        if (m_numBindColonMeshes != m_numSphereModels)
        {
            Debug.Log("Error(split): unmatched colon mesh and sphereJoint models!");
            return false;
        }

        // split the mesh and sphereJointModel for each colon
        for (int i = 0; i < m_numSphereModels; i++)
        {
            // Processing colonMesh and bind it to sphereJointModel
            if (!Application.isPlaying) // Edit mode
            {
                // examine and handle duplicated vertices
                if (!m_colonMeshes[i].bRemoveDuplicate && m_colonMeshes[i].verDupMap.Count <= 0)
                {
                    if (!m_colonMeshes[i].checkDuplicateVertices())
                    {
                        m_colonMeshes[i].removeDuplicateVertices();
                        m_colonMeshes[i].bRemoveDuplicate = true;
                    }
                    Debug.Log("Duplicated already handled");
                }
                // check inner/outer layer info
                if (m_colonMeshes[i].triIOLayerInfo.Count <= 0)
                {
                    m_colonMeshes[i].evaluateTriIOLayerInfo();
                    Debug.Log("Done build inner/outer layer info");
                }
                // bind colon mesh to sphereJointModel
                if (!m_colonMeshes[i].isBound && m_colonMeshes[i].neighborSpheres.Count == 0)
                {
                    if (m_sphereJointModels[i])
                    {
                        if (m_colonMeshes[i].neighborSphereSearching1(m_sphereJointModels[i]))
                        {
                            m_colonMeshes[i].bindSphereJointModel();
                            m_colonMeshes[i].isBound = true;
                        }
                    }
                    Debug.Log("Binding sphereJointModel done!");
                }
            }

            // colonMesh
            if (m_colonMeshes[i].split1(m_layers2Split, m_sphereIdx2Split[i], m_sphereJointModels[i]))
                Debug.Log("Split done for outer " + i.ToString());
            else
            {
                Debug.Log("Split failed for outer " + i.ToString());
                return false;
            }

            // sphereJointModel
            m_sphereJointModels[i].splitInlayerJoints_sigle(ref m_layers2Split, m_sphereIdx2Split[i]);
        }

        m_bSplit = true;
        return true;
    }

    /// <summary>
    /// Description: Join two sphereJoint models that have been split 
    ///     Note: need to identify which ones to join when more than 2 models
    /// Method:
    ///     1. Move the spheres to be joined between two models to the mid point and contact with each other
    ///     2. Add only one joint between the spheres of the same layer of across the two models
    ///         m_sphereIdx2Split[0] -> m_sphereIdx2Split[1]+1
    ///         m_sphereIdx2Split[1] -> m_sphereIdx2Split[0]+1
    /// Input: 
    ///     int[] m_layers2Split: assume the layers for both models to be joined are the same
    ///     int[] m_sphereIdx2Split: the first sphereIdx of the first and second colons to join
    /// </summary>
    bool joinSphereJointModels(int numLayer2Join, int[] objIdx, int[] sphereIdxB, float[] sphereRadius)
    {
        ColonStaplerJointManager.instance.DeactivateAllAnchors();
        // Adding two new joints between two models
        //      m_sphereIdx2Split[0] -> sphereIdxB[1] &
        //      m_sphereIdx2Split[1] -> sphereIdxB[0]
        int numJointsAdded = 0;

        float dist0A1B, dist1A0B; // initial distance between each pair of spheres to join
        Vector3 vec0A1B, vec1A0B; // unit vector between each pair of spheres to join

        for (int l = 0; l < numLayer2Join; l++)
        {
            // Add new list for supported spheres and new fixed joints
            supportedColonSpheresAfterJoining.Add(new List<Transform>());
            supportingJointsForJoining.Add(new List<FixedJoint>());

            // connect sphereA of object0 to sphereB of object1
            GameObject sphere0A = m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[0].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + m_sphereIdx2Split[0].ToString());
            GameObject sphere1B = m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[1].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + sphereIdxB[1].ToString());
            if (sphere0A && sphere1B)
            {
                // move the spheres to the middle
                vec0A1B = sphere1B.transform.position - sphere0A.transform.position; // 0A->1B
                vec0A1B.Normalize();
                dist0A1B = Vector3.Distance(sphere0A.transform.position, sphere1B.transform.position);
                sphere0A.transform.position = sphere0A.transform.position + (0.5f * dist0A1B - sphereRadius[0]) * vec0A1B;
                sphere1B.transform.position = sphere1B.transform.position - (0.5f * dist0A1B - sphereRadius[1]) * vec0A1B;
                // move the spheres up a bit
                //sphere0A.transform.position = sphere0A.transform.position + Vector3.up;
                //sphere1B.transform.position = sphere1B.transform.position + Vector3.up;
                // add a fixed joint
                FixedJoint joint0A1B = sphere0A.AddComponent<FixedJoint>();
                joint0A1B.connectedBody = sphere1B.GetComponent<Rigidbody>();
                // Save modified spheres and joints
                supportingJointsForJoining[l].Add(joint0A1B);
                // reconfigure the joint
                //joint0A1B.linearLimitSpring = new SoftJointLimitSpring() { spring = 50.0f, damper = 50.0f };
                //joint0A1B.linearLimit = new SoftJointLimit() { limit = 0.01f };
                //joint0A1B.autoConfigureConnectedAnchor = false;
                //joint0A1B.connectedAnchor = new Vector3(0, 0, 0);
                // update jointInfo of sphere0A
                sphereJointsInfo joint0A1BInfo = sphere0A.GetComponent<sphereJointsInfo>();
                joint0A1BInfo.m_inLayerJointList[joint0A1BInfo.m_inLayerJointNum, 0] = joint0A1BInfo.m_inLayerJointNum;
                joint0A1BInfo.m_inLayerJointList[joint0A1BInfo.m_inLayerJointNum, 1] = objIdx[1];
                joint0A1BInfo.m_inLayerJointList[joint0A1BInfo.m_inLayerJointNum, 2] = m_layers2Split[0] + l;
                joint0A1BInfo.m_inLayerJointList[joint0A1BInfo.m_inLayerJointNum, 3] = sphereIdxB[1];
                joint0A1BInfo.m_inLayerJointNum += 1;
                numJointsAdded += 1;
            }

            // connect sphereA of object1 to sphereB of object0
            GameObject sphere1A = m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[1].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + m_sphereIdx2Split[1].ToString());
            GameObject sphere0B = m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[0].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + sphereIdxB[0].ToString());
            if (sphere1A && sphere0B)
            {
                // move the spheres to the middle
                //vec1A0B = sphere0B.transform.position - sphere1A.transform.position; // 1A->0B
                vec1A0B = sphere1A.transform.position - sphere0B.transform.position; // 1A->0B
                vec1A0B.Normalize();
                dist1A0B = Vector3.Distance(sphere1A.transform.position, sphere0B.transform.position);
                //sphere1A.transform.position = sphere1A.transform.position + (0.5f * dist1A0B - sphereRadius[1]) * vec1A0B;
                //sphere0B.transform.position = sphere0B.transform.position - (0.5f * dist1A0B - sphereRadius[0]) * vec1A0B;
                // add a fixed joint
                FixedJoint joint1A0B = sphere1A.AddComponent<FixedJoint>();
                joint1A0B.connectedBody = sphere0B.GetComponent<Rigidbody>();

                // Save modified spheres and joints
                supportedColonSpheresAfterJoining[l].Add(sphere0A.transform);
                supportedColonSpheresAfterJoining[l].Add(sphere0B.transform);
                supportedColonSpheresAfterJoining[l].Add(sphere1A.transform);
                supportedColonSpheresAfterJoining[l].Add(sphere1B.transform);
                // Save modified spheres and joints
                supportingJointsForJoining[l].Add(joint1A0B);

                // move the upper spheres up a bit
                sphere1A.transform.position = new Vector3(sphere1A.transform.position.x, BetterRandom.betterRandom(18500, 19500) / 10000f, sphere1A.transform.position.z);
                sphere0B.transform.position = new Vector3(sphere0B.transform.position.x, BetterRandom.betterRandom(18500, 19500) / 10000f, sphere0B.transform.position.z);
                // add supporting vertical fixed joint
                FixedJoint joint0A0B = sphere0A.AddComponent<FixedJoint>();
                joint0A0B.connectedBody = sphere0B.GetComponent<Rigidbody>();
                FixedJoint joint1A1B = sphere1A.AddComponent<FixedJoint>();
                joint1A1B.connectedBody = sphere1B.GetComponent<Rigidbody>();

                // Save modified spheres and joints
                supportingJointsForJoining[l].Add(joint0A0B);
                supportingJointsForJoining[l].Add(joint1A1B);

                // Move more spheres up and add vertical supportive joints
                GameObject sphere0C = m_sphereJointObjects.Find(g => g.name == "sphere_" + 0.ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + 5.ToString());
                sphere0C.transform.position = new Vector3(sphere0C.transform.position.x, BetterRandom.betterRandom(18500, 19500) / 10000f, sphere0C.transform.position.z);
                FixedJoint joint0C0B = sphere0C.AddComponent<FixedJoint>();
                joint0C0B.connectedBody = sphere0B.GetComponent<Rigidbody>();
                FixedJoint joint0C1B = sphere0C.AddComponent<FixedJoint>();
                joint0C1B.connectedBody = sphere1B.GetComponent<Rigidbody>();
                GameObject sphere1C = m_sphereJointObjects.Find(g => g.name == "sphere_" + 1.ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + 13.ToString());
                sphere1C.transform.position = new Vector3(sphere1C.transform.position.x, BetterRandom.betterRandom(18500, 19500) / 10000f, sphere1C.transform.position.z);
                FixedJoint joint1C0B = sphere1C.AddComponent<FixedJoint>();
                joint1C0B.connectedBody = sphere0B.GetComponent<Rigidbody>();
                FixedJoint joint1C1B = sphere1C.AddComponent<FixedJoint>();
                joint1C1B.connectedBody = sphere1B.GetComponent<Rigidbody>();
                GameObject sphere1D = m_sphereJointObjects.Find(g => g.name == "sphere_" + 1.ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + 12.ToString());
                sphere1D.transform.position = new Vector3(sphere1D.transform.position.x, BetterRandom.betterRandom(18500, 19500) / 10000f, sphere1D.transform.position.z);
                FixedJoint joint1D0B = sphere1D.AddComponent<FixedJoint>();
                joint1D0B.connectedBody = sphere0B.GetComponent<Rigidbody>();
                FixedJoint joint1D1B = sphere1D.AddComponent<FixedJoint>();
                joint1D1B.connectedBody = sphere1B.GetComponent<Rigidbody>();

                // Save modified spheres and joints
                supportedColonSpheresAfterJoining[l].Add(sphere0C.transform);
                supportedColonSpheresAfterJoining[l].Add(sphere1C.transform);
                supportedColonSpheresAfterJoining[l].Add(sphere1D.transform);
                supportingJointsForJoining[l].Add(joint0C0B);
                supportingJointsForJoining[l].Add(joint0C1B);
                supportingJointsForJoining[l].Add(joint1C0B);
                supportingJointsForJoining[l].Add(joint1C1B);
                supportingJointsForJoining[l].Add(joint1D0B);
                supportingJointsForJoining[l].Add(joint1D1B);


                // reconfigure neighbor joints
                List<ConfigurableJoint> joints0 = sphere0B.GetComponents<ConfigurableJoint>().ToList();
                foreach (ConfigurableJoint j in joints0)
                {
                    Rigidbody sphere = j.connectedBody;
                    if (GetSphereLayer(sphere.name) != GetSphereLayer(sphere0B.name))
                    {
                        continue;
                    }
                    Destroy(j);
                    FixedJoint newJ = sphere0B.AddComponent<FixedJoint>();
                    newJ.connectedBody = sphere;

                    // Save modified spheres and joints
                    supportingJointsForJoining[GetSphereLayer(sphere.name)].Add(newJ);
                }
                for (int i = 0; i < 20; i++)
                {
                    List<ConfigurableJoint> joints1 =
                        m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[1].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + i.ToString()).
                        GetComponents<ConfigurableJoint>().ToList();
                    foreach (ConfigurableJoint j in joints1)
                    {
                        if (j.connectedBody.name == sphere1A.name)
                        {
                            GameObject sphere = j.gameObject;
                            Destroy(j);
                            ConfigurableJoint newJ = sphere.AddComponent<ConfigurableJoint>();
                            newJ.connectedBody = sphere1A.GetComponent<Rigidbody>();
                        }
                        if (j.connectedBody.name == sphere1C.name)
                        {
                            GameObject sphere = j.gameObject;
                            Destroy(j);
                            ConfigurableJoint newJ = sphere.AddComponent<ConfigurableJoint>();
                            newJ.connectedBody = sphere1C.GetComponent<Rigidbody>();
                        }
                        //if (j.connectedBody.name == sphere1D.name)
                        //{
                        //    GameObject sphere = j.gameObject;
                        //    Destroy(j);
                        //    FixedJoint newJ = sphere.AddComponent<FixedJoint>();
                        //    newJ.connectedBody = sphere1D.GetComponent<Rigidbody>();
                        //}
                    }
                    //List<ConfigurableJoint> joints2 =
                    //    m_sphereJointObjects.Find(g => g.name == "sphere_" + objIdx[1].ToString() + "_" + (m_layers2Split[0] + l).ToString() + "_" + i.ToString()).
                    //    GetComponents<ConfigurableJoint>().ToList();
                    //foreach (ConfigurableJoint j in joints2)
                    //{
                    //    if (j.connectedBody.name == sphere1C.name)
                    //    {
                    //        GameObject sphere = j.gameObject;
                    //        Destroy(j);
                    //        ConfigurableJoint newJ = sphere.AddComponent<ConfigurableJoint>();
                    //        newJ.connectedBody = sphere1C.GetComponent<Rigidbody>();
                    //    }
                    //}
                }

                // Modify every supporting fixed joints
                supportingJointsForJoining.ForEach(jl => jl.ForEach(j => j.breakForce = 10));

                // reconfigure the joint
                //joint1A0B.linearLimitSpring = new SoftJointLimitSpring() { spring = 50.0f, damper = 50.0f };
                //joint1A0B.linearLimit = new SoftJointLimit() { limit = 0.01f };
                //joint1A0B.autoConfigureConnectedAnchor = false;
                //joint1A0B.connectedAnchor = new Vector3(0, 0, 0);
                // update jointInfo of sphere0A
                sphereJointsInfo joint1A0BInfo = sphere1A.GetComponent<sphereJointsInfo>();
                joint1A0BInfo.m_inLayerJointList[joint1A0BInfo.m_inLayerJointNum, 0] = joint1A0BInfo.m_inLayerJointNum;
                joint1A0BInfo.m_inLayerJointList[joint1A0BInfo.m_inLayerJointNum, 1] = objIdx[0];
                joint1A0BInfo.m_inLayerJointList[joint1A0BInfo.m_inLayerJointNum, 2] = m_layers2Split[0] + l;
                joint1A0BInfo.m_inLayerJointList[joint1A0BInfo.m_inLayerJointNum, 3] = sphereIdxB[0];
                joint1A0BInfo.m_inLayerJointNum += 1;
                numJointsAdded += 1;
            }
        }

        if (numJointsAdded != 2 * numLayer2Join)
        {
            Debug.Log("Error(joinSphereJointModels): new joints of some layers were not added successfully!");
            Debug.Log("---- Should delete & re-create the sphereJointModels and re-do the operation again");
            return false;
        }

        Debug.Log("joinSphereJointModels: All layers of two sphereJointModels are joined!");
        return true;
    }

    /// <summary>
    /// Method:
    ///     1. join two sphereJointModels
    ///     2. update mesh vertices' neighboring spheres for joining with new sphereJointModels
    /// </summary>
    /// <returns></returns>
    bool join()
    {
        if (m_numSphereModels < 2)
        {
            Debug.Log("Error(join): < 2 colons to join!");
            return false;
        }

        /// join sphereJointModels
        // prepare for splitting layer and sphereIdx attributes
        int numLayer2Join = m_layers2Split[1] - m_layers2Split[0] + 1;
        int[] objIdx = new int[m_numSphereModels];
        int[] numSpheres = new int[m_numSphereModels];
        int[] sphereIdxB = new int[m_numSphereModels];
        float[] sphereRadius = new float[m_numSphereModels];
        for (int i = 0; i < m_numSphereModels; i++)
        {
            objIdx[i] = m_sphereJointModels[i].m_objIndex;
            numSpheres[i] = m_sphereJointModels[i].m_numSpheres;
            sphereIdxB[i] = m_sphereIdx2Split[i] + 1;
            if (sphereIdxB[i] >= numSpheres[i])
                sphereIdxB[i] = sphereIdxB[i] - numSpheres[i];
            sphereRadius[i] = m_sphereJointModels[i].m_sphereRadius;
        }

        // join
        if (!joinSphereJointModels(numLayer2Join, objIdx, sphereIdxB, sphereRadius))
            return false;

        // udpate the vertices' neighboring spheres for colon meshes
        bool[] bSucceed = new bool[m_numSphereModels];
        bSucceed[0] = m_colonMeshes[0].updateNeighboringSpheres4Join(objIdx[1], m_layers2Split, m_sphereIdx2Split[1], sphereIdxB[1]);
        bSucceed[1] = m_colonMeshes[1].updateNeighboringSpheres4Join(objIdx[0], m_layers2Split, m_sphereIdx2Split[0], sphereIdxB[0]);

        if (!bSucceed[0] || !bSucceed[1])
        {
            Debug.Log("Join failed");
            return false;
        }

        // update colon meshes' sphereJointModel2Join
        m_colonMeshes[0].physicsModel2Join = m_sphereJointModels[1];
        m_colonMeshes[1].physicsModel2Join = m_sphereJointModels[0];
        m_bJoin = true;

        ColonMovementController.instance.ChangeFollowStates(0, 0, false, true);
        ColonMovementController.instance.ChangeFollowStates(1, 0, false, true);

        Debug.Log("Join succeed!");
        return true;
    }

    bool cornerCut()
    {
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error(cornerCut): no models to conduct corner-cut!");
            return false;
        }

        if (m_bSplit || m_bJoin)
        {
            Debug.Log("Error(cornerCut): colons are already split or joined!");
            return false;
        }

        // cut both models at the same time for now
        int layerIdx = 0; // layer to cut-corner
        int[] cutCornerSphereIndices;
        for (int objIdx = 0; objIdx < m_numSphereModels; objIdx++)
        {
            if (objIdx == 0)
            {
                cutCornerSphereIndices = new int[] { 6, 7, 8 };
                if (!m_sphereJointModels[objIdx].cornerCut(layerIdx, cutCornerSphereIndices))
                    return false;
                m_bCornerCut[0] = true;
                Debug.Log("cornerCut done for sphereJointModel" + objIdx.ToString());
            }
            else if (objIdx == 1)
            {
                cutCornerSphereIndices = new int[] { 13, 12, 11 };
                if (!m_sphereJointModels[objIdx].cornerCut(layerIdx, cutCornerSphereIndices))
                    return false;
                m_bCornerCut[1] = true;
                Debug.Log("cornerCut done for sphereJointModel" + objIdx.ToString());
            }
        }

        // ### Easier testing
        m_LRCornerCutIdices[0][1] = true;
        m_LRCornerCutIdices[1][0] = true;

        return true;
    }

    bool cornerCut(int objIdx)
    {
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error(cornerCut): no models to conduct corner-cut!");
            return false;
        }

        if (m_bSplit || m_bJoin)
        {
            Debug.Log("Error(cornerCut): colons are already split or joined!");
            return false;
        }

        // cut the specified model
        int layerIdx = 0; // layer to cut-corner
        int[] cutCornerSphereIndices;
        if (objIdx == 0)
        {
            cutCornerSphereIndices = new int[] { 6, 7, 8 };
            if (!m_sphereJointModels[objIdx].cornerCut(layerIdx, cutCornerSphereIndices))
                return false;
            m_bCornerCut[0] = true;
            Debug.Log("cornerCut done for sphereJointModel" + objIdx.ToString());
        }
        else if (objIdx == 1)
        {
            cutCornerSphereIndices = new int[] { 13, 12, 11 };
            if (!m_sphereJointModels[objIdx].cornerCut(layerIdx, cutCornerSphereIndices))
                return false;
            m_bCornerCut[1] = true;
            Debug.Log("cornerCut done for sphereJointModel" + objIdx.ToString());
        }

        return true;
    }

    bool cornerCut(int objIdx, int sphereIdx, ref int LorR)
    {
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error(cornerCut): no models to conduct corner-cut!");
            return false;
        }

        if (m_bSplit || m_bJoin)
        {
            Debug.Log("Error(cornerCut): colons are already split or joined!");
            return false;
        }

        //m_hapticSurgTools["Scissors"].cutSphereIdx.ToList()
        if (!VerifyScissorCutTransformCondition())
        {
            return false;
        }

        // check which corner of sphereJointModel objIdx to cut
        string[] debuguse = { "left", "right" };
        LorR = -1; // 0: left; 1: right
        for (int c = 0; c < m_cornerSphereIdxRange.Length; c++)
        {
            if (sphereIdx >= m_cornerSphereIdxRange[c].Item1 && sphereIdx <= m_cornerSphereIdxRange[c].Item2)
            {
                LorR = c;
                break;
            }
        }
        if (LorR < 0 || LorR >= 2)
        {
            Debug.Log("Error (cornerCut): attemped cut is not in the corner range, exit!");
            return false;
        }

        // determine if the corner has already cut
        if (m_LRCornerCutIdices[objIdx][LorR] == true) // already cut
        {
            //Debug.Log("Error(cornerCut) on sphereJointModel" + objIdx.ToString() + "on the " + debuguse[LorR] + " has already done!!");
            return false;
        }

        // cut the objIdx at a specific layer and corner spheres
        int layerIdx = 0; // layer to cut-corner
        if (!m_sphereJointModels[objIdx].cornerCut(layerIdx, m_cutCornerSphereIndices[LorR]))
            return false;

        m_LRCornerCutIdices[objIdx][LorR] = true;
        Debug.Log("cornerCut done for sphereJointModel" + objIdx.ToString() + " on the " + debuguse[LorR]);

        // whether cut left model (objIdx=0) on the right while right model (objIdx=1) on the left
        if ((objIdx == 0 && LorR == 1) || (objIdx == 1 && LorR == 0))
        {
            m_bCornerCut[objIdx] = true;
        }

        return true;
    }

    public bool VerifyScissorCutTransformCondition(List<Transform> cutSphereGroup = null)
    {
        if (cutSphereGroup != null && cutSphereGroup.Count > 0)
        {
            Vector3 groupPosition = cutSphereGroup.Select(s => s.position).Aggregate(Vector3.zero, (acc, v) => acc + v) / (float)cutSphereGroup.Count;
        }

        if (!CheckAngleDifference(Vector3.Angle(scissorsValidator.right, Vector3.up), upDirectionAngle, upDirectionThreshold, true))
        {
            return false;
        }
        if (!CheckAngleDifference(Mathf.Atan(Mathf.Abs(scissorsValidator.forward.x / scissorsValidator.forward.z)) * Mathf.Rad2Deg, forwardDirectionAngle, forwardDirectionThreshold, true))
        {
            return false;
        }
        if (!CheckAngleDifference(Mathf.Atan(Mathf.Abs(scissorsValidator.up.y / Mathf.Sqrt(Mathf.Pow(scissorsValidator.up.z, 2) + Mathf.Pow(scissorsValidator.up.x, 2)))) * Mathf.Rad2Deg,
            rightDirectionAngle, rightDirectionThreshold, true))
        {
            return false;
        }
        if (leftCornerSpheres.Contains(scissorTouchingSphere)) // If trying to cut left colon corner
        {
            if (scissorsValidator.forward.x / scissorsValidator.forward.z > 0)
            {
                return false;
            }
        }
        else
        {
            if (scissorsValidator.forward.x / scissorsValidator.forward.z < 0)
            {
                return false;
            }
        }

        return true;
    }

    public void TestScissorCutConditions()
    {
        scissorUpDiff = Vector3.Angle(scissorsValidator.right, Vector3.up);
        scissorRightDiff = Mathf.Abs(Mathf.Atan(scissorsValidator.up.y / Mathf.Sqrt(Mathf.Pow(scissorsValidator.up.z, 2) + Mathf.Pow(scissorsValidator.up.x, 2))) * Mathf.Rad2Deg);
        scissorForwardDiff = Mathf.Abs(Mathf.Atan(scissorsValidator.forward.x / scissorsValidator.forward.z) * Mathf.Rad2Deg);
    }

    public bool CheckAngleDifference(float actualAngle, float targetAngle, float validRange, bool canInverse)
    {
        if (Mathf.Abs(actualAngle - targetAngle) <= validRange)
        {
            return true;
        }
        if (canInverse)
        {
            if (targetAngle >= 90)
            {
                if (Mathf.Abs(actualAngle - (180 - targetAngle)) <= validRange)
                {
                    return true;
                }

            }
            else
            {
                if (Mathf.Abs((180 - actualAngle) - targetAngle) <= validRange)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Steps:
    ///     1.Close opening
    ///     2.Lift up first few layers
    ///     3. Attach forceps accordingly
    /// 
    bool openingSecure(bool bSecureOpeningsAtOnce)
    {
        if (m_numSphereModels < 2)
        {
            Debug.Log("Error(openingSecure): no models to hold!");
            return false;
        }
        for (int objIdx = 0; objIdx < m_numSphereModels; objIdx++)
        {
            if (!m_sphereJointModels[objIdx])
            {
                Debug.Log("Error(openingSecure): invalid sphereJointModels!");
                return false;
            }
        }

        /// Close opening: close all openings at once
        int layerIdx = 0;
        if (bSecureOpeningsAtOnce)
        {
            var closingPairs_obj0 = new (int top, int bottom)[] { (8, 1), (7, 2), (6, 3), (4, 3) };
            var closingPairs_obj1 = new (int top, int bottom)[] { (11, 18), (12, 17), (13, 16), (14, 15) };
            int maxSpherePairsToClose = closingPairs_obj0.Length + closingPairs_obj1.Length;
            while (m_numSpherePairsProcesed < maxSpherePairsToClose)
            {
                // close up three pairs of spheres at once               
                for (int i = 0; i < closingPairs_obj0.Length; i++)
                {
                    m_sphereJointModels[0].closeupSpherePair(layerIdx, closingPairs_obj0[i].top, closingPairs_obj0[i].bottom);
                    m_numSpherePairsProcesed++;
                }
                for (int i = 0; i < closingPairs_obj1.Length; i++)
                {
                    m_sphereJointModels[1].closeupSpherePair(layerIdx, closingPairs_obj1[i].top, closingPairs_obj1[i].bottom);
                    m_numSpherePairsProcesed++;
                }

                if (m_numSpherePairsProcesed == maxSpherePairsToClose)
                    Debug.Log("openSecure: close up all " + m_numSpherePairsProcesed.ToString() + " sphere pairs");
            }
        }

        /// lift up the first few layers of spheres
        int numLayersLifted = 3;// 0,1,2
        int numSpheresPerLayer;
        Vector3[] liftVecs = new[] {new Vector3(0.0f, 0.1f, 0.0f),   // layer==0
                                    new Vector3(0.0f, 0.1f, 0.0f),   // layer==1
                                    new Vector3(0.0f, 0.05f, 0.0f)};  // layer==2
        if (liftVecs.Length != numLayersLifted)
        {
            Debug.Log("Error(openingSecure): invalid liftVecs");
            return false;
        }
        GameObject sphere = null;
        for (int objIdx = 0; objIdx < m_numSphereModels; objIdx++)
        {
            numSpheresPerLayer = m_sphereJointModels[objIdx].m_numSpheres;
            for (int l = 0; l < numLayersLifted; l++)
            {
                for (int s = 0; s < numSpheresPerLayer; s++)
                {
                    sphere = m_sphereJointModels[objIdx].m_sphereGameObjects[l, s];
                    sphere.GetComponent<Rigidbody>().AddForce(liftVecs[l] * Time.deltaTime);
                }
            }
        }

        return true;
    }

    bool finalClosure(int layerIdx, bool bFullGrasping = true)
    {
        if (m_numSphereModels <= 0)
        {
            Debug.Log("Error(finalClosure): no models to conduct finalClosure!");
            return false;
        }

        // Remove fixed joints for closed layers
        for (int layer = 0; layer < layerIdx + 1; layer++)
        {
            if (layer > supportingJointsForJoining.Count - 1)
            {
                break;
            }

            supportingJointsForJoining[layer].ForEach(j => Destroy(j));
        }

        // conduct final-closure for both models at the same time
        List<bool> bFinalClosure = new List<bool>();
        bool bHalfClosure = false;
        for (int objIdx = 0; objIdx < m_numSphereModels; objIdx++)
        {
            bFinalClosure.Add(false);
            bHalfClosure = false;
            if (bFullGrasping == false && objIdx == 0)
                bHalfClosure = true;
            if (m_sphereJointModels[objIdx].closeupLayers(layerIdx, bHalfClosure))
                bFinalClosure[bFinalClosure.Count - 1] = true;
            else
            {
                Debug.Log("Error(finalClosure: failed on sphereJointModel" + objIdx.ToString());
                return false;
            }
        }

        m_bFinalClosure = true;
        Debug.Log("FinalClosure succeeds!");
        return true;
    }

    // get the final layer to split, m_layers2Split[1], based on LS tips positions
    bool getLayer2SplitBasedonLS()
    {
        int layer2Split = -1;

        Vector3 tipPos = lsController.topHalfFrontTip.position;

        // check LS-top tip is within which sphereModel's which layer bbox
        int whichColon = -1; // 0 or 1
        int layerNum;
        if ((m_bInsert[0] != 1 && m_bInsert[1] != 1) || (m_bInsert[0] == 1 && m_bInsert[1] == 1))
        {
            Debug.Log("Error in getLayer2SplitBasedonLS: invalid m_bInsert values!");
            return false;
        }

        for (int i = 0; i < 2; i++)
        {
            if (m_bInsert[i] == 1)
            {
                whichColon = i;
                break;
            }
        }
        layerNum = m_sphereJointModels[whichColon].m_numLayers;

        // condition 1: which layerBbox contains tipPos
        int j;
        for (j = 0; j < layerNum; j++)
        {
            if (m_sphereJointModels[whichColon].m_layerBoundingBox[j].Contains(tipPos))
            {
                layer2Split = j;
                //break;
            }
        }

        // condition 2: check z value
        if (layer2Split == -1)
        {
            float layerAvgZ, layerAvgZ1;
            for (j = 0; j < (layerNum - 1); j++)
            {
                layerAvgZ = 0.5f * (m_sphereJointModels[whichColon].m_layerBoundingBox[j].min.z + m_sphereJointModels[whichColon].m_layerBoundingBox[j].max.z);
                layerAvgZ1 = 0.5f * (m_sphereJointModels[whichColon].m_layerBoundingBox[j + 1].min.z + m_sphereJointModels[whichColon].m_layerBoundingBox[j + 1].max.z);
                if ((tipPos.z > layerAvgZ && tipPos.z <= layerAvgZ1) || ((j + 1) == (layerNum - 1) && tipPos.z > layerAvgZ1))
                    layer2Split = j;
            }
        }

        if (layer2Split == -1)
        {
            Debug.Log("Error in getLayer2SplitBasedonLS: invalid layer2Split!");
            return false;
        }

        Debug.Log("layer2Split: " + layer2Split.ToString());

        // incorporate button value
        if (m_bLSButtonFullDown)
            m_layers2Split[1] = layer2Split;
        else
            m_layers2Split[1] = (int)Mathf.Ceil(layer2Split * m_LSButtonValue);

        // safety check
        if (m_layers2Split[1] < 3)
            m_layers2Split[1] = 3;
        else if (m_layers2Split[1] > 14)
            m_layers2Split[1] = 14;

        Debug.Log("m_layers2Split: " + m_layers2Split[1].ToString());

        return true;
    }

    /// <summary>
    /// During the final-closure stage, check if LS fully grasps both colons
    ///     compare the LS tip with spheres of the given layer along x axis
    ///     check sphereJointModel0 only
    /// </summary>
    /// <returns>True: LS fully grasps both colons; False: no </returns> 
    bool checkLSFullGrasping()
    {
        Vector3 tipPos = lsController.bottomHalfFrontTip.position;
        float xMinColon0 = float.MaxValue;
        float tempX;
        for (int i = 0; i < m_sphereJointModels[0].m_numSpheres; i++)
        {
            tempX = m_sphereJointModels[0].m_sphereGameObjects[m_layer2FinalClose, i].transform.position.x;
            if (tempX < xMinColon0)
                xMinColon0 = tempX;
        }

        float tipOffsetX = 0.96f; // dist from tip to staple-end position along x
        m_bLSFullGraspFinalClosure = ((tipPos.x + tipOffsetX) <= xMinColon0) ? true : false;

        Debug.Log("LS fully grasps both colons? " + m_bLSFullGraspFinalClosure);

        return m_bLSFullGraspFinalClosure;
    }

    // update variables of LS
    void synchLSStas()
    {
        if (lsController)
        {
            m_bLSButtonPushing = lsController.isPushingHandle;
            m_bLSButtonPulling = lsController.isPushingHandle;
            m_bLSButtonFullDown = lsController.handlePushed;
            m_bLSInserting = (m_bInsert[0] > 0 || m_bInsert[1] > 0) ? true : false;
            m_bLSRemoving = (lsController.isTopRemoving == true || lsController.isBottomRemoving == true);
            m_bLSTransversing = lsController.isBottomHalfMovingInCuttingPlane;
            m_bLSLocked = LinearStaplerTool.leverLocked;
            m_LSButtonValue = lsController.handleReading;
            m_LSGraspLengthFinalClosure = lsController.bottomLastPhaseX;
            // record LS actions: {inserting, removing, buttongPushing, buttonPulling, lock, transversing}
            m_LSStates = new int[] {Convert.ToInt32(m_bLSInserting), Convert.ToInt32(m_bLSRemoving), Convert.ToInt32(m_bLSButtonPushing),
                                    Convert.ToInt32(m_bLSButtonPulling), Convert.ToInt32(m_bLSLocked), Convert.ToInt32(m_bLSTransversing)};
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateLayerPosition();
            /// find the forceps currently holding or grasping objs
            HapticSurgTools graspingForceps = null;
            HapticSurgTools holdingForceps = null;
            m_numHoldingForceps = 0;
            foreach (KeyValuePair<string, HapticSurgTools> ele in m_hapticSurgTools)
            {
                // check forceps only
                if (ele.Key == m_surgToolNames[0] || ele.Key == m_surgToolNames[1] || ele.Key == m_surgToolNames[2])
                {
                    if (m_hapticSurgTools[ele.Key])
                    {
                        // find the forceps currently grasping
                        if (graspingForceps == null && m_hapticSurgTools[ele.Key].curAction == HapticSurgTools.toolAction.grabbing)
                        {
                            graspingForceps = m_hapticSurgTools[ele.Key];
                        }
                        // find the forceps currently holding
                        if (m_hapticSurgTools[ele.Key].curAction == HapticSurgTools.toolAction.holding)
                        {
                            m_numHoldingForceps += 1;
                            if (holdingForceps == null)
                                holdingForceps = m_hapticSurgTools[ele.Key];
                        }
                    }
                }
            }

            /// Enterotomy
            // 'C': corner-cut both sphere-joint models
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (cornerCut())
                {
                    // Hide corner cut staples for both colon models
                    int[] LorR = { 1, 0 }; //objIdx==0: 1 (right); objIdx==1: 0 (left)
                    for (int objIdx = 0; objIdx < m_numSphereModels; objIdx++)
                    {
                        StapleLineManager.instance.LSSimStepTwo(objIdx, LorR[objIdx]);
                    }
                }
            }
            // [Haptics version]
            if (m_hapticSurgTools.Count > 0 && m_hapticSurgTools["Scissors"]) //scissors
            {
                // Check if corner-cut (Enterotomy) starts, and start the timer
                if (m_bCornerCutStarted == false && m_hapticSurgTools["Scissors"].curAction == HapticSurgTools.toolAction.touching)
                {
                    if (MetricsScoringManager)
                        MetricsScoringManager.m_EnterotomyStartTime = Time.time;
                    m_bCornerCutStarted = true;
                    Debug.Log("Enterotomy starts, startTime: " + MetricsScoringManager.m_EnterotomyStartTime.ToString());
                }

                if (m_hapticSurgTools["Scissors"].curAction == HapticSurgTools.toolAction.cutting)
                {
                    int objIdx = m_hapticSurgTools["Scissors"].cutSphereIdx[0];
                    if (objIdx >= 0 && objIdx < m_numSphereModels)
                    {
                        int LorR = -1;
                        if (cornerCut(objIdx, m_hapticSurgTools["Scissors"].cutSphereIdx[2], ref LorR))
                        {
                            StapleLineManager.instance.LSSimStepTwo(objIdx, LorR);
                            // update metrics scoring
                            if (MetricsScoringManager)
                            {
                                bool bOpeningSecure = (graspingForceps != null) ? true : false;
                                MetricsScoringManager.updateEnterotomyScores(objIdx, LorR, bOpeningSecure);

                                // update completion time
                                if (MetricsScoringManager.m_bEnterotomyTimeEvaluated == false
                                        && m_bCornerCutStarted && m_bCornerCut[0] && m_bCornerCut[1]) // completes when both corners are cut
                                {
                                    MetricsScoringManager.m_EnterotomyTime = Time.time - MetricsScoringManager.m_EnterotomyStartTime;
                                    MetricsScoringManager.m_bEnterotomyTimeEvaluated = true;
                                }
                            }
                        }
                    }

                    m_hapticSurgTools["Scissors"].curAction = HapticSurgTools.toolAction.touching;
                }
            }

            /// LS-Insertion 
            // [Haptic version]
            if (lsController && m_bLSInserting && !m_bJoin)
            {
                if (m_bInsert[0] + m_bInsert[1] > 0)
                {
                    // check if the openings are grasping by the forceps when inserting
                    bool bOpeningSecure = false;
                    if (graspingForceps)
                    {
                        bOpeningSecure = true;
                    }
                    // update metrics scoring
                    if (MetricsScoringManager)
                    {
                        // check if LS-Insertion starts and start the timeer
                        if (m_bLSInsertStarted == false)
                        {
                            MetricsScoringManager.m_LSInsertionStartTime = Time.time;
                            m_bLSInsertStarted = true;
                            Debug.Log("LSInsertion starts, startTime: " + MetricsScoringManager.m_EnterotomyStartTime.ToString());
                        }

                        if (m_bInsert[0] > 0)
                            MetricsScoringManager.updateLSInsertionScores(0, bOpeningSecure);
                        if (m_bInsert[1] > 0)
                            MetricsScoringManager.updateLSInsertionScores(1, bOpeningSecure);

                        // update completion time
                        if (MetricsScoringManager.m_bLSInsertionTimeEvaluated == false && m_bLSInsertStarted
                               && m_bLSLocked && m_bLSButtonPushing == true) // completes when LS locked and about to push button
                        {
                            MetricsScoringManager.m_LSInsertionTime = Time.time - MetricsScoringManager.m_LSInsertionStartTime;
                            MetricsScoringManager.m_bLSInsertionTimeEvaluated = true;
                        }
                    }
                }
            }

            /// Stapled-Anastomosis
            /*// 'S': split all the colons in the scene
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (m_bCornerCut[0] && m_bCornerCut[1])
                    split();
                else
                    Debug.Log("Error: Cannot split as either of the colons needs corner-cut!");
            }*/

            // 'J': split & join colons in one-action
            if (Input.GetKeyDown(KeyCode.J))
            {
                // split
                if (m_bCornerCut[0] && m_bCornerCut[1])
                    split();
                else
                    Debug.Log("Error: Cannot split as either of the colons needs corner-cut!");

                // join
                if (m_bSplit)
                {
                    if (join())
                    {
                        StapleLineManager.instance.LSSimStepThree(m_layers2Split[1]);
                        if (lsController)
                            lsController.JoinColonToolLogic();
                    }
                }
                else
                    Debug.Log("Error: Cannot join as colons have not been split yet!");
            }
            // [Haptic version]
            if (lsController && (m_bInsert[0] * m_bInsert[1] > 0))
            {
                // stapled anastomosis
                if (!m_bJoin && m_bLSLocked && m_LSButtonValue > 0.05 && lsController.isPullingHandle == true)
                {
                    if (getLayer2SplitBasedonLS())// get the final layer to split
                    {
                        // split
                        split();
                        // join
                        if (m_bSplit)
                        {
                            if (join())
                            {
                                StapleLineManager.instance.LSSimStepThree(m_layers2Split[1]);
                                if (lsController)
                                    lsController.JoinColonToolLogic();
                            }
                        }
                    }
                }
                // update metrics scoring
                if (MetricsScoringManager)
                {
                    // check if SA starts and start the timer
                    if (m_bSAStarted == false && !m_bJoin && m_bLSLocked && m_bLSButtonPushing) // LS locked and start pushing button
                    {
                        MetricsScoringManager.m_StapledAnastStartTime = Time.time;
                        m_bSAStarted = true;
                        Debug.Log("SA starts, startTime: " + MetricsScoringManager.m_StapledAnastStartTime.ToString());
                    }

                    MetricsScoringManager.updateStapledAnastScores(m_bLSButtonPushing, m_bLSButtonFullDown, m_bJoin, m_bLSRemoving, m_bLSLocked);
                }
            }
            // update completion time: evaluated when SA operation is done, both LS completely removed from colons
            if (lsController && m_bSAStarted == true && m_bJoin == true && (m_bInsert[0] + m_bInsert[1] == 0))
            {
                if (MetricsScoringManager && MetricsScoringManager.m_bStapledAnastTimeEvaluated == false)
                {
                    MetricsScoringManager.m_StapledAnastTime = Time.time - MetricsScoringManager.m_StapledAnastStartTime;
                    MetricsScoringManager.m_bStapledAnastTimeEvaluated = true;
                }
            }

            // handle partial-split join: pull end-split vertices for both colon meshes together
            if (m_bJoin && m_colonMeshes[0].endSplitVers2Join.Count <= 0 && m_colonMeshes[1].endSplitVers2Join.Count <= 0)
            {
                if (!m_colonMeshes[0].bFullSplit && !m_colonMeshes[1].bFullSplit)
                {
                    m_colonMeshes[0].endSplitVers2Join.Add("end_outer", m_colonMeshes[1].vertices[m_colonMeshes[1].startEndSplitVers["end_outer"]]);
                    m_colonMeshes[0].endSplitVers2Join.Add("end_inner", m_colonMeshes[1].vertices[m_colonMeshes[1].startEndSplitVers["end_inner"]]);
                    m_colonMeshes[1].endSplitVers2Join.Add("end_outer", m_colonMeshes[0].vertices[m_colonMeshes[0].startEndSplitVers["end_outer"]]);
                    m_colonMeshes[1].endSplitVers2Join.Add("end_inner", m_colonMeshes[0].vertices[m_colonMeshes[0].startEndSplitVers["end_inner"]]);
                }
            }

            /// Opening-Secure
            // 'H': opening-secure using forceps' holding action
            if (Input.GetKeyDown(KeyCode.H))
            {
                if (m_bJoin)
                {
                    m_bOpeningSecure = !m_bOpeningSecure;
                }
                else
                {
                    Debug.Log("Cannot conduct opening-secure: Do split&join first!");
                }
            }
            if (m_bOpeningSecure)
                openingSecure(true);

            //  [haptic version]
            if (holdingForceps) //forceps
            {
                if (m_bJoin)
                {
                    // check which obj being holding, only if 1st layer held, valid objIdx
                    if ((holdingForceps.holdSphereIDs[0] == 0 || holdingForceps.holdSphereIDs[0] == 1)
                         && holdingForceps.holdSphereIDs[1] < 2) // holdSphereIDs: [objIdx, layerIdx, sphereIdx]
                    {
                        // check the sphere being held belongs to one of the predefined openings
                        bool bSecureNewOpening = false;
                        for (int whichOpening = 0; whichOpening < m_numSecureOpenings; whichOpening++)
                        {
                            if (m_sphIndices4Secure[whichOpening].Contains(holdingForceps.holdSphereIDs[2]))
                            {
                                if (m_sphereIdx4EachOpening[whichOpening] == -1) // new opening secure
                                {
                                    bSecureNewOpening = true;
                                    m_sphereIdx4EachOpening[whichOpening] = holdingForceps.holdSphereIDs[2];
                                    Debug.Log("Hold opening " + whichOpening.ToString());
                                }
                                break;
                            }
                        }
                        openingSecure(bSecureNewOpening);
                    }
                }
            }

            /// Final-Closure
            // 'V': final close both colons using LS
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (!m_bJoin)
                    Debug.Log("Error: Cannot conduct finalClosure as the colons have not joined yet!");
                else if (m_bFinalClosure)
                    Debug.Log("Error: Cannot conduct finalClosure as that's already conducted!");
                else
                {
                    if (m_layer2FinalClose <= 0 || m_layer2FinalClose >= 20)
                    {
                        Debug.Log("Error: Invalid final close layer!");
                        return;
                    }
                    if (!finalClosure(m_layer2FinalClose))
                        Debug.Log("Final Closure failed!");
                    else
                    {
                        StapleLineManager.instance.LSSimStepFour(m_layer2FinalClose - 1);
                    }
                }
            }
            // [Haptic version]
            if (lsController)
            {
                if (m_bLSTransversing && !m_bFinalClosure)
                {
                    // Final closure
                    bool LSButton2Push = (lsController.handleReading > 0.05 && lsController.isPullingHandle == true); // true if the button is about to push
                    bool bFullGrasping = false;
                    if (m_bLSLocked == true)
                    {
                        bFullGrasping = checkLSFullGrasping();
                        m_layer2FinalClose = lsController.lastPhaseLockedLayer;
                        if (m_layer2FinalClose <= 0 || m_layer2FinalClose >= 20)
                        {
                            Debug.Log("Error: Invalid final close layer!");
                            return;
                        }
                        if (LSButton2Push)
                        {
                            if (!m_bJoin)
                                Debug.Log("Error: Cannot conduct finalClosure as the colons have not joined yet!");
                            else
                            {
                                if (!finalClosure(m_layer2FinalClose, bFullGrasping))
                                    Debug.Log("Final Closure failed!");
                                else
                                {
                                    StapleLineManager.instance.LSSimStepFour(m_layer2FinalClose - 1, !bFullGrasping);
                                }
                            }
                        }
                    }

                    // update metrics scores
                    if (MetricsScoringManager)
                    {
                        // check if Final-Closure started and start the timer: LS's bottom part is just transversely placed
                        if (m_bFinalClosureStarted == false)
                        {
                            MetricsScoringManager.m_FinalClosureStartTime = Time.time;
                            m_bFinalClosureStarted = true;
                            Debug.Log("Final-Closure starts, start time: " + MetricsScoringManager.m_FinalClosureStartTime.ToString());
                        }

                        MetricsScoringManager.updateFinalClosureScores(m_bFinalClosure, m_numHoldingForceps, bFullGrasping,
                                                                       m_layer2FinalClose, LSButton2Push, m_bLSLocked, m_bLSButtonFullDown);

                        // update completion time at soon as Final-Closure is just done
                        if (m_bFinalClosure == true && m_bFinalClosureStarted == true)
                        {
                            if (MetricsScoringManager.m_FinalClosureTimeEvaluated == false)
                            {
                                MetricsScoringManager.m_FinalClosureTime = Time.time - MetricsScoringManager.m_FinalClosureStartTime;
                                MetricsScoringManager.m_FinalClosureTimeEvaluated = true;
                            }
                        }
                    }
                }
            }

            // Evaluate the final score and time
            if (lsController && MetricsScoringManager)
            {
                if (MetricsScoringManager.m_bFinalResultEvaluated == false && (m_bFinalClosure == true || m_bSimEnd == true))
                {
                    MetricsScoringManager.evaluateLSCompletion();
                }
            }

            // Check user manual stop simulation
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_bSimEnd = true;
            }

            // update LS status
            synchLSStas();
        }

        // Testing
        TestScissorCutConditions();
    }

    /// <summary>
    /// 
    /// </summary>
    public void UpdateLayerPosition()
    {
        for (int l = 0; l < colonLayerAveragePosition[0].Count; l++)
        {
            Vector3 sum = Vector3.zero;
            for (int s = 0; s < colon0Spheres[l].Count; s++)
            {
                sum += colon0Spheres[l][s].position;
            }
            colonLayerAveragePosition[0][l] = sum / (float)colon0Spheres[l].Count;
        }

        for (int l = 0; l < colonLayerAveragePosition[1].Count; l++)
        {
            Vector3 sum = Vector3.zero;
            for (int s = 0; s < colon1Spheres[l].Count; s++)
            {
                sum += colon1Spheres[l][s].position;
            }
            colonLayerAveragePosition[1][l] = sum / (float)colon1Spheres[l].Count;
        }
    }

    public static int GetSphereLayer(string sphereName)
    {
        if (sphereName[10] == '_')
        {
            return int.Parse(sphereName[9].ToString());
        }
        else
        {
            return int.Parse(sphereName[9].ToString() + sphereName[10].ToString());
        }
    }

    public bool IsSphereGrabbedByForceps(GameObject sphere)
    {
        return m_hapticSurgTools.Values.ToList().Exists(s => s.grabbing == sphere);
    }
}
