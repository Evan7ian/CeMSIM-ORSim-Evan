﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;

[RequireComponent(typeof(HVRGrabbable))]
public class GloveStretch : MonoBehaviour
{
    public enum GloveStretchState { AtWrist, UnderGown, OverGown }

    public PPEOptionPoint[] stretchReferencePoints;
    public MultiGloveToggle multiGlove;
    public GloveStretchState stretchState { get; private set; }

    private HVRGrabbable grabbable;
    private PPEOptionPoint closestPoint;
    private Transform originalParent;
    private bool grabbed = false;
    private bool pointsActivated = false;
    private bool gownEquipped = false;

    void Start()
    {
        originalParent = transform.parent;
        gameObject.SetActive(false);
        SetPointsActive(false);

        grabbable = GetComponent<HVRGrabbable>();
    }

    void Update()
    {
        if (grabbed)
        {
            if (!pointsActivated)
                SetPointsActive(true);

            float minimumDistance = float.MaxValue;
            int minimumIndex = -1;

            for (int i = 0; i < stretchReferencePoints.Length; i++)
            {
                float distance = Vector3.Distance(transform.position, stretchReferencePoints[i].transform.position);

                if (distance < minimumDistance)
                {
                    minimumDistance = distance;
                    minimumIndex = i;
                }
            }

            if (closestPoint != stretchReferencePoints[minimumIndex])
            {
                if (closestPoint)
                    closestPoint.Unhover();

                closestPoint = stretchReferencePoints[minimumIndex];
                closestPoint.Hover();
            }
        }
    }

    public void GownEquipped()
    {
        gownEquipped = true;
    }

    public void GownUnequipped()
    {
        gownEquipped = false;
    }

    public void GloveEquipped(HVRHandGrabber grabber)
    {
        if (multiGlove.currentGloveEquippedCount == 0)
        {
            if (gownEquipped)
            {
                GrabStretchPoint(grabber);
            }
            else
            {
                stretchReferencePoints[0].Hover();
                stretchReferencePoints[0].Select();
            }
        }
    }

    public void GloveUnequipped()
    {
        if (closestPoint && multiGlove.currentGloveEquippedCount == 0)
            closestPoint.Unhover();
    }

    public void GrabStretchPoint(HVRHandGrabber grabber)
    {
        if (grabber)
        {
            transform.parent = null;
            gameObject.SetActive(true);
            grabber.TryGrab(grabbable, true);
            grabbed = true;
        }
    }

    public void SelectClosestPoint()
    {
        closestPoint.Select();

        gameObject.SetActive(false);
        SetPointsActive(false);
        transform.parent = originalParent;

        grabbed = false;
    }

    public void SetStretchState(int state)
    {
        stretchState = (GloveStretchState)state;
    }

    private void SetPointsActive(bool state)
    {
        foreach (PPEOptionPoint point in stretchReferencePoints)
            point.gameObject.SetActive(state);

        pointsActivated = state;
    }
}
