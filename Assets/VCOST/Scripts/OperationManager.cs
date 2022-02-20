﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;
using Utility;

public class OperationManager : MonoBehaviour
{
    public static OperationManager instance = null;

    static Mesh s_anvilColonMesh;

    [Header("Prefabs")]
    public StaticHoleColliderBehavior p_attachObject;
    public StaticHoleColliderManager p_staticHoleColliderManager;
    public GameObject p_sutureRodNeedlePrefab;
    public GameObject p_anvil;
    public ObiCollider p_pseudoColonCollider;
    public ForcepBehavior p_needleHolder;

    [Header("Managers")]
    public HapticColonGrabbersBehavior grabberControl;
    private StaticHoleColliderManager anvilColonHoleColliderManager = null;

    [Header("Scene References")]
    public HapticPlugin hapticPlugin;
    public Transform spawnEquipmentAt;
    public BlueprintParticleIndividualizer anvilColon;
    public SkinnedMeshRenderer anvilColonSkin;
    public NeedleBehavior anvilNeedle;
    public RodBlueprintParticleIndividualizer anvilRod;
    public GameObject CircularStaplerPair;

    [Header("Demo Objects - To be removed")]
    public HapticGrabber p_grabber;

    private int _currentPhase = 0;

    private GameObject activePseudoColonCollider;

    int mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    private void Awake()
    {
        if (instance != null) Destroy(this);
        else instance = this;
    }

