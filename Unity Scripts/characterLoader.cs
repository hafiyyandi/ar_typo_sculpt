using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEngine.XR.iOS;
using UnityEngine.Networking;

using System.Linq;
using SimpleJSON;

public class characterLoader : MonoBehaviour {

	public string test_text = "H"; //string of the location name
	private JSONNode allglyphs; //default coords of character or letter glyphs
	private JSONNode database; //translation amount for each coord in glyphs
	public GameObject spawnee;
	public float scale = 0.0025f;
	public LineRenderer lRend;
	public float x_dist = 5.0f;
	public float y_dist = 5.0f;

	private bool isGlyphDone = false;
	private bool isLocDone = false;
	private bool hasSaved = false; //has this location been saved before?

	private bool wasHidden = false;
	private List<GameObject> sphereList = new List<GameObject> ();

	private  List<Vector3> positions = new List<Vector3>();

	private float lat;
	private float lng;
	private string loc_type;
	private string loc_name;

	//View Mode variables
	private bool isAnimLineInitialized = false;
	List<GameObject> animLines = new List<GameObject>();
	List<float> animLineDists = new List<float> ();
	List<float> animLineCounters = new List<float> ();
	List<Vector3> animLineDestinations = new List<Vector3> ();
	List<Vector3> animLineOrigins = new List<Vector3> ();
	public float LineDrawSpeed = 1.0f;
	public GameObject animLineObj;

	private bool isEditMode = true;

	// Use this for initialization
	void Start () {
		StartCoroutine (GETGlyphs ());
		StartCoroutine (GETLocation ());
	}

	void OnGUI()
	{
		string modeString = isEditMode == false ? "VIEW" : "EDIT";
		if (GUI.Button(new Rect(Screen.width -150.0f, 0.0f, 150.0f, 100.0f), modeString))
		{
			isEditMode = !isEditMode; //toggle the state
			Debug.Log ("NOW ON EDIT: " + isEditMode);
		}

	}
	
	// Update is called once per frame
	void Update () {
		if (isGlyphDone && isLocDone) {

			if (isEditMode) { //if app is in edit mode

				//Debug.Log ("APP IN EDIT MODE");

				if (wasHidden) {

					//Destroy previous animLines
					GameObject[] goArray2 = GameObject.FindGameObjectsWithTag("animlineobj");
					if (goArray2.Length > 0)
					{
						Debug.Log ("DESTROYING PREVIOUS ANIMLINES");
						for (int i = 0; i < goArray2.Length; i++)
						{
							GameObject go2 = goArray2[i];
							Destroy (go2);
						}
					}

					isAnimLineInitialized = false; //!IMPORTANT

					Debug.Log ("UNHIDE SPHERES & WHITE LINES");

					//UNHide all spheres
					if (sphereList.Count > 0)
					{
						Debug.Log ("UNHIDING SPHERES");
						for (int i = 0; i < sphereList.Count; i++)
						{
							GameObject go = sphereList[i];
							go.SetActive (true);
						}
					}

					//UNHide white line
					lRend.gameObject.SetActive (true);
					Debug.Log ("UNHIDING WHITELINE");

					wasHidden = false;
				}



				lRend.positionCount = positions.Count;
				Vector3[] posArray = positions.ToArray ();
				lRend.SetPositions (posArray);


			} else {
				
				//Debug.Log ("APP IN VIEW MODE");

				if (!isAnimLineInitialized) { //if anim lines has not been initialized yet
					init_AnimLine ();
				} else {
					update_AnimLine (); //if anim lines have been previously been initialized, update/animate the lines
				}
			}



		}
		
	}

	//Load DEFAULT glyphs coordinates from a server
	IEnumerator GETGlyphs(){

		// using some web API that uses REST and spits out JSON
		UnityWebRequest request = UnityWebRequest.Get("http://68.183.20.22:8080/api/glyphs");
		yield return request.SendWebRequest ();

		if (request.isNetworkError) {
			Debug.Log (request.error);		
		} else {
			Debug.Log ("GETTING RESPONSE FOR GETGLYPH()");

			//var resultTxt = request.downloadHandler.text;

			// now let's parse this 
			allglyphs = JSON.Parse(request.downloadHandler.text);

		}
	}

