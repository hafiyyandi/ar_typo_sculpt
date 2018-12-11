using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class sphereController : MonoBehaviour {

	private int index;
	private Renderer rend;

	private bool isHit = false;
	private GameObject dragLock;

	private float def_x;
	private float def_y;
	private float def_z;

	private float plane_x;
	private float plane_y;
	private float plane_z;

	private string location;

	// Use this for initialization
	void Start () {
		rend = GetComponent<Renderer>();
		dragLock = GameObject.FindGameObjectWithTag("draglock");
	}
	
	// Update is called once per frame
	void Update () {
		if (isHit) { //if it's hit by raycast, drag the sphere with the camera
			Vector3 cam_pos = dragLock.transform.position;
			//cam_pos.z += 0.001f;
			Drag (cam_pos);
		}	
	}

	void Indexing(int i){
		//Debug.Log ("Object number: " + i);
		index = i;
	}

	void Initializing (string[] Str){
		//0 is index. 1-3 are original glyphs coords. 4-6 are plane coordinate.
		int id = int.Parse (Str [0]);
		//Debug.Log (id);
		index = id;

		def_x = float.Parse (Str [1]);
		def_y = float.Parse (Str [2]);
		def_z = float.Parse (Str [3]);

		plane_x = float.Parse (Str [4]);
		plane_y = float.Parse (Str [5]);
		plane_z = float.Parse (Str [6]);

		location = Str [7];

			
	}

	void Drag(Vector3 newpos){

		//Set the main Color of the Material to green
		rend.material.shader = Shader.Find("_Color");
		rend.material.SetColor("_Color", Color.green);

		this.transform.position = newpos;

		//send message to letterloader to update linerenderer

		string[] tempStorage = new string[4];
		tempStorage[0] = index.ToString();
		tempStorage[1] = newpos.x.ToString();
		tempStorage[2] = newpos.y.ToString();
		tempStorage[3] = newpos.z.ToString();

		GameObject LetterLoader = GameObject.FindGameObjectWithTag ("letterloader");
		LetterLoader.SendMessage ("moveLine", tempStorage);	
	}

	void GetHit(){
		isHit = !isHit; //toggle isHit
		if (isHit) {
			Debug.Log ("Is hitting sphere number: " + index);
		} else {
			Debug.Log ("Is releasing sphere number: " + index);

			//send message to server to save new coordinate
			StartCoroutine (Upload());
		}
	}

	IEnumerator Upload() {

		Debug.Log ("HEYO");

		string trans_x = (this.transform.position.x - plane_x - def_x).ToString();
		string trans_y = (this.transform.position.y - plane_y - def_y).ToString();
		string trans_z = (this.transform.position.z - plane_z - def_z).ToString();

		//CAN'T GET THIS POST METHOD TO WORK AND BE PARSED CORRECTLY IN SERVER

//		List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
//		formData.Add( new MultipartFormDataSection("index="+index+"&x="+trans_x+"&y="+trans_y+"&z="+trans_z) );
//
//		UnityWebRequest www = UnityWebRequest.Post("http://68.183.20.22:8080/api/save", formData);
//		yield return www.SendWebRequest();
//
//		if(www.isNetworkError || www.isHttpError) {
//			Debug.Log(www.error);
//		}
//		else {
//			Debug.Log("Form upload complete!");
//		}


		//USE GET METHOD INSTEAD

		string URL = "http://68.183.20.22:8080/api/save?loc="+location+"&index="+index+"&x="+trans_x+"&y="+trans_y+"&z="+trans_z;
		// using some web API that uses REST and spits out JSON
		UnityWebRequest request = UnityWebRequest.Get(URL);
		yield return request.SendWebRequest ();

		if (request.isNetworkError) {
			Debug.Log (request.error);		
		} else {
			Debug.Log("Form upload complete!");
		}
	}
}
