﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Simulation logic & steps
/// Not allowing flat entry then lift up (original plan?):
/// 1. If user inserted LS without using forceps, then the colon will stay flat on the table, and as long as the LS is inserted, the colon can not be raised up by the forceps
/// 2. During lifting up, when user release forceps, colon will raise/fall with the stapler if the stapler is inserted. If the stapler is not inserted, then the colon will hang in mid-air with the released forceps
/// 
/// Allowing flat entry then lift up (but only lift up by the forceps):
/// 1. User insert LS without lift up, then use forceps to grab the colon then lift it up
/// 2. During lifting up, when user release forceps, colon will raise/fall with the stapler if the stapler is inserted. If the stapler is not inserted, then the colon will hang in mid-air with the released forceps
/// 
/// Allowing flat entry then lift up (can also lift up without using the forceps and just raise the LS, better imo):
/// If the colon is lifted up with the forceps without LS insertion, then the colon raise is controlled by the LS, however as long as LS is inserted, the colon will be solely controlled by the LS, 
/// and the forceps will not move with the colon anymore, and the picking will behave like the general picking
/// 
/// Do not allow LS entry at all if the colon is not picked up by the forceps:
/// Need further discussion on what to do if user try to push in the LS without lifting the colon up
/// Potential behavior: 
/// LS will push the colon away (more difficult to do because need to have a method to bring colon back to position)
/// Colon will stick on the table, if LS is pushed against the colon, it will stop moving forward
/// 
/// </summary>
public class LinearStaplerTool : MonoBehaviour //inherits Tool class
{
    public bool usingRawSensorControl;
    public LS_UIcontroller uiController;
    public GameObject FiringHandle;
    public Transform firingHandleStartPosition;
    public Transform firingHandleEndPosition;
    public float firingHandleMoveSpeed; // How fast the handle moves with key press
    public GameObject LockingLever;
    public List<StaplerAttachDetection> attachValidators; // Trigger colliders that validates the tool's two parts' positions to see if they are within attaching distance
    public float attachDepthDifference; // How close (on a 0-1 scale) the two LS parts needs to be during insertion for them to be able to be locked together
    public Transform bottomPartLockingPosition; // Where the bottom part of the tool should be when it is locked with the top part in the insertion phase
    public Transform bottomPartFullyLockingPosition; // Where the bottom part of the tool should be when it is locked with the top part after joining colon
    public Transform bottomPartLastPhaseLockingPosition; // Where the bottom part of the tool should be when it is locked with the top part in the last phase
    public GameObject topHalf;
    public GameObject bottomHalf; // Bottom half of the tool (the half without moving parts)
    public Transform topTracker;
    public Transform bottomTracker; // Tracker for the bottom half of the tool
    public Transform topPartDesignatedCalibrationReference; // Reference point that user have to match up to with the real tool when performing calibration for tool rotation and position
    public Transform bottomPartDesignatedCalibrationReference;
    public List<Collider> topPartColliders;
    public List<Collider> bottomPartColliders;
    public float calibrationMoveSpeed; // How fast the tool will move when user using UI button to move it around
    public float calibrationRotateSpeed;
    public Transform calibrateRotationParent; // When calibrating tool rotation, the tool will be put under this object
    public Transform calibratingToolOriginalParent;
    // For LS tool insertion detection
    public List<Transform> colonAopenSpheres; // Spheres that will create the insertion opening on the colon0
    public List<Transform> colonBopenSpheres;
    public Transform topHalfFrontTip; // The front of the tip of the top half of the LS tool
    public Transform bottomHalfFrontTip;
    public float tipProximityCondition;
    public List<Transform> colonAsecondLayerSpheres;
    public List<Transform> colonAlastLayerSpheres;
    public List<Transform> colonA10thLayerSpheres;
    public List<Transform> colonBsecondLayerSpheres;
    public List<Transform> colonBlastLayerSpheres;
    public List<Transform> colonB10thLayerSpheres;
    public float angleDifferenceCondition; // What's the maximum allowing angle difference from the LS tool to the colon direction for the LS tool to enter insertion phase
    public float tipExitProximityMultiplier; // How many times the tip exiting proximity range it is for the LS tool to exit insertion phase
    public LinearStaplerColonDetector topHalfColonDetector; // The trigger at the front of the LS that detects colon sphere and stop it inserting if the colon is not secured by forceps
    public LinearStaplerColonDetector bottomHalfColonDetector;
    // For LS tool after joining colon action
    public List<Transform> joinedColonFirstLayerSpheres;
    public List<Transform> joinedColonLastLayerSpheres;
    public List<Transform> joinedColon10thLayerSpheres;
    public float joiningPhaseToolMovingAxisDifference; // After the colons are joined, how much the top tool should move down
    // For LS tool in closure phase
    public Transform bottomPartLastPhaseProximityDetector; // Use to verify if bottom part enters target position for last closure phase
    public List<Transform> joinedColonFirstLayerLowerSpheres;
    public List<Transform> joinedColonSecondLayerLowerSpheres;
    public List<Transform> joinedColonThirdLayerLowerSpheres;
    public List<Transform> joinedColonForthLayerLowerSpheres;
    public List<Transform> joinedColonFifthLayerLowerSpheres;
    public List<Transform> joinedColonSixthLayerLowerSpheres;
    public float lastPhaseToolBottomMovingAxisDifference; // During the last phase, how much the bottom tool should move down
    public float lastPhaseTipHorizontalProximityCondition; // How close the tip has to be to the joined colon center on the x axis to enter last phase moving plane
    public float lastPhaseBottomFurthestDistance; // How far end the movement plane far end can be from the colon opening
    public float lsToolInsertDepthRecordTime; // How far back in time do we keep record the LS tool depth during insertion phase
    public float lsToolMovingStateThreshold; // How much the tool depth has to move in the given time for it to be considered being inserting/removing
    public float lsToolMovingStateThresholdTime; // The given time
    // For simulate colon friction when inserting LS
    public List<Rigidbody> colonAfrictionSpheres; // Spheres to add force while LS is moving inside to simulate friction
    public List<Rigidbody> colonBfrictionSpheres;
    public float frictionForceStrength;
    public List<sphereJointModel> colonControllers;
    // For LS collision with colon
    public Transform topLineStart; // Start point of the collision line of the top LS part
    public Transform topLineEnd; // End point of the collision line of the top LS part
    public Transform bottomLineStart; // Start point of the collision line of the bottom LS part
    public Transform bottomLineEnd; // End point of the collision line of the bottom LS part
    public float staplerInsertionCollisionThreshold; // Distance from any sphere to the LS to be pushed back

    public Transform currentCalibratingHalf; // Which half of the tool is being calibrated;
    public Vector3 currentCalibratingDirection; // Which direction (local position or local eulerangles) is being calibrated
    public bool calibratingPosition; // Is the local position (true) or local eulerangles (false) being calibrated currently
    public bool handlePushed;
    public static bool leverLocked;
    public bool inAnimation; // Is the tool currently in any animation
    public float handleReading; // Sensor input for the firing handle position (should be from 0 to 1, 1 is pushed all the way in)
    public float leverReading; // Sensor input for the locking level angle (1 should be the lock position)
    public bool isPushingHandle;
    public bool isPullingHandle; // Is user pulling handle (from sensor reading or key press)
    public bool topHalfInserted; // Is top half inserting into colon ### May not be needed
    public bool bottomHalfInserted;
    public Vector3 bottomPartRelativeTrackerPosition;
    public Quaternion bottomPartRelativeTrackerRotation;
    public Vector3 topPartRelativeTrackerPosition;
    public Quaternion topPartRelativeTrackerRotation;
    public bool canUserLockToolTransform;
    public bool topTransformLocked; // Is the position & rotation of the tool top part locked
    public Transform topParentBeforeLock;
    public Vector3 topLocalPositionBeforeLock;
    public Quaternion topLocalRotationBeforeLock;
    public bool bottomTransformLocked;
    public Transform bottomParentBeforeLock;
    public Vector3 bottomLocalPositionBeforeLock;
    public Quaternion bottomLocalRotationBeforeLock;
    // Simulation states
    public int simStates; // Which step the simulation is at (0: before joining, 1: after joining before take out the tool from colon, 2: after take out the tool from colon
    // Insertion phase states
    /// <summary>
    /// 0: colon is not being inserted and is not being picked up by forceps, both methods can start
    /// 1: colon is being inserted without being lifted up, it will stay flat on the table during the insertion, and cannot be lifted up
    /// 2: colon is being lifted up first
    /// </summary>
    public int insertionStates;
    // Colon info
    public Vector3 colonAopeningPos;
    public Vector3 colonBopeningPos;
    public List<float> insertionDepthInspector;
    public bool isTopInserting; // Is user inserting LS top part into colon
    public bool isTopRemoving;
    public bool isBottomInserting;
    public bool isBottomRemoving;
    public bool isColonAInserting;
    public bool isColonARemoving;
    public bool isColonBInserting;
    public bool isColonBRemoving;
    public List<bool> colonSecuredByForceps;
    //public bool isTopHalfMovingInCuttingPlane; // Is the top half tool locked onto the moving plane for the last cutting step
    public bool isBottomHalfMovingInCuttingPlane; // Is the bottom half tool locked onto the moving plane for the last cutting step
    public float bottomLastPhaseX; // The position difference on the x-axis from the mean joinedColonFirstLayerLowerSpheresPosition to bottom half position
    public List<float> colonAInsertDepthRecord; // Record the tool insert depth inside colonA
    public List<float> colonBInsertDepthRecord;
    public List<float> insertDepthRecordTimeStamps;
    public static LinearStaplerTool instance;
    public List<List<List<Transform>>> colonSpheres;
    public List<List<Vector3>> colonLayerSpherePositionAVG; // Average position of the spheres in each layer for each colons