	//Check if location has been saved in DB. If yes, get the saved coord translations
	IEnumerator GET_DBCoords (string location){
		Debug.Log ("CHECKING LOCATION NAME: " + location + " IN DB");
		string URL = "http://68.183.20.22:8080/api/coords?loc=" + location;

		// using some web API that uses REST and spits out JSON
		UnityWebRequest request = UnityWebRequest.Get(URL);
		yield return request.SendWebRequest ();

		if (request.isNetworkError) {
			Debug.Log (request.error);		
		} else {

			var resultTxt = request.downloadHandler.text;
			if (resultTxt == "NONE") {
				hasSaved = false;
			} else {
				Debug.Log ("DB DB DB");
				Debug.Log (request.downloadHandler.text);
				hasSaved = true;
				database = JSON.Parse(request.downloadHandler.text);	
				Debug.Log ("DB COUNT: "+ database [0]["translations"].Count);
			}
				
			isLocDone = true;

		}
	}



	//Get Lat & Lng of current device
	IEnumerator GETLocation(){

		// First, check if user has location service enabled
		if (!Input.location.isEnabledByUser)
			yield break;

		// Start service before querying location
		Input.location.Start();
		Debug.Log ("GETTING LOCATION!");

		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
		{
			yield return new WaitForSeconds(1);
			maxWait--;
		}

		// Service didn't initialize in 20 seconds
		if (maxWait < 1)
		{
			print("Timed out");
			yield break;
		}

		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed)
		{
			print("Unable to determine device location");
			yield break;
		}
		else
		{
			// Access granted and location value could be retrieved
			//print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
			Debug.Log ("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
			lat = Input.location.lastData.latitude;
			lng = Input.location.lastData.longitude;

			//Get location name using GMaps Reverse Geocoding
			StartCoroutine (GETLocName (lat, lng));
		}

		// Stop service if there is no need to query location updates continuously
		Input.location.Stop();
	}


	//Get location name using GMaps Reverse Geocoding
	IEnumerator GETLocName(float lat, float lng){
		var s_lat = lat.ToString ();
		var s_lng = lng.ToString ();
		string URL = "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + s_lat + "," + s_lng + "&key=AIzaSyAkME1xnJuC3gQIH-8sTaE3ysWv6LgzIoU";

		// using some web API that uses REST and spits out JSON
		UnityWebRequest request = UnityWebRequest.Get(URL);
		yield return request.SendWebRequest ();

		if (request.isNetworkError) {
			Debug.Log (request.error);		
		} else {
			Debug.Log ("GETTING RESPONSE FOR GET LOCNAME!!");

			// we got something so lets show the results
			var resultTxt = request.downloadHandler.text;
			//Debug.Log(resultTxt);

			// now let's parse this 
			var results = JSON.Parse(request.downloadHandler.text);
			Debug.Log(results["status"]);

			loc_type = results ["results"] [0] ["address_components"] [0] ["types"] [0].Value;
			loc_name = results ["results"] [0] ["address_components"] [0] ["short_name"].Value;

			string name0 = results ["results"] [0] ["address_components"] [0] ["short_name"].Value.ToString ();
			string name1 = results ["results"] [0] ["address_components"] [1] ["short_name"].Value.ToString ();
			string name2 = results ["results"] [0] ["address_components"] [2] ["short_name"].Value.ToString ();

			//Debug.Log (loc_name);
			//Debug.Log (loc_type);

			//test_text = loc_name.ToString () + " " + loc_type.ToString ();
			test_text = name0 + " " + name1 + " " + name2;
			//test_text = "hello world";

			StartCoroutine (GET_DBCoords (test_text));

		}	
	}

	//find the index that has the drawing command of specified character
	int findIndexOfGlyph(string Char){ 

		//spell out numbers
		switch (Char)
		{
		case "0":
			Char = "zero";
			break;
		case "1":
			Char = "one";
			break;
		case "2":
			Char = "two";
			break;
		case "3":
			Char = "three";
			break;
		case "4":
			Char = "four";
			break;
		case "5":
			Char = "five";
			break;
		case "6":
			Char = "six";
			break;
		case "7":
			Char = "seven";
			break;
		case "8":
			Char = "eight";
			break;
		case "9":
			Char = "nine";
			break;
		}

		for (var i = 0; i < allglyphs.Count; i++) {
			var currentChar = allglyphs [i] ["name"].Value;
			if (currentChar == Char) {
				return i;
			}
		}
		return 1000; //return impossible index if character is not found
	}