    private void Start()
    {
        //Go to phase 1
        AdvancePhase();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Advancing to phase " + (_currentPhase + 1));
            AdvancePhase();
        }
    }

    public void AdvancePhase()
    {
        switch (++_currentPhase)
        {
            case 1:
                PhaseOneStart();
                break;
            case 2:
                PhaseTwoStart();
                break;
            case 3:
                PhaseThreeStart();
                break;
            case 4:
                PhaseFourStart();
                break;
            case 5:
                PhaseFiveStart();
                break;
            default:
                Debug.Log(this.ToString() + " Is at invalid phase!");
                break;
        }
    }

    //Grab and secure anvil side colon
    //Disable Needle
    public void PhaseOneStart()
    {
        anvilNeedle.enabled = false;
        grabberControl.SetForcepSpawning(true);
    }

    List<int> foundParticles;
    //Suture anvil side colon
    /*
     * Find side of colon held open by curved forceps,
     * spawn StaticHoleColliderManager and enable/create suture needle
     */
    public void PhaseTwoStart()
    {
        //Haptic device gameobject may be disabled when debugging
        if (grabberControl.gameObject.activeSelf)
        {
            grabberControl.SwitchGrabberObject(p_needleHolder);
            grabberControl.SetForcepSpawning(false);
            grabberControl.activeForceps.doObi = false;
        }

        //Disable forcep colliders to prevent explosions AND
        //Get average position of forceps
        Vector3 averagePosition = Vector3.zero;
        foreach(ForcepBehavior f in grabberControl.attachedForceps)
        {
            ObiCollider[] foundColliders = f.GetComponentsInChildren<ObiCollider>();
            foreach(ObiCollider col in foundColliders)
            {
                col.enabled = false;
                col.sourceCollider.enabled = false;
            }

            averagePosition += f.transform.position;
        }
        if(grabberControl.attachedForceps.Count > 0)
            averagePosition /= grabberControl.attachedForceps.Count;

        anvilColonHoleColliderManager = Instantiate(p_staticHoleColliderManager, averagePosition, Quaternion.identity, transform);
        anvilColonHoleColliderManager.softbody = anvilColon;
        anvilColonHoleColliderManager.rod = anvilRod;
        anvilNeedle.holeManager = anvilColonHoleColliderManager;

        //Set up the anvil suture to advance the phase of the operation once
        //the end of the line is reached
        anvilColonHoleColliderManager._E_EndOfRodReached.AddListener(AdvancePhase);

        anvilNeedle.enabled = true;
        anvilNeedle.suturable = anvilColon;
        anvilNeedle.rod = anvilRod;

        //Create the pseudo colon collider for rod collisions
        activePseudoColonCollider = anvilColon.CreateColliderSnapshot(p_pseudoColonCollider);
    }

    //Place anvil
    public void PhaseThreeStart()
    {
        GameObject anvil = Instantiate(
            p_anvil,
            Vector3.zero,
            Quaternion.identity);

        //Set pull towards from reference in anvil
        GameObject pullTowards = anvil.GetComponent<GameObjectReference>().reference;
        anvilColonHoleColliderManager.CenterPullTowards = pullTowards.transform;

        grabberControl.SwitchToObject(anvil);
        grabberControl.SetForcepSpawning(true);
    }

    //Begin Closing of the colon
    public void PhaseFourStart()
    {
        //Destroy pseudo colon collider
        Destroy(activePseudoColonCollider);

        if(grabberControl.gameObject.activeSelf)
            grabberControl.activeForceps.doObi = false;

        //Remove rod attachment to end-of-line gameobject
        Destroy(anvilRod.GetComponent<ParticleAttachmentReference>().reference);

        //Remove forceps holding colon in place
        foreach(ForcepBehavior f in grabberControl.attachedForceps)
        {
            f.DestroyAttachment();
            Destroy(f.gameObject);
        }

        //Remove attachment between holecolliders and colon
        foreach(StaticHoleColliderBehavior staticHoleCollider in anvilColonHoleColliderManager.inner)
        {
            Destroy(staticHoleCollider.heldAttachment);
        }


        foundParticles =
            anvilColon.GetClosestRestingSliceOfParticles(
                Vector3.back * 999f,
                0.2f,
                true);

        foreach (int i in foundParticles)
        {
            StaticHoleColliderBehavior outerPart = Instantiate(
                p_attachObject, 
                anvilColon.WorldPositionOfParticle(i), 
                Quaternion.identity, 
                anvilColonHoleColliderManager.transform);
            anvilColonHoleColliderManager.AddOuter(outerPart);
        }

        //Done, tell collider manager to start closing
        anvilColonHoleColliderManager.StartPullingBehaviour();

        //Enable rod normalizer so stretching artifacts are minimized
        /*
        ObiParticleAttachment[] rodAttachments = anvilRod.GetComponents<ObiParticleAttachment>();
        ObiParticleAttachment needleAttachment = null;
        foreach (ObiParticleAttachment p in rodAttachments)
        {
            if (p.particleGroup.name == "needleEnd")
            {
                needleAttachment = p;
            }
        }
        ObiParticleAttachment lastHoleCollider = null;
        lastHoleCollider = anvilColonHoleColliderManager.rodAttachments[anvilColonHoleColliderManager.rodAttachments.Count - 1];

        if (needleAttachment == null || lastHoleCollider == null) Debug.LogError("OperationManager - Getting rod strech range failed!");
        
        anvilRod.GetComponent<RodStretchNormalizer>().EnableAndLimitBetweenExclusively(
            lastHoleCollider.particleGroup.particleIndices[0],
            needleAttachment.particleGroup.particleIndices[0]);
            */

        //Disable rod smoothing if it is present as it can cause weird behavior when combined with the buncher
        ObiPathSmoother pathSmoother = anvilRod.GetComponent<ObiPathSmoother>();
        if (pathSmoother)
        {
            pathSmoother.decimation = 0;
            pathSmoother.smoothing = 0;
            pathSmoother.twist = 0;
        }

        //Enable rod bunching to eliminate extra rod slack between suture points
        anvilRod.GetComponent<RodBuncher>().EnableAndDoScanFrom(anvilColonHoleColliderManager.rodAttachments);
    }

    //Use circular stapler
    private void PhaseFiveStart()
    {
        CircularStaplerPair.SetActive(true);

        //This will need to be removed at some point
        MeshRenderer[] meshes = grabberControl.activeForceps.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer m in meshes)
        {
            m.enabled = false;
        }

        grabberControl.enabled = false;
    }
}
