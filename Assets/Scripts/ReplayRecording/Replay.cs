﻿using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.UI;


public class Replay : MonoBehaviour {

	Experiment exp;

	//image recording
	public ScreenRecorder MyScreenRecorder;


	//I/O
	StreamReader fileReader;

	//path requires the @symbol at the beginning because we've imported RegularExpressions
	string LogFilePath = @"DataFile\TestLog.txt";
	string currentLogFileLine;


	//keeping track of objects
	Dictionary<String, GameObject> objsInSceneDict;


	// Use this for initialization
	void Start () {
		exp = GameObject.FindGameObjectWithTag ("Experiment").GetComponent<Experiment> ();

		objsInSceneDict = new Dictionary<String, GameObject> ();

		if (exp.isReplay) {
			ReplayScene();
		}
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void ReplayScene(){ //gets called via replay button in the main menu scene
		objsInSceneDict.Clear ();

		
		try 
		{
			// Create an instance of StreamReader to read from a file. 
			// The using statement also closes the StreamReader. 
			using (fileReader = new StreamReader (LogFilePath)) 
			{
				Debug.Log("PROCESSING LOG FILE OH HEYYYY");
				StartCoroutine(ProcessLogFile());
			}
		}
		catch (Exception e) 
		{
			Debug.Log("Invalid log file path. Cannot replay.");
		}
		


	}

	void RecordScreenShot(){
		if(MyScreenRecorder != null){
			//will check if it's supposed to record or not
			//also will wait until endofframe in order to take the shot
			MyScreenRecorder.TakeNextContinuousScreenShot();
		}
		else{
			Debug.Log("No screen recorder attached!");
		}
	}
	
	//THIS PARSING DEPENDS GREATLY ON THE FORMATTING OF THE LOG FILE.
	//IF THE FORMATTING OF THE LOG FILE IS CHANGED, THIS WILL VERY LIKELY HAVE TO CHANGE AS WELL.
	IEnumerator ProcessLogFile(){

		long currentFrame = 0;

		if (LogFilePath != "") { 

			fileReader = new StreamReader (LogFilePath);
		
			currentLogFileLine = fileReader.ReadLine (); //the first line in the file should be the date.
			currentLogFileLine = fileReader.ReadLine (); //the second line should be the first real line with logged data

			string[] splitLine;

			bool hasFinishedSettingFrame = false;
		
			//PARSE
			while (currentLogFileLine != null) {

				splitLine = currentLogFileLine.Split(',');

				if(splitLine.Length > 0){
					for (int i = 0; i < splitLine.Length; i++){

						//0 -- timestamp
						if (i == 0){

						}
						else if(i == 1){
							long readFrame = long.Parse(splitLine[i]);
							while(readFrame != currentFrame){
								currentFrame++;
								hasFinishedSettingFrame = true;

								if(exp.isSavingToPng){
									RecordScreenShot();
								}

								yield return 0;

								//Debug.Log(currentFrame);
							}
						}
						//2 -- name of object
						else if (i == 2){
							string objName = splitLine[i];

							GameObject objInScene;

							if(objsInSceneDict.ContainsKey(objName)){
								
								objInScene = objsInSceneDict[objName];

							}
							else{

								objInScene = GameObject.Find(objName);

								if(objInScene != null){
									objsInSceneDict.Add(objName, objInScene);
								}
								else{ //if the object is not in the scene, but is in the log file, we should instantiate it!
										//we could also check for the SPAWNED keyword
									//parse out name of object from ID
									string fruitID = "";
									if(objName.Contains("banana")){
										string[] splitFruitName = objName.Split('a');
										fruitID = splitFruitName[splitFruitName.Length -1]; //get the last thing in the split
									}
									if(objName.Contains("Cherry")){
										string[] splitFruitName = objName.Split('y');
										fruitID = splitFruitName[splitFruitName.Length -1]; //get the last thing in the split
									}

									//data.Split(new string[] { "xx" }, StringSplitOptions.None)

									string[] splitIDName = objName.Split(new string[] {fruitID}, StringSplitOptions.None);
									objInScene = exp.myFoodController.SpawnObjectByName(splitIDName[0], int.Parse(fruitID));

									if(objInScene != null){ //if it did spawn the object...
										objsInSceneDict.Add(objName, objInScene);
									}
								}
							}
							if(objInScene != null){
								//NOW MOVE & ROTATE THE OBJECT.
								string loggedProperty = splitLine[i+1];
								
								if(loggedProperty == "POSITION"){
									
									float posX = float.Parse(splitLine[i+2]);
									float posY = float.Parse(splitLine[i+3]);
									float posZ = float.Parse(splitLine[i+4]);
									
									objInScene.transform.position = new Vector3(posX, posY, posZ);
									
								}
								else if(loggedProperty == "ROTATION"){
									float rotX = float.Parse(splitLine[i+2]);
									float rotY = float.Parse(splitLine[i+3]);
									float rotZ = float.Parse(splitLine[i+4]);

									Quaternion newRot = Quaternion.Euler(rotX, rotY, rotZ);

									objInScene.transform.rotation = newRot;

								}
								else if(loggedProperty == "ALPHA"){
									Food spawnedFood = objInScene.GetComponent<Food>();
									if(spawnedFood != null){
										float newAlpha = float.Parse(splitLine[i+2]);
										spawnedFood.SetAlpha(newAlpha);
									}
									else{
										Debug.Log("no food component!");
									}
								}
								else if(loggedProperty == "DESTROYED"){
									Debug.Log("Destroying object! " + objInScene.name);
									GameObject.Destroy(objInScene);
								}

								else if(loggedProperty == "CAMERA_ENABLED"){
									Camera objCamera = objInScene.GetComponent<Camera>();
									if(objCamera != null){
										if(splitLine[i+2] == "true" || splitLine[i+2] == "True"){
											objCamera.enabled = true;
										}
										else{
											objCamera.enabled = false;
										}
									}
								}
								else if(loggedProperty == "DESTROYED"){
									Debug.Log("Destroying object! " + objInScene.name);
									GameObject.Destroy(objInScene);
								}
							}
							else{
								Debug.Log("REPLAY: No obj in scene named " + objName);
							}
						}

					}


				}

				//read the next line at the end of the while loop
				currentLogFileLine = fileReader.ReadLine ();

				if(hasFinishedSettingFrame){ //
					//yield return new WaitForFixedUpdate();	 //REPLAY BASED ON FIXEDUPDATE FOR FRAMERATE INDEPENDENCE (logging was also logged via FixedUpdate())
					yield return 0; //WHILE LOGGED ON FIXED UPDATE, REPLAY ON UPDATE TO GET A CONSTANT #RENDERED FRAMES

					hasFinishedSettingFrame = false;

				}
			}
		}

		//take the last screenshot
		RecordScreenShot();
		yield return 0;
	}

}
