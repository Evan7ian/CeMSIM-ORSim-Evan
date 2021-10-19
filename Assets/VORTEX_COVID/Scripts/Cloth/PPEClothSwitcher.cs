﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PPEClothSwitcher : MonoBehaviour
{
    public GameObject[] objectsWithClothMesh;
    public MaterialVisibilityToggle handMeshMaterialToggle;
    private MultiGloveToggle multiGloveToggle;

    void Start()
    {
        multiGloveToggle = GetComponentInParent<MultiGloveToggle>();
    }

    public void Show(int index)
    {
        if (index >= 0 && index < objectsWithClothMesh.Length)
        {
            objectsWithClothMesh[index].SetActive(true);
            handMeshMaterialToggle.HideMaterial(index);
        }
        else
            Debug.LogWarning("ClothSwitcher index out of range.");
    }

    public void Hide(int index)
    {
        if (index >= 0 && index < objectsWithClothMesh.Length)
        {
            objectsWithClothMesh[index].SetActive(false);
            handMeshMaterialToggle.ShowMaterial(index);
        }
        else
            Debug.LogWarning("ClothSwitcher index out of range.");
    }

    public void HideAll()
    {
        multiGloveToggle.GloveUnequipped();
        if (multiGloveToggle.currentGloveEquippedCount == 0)
        {
            Debug.Log("Hide all was called.");
            foreach (GameObject clothMesh in objectsWithClothMesh)
                clothMesh.SetActive(false);

            handMeshMaterialToggle.ShowAll();

        }

    }
}
