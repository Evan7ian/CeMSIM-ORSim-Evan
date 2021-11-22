﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LungBehavior : MonoBehaviour
{
    public Material defaultMaterial;
    public Material enterMaterial;

    public bool inLung; //TO DO: Only handles the insertion of one tool, need to update for multiple tool tracking
    public bool syringeInLung;

    private PatientManager patient;
    private MeshRenderer meshRenderer;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = this.GetComponent<MeshRenderer>();
        patient = PatientManager.Instance;
    }

    public void NeedleDecompression()
    {
        inLung = true;
            meshRenderer.material = enterMaterial;
            Debug.Log("Needle decompression event");
            PatientEvents.Instance.TriggerNeedleDecompression();      
            // PatientManager.Instance.pulseEventManager.TriggerPulseAction(Pulse.CDM.PulseAction.TensionPneumothorax, 0); 
            ScenarioManager.Instance.tensionPneumothorax = false;
            ScenarioManager.Instance.pneumothoraxSeverity = 0; 
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Tool")
        {
            // other.GetComponent<NeedleBehavior>().NeedleInserted(true);
            NeedleDecompression();
        }

        if(other.tag == "Syringe")
        {
            syringeInLung = true;
            meshRenderer.material = enterMaterial;
            other.GetComponent<NeedleBehavior>().NeedleInserted(true);
            string med = other.GetComponent<Medication>().medication.ToString();
            Debug.Log(med + " administered");
            patient.pulseEventManager.AdministerMedication(med);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Tool")
        {
            inLung = false;
            meshRenderer.material = defaultMaterial;
            other.GetComponent<NeedleBehavior>().NeedleInserted(false);
        }
    }
}
