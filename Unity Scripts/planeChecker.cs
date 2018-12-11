using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.iOS;

public class planeChecker : MonoBehaviour {

	public GameObject LetterLoader;
	private int counter;

	// Use this for initialization
	void Start () {
		Debug.Log ("PLANE INITIALIZED!!");

		Debug.Log ("PLANE COUNTER: " + GameObject.Find ("PlaneManager").GetComponent<planeCount> ().plane_count);
		counter = GameObject.Find ("PlaneManager").GetComponent<planeCount> ().plane_count;

		//Vector3 position = UnityARMatrixOps.GetPosition (this.transform);
		Vector3 position = new Vector3 (this.transform.position.x, this.transform.position.y, this.transform.position.z);
		//Debug.Log (position.x);
		//Debug.Log (position.y);
		//Debug.Log (position.z);

		if (counter == 0) {
			LetterLoader = GameObject.FindGameObjectWithTag ("letterloader");

			//send message to letterloader to start drawing letters
			LetterLoader.SendMessage ("drawLetters", position);	
			GameObject.Find ("PlaneManager").GetComponent<planeCount> ().plane_count++;

			
		}

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