    // Tool moving axis
    public List<Transform> topPartMovingAxisStart;
    public List<Transform> topPartMovingAxisEnd;
    public List<Transform> bottomPartMovingAxisStart;
    public List<Transform> bottomPartMovingAxisEnd;
    public Vector3 topPartMovingAxisStartPoint;
    public Vector3 topPartMovingAxisEndPoint;
    public Vector3 bottomPartMovingAxisStartPoint;
    public Vector3 bottomPartMovingAxisEndPoint;
    public Vector3 joinedColonFirstLayerLowerSpheresPosition;
    public Vector3 joinedColonSecondLayerLowerSpheresPosition;
    public Vector3 joinedColonThirdLayerLowerSpheresPosition;
    public Vector3 joinedColonForthLayerLowerSpheresPosition;
    public Vector3 joinedColonFifthLayerLowerSpheresPosition;
    public Vector3 joinedColonSixthLayerLowerSpheresPosition;
    public int lastPhaseLockedLayer;

    // Calibration
    public float positionCalibrationSpeed;
    public float rotationCalibrationSpeed;

    public void Start()
    {
        LoadStapleToolCalibrationData();
        SaveToolLocalPositionRotation();
        insertionDepthInspector = new List<float>(globalOperators.m_insertDepth);
        simStates = 0;
        canUserLockToolTransform = false;

        colonAInsertDepthRecord = new List<float>();
        colonBInsertDepthRecord = new List<float>();
        insertDepthRecordTimeStamps = new List<float>();
        colonSecuredByForceps = new List<bool>();
        colonSecuredByForceps.Add(false);
        colonSecuredByForceps.Add(false);

        colonSpheres = new List<List<List<Transform>>>();
        colonLayerSpherePositionAVG = new List<List<Vector3>>();
        for (int c = 0; c < colonControllers.Count; c++)
        {
            colonLayerSpherePositionAVG.Add(new List<Vector3>());
            for (int l = 0; l < 20; l++)
            {
                colonLayerSpherePositionAVG[c].Add(Vector3.zero);
            }
        }

        instance = this;
    }

    public void InitializaColonSphereRef()
    {
        for (int c = 0; c < colonControllers.Count; c++)
        {
            colonSpheres.Add(new List<List<Transform>>());
            for (int l = 0; l < colonControllers[0].m_sphereGameObjects.GetLength(0); l++)
            {
                colonSpheres[c].Add(new List<Transform>());
                for (int s = 0; s < colonControllers[0].m_sphereGameObjects.GetLength(1); s++)
                {
                    colonSpheres[c][l].Add(colonControllers[c].m_sphereGameObjects[l, s].transform);
                }
            }
        }
    }