	//Initialize coordinates to draw
	void drawLetters(Vector3 plane_pos){
		Debug.Log ("DRAWING LETTERS: "+test_text);
		int index = 0;
		int line = 4;
		foreach (char c in test_text) {
			var ind = findIndexOfGlyph (c.ToString ());
			if (c.ToString () == " ") { //if character is a space
				line--;
				index = 0;
			}
			var commands = allglyphs [ind] ["commands"];

			lRend.positionCount = commands.Count;

			for (var i = 0; i < commands.Count; i++) {
				//Debug.Log (commands [i] ["type"].Value);
				string type = commands [i] ["type"].Value;

//				if (type == "Z")
//					break;
				
				if (type == "C") {
//					var x1 = float.Parse (commands [i] ["x1"].Value) * scale;
//					var y1 = float.Parse (commands [i] ["y1"].Value) * scale;
					//Instantiate(spawnee, new Vector3(x1, y1, 0), Quaternion.identity);

//					var x2 = float.Parse (commands [i] ["x2"].Value) * scale;
//					var y2 = float.Parse (commands [i] ["y2"].Value) * scale;
					//Instantiate(spawnee, new Vector3(x2, y2, 0), Quaternion.identity);
				}
					

				if (type != "Z") {
					//Debug.Log (commands [i] ["x"].Value);
					//Debug.Log (commands [i] ["y"].Value);

					var x = float.Parse (commands [i] ["x"].Value) * scale * 0.1f + (index * x_dist * 0.2f) - 0.5f;
					var y = float.Parse (commands [i] ["y"].Value) * scale * 0.1f + (line * y_dist * 0.2f);
					var z = 0.0f;
					var coord_index = positions.Count; //positions.Count is the index of the current coordinate

					Vector3 ori_pos = new Vector3 (x, y, z);
					if (hasSaved) { //if DB has location, translate the coordinates according to DB data. if not move ahead.
						ori_pos = transCoord (ori_pos, coord_index);	
					}

					//Debug.Log (y);

					var adj_x = ori_pos.x + plane_pos.x;
					//Debug.Log ("ori_pos.x = " + ori_pos.x);

					var adj_y = ori_pos.y + plane_pos.y;
					var adj_z = ori_pos.z + plane_pos.z;

					//draw sphere
					GameObject go = Instantiate(spawnee, new Vector3(adj_x, adj_y, adj_z), Quaternion.identity);
					//send message to prefab to assign index in array


					//send & save default coord and initialization data to sphere instance
					string[] tempStorage = new string[8];
					tempStorage[0] = coord_index.ToString();
					tempStorage[1] = x.ToString();
					tempStorage[2] = y.ToString();
					tempStorage[3] = z.ToString();
					tempStorage[4] = plane_pos.x.ToString();
					tempStorage[5] = plane_pos.y.ToString();
					tempStorage[6] = plane_pos.z.ToString();
					tempStorage[7] = test_text;

					//go.SendMessage("Indexing", coord_index);
					go.SendMessage("Initializing", tempStorage);

					Vector3 pos = new Vector3 (adj_x, adj_y, adj_z);
					positions.Add (pos);
				}

			}
			index++;
		}

		isGlyphDone = true;
	}

	//Translate coordinate using found data in database
	Vector3 transCoord (Vector3 pos, int index){
		var x = pos.x;
		var y = pos.y;
		var z = pos.z;

		for (var i = 0; i < database [0]["translations"].Count; i++) {
			//Debug.Log ("HERE");
			//Debug.Log (index);
			var db_index = int.Parse(database [0]["translations"] [i] ["index"].Value);
			//Debug.Log (db_index);
			if (index == db_index) {
				Debug.Log ("TRANSLATING COORDINATE INDEX: " + index);

				Debug.Log (x);
				Debug.Log (float.Parse(database [0]["translations"] [i] ["x"].Value));

				x = pos.x + float.Parse(database [0]["translations"] [i] ["x"].Value);
				Debug.Log ("db adjusted: "+x);

				y = pos.y + float.Parse(database [0]["translations"] [i] ["y"].Value);
				z = pos.z + float.Parse(database [0]["translations"] [i] ["z"].Value);
				//break;
			}
			
		}

		Vector3 final_pos = new Vector3 (x, y, z);
		return final_pos;	
	}

