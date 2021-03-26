﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CEMSIM.Network;
using CEMSIM.GameLogic;
using CEMSIM.GameLogic;


namespace CEMSIM{

	public class ServerItemManager : MonoBehaviour
	{
		public List<GameObject> itemList = new List<GameObject>();	//This List contains all items to be instantiated. To use: drag gameobject into the list in Unity IDE
		public Vector3 spawnPosition;								//This Vector3 controls where the spawned item is located
		public List<Item> itemManageList = new List<Item>();		//This List contains all items to be managed.



	    // Start is called before the first frame update
	    void Start()
	    {
	    	CollectItems();

	    	
	    }

	    // Update is called once per frame
	    void FixedUpdate()
	    {	
	    	SendItemStatus();
	    }


	    private void SendItemStatus(){
	    	foreach(Item item in itemManageList){

	    		//Brodcast item position via UDP
	    		ServerSend.BrodcastItemPosition(item);
	    		//Brodcast item rotation via UDP
	    		ServerSend.BrodcastItemRotation(item);
	    		//Brodcast item owner via TCP
	    		//*****TO DO: Brodcast ownership information via TCP********
	    		
	    	}
	    }



	    /// <summary>
        /// Add all items under ItemManager into list
        /// </summary>
	    private void CollectItems(){
	    	int id = 0;
	    	int owner = 0;
			foreach (GameObject itemPrefab in itemList)
			{ 
				GameObject item = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
				itemManageList.Add( new Item(item, id, owner ) );
			    id++;
			}
	    }

	    /// <summary>
        /// Update an item's position
        /// </summary>
        /// <param name="itemID"> The id of the item to be updated </param>
        /// <param name="position"> The vector3 position of the item </param>
	    public void UpdateItemPosition(int itemId, Vector3 position){
	    	itemManageList[itemId].gameObject.transform.position = position;
	    }

	    /// <summary>
        /// Update an item's rotation
        /// </summary>
        /// <param name="itemID"> The id of the item to be updated </param>
        /// <param name="position"> The vector3 position of the item </param>
	    public void UpdateItemRotation(int itemId, Quaternion rotation){
	    	itemManageList[itemId].gameObject.transform.rotation = rotation;
	    }



 

	}
}