    void Update() //Checks status of knob, lever, and linear stapler in every frame
    {
        if (colonSpheres.Count == 0 && colonControllers[0].m_sphereGameObjects != null && colonControllers[1].m_sphereGameObjects != null)
        {
            InitializaColonSphereRef();
        }

        UpdateColonLayerPosition();

        // Prevent user interaction if the tool is currently in an animation
        if (inAnimation)
        {
            return;
        }

        if (currentCalibratingHalf != null)
        {
            // If just started calibration
            if (currentCalibratingHalf.parent != calibrateRotationParent)
            {
                BeginCalibrateRotation();
            }

            // If user release mouse from currently pushed calibrating button then stop calibrating
            if (Input.GetMouseButtonUp(0))
            {
                EndCalibrateRotation();
                currentCalibratingHalf = null;
                return;
            }

            if (calibratingPosition)
            {
                Vector3 newPos = currentCalibratingHalf.localPosition + currentCalibratingDirection * calibrationMoveSpeed * Time.deltaTime;
                currentCalibratingHalf.localPosition = newPos;
            }
            else
            {
                Quaternion newRot = Quaternion.Euler(calibrateRotationParent.eulerAngles + currentCalibratingDirection * calibrationRotateSpeed * Time.deltaTime);
                calibrateRotationParent.rotation = newRot;
            }
        }

        // Check user input for locking LS parts transform
        if (canUserLockToolTransform)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                ToggleTopTransformLock();
            }
            if (Input.GetKeyDown(KeyCode.Semicolon))
            {
                ToggleBottomTransformLock();
            }
        }

        // Read key press for moving firing handle
        if (Input.GetKey(KeyCode.UpArrow))
        {
            isPushingHandle = true;
            handleReading = Mathf.Clamp01(handleReading + Time.deltaTime * firingHandleMoveSpeed);
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            isPushingHandle = false;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            isPullingHandle = true;
            handleReading = Mathf.Clamp01(handleReading - Time.deltaTime * firingHandleMoveSpeed);
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            isPullingHandle = false;
        }

        UpdateKnobWithSensorReading();
        LockLeverWithKeyboardInput();
        insertionDepthInspector[0] = globalOperators.m_insertDepth[0];
        insertionDepthInspector[1] = globalOperators.m_insertDepth[1];

        // If top half is close to bottom half in the last phase and is on the moving plane then auto adjust it's alignment to always face up
        if (simStates >= 2 && Vector3.Distance(topHalf.transform.position, bottomHalf.transform.position) < 2 && !topTransformLocked && isBottomHalfMovingInCuttingPlane)
        {
            if (!LinearStaplerTool.instance.usingRawSensorControl)
            {
                topHalf.transform.LookAt(topHalf.transform.position + Vector3.down);
            }
        }
    }

    private void LateUpdate()
    {
        // Update colon info
        colonAopeningPos = GetPositionMean(colonAopenSpheres);
        colonBopeningPos = GetPositionMean(colonBopenSpheres);
        //colonAsecondLayerPos = GetPositionMean(colonAsecondLayerSpheres);
        //colonBsecondLayerPos = GetPositionMean(colonBsecondLayerSpheres);
        //colonAlastLayerPos = GetPositionMean(colonAlastLayerSpheres);
        //colonBlastLayerPos = GetPositionMean(colonBlastLayerSpheres);

        //if (simStates == 1)
        {
            // Update tool moving axis info
            topPartMovingAxisStartPoint = GetPositionMean(topPartMovingAxisStart);
            topPartMovingAxisEndPoint = GetPositionMean(topPartMovingAxisEnd);
            topPartMovingAxisStartPoint += Vector3.up * joiningPhaseToolMovingAxisDifference;
            topPartMovingAxisEndPoint += Vector3.up * joiningPhaseToolMovingAxisDifference;
        }
        //else if (simStates > 1)
        {
            bottomPartMovingAxisStartPoint = GetPositionMean(bottomPartMovingAxisStart);
            bottomPartMovingAxisEndPoint = GetPositionMean(bottomPartMovingAxisEnd);
            bottomPartMovingAxisStartPoint += Vector3.up * lastPhaseToolBottomMovingAxisDifference;
            bottomPartMovingAxisEndPoint += Vector3.up * lastPhaseToolBottomMovingAxisDifference;
        }

        if (LinearStaplerTool.instance.usingRawSensorControl)
        {
            if (globalOperators.m_bInsert[0] == 1)
            {
                globalOperators.m_insertDepth[0] = GetToolInsertionDepth(topLineStart, topPartMovingAxisStartPoint, topPartMovingAxisEndPoint);
            }
            if (globalOperators.m_bInsert[1] == 1)
            {
                globalOperators.m_insertDepth[1] = GetToolInsertionDepth(topLineStart.transform, topPartMovingAxisStartPoint, topPartMovingAxisEndPoint);
            }
            if (globalOperators.m_bInsert[0] == 2)
            {
                globalOperators.m_insertDepth[0] = GetToolInsertionDepth(bottomLineStart.transform, bottomPartMovingAxisStartPoint, bottomPartMovingAxisEndPoint);
            }
            if (globalOperators.m_bInsert[1] == 2)
            {
                globalOperators.m_insertDepth[1] = GetToolInsertionDepth(bottomLineStart.transform, bottomPartMovingAxisStartPoint, bottomPartMovingAxisEndPoint);
            }

            CheckAndUpdateLStoolInsertionStates();
            LinearStaplerColonInsertionCollisionInteraction();

            return;
        }

        if (simStates < 2)
        {
            CheckAndUpdateLStoolInsertionStatesForConfinedLSmovement();
        }
        if (globalOperators.m_bInsert[0] == 1)
        {
            globalOperators.m_insertDepth[0] = LockToolMovement(topHalf.transform, topPartMovingAxisStartPoint, topPartMovingAxisEndPoint, Vector3.up * Mathf.Sign(topHalf.transform.up.y));
        }
        if (globalOperators.m_bInsert[1] == 1)
        {
            globalOperators.m_insertDepth[1] = LockToolMovement(topHalf.transform, topPartMovingAxisStartPoint, topPartMovingAxisEndPoint, Vector3.up * Mathf.Sign(topHalf.transform.up.y));
        }

        // If bottom part is locked with top part then stop updating it
        if (bottomHalf.transform.parent == bottomTracker)
        {
            if (globalOperators.m_bInsert[0] == 2)
            {
                globalOperators.m_insertDepth[0] = LockToolMovement(bottomHalf.transform, bottomPartMovingAxisStartPoint, bottomPartMovingAxisEndPoint, Vector3.up * Mathf.Sign(bottomHalf.transform.up.y));
            }
            if (globalOperators.m_bInsert[1] == 2)
            {
                globalOperators.m_insertDepth[1] = LockToolMovement(bottomHalf.transform, bottomPartMovingAxisStartPoint, bottomPartMovingAxisEndPoint, Vector3.up * Mathf.Sign(bottomHalf.transform.up.y));
            }
        }

        // Check for bottom part enter cutting plane
        if (simStates >= 2)
        {
            CheckAndUpdateLStoolTransverseStates();
            // Update track
            joinedColonFirstLayerLowerSpheresPosition = GetPositionMean(joinedColonFirstLayerLowerSpheres);
            joinedColonSecondLayerLowerSpheresPosition = GetPositionMean(joinedColonSecondLayerLowerSpheres);
            joinedColonThirdLayerLowerSpheresPosition = GetPositionMean(joinedColonThirdLayerLowerSpheres);
            joinedColonForthLayerLowerSpheresPosition = GetPositionMean(joinedColonForthLayerLowerSpheres);
            joinedColonFifthLayerLowerSpheresPosition = GetPositionMean(joinedColonFifthLayerLowerSpheres);
            joinedColonSixthLayerLowerSpheresPosition = GetPositionMean(joinedColonSixthLayerLowerSpheres);
        }
        // Move bottom part along colon during last phase
        if (isBottomHalfMovingInCuttingPlane)
        {
            joinedColonFirstLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            joinedColonSecondLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            joinedColonThirdLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            joinedColonForthLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            joinedColonFifthLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            joinedColonSixthLayerLowerSpheresPosition += lastPhaseToolBottomMovingAxisDifference * Vector3.up;
            LockToolMovementInPlaneDuringLastStep(bottomHalf.transform, joinedColonFirstLayerLowerSpheresPosition, joinedColonFifthLayerLowerSpheresPosition);
        }
        //if (isTopHalfMovingInCuttingPlane)
        //{
        //    joinedColonFirstLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    joinedColonSecondLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    joinedColonThirdLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    joinedColonForthLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    joinedColonFifthLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    joinedColonSixthLayerLowerSpheresPosition += lastPhaseToolTopMovingAxisDifference * Vector3.up;
        //    LockToolMovementInPlaneDuringLastStep(topHalf.transform, joinedColonFirstLayerLowerSpheresPosition, joinedColonSixthLayerLowerSpheresPosition);
        //}

        RecordToolInsertionStatus();
    }

    void FireKnobWithKeyboardInput()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            // Handle translates to final position if T is pressed and linear stapler is enabled
            if (!handlePushed)
            {
                FiringHandle.transform.localPosition = firingHandleEndPosition.localPosition;
                print("Firing Handle is engaged.");
            }
            // Handle translates to initial position if T is pressed again and linear stapler is enabled
            else
            {
                FiringHandle.transform.localPosition = firingHandleStartPosition.localPosition;
                print("Firing Handle is not engaged.");
            }

            handlePushed = !handlePushed;
        }
    }

    void UpdateKnobWithSensorReading()
    {
        FiringHandle.transform.localPosition = Vector3.Lerp(firingHandleStartPosition.localPosition, firingHandleEndPosition.localPosition, handleReading);

        handlePushed = handleReading > 0.95f ? true : false;
    }

    void LockLeverWithKeyboardInput()
    {
        if (Input.GetKeyUp(KeyCode.Y))
        {
            //Locking lever closes if Y is pressed and linear stapler is enabled
            if (!leverLocked)
            {
                LockingLever.transform.localEulerAngles = Vector3.zero;
                print("Locking Lever is closed.");

                // If the tool parts are in valid locking position then lock the tools together
                if (ValidateToolLockingCondition())
                {
                    if (!isBottomHalfMovingInCuttingPlane)
                    {
                        if (bottomTransformLocked)
                        {
                            UnlockBottomTransform();
                        }
                        if (topTransformLocked)
                        {
                            UnlockTopTransform();
                        }
                    }

                    LockToolPartsTogether();
                }
            }
            //Locking lever opens if Y is pressed again and linear stapler is enabled
            else
            {
                LockingLever.transform.localEulerAngles = new Vector3(0, 15, 0);
                print("Locking Lever is open.");

                SeparateToolParts();
            }

            leverLocked = !leverLocked;
        }
    }

    void UpdateLeverWithSensorReading()
    {
        LockingLever.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, new Vector3(0, 15, 0), leverReading);

        // Lerp bottom half model towards top half based on handle reading
        if (leverReading >= 0.03f)
        {
            bottomHalf.transform.localPosition = Vector3.Lerp(bottomHalf.transform.localPosition, Vector3.zero, leverReading);
            bottomHalf.transform.localRotation = Quaternion.Lerp(bottomHalf.transform.localRotation, Quaternion.identity, leverReading);
        }

        leverLocked = leverReading > 0.95f ? true : false;

        if (leverLocked)
        {
            bottomHalf.transform.parent = bottomPartLockingPosition;
        }
        else
        {
            bottomHalf.transform.parent = bottomTracker;
        }
    }

    /// <summary>
    /// When user try to lock the tool parts together, validate it the tool parts meets the locking condition
    /// </summary>
    /// <returns></returns>
    public bool ValidateToolLockingCondition()
    {
        //if (simStates < 2)
        //{
        //    return Mathf.Abs(topHalf.transform.position.z - bottomHalf.transform.position.z) <= attachDepthDifference;
        //    //return Mathf.Abs(globalOperators.m_insertDepth[0] - globalOperators.m_insertDepth[1]) <= attachDepthDifference;
        //}
        //else
        {
            return !attachValidators.Find(v => !v.isTogether);
        }
    }

    /// <summary>
    /// Enable the colliders on tool top part
    /// </summary>
    public void EnableTopPartCollision()
    {
        topPartColliders.ForEach(c => c.enabled = true);
    }
    public void DisableTopPartCollision()
    {
        topPartColliders.ForEach(c => c.enabled = false);
    }

    /// <summary>
    /// Enable the colliders on tool bottom part
    /// </summary>
    public void EnableBottomPartCollision()
    {
        bottomPartColliders.ForEach(c => c.enabled = true);
    }
    public void DisableBottomPartCollision()
    {
        bottomPartColliders.ForEach(c => c.enabled = false);
    }

    /// <summary>
    /// Lock the bottom half of the tool to the top half, stop tracking of the bottom half
    /// </summary>
    public void LockToolPartsTogether()
    {
        // If the tool is locked during insertion phase or last phase then dont let them come too much close together
        if (globalOperators.m_bInsert[0] == 0 && !isBottomHalfMovingInCuttingPlane)
        {
            bottomHalf.transform.parent = bottomPartFullyLockingPosition;
        }
        else
        {
            if (simStates == 0)
            {
                bottomHalf.transform.parent = bottomPartLockingPosition;
            }
            else if (simStates < 2)
            {
                bottomHalf.transform.parent = bottomPartFullyLockingPosition;
            }
            else
            {
                // Align top part to bottom part before lock bottom part onto top part
                Vector3 bottomAttachLocalPosition = bottomPartLastPhaseLockingPosition.localPosition;
                Vector3 topHalfToAttachPointRelativePosition = bottomPartLastPhaseLockingPosition.InverseTransformPoint(topHalf.transform.position);
                bottomPartLastPhaseLockingPosition.position = bottomHalf.transform.position;
                topHalf.transform.position = bottomPartLastPhaseLockingPosition.TransformPoint(topHalfToAttachPointRelativePosition);
                bottomPartLastPhaseLockingPosition.localPosition = bottomAttachLocalPosition;
                topHalf.transform.rotation = bottomHalf.transform.rotation;

                bottomHalf.transform.parent = bottomPartLastPhaseLockingPosition;
                //isTopHalfMovingInCuttingPlane = true; // When user lock tool parts together during last phase, put top tool part onto the moving plane
            }
        }

        // Check if tool is locked onto colon, if yes freeze tool
        if (globalOperators.m_bInsert[0] != 0 || isBottomHalfMovingInCuttingPlane)
        {
            LockTopTransform();
        }

        inAnimation = true;
        StartCoroutine(LockToolAnimation());
    }

    /// <summary>
    /// 
    /// </summary>
    public void JoinColonToolLogic()
    {
        bottomHalf.transform.parent = bottomPartFullyLockingPosition;
        bottomHalf.transform.localPosition = Vector3.zero;
        bottomHalf.transform.localRotation = Quaternion.identity;
        simStates = 1; // Update the simulation step state
        topPartMovingAxisStart = joinedColonFirstLayerSpheres;
        topPartMovingAxisEnd = joinedColon10thLayerSpheres; // ### PROBABLY NEED UPDATE
        DisableBottomPartCollision();
        DisableTopPartCollision();

        if (topTransformLocked)
        {
            UnlockTopTransform();
        }
    }

    /// <summary>
    /// Animation for locking the tool parts
    /// </summary>
    /// <returns></returns>
    public IEnumerator LockToolAnimation()
    {
        Vector3 startLocalPosition = bottomHalf.transform.localPosition;
        Quaternion startLocalRotation = bottomHalf.transform.localRotation;

        for (float t = 0; t < 1; t += Time.deltaTime)
        {
            yield return null;
            bottomHalf.transform.localPosition = Vector3.Lerp(startLocalPosition, Vector3.zero, t);
            bottomHalf.transform.localRotation = Quaternion.Lerp(startLocalRotation, Quaternion.identity, t);
        }

        bottomHalf.transform.localPosition = Vector3.zero;
        bottomHalf.transform.localRotation = Quaternion.identity;
        inAnimation = false;
    }

    /// <summary>
    /// Unlock the bottom half of the tool from the top half, resume tracking of the bottom half
    /// </summary>
    public void SeparateToolParts()
    {
        // If top part movement is locked then unlock it
        if (topTransformLocked)
        {
            UnlockTopTransform();
        }

        if (isBottomHalfMovingInCuttingPlane)
        {
            if (bottomTransformLocked)
            {
                bottomHalf.transform.parent = null;
            }
            else
            {
                bottomHalf.transform.parent = bottomTracker;
                bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;
            }
        }

        if ((!bottomTransformLocked && !isBottomHalfMovingInCuttingPlane) || globalOperators.m_bFinalClosure)
        {
            bottomHalf.transform.parent = bottomTracker;
            if (globalOperators.m_bInsert[0] != 2 && globalOperators.m_bInsert[1] != 2) // If the bottom part is not inserted in colon //### Maybe? or at last step moving plane
            {
                bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;
            }
        }
    }

    /// <summary>
    /// Make sure the tool move along the colon during insertion
    /// </summary>
    /// <param name="controlledObject"></param>
    /// <param name="startPosition"></param>
    /// <param name="endPosition"></param>
    /// <param name="rotateUpDir"></param> Controller object's up direction when align it along the movement direction
    /// <returns></returns> Return float number that indicate if the controlled object is out of controlled path (if outside of 0-1 meaning it moves out of controlled path)
    public float LockToolMovement(Transform controlledObject, Vector3 startPosition, Vector3 endPosition, Vector3 rotateUpDir)
    {
        Vector3 controlledObjectOriginalPosition = controlledObject.position;
        Vector3 objectToStartVector = controlledObject.position - startPosition;
        float movementRange = Vector3.Distance(startPosition, endPosition);
        float objectNormalToStartDistance = Vector3.Dot(objectToStartVector, Vector3.Normalize(endPosition - startPosition));
        controlledObject.position = startPosition + (endPosition - startPosition).normalized * objectNormalToStartDistance; // Place object on the direction vector

        // Rotate the LS tool to align it with the colon
        controlledObject.LookAt(controlledObject.position + (endPosition - startPosition).normalized, rotateUpDir);
        controlledObject.Rotate(0, -90 * Mathf.Sign(rotateUpDir.y), 0, Space.Self);

        // If the controlled object is linear stapler and it is controlling the colon movement then use its original y position control the colon movement


        return objectNormalToStartDistance / movementRange;
    }

    public float GetToolInsertionDepth(Transform controlledObject, Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 objectToStartVector = controlledObject.position - startPosition;
        float movementRange = Vector3.Distance(startPosition, endPosition);
        float objectNormalToStartDistance = Vector3.Dot(objectToStartVector, Vector3.Normalize(endPosition - startPosition));

        return objectNormalToStartDistance / movementRange;
    }


    public float LockToolMovementInPlaneDuringLastStep(Transform controlledObject, Vector3 startPosition, Vector3 endPosition)
    {
        //Debug.DrawLine(startPosition, endPosition, Color.red, 3f);
        Vector3 objectOriginalPosition = controlledObject.position;
        //Vector3 objectToStartVector = controlledObject.position - startPosition;
        //Debug.DrawLine(startPosition, controlledObject.position, Color.green, 3f);
        //float movementRange = Vector3.Distance(startPosition, endPosition);
        //float objectNormalToStartDistance = Vector3.Dot(objectToStartVector, Vector3.Normalize(endPosition - startPosition));
        //controlledObject.position = startPosition + (endPosition - startPosition).normalized * objectNormalToStartDistance; // Place object on the direction vector
        //Debug.DrawLine(startPosition, controlledObject.position, Color.magenta, 3f);
        float depth = Mathf.Clamp((controlledObject.position.z - startPosition.z) / (endPosition.z - startPosition.z), -10, 1);
        controlledObject.position = Vector3.LerpUnclamped(startPosition, endPosition, depth);

        // See which movement segment the object should belong to
        if (controlledObject.position.z < joinedColonSecondLayerLowerSpheresPosition.z)
        {
            float unit = joinedColonSecondLayerLowerSpheresPosition.z - joinedColonFirstLayerLowerSpheresPosition.z;
            float lerpT = Mathf.Clamp((controlledObject.position.z - joinedColonFirstLayerLowerSpheresPosition.z) / unit, lastPhaseBottomFurthestDistance, 1.1f);
            controlledObject.position = Vector3.LerpUnclamped(joinedColonFirstLayerLowerSpheresPosition, joinedColonSecondLayerLowerSpheresPosition, lerpT);
        }
        else if (controlledObject.position.z < joinedColonThirdLayerLowerSpheresPosition.z)
        {
            float unit = joinedColonThirdLayerLowerSpheresPosition.z - joinedColonSecondLayerLowerSpheresPosition.z;
            float lerpT = (controlledObject.position.z - joinedColonSecondLayerLowerSpheresPosition.z) / unit;
            controlledObject.position = Vector3.Lerp(joinedColonSecondLayerLowerSpheresPosition, joinedColonThirdLayerLowerSpheresPosition, lerpT);
        }
        else if (controlledObject.position.z < joinedColonForthLayerLowerSpheresPosition.z)
        {
            float unit = joinedColonForthLayerLowerSpheresPosition.z - joinedColonThirdLayerLowerSpheresPosition.z;
            float lerpT = (controlledObject.position.z - joinedColonThirdLayerLowerSpheresPosition.z) / unit;
            controlledObject.position = Vector3.Lerp(joinedColonThirdLayerLowerSpheresPosition, joinedColonForthLayerLowerSpheresPosition, lerpT);
        }
        else if (controlledObject.position.z < joinedColonFifthLayerLowerSpheresPosition.z)
        {
            float unit = joinedColonFifthLayerLowerSpheresPosition.z - joinedColonForthLayerLowerSpheresPosition.z;
            float lerpT = (controlledObject.position.z - joinedColonForthLayerLowerSpheresPosition.z) / unit;
            controlledObject.position = Vector3.Lerp(joinedColonForthLayerLowerSpheresPosition, joinedColonFifthLayerLowerSpheresPosition, lerpT);
        }
        else
        {
            float unit = joinedColonSixthLayerLowerSpheresPosition.z - joinedColonFifthLayerLowerSpheresPosition.z;
            float lerpT = (controlledObject.position.z - joinedColonFifthLayerLowerSpheresPosition.z) / unit;
            controlledObject.position = Vector3.Lerp(joinedColonFifthLayerLowerSpheresPosition, joinedColonSixthLayerLowerSpheresPosition, lerpT);
        }

        // Move object back to horizontal position
        objectOriginalPosition.z = controlledObject.position.z;
        objectOriginalPosition.y = controlledObject.position.y;
        controlledObject.position = objectOriginalPosition;

        // Rotate the LS tool to align it with the colon
        controlledObject.LookAt(controlledObject.position + Vector3.down, Vector3.forward);
        //controlledObject.Rotate(0, -90 * Mathf.Sign(rotateUpDir.y), 0, Space.Self);

        // Check which layer of sphere is the LS tool closest to
        lastPhaseLockedLayer = 0;
        float closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonFirstLayerLowerSpheresPosition.z);
        bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonFirstLayerLowerSpheresPosition.x;
        if (Mathf.Abs(controlledObject.position.z - joinedColonSecondLayerLowerSpheresPosition.z) < closestLayerDist)
        {
            closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonSecondLayerLowerSpheresPosition.z);
            lastPhaseLockedLayer = 1;
            bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonSecondLayerLowerSpheresPosition.x;
        }
        if (Mathf.Abs(controlledObject.position.z - joinedColonThirdLayerLowerSpheresPosition.z) < closestLayerDist)
        {
            closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonThirdLayerLowerSpheresPosition.z);
            lastPhaseLockedLayer = 2;
            bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonThirdLayerLowerSpheresPosition.x;
        }
        if (Mathf.Abs(controlledObject.position.z - joinedColonForthLayerLowerSpheresPosition.z) < closestLayerDist)
        {
            closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonForthLayerLowerSpheresPosition.z);
            lastPhaseLockedLayer = 3;
            bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonForthLayerLowerSpheresPosition.x;
        }
        if (Mathf.Abs(controlledObject.position.z - joinedColonFifthLayerLowerSpheresPosition.z) < closestLayerDist)
        {
            closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonFifthLayerLowerSpheresPosition.z);
            lastPhaseLockedLayer = 4;
            bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonFifthLayerLowerSpheresPosition.x;
        }
        if (Mathf.Abs(controlledObject.position.z - joinedColonSixthLayerLowerSpheresPosition.z) < closestLayerDist)
        {
            closestLayerDist = Mathf.Abs(controlledObject.position.z - joinedColonSixthLayerLowerSpheresPosition.z);
            lastPhaseLockedLayer = 5;
            bottomLastPhaseX = bottomHalfFrontTip.position.x - joinedColonSixthLayerLowerSpheresPosition.x;
        }

        return depth;
        //return objectNormalToStartDistance / movementRange;
    }

    public void UpdateStaplerMovementTrack(int stapler)
    {
        if (stapler == 0)
        {
            // Update tool moving axis info
            topPartMovingAxisStartPoint = GetPositionMean(topPartMovingAxisStart);
            topPartMovingAxisEndPoint = GetPositionMean(topPartMovingAxisEnd);
        }
        else
        {
            // Update tool moving axis info
            bottomPartMovingAxisStartPoint = GetPositionMean(bottomPartMovingAxisStart);
            bottomPartMovingAxisEndPoint = GetPositionMean(bottomPartMovingAxisEnd);
        }
    }

    /// <summary>
    /// Check if either part of the LS tool is inserting into the colon
    /// </summary>
    public void CheckAndUpdateLStoolInsertionStates()
    {
        if (!globalOperators.m_bSimStart)
        {
            return;
        }

        // Get the distance from each LS tip to each colon opening
        float topToColonA = Vector3.Distance(topHalfFrontTip.position, colonAopeningPos);
        float topToColonB = Vector3.Distance(topHalfFrontTip.position, colonBopeningPos);
        float bottomToColonA = Vector3.Distance(bottomHalfFrontTip.position, colonAopeningPos);
        float bottomToColonB = Vector3.Distance(bottomHalfFrontTip.position, colonBopeningPos);

        // If colon0 is not being inserted
        if (globalOperators.m_bInsert[0] == 0)
        {
            // Check for angle and colon secure and stapler status
            if (topToColonA <= tipProximityCondition)
            {
                topPartMovingAxisStart = colonAsecondLayerSpheres;
                topPartMovingAxisEnd = colonAlastLayerSpheres;
                //if (Vector3.Angle(topHalf.transform.right, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) < angleDifferenceCondition)
                {
                    globalOperators.m_bInsert[0] = 1;
                    topHalfInserted = true;

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Inserting in colon");
                }
            }

            if (bottomToColonA <= tipProximityCondition)
            {
                bottomPartMovingAxisStart = colonAsecondLayerSpheres;
                bottomPartMovingAxisEnd = colonAlastLayerSpheres;
                //if (Vector3.Angle(bottomHalf.transform.right, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) < angleDifferenceCondition)
                {
                    globalOperators.m_bInsert[0] = 2;
                    bottomHalfInserted = true;

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Inserting in colon");
                }
            }

        }
        // If colon1 is not being inserted
        if (globalOperators.m_bInsert[1] == 0)
        {
            // Check for angle and colon secure and stapler status
            if (topToColonB <= tipProximityCondition)
            {
                topPartMovingAxisStart = colonBsecondLayerSpheres;
                topPartMovingAxisEnd = colonBlastLayerSpheres;
                //if (Vector3.Angle(topHalf.transform.right, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) < angleDifferenceCondition)
                {
                    globalOperators.m_bInsert[1] = 1;
                    topHalfInserted = true;

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Inserting in colon");
                }
            }

            if (bottomToColonB <= tipProximityCondition)
            {
                bottomPartMovingAxisStart = colonBsecondLayerSpheres;
                bottomPartMovingAxisEnd = colonBlastLayerSpheres;
                //if (Vector3.Angle(bottomHalf.transform.right, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) < angleDifferenceCondition)
                {
                    globalOperators.m_bInsert[1] = 2;
                    bottomHalfInserted = true;

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Inserting in colon");
                }
            }
        }

        // Check for LS tool removal
        if (globalOperators.m_bInsert[0] == 1) // If top part is in colon0
        {
            if (topToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonAopeningPos - topHalfFrontTip.position, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[0] = 0;
                    topHalfInserted = false;

                    // Put tool back to default local position and rotation
                    topHalf.transform.localPosition = topPartRelativeTrackerPosition;
                    topHalf.transform.localRotation = topPartRelativeTrackerRotation;

                    DisableTopPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[0] == 2 || ColonMovementController.instance.updateMode[0] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[0] == 2) // If bottom part is in colon0
        {
            if (bottomToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonAopeningPos - bottomHalfFrontTip.position, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[0] = 0;
                    bottomHalfInserted = false;

                    // Put tool back to default local position and rotation
                    bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                    bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;

                    DisableBottomPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[0] == 2 || ColonMovementController.instance.updateMode[0] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[1] == 1) // If top part is in colon1
        {
            if (topToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonBopeningPos - topHalfFrontTip.position, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[1] = 0;
                    topHalfInserted = false;

                    // Put tool back to default local position and rotation
                    topHalf.transform.localPosition = topPartRelativeTrackerPosition;
                    topHalf.transform.localRotation = topPartRelativeTrackerRotation;

                    DisableTopPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[1] == 2 || ColonMovementController.instance.updateMode[1] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[1] == 2) // If bottom part is in colon1
        {
            if (bottomToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonBopeningPos - bottomHalfFrontTip.position, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[1] = 0;
                    bottomHalfInserted = false;

                    // Put tool back to default local position and rotation
                    bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                    bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;

                    DisableBottomPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[1] == 2 || ColonMovementController.instance.updateMode[1] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Free");
                }
            }
        }

        // If user finish joining and moved both LS parts out of the colon then enter next phase
        if (simStates == 1 && globalOperators.m_bInsert[0] == 0 && globalOperators.m_bInsert[1] == 0)
        {
            simStates = 2;

            // Stop colon motion control from stapler for joining phase
            ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
            ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
        }
    }

    /// <summary>
    /// Check if either part of the LS tool meets the condition to enter colon model through the opening
    /// </summary>
    public void CheckAndUpdateLStoolInsertionStatesForConfinedLSmovement()
    {
        if (!globalOperators.m_bSimStart)
        {
            return;
        }

        // Get the distance from each LS tip to each colon opening
        float topToColonA = Vector3.Distance(topHalfFrontTip.position, colonAopeningPos);
        float topToColonB = Vector3.Distance(topHalfFrontTip.position, colonBopeningPos);
        float bottomToColonA = Vector3.Distance(bottomHalfFrontTip.position, colonAopeningPos);
        float bottomToColonB = Vector3.Distance(bottomHalfFrontTip.position, colonBopeningPos);

        // If colon is not joined yet
        if (simStates == 0)
        {
            // If colon0 is not being inserted
            if (globalOperators.m_bInsert[0] == 0)
            {
                // Check for angle and colon secure and stapler status
                if (topToColonA <= tipProximityCondition && colonSecuredByForceps[0] && !topHalfColonDetector.stopped)
                {
                    topPartMovingAxisStart = colonAsecondLayerSpheres;
                    topPartMovingAxisEnd = colonA10thLayerSpheres;
                    if (Vector3.Angle(topHalf.transform.right, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) < angleDifferenceCondition)
                    {
                        globalOperators.m_bInsert[0] = 1;
                        topHalfInserted = true;
                        EnableTopPartCollision();
                        // Update tool moving axis info
                        UpdateStaplerMovementTrack(0);

                        // Update tool UI
                        uiController.UpdateToolStatusText("lsTop", "Inserting in colon");
                    }
                }

                if (bottomToColonA <= tipProximityCondition && colonSecuredByForceps[0] && !bottomHalfColonDetector.stopped)
                {
                    bottomPartMovingAxisStart = colonAsecondLayerSpheres;
                    bottomPartMovingAxisEnd = colonA10thLayerSpheres;
                    if (Vector3.Angle(bottomHalf.transform.right, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) < angleDifferenceCondition)
                    {
                        globalOperators.m_bInsert[0] = 2;
                        bottomHalfInserted = true;
                        EnableBottomPartCollision();
                        // Update tool moving axis info
                        UpdateStaplerMovementTrack(1);

                        // Update tool UI
                        uiController.UpdateToolStatusText("lsBottom", "Inserting in colon");
                    }
                }

            }
            // If colon1 is not being inserted
            if (globalOperators.m_bInsert[1] == 0)
            {
                // Check for angle and colon secure and stapler status
                if (topToColonB <= tipProximityCondition && colonSecuredByForceps[1] && !topHalfColonDetector.stopped)
                {
                    topPartMovingAxisStart = colonBsecondLayerSpheres;
                    topPartMovingAxisEnd = colonB10thLayerSpheres;
                    if (Vector3.Angle(topHalf.transform.right, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) < angleDifferenceCondition)
                    {
                        globalOperators.m_bInsert[1] = 1;
                        topHalfInserted = true;
                        EnableTopPartCollision();
                        // Update tool moving axis info
                        UpdateStaplerMovementTrack(0);

                        // Update tool UI
                        uiController.UpdateToolStatusText("lsTop", "Inserting in colon");
                    }
                }

                if (bottomToColonB <= tipProximityCondition && colonSecuredByForceps[1] && !bottomHalfColonDetector.stopped)
                {
                    bottomPartMovingAxisStart = colonBsecondLayerSpheres;
                    bottomPartMovingAxisEnd = colonB10thLayerSpheres;
                    if (Vector3.Angle(bottomHalf.transform.right, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) < angleDifferenceCondition)
                    {
                        globalOperators.m_bInsert[1] = 2;
                        bottomHalfInserted = true;
                        EnableBottomPartCollision();
                        // Update tool moving axis info
                        UpdateStaplerMovementTrack(1);

                        // Update tool UI
                        uiController.UpdateToolStatusText("lsBottom", "Inserting in colon");
                    }
                }
            }
        }

        // Check for LS tool removal
        if (globalOperators.m_bInsert[0] == 1) // If top part is in colon0
        {
            if (topToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonAopeningPos - topHalfFrontTip.position, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[0] = 0;
                    topHalfInserted = false;

                    // Put tool back to default local position and rotation
                    topHalf.transform.localPosition = topPartRelativeTrackerPosition;
                    topHalf.transform.localRotation = topPartRelativeTrackerRotation;

                    DisableTopPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[0] == 2 || ColonMovementController.instance.updateMode[0] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[0] == 2) // If bottom part is in colon0
        {
            if (bottomToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonAopeningPos - bottomHalfFrontTip.position, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[0] = 0;
                    bottomHalfInserted = false;

                    // Put tool back to default local position and rotation
                    bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                    bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;

                    DisableBottomPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[0] == 2 || ColonMovementController.instance.updateMode[0] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[1] == 1) // If top part is in colon1
        {
            if (topToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonBopeningPos - topHalfFrontTip.position, GetPositionMean(topPartMovingAxisStart) - GetPositionMean(topPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[1] = 0;
                    topHalfInserted = false;

                    // Put tool back to default local position and rotation
                    topHalf.transform.localPosition = topPartRelativeTrackerPosition;
                    topHalf.transform.localRotation = topPartRelativeTrackerRotation;

                    DisableTopPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[1] == 2 || ColonMovementController.instance.updateMode[1] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsTop", "Free");
                }
            }
        }
        if (globalOperators.m_bInsert[1] == 2) // If bottom part is in colon1
        {
            if (bottomToColonA >= tipProximityCondition * 2) // If tool is far away from colon opening and outside of the colon
            {
                if (Vector3.Angle(colonBopeningPos - bottomHalfFrontTip.position, GetPositionMean(bottomPartMovingAxisStart) - GetPositionMean(bottomPartMovingAxisEnd)) > 90)
                {
                    globalOperators.m_bInsert[1] = 0;
                    bottomHalfInserted = false;

                    // Put tool back to default local position and rotation
                    bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
                    bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;

                    DisableBottomPartCollision();

                    // If tool is controlling colon movement then stop it from controlling colon movement
                    if (ColonMovementController.instance.updateMode[1] == 2 || ColonMovementController.instance.updateMode[1] == 3)
                    {
                        ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
                    }

                    // Update tool UI
                    uiController.UpdateToolStatusText("lsBottom", "Free");
                }
            }
        }

        // If user finish joining and moved both LS parts out of the colon then enter next phase
        if (simStates == 1 && globalOperators.m_bInsert[0] == 0 && globalOperators.m_bInsert[1] == 0)
        {
            simStates = 2;

            // Stop colon motion control from stapler for joining phase
            ColonMovementController.instance.ChangeFollowStates(0, 0, false, false);
            ColonMovementController.instance.ChangeFollowStates(1, 0, false, false);
        }
    }

    /// <summary>
    /// Check if the tool bottom part is ready to do the last step
    /// </summary>
    public void CheckAndUpdateLStoolTransverseStates()
    {
        // Check if bottom part reach target height and z distance towards joined colon, and right axis is aligned with world right, and tip is within the colon width range
        if (!isBottomHalfMovingInCuttingPlane &&
            Mathf.Abs(bottomHalfFrontTip.position.y - (joinedColonFirstLayerLowerSpheresPosition.y + lastPhaseToolBottomMovingAxisDifference)) < 0.75f &&
            Mathf.Abs(bottomHalfFrontTip.position.z - joinedColonFirstLayerLowerSpheresPosition.z) < 0.75f * 0.5f &&
            Mathf.Abs(bottomHalfFrontTip.position.x - joinedColonFirstLayerLowerSpheresPosition.x) < lastPhaseTipHorizontalProximityCondition &&
            Vector3.Angle(bottomHalf.transform.right, Vector3.right) < angleDifferenceCondition)
        {
            isBottomHalfMovingInCuttingPlane = true;
            canUserLockToolTransform = true;

            // Update tool UI
            uiController.UpdateToolStatusText("lsBottom", "Close to colon opening");
        }
        // Check if bottom part exit cutting plane
        if (isBottomHalfMovingInCuttingPlane &&
            Vector3.Angle((bottomHalfFrontTip.position - joinedColonFirstLayerLowerSpheresPosition), (joinedColonSixthLayerLowerSpheresPosition - joinedColonFirstLayerLowerSpheresPosition)) > 90 &&
            Mathf.Abs(bottomHalfFrontTip.position.z - joinedColonFirstLayerLowerSpheresPosition.z) > 0.75f * 0.5f)
        {
            isBottomHalfMovingInCuttingPlane = false;
            canUserLockToolTransform = false;

            // Put tool back to default local position and rotation
            bottomHalf.transform.localPosition = bottomPartRelativeTrackerPosition;
            bottomHalf.transform.localRotation = bottomPartRelativeTrackerRotation;

            // Update tool UI
            uiController.UpdateToolStatusText("lsBottom", "Free");
        }
    }

    /// <summary>
    /// Toggle the transform lock states of tool top part
    /// </summary>
    public void ToggleTopTransformLock()
    {
        if (!topTransformLocked)
        {
            LockTopTransform();
        }
        else
        {
            UnlockTopTransform();
        }
    }

    /// <summary>
    /// Lock the position and rotation of tool top part
    /// </summary>
    public void LockTopTransform()
    {
        topParentBeforeLock = topHalf.transform.parent;
        topLocalPositionBeforeLock = topHalf.transform.localPosition;
        topLocalRotationBeforeLock = topHalf.transform.localRotation;
        topHalf.transform.parent = null;
        topTransformLocked = true;
    }

    public void UnlockTopTransform()
    {
        topHalf.transform.parent = topParentBeforeLock;
        topHalf.transform.localPosition = topLocalPositionBeforeLock;
        topHalf.transform.localRotation = topLocalRotationBeforeLock;
        topTransformLocked = false;
    }

    public void ToggleBottomTransformLock()
    {
        if (!bottomTransformLocked)
        {
            LockBottomTransform();
        }
        else
        {
            UnlockBottomTransform();
        }
    }

    public void LockBottomTransform()
    {
        bottomParentBeforeLock = bottomHalf.transform.parent;
        bottomLocalPositionBeforeLock = bottomHalf.transform.localPosition;
        bottomLocalRotationBeforeLock = bottomHalf.transform.localRotation;
        bottomHalf.transform.parent = null;
        bottomTransformLocked = true;
    }

    public void UnlockBottomTransform()
    {
        bottomHalf.transform.parent = bottomParentBeforeLock;
        bottomHalf.transform.localPosition = bottomLocalPositionBeforeLock;
        bottomHalf.transform.localRotation = bottomLocalRotationBeforeLock;
        bottomTransformLocked = false;
    }

    /// <summary>
    /// Extra logic handles the collision between the LS and the colon sphere during LS insertion
    /// </summary>
    public void LinearStaplerColonInsertionCollisionInteraction()
    {
        if (globalOperators.m_bInsert[0] == 0 && globalOperators.m_bInsert[1] == 0)
        {
            return;
        }

        Ray topRay = new Ray(topLineEnd.position, topLineStart.position - topLineEnd.position);
        Ray bottomRay = new Ray(bottomLineEnd.position, bottomLineStart.position - bottomLineEnd.position);

        for (int colon = 0; colon < colonSpheres.Count; colon++)
        {
            for (int layer = 0; layer < colonSpheres[colon].Count; layer++)
            {
                // If LS reached this layer
                if (globalOperators.m_insertDepth[colon] * colonSpheres[colon].Count >= layer)
                {


                    Ray partRay = new Ray();
                    if (globalOperators.m_bInsert[colon] == 1)
                    {
                        partRay = topRay;
                    }
                    else if (globalOperators.m_bInsert[colon] == 2)
                    {
                        partRay = bottomRay;
                    }

                    for (int sphere = 0; sphere < 20; sphere++)
                    {
                        ColonStaplerJointBehavior anchor = ColonStaplerJointManager.instance.colonJointAnchors[colon][layer * 20 + sphere];
                        if (MathUtil.DistancePointToLine(partRay, colonSpheres[colon][layer][sphere].position) < staplerInsertionCollisionThreshold)
                        {
                            if (!anchor.gameObject.activeInHierarchy)
                            {
                                anchor.transform.position = MathUtil.ProjectionPointOnLine(partRay, colonSpheres[colon][layer][sphere].position);
                                anchor.detachDistance = staplerInsertionCollisionThreshold;
                                anchor.gameObject.SetActive(true);
                                anchor.AttachColonSphere();
                            }

                            //Vector3 proj = MathUtil.ProjectionPointOnLine(partRay, sphere.position);
                            ////sphere.position = proj + (sphere.position - proj).normalized * Mathf.Clamp(staplerInsertionCollisionThreshold, 0, 0.02f);
                            //sphere.GetComponent<Rigidbody>().AddForce(proj + (sphere.position - proj).normalized * Mathf.Clamp(staplerInsertionCollisionThreshold, 0, 0.1f), ForceMode.VelocityChange);
                        }
                        else
                        {
                            if (anchor.gameObject.activeInHierarchy)
                            {
                                anchor.DetachColonSphere();
                                anchor.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Update the tool insertion depth record and status
    /// </summary>
    public void RecordToolInsertionStatus()
    {
        // Record new data
        insertDepthRecordTimeStamps.Add(Time.time);
        colonAInsertDepthRecord.Add(globalOperators.m_insertDepth[0]);
        colonBInsertDepthRecord.Add(globalOperators.m_insertDepth[1]);

        // Remove data too old
        int toRemove = insertDepthRecordTimeStamps.FindLastIndex(f => Time.time - f > 1);
        colonAInsertDepthRecord.RemoveRange(0, toRemove + 1);
        colonBInsertDepthRecord.RemoveRange(0, toRemove + 1);
        insertDepthRecordTimeStamps.RemoveRange(0, toRemove + 1);

        // Reset insertion states
        isTopInserting = false;
        isTopRemoving = false;
        isBottomInserting = false;
        isBottomRemoving = false;
        isColonAInserting = false;
        isColonARemoving = false;
        isColonBInserting = false;
        isColonBRemoving = false;

        // Check insertion states
        int toCompare = insertDepthRecordTimeStamps.FindLastIndex(f => Time.time - f > lsToolMovingStateThresholdTime);
        if (globalOperators.m_insertDepth[0] - colonAInsertDepthRecord[toCompare] > lsToolMovingStateThreshold)
        {
            isColonAInserting = true;
            colonAfrictionSpheres.ForEach(r => r.AddForce(Vector3.forward * frictionForceStrength, ForceMode.VelocityChange));
        }
        if (globalOperators.m_insertDepth[0] - colonAInsertDepthRecord[toCompare] < -lsToolMovingStateThreshold)
        {
            isColonARemoving = true;
            colonAfrictionSpheres.ForEach(r => r.AddForce(Vector3.back * frictionForceStrength, ForceMode.VelocityChange));
        }
        if (globalOperators.m_insertDepth[1] - colonBInsertDepthRecord[toCompare] > lsToolMovingStateThreshold)
        {
            isColonBInserting = true;
            colonBfrictionSpheres.ForEach(r => r.AddForce(Vector3.forward * frictionForceStrength, ForceMode.VelocityChange));
        }
        if (globalOperators.m_insertDepth[1] - colonBInsertDepthRecord[toCompare] < -lsToolMovingStateThreshold)
        {
            isColonBRemoving = true;
            colonBfrictionSpheres.ForEach(r => r.AddForce(Vector3.back * frictionForceStrength, ForceMode.VelocityChange));
        }
        if (globalOperators.m_bInsert[0] == 1)
        {
            isTopInserting = isColonAInserting;
            isTopRemoving = isColonARemoving;
        }
        if (globalOperators.m_bInsert[1] == 1)
        {
            isTopInserting = isColonBInserting;
            isTopRemoving = isColonBRemoving;
        }
        if (globalOperators.m_bInsert[0] == 2)
        {
            isBottomInserting = isColonAInserting;
            isBottomRemoving = isColonARemoving;
        }
        if (globalOperators.m_bInsert[1] == 2)
        {
            isBottomInserting = isColonBInserting;
            isBottomRemoving = isColonBRemoving;
        }
    }

    public void UpdateColonLayerPosition()
    {
        for (int c = 0; c < colonSpheres.Count; c++)
        {
            for (int l = 0; l < colonSpheres[0].Count; l++)
            {
                colonLayerSpherePositionAVG[c][l] = GetPositionMean(colonSpheres[c][l]);
            }
        }
    }

    public Vector3 GetPositionMean(List<Transform> positions)
    {
        Vector3 sum = Vector3.zero;
        positions.ForEach(t => sum += t.position);

        return sum / positions.Count;
    }

    /// <summary>
    /// Save and auto load user calibrated LS tool local position & eulerangles relative to the tracker transform
    /// </summary>
    public void SaveStapleToolCalibrationData()
    {
        PlayerPrefs.SetFloat("TopHalfLocalPosX", topHalf.transform.localPosition.x);
        PlayerPrefs.SetFloat("TopHalfLocalPosY", topHalf.transform.localPosition.y);
        PlayerPrefs.SetFloat("TopHalfLocalPosZ", topHalf.transform.localPosition.z);
        PlayerPrefs.SetFloat("TopHalfLocalEulerX", topHalf.transform.localEulerAngles.x);
        PlayerPrefs.SetFloat("TopHalfLocalEulerY", topHalf.transform.localEulerAngles.y);
        PlayerPrefs.SetFloat("TopHalfLocalEulerZ", topHalf.transform.localEulerAngles.z);
        PlayerPrefs.SetFloat("BottomHalfLocalPosX", bottomHalf.transform.localPosition.x);
        PlayerPrefs.SetFloat("BottomHalfLocalPosY", bottomHalf.transform.localPosition.y);
        PlayerPrefs.SetFloat("BottomHalfLocalPosZ", bottomHalf.transform.localPosition.z);
        PlayerPrefs.SetFloat("BottomHalfLocalEulerX", bottomHalf.transform.localEulerAngles.x);
        PlayerPrefs.SetFloat("BottomHalfLocalEulerY", bottomHalf.transform.localEulerAngles.y);
        PlayerPrefs.SetFloat("BottomHalfLocalEulerZ", bottomHalf.transform.localEulerAngles.z);

        SaveToolLocalPositionRotation();
    }
    public void LoadStapleToolCalibrationData()
    {
        Vector3 topHalfLocalPos = new Vector3();
        topHalfLocalPos.x = PlayerPrefs.GetFloat("TopHalfLocalPosX");
        topHalfLocalPos.y = PlayerPrefs.GetFloat("TopHalfLocalPosY");
        topHalfLocalPos.z = PlayerPrefs.GetFloat("TopHalfLocalPosZ");
        Vector3 topHalfLocalEuler = new Vector3();
        topHalfLocalEuler.x = PlayerPrefs.GetFloat("TopHalfLocalEulerX");
        topHalfLocalEuler.y = PlayerPrefs.GetFloat("TopHalfLocalEulerY");
        topHalfLocalEuler.z = PlayerPrefs.GetFloat("TopHalfLocalEulerZ");
        Vector3 bottomHalfLocalPos = new Vector3();
        bottomHalfLocalPos.x = PlayerPrefs.GetFloat("BottomHalfLocalPosX");
        bottomHalfLocalPos.y = PlayerPrefs.GetFloat("BottomHalfLocalPosY");
        bottomHalfLocalPos.z = PlayerPrefs.GetFloat("BottomHalfLocalPosZ");
        Vector3 bottomHalfLocalEuler = new Vector3();
        bottomHalfLocalEuler.x = PlayerPrefs.GetFloat("BottomHalfLocalEulerX");
        bottomHalfLocalEuler.y = PlayerPrefs.GetFloat("BottomHalfLocalEulerY");
        bottomHalfLocalEuler.z = PlayerPrefs.GetFloat("BottomHalfLocalEulerZ");

        // Don't load data if there is no data
        if (topHalfLocalEuler == Vector3.zero)
        {
            return;
        }

        topHalf.transform.localEulerAngles = topHalfLocalEuler;
        topHalf.transform.localPosition = topHalfLocalPos;
        bottomHalf.transform.localEulerAngles = bottomHalfLocalEuler;
        bottomHalf.transform.localPosition = bottomHalfLocalPos;
    }

    /// <summary>
    /// Adjust tool local position and eulerangles
    /// </summary>
    /// <param name="direction"></param>
    public void MoveTopHalfLocalXPosition(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.right * direction * positionCalibrationSpeed;
    }
    public void MoveTopHalfLocalYPosition(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.up * direction * positionCalibrationSpeed;
    }
    public void MoveTopHalfLocalZPosition(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.forward * direction * positionCalibrationSpeed;
    }
    public void MoveTopHalfLocalXEulerAngle(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.right * direction * rotationCalibrationSpeed;
    }
    public void MoveTopHalfLocalYEulerAngle(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.up * direction * rotationCalibrationSpeed;
    }
    public void MoveTopHalfLocalZEulerAngle(int direction)
    {
        currentCalibratingHalf = topHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.forward * direction * rotationCalibrationSpeed;
    }
    public void MoveBottomHalfLocalXPosition(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.right * direction * positionCalibrationSpeed;
    }
    public void MoveBottomHalfLocalYPosition(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.up * direction * positionCalibrationSpeed;
    }
    public void MoveBottomHalfLocalZPosition(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = true;
        currentCalibratingDirection = Vector3.forward * direction * positionCalibrationSpeed;
    }
    public void MoveBottomHalfLocalXEulerAngle(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.right * direction * rotationCalibrationSpeed;
    }
    public void MoveBottomHalfLocalYEulerAngle(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.up * direction * rotationCalibrationSpeed;
    }
    public void MoveBottomHalfLocalZEulerAngle(int direction)
    {
        currentCalibratingHalf = bottomHalf.transform;
        calibratingPosition = false;
        currentCalibratingDirection = Vector3.forward * direction * rotationCalibrationSpeed;
    }
    /// <summary>
    /// Things to do when begin and end calibrating rotation
    /// </summary>
    public void BeginCalibrateRotation()
    {
        calibratingToolOriginalParent = currentCalibratingHalf.parent;
        calibrateRotationParent.position = currentCalibratingHalf.position;
        calibrateRotationParent.rotation = Quaternion.identity;
        currentCalibratingHalf.parent = calibrateRotationParent;
    }
    public void EndCalibrateRotation()
    {
        currentCalibratingHalf.parent = calibratingToolOriginalParent;
    }

    /// <summary>
    /// After user orient the real tool to the designated orientation, update the rotation on the tool model in Unity so that it matches up with the real tool
    /// </summary>
    [ShowInInspector]
    public void CalibrateToolTopPartRotation()
    {
        topHalf.transform.rotation = topPartDesignatedCalibrationReference.rotation;
        SaveToolLocalPositionRotation();
    }
    [ShowInInspector]
    public void CalibrateToolBottomPartRotation()
    {
        bottomHalf.transform.rotation = bottomPartDesignatedCalibrationReference.rotation;
        SaveToolLocalPositionRotation();
    }

    public void SaveToolLocalPositionRotation()
    {
        // Save the local position & rotation of the bottom half of the tool relative to the tracker for bottom part
        bottomPartRelativeTrackerPosition = bottomHalf.transform.localPosition;
        bottomPartRelativeTrackerRotation = bottomHalf.transform.localRotation;
        topPartRelativeTrackerPosition = topHalf.transform.localPosition;
        topPartRelativeTrackerRotation = topHalf.transform.localRotation;
    }
}