	void moveLine (string[] Str){
		//Debug.Log ("moving line?");

		int id = int.Parse (Str [0]);
		//Debug.Log (id);

		float new_x = float.Parse (Str [1]);
		float new_y = float.Parse (Str [2]);
		float new_z = float.Parse (Str [3]);

		//Debug.Log (new_x);
		//Debug.Log (new_y);
		//Debug.Log (new_z);
		Vector3 new_pos = new Vector3 (new_x, new_y, new_z);
		positions [id] = new_pos;	
	}

	void init_AnimLine (){

		//Hide all spheres
		GameObject[] goArray = GameObject.FindGameObjectsWithTag("sphere");
		if (goArray.Length > 0)
		{
			Debug.Log ("HIDING SPHERES");
			sphereList.Clear ();
			for (int i = 0; i < goArray.Length; i++)
			{
				GameObject go = goArray[i];
				sphereList.Add (go);
				go.SetActive (false);
			}
		}

		//Hide white line
		lRend.gameObject.SetActive (false);
		Debug.Log ("HIDING WHITE LINE");

		wasHidden = true;

		//Destroy previous animLines
		GameObject[] goArray2 = GameObject.FindGameObjectsWithTag("animlineobj");
		if (goArray2.Length > 0)
		{
			Debug.Log ("DESTROYING PREVIOUS ANIMLINES");
			for (int i = 0; i < goArray2.Length; i++)
			{
				GameObject go2 = goArray2[i];
				Destroy (go2);
			}
		}

		//Reset all animLines Lists, because the code below use .Add() – maybe use indexing instead of add?
		animLines.Clear();
		animLineDists.Clear ();
		animLineCounters.Clear ();
		animLineDestinations.Clear ();
		animLineOrigins.Clear ();
		Debug.Log ("CLEARING ANIMLINES LISTS");

		//Initialize the actual coords & lines
		for (var i = 0; i < positions.Count; i++) {

			Debug.Log ("INITIALIZING ANIM LINE: " + i);

			GameObject animLine = Instantiate(animLineObj, new Vector3(0.0f, 0.0f, 0.0f), Quaternion.identity);
			animLines.Add (animLine);

			LineRenderer lRend = animLines[i].AddComponent<LineRenderer>();

			var thisLine = animLines [i].GetComponent<LineRenderer> ();

			thisLine.SetColors (Color.red,Color.blue);
			thisLine.material = new Material(Shader.Find("Particles/Additive"));

			AnimationCurve curve = new AnimationCurve();
			curve.AddKey(0, 0.004f);
			curve.AddKey(1, 0.004f);

			thisLine.widthCurve = curve;
			thisLine.SetPosition(0,positions[i]);
			animLineOrigins.Add (positions [i]);

			if (i == positions.Count - 1) {
				animLineDists.Add (Vector3.Distance (positions [i], positions [0]));
				animLineDestinations.Add (positions [0]);

				thisLine.SetPosition (1, positions [0]);

			} else {
				animLineDists.Add (Vector3.Distance (positions [i], positions [i+1]));
				animLineDestinations.Add (positions [i+1]);

				thisLine.SetPosition(1,positions[i+1]);
			}

			//initialize counter for each line
			animLineCounters.Add (0.0f);
					
		}

		isAnimLineInitialized = true; //anim lines initializing is complete, set its bool to true
		
	}

	void update_AnimLine(){
		for (var i = 0; i < animLines.Count; i++) {

			//if (animLineCounters [i] < animLineDists [i]) {

				animLineCounters [i] += .005f / LineDrawSpeed;

				float d = Mathf.Lerp (0, animLineDists [i], animLineCounters [i]);

				Vector3 pointA = animLineOrigins [i];
				Vector3 pointB = animLineDestinations [i];

				Vector3 pointBetween = d * Vector3.Normalize (pointB - pointA) + pointA;

				var thisLine = animLines [i].GetComponent<LineRenderer> ();
				thisLine.SetPosition (1, pointBetween);

			float distOri = Mathf.Abs(Vector3.Distance (pointB, pointA));
			float distNow = Mathf.Abs(Vector3.Distance (pointBetween, pointA));

			if (distOri == distNow) {
				animLineCounters [i] = 0.0f;
			}


			//} else {
			//	animLineCounters [i] = 0.0f;
			//}
		}

		isAnimLineInitialized = true; //anim lines initializing is complete, set its bool to true
	}

}
