﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class RodBlueprintParticleIndividualizer : MonoBehaviour
{

    public ObiSolver solver;
    public ObiRod instance;
    private ObiRodBlueprint blueprint;
    public bool doDebugPrintout = false;

    public int particleCount;

    private List<ObiParticleAttachment> activeParticleAttachments = new List<ObiParticleAttachment>();

    private Matrix4x4 solver2World;
    public Vector3 WorldPositionOfParticle(int p) { return solver2World.MultiplyPoint3x4(solver.positions[p]); }

    public float GetTension() { return instance.CalculateLength() / instance.restLength; }

    private void Start()
    {
        if (solver == null) solver = GetComponentInParent<ObiSolver>();
        if(instance == null) instance = GetComponent<ObiRod>();
        blueprint = (ObiRodBlueprint)instance.blueprint;

        particleCount = solver.positions.count;

        solver2World = solver.transform.localToWorldMatrix;

        ObiParticleAttachment[] particleAttachments = gameObject.GetComponents<ObiParticleAttachment>();
        foreach (ObiParticleAttachment p in particleAttachments)
        {
            activeParticleAttachments.Add(p);
        }
    }

    public ObiParticleAttachment CreateParticleAttachment(int i, Transform t)
    {
        return CreateParticleAttachment(new List<int>() { i }, t);
    }

    public ObiParticleAttachment CreateParticleAttachment(List<int> i, Transform t)
    {
        ObiParticleAttachment particleAttachment = gameObject.AddComponent<ObiParticleAttachment>();
        particleAttachment.name = i[0].ToString();
        particleAttachment.target = t;
        particleAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Static;
        particleAttachment.constrainOrientation = true;
        particleAttachment.compliance = 0;
        particleAttachment.breakThreshold = Mathf.Infinity;

        ObiParticleGroup g = ScriptableObject.CreateInstance<ObiParticleGroup>();
        g.SetSourceBlueprint(blueprint);
        g.particleIndices = i;

        particleAttachment.particleGroup = g;
        particleAttachment.enabled = true;

        activeParticleAttachments.Add(particleAttachment);

        return particleAttachment;
    }

    public ObiParticleAttachment CreateNewParticleAttachmentClosestTo(Transform t)
    {
        ObiParticleAttachment particleAttachment = gameObject.AddComponent<ObiParticleAttachment>();
        particleAttachment.target = t;
        particleAttachment.attachmentType = ObiParticleAttachment.AttachmentType.Static;
        particleAttachment.constrainOrientation = true;
        particleAttachment.compliance = 0;
        particleAttachment.breakThreshold = Mathf.Infinity;

        ObiParticleGroup g = ScriptableObject.CreateInstance<ObiParticleGroup>();
        g.SetSourceBlueprint(blueprint);
        g.particleIndices = new List<int>() { GetClosestParticleToPosition(t.position) };

        particleAttachment.particleGroup = g;
        particleAttachment.enabled = true;

        activeParticleAttachments.Add(particleAttachment);

        return particleAttachment;
    }

    public float ShortestDistanceToAnyParticle(Vector3 position)
    {
        return Vector3.Distance(WorldPositionOfParticle(GetClosestParticleToPosition(position)), position);
    }

    public int GetClosestParticleToPosition(Vector3 position)
    {
        float closestParticleDistance = float.MaxValue;
        int closestParticleInt = -1;
        for (int i = 0; i < solver.positions.count; i++)
        {
            float dist = Vector3.Distance(WorldPositionOfParticle(i), position);
            if (dist < closestParticleDistance)
            {
                closestParticleDistance = dist;
                closestParticleInt = i;
            }
        }

        if (closestParticleInt == -1) Debug.LogError("Closest particle to position " + position.ToString() + " could not be resolved!");
        return closestParticleInt;
    }

    public Vector3 GetAveragePositionOfClosestParticlesToPosition(Vector3 position, int amountOfParticles)
    {
        List<int> foundParticles = new List<int>();
        for (int o = 0; o < amountOfParticles; o++)
        {
            float closestParticleDistance = float.MaxValue;
            int closestParticleInt = -1;
            for (int i = 0; i < solver.positions.count; i++)
            {
                if (foundParticles.Contains(i)) continue;
                float dist = Vector3.Distance(WorldPositionOfParticle(i), position);
                if (dist < closestParticleDistance)
                {
                    closestParticleDistance = dist;
                    closestParticleInt = i;
                }
            }
            foundParticles.Add(closestParticleInt);
        }

        Vector3 averagePosition = Vector3.zero;
        foreach(int p in foundParticles)
        {
            averagePosition += WorldPositionOfParticle(p);
        }

        return averagePosition / foundParticles.Count;
    }
}